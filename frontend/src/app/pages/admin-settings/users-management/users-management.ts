import { HttpErrorResponse } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { EMPTY, Subject, catchError, debounceTime, finalize, switchMap, tap } from 'rxjs';
import {
  TpButton,
  TpChip,
  TpInput,
  TpInputAutocompleteMultiselect,
  TpInputDatePicker,
  TpSearchBar,
  TpSelectOption,
  TpTable,
  TpTableCellContent,
  TpTableColumn,
  TpTableConfig,
  TpTableData,
  TpTableFilterContent,
  TpTablePageChange,
  TpTableSort,
  TpInputValue,
} from '@team-platform/ui';
import { FilterOperator, TemporalPartType } from '../../../core/enums/filter';
import { FilterCriterionRequest } from '../../../core/models/common/filter';
import { GetUsersRequest, UserListItem } from '../../../core/models/users/get-users';
import { UserService } from '../../../core/services/user-service';

type UserFilterField =
  | 'employeeCode'
  | 'firstName'
  | 'lastName'
  | 'displayName'
  | 'username'
  | 'email';

@Component({
  selector: 'app-users-management',
  imports: [
    TpButton,
    TpChip,
    TpInput,
    TpInputAutocompleteMultiselect,
    TpInputDatePicker,
    TpSearchBar,
    TpTable,
    TpTableCellContent,
    TpTableFilterContent,
  ],
  templateUrl: './users-management.html',
  styleUrl: './users-management.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersManagement implements OnInit {
  private readonly userService = inject(UserService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly filterChanges = new Subject<void>();
  private readonly userRequests = new Subject<GetUsersRequest>();

  readonly users = signal<UserListItem[]>([]);
  readonly totalUsers = signal(0);
  readonly currentPage = signal(1);
  readonly pageSize = signal(20);
  readonly globalSearch = signal('');
  readonly textFilters = signal<Partial<Record<UserFilterField, string>>>({});
  readonly statusFilters = signal<string[]>([]);
  readonly lastLoginDateFilter = signal<Date | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  readonly statusOptions = ['Active', 'Inactive'];

  private readonly activeSort = signal<TpTableSort>({
    key: 'displayName',
    direction: 'asc',
  });

  private readonly columns: readonly TpTableColumn<UserListItem>[] = [
    { key: 'employeeCode', title: 'Employee Code', sortable: true, width: '150px' },
    { key: 'firstName', title: 'First Name', sortable: true, width: '150px' },
    { key: 'lastName', title: 'Last Name', sortable: true, width: '150px' },
    { key: 'displayName', title: 'Display Name', sortable: true, width: '180px' },
    { key: 'username', title: 'Username', sortable: true, width: '160px' },
    { key: 'email', title: 'Email', sortable: true, width: '220px' },
    { key: 'status', title: 'Status', sortable: true, width: '130px' },
    {
      key: 'lastLoginAt',
      title: 'Last Login At',
      sortable: true,
      width: '190px',
      value: (user) => this.formatDateTime(user.lastLoginAt),
    },
  ];

  readonly tableData = computed<TpTableData<UserListItem>>(() => ({
    rows: this.users(),
    dataMode: 'server',
    totalRows: this.totalUsers(),
  }));

  readonly tableState = computed(() => ({ loading: { enabled: this.loading() } }));

  readonly tableConfig = computed<TpTableConfig<UserListItem>>(() => ({
    columns: this.columns,
    maxCellContentLength: 64,
    filter: { enabled: true },
    sort: { enabled: true, value: this.activeSort() },
    pagination: {
      enabled: true,
      page: this.currentPage(),
      pageSize: this.pageSize(),
      pageSizeOptions: [10, 20, 50, 100],
      prevPageButtonDisabled: this.loading(),
      nextPageButtonDisabled: this.loading(),
    },
    reload: { enabled: true, buttonDisabled: this.loading() },
  }));

  constructor() {
    this.filterChanges
      .pipe(debounceTime(500), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.loadUsers());

    this.userRequests
      .pipe(
        switchMap((request) => {
          this.loading.set(true);
          this.loadError.set(null);

          return this.userService.getUsers(request).pipe(
            tap((result) => {
              this.users.set(result.items);
              this.totalUsers.set(result.totalItems);
            }),
            catchError((error: unknown) => {
              this.users.set([]);
              this.totalUsers.set(0);
              this.loadError.set(this.getErrorMessage(error, 'Could not load users.'));
              return EMPTY;
            }),
            finalize(() => this.loading.set(false)),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  ngOnInit(): void {
    this.loadUsers();
  }

  onGlobalSearch(value: string): void {
    this.globalSearch.set(value.trim());
    this.scheduleFilteredLoad();
  }

  onTextFilterChange(field: string, value: TpInputValue): void {
    if (!this.isUserFilterField(field)) return;

    this.textFilters.update((filters) => ({
      ...filters,
      [field]: typeof value === 'string' ? value : value === null ? '' : String(value),
    }));
    this.scheduleFilteredLoad();
  }

  onStatusFilterChange(values: TpSelectOption[]): void {
    this.statusFilters.set(values.filter((value): value is string => typeof value === 'string'));
    this.scheduleFilteredLoad();
  }

  onLastLoginDateChange(value: Date | null): void {
    this.lastLoginDateFilter.set(value);
    this.scheduleFilteredLoad();
  }

  onPaginationChange(change: TpTablePageChange): void {
    this.currentPage.set(change.page);
    this.pageSize.set(change.pageSize);
    this.loadUsers();
  }

  onSortChange(sort: TpTableSort | null): void {
    if (!sort) return;
    this.activeSort.set(sort);
    this.currentPage.set(1);
    this.loadUsers();
  }

  reloadUsers(): void {
    this.loadUsers();
  }

  textFilterValue(field: string): string {
    return this.isUserFilterField(field) ? (this.textFilters()[field] ?? '') : '';
  }

  columnTitle(key: string): string {
    return this.columns.find((column) => column.key === key)?.title ?? key;
  }

  formatDateTime(value: string | null): string {
    if (!value) return '—';

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '—';

    return new Intl.DateTimeFormat(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short',
    }).format(date);
  }

  private scheduleFilteredLoad(): void {
    this.currentPage.set(1);
    this.filterChanges.next();
  }

  private isUserFilterField(field: string): field is UserFilterField {
    return [
      'employeeCode',
      'firstName',
      'lastName',
      'displayName',
      'username',
      'email',
    ].includes(field);
  }

  private loadUsers(): void {
    const filterCriteria = this.buildFilterCriteria();
    const sort = this.activeSort();
    const request: GetUsersRequest = {
      currentPage: this.currentPage(),
      itemsPerPage: this.pageSize(),
      searchGlobalText: this.globalSearch().trim() || undefined,
      filterCriteria,
      sortFields: [
        {
          fieldName: sort.key,
          isAscending: sort.direction === 'asc',
        },
      ],
    };

    this.userRequests.next(request);
  }

  private buildFilterCriteria(): FilterCriterionRequest[] {
    const criteria: FilterCriterionRequest[] = Object.entries(this.textFilters())
      .filter((entry): entry is [UserFilterField, string] => !!entry[1]?.trim())
      .map(([fieldName, value]) => ({
        fieldName,
        operator: FilterOperator.StartsWith,
        values: [value.trim()],
      }));

    const statuses = this.statusFilters();
    if (statuses.length > 0) {
      criteria.push({
        fieldName: 'status',
        operator: FilterOperator.Equals,
        values: statuses,
      });
    }

    const date = this.lastLoginDateFilter();
    if (date) {
      const dateAtUtcMidnight = new Date(
        Date.UTC(date.getFullYear(), date.getMonth(), date.getDate()),
      );
      criteria.push({
        fieldName: 'lastLoginAt',
        operator: FilterOperator.Equals,
        values: [dateAtUtcMidnight.toISOString()],
        dateTimeFilterOptions: {
          offsetMinutes: -date.getTimezoneOffset(),
          temporalPartType: TemporalPartType.Date,
        },
      });
    }

    return criteria;
  }

  private getErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof HttpErrorResponse) {
      const apiMessage = error.error?.message;
      if (typeof apiMessage === 'string' && apiMessage.trim()) return apiMessage;
    }

    return fallback;
  }
}
