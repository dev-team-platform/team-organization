import { Component, computed, input, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TpTextButton } from '@team-platform/ui';
import { HasAccess } from '../../../core/directives/has-access';
import { CurrentUser } from '../../../core/models/auth/current-user';
import { Role } from '../../../enums/role';

interface SideBarNavigationItem {
  label: string;
  hasAccess: boolean;
  icon: string;
  route: string | null;
  type: 'navigate' | 'dropdown';
  children: SideBarNavigationItem[];
}

@Component({
  selector: 'app-side-bar',
  imports: [RouterLink, RouterLinkActive, TpTextButton, HasAccess],
  host: {
    '[class.tt-side-bar-host--collapsed]': 'collapsed()',
  },
  templateUrl: './side-bar.html',
  styleUrl: './side-bar.scss',
})
export class SideBar {
  readonly collapsed = input(false);
  readonly currentUser = input<CurrentUser | null>(null);

  protected readonly expandedDropdownLabels = signal<ReadonlySet<string>>(new Set());

  protected readonly navigationItems = computed<SideBarNavigationItem[]>(() => [
    {
      label: 'Home',
      hasAccess: true,
      icon: 'home',
      route: '/home',
      type: 'navigate',
      children: [],
    },
    {
      label: 'Admin Settings',
      icon: 'admin_panel_settings',
      hasAccess: this.hasAdminAccess(),
      route: null,
      type: 'dropdown',
      children: [
        {
          label: 'Users Management',
          icon: 'manage_accounts',
          hasAccess: this.hasAdminAccess(),
          route: '/admin-settings/users-management',
          type: 'navigate',
          children: [],
        },
      ],
    },
    {
      label: 'Settings',
      hasAccess: true,
      icon: 'settings',
      route: '/settings',
      type: 'navigate',
      children: [],
    },
  ]);

  private hasAdminAccess(): boolean {
    const roleName = this.currentUser()?.roleName;
    return roleName === Role.SuperAdmin || roleName === Role.Admin;
  }

  protected isDropdownExpanded(item: SideBarNavigationItem): boolean {
    return this.expandedDropdownLabels().has(item.label);
  }

  protected toggleDropdown(item: SideBarNavigationItem): void {
    this.expandedDropdownLabels.update((expandedLabels) => {
      const nextExpandedLabels = new Set(expandedLabels);

      if (nextExpandedLabels.has(item.label)) {
        nextExpandedLabels.delete(item.label);
      } else {
        nextExpandedLabels.add(item.label);
      }

      return nextExpandedLabels;
    });
  }
}
