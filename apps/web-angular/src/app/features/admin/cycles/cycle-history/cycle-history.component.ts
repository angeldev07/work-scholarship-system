import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CycleService } from '../../../../core/services/cycle.service';
import {
  CycleListItemDto,
  CycleStatus,
  CYCLE_STATUS_MAP,
  ListCyclesParams,
} from '../../../../core/models/cycle.models';
import { PaginatedList } from '../../../../core/models/api.models';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { PaginatorModule } from 'primeng/paginator';
import { SkeletonModule } from 'primeng/skeleton';
import { CardModule } from 'primeng/card';

interface StatusOption {
  label: string;
  value: CycleStatus | null;
}

@Component({
  selector: 'app-cycle-history',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    TableModule,
    ButtonModule,
    TagModule,
    InputTextModule,
    SelectModule,
    PaginatorModule,
    SkeletonModule,
    CardModule,
  ],
  templateUrl: './cycle-history.component.html',
  styleUrl: './cycle-history.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleHistoryComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly cycleService = inject(CycleService);

  readonly result = signal<PaginatedList<CycleListItemDto> | null>(null);
  readonly isLoading = signal(true);

  // Filters
  departmentFilter = '';
  yearFilter: number | null = null;
  statusFilter: CycleStatus | null = null;
  page = 1;
  pageSize = 10;

  readonly statusOptions: StatusOption[] = [
    { label: 'Todos', value: null },
    { label: 'Configuración', value: CycleStatus.Configuration },
    { label: 'Postulaciones Abiertas', value: CycleStatus.ApplicationsOpen },
    { label: 'Postulaciones Cerradas', value: CycleStatus.ApplicationsClosed },
    { label: 'Activo', value: CycleStatus.Active },
    { label: 'Cerrado', value: CycleStatus.Closed },
  ];

  ngOnInit(): void {
    this.loadCycles();
  }

  loadCycles(): void {
    this.isLoading.set(true);

    const params: ListCyclesParams = {
      page: this.page,
      pageSize: this.pageSize,
    };
    if (this.departmentFilter) params.department = this.departmentFilter;
    if (this.yearFilter) params.year = this.yearFilter;
    if (this.statusFilter != null) params.status = this.statusFilter;

    this.cycleService.listCycles(params).subscribe({
      next: (data) => {
        this.result.set(data);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  onFilter(): void {
    this.page = 1;
    this.loadCycles();
  }

  onPageChange(event: { page?: number; rows?: number }): void {
    this.page = (event.page ?? 0) + 1;
    this.pageSize = event.rows ?? 10;
    this.loadCycles();
  }

  onRowClick(cycle: CycleListItemDto): void {
    this.router.navigate(['/admin/cycles', cycle.id]);
  }

  goToCreate(): void {
    this.router.navigate(['/admin/cycles/new']);
  }

  getStatusLabel(status: CycleStatus): string {
    return CYCLE_STATUS_MAP[status]?.label ?? 'Desconocido';
  }

  getStatusSeverity(status: CycleStatus): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    return CYCLE_STATUS_MAP[status]?.severity ?? 'info';
  }
}
