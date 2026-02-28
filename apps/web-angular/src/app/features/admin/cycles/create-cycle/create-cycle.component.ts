import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CycleService } from '../../../../core/services/cycle.service';
import { AdminDashboardStateDto, CreateCycleRequest, CycleDto } from '../../../../core/models/cycle.models';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DatePickerModule } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageModule } from 'primeng/message';
import { DividerModule } from 'primeng/divider';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';

const DEPARTMENT = 'Biblioteca';

@Component({
  selector: 'app-create-cycle',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    CardModule,
    ButtonModule,
    InputTextModule,
    DatePickerModule,
    InputNumberModule,
    CheckboxModule,
    MessageModule,
    DividerModule,
    IconFieldModule,
    InputIconModule,
    TagModule,
    TooltipModule,
  ],
  templateUrl: './create-cycle.component.html',
  styleUrl: './create-cycle.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateCycleComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly cycleService = inject(CycleService);

  readonly isLoading = this.cycleService.isLoading;
  readonly error = this.cycleService.error;
  readonly lastClosedCycle = signal<CycleDto | null>(null);
  readonly cloneEnabled = signal(false);
  readonly smartDefaultsApplied = signal(false);

  readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    department: [DEPARTMENT, [Validators.required]],
    startDate: [null as Date | null, [Validators.required]],
    endDate: [null as Date | null, [Validators.required]],
    applicationDeadline: [null as Date | null, [Validators.required]],
    interviewDate: [null as Date | null, [Validators.required]],
    selectionDate: [null as Date | null, [Validators.required]],
    totalScholarshipsAvailable: [10, [Validators.required, Validators.min(1)]],
  });

  readonly minDate = new Date();

  /** Date flow steps shown in the visual timeline above the date fields */
  readonly dateFlowSteps = [
    { key: 'startDate', label: 'Inicio', icon: 'pi-play' },
    { key: 'applicationDeadline', label: 'Postulaciones', icon: 'pi-users' },
    { key: 'interviewDate', label: 'Entrevistas', icon: 'pi-comments' },
    { key: 'selectionDate', label: 'Selección', icon: 'pi-star' },
    { key: 'endDate', label: 'Fin', icon: 'pi-flag' },
  ] as const;

  ngOnInit(): void {
    this.loadDashboardForClone();
    this.applySmartDefaults();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();

    const request: CreateCycleRequest = {
      name: v.name!,
      department: v.department!,
      startDate: this.formatDate(v.startDate!),
      endDate: this.formatDate(v.endDate!),
      applicationDeadline: this.formatDate(v.applicationDeadline!),
      interviewDate: this.formatDate(v.interviewDate!),
      selectionDate: this.formatDate(v.selectionDate!),
      totalScholarshipsAvailable: v.totalScholarshipsAvailable!,
      ...(this.cloneEnabled() && this.lastClosedCycle()
        ? { cloneFromCycleId: this.lastClosedCycle()!.id }
        : {}),
    };

    this.cycleService.createCycle(request).subscribe({
      next: (cycle) => {
        this.router.navigate(['/admin/cycles', cycle.id]);
      },
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/dashboard']);
  }

  toggleClone(checked: boolean): void {
    this.cloneEnabled.set(checked);
  }

  isFieldInvalid(field: string): boolean {
    const control = this.form.get(field);
    return !!control && control.invalid && control.touched;
  }

  private applySmartDefaults(): void {
    const now = new Date();
    const currentMonth = now.getMonth();
    const currentYear = now.getFullYear();

    // Determine semester: Jan-Jun = 1, Jul-Dec = 2
    const semester = currentMonth < 6 ? 1 : 2;
    this.form.patchValue({ name: `${currentYear}-${semester}` });

    // Calculate dates
    const startDate = new Date(currentYear, semester === 1 ? 1 : 7, 1);
    if (startDate < now) {
      startDate.setMonth(now.getMonth() + 1, 1);
    }

    const applicationDeadline = this.addWeeks(startDate, 2);
    const interviewDate = this.addWeeks(applicationDeadline, 1);
    const selectionDate = this.addWeeks(interviewDate, 1);
    const endDate = this.addWeeks(startDate, 16);

    this.form.patchValue({
      startDate,
      endDate,
      applicationDeadline,
      interviewDate,
      selectionDate,
    });

    this.smartDefaultsApplied.set(true);
  }

  private loadDashboardForClone(): void {
    this.cycleService.getDashboardState(DEPARTMENT).subscribe({
      next: (state: AdminDashboardStateDto) => {
        if (state.lastClosedCycle) {
          this.lastClosedCycle.set(state.lastClosedCycle);
        }
      },
    });
  }

  private addWeeks(date: Date, weeks: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + weeks * 7);
    return result;
  }

  private formatDate(date: Date): string {
    return date.toISOString().split('T')[0] + 'T00:00:00Z';
  }
}
