import { CdkOverlayOrigin } from '@angular/cdk/overlay';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  input,
  OnInit,
  output,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TpAvatar, TpBellNotification, TpTextButton } from '@team-platform/ui';
import { finalize } from 'rxjs';
import { BellNotificationItem, NotificationItem } from '../../../core/models/notifications';
import { ClientCorrelationService } from '../../../core/services/client-correlation-service';
import { NotificationService } from '../../../core/services/notification-service';
import { BellNotificationMenu } from './bell-notification-menu/bell-notification-menu';
import { UserAccountMenu } from './user-account-menu/user-account-menu';

@Component({
  selector: 'app-nav-bar',
  imports: [
    CdkOverlayOrigin,
    TpAvatar,
    TpBellNotification,
    TpTextButton,
    BellNotificationMenu,
    UserAccountMenu,
  ],
  templateUrl: './nav-bar.html',
  styleUrl: './nav-bar.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NavBar implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly clientCorrelationService = inject(ClientCorrelationService);
  private readonly notificationService = inject(NotificationService);

  readonly sideBarCollapsed = input(false);
  readonly sideBarToggle = output<void>();

  readonly isBellNotificationMenuOpen = signal(false);
  readonly isUserAccountMenuOpen = signal(false);
  readonly notifications = signal<NotificationItem[]>([]);
  readonly unreadCount = computed(
    () => this.notifications().filter((notification) => notification.isRead !== true).length,
  );
  readonly isMarkingAllNotificationsRead = signal(false);

  private readonly pendingMarkReadIds = new Set<string>();
  private readonly inFlightMarkReadIds = new Set<string>();
  private pendingMarkReadTimer: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.notificationService.bellNotifications$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((notification) => this.addNotification(notification));

    const hubActionId = this.clientCorrelationService.startAction();

    void this.notificationService
      .startHubConnection()
      .catch((error: unknown) => {
        console.error('Failed to connect to the notification hub.', error);
      })
      .finally(() => {
        if (this.clientCorrelationService.getClientActionId() === hubActionId) {
          this.clientCorrelationService.completeAction();
        }

        if (this.destroyRef.destroyed) {
          return;
        }

        this.loadNotifications();
      });

    this.destroyRef.onDestroy(() => {
      this.clearPendingMarkReadTimer();
      this.pendingMarkReadIds.clear();
      void this.notificationService.stopHubConnection().catch((error: unknown) => {
        console.error('Failed to stop the notification hub connection.', error);
      });
    });
  }

  scheduleMarkNotificationRead(notificationId: string): void {
    if (
      this.isMarkingAllNotificationsRead() ||
      this.pendingMarkReadIds.has(notificationId) ||
      this.inFlightMarkReadIds.has(notificationId)
    ) {
      return;
    }

    const notification = this.notifications().find(
      (item) => item.notificationId === notificationId,
    );
    if (!notification || notification.isRead === true) return;

    this.pendingMarkReadIds.add(notificationId);
    this.clearPendingMarkReadTimer();
    this.pendingMarkReadTimer = setTimeout(() => {
      this.pendingMarkReadTimer = null;
      this.markPendingNotificationsRead();
    }, 500);
  }

  markAllNotificationsRead(): void {
    if (
      this.isMarkingAllNotificationsRead() ||
      !this.notifications().some((notification) => notification.isRead !== true)
    ) {
      return;
    }

    this.clearPendingMarkReadTimer();
    this.pendingMarkReadIds.clear();
    this.isMarkingAllNotificationsRead.set(true);

    this.clientCorrelationService.startAction();
    this.notificationService
      .markNotifications({ isAll: true })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          this.clientCorrelationService.completeAction();
          this.isMarkingAllNotificationsRead.set(false);
        }),
      )
      .subscribe({
        next: () => {
          const readAt = new Date().toISOString();
          this.notifications.update((notifications) =>
            notifications.map((notification) =>
              notification.isRead === true
                ? notification
                : { ...notification, isRead: true, readAt },
            ),
          );
        },
        error: (error: unknown) => console.error('Failed to mark all notifications read.', error),
      });
  }

  toggleBellNotificationMenu(): void {
    this.isBellNotificationMenuOpen.update((isOpen) => !isOpen);
  }

  toggleUserAccountMenu(): void {
    this.isUserAccountMenuOpen.update((isOpen) => !isOpen);
  }

  private addNotification(notification: BellNotificationItem): void {
    this.notifications.update((notifications) => [
      notification,
      ...notifications.filter((item) => item.notificationId !== notification.notificationId),
    ]);
  }

  private loadNotifications(): void {
    const requestActionId = this.clientCorrelationService.startAction();

    this.notificationService
      .getLatelyNotificationForUser()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          if (this.clientCorrelationService.getClientActionId() === requestActionId) {
            this.clientCorrelationService.completeAction();
          }
        }),
      )
      .subscribe({
        next: (notifications) => this.mergeNotifications(notifications),
        error: (error: unknown) => console.error('Failed to load notifications.', error),
      });
  }

  private markPendingNotificationsRead(): void {
    if (this.destroyRef.destroyed || this.isMarkingAllNotificationsRead()) return;

    const notificationIds = [...this.pendingMarkReadIds];
    if (notificationIds.length === 0) return;

    this.pendingMarkReadIds.clear();
    notificationIds.forEach((notificationId) => this.inFlightMarkReadIds.add(notificationId));
    const notificationIdSet = new Set(notificationIds);

    const actionId = this.clientCorrelationService.startAction();
    this.notificationService
      .markNotifications({ notificationIds, isAll: false })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          notificationIds.forEach((notificationId) =>
            this.inFlightMarkReadIds.delete(notificationId),
          );
          if (this.clientCorrelationService.getClientActionId() === actionId) {
            this.clientCorrelationService.completeAction();
          }
        }),
      )
      .subscribe({
        next: () => {
          const readAt = new Date().toISOString();
          this.notifications.update((notifications) =>
            notifications.map((notification) =>
              notificationIdSet.has(notification.notificationId)
                ? { ...notification, isRead: true, readAt: notification.readAt ?? readAt }
                : notification,
            ),
          );
        },
        error: (error: unknown) => console.error('Failed to mark notification read.', error),
      });
  }

  private clearPendingMarkReadTimer(): void {
    if (this.pendingMarkReadTimer !== null) {
      clearTimeout(this.pendingMarkReadTimer);
      this.pendingMarkReadTimer = null;
    }
  }

  private mergeNotifications(incoming: NotificationItem[]): void {
    this.notifications.update((current) => {
      const notificationsById = new Map<string, NotificationItem>();
      for (const notification of [...incoming, ...current]) {
        notificationsById.set(notification.notificationId, notification);
      }

      return [...notificationsById.values()].sort(
        (left, right) => Date.parse(right.createdAt) - Date.parse(left.createdAt),
      );
    });
  }
}
