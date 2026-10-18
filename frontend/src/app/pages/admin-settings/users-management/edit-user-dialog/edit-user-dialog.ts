import {
  ChangeDetectionStrategy,
  Component,
  effect,
  input,
  model,
  output,
  signal,
  untracked,
} from '@angular/core';
import { FormField, form, pattern, required } from '@angular/forms/signals';
import type { TpSelectOption } from '@team-platform/ui';
import {
  TpButton,
  TpInput,
  TpInputDatePicker,
  TpInputSingleselect,
  TpRadio,
} from '@team-platform/ui';
import { environment } from '../../../../../environments/environment';
import { TitleDialog } from '../../../../components/title-dialog/title-dialog';
import { USERNAME_REGEX } from '../../../../core/constants/users';
import { RoleName } from '../../../../core/enums/uam';
import { UserGender } from '../../../../core/enums/users';
import type { UserListItem } from '../../../../core/models/users';
import { buildFullEmail } from '../../../../core/utils/user';

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
  readonly open = model(false);
  readonly user = input<UserListItem | null>(null);
  readonly updated = output<UserListItem>();

  protected readonly emailDomain = environment.emailDomain;
  protected readonly genders = UserGender;
  protected readonly roleOptions = [RoleName.SuperAdmin, RoleName.Admin, RoleName.Employee];

  private readonly formModel = signal<EditUserFormValue>(this.createEmptyFormValue());
  protected readonly selectedGender = () => this.formModel().gender;
  protected readonly userForm = form(this.formModel, (user) => {
    required(user.firstName, { message: 'First Name is required.' });
    required(user.displayName, { message: 'Display Name is required.' });
    required(user.username, { message: 'Username is required.' });
    pattern(user.username, USERNAME_REGEX, {
      message: 'Username can only contain letters and numbers.',
    });
    required(user.email, { message: 'Email is required.' });
    required(user.employeeCode, { message: 'Employee Code is required.' });
    required(user.roleName, { message: 'Role is required.' });
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
    const user = this.user();
    this.trimFormValues();

    if (!user || !this.userForm().valid()) {
      this.markFormTouched();
      return;
    }

    const values = this.formModel();
    this.updated.emit({
      ...user,
      firstName: values.firstName.trim(),
      lastName: values.lastName.trim() || null,
      displayName: values.displayName.trim(),
      username: values.username.trim(),
      email: buildFullEmail(values.email),
      employeeCode: values.employeeCode.trim(),
      roleName: typeof values.roleName === 'string' ? (values.roleName as RoleName) : user.roleName,
      gender: values.gender,
      birthDate: values.birthDate ? this.formatDateOnly(values.birthDate) : null,
    });
    this.open.set(false);
  }

  protected cancel(): void {
    this.open.set(false);
  }

  private markFormTouched(): void {
    this.userForm.firstName().markAsTouched();
    this.userForm.lastName().markAsTouched();
    this.userForm.displayName().markAsTouched();
    this.userForm.username().markAsTouched();
    this.userForm.email().markAsTouched();
    this.userForm.employeeCode().markAsTouched();
    this.userForm.roleName().markAsTouched();
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
      username: values.username.trim(),
      email: values.email.trim(),
      employeeCode: values.employeeCode.trim(),
    }));
  }
}
