import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { StepperModule } from 'primeng/stepper';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';
import { CycleService } from '../../../../core/services/cycle.service';
import { CycleDetailDto, ConfigureCycleRequest } from '../../../../core/models/cycle.models';
import { LocationsStepComponent } from './steps/locations-step.component';
import { SupervisorsStepComponent } from './steps/supervisors-step.component';
import { ScheduleStepComponent } from './steps/schedule-step.component';
import {
  LocationRow,
  LocationScheduleMap,
  SupervisorAssignmentMap,
} from './wizard.models';

@Component({
  selector: 'app-cycle-wizard',
  standalone: true,
  imports: [
    ButtonModule,
    StepperModule,
    MessageModule,
    SkeletonModule,
    LocationsStepComponent,
    SupervisorsStepComponent,
    ScheduleStepComponent,
  ],
  templateUrl: './cycle-wizard.component.html',
  styleUrl: './cycle-wizard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleWizardComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cycleService = inject(CycleService);

  // ─── Routing ────────────────────────────────────────────────────────────────
  private cycleId = '';

  // ─── Page State ─────────────────────────────────────────────────────────────
  readonly cycle = signal<CycleDetailDto | null>(null);
  readonly isLoadingCycle = signal(true);
  readonly loadError = signal<string | null>(null);
  readonly isSaving = signal(false);
  readonly saveError = signal<string | null>(null);

  // ─── Stepper State ──────────────────────────────────────────────────────────
  /** PrimeNG Stepper value: steps are 1-indexed */
  readonly activeStep = signal<number>(1);

  // ─── Step Data ──────────────────────────────────────────────────────────────
  readonly locationRows = signal<LocationRow[]>([]);
  readonly supervisorAssignments = signal<SupervisorAssignmentMap>({});
  readonly scheduleMap = signal<LocationScheduleMap>({});

  // ─── Computed helpers ───────────────────────────────────────────────────────
  readonly activeLocations = computed(() => this.locationRows().filter((r) => r.isActive));

  readonly canProceedFromLocations = computed(() => this.activeLocations().length > 0);

  readonly canProceedFromSupervisors = computed(() => {
    const locs = this.activeLocations();
    if (locs.length === 0) return false;
    const map = this.supervisorAssignments();
    return locs.every((l) => !!map[l.locationId]);
  });

  readonly canSave = computed(() => {
    const map = this.scheduleMap();
    return this.activeLocations().every((loc) => {
      const slots = map[loc.locationId];
      return slots && slots.length > 0 && slots.every((s) => s.startTime && s.endTime);
    });
  });

  ngOnInit(): void {
    this.cycleId = this.route.snapshot.paramMap.get('id') ?? '';
    if (this.cycleId) {
      this.loadCycle();
    }
  }

  onLocationsChange(rows: LocationRow[]): void {
    this.locationRows.set(rows);
  }

  onSupervisorsChange(map: SupervisorAssignmentMap): void {
    this.supervisorAssignments.set(map);
  }

  onSchedulesChange(map: LocationScheduleMap): void {
    this.scheduleMap.set(map);
  }

  goToStep(step: number): void {
    this.activeStep.set(step);
  }

  nextFromLocations(): void {
    if (!this.canProceedFromLocations()) return;
    this.activeStep.set(2);
  }

  nextFromSupervisors(): void {
    if (!this.canProceedFromSupervisors()) return;
    this.activeStep.set(3);
  }

  save(): void {
    if (!this.canSave()) return;
    this.saveError.set(null);
    this.isSaving.set(true);

    const request = this.buildRequest();
    this.cycleService.configureCycle(this.cycleId, request).subscribe({
      next: () => {
        this.router.navigate(['/admin/cycles', this.cycleId]);
      },
      error: (err) => {
        this.isSaving.set(false);
        this.saveError.set(err?.message ?? 'Error al guardar la configuración. Por favor intenta de nuevo.');
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/cycles', this.cycleId]);
  }

  private loadCycle(): void {
    this.isLoadingCycle.set(true);
    this.loadError.set(null);
    this.cycleService.getCycleById(this.cycleId).subscribe({
      next: (data) => {
        this.cycle.set(data);
        this.isLoadingCycle.set(false);
      },
      error: (err) => {
        this.isLoadingCycle.set(false);
        this.loadError.set(err?.message ?? 'Error al cargar el ciclo');
      },
    });
  }

  private buildRequest(): ConfigureCycleRequest {
    const locationInputs = this.activeLocations().map((loc) => {
      const slots = (this.scheduleMap()[loc.locationId] ?? []).map((s) => ({
        dayOfWeek: s.dayOfWeek,
        startTime: this.formatTime(s.startTime!),
        endTime: this.formatTime(s.endTime!),
        requiredScholars: s.requiredScholars,
      }));

      return {
        locationId: loc.locationId,
        scholarshipsAvailable: loc.scholarshipsAvailable,
        isActive: true,
        scheduleSlots: slots,
      };
    });

    const supervisorAssignments = this.activeLocations()
      .filter((loc) => !!this.supervisorAssignments()[loc.locationId])
      .map((loc) => ({
        supervisorId: this.supervisorAssignments()[loc.locationId],
        cycleLocationId: loc.locationId,
      }));

    return { locations: locationInputs, supervisorAssignments };
  }

  private formatTime(date: Date): string {
    const h = String(date.getHours()).padStart(2, '0');
    const m = String(date.getMinutes()).padStart(2, '0');
    return `${h}:${m}`;
  }
}
