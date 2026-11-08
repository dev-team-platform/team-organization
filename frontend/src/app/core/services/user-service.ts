import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import type {
  ChangeRoleConfirmationRequest,
  ChangeRoleConfirmationResponse,
  ChangeRoleRequest,
  ChangeRoleResponse,
  CreateNewUserRequest,
  CreateNewUserResponse,
  DisableUserConfirmationRequest,
  DisableUserConfirmationResponse,
  DisableUserResponse,
  GetUsersRequest,
  GetUsersResponse,
  UpdateUserRequest,
  UpdateUserResponse
} from '../models/users';
import { AppHttpContext } from './http-context';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly httpClient = inject(HttpClient);
  private readonly appHttpContext = inject(AppHttpContext);

  getUsers(request: GetUsersRequest): Observable<GetUsersResponse> {
    return this.httpClient.post<GetUsersResponse>('users/all', request, {
      context: this.appHttpContext.baseContext,
    });
  }

  createNewUser(request: CreateNewUserRequest): Observable<CreateNewUserResponse> {
    return this.httpClient.post<CreateNewUserResponse>('users/new-user', request, {
      context: this.appHttpContext.baseContext,
    });
  }

  createNewAdminUser(request: CreateNewUserRequest): Observable<CreateNewUserResponse> {
    return this.httpClient.post<CreateNewUserResponse>('users/new-admin-user', request, {
      context: this.appHttpContext.baseContext,
    });
  }

  updateUser(userId: string, request: UpdateUserRequest): Observable<UpdateUserResponse> {
    return this.httpClient.put<UpdateUserResponse>(`users/${userId}`, request, {
      context: this.appHttpContext.baseContext,
    });
  }

  requestDisableUserOtp(userId: string): Observable<DisableUserResponse> {
    return this.httpClient.post<DisableUserResponse>(
      `users/disable/${userId}`,
      {},
      {
        context: this.appHttpContext.baseContext,
      },
    );
  }

  confirmDisableUser(
    userId: string,
    request: DisableUserConfirmationRequest,
  ): Observable<DisableUserConfirmationResponse> {
    return this.httpClient.patch<DisableUserConfirmationResponse>(
      `users/disable/${userId}`,
      request,
      { context: this.appHttpContext.baseContext },
    );
  }

  requestChangeRoleOtp(userId: string, request: ChangeRoleRequest): Observable<ChangeRoleResponse> {
    return this.httpClient.post<ChangeRoleResponse>(`users/change-role/${userId}`, request, {
      context: this.appHttpContext.baseContext,
    });
  }

  confirmChangeRole(
    userId: string,
    request: ChangeRoleConfirmationRequest,
  ): Observable<ChangeRoleConfirmationResponse> {
    return this.httpClient.patch<ChangeRoleConfirmationResponse>(
      `users/change-role/${userId}`,
      request,
      { context: this.appHttpContext.baseContext },
    );
  }
}
