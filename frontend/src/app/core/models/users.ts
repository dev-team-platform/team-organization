import type { RoleCode, RoleName } from '../enums/uam';
import type { UserGender } from '../enums/users';
import { FilterRequest, FilterResult } from './common';

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
  gender: UserGender;
  birthDate: string | null;
  roleName: RoleName | null;
  lastLoginAt: string | null;
  isPendingDisabled: boolean;
  isPendingChangedRole: boolean;
}

export type GetUsersRequest = FilterRequest;
export type GetUsersResponse = FilterResult<UserListItem>;

export interface CreateNewUserRequest {
  username: string;
  email: string;
  firstName: string;
  lastName: string | null;
  displayName: string;
  employeeCode: string;
  roleCode: RoleCode;
  gender: UserGender;
  birthDate: string | null;
}

export interface CreateNewUserResponse {
  id: string;
  identitySubject: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string | null;
  displayName: string;
  employeeCode: string;
  gender: UserGender;
  birthDate: string | null;
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string | null;
  displayName: string;
  employeeCode: string;
  gender: UserGender;
  birthDate: string | null;
}

export interface UpdateUserResponse {
  id: string;
  identitySubject: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string | null;
  displayName: string;
  employeeCode: string;
  gender: UserGender;
  birthDate: string | null;
}

export interface DisableUserResponse {
  userId: string;
  otpExpiresInMinutes: number;
}

export interface DisableUserConfirmationRequest {
  otp: string;
}

export interface DisableUserConfirmationResponse {
  userId: string;
  status: UserStatus;
}

export interface ChangeRoleRequest {
  roleCode: RoleCode.Admin | RoleCode.Employee;
}

export interface ChangeRoleResponse {
  userId: string;
  roleCode: RoleCode.Admin | RoleCode.Employee;
  otpExpiresInMinutes: number;
}

export interface ChangeRoleConfirmationRequest {
  otp: string;
}

export interface ChangeRoleConfirmationResponse {
  userId: string;
  roleCode: RoleCode.Admin | RoleCode.Employee;
}
