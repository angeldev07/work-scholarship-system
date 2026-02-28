import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { CycleService } from './cycle.service';
import { ApiResponse, PaginatedList } from '../models/api.models';
import {
  AdminDashboardStateDto,
  CycleDetailDto,
  CycleDto,
  CycleListItemDto,
  CycleStatus,
} from '../models/cycle.models';

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

const mockCycleDetail: CycleDetailDto = {
  ...mockCycle,
  closedBy: null,
  createdBy: 'admin-1',
  scholarsCount: 0,
};

const mockListItem: CycleListItemDto = {
  id: 'c1',
  name: '2026-1',
  department: 'Biblioteca',
  status: CycleStatus.Configuration,
  startDate: '2026-03-01',
  endDate: '2026-07-01',
  totalScholarshipsAvailable: 10,
  totalScholarshipsAssigned: 0,
  createdAt: '2026-02-28T00:00:00Z',
  closedAt: null,
};

const mockDashboardState: AdminDashboardStateDto = {
  hasLocations: true,
  locationsCount: 3,
  hasSupervisors: true,
  supervisorsCount: 2,
  activeCycle: mockCycle,
  lastClosedCycle: null,
  cycleInConfiguration: null,
  pendingActions: [],
};

describe('CycleService', () => {
  let service: CycleService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        CycleService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(CycleService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should have default signal values', () => {
    expect(service.isLoading()).toBeFalse();
    expect(service.error()).toBeNull();
    expect(service.currentCycle()).toBeNull();
    expect(service.dashboardState()).toBeNull();
  });

  // ─── createCycle ─────────────────────────────────────────────────────────
  describe('createCycle', () => {
    it('should create a cycle and update currentCycle signal', () => {
      const response: ApiResponse<CycleDto> = { success: true, data: mockCycle };

      service.createCycle({
        name: '2026-1',
        department: 'Biblioteca',
        startDate: '2026-03-01',
        endDate: '2026-07-01',
        applicationDeadline: '2026-03-15',
        interviewDate: '2026-03-20',
        selectionDate: '2026-03-25',
        totalScholarshipsAvailable: 10,
      }).subscribe((cycle) => {
        expect(cycle).toEqual(mockCycle);
      });

      const req = httpMock.expectOne('/api/cycles');
      expect(req.request.method).toBe('POST');
      req.flush(response);

      expect(service.currentCycle()).toEqual(mockCycle);
      expect(service.isLoading()).toBeFalse();
    });

    it('should set error on API failure', () => {
      service.createCycle({
        name: '2026-1',
        department: 'Biblioteca',
        startDate: '2026-03-01',
        endDate: '2026-07-01',
        applicationDeadline: '2026-03-15',
        interviewDate: '2026-03-20',
        selectionDate: '2026-03-25',
        totalScholarshipsAvailable: 10,
      }).subscribe({ error: () => {} });

      const req = httpMock.expectOne('/api/cycles');
      req.flush(
        { success: false, error: { code: 'DUPLICATE_CYCLE', message: 'Ya existe un ciclo' } },
        { status: 400, statusText: 'Bad Request' },
      );

      expect(service.error()?.code).toBe('DUPLICATE_CYCLE');
      expect(service.isLoading()).toBeFalse();
    });

    it('should set isLoading to true during request', () => {
      service.createCycle({
        name: '2026-1',
        department: 'Biblioteca',
        startDate: '2026-03-01',
        endDate: '2026-07-01',
        applicationDeadline: '2026-03-15',
        interviewDate: '2026-03-20',
        selectionDate: '2026-03-25',
        totalScholarshipsAvailable: 10,
      }).subscribe();

      expect(service.isLoading()).toBeTrue();

      const req = httpMock.expectOne('/api/cycles');
      req.flush({ success: true, data: mockCycle });
    });
  });

  // ─── listCycles ──────────────────────────────────────────────────────────
  describe('listCycles', () => {
    it('should list cycles with default params', () => {
      const paginated: PaginatedList<CycleListItemDto> = {
        items: [mockListItem],
        page: 1,
        pageSize: 10,
        totalCount: 1,
        totalPages: 1,
        hasPreviousPage: false,
        hasNextPage: false,
      };
      const response: ApiResponse<PaginatedList<CycleListItemDto>> = { success: true, data: paginated };

      service.listCycles().subscribe((result) => {
        expect(result.items.length).toBe(1);
        expect(result.totalCount).toBe(1);
      });

      const req = httpMock.expectOne('/api/cycles');
      expect(req.request.method).toBe('GET');
      req.flush(response);
    });

    it('should pass query params when provided', () => {
      const paginated: PaginatedList<CycleListItemDto> = {
        items: [],
        page: 2,
        pageSize: 5,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: true,
        hasNextPage: false,
      };

      service.listCycles({
        department: 'Biblioteca',
        year: 2026,
        status: CycleStatus.Active,
        page: 2,
        pageSize: 5,
      }).subscribe((result) => {
        expect(result.page).toBe(2);
      });

      const req = httpMock.expectOne((r) =>
        r.url === '/api/cycles'
        && r.params.get('department') === 'Biblioteca'
        && r.params.get('year') === '2026'
        && r.params.get('status') === '3'
        && r.params.get('page') === '2'
        && r.params.get('pageSize') === '5',
      );
      expect(req).toBeTruthy();
      req.flush({ success: true, data: paginated });
    });
  });

  // ─── getActiveCycle ──────────────────────────────────────────────────────
  describe('getActiveCycle', () => {
    it('should return active cycle and update signal', () => {
      const response: ApiResponse<CycleDto | null> = { success: true, data: mockCycle };

      service.getActiveCycle('Biblioteca').subscribe((cycle) => {
        expect(cycle).toEqual(mockCycle);
      });

      const req = httpMock.expectOne((r) =>
        r.url === '/api/cycles/active' && r.params.get('department') === 'Biblioteca',
      );
      req.flush(response);

      expect(service.currentCycle()).toEqual(mockCycle);
    });

    it('should return null when no active cycle exists', () => {
      const response: ApiResponse<CycleDto | null> = { success: true, data: null };

      service.getActiveCycle('Biblioteca').subscribe((cycle) => {
        expect(cycle).toBeNull();
      });

      const req = httpMock.expectOne((r) => r.url === '/api/cycles/active');
      req.flush(response);
    });
  });

  // ─── getCycleById ────────────────────────────────────────────────────────
  describe('getCycleById', () => {
    it('should return cycle detail and update signal', () => {
      const response: ApiResponse<CycleDetailDto> = { success: true, data: mockCycleDetail };

      service.getCycleById('c1').subscribe((cycle) => {
        expect(cycle.createdBy).toBe('admin-1');
      });

      const req = httpMock.expectOne('/api/cycles/c1');
      expect(req.request.method).toBe('GET');
      req.flush(response);

      expect(service.currentCycle()).toEqual(mockCycleDetail);
    });

    it('should handle 404 not found', () => {
      service.getCycleById('invalid').subscribe({ error: () => {} });

      const req = httpMock.expectOne('/api/cycles/invalid');
      req.flush(
        { success: false, error: { code: 'CYCLE_NOT_FOUND', message: 'Ciclo no encontrado' } },
        { status: 404, statusText: 'Not Found' },
      );

      expect(service.error()?.code).toBe('CYCLE_NOT_FOUND');
    });
  });

  // ─── configureCycle ──────────────────────────────────────────────────────
  describe('configureCycle', () => {
    it('should configure cycle and update signal', () => {
      const configured = { ...mockCycle, locationsCount: 2, supervisorsCount: 1 };
      const response: ApiResponse<CycleDto> = { success: true, data: configured };

      service.configureCycle('c1', {
        locations: [],
        supervisorAssignments: [],
      }).subscribe((cycle) => {
        expect(cycle.locationsCount).toBe(2);
      });

      const req = httpMock.expectOne('/api/cycles/c1/configure');
      expect(req.request.method).toBe('PUT');
      req.flush(response);
    });
  });

  // ─── State transitions ──────────────────────────────────────────────────
  describe('openApplications', () => {
    it('should open applications', () => {
      const updated = { ...mockCycle, status: CycleStatus.ApplicationsOpen };
      const response: ApiResponse<CycleDto> = { success: true, data: updated };

      service.openApplications('c1').subscribe((cycle) => {
        expect(cycle.status).toBe(CycleStatus.ApplicationsOpen);
      });

      const req = httpMock.expectOne('/api/cycles/c1/open-applications');
      expect(req.request.method).toBe('POST');
      req.flush(response);
    });
  });

  describe('closeApplications', () => {
    it('should close applications', () => {
      const updated = { ...mockCycle, status: CycleStatus.ApplicationsClosed };

      service.closeApplications('c1').subscribe((cycle) => {
        expect(cycle.status).toBe(CycleStatus.ApplicationsClosed);
      });

      const req = httpMock.expectOne('/api/cycles/c1/close-applications');
      expect(req.request.method).toBe('POST');
      req.flush({ success: true, data: updated });
    });
  });

  describe('reopenApplications', () => {
    it('should reopen applications', () => {
      const updated = { ...mockCycle, status: CycleStatus.ApplicationsOpen };

      service.reopenApplications('c1').subscribe((cycle) => {
        expect(cycle.status).toBe(CycleStatus.ApplicationsOpen);
      });

      const req = httpMock.expectOne('/api/cycles/c1/reopen-applications');
      expect(req.request.method).toBe('POST');
      req.flush({ success: true, data: updated });
    });
  });

  describe('closeCycle', () => {
    it('should close the cycle', () => {
      const updated = { ...mockCycle, status: CycleStatus.Closed, closedAt: '2026-07-01T00:00:00Z' };

      service.closeCycle('c1').subscribe((cycle) => {
        expect(cycle.status).toBe(CycleStatus.Closed);
      });

      const req = httpMock.expectOne('/api/cycles/c1/close');
      expect(req.request.method).toBe('POST');
      req.flush({ success: true, data: updated });
    });

    it('should handle transition error (409 Conflict)', () => {
      service.closeCycle('c1').subscribe({ error: () => {} });

      const req = httpMock.expectOne('/api/cycles/c1/close');
      req.flush(
        { success: false, error: { code: 'INVALID_TRANSITION', message: 'Transición inválida' } },
        { status: 409, statusText: 'Conflict' },
      );

      expect(service.error()?.code).toBe('INVALID_TRANSITION');
    });
  });

  // ─── extendDates ─────────────────────────────────────────────────────────
  describe('extendDates', () => {
    it('should extend dates', () => {
      const updated = { ...mockCycle, endDate: '2026-08-01' };
      const response: ApiResponse<CycleDto> = { success: true, data: updated };

      service.extendDates('c1', { newEndDate: '2026-08-01' }).subscribe((cycle) => {
        expect(cycle.endDate).toBe('2026-08-01');
      });

      const req = httpMock.expectOne('/api/cycles/c1/extend-dates');
      expect(req.request.method).toBe('PUT');
      req.flush(response);
    });
  });

  // ─── getDashboardState ───────────────────────────────────────────────────
  describe('getDashboardState', () => {
    it('should fetch dashboard state and update signal', () => {
      const response: ApiResponse<AdminDashboardStateDto> = { success: true, data: mockDashboardState };

      service.getDashboardState('Biblioteca').subscribe((state) => {
        expect(state.hasLocations).toBeTrue();
        expect(state.activeCycle).toEqual(mockCycle);
      });

      const req = httpMock.expectOne((r) =>
        r.url === '/api/admin/dashboard-state' && r.params.get('department') === 'Biblioteca',
      );
      expect(req.request.method).toBe('GET');
      req.flush(response);

      expect(service.dashboardState()).toEqual(mockDashboardState);
    });
  });

  // ─── clearError ──────────────────────────────────────────────────────────
  describe('clearError', () => {
    it('should clear the error signal', () => {
      service.createCycle({
        name: '2026-1',
        department: 'Biblioteca',
        startDate: '2026-03-01',
        endDate: '2026-07-01',
        applicationDeadline: '2026-03-15',
        interviewDate: '2026-03-20',
        selectionDate: '2026-03-25',
        totalScholarshipsAvailable: 10,
      }).subscribe({ error: () => {} });

      httpMock.expectOne('/api/cycles').flush(
        { success: false, error: { code: 'DUPLICATE_CYCLE', message: 'Ya existe' } },
        { status: 400, statusText: 'Bad Request' },
      );

      expect(service.error()).not.toBeNull();

      service.clearError();
      expect(service.error()).toBeNull();
    });
  });

  // ─── Network error ───────────────────────────────────────────────────────
  describe('network error handling', () => {
    it('should handle network error', () => {
      service.getCycleById('c1').subscribe({ error: () => {} });

      const req = httpMock.expectOne('/api/cycles/c1');
      req.error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });

      expect(service.error()?.code).toBe('NETWORK_ERROR');
    });

    it('should handle server error', () => {
      service.getCycleById('c1').subscribe({ error: () => {} });

      const req = httpMock.expectOne('/api/cycles/c1');
      req.flush('Internal Server Error', { status: 500, statusText: 'Internal Server Error' });

      expect(service.error()?.code).toBe('SERVER_ERROR');
    });
  });
});
