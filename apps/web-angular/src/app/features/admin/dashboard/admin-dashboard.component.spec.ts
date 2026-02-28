import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { AdminDashboardComponent } from './admin-dashboard.component';
import { ApiResponse } from '../../../core/models/api.models';
import {
  AdminDashboardStateDto,
  CycleDto,
  CycleStatus,
  PendingActionCode,
} from '../../../core/models/cycle.models';

const mockCycle: CycleDto = {
  id: 'c1',
  name: '2026-1',
  department: 'Biblioteca',
  status: CycleStatus.Active,
  startDate: '2026-03-01',
  endDate: '2026-07-01',
  applicationDeadline: '2026-03-15',
  interviewDate: '2026-03-20',
  selectionDate: '2026-03-25',
  totalScholarshipsAvailable: 10,
  totalScholarshipsAssigned: 5,
  renewalProcessCompleted: true,
  clonedFromCycleId: null,
  closedAt: null,
  locationsCount: 3,
  supervisorsCount: 2,
  createdAt: '2026-02-28T00:00:00Z',
  updatedAt: null,
};

const mockStateWithActive: AdminDashboardStateDto = {
  hasLocations: true,
  locationsCount: 3,
  hasSupervisors: true,
  supervisorsCount: 2,
  activeCycle: mockCycle,
  lastClosedCycle: null,
  cycleInConfiguration: null,
  pendingActions: [],
};

const mockStateEmpty: AdminDashboardStateDto = {
  hasLocations: false,
  locationsCount: 0,
  hasSupervisors: false,
  supervisorsCount: 0,
  activeCycle: null,
  lastClosedCycle: null,
  cycleInConfiguration: null,
  pendingActions: [
    { code: PendingActionCode.NoActiveCycle, codeString: 'NO_ACTIVE_CYCLE' },
  ],
};

const mockStateConfig: AdminDashboardStateDto = {
  hasLocations: true,
  locationsCount: 2,
  hasSupervisors: true,
  supervisorsCount: 1,
  activeCycle: null,
  lastClosedCycle: null,
  cycleInConfiguration: { ...mockCycle, status: CycleStatus.Configuration },
  pendingActions: [],
};

describe('AdminDashboardComponent', () => {
  let component: AdminDashboardComponent;
  let fixture: ComponentFixture<AdminDashboardComponent>;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminDashboardComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminDashboardComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  function flushDashboard(state: AdminDashboardStateDto): void {
    const response: ApiResponse<AdminDashboardStateDto> = { success: true, data: state };
    const req = httpMock.expectOne((r) =>
      r.url === '/api/admin/dashboard-state' && r.params.get('department') === 'Biblioteca',
    );
    req.flush(response);
  }

  it('should create', () => {
    fixture.detectChanges();
    flushDashboard(mockStateWithActive);
    expect(component).toBeTruthy();
  });

  it('should show loading initially', () => {
    fixture.detectChanges();
    expect(component.isLoading()).toBeTrue();
    flushDashboard(mockStateWithActive);
  });

  it('should display active cycle metrics after loading', () => {
    fixture.detectChanges();
    flushDashboard(mockStateWithActive);
    fixture.detectChanges();

    expect(component.state()).toEqual(mockStateWithActive);
    expect(component.isLoading()).toBeFalse();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('2026-1');
    expect(compiled.textContent).toContain('3');
    expect(compiled.textContent).toContain('Ubicaciones');
  });

  it('should display empty state when no cycle', () => {
    fixture.detectChanges();
    flushDashboard(mockStateEmpty);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('No hay ciclo activo');
  });

  it('should display configuration state', () => {
    fixture.detectChanges();
    flushDashboard(mockStateConfig);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Continuar Setup');
  });

  it('should display pending actions', () => {
    fixture.detectChanges();
    flushDashboard(mockStateEmpty);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('No hay ciclo activo');
  });

  it('should handle error state', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url === '/api/admin/dashboard-state');
    req.flush(
      { success: false, error: { code: 'SERVER_ERROR', message: 'Error del servidor' } },
      { status: 500, statusText: 'Internal Server Error' },
    );
    fixture.detectChanges();

    expect(component.hasError()).toBeTrue();
  });

  it('should navigate to create cycle', () => {
    spyOn(router, 'navigate');
    component.goToCreateCycle();
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles/new']);
  });

  it('should navigate to cycle detail', () => {
    spyOn(router, 'navigate');
    component.goToCycleDetail('c1');
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles', 'c1']);
  });

  it('should navigate to history', () => {
    spyOn(router, 'navigate');
    component.goToHistory();
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles/history']);
  });

  it('should return correct status labels', () => {
    expect(component.getStatusLabel(CycleStatus.Active)).toBe('Activo');
    expect(component.getStatusLabel(CycleStatus.Configuration)).toBe('Configuración');
    expect(component.getStatusLabel(CycleStatus.Closed)).toBe('Cerrado');
  });

  it('should retry loading on reintentar click', () => {
    fixture.detectChanges();

    const req1 = httpMock.expectOne((r) => r.url === '/api/admin/dashboard-state');
    req1.flush('error', { status: 500, statusText: 'Error' });
    fixture.detectChanges();

    expect(component.hasError()).toBeTrue();

    component.loadDashboard();
    flushDashboard(mockStateWithActive);
    fixture.detectChanges();

    expect(component.hasError()).toBeFalse();
    expect(component.state()).toEqual(mockStateWithActive);
  });
});
