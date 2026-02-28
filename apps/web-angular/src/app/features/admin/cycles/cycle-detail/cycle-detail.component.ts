import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { CycleService } from '../../../../core/services/cycle.service';
import {
  CycleDetailDto,
  CycleStatus,
  CYCLE_STATUS_MAP,
} from '../../../../core/models/cycle.models';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { DividerModule } from 'primeng/divider';

@Component({
  selector: 'app-cycle-detail',
  standalone: true,
  imports: [
    DatePipe,
    CardModule,
    ButtonModule,
    TagModule,
    MessageModule,
    SkeletonModule,
    ConfirmDialogModule,
    DividerModule,
  ],
  providers: [ConfirmationService],
  templateUrl: './cycle-detail.component.html',
  styleUrl: './cycle-detail.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cycleService = inject(CycleService);
  private readonly confirmationService = inject(ConfirmationService);

  readonly cycle = signal<CycleDetailDto | null>(null);
  readonly isLoading = signal(true);
  readonly hasError = signal(false);
  readonly errorMessage = signal('');
  readonly transitioning = signal(false);

  readonly CycleStatus = CycleStatus;

  readonly timelineSteps = [
    { status: CycleStatus.Configuration, label: 'Configuración' },
    { status: CycleStatus.ApplicationsOpen, label: 'Postulaciones' },
    { status: CycleStatus.ApplicationsClosed, label: 'Entrevistas' },
    { status: CycleStatus.Active, label: 'Activo' },
    { status: CycleStatus.Closed, label: 'Cerrado' },
  ];

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadCycle(id);
    }
  }

  loadCycle(id: string): void {
    this.isLoading.set(true);
    this.hasError.set(false);

    this.cycleService.getCycleById(id).subscribe({
      next: (data) => {
        this.cycle.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        this.hasError.set(true);
        this.errorMessage.set(err?.message ?? 'Error al cargar el ciclo');
      },
    });
  }

  getStatusLabel(status: CycleStatus): string {
    return CYCLE_STATUS_MAP[status]?.label ?? 'Desconocido';
  }

  getStatusSeverity(status: CycleStatus): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    return CYCLE_STATUS_MAP[status]?.severity ?? 'info';
  }

  isStepCompleted(stepStatus: CycleStatus): boolean {
    const current = this.cycle()?.status;
    if (current == null) return false;
    return stepStatus < current;
  }

  isStepActive(stepStatus: CycleStatus): boolean {
    return this.cycle()?.status === stepStatus;
  }

  // ─── State Transitions ────────────────────────────────────────────────
  confirmOpenApplications(): void {
    this.confirmationService.confirm({
      message: 'Se abrirán las postulaciones para este ciclo. Los postulantes podrán enviar sus solicitudes.',
      header: 'Abrir Postulaciones',
      icon: 'pi pi-lock-open',
      accept: () => this.executeTransition('openApplications'),
    });
  }

  confirmCloseApplications(): void {
    this.confirmationService.confirm({
      message: 'Se cerrarán las postulaciones. No se aceptarán más solicitudes.',
      header: 'Cerrar Postulaciones',
      icon: 'pi pi-lock',
      accept: () => this.executeTransition('closeApplications'),
    });
  }

  confirmReopenApplications(): void {
    this.confirmationService.confirm({
      message: 'Se reabrirán las postulaciones. Los postulantes podrán enviar solicitudes nuevamente.',
      header: 'Reabrir Postulaciones',
      icon: 'pi pi-lock-open',
      accept: () => this.executeTransition('reopenApplications'),
    });
  }

  confirmCloseCycle(): void {
    this.confirmationService.confirm({
      message: 'Se cerrará oficialmente el ciclo. Esta acción es irreversible.',
      header: 'Cerrar Ciclo',
      icon: 'pi pi-exclamation-triangle',
      accept: () => this.executeTransition('closeCycle'),
    });
  }

  openExtendDates(): void {
    this.router.navigate(['/admin/cycles', this.cycle()!.id, 'extend-dates']);
  }

  goToConfigure(): void {
    this.router.navigate(['/admin/cycles', this.cycle()!.id, 'configure']);
  }

  goBack(): void {
    this.router.navigate(['/admin/cycles/active']);
  }

  private executeTransition(action: 'openApplications' | 'closeApplications' | 'reopenApplications' | 'closeCycle'): void {
    const id = this.cycle()!.id;
    this.transitioning.set(true);

    this.cycleService[action](id).subscribe({
      next: (updated) => {
        this.cycle.set({ ...this.cycle()!, ...updated });
        this.transitioning.set(false);
      },
      error: () => {
        this.transitioning.set(false);
      },
    });
  }
}
