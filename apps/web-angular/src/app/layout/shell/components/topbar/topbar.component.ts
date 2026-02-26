import {
  ChangeDetectionStrategy,
  Component,
  InputSignal,
  OutputEmitterRef,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs/operators';
import { signal } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { BadgeModule } from 'primeng/badge';
import { BreadcrumbModule } from 'primeng/breadcrumb';
import { NavigationService } from '../../services/navigation.service';
import { UserMenuComponent } from '../user-menu/user-menu.component';

export interface BreadcrumbItem {
  label: string;
  route?: string;
}

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [ButtonModule, BadgeModule, BreadcrumbModule, UserMenuComponent],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopbarComponent {
  readonly collapsed: InputSignal<boolean> = input<boolean>(false);
  readonly isMobile: InputSignal<boolean> = input<boolean>(false);
  readonly toggleSidebar: OutputEmitterRef<void> = output<void>();

  private readonly router = inject(Router);
  private readonly navigationService = inject(NavigationService);

  readonly breadcrumbs = signal<BreadcrumbItem[]>([]);

  constructor() {
    // Rebuild breadcrumb on every successful navigation
    this.router.events
      .pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        takeUntilDestroyed(),
      )
      .subscribe((e) => {
        this.breadcrumbs.set(this.buildBreadcrumbs(e.urlAfterRedirects));
      });

    // Build breadcrumbs for the initial load
    this.breadcrumbs.set(this.buildBreadcrumbs(this.router.url));
  }

  onToggle(): void {
    this.toggleSidebar.emit();
  }

  private buildBreadcrumbs(url: string): BreadcrumbItem[] {
    // Strip query params and fragments
    const path = url.split('?')[0].split('#')[0];

    const items: BreadcrumbItem[] = [];

    // Try exact route match first
    const label = this.navigationService.getLabelForRoute(path);
    const parentLabel = this.navigationService.getParentLabelForRoute(path);

    if (parentLabel) {
      items.push({ label: parentLabel });
    }

    if (label) {
      items.push({ label, route: path });
    } else {
      // Fallback: humanize the last segment
      const segments = path.split('/').filter(Boolean);
      if (segments.length > 0) {
        items.push({ label: this.humanizeSegment(segments[segments.length - 1]) });
      }
    }

    return items;
  }

  private humanizeSegment(segment: string): string {
    return segment
      .replace(/-/g, ' ')
      .replace(/\b\w/g, (c) => c.toUpperCase());
  }
}
