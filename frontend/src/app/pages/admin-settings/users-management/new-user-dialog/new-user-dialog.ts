import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  model,
  output,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormField, disabled, form, pattern, required } from '@angular/forms/signals';
import type { TpSelectOption, TpSelectOptionDisplayFn } from '@team-platform/ui';
import {
  TpButton,
  TpInput,
  TpInputDatePicker,
  TpInputSingleselect,
  TpRadio,
} from '@team-platform/ui';
import { EMPTY, catchError, finalize, tap } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { TitleDialog } from '../../../../components/title-dialog/title-dialog';
import { DEFAULT_ERROR_MESSAGE } from '../../../../core/constants/common';
import {
  EMAIL_LOCAL_PART_REGEX,
  USERNAME_EMAIL_ALREADY_EXISTS_ERROR_MESSAGE,
  USERNAME_REGEX,
} from '../../../../core/constants/users';
import { RoleCode, RoleName } from '../../../../core/enums/uam';
import { UserGender } from '../../../../core/enums/users';
import { ErrorResponse } from '../../../../core/models/common';
import type { CreateNewUserRequest } from '../../../../core/models/users';
import { AuthService } from '../../../../core/services/auth-service';
import { ClientCorrelationService } from '../../../../core/services/client-correlation-service';
import { ToastService } from '../../../../core/services/toast-service';
import { UserService } from '../../../../core/services/user-service';
import { isNotEmpty, resolveErrorMessage } from '../../../../core/utils/common';
import { buildFullEmail } from '../../../../core/utils/user';

interface NewUserFormValue extends Omit<CreateNewUserRequest, 'roleCode' | 'birthDate'> {
  role: TpSelectOption | null;
  birthDate: Date | null;
}

@Component({
  selector: 'app-new-user-dialog',
  imports: [
    TitleDialog,
    TpButton,
    TpInput,
    TpInputDatePicker,
    TpInputSingleselect,
    TpRadio,
    FormField,
  ],
  templateUrl: './new-user-dialog.html',
  styleUrl: './new-user-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewUserDialog {
  private readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly clientCorrelationService = inject(ClientCorrelationService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly open = model(false);
  readonly created = output<void>();
  readonly createdError = signal<string | null>(null);
  protected readonly emailDomain = environment.emailDomain;

  protected readonly isCreating = signal(false);
  private readonly isEmailSyncedWithUsername = signal(true);

  protected readonly isSuperAdmin = computed(
    () => this.authService.currentUser()?.roleName === RoleName.SuperAdmin,
  );

  protected readonly roleOptions = computed<readonly TpSelectOption[]>(() =>
    this.isSuperAdmin() ? [RoleCode.Admin, RoleCode.Employee] : [RoleCode.Employee],
  );
  protected readonly genders = UserGender;

  private readonly formModel = signal<NewUserFormValue>(this.createInitialFormValue());
  protected readonly selectedGender = computed(() => this.formModel().gender);
  protected readonly userForm = form(this.formModel, (user) => {
    disabled(user.firstName, () => this.isCreating());
    disabled(user.lastName, () => this.isCreating());
    disabled(user.displayName, () => this.isCreating());
    disabled(user.username, () => this.isCreating());
    disabled(user.email, () => this.isCreating());
    disabled(user.employeeCode, () => this.isCreating());
    disabled(user.role, () => this.isCreating());
    disabled(user.gender, () => this.isCreating());
    disabled(user.birthDate, () => this.isCreating());
    required(user.firstName, { message: 'First Name is required.' });
    required(user.displayName, { message: 'Display Name is required.' });
    required(user.username, { message: 'Username is required.' });
    pattern(user.username, USERNAME_REGEX, {
      message: 'Username can only contain letters and numbers.',
    });
    required(user.email, { message: 'Email is required.' });
    pattern(user.email, EMAIL_LOCAL_PART_REGEX, {
      message: 'Enter a valid email address.',
    });
    required(user.employeeCode, { message: 'Employee Code is required.' });
    required(user.role, { message: 'Role is required.' });
  });

  protected readonly displayRoleName: TpSelectOptionDisplayFn = (option) => {
    switch (option) {
      case RoleCode.Admin:
        return RoleName.Admin;
      case RoleCode.Employee:
        return RoleName.Employee;
      default:
        return '';
    }
  };

  constructor() {
    effect(() => {
      if (this.open()) {
        untracked(() => {
          this.resetForm();
          this.createdError.set(null);
        });
      }
    });

    effect(() => {
      const error = this.createdError();
      if (isNotEmpty(error)) {
        this.toastService.enqueue({
          type: 'error',
          content: error!,
        });
      }
    });

    effect(() => {
      const username = this.userForm.username().value();
      if (!this.isEmailSyncedWithUsername()) return;

      untracked(() => {
        this.formModel.update((values) => ({ ...values, email: username }));
      });
    });
  }

  protected onEmailValueChange(value: unknown): void {
    if (value !== this.userForm.username().value()) {
      this.isEmailSyncedWithUsername.set(false);
    }
  }

  protected createUser(): void {
    if (this.isCreating()) return;

    this.trimFormValues();
    if (!this.userForm().valid()) {
      this.markFormTouched();
      return;
    }

    this.createdError.set(null);
    const values = this.formModel();
    const roleCode = this.getRoleCode(values.role);
    const request: CreateNewUserRequest = {
      firstName: values.firstName,
      lastName: values.lastName || null,
      displayName: values.displayName,
      username: values.username,
      email: buildFullEmail(values.email),
      employeeCode: values.employeeCode,
      roleCode,
      gender: values.gender,
      birthDate: values.birthDate ? this.formatDateOnly(values.birthDate) : null,
    };

    this.isCreating.set(true);
    this.clientCorrelationService.startAction();

    const createRequest =
      roleCode === RoleCode.Admin
        ? this.userService.createNewAdminUser(request)
        : this.userService.createNewUser(request);

    let creationSucceeded = false;

    createRequest
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        tap(() => {
          creationSucceeded = true;
          this.toastService.enqueue({
            type: 'success',
            content: `${roleCode === RoleCode.Admin ? 'Admin' : 'User'} created successfully.`,
          });
          this.open.set(false);
        }),
        catchError((error: unknown) => {
          this.createdError.set(
            resolveErrorMessage(error, (error) => {
              console.log(error.error);
              const response = error.error as ErrorResponse;
              console.log(response);
              if (response.message.includes(USERNAME_EMAIL_ALREADY_EXISTS_ERROR_MESSAGE)) {
                return 'Username or email already exists';
              }
              return DEFAULT_ERROR_MESSAGE;
            }),
          );
          return EMPTY;
        }),
        finalize(() => {
          this.clientCorrelationService.completeAction();
          this.isCreating.set(false);
          if (creationSucceeded) this.created.emit();
        }),
      )
      .subscribe();
  }

  protected cancel(): void {
    this.open.set(false);
  }

  private trimFormValues(): void {
    this.formModel.update((values) => ({
      ...values,
      firstName: values.firstName.trim(),
      lastName: values.lastName?.trim() || null,
      displayName: values.displayName.trim(),
      username: values.username.trim(),
      email: values.email.trim(),
      employeeCode: values.employeeCode.trim(),
    }));
  }

  private markFormTouched(): void {
    this.userForm.firstName().markAsTouched();
    this.userForm.lastName().markAsTouched();
    this.userForm.displayName().markAsTouched();
    this.userForm.username().markAsTouched();
    this.userForm.email().markAsTouched();
    this.userForm.employeeCode().markAsTouched();
    this.userForm.role().markAsTouched();
    this.userForm.gender().markAsTouched();
    this.userForm.birthDate().markAsTouched();
  }

  protected onGenderChecked(gender: UserGender, checked: boolean): void {
    if (checked) {
      this.formModel.update((values) => ({ ...values, gender }));
      this.userForm.gender().markAsTouched();
    }
  }

  private getRoleCode(role: TpSelectOption | null): RoleCode.Admin | RoleCode.Employee {
    return role === RoleCode.Admin && this.isSuperAdmin() ? RoleCode.Admin : RoleCode.Employee;
  }

  private formatDateOnly(date: Date): string {
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }

  private resetForm(): void {
    this.isEmailSyncedWithUsername.set(true);
    this.userForm().reset(this.createInitialFormValue());
  }

  private createInitialFormValue(): NewUserFormValue {
    return {
      firstName: '',
      lastName: '',
      displayName: '',
      username: '',
      email: '',
      employeeCode: '',
      role: RoleCode.Employee,
      gender: UserGender.Male,
      birthDate: null,
    };
  }
}
