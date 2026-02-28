import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { CreateCycleComponent } from './create-cycle.component';
import { ApiResponse } from '../../../../core/models/api.models';
import { AdminDashboardStateDto, CycleDto, CycleStatus } from '../../../../core/models/cycle.models';

const mockCycle: CycleDto = {
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
  locationsCount: 0,
  supervisorsCount: 0,
  createdAt: '2026-02-28T00:00:00Z',
  updatedAt: null,
};

const mockDashboardState: AdminDashboardStateDto = {
  hasLocations: false,
  locationsCount: 0,
  hasSupervisors: false,
  supervisorsCount: 0,
  activeCycle: null,
  lastClosedCycle: null,
  cycleInConfiguration: null,
  pendingActions: [],
};

describe('CreateCycleComponent', () => {
  let component: CreateCycleComponent;
  let fixture: ComponentFixture<CreateCycleComponent>;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CreateCycleComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CreateCycleComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  function flushDashboard(state: AdminDashboardStateDto = mockDashboardState): void {
    const req = httpMock.expectOne((r) => r.url === '/api/admin/dashboard-state');
    req.flush({ success: true, data: state } as ApiResponse<AdminDashboardStateDto>);
  }

  describe('Initialization', () => {
    it('should create', () => {
      fixture.detectChanges();
      flushDashboard();
      expect(component).toBeTruthy();
    });

    it('should populate smart defaults on init', () => {
      fixture.detectChanges();
      flushDashboard();

      const name = component.form.get('name')?.value;
      expect(name).toMatch(/^\d{4}-[12]$/);

      expect(component.form.get('startDate')?.value).toBeTruthy();
      expect(component.form.get('endDate')?.value).toBeTruthy();
    });

    it('should set smartDefaultsApplied to true after init', () => {
      fixture.detectChanges();
      flushDashboard();

      expect(component.smartDefaultsApplied()).toBeTrue();
    });

    it('should initialize department as Biblioteca (readonly)', () => {
      fixture.detectChanges();
      flushDashboard();

      expect(component.form.get('department')?.value).toBe('Biblioteca');
    });

    it('should initialize cloneEnabled as false', () => {
      fixture.detectChanges();
      flushDashboard();

      expect(component.cloneEnabled()).toBeFalse();
    });
  });

  describe('Form validation', () => {
    it('should not submit when form is invalid', () => {
      fixture.detectChanges();
      flushDashboard();

      component.form.patchValue({ name: '' });
      component.onSubmit();

      expect(component.form.get('name')?.touched).toBeTrue();
    });

    it('should validate field invalid state when untouched', () => {
      fixture.detectChanges();
      flushDashboard();

      expect(component.isFieldInvalid('name')).toBeFalse();
    });

    it('should validate field invalid state when touched and empty', () => {
      fixture.detectChanges();
      flushDashboard();

      component.form.get('name')?.setValue('');
      component.form.get('name')?.markAsTouched();
      expect(component.isFieldInvalid('name')).toBeTrue();
    });

    it('should not show field invalid when field has value', () => {
      fixture.detectChanges();
      flushDashboard();

      component.form.get('name')?.setValue('2026-1');
      component.form.get('name')?.markAsTouched();
      expect(component.isFieldInvalid('name')).toBeFalse();
    });
  });

  describe('Form submission', () => {
    it('should submit valid form and navigate to cycle detail', () => {
      fixture.detectChanges();
      flushDashboard();

      spyOn(router, 'navigate');

      component.onSubmit();

      const req = httpMock.expectOne('/api/cycles');
      expect(req.request.method).toBe('POST');
      expect(req.request.body.department).toBe('Biblioteca');
      req.flush({ success: true, data: mockCycle } as ApiResponse<CycleDto>);

      expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles', 'c1']);
    });

    it('should include cloneFromCycleId when clone is enabled', () => {
      const stateWithClosed = {
        ...mockDashboardState,
        lastClosedCycle: { ...mockCycle, status: CycleStatus.Closed, closedAt: '2026-01-15' },
      };

      fixture.detectChanges();
      flushDashboard(stateWithClosed);
      fixture.detectChanges();

      component.toggleClone(true);
      component.onSubmit();

      const req = httpMock.expectOne('/api/cycles');
      expect(req.request.body.cloneFromCycleId).toBe('c1');
      req.flush({ success: true, data: mockCycle } as ApiResponse<CycleDto>);
    });

    it('should not include cloneFromCycleId when clone is disabled', () => {
      fixture.detectChanges();
      flushDashboard();

      component.onSubmit();

      const req = httpMock.expectOne('/api/cycles');
      expect(req.request.body.cloneFromCycleId).toBeUndefined();
      req.flush({ success: true, data: mockCycle } as ApiResponse<CycleDto>);
    });
  });

  describe('Clone configuration', () => {
    it('should show clone option when lastClosedCycle exists', () => {
      const stateWithClosed = {
        ...mockDashboardState,
        lastClosedCycle: { ...mockCycle, status: CycleStatus.Closed, closedAt: '2026-01-15' },
      };

      fixture.detectChanges();
      flushDashboard(stateWithClosed);
      fixture.detectChanges();

      expect(component.lastClosedCycle()).toBeTruthy();
    });

    it('should not set lastClosedCycle when dashboard has no closed cycle', () => {
      fixture.detectChanges();
      flushDashboard(mockDashboardState);
      fixture.detectChanges();

      expect(component.lastClosedCycle()).toBeNull();
    });

    it('should toggle clone flag', () => {
      fixture.detectChanges();
      flushDashboard();

      expect(component.cloneEnabled()).toBeFalse();
      component.toggleClone(true);
      expect(component.cloneEnabled()).toBeTrue();
      component.toggleClone(false);
      expect(component.cloneEnabled()).toBeFalse();
    });
  });

  describe('Navigation', () => {
    it('should navigate to dashboard on cancel', () => {
      fixture.detectChanges();
      flushDashboard();

      spyOn(router, 'navigate');
      component.onCancel();
      expect(router.navigate).toHaveBeenCalledWith(['/admin/dashboard']);
    });
  });

  describe('Date flow steps', () => {
    it('should define 5 date flow steps', () => {
      fixture.detectChanges();
      flushDashboard();

      expect(component.dateFlowSteps.length).toBe(5);
    });

    it('should include all required date flow step keys', () => {
      fixture.detectChanges();
      flushDashboard();

      const keys = component.dateFlowSteps.map((s) => s.key);
      expect(keys).toContain('startDate');
      expect(keys).toContain('applicationDeadline');
      expect(keys).toContain('interviewDate');
      expect(keys).toContain('selectionDate');
      expect(keys).toContain('endDate');
    });
  });
});
