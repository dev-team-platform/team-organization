import { CdkOverlayOrigin } from '@angular/cdk/overlay';
import { DatePipe, NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  TemplateRef,
  computed,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  TpDivider,
  TpPopoverMenu,
  TpSkeleton,
  TpSlideToggle,
  TpTextButton,
} from '@team-platform/ui';
import { BELL_NOTIFICATION_TITLE } from '../../../../core/constants/notifications';
import { NotificationItem } from '../../../../core/models/notifications';

interface NotificationContentContext {
  $implicit: NotificationItem;
}

type NotificationTitle = (typeof BELL_NOTIFICATION_TITLE)[keyof typeof BELL_NOTIFICATION_TITLE];

function asRecord(value: unknown): Record<string, unknown> | null {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null;
}

function getTemplateValue(data: Record<string, unknown>, path: string): unknown {
  return path.split('.').reduce<unknown>((value, key) => {
    const record = asRecord(value);
    return record?.[key];
  }, data);
}

function formatContentTemplate(content: string, data: unknown): string {
  const values = asRecord(data);
  if (!values) return content;

  let hasMissingValue = false;
  const formattedContent = content.replace(/\{([\w.]+)\}/g, (placeholder, key: string) => {
    const value = getTemplateValue(values, key);
    if (value === undefined || value === null) {
      hasMissingValue = true;
      return placeholder;
    }

    return String(value);
  });

  return hasMissingValue ? content : formattedContent;
}

@Component({
  selector: 'app-bell-notification-menu',
  imports: [
    DatePipe,
    NgTemplateOutlet,
    RouterLink,
    TpDivider,
    TpPopoverMenu,
    TpSkeleton,
    TpSlideToggle,
    TpTextButton,
  ],
  templateUrl: './bell-notification-menu.html',
  styleUrl: './bell-notification-menu.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BellNotificationMenu {
  readonly open = model(false);
  readonly isUnreadOnly = model(false);
  readonly origin = input<CdkOverlayOrigin | null>(null);
  readonly notifications = input<NotificationItem[]>([]);
  readonly loading = input(false);
  readonly markAllReadDisabled = input(false);
  readonly markAsRead = output<string>();
  readonly markAllRead = output<void>();
  protected readonly canMarkAllRead = computed(
    () =>
      !this.loading() &&
      !this.markAllReadDisabled() &&
      this.notifications().some((notification) => notification.isRead !== true),
  );
  protected readonly skeletonItems = [0, 1, 2, 3, 4];
  private readonly defaultContentTemplate =
    viewChild.required<TemplateRef<NotificationContentContext>>('defaultContentTemplate');

  // Register templates here only for notification types that need interactive content.
  // Plain placeholder replacement is handled by the default template.
  private readonly interactiveContentTemplates: Partial<
    Record<NotificationTitle, () => TemplateRef<NotificationContentContext>>
  > = {};

  protected closeMenu(): void {
    this.open.set(false);
  }

  protected markNotificationRead(notification: NotificationItem): void {
    if (notification.isRead === true) return;

    this.markAsRead.emit(notification.notificationId);
  }

  protected handleNotificationKeydown(event: KeyboardEvent, notification: NotificationItem): void {
    if (event.key !== 'Enter' && event.key !== ' ') return;

    event.preventDefault();
    this.markNotificationRead(notification);
  }

  formatContent(item: NotificationItem): TemplateRef<NotificationContentContext> {
    const templateResolver = Object.entries(this.interactiveContentTemplates).find(([title]) =>
      item.title.includes(title),
    )?.[1];

    return templateResolver?.() ?? this.defaultContentTemplate();
  }

  protected interpolateContentTemplate(item: NotificationItem): string {
    return formatContentTemplate(item.content, item.data);
  }
}
