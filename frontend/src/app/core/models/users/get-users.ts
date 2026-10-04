import { FilterRequest, FilterResult } from '../common/filter';

export type UserStatus = 'Active' | 'Inactive' | 'Pending';

export interface UserListItem {
  id: string;
  employeeCode: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string | null;
  displayName: string;
  avatarUrl: string | null;
  status: UserStatus;
  lastLoginAt: string | null;
}

export type GetUsersRequest = FilterRequest;
export type GetUsersResponse = FilterResult<UserListItem>;


