import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormField, disabled, form, required } from '@angular/forms/signals';
import { StepperSelectionEvent } from '@angular/cdk/stepper';
import { MatStepperModule } from '@angular/material/stepper';
import { TpButton, TpConfirmationDialog, TpInput, TpInputSingleselect } from '@team-platform/ui';
import { EMPTY, catchError, finalize, tap } from 'rxjs';
import { TitleDialog } from '../../../../components/title-dialog/title-dialog';
import { RoleCode, RoleName } from '../../../../core/enums/uam';
import type { ChangeRoleRequest, UserListItem } from '../../../../core/models/users';
import { ClientCorrelationService } from '../../../../core/services/client-correlation-service';
import { ToastService } from '../../../../core/services/toast-service';
import { UserService } from '../../../../core/services/user-service';
import { resolveErrorMessage } from '../../../../core/utils/common';

interface ChooseRoleFormValue {
  roleName: RoleName | null;
}

interface ChangeRoleOtpFormValue {
  otp: string;
}

type ChangeRoleDialogStage = 'closed' | 'choose-role' | 'requesting' | 'confirmation';

@Component({
  selector: 'app-change-role-dialog',
  imports: [
    TitleDialog,
    TpButton,
    TpConfirmationDialog,
    TpInput,
    TpInputSingleselect,
    FormField,
    MatStepperModule,
    ReactiveFormsModule,
  ],
  templateUrl: './change-role-dialog.html',
  styleUrl: './change-role-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangeRoleDialog {
  private readonly userService = inject(UserService);
  private readonly clientCorrelationService = inject(ClientCorrelationService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly open = model(false);
  readonly user = input<UserListItem | null>(null);
  readonly otpRequested = output<void>();
  readonly completed = output<void>();

  protected readonly roleOptions = [RoleName.Employee, RoleName.Admin];
  protected readonly isBusy = signal(false);
  protected readonly stage = signal<ChangeRoleDialogStage>('closed');
  protected readonly stepIndex = signal(0);
  protected readonly warningOpen = signal(false);

  private readonly roleModel = signal<ChooseRoleFormValue>({ roleName: null });
  private readonly otpModel = signal<ChangeRoleOtpFormValue>({ otp: '' });
  protected readonly roleForm = form(this.roleModel, (role) => {
    disabled(role.roleName, () => this.isBusy());
    required(role.roleName, { message: 'Role is required.' });
  });
  protected readonly otpForm = form(this.otpModel, (otp) => {
    disabled(otp.otp, () => this.isBusy());
    required(otp.otp, { message: 'OTP is required.' });
  });

  protected readonly selectedRoleIsCurrent = computed(() => {
    const selectedRole = this.roleModel().roleName;
    const currentRole = this.user()?.roleName;
    return selectedRole !== null && selectedRole === currentRole;
  });

  protected readonly dialogTitle = computed(() =>
    this.stepIndex() === 0 ? 'Choose Role' : 'Change Role Confirmation',
  );

  protected readonly requestStepGuard = new FormGroup({
    requestSent: new FormControl(false, {
      nonNullable: true,
      validators: Validators.requiredTrue,
    }),
  });

  constructor() {
    effect(() => {
      const isOpen = this.open();
      const stage = this.stage();
      const user = this.user();
      const isBusy = this.isBusy();

      if (!isOpen) {
        if (stage !== 'closed' && !isBusy) {
          untracked(() => this.resetSession());
        }
        return;
      }

      if (stage === 'closed' && user) {
        untracked(() => this.startSession());
      }
    });
  }

  protected showChangeRoleWarning(): void {
    this.roleForm.roleName().markAsTouched();
    if (!this.roleForm().valid() || this.selectedRoleIsCurrent() || this.isBusy()) return;

    this.warningOpen.set(true);
  }

  protected requestOtp(): void {
    const user = this.user();
    const roleName = this.roleModel().roleName;
    if (!user || !roleName || this.isBusy()) return;

    const request: ChangeRoleRequest = {
      roleCode: roleName === RoleName.Admin ? RoleCode.Admin : RoleCode.Employee,
    };

    this.warningOpen.set(false);
    this.stage.set('requesting');
    this.isBusy.set(true);
    this.clientCorrelationService.startAction();

    this.userService
      .requestChangeRoleOtp(user.id, request)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        tap(() => {
          this.otpRequested.emit();
          if (!this.open()) return;
          this.otpModel.set({ otp: '' });
          this.requestStepGuard.controls.requestSent.setValue(true);
          this.requestStepGuard.updateValueAndValidity();
          this.stepIndex.set(1);
          this.stage.set('confirmation');
        }),
        catchError((error: unknown) => {
          this.toastService.enqueue({ type: 'error', content: resolveErrorMessage(error) });
          if (this.open()) this.stage.set('choose-role');
          return EMPTY;
        }),
        finalize(() => {
          this.clientCorrelationService.completeAction();
          this.isBusy.set(false);
        }),
      )
      .subscribe();
  }

  protected confirmChangeRole(): void {
    const user = this.user();
    const otp = this.otpModel().otp.trim();
    if (!user || this.isBusy()) return;

    this.otpModel.update((value) => ({ ...value, otp }));
    if (!otp || !this.otpForm().valid()) {
      this.otpForm.otp().markAsTouched();
      return;
    }

    this.isBusy.set(true);
    this.clientCorrelationService.startAction();
    this.userService
      .confirmChangeRole(user.id, { otp })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        tap(() => {
          this.toastService.enqueue({
            type: 'success',
            content: 'User role changed successfully.',
          });
          this.completed.emit();
          this.closeSession();
        }),
        catchError((error: unknown) => {
          this.toastService.enqueue({ type: 'error', content: resolveErrorMessage(error) });
          return EMPTY;
        }),
        finalize(() => {
          this.clientCorrelationService.completeAction();
          this.isBusy.set(false);
        }),
      )
      .subscribe();
  }

  protected onStepSelectionChange(event: StepperSelectionEvent): void {
    if (event.selectedIndex === 0 || this.requestStepGuard.valid) {
      this.stepIndex.set(event.selectedIndex);
    }
  }

  protected cancel(): void {
    if (this.isBusy()) return;
    this.warningOpen.set(false);
    this.closeSession();
  }

  private startSession(): void {
    this.roleModel.set({ roleName: null });
    this.otpModel.set({ otp: '' });
    this.requestStepGuard.controls.requestSent.setValue(false);
    this.requestStepGuard.updateValueAndValidity();
    this.stepIndex.set(0);
    this.warningOpen.set(false);
    this.isBusy.set(false);
    this.stage.set('choose-role');
  }

  private closeSession(): void {
    this.open.set(false);
    this.stage.set('closed');
    this.warningOpen.set(false);
    this.roleModel.set({ roleName: null });
    this.otpModel.set({ otp: '' });
    this.stepIndex.set(0);
    this.requestStepGuard.controls.requestSent.setValue(false);
    this.requestStepGuard.updateValueAndValidity();
  }

  private resetSession(): void {
    this.stage.set('closed');
    this.warningOpen.set(false);
    this.roleModel.set({ roleName: null });
    this.otpModel.set({ otp: '' });
    this.stepIndex.set(0);
    this.requestStepGuard.controls.requestSent.setValue(false);
    this.requestStepGuard.updateValueAndValidity();
    this.isBusy.set(false);
  }
}
