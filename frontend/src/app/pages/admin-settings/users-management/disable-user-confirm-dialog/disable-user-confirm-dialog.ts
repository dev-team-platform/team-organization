import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-disable-user-confirm-dialog',
  imports: [],
  templateUrl: './disable-user-confirm-dialog.html',
  styleUrl: './disable-user-confirm-dialog.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DisableUserConfirmDialog {}
