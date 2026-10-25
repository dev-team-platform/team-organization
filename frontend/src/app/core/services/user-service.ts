import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import type {
  CreateNewUserRequest,
  CreateNewUserResponse,
  GetUsersRequest,
  GetUsersResponse,
  UpdateUserRequest,
  UpdateUserResponse,
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
}
