import {
  ChangeDetectionStrategy,
  Component,
  HostListener,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { DrawerModule } from 'primeng/drawer';
import { SidebarComponent } from './components/sidebar/sidebar.component';
import { TopbarComponent } from './components/topbar/topbar.component';
import { NavigationService } from './services/navigation.service';

/** Breakpoints must match tokens.scss */
const BREAKPOINT_MOBILE = 768;
const BREAKPOINT_TABLET = 1024;

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, DrawerModule, SidebarComponent, TopbarComponent],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShellComponent implements OnInit {
  readonly navigationService = inject(NavigationService);

  /** Whether the desktop sidebar is in collapsed (icon-only) mode */
  readonly collapsed = signal(false);

  /** Whether the mobile drawer is open */
  readonly mobileOpen = signal(false);

  /** Whether we are in mobile breakpoint (<768px) */
  readonly isMobile = signal(false);

  ngOnInit(): void {
    this.evaluateBreakpoint(window.innerWidth);
  }

  @HostListener('window:resize')
  onResize(): void {
    this.evaluateBreakpoint(window.innerWidth);
  }

  private evaluateBreakpoint(width: number): void {
    const wasMobile = this.isMobile();
    const nowMobile = width < BREAKPOINT_MOBILE;
    this.isMobile.set(nowMobile);

    // Auto-collapse on tablet, expand on desktop — only when transitioning
    if (!nowMobile && wasMobile !== nowMobile) {
      this.mobileOpen.set(false);
    }

    if (!nowMobile) {
      // Default: collapsed on tablet, expanded on desktop
      if (width < BREAKPOINT_TABLET) {
        this.collapsed.set(true);
      } else {
        this.collapsed.set(false);
      }
    }
  }

  toggleSidebar(): void {
    if (this.isMobile()) {
      this.mobileOpen.update((v) => !v);
    } else {
      this.collapsed.update((v) => !v);
    }
  }

  closeMobileDrawer(): void {
    this.mobileOpen.set(false);
  }
}
