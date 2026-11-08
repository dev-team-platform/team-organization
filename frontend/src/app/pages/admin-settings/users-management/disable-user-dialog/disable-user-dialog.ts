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
import { TpButton, TpConfirmationDialog, TpInput } from '@team-platform/ui';
import { EMPTY, catchError, finalize, tap } from 'rxjs';
import { TitleDialog } from '../../../../components/title-dialog/title-dialog';
import type { UserListItem } from '../../../../core/models/users';
import { ClientCorrelationService } from '../../../../core/services/client-correlation-service';
import { ToastService } from '../../../../core/services/toast-service';
import { UserService } from '../../../../core/services/user-service';
import { resolveErrorMessage } from '../../../../core/utils/common';

interface DisableUserFormValue {
  otp: string;
}

type DisableUserDialogStage = 'closed' | 'warning' | 'requesting' | 'otp';

@Component({
  selector: 'app-disable-user-dialog',
  imports: [TitleDialog, TpButton, TpConfirmationDialog, TpInput, FormField],
  templateUrl: './disable-user-dialog.html',
  styleUrl: './disable-user-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DisableUserDialog {
  private readonly userService = inject(UserService);
  private readonly clientCorrelationService = inject(ClientCorrelationService);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly open = model(false);
  readonly user = input<UserListItem | null>(null);
  readonly otpRequested = output<void>();
  readonly completed = output<void>();

  protected readonly isBusy = signal(false);
  protected readonly stage = signal<DisableUserDialogStage>('closed');

  private readonly formModel = signal<DisableUserFormValue>({ otp: '' });
  protected readonly otpForm = form(this.formModel, (otp) => {
    disabled(otp.otp, () => this.isBusy());
    required(otp.otp, { message: 'OTP is required.' });
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
        untracked(() => this.startSession(user));
      }
    });
  }

  protected requestOtp(): void {
    const user = this.user();
    if (!user || this.isBusy()) return;

    this.stage.set('requesting');
    this.open.set(true);
    this.isBusy.set(true);
    this.clientCorrelationService.startAction();

    this.userService
      .requestDisableUserOtp(user.id)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        tap(() => {
          this.otpRequested.emit();
          if (!this.open()) return;
          this.formModel.set({ otp: '' });
          this.stage.set('otp');
        }),
        catchError((error: unknown) => {
          this.toastService.enqueue({ type: 'error', content: resolveErrorMessage(error) });
          if (this.open()) this.stage.set('warning');
          return EMPTY;
        }),
        finalize(() => {
          this.clientCorrelationService.completeAction();
          this.isBusy.set(false);
        }),
      )
      .subscribe();
  }

  protected confirmDisable(): void {
    const user = this.user();
    const otp = this.formModel().otp.trim();
    if (!user || this.isBusy()) return;

    this.formModel.update((value) => ({ ...value, otp }));
    if (!otp || !this.otpForm().valid()) {
      this.otpForm.otp().markAsTouched();
      return;
    }

    this.isBusy.set(true);
    this.clientCorrelationService.startAction();
    this.userService
      .confirmDisableUser(user.id, { otp })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        tap(() => {
          this.toastService.enqueue({ type: 'success', content: 'User disabled successfully.' });
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

  protected cancel(): void {
    if (this.isBusy()) return;
    this.closeSession();
  }

  private startSession(user: UserListItem): void {
    this.formModel.set({ otp: '' });
    this.isBusy.set(false);
    this.stage.set(user.isPendingDisabled ? 'otp' : 'warning');
  }

  private closeSession(): void {
    this.open.set(false);
    this.stage.set('closed');
    this.formModel.set({ otp: '' });
  }

  private resetSession(): void {
    this.stage.set('closed');
    this.formModel.set({ otp: '' });
    this.isBusy.set(false);
  }
}
