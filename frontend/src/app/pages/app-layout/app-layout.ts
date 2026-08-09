import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HasAccess } from '../../core/directives/has-access';
import { NavBar } from './nav-bar/nav-bar';
import { SideBar } from './side-bar/side-bar';

@Component({
  selector: 'app-app-layout',
  imports: [NavBar, SideBar, HasAccess, RouterOutlet],
  templateUrl: './app-layout.html',
  styleUrl: './app-layout.scss',
})
export class AppLayout {
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
