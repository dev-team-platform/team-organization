import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  TpButton,
  TpChip,
  TpInput,
  TpInputAutocompleteMultiselect,
  TpInputDatePicker,
  TpInputFilter,
  TpInputValue,
  TpSearchBar,
  TpSelectOption,
  TpTable,
  TpTableActionContent,
  TpTableCellContent,
  TpTableColumn,
  TpTableConfig,
  TpTableData,
  TpTableFilterContent,
  TpTablePageChange,
  TpTableSort,
  TpTextButton,
  type TpTableActionColumnConfig,
} from '@team-platform/ui';
import { EMPTY, Subject, catchError, debounceTime, finalize, switchMap, tap } from 'rxjs';
import { MainSkeleton } from '../../../components/main-skeleton/main-skeleton';
import { DEFAULT_CHIP_MAPPING } from '../../../core/constants/common';
import { FilterOperator, TemporalPartType } from '../../../core/enums/common';
import { RoleName } from '../../../core/enums/uam';
import { UserGender, UserStatus } from '../../../core/enums/users';
import { ChipMapping, FilterCriterionRequest } from '../../../core/models/common';
import { GetUsersRequest } from '../../../core/models/users';
import { AuthService } from '../../../core/services/auth-service';
import { ClientCorrelationService } from '../../../core/services/client-correlation-service';
import { ToastService } from '../../../core/services/toast-service';
import { UserService } from '../../../core/services/user-service';
import { isNotEmpty, resolveErrorMessage } from '../../../core/utils/common';
import { UserListItem } from './../../../core/models/users';
import {
  BirthDateFilterMenu,
  type BirthDateFilterValue,
} from './birth-date-filter-menu/birth-date-filter-menu';
import { EditUserDialog } from './edit-user-dialog/edit-user-dialog';
import { NewUserDialog } from './new-user-dialog/new-user-dialog';

type UserFilterField =
  'employeeCode' | 'firstName' | 'lastName' | 'displayName' | 'username' | 'email';

@Component({
  selector: 'app-users-management',
  imports: [
    TpButton,
    TpChip,
    TpInput,
    TpInputAutocompleteMultiselect,
    TpInputDatePicker,
    TpInputFilter,
    TpSearchBar,
    TpTable,
    TpTableActionContent,
    TpTableCellContent,
    TpTableFilterContent,
    TpTextButton,
    DatePipe,
    NewUserDialog,
    EditUserDialog,
    BirthDateFilterMenu,
    MainSkeleton,
  ],
  templateUrl: './users-management.html',
  styleUrl: './users-management.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersManagement implements OnInit {
  private readonly userService = inject(UserService);
  private readonly authService = inject(AuthService);
  private readonly clientCorrelationService = inject(ClientCorrelationService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly filterChanges = new Subject<void>();
  private readonly userRequests = new Subject<GetUsersRequest>();
  private lastUsersRequest: GetUsersRequest | null = null;

  protected readonly actionColumn: TpTableActionColumnConfig = {
    enabled: true,
    menuWidth: '160px',
  };

  readonly users = signal<UserListItem[]>([]);
  readonly totalUsers = signal(0);
  readonly currentPage = signal(1);
  readonly pageSize = signal(20);
  readonly globalSearch = signal('');
  readonly textFilters = signal<Partial<Record<UserFilterField, string>>>({});
  readonly roleFilters = signal<string[]>([]);
  readonly statusFilters = signal<string[]>([]);
  readonly genderFilters = signal<string[]>([]);
  readonly statusOptionSearch = signal('');
  readonly roleOptionSearch = signal('');
  readonly genderOptionSearch = signal('');
  readonly lastLoginDateFilter = signal<Date | null>(null);
  readonly birthDateFilter = signal<BirthDateFilterValue | null>(null);
  readonly birthDateFilterOpen = signal(false);
  readonly birthDateFilterResetKey = signal(0);
  readonly birthDateFilterDisplay = computed(() => this.birthDateFilter()?.displayValue ?? '');
  readonly loading = signal(true);
  readonly initialLoading = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly newUserDialogOpen = signal(false);
  readonly editUserDialogOpen = signal(false);
  readonly selectedUser = signal<UserListItem | null>(null);
  readonly statusOptions = [UserStatus.Active, UserStatus.Inactive, UserStatus.Pending];
  readonly genderOptions = [UserGender.Male, UserGender.Female, UserGender.Unknown];
  readonly roleOptions = [RoleName.SuperAdmin, RoleName.Admin, RoleName.Employee];
  readonly filteredStatusOptions = computed(() =>
    this.filterSelectOptions(this.statusOptions, this.statusOptionSearch()),
  );
  readonly filteredGenderOptions = computed(() =>
    this.filterSelectOptions(this.genderOptions, this.genderOptionSearch()),
  );
  readonly filteredRoleOptions = computed(() =>
    this.filterSelectOptions(this.roleOptions, this.roleOptionSearch()),
  );

  readonly statusMapping: Record<UserStatus, ChipMapping> = {
    [UserStatus.Active]: { color: 'emerald', displayValue: UserStatus.Active.toUpperCase() },
    [UserStatus.Inactive]: { color: 'red', displayValue: UserStatus.Inactive.toUpperCase() },
    [UserStatus.Pending]: { color: 'amber', displayValue: UserStatus.Pending.toUpperCase() },
  };

  readonly genderMapping: Record<UserGender, ChipMapping> = {
    [UserGender.Male]: { color: 'blue', displayValue: UserGender.Male.toUpperCase() },
    [UserGender.Female]: { color: 'amber', displayValue: UserGender.Female.toUpperCase() },
    [UserGender.Unknown]: { color: 'gray', displayValue: UserGender.Unknown.toUpperCase() },
  };

  readonly roleMapping: Record<RoleName, ChipMapping> = {
    [RoleName.SuperAdmin]: { color: 'purple', displayValue: 'SUPER ADMIN' },
    [RoleName.Admin]: { color: 'blue', displayValue: 'ADMIN' },
    [RoleName.Employee]: { color: 'gray', displayValue: 'EMPLOYEE' },
  };

  getStatus(user: any): ChipMapping {
    const status = user['status'] as UserStatus;
    return this.statusMapping[status] ?? DEFAULT_CHIP_MAPPING;
  }

  getRole(user: any): ChipMapping {
    const role = user['roleName'] as RoleName;
    return this.roleMapping[role] ?? DEFAULT_CHIP_MAPPING;
  }

  getGender(user: any): ChipMapping {
    const gender = user['gender'] as UserGender;
    return this.genderMapping[gender] ?? this.genderMapping[UserGender.Unknown];
  }

  protected dateOnlyAsUtc(value: string): string {
    return `${value}T00:00:00Z`;
  }

  private readonly activeSort = signal<TpTableSort>({
    key: 'displayName',
    direction: 'asc',
  });

  private readonly columns: readonly TpTableColumn<UserListItem>[] = [
    { key: 'employeeCode', title: 'Employee Code', sortable: true, width: '100px' },
    { key: 'firstName', title: 'First Name', sortable: true, width: '100px' },
    { key: 'lastName', title: 'Last Name', sortable: true, width: '100px' },
    { key: 'displayName', title: 'Display Name', sortable: true, width: '100px' },
    { key: 'username', title: 'Username', sortable: true, width: '100px' },
    { key: 'email', title: 'Email', sortable: true, width: '250px', maxCellContentLength: 255 },
    { key: 'roleName', title: 'Role', sortable: true, width: '200px' },
    { key: 'status', title: 'Status', sortable: true, width: '200px' },
    { key: 'gender', title: 'Gender', sortable: true, width: '200px' },
    { key: 'birthDate', title: 'Birth Date', sortable: true, width: '250px' },
    { key: 'lastLoginAt', title: 'Last Login At', sortable: true, width: '100px' },
  ];

  readonly tableData = computed<TpTableData<UserListItem>>(() => ({
    rows: this.users(),
    dataMode: 'server',
    totalRows: this.totalUsers(),
  }));

  isDisabledAction(user: unknown) {
    const checkedUser = user as UserListItem;
    return (
      checkedUser.roleName === RoleName.SuperAdmin ||
      this.authService.currentUser()?.id === checkedUser.id
    );
  }

  readonly tableState = computed(() => ({ loading: { enabled: this.loading() } }));

  readonly tableConfig = computed<TpTableConfig<UserListItem>>(() => ({
    columns: this.columns,
    maxCellContentLength: 64,
    maxHeight: '65vh',
    rowHeight: '40px',
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
    rowTrackBy: (row, _) => row.id,
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
          this.clientCorrelationService.startAction();

          return this.userService.getUsers(request).pipe(
            tap((result) => {
              this.users.set(result.items);
              this.totalUsers.set(result.totalItems);
              this.initialLoading.set(false);
            }),
            catchError((error: unknown) => {
              this.users.set([]);
              this.totalUsers.set(0);
              this.loadError.set(resolveErrorMessage(error));
              this.initialLoading.set(false);
              return EMPTY;
            }),
            finalize(() => {
              this.loading.set(false);
              this.clientCorrelationService.completeAction();
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();

    effect(() => {
      const error = this.loadError();
      if (isNotEmpty(error)) {
        this.toastService.enqueue({
          type: 'error',
          content: error!,
        });
      }
    });
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

  onGenderFilterChange(values: TpSelectOption[]): void {
    this.genderFilters.set(values.filter((value): value is string => typeof value === 'string'));
    this.scheduleFilteredLoad();
  }

  onRoleFilterChange(values: TpSelectOption[]): void {
    this.roleFilters.set(values.filter((value): value is string => typeof value === 'string'));
    this.scheduleFilteredLoad();
  }

  onStatusOptionSearch(value: string): void {
    this.statusOptionSearch.set(value);
  }

  onGenderOptionSearch(value: string): void {
    this.genderOptionSearch.set(value);
  }

  onRoleOptionSearch(value: string): void {
    this.roleOptionSearch.set(value);
  }

  onLastLoginDateChange(value: Date | null): void {
    this.lastLoginDateFilter.set(value);
    this.scheduleFilteredLoad();
  }

  onBirthDateFilterMenuOpen(): void {
    this.birthDateFilterResetKey.update((key) => key + 1);
  }

  applyBirthDateFilter(filter: BirthDateFilterValue): void {
    this.birthDateFilter.set(filter);
    this.birthDateFilterOpen.set(false);
    this.scheduleFilteredLoad();
  }

  clearBirthDateFilter(): void {
    if (this.birthDateFilter() !== null) {
      this.birthDateFilter.set(null);
      this.scheduleFilteredLoad();
    }
    this.birthDateFilterOpen.set(false);
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

  openNewUserDialog(): void {
    this.newUserDialogOpen.set(true);
  }

  openEditUserDialog(user: UserListItem): void {
    this.selectedUser.set(user);
    this.editUserDialogOpen.set(true);
  }

  handleUserEdited(updatedUser: UserListItem): void {
    this.users.update((users) =>
      users.map((user) => (user.id === updatedUser.id ? updatedUser : user)),
    );
  }

  handleUserCreated(): void {
    if (this.lastUsersRequest) {
      this.userRequests.next(this.lastUsersRequest);
    } else {
      this.loadUsers();
    }
  }

  textFilterValue(field: string): string {
    return this.isUserFilterField(field) ? (this.textFilters()[field] ?? '') : '';
  }

  columnTitle(key: string): string {
    return this.columns.find((column) => column.key === key)?.title ?? key;
  }

  private scheduleFilteredLoad(): void {
    this.currentPage.set(1);
    this.filterChanges.next();
  }

  private isUserFilterField(field: string): field is UserFilterField {
    return ['employeeCode', 'firstName', 'lastName', 'displayName', 'username', 'email'].includes(
      field,
    );
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

    this.lastUsersRequest = request;
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

    const genders = this.genderFilters();
    if (genders.length > 0) {
      criteria.push({
        fieldName: 'gender',
        operator: FilterOperator.Equals,
        values: genders,
      });
    }

    const roles = this.roleFilters();
    if (roles.length > 0) {
      criteria.push({
        fieldName: 'roleName',
        operator: FilterOperator.Equals,
        values: roles,
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
          temporalPartType: TemporalPartType.None,
        },
      });
    }

    const birthDateFilter = this.birthDateFilter();
    if (birthDateFilter?.mode === 'exact') {
      const exactParts: Array<[number | null, TemporalPartType]> = [
        [birthDateFilter.date, TemporalPartType.Date],
        [birthDateFilter.month, TemporalPartType.Month],
        [birthDateFilter.year, TemporalPartType.Year],
      ];

      for (const [value, temporalPartType] of exactParts) {
        if (value === null) continue;
        criteria.push({
          fieldName: 'birthDate',
          operator: FilterOperator.Equals,
          values: [String(value)],
          dateTimeFilterOptions: { offsetMinutes: 0, temporalPartType },
        });
      }
    } else if (birthDateFilter?.mode === 'age') {
      const today = this.todayDate();

      if (birthDateFilter.fromAge !== null) {
        criteria.push({
          fieldName: 'birthDate',
          operator: FilterOperator.LessThanOrEquals,
          values: [this.formatDateOnly(this.shiftDateByYears(today, -birthDateFilter.fromAge))],
        });
      }

      if (birthDateFilter.toAge !== null) {
        const oldestBirthDate = this.shiftDateByYears(today, -(birthDateFilter.toAge + 1));
        oldestBirthDate.setDate(oldestBirthDate.getDate() + 1);
        criteria.push({
          fieldName: 'birthDate',
          operator: FilterOperator.GreaterThanOrEquals,
          values: [this.formatDateOnly(oldestBirthDate)],
        });
      }
    }

    return criteria;
  }

  private filterSelectOptions<T extends string>(
    options: readonly T[],
    query: string,
  ): readonly T[] {
    const normalizedQuery = query.trim().toLowerCase();
    return normalizedQuery
      ? options.filter((option) => option.toLowerCase().includes(normalizedQuery))
      : options;
  }

  private formatDateOnly(date: Date): string {
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }

  private todayDate(): Date {
    const now = new Date();
    return new Date(now.getFullYear(), now.getMonth(), now.getDate());
  }

  private shiftDateByYears(date: Date, yearOffset: number): Date {
    const year = date.getFullYear() + yearOffset;
    const month = date.getMonth();
    const day = Math.min(date.getDate(), new Date(year, month + 1, 0).getDate());
    return new Date(year, month, day);
  }
}
