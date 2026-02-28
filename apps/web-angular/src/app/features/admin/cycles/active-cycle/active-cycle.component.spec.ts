import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ActiveCycleComponent } from './active-cycle.component';
import { ApiResponse } from '../../../../core/models/api.models';
import { CycleDto, CycleStatus } from '../../../../core/models/cycle.models';

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

describe('ActiveCycleComponent', () => {
  let component: ActiveCycleComponent;
  let fixture: ComponentFixture<ActiveCycleComponent>;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ActiveCycleComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ActiveCycleComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should create', () => {
    fixture.detectChanges();
    const req = httpMock.expectOne((r) => r.url === '/api/cycles/active');
    req.flush({ success: true, data: null } as ApiResponse<null>);
    expect(component).toBeTruthy();
  });

  it('should redirect to cycle detail when active cycle found', () => {
    spyOn(router, 'navigate');
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url === '/api/cycles/active');
    req.flush({ success: true, data: mockCycle } as ApiResponse<CycleDto>);

    expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles', 'c1']);
  });

  it('should show empty state when no active cycle', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne((r) => r.url === '/api/cycles/active');
    req.flush({ success: true, data: null } as ApiResponse<null>);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('No hay ciclo activo');
  });

  it('should navigate to create cycle', () => {
    fixture.detectChanges();
    httpMock.expectOne((r) => r.url === '/api/cycles/active').flush({ success: true, data: null });

    spyOn(router, 'navigate');
    component.goToCreate();
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles/new']);
  });
});
