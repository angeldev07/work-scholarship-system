import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { CycleHistoryComponent } from './cycle-history.component';
import { ApiResponse, PaginatedList } from '../../../../core/models/api.models';
import { CycleListItemDto, CycleStatus } from '../../../../core/models/cycle.models';

const mockItem: CycleListItemDto = {
  id: 'c1',
  name: '2026-1',
  department: 'Biblioteca',
  status: CycleStatus.Closed,
  startDate: '2026-03-01',
  endDate: '2026-07-01',
  totalScholarshipsAvailable: 10,
  totalScholarshipsAssigned: 8,
  createdAt: '2026-02-28T00:00:00Z',
  closedAt: '2026-07-01T00:00:00Z',
};

const mockPaginated: PaginatedList<CycleListItemDto> = {
  items: [mockItem],
  page: 1,
  pageSize: 10,
  totalCount: 1,
  totalPages: 1,
  hasPreviousPage: false,
  hasNextPage: false,
};

describe('CycleHistoryComponent', () => {
  let component: CycleHistoryComponent;
  let fixture: ComponentFixture<CycleHistoryComponent>;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CycleHistoryComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideNoopAnimations(),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CycleHistoryComponent);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  function flushCycles(data: PaginatedList<CycleListItemDto> = mockPaginated): void {
    const req = httpMock.expectOne((r) => r.url === '/api/cycles');
    req.flush({ success: true, data } as ApiResponse<PaginatedList<CycleListItemDto>>);
  }

  it('should create and load cycles', () => {
    fixture.detectChanges();
    flushCycles();
    expect(component.result()).toEqual(mockPaginated);
  });

  it('should display cycle in table', () => {
    fixture.detectChanges();
    flushCycles();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('2026-1');
    expect(compiled.textContent).toContain('Biblioteca');
  });

  it('should display empty state when no cycles', () => {
    fixture.detectChanges();
    flushCycles({ ...mockPaginated, items: [], totalCount: 0 });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('No se encontraron ciclos');
  });

  it('should navigate to cycle detail on row click', () => {
    fixture.detectChanges();
    flushCycles();

    spyOn(router, 'navigate');
    component.onRowClick(mockItem);
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles', 'c1']);
  });

  it('should reset page on filter', () => {
    fixture.detectChanges();
    flushCycles();

    component.page = 3;
    component.departmentFilter = 'Biblioteca';
    component.onFilter();

    expect(component.page).toBe(1);
    flushCycles();
  });

  it('should update page on page change', () => {
    fixture.detectChanges();
    flushCycles();

    component.onPageChange({ page: 2, rows: 10 });
    expect(component.page).toBe(3); // page + 1
    flushCycles();
  });

  it('should navigate to create cycle', () => {
    fixture.detectChanges();
    flushCycles();

    spyOn(router, 'navigate');
    component.goToCreate();
    expect(router.navigate).toHaveBeenCalledWith(['/admin/cycles/new']);
  });

  it('should return correct status labels', () => {
    expect(component.getStatusLabel(CycleStatus.Closed)).toBe('Cerrado');
    expect(component.getStatusLabel(CycleStatus.Active)).toBe('Activo');
  });
});
