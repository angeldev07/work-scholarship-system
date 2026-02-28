import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { CycleService } from '../../../core/services/cycle.service';
import {
  AdminDashboardStateDto,
  CycleDto,
  CycleStatus,
  CYCLE_STATUS_MAP,
  PENDING_ACTION_MAP,
  PendingActionItem,
} from '../../../core/models/cycle.models';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { DatePipe } from '@angular/common';

const DEPARTMENT = 'Biblioteca';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CardModule, ButtonModule, TagModule, MessageModule, SkeletonModule, DatePipe],
  template: `
    <!-- Loading skeleton -->
    @if (isLoading()) {
      <div class="dashboard">
        <div class="dashboard__header">
          <p-skeleton width="300px" height="2rem" />
          <p-skeleton width="200px" height="1rem" />
        </div>
        <div class="dashboard__grid">
          @for (_ of [1,2,3,4]; track $index) {
            <p-card>
              <p-skeleton width="100%" height="100px" />
            </p-card>
          }
        </div>
      </div>
    }

    <!-- Error state -->
    @else if (hasError()) {
      <div class="dashboard">
        <p-message severity="error" [text]="errorMessage()" />
        <p-button label="Reintentar" icon="pi pi-refresh" (onClick)="loadDashboard()" [outlined]="true" />
      </div>
    }

    <!-- Dashboard loaded -->
    @else if (state()) {
      <div class="dashboard">
        <div class="dashboard__header">
          <h1>Panel de Administración</h1>
          <p>Bienvenido, <strong>{{ authService.currentUser()?.fullName }}</strong></p>
        </div>

        <!-- Pending Actions -->
        @if (state()!.pendingActions.length > 0) {
          <div class="dashboard__alerts">
            @for (action of state()!.pendingActions; track action.codeString) {
              <p-message
                [severity]="getActionSeverity(action)"
                [text]="getActionLabel(action)"
                [icon]="getActionIcon(action)"
              />
            }
          </div>
        }

        <!-- No cycle: show create CTA -->
        @if (!state()!.activeCycle && !state()!.cycleInConfiguration) {
          <p-card styleClass="dashboard__empty-card">
            <div class="dashboard__empty">
              <i class="pi pi-calendar-plus dashboard__empty-icon"></i>
              <h2>No hay ciclo activo</h2>
              @if (state()!.lastClosedCycle; as lastCycle) {
                <p>Último ciclo: <strong>{{ lastCycle.name }}</strong> — cerrado el {{ lastCycle.closedAt | date:'d MMM yyyy' }}</p>
              } @else {
                <p>Crea tu primer ciclo semestral para comenzar.</p>
              }
              <p-button label="Crear Ciclo" icon="pi pi-plus" (onClick)="goToCreateCycle()" />
            </div>
          </p-card>
        }

        <!-- Cycle in configuration -->
        @else if (state()!.cycleInConfiguration; as configCycle) {
          <p-card>
            <div class="dashboard__config">
              <div class="dashboard__config-header">
                <h2>{{ configCycle.name }}</h2>
                <p-tag
                  [value]="getStatusLabel(configCycle.status)"
                  [severity]="getStatusSeverity(configCycle.status)"
                />
              </div>
              <p>Este ciclo necesita configuración antes de poder iniciar.</p>
              <div class="dashboard__config-stats">
                <span><i class="pi pi-map-marker"></i> {{ configCycle.locationsCount }} ubicaciones</span>
                <span><i class="pi pi-users"></i> {{ configCycle.supervisorsCount }} supervisores</span>
                <span><i class="pi pi-user"></i> {{ configCycle.totalScholarshipsAvailable }} becas disponibles</span>
              </div>
              <div class="dashboard__config-actions">
                <p-button label="Continuar Setup" icon="pi pi-cog" (onClick)="goToCycleDetail(configCycle.id)" />
                <p-button label="Ver Detalle" icon="pi pi-eye" [outlined]="true" (onClick)="goToCycleDetail(configCycle.id)" />
              </div>
            </div>
          </p-card>
        }

        <!-- Active cycle: metrics -->
        @else if (state()!.activeCycle; as activeCycle) {
          <div class="dashboard__active-header">
            <h2>{{ activeCycle.name }}</h2>
            <p-tag
              [value]="getStatusLabel(activeCycle.status)"
              [severity]="getStatusSeverity(activeCycle.status)"
            />
          </div>

          <div class="dashboard__grid">
            <p-card>
              <div class="dashboard__metric">
                <i class="pi pi-map-marker dashboard__metric-icon"></i>
                <div class="dashboard__metric-value">{{ activeCycle.locationsCount }}</div>
                <div class="dashboard__metric-label">Ubicaciones</div>
              </div>
            </p-card>

            <p-card>
              <div class="dashboard__metric">
                <i class="pi pi-users dashboard__metric-icon"></i>
                <div class="dashboard__metric-value">{{ activeCycle.supervisorsCount }}</div>
                <div class="dashboard__metric-label">Supervisores</div>
              </div>
            </p-card>

            <p-card>
              <div class="dashboard__metric">
                <i class="pi pi-user dashboard__metric-icon"></i>
                <div class="dashboard__metric-value">{{ activeCycle.totalScholarshipsAssigned }}/{{ activeCycle.totalScholarshipsAvailable }}</div>
                <div class="dashboard__metric-label">Becas Asignadas</div>
              </div>
            </p-card>

            <p-card>
              <div class="dashboard__metric">
                <i class="pi pi-calendar dashboard__metric-icon"></i>
                <div class="dashboard__metric-value">{{ activeCycle.endDate | date:'d MMM' }}</div>
                <div class="dashboard__metric-label">Fin del Ciclo</div>
              </div>
            </p-card>
          </div>

          <div class="dashboard__actions">
            <p-button label="Ver Detalle del Ciclo" icon="pi pi-eye" (onClick)="goToCycleDetail(activeCycle.id)" />
            <p-button label="Historial de Ciclos" icon="pi pi-history" [outlined]="true" (onClick)="goToHistory()" />
          </div>
        }
      </div>
    }
  `,
  styleUrl: './admin-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminDashboardComponent implements OnInit {
  readonly authService = inject(AuthService);
  private readonly cycleService = inject(CycleService);
  private readonly router = inject(Router);

  readonly state = signal<AdminDashboardStateDto | null>(null);
  readonly isLoading = signal(true);
  readonly hasError = signal(false);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.hasError.set(false);

    this.cycleService.getDashboardState(DEPARTMENT).subscribe({
      next: (data) => {
        this.state.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.hasError.set(true);
        this.errorMessage.set(err?.message ?? 'Error al cargar el dashboard');
      },
    });
  }

  getStatusLabel(status: CycleStatus): string {
    return CYCLE_STATUS_MAP[status]?.label ?? 'Desconocido';
  }

  getStatusSeverity(status: CycleStatus): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    return CYCLE_STATUS_MAP[status]?.severity ?? 'info';
  }

  getActionLabel(action: PendingActionItem): string {
    return PENDING_ACTION_MAP[action.codeString]?.label ?? action.codeString;
  }

  getActionSeverity(action: PendingActionItem): 'info' | 'warn' | 'error' {
    const s = PENDING_ACTION_MAP[action.codeString]?.severity ?? 'info';
    return s === 'danger' ? 'error' : s;
  }

  getActionIcon(action: PendingActionItem): string {
    return PENDING_ACTION_MAP[action.codeString]?.icon ?? 'pi pi-info-circle';
  }

  goToCreateCycle(): void {
    this.router.navigate(['/admin/cycles/new']);
  }

  goToCycleDetail(id: string): void {
    this.router.navigate(['/admin/cycles', id]);
  }

  goToHistory(): void {
    this.router.navigate(['/admin/cycles/history']);
  }
}
