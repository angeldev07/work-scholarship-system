import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { CycleDetailComponent } from './cycle-detail.component';
import { ApiResponse } from '../../../../core/models/api.models';
import { CycleDetailDto, CycleDto, CycleStatus } from '../../../../core/models/cycle.models';

const mockCycleDetail: CycleDetailDto = {
  id: 'c1',
  name: '2026-1',
  department: 'Biblioteca',
  status: CycleStatus.Configuration,
  startDate: '2026-03-01',
  endDate: '2026-07-01',
  applicationDeadline: '2026-03-15',
  interviewDate: '2026-03-20',
  selectionDate: '2026-03-25',
  totalScholarshipsAvailable: 10,
  totalScholarshipsAssigned: 0,
  renewalProcessCompleted: false,
  clonedFromCycleId: null,
  closedAt: null,
  locationsCount: 2,
  supervisorsCount: 1,
  createdAt: '2026-02-28T00:00:00Z',
  updatedAt: null,
  closedBy: null,
  createdBy: 'admin-1',
  scholarsCount: 0,
};

describe('CycleDetailComponent', () => {
  let component: CycleDetailComponent;
  let fixture: ComponentFixture<CycleDetailComponent>;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CycleDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: { paramMap: { get: (key: string) => key === 'id' ? 'c1' : null } },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CycleDetailComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  function flushCycle(cycle: CycleDetailDto = mockCycleDetail): void {
    const req = httpMock.expectOne('/api/cycles/c1');
    req.flush({ success: true, data: cycle } as ApiResponse<CycleDetailDto>);
  }

  it('should create and load cycle', () => {
    fixture.detectChanges();
    flushCycle();
    expect(component.cycle()).toEqual(mockCycleDetail);
  });

  it('should show loading initially', () => {
    fixture.detectChanges();
    expect(component.isLoading()).toBeTrue();
    flushCycle();
  });

  it('should display cycle name and status', () => {
    fixture.detectChanges();
    flushCycle();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('2026-1');
  });

  it('should show configuration actions when in Configuration state', () => {
    fixture.detectChanges();
    flushCycle();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Configurar Ciclo');
    expect(compiled.textContent).toContain('Abrir Postulaciones');
  });

  it('should show close applications button when ApplicationsOpen', () => {
    fixture.detectChanges();
    flushCycle({ ...mockCycleDetail, status: CycleStatus.ApplicationsOpen });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Cerrar Postulaciones');
  });

  it('should show reopen button when ApplicationsClosed', () => {
    fixture.detectChanges();
    flushCycle({ ...mockCycleDetail, status: CycleStatus.ApplicationsClosed });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Reabrir Postulaciones');
  });

  it('should show close cycle button when Active', () => {
    fixture.detectChanges();
    flushCycle({ ...mockCycleDetail, status: CycleStatus.Active });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Cerrar Ciclo');
  });

  it('should not show actions when Closed', () => {
    fixture.detectChanges();
    flushCycle({ ...mockCycleDetail, status: CycleStatus.Closed, closedAt: '2026-07-01' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).not.toContain('Cerrar Ciclo');
    expect(compiled.textContent).not.toContain('Abrir Postulaciones');
  });

  it('should handle error state', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/cycles/c1');
    req.flush(
      { success: false, error: { code: 'CYCLE_NOT_FOUND', message: 'No encontrado' } },
      { status: 404, statusText: 'Not Found' },
    );
    fixture.detectChanges();

    expect(component.hasError()).toBeTrue();
  });

  it('should detect completed steps correctly', () => {
    fixture.detectChanges();
    flushCycle({ ...mockCycleDetail, status: CycleStatus.Active });

    expect(component.isStepCompleted(CycleStatus.Configuration)).toBeTrue();
    expect(component.isStepCompleted(CycleStatus.ApplicationsOpen)).toBeTrue();
    expect(component.isStepCompleted(CycleStatus.Active)).toBeFalse();
    expect(component.isStepActive(CycleStatus.Active)).toBeTrue();
  });

  it('should navigate to configure', () => {
    fixture.detectChanges();
    flushCycle();

    spyOn(router, 'navigate');
    component.goToConfigure();
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles', 'c1', 'configure']);
  });

  it('should navigate back', () => {
    fixture.detectChanges();
    flushCycle();

    spyOn(router, 'navigate');
    component.goBack();
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles/active']);
  });

  it('should return correct status labels', () => {
    expect(component.getStatusLabel(CycleStatus.Active)).toBe('Activo');
    expect(component.getStatusLabel(CycleStatus.Closed)).toBe('Cerrado');
  });
});
