import { FilterRequest, FilterResult } from './common';

export interface NotificationItem {
  notificationId: string;
  title: string;
  content: string;
  data: unknown;
  createdAt: string;
  isRead?: boolean;
  readAt?: string | null;
}

export type GetAllNotificationsRequest = FilterRequest;
export type GetAllNotificationsResponse = FilterResult<NotificationItem>;
export type GetLatestNotificationForUserResponse = NotificationItem[];

export interface MarkNotificationsRequest {
  notificationIds?: string[];
  isAll: boolean;
}

export interface MarkNotificationsResponse {
  markedCount: number;
}

export interface BellNotificationItem extends NotificationItem {
  recipientId: string;
}
