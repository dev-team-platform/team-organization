import { Permission } from '../../enums/permission';
import { Role } from '../../enums/role';

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
  roleName: Role;
  permissions: Permission[];
}
