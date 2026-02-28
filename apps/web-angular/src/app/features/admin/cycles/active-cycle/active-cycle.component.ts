import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CycleService } from '../../../../core/services/cycle.service';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';

const DEPARTMENT = 'Biblioteca';

@Component({
  selector: 'app-active-cycle',
  standalone: true,
  imports: [CardModule, ButtonModule, SkeletonModule],
  template: `
    @if (isLoading()) {
      <div class="active-cycle">
        <p-skeleton width="100%" height="200px" />
      </div>
    } @else if (!found()) {
      <div class="active-cycle">
        <p-card>
          <div class="active-cycle__empty">
            <i class="pi pi-calendar-plus active-cycle__empty-icon"></i>
            <h2>No hay ciclo activo</h2>
            <p>Crea un nuevo ciclo semestral para comenzar.</p>
            <p-button label="Crear Ciclo" icon="pi pi-plus" (onClick)="goToCreate()" />
          </div>
        </p-card>
      </div>
    }
  `,
  styles: [`
    .active-cycle {
      &__empty {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 1rem;
        padding: 3rem 1rem;
        text-align: center;

        h2 { font-size: 1.25rem; color: #334155; margin: 0; }
        p { color: #64748b; margin: 0; }
      }
      &__empty-icon { font-size: 3rem; color: #cbd5e1; }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActiveCycleComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly cycleService = inject(CycleService);

  readonly isLoading = signal(true);
  readonly found = signal(false);

  ngOnInit(): void {
    this.cycleService.getActiveCycle(DEPARTMENT).subscribe({
      next: (cycle) => {
        this.isLoading.set(false);
        if (cycle) {
          this.found.set(true);
          this.router.navigate(['/admin/cycles', cycle.id]);
        }
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  goToCreate(): void {
    this.router.navigate(['/admin/cycles/new']);
  }
}
