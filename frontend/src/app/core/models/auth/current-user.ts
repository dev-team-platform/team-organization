export interface CurrentUser {
  id: string;
  employeeCode: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string | null;
  displayName: string;
  avatarUrl: string | null;
  lastLoginAt: string | null;
  lastLogoutAt: string | null;
  roleName: string;
  permissions: string[];
}
