import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Breadcrumb } from './breadcrumb/breadcrumb';
import { NavBar } from './nav-bar/nav-bar';
import { SideBar } from './side-bar/side-bar';

@Component({
  selector: 'app-app-layout',
  imports: [NavBar, SideBar, Breadcrumb, RouterOutlet],
  templateUrl: './app-layout.html',
  styleUrl: './app-layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppLayout {
  protected readonly latestSearch = signal('');
  protected readonly sideBarCollapsed = signal(false);

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
