import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CurrentUser } from '../../core/models/auth/current-user';
import { AuthService } from '../../core/services/auth-service';
import { Breadcrumb } from './breadcrumb/breadcrumb';
import { NavBar } from './nav-bar/nav-bar';
import { SideBar } from './side-bar/side-bar';

@Component({
  selector: 'app-app-layout',
  imports: [NavBar, SideBar, Breadcrumb, RouterOutlet],
  templateUrl: './app-layout.html',
  styleUrl: './app-layout.scss',
})
export class AppLayout implements OnInit {
  private readonly authService = inject(AuthService);
  protected readonly latestSearch = signal('');
  protected readonly sideBarCollapsed = signal(false);
  readonly currentUser = signal<CurrentUser | null>(null);

  ngOnInit(): void {
    this.currentUser.set(this.authService.currentUser());
  }

  protected handleSearch(query: string): void {
    this.latestSearch.set(query);
  }

  protected toggleSideBar(): void {
    this.sideBarCollapsed.update((collapsed) => !collapsed);
  }

  protected expandSideBar(): void {
    this.sideBarCollapsed.set(false);
  }
}
