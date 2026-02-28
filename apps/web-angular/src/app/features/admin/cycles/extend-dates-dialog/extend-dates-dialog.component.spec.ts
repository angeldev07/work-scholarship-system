import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ExtendDatesDialogComponent } from './extend-dates-dialog.component';
import { ApiResponse } from '../../../../core/models/api.models';
import { CycleDetailDto, CycleDto, CycleStatus } from '../../../../core/models/cycle.models';

const mockCycleDetail: CycleDetailDto = {
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
  locationsCount: 2,
  supervisorsCount: 1,
  createdAt: '2026-02-28T00:00:00Z',
  updatedAt: null,
  closedBy: null,
  createdBy: 'admin-1',
  scholarsCount: 3,
};

describe('ExtendDatesDialogComponent', () => {
  let component: ExtendDatesDialogComponent;
  let fixture: ComponentFixture<ExtendDatesDialogComponent>;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExtendDatesDialogComponent],
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

    fixture = TestBed.createComponent(ExtendDatesDialogComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  function flushCycle(): void {
    const req = httpMock.expectOne('/api/cycles/c1');
    req.flush({ success: true, data: mockCycleDetail } as ApiResponse<CycleDetailDto>);
  }

  it('should create and load cycle', () => {
    fixture.detectChanges();
    flushCycle();
    expect(component.cycle()).toEqual(mockCycleDetail);
  });

  it('should display current dates', () => {
    fixture.detectChanges();
    flushCycle();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Extender Fechas');
    expect(compiled.textContent).toContain('2026-1');
  });

  it('should not submit when no changes made', () => {
    fixture.detectChanges();
    flushCycle();

    component.onSubmit();
    // No HTTP request should be made since form is empty
    httpMock.expectNone('/api/cycles/c1/extend-dates');
  });

  it('should submit extended dates and navigate', () => {
    fixture.detectChanges();
    flushCycle();

    spyOn(router, 'navigate');

    component.form.patchValue({ newEndDate: new Date('2026-08-01') });
    component.onSubmit();

    const req = httpMock.expectOne('/api/cycles/c1/extend-dates');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body.newEndDate).toBe('2026-08-01');

    const updated: CycleDto = { ...mockCycleDetail, endDate: '2026-08-01' };
    req.flush({ success: true, data: updated } as ApiResponse<CycleDto>);

    expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles', 'c1']);
  });

  it('should navigate on cancel', () => {
    fixture.detectChanges();
    flushCycle();

    spyOn(router, 'navigate');
    component.onCancel();
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles', 'c1']);
  });

  it('should calculate min date as day after current', () => {
    const baseDate = new Date('2026-03-15');
    const expectedDate = new Date(baseDate);
    expectedDate.setDate(expectedDate.getDate() + 1);

    const minDate = component.getMinDate('2026-03-15');
    expect(minDate.getTime()).toBe(expectedDate.getTime());
  });
});
