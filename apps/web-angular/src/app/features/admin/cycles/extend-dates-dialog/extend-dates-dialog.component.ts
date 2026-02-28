import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { CycleService } from '../../../../core/services/cycle.service';
import { CycleDetailDto, ExtendCycleDatesRequest } from '../../../../core/models/cycle.models';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageModule } from 'primeng/message';
import { SkeletonModule } from 'primeng/skeleton';

@Component({
  selector: 'app-extend-dates-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    DatePipe,
    CardModule,
    ButtonModule,
    DatePickerModule,
    MessageModule,
    SkeletonModule,
  ],
  templateUrl: './extend-dates-dialog.component.html',
  styleUrl: './extend-dates-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExtendDatesDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly cycleService = inject(CycleService);

  readonly cycle = signal<CycleDetailDto | null>(null);
  readonly loadingCycle = signal(true);
  readonly isSubmitting = this.cycleService.isLoading;
  readonly error = this.cycleService.error;

  readonly form = this.fb.group({
    newApplicationDeadline: [null as Date | null],
    newInterviewDate: [null as Date | null],
    newSelectionDate: [null as Date | null],
    newEndDate: [null as Date | null],
  });

  private cycleId = '';

  ngOnInit(): void {
    this.cycleId = this.route.snapshot.paramMap.get('id') ?? '';
    if (this.cycleId) {
      this.loadCycle();
    }
  }

  onSubmit(): void {
    const v = this.form.getRawValue();

    const hasChanges = v.newApplicationDeadline || v.newInterviewDate || v.newSelectionDate || v.newEndDate;
    if (!hasChanges) return;

    const request: ExtendCycleDatesRequest = {};
    if (v.newApplicationDeadline) request.newApplicationDeadline = this.formatDate(v.newApplicationDeadline);
    if (v.newInterviewDate) request.newInterviewDate = this.formatDate(v.newInterviewDate);
    if (v.newSelectionDate) request.newSelectionDate = this.formatDate(v.newSelectionDate);
    if (v.newEndDate) request.newEndDate = this.formatDate(v.newEndDate);

    this.cycleService.extendDates(this.cycleId, request).subscribe({
      next: () => {
        this.router.navigate(['/admin/cycles', this.cycleId]);
      },
    });
  }

  onCancel(): void {
    this.router.navigate(['/admin/cycles', this.cycleId]);
  }

  getMinDate(currentDateStr: string): Date {
    const d = new Date(currentDateStr);
    d.setDate(d.getDate() + 1);
    return d;
  }

  private loadCycle(): void {
    this.loadingCycle.set(true);
    this.cycleService.getCycleById(this.cycleId).subscribe({
      next: (data) => {
        this.cycle.set(data);
        this.loadingCycle.set(false);
      },
      error: () => {
        this.loadingCycle.set(false);
      },
    });
  }

  private formatDate(date: Date): string {
    return date.toISOString().split('T')[0];
  }
}
