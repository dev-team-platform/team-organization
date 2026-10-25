import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormField, disabled, form, required } from '@angular/forms/signals';
import type { TpSelectOption } from '@team-platform/ui';
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
import { RoleName } from '../../../../core/enums/uam';
import { UserGender } from '../../../../core/enums/users';
import type { UpdateUserRequest, UserListItem } from '../../../../core/models/users';
import { ClientCorrelationService } from '../../../../core/services/client-correlation-service';
import { ToastService } from '../../../../core/services/toast-service';
import { UserService } from '../../../../core/services/user-service';
import { resolveErrorMessage } from '../../../../core/utils/common';

interface EditUserFormValue {
  firstName: string;
  lastName: string;
  displayName: string;
  username: string;
  email: string;
  employeeCode: string;
  roleName: TpSelectOption | null;
  gender: UserGender;
  birthDate: Date | null;
}

@Component({
  selector: 'app-edit-user-dialog',
  imports: [
    TitleDialog,
    TpButton,
    TpInput,
    TpInputDatePicker,
    TpInputSingleselect,
    TpRadio,
    FormField,
  ],
  templateUrl: './edit-user-dialog.html',
  styleUrl: './edit-user-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditUserDialog {
  private readonly userService = inject(UserService);
  private readonly clientCorrelationService = inject(ClientCorrelationService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly open = model(false);
  readonly user = input<UserListItem | null>(null);
  readonly updated = output<UserListItem>();

  protected readonly emailDomain = environment.emailDomain;
  protected readonly genders = UserGender;
  protected readonly roleOptions = [RoleName.SuperAdmin, RoleName.Admin, RoleName.Employee];
  protected readonly isSaving = signal(false);

  private readonly formModel = signal<EditUserFormValue>(this.createEmptyFormValue());
  protected readonly selectedGender = () => this.formModel().gender;
  protected readonly userForm = form(this.formModel, (user) => {
    disabled(user.firstName, () => this.isSaving());
    disabled(user.lastName, () => this.isSaving());
    disabled(user.displayName, () => this.isSaving());
    disabled(user.username);
    disabled(user.email);
    disabled(user.employeeCode, () => this.isSaving());
    disabled(user.roleName);
    disabled(user.gender, () => this.isSaving());
    disabled(user.birthDate, () => this.isSaving());
    required(user.firstName, { message: 'First Name is required.' });
    required(user.displayName, { message: 'Display Name is required.' });
    required(user.employeeCode, { message: 'Employee Code is required.' });
  });

  constructor() {
    effect(() => {
      const isOpen = this.open();
      const user = this.user();
      if (!isOpen || !user) return;

      untracked(() => this.userForm().reset(this.toFormValue(user)));
    });
  }

  protected onGenderChecked(gender: UserGender, checked: boolean): void {
    if (!checked) return;

    this.formModel.update((values) => ({ ...values, gender }));
    this.userForm.gender().markAsTouched();
  }

  protected saveChanges(): void {
    if (this.isSaving()) return;

    const user = this.user();
    this.trimFormValues();

    if (!user || !this.userForm().valid()) {
      this.markFormTouched();
      return;
    }

    const values = this.formModel();
    const request: UpdateUserRequest = {
      firstName: values.firstName,
      lastName: values.lastName || null,
      displayName: values.displayName,
      employeeCode: values.employeeCode,
      gender: values.gender,
      birthDate: values.birthDate ? this.formatDateOnly(values.birthDate) : null,
    };

    this.isSaving.set(true);
    this.clientCorrelationService.startAction();
    this.userService
      .updateUser(user.id, request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        tap((response) => {
          this.updated.emit({
            ...user,
            id: response.id,
            username: response.username,
            email: response.email,
            firstName: response.firstName,
            lastName: response.lastName,
            displayName: response.displayName,
            employeeCode: response.employeeCode,
            gender: response.gender,
            birthDate: response.birthDate,
          });
          this.toastService.enqueue({
            type: 'success',
            content: 'User updated successfully.',
          });
          this.open.set(false);
        }),
        catchError((error: unknown) => {
          this.toastService.enqueue({
            type: 'error',
            content: resolveErrorMessage(error),
          });
          return EMPTY;
        }),
        finalize(() => {
          this.clientCorrelationService.completeAction();
          this.isSaving.set(false);
        }),
      )
      .subscribe();
  }

  protected cancel(): void {
    if (this.isSaving()) return;
    this.open.set(false);
  }

  private markFormTouched(): void {
    this.userForm.firstName().markAsTouched();
    this.userForm.lastName().markAsTouched();
    this.userForm.displayName().markAsTouched();
    this.userForm.employeeCode().markAsTouched();
  }

  private toFormValue(user: UserListItem): EditUserFormValue {
    return {
      firstName: user.firstName,
      lastName: user.lastName ?? '',
      displayName: user.displayName,
      username: user.username,
      email: user.email.split('@')[0],
      employeeCode: user.employeeCode,
      roleName: user.roleName,
      gender: user.gender,
      birthDate: user.birthDate ? this.parseDateOnly(user.birthDate) : null,
    };
  }

  private createEmptyFormValue(): EditUserFormValue {
    return {
      firstName: '',
      lastName: '',
      displayName: '',
      username: '',
      email: '',
      employeeCode: '',
      roleName: null,
      gender: UserGender.Unknown,
      birthDate: null,
    };
  }

  private parseDateOnly(value: string): Date {
    const [year, month, day] = value.split('-').map(Number);
    return new Date(year, month - 1, day);
  }

  private formatDateOnly(date: Date): string {
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${date.getFullYear()}-${month}-${day}`;
  }

  private trimFormValues(): void {
    this.formModel.update((values) => ({
      ...values,
      firstName: values.firstName.trim(),
      lastName: values.lastName.trim(),
      displayName: values.displayName.trim(),
      employeeCode: values.employeeCode.trim(),
    }));
  }
}
