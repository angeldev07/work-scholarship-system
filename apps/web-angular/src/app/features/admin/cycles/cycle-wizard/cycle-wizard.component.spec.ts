import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { CycleWizardComponent } from './cycle-wizard.component';
import { ApiResponse } from '../../../../core/models/api.models';
import { CycleDetailDto, CycleDto, CycleStatus, ConfigureCycleRequest } from '../../../../core/models/cycle.models';

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
  locationsCount: 0,
  supervisorsCount: 0,
  createdAt: '2026-02-28T00:00:00Z',
  updatedAt: null,
  closedBy: null,
  createdBy: 'admin-1',
  scholarsCount: 0,
};

describe('CycleWizardComponent', () => {
  let component: CycleWizardComponent;
  let fixture: ComponentFixture<CycleWizardComponent>;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CycleWizardComponent],
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

    fixture = TestBed.createComponent(CycleWizardComponent);
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

  describe('Rendering', () => {
    it('should create', () => {
      fixture.detectChanges();
      flushCycle();
      expect(component).toBeTruthy();
    });

    it('should show loading state initially', () => {
      fixture.detectChanges();
      expect(component.isLoadingCycle()).toBeTrue();
      flushCycle();
    });

    it('should load cycle on init and set cycle signal', () => {
      fixture.detectChanges();
      flushCycle();

      expect(component.cycle()).toEqual(mockCycleDetail);
      expect(component.isLoadingCycle()).toBeFalse();
    });

    it('should show error state on load failure', () => {
      fixture.detectChanges();

      const req = httpMock.expectOne('/api/cycles/c1');
      req.flush(
        { success: false, error: { code: 'CYCLE_NOT_FOUND', message: 'No encontrado' } },
        { status: 404, statusText: 'Not Found' },
      );
      fixture.detectChanges();

      expect(component.loadError()).toBeTruthy();
      expect(component.isLoadingCycle()).toBeFalse();
    });

    it('should start at step 1', () => {
      fixture.detectChanges();
      flushCycle();
      expect(component.activeStep()).toBe(1);
    });
  });

  describe('Step Navigation', () => {
    beforeEach(() => {
      fixture.detectChanges();
      flushCycle();
      fixture.detectChanges();
    });

    it('should advance to step 2 when locations are active', () => {
      // Default: MOCK_LOCATIONS all active
      component.onLocationsChange([
        { locationId: 'loc-1', locationName: 'Sala', building: 'B', isActive: true, scholarshipsAvailable: 2 },
      ]);
      component.nextFromLocations();
      expect(component.activeStep()).toBe(2);
    });

    it('should not advance from step 1 when no active locations', () => {
      component.onLocationsChange([
        { locationId: 'loc-1', locationName: 'Sala', building: 'B', isActive: false, scholarshipsAvailable: 0 },
      ]);
      component.nextFromLocations();
      expect(component.activeStep()).toBe(1);
    });

    it('should advance to step 3 from step 2 when all supervisors assigned', () => {
      component.onLocationsChange([
        { locationId: 'loc-1', locationName: 'Sala', building: 'B', isActive: true, scholarshipsAvailable: 2 },
      ]);
      component.onSupervisorsChange({ 'loc-1': 'sup-1' });
      component.goToStep(2);
      component.nextFromSupervisors();
      expect(component.activeStep()).toBe(3);
    });

    it('should not advance from step 2 when supervisor unassigned', () => {
      component.onLocationsChange([
        { locationId: 'loc-1', locationName: 'Sala', building: 'B', isActive: true, scholarshipsAvailable: 2 },
      ]);
      component.onSupervisorsChange({ 'loc-1': '' });
      component.goToStep(2);
      component.nextFromSupervisors();
      expect(component.activeStep()).toBe(2);
    });

    it('should go to specific step via goToStep', () => {
      component.goToStep(3);
      expect(component.activeStep()).toBe(3);
    });
  });

  describe('Computed signals', () => {
    beforeEach(() => {
      fixture.detectChanges();
      flushCycle();
    });

    it('should return only active locations', () => {
      component.onLocationsChange([
        { locationId: 'loc-1', locationName: 'A', building: 'B', isActive: true, scholarshipsAvailable: 2 },
        { locationId: 'loc-2', locationName: 'B', building: 'C', isActive: false, scholarshipsAvailable: 0 },
      ]);
      expect(component.activeLocations().length).toBe(1);
      expect(component.activeLocations()[0].locationId).toBe('loc-1');
    });

    it('canProceedFromLocations should be false when all inactive', () => {
      component.onLocationsChange([
        { locationId: 'loc-1', locationName: 'A', building: 'B', isActive: false, scholarshipsAvailable: 0 },
      ]);
      expect(component.canProceedFromLocations()).toBeFalse();
    });

    it('canProceedFromLocations should be true when at least one active', () => {
      component.onLocationsChange([
        { locationId: 'loc-1', locationName: 'A', building: 'B', isActive: true, scholarshipsAvailable: 2 },
      ]);
      expect(component.canProceedFromLocations()).toBeTrue();
    });

    it('canProceedFromSupervisors should be false when supervisor missing', () => {
      component.onLocationsChange([
        { locationId: 'loc-1', locationName: 'A', building: 'B', isActive: true, scholarshipsAvailable: 2 },
      ]);
      component.onSupervisorsChange({ 'loc-1': '' });
      expect(component.canProceedFromSupervisors()).toBeFalse();
    });

    it('canProceedFromSupervisors should be true when all assigned', () => {
      component.onLocationsChange([
        { locationId: 'loc-1', locationName: 'A', building: 'B', isActive: true, scholarshipsAvailable: 2 },
      ]);
      component.onSupervisorsChange({ 'loc-1': 'sup-1' });
      expect(component.canProceedFromSupervisors()).toBeTrue();
    });
  });

  describe('Save / Submit', () => {
    beforeEach(() => {
      fixture.detectChanges();
      flushCycle();

      // Set up valid wizard state
      component.onLocationsChange([
        { locationId: 'loc-1', locationName: 'Sala', building: 'B', isActive: true, scholarshipsAvailable: 2 },
      ]);
      component.onSupervisorsChange({ 'loc-1': 'sup-1' });
      const start = new Date();
      start.setHours(9, 0, 0, 0);
      const end = new Date();
      end.setHours(13, 0, 0, 0);
      component.onSchedulesChange({
        'loc-1': [{ dayOfWeek: 1, startTime: start, endTime: end, requiredScholars: 1 }],
      });
    });

    it('should not call API when canSave is false', () => {
      component.onSchedulesChange({ 'loc-1': [{ dayOfWeek: 1, startTime: null, endTime: null, requiredScholars: 1 }] });
      expect(component.canSave()).toBeFalse();
      component.save();
      httpMock.expectNone('/api/cycles/c1/configure');
    });

    it('should call configureCycle API with correct payload', () => {
      spyOn(router, 'navigate');
      component.save();

      const req = httpMock.expectOne('/api/cycles/c1/configure');
      expect(req.request.method).toBe('PUT');

      const body = req.request.body as ConfigureCycleRequest;
      expect(body.locations.length).toBe(1);
      expect(body.locations[0].locationId).toBe('loc-1');
      expect(body.locations[0].scholarshipsAvailable).toBe(2);
      expect(body.locations[0].scheduleSlots[0].dayOfWeek).toBe(1);
      expect(body.locations[0].scheduleSlots[0].startTime).toBe('09:00');
      expect(body.supervisorAssignments[0].supervisorId).toBe('sup-1');
      expect(body.supervisorAssignments[0].cycleLocationId).toBe('loc-1');

      const updatedCycle: CycleDto = {
        ...mockCycleDetail,
        locationsCount: 1,
        supervisorsCount: 1,
      };
      req.flush({ success: true, data: updatedCycle } as ApiResponse<CycleDto>);

      expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles', 'c1']);
    });

    it('should set saveError on API failure', () => {
      component.save();

      const req = httpMock.expectOne('/api/cycles/c1/configure');
      req.flush(
        { success: false, error: { code: 'NOT_IN_CONFIGURATION', message: 'No en configuración' } },
        { status: 422, statusText: 'Unprocessable Entity' },
      );

      expect(component.saveError()).toBeTruthy();
      expect(component.isSaving()).toBeFalse();
    });
  });

  describe('Navigation', () => {
    it('should navigate back to cycle detail', () => {
      fixture.detectChanges();
      flushCycle();

      spyOn(router, 'navigate');
      component.goBack();
      expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles', 'c1']);
    });
  });

  describe('Event handlers', () => {
    beforeEach(() => {
      fixture.detectChanges();
      flushCycle();
    });

    it('should update locationRows when onLocationsChange fires', () => {
      const rows = [{ locationId: 'x', locationName: 'X', building: 'Y', isActive: true, scholarshipsAvailable: 3 }];
      component.onLocationsChange(rows);
      expect(component.locationRows()).toEqual(rows);
    });

    it('should update supervisorAssignments when onSupervisorsChange fires', () => {
      component.onSupervisorsChange({ 'loc-1': 'sup-2' });
      expect(component.supervisorAssignments()['loc-1']).toBe('sup-2');
    });

    it('should update scheduleMap when onSchedulesChange fires', () => {
      const start = new Date();
      start.setHours(8, 0, 0, 0);
      const end = new Date();
      end.setHours(12, 0, 0, 0);
      const map = { 'loc-1': [{ dayOfWeek: 2, startTime: start, endTime: end, requiredScholars: 2 }] };
      component.onSchedulesChange(map);
      expect(component.scheduleMap()['loc-1'][0].dayOfWeek).toBe(2);
    });
  });
});
