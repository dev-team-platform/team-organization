import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HasAccess } from './core/directives/has-access';
import { NavBar } from './shared/layouts/nav-bar/nav-bar';
import { SideBar } from './shared/layouts/side-bar/side-bar';

@Component({
  selector: 'app-root',
  imports: [HasAccess, RouterOutlet, NavBar, SideBar],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly title = signal('Team Tickets');
  protected readonly latestSearch = signal('');
  protected readonly hasAccess = signal(false);
  protected readonly sideBarCollapsed = signal(false);

  protected handleSearch(query: string): void {
    this.latestSearch.set(query);
  }

  protected toggleSideBar(): void {
    this.sideBarCollapsed.update((collapsed) => !collapsed);
  }
}
