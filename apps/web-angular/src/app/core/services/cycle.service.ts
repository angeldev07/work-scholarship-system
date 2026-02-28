import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, catchError, map, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiError, ApiResponse, PaginatedList } from '../models/api.models';
import {
  AdminDashboardStateDto,
  ConfigureCycleRequest,
  CreateCycleRequest,
  CycleDetailDto,
  CycleDto,
  CycleListItemDto,
  ExtendCycleDatesRequest,
  ListCyclesParams,
} from '../models/cycle.models';

@Injectable({ providedIn: 'root' })
export class CycleService {
  private readonly http = inject(HttpClient);

  private readonly apiUrl = environment.apiUrl;

  // ─── Private state signals ────────────────────────────────────────────────
  private readonly _isLoading = signal(false);
  private readonly _error = signal<ApiError | null>(null);
  private readonly _currentCycle = signal<CycleDto | null>(null);
  private readonly _dashboardState = signal<AdminDashboardStateDto | null>(null);

  // ─── Public readonly signals ──────────────────────────────────────────────
  readonly isLoading = this._isLoading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly currentCycle = this._currentCycle.asReadonly();
  readonly dashboardState = this._dashboardState.asReadonly();

  // ─── Clear error ──────────────────────────────────────────────────────────
  clearError(): void {
    this._error.set(null);
  }

  // ─── Create cycle ─────────────────────────────────────────────────────────
  createCycle(request: CreateCycleRequest): Observable<CycleDto> {
    this._isLoading.set(true);
    this._error.set(null);

    return this.http
      .post<ApiResponse<CycleDto>>(`${this.apiUrl}/api/cycles`, request)
      .pipe(
        map((response) => this.extractData(response)),
        tap((cycle) => {
          this._isLoading.set(false);
          this._currentCycle.set(cycle);
        }),
        catchError((err) => this.handleError(err)),
      );
  }

  // ─── List cycles ──────────────────────────────────────────────────────────
  listCycles(params?: ListCyclesParams): Observable<PaginatedList<CycleListItemDto>> {
    this._isLoading.set(true);
    this._error.set(null);

    let httpParams = new HttpParams();
    if (params?.department) httpParams = httpParams.set('department', params.department);
    if (params?.year != null) httpParams = httpParams.set('year', params.year.toString());
    if (params?.status != null) httpParams = httpParams.set('status', params.status.toString());
    if (params?.page != null) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize != null) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http
      .get<ApiResponse<PaginatedList<CycleListItemDto>>>(`${this.apiUrl}/api/cycles`, { params: httpParams })
      .pipe(
        map((response) => this.extractData(response)),
        tap(() => this._isLoading.set(false)),
        catchError((err) => this.handleError(err)),
      );
  }

  // ─── Get active cycle ─────────────────────────────────────────────────────
  getActiveCycle(department: string): Observable<CycleDto | null> {
    this._isLoading.set(true);
    this._error.set(null);

    const params = new HttpParams().set('department', department);

    return this.http
      .get<ApiResponse<CycleDto | null>>(`${this.apiUrl}/api/cycles/active`, { params })
      .pipe(
        map((response) => {
          if (!response.success) {
            throw response.error ?? { code: 'UNKNOWN', message: 'Error al obtener ciclo activo' };
          }
          return response.data ?? null;
        }),
        tap((cycle) => {
          this._isLoading.set(false);
          if (cycle) this._currentCycle.set(cycle);
        }),
        catchError((err) => this.handleError(err)),
      );
  }

  // ─── Get cycle by ID ──────────────────────────────────────────────────────
  getCycleById(id: string): Observable<CycleDetailDto> {
    this._isLoading.set(true);
    this._error.set(null);

    return this.http
      .get<ApiResponse<CycleDetailDto>>(`${this.apiUrl}/api/cycles/${id}`)
      .pipe(
        map((response) => this.extractData(response)),
        tap((cycle) => {
          this._isLoading.set(false);
          this._currentCycle.set(cycle);
        }),
        catchError((err) => this.handleError(err)),
      );
  }

  // ─── Configure cycle ──────────────────────────────────────────────────────
  configureCycle(cycleId: string, request: ConfigureCycleRequest): Observable<CycleDto> {
    this._isLoading.set(true);
    this._error.set(null);

    return this.http
      .put<ApiResponse<CycleDto>>(`${this.apiUrl}/api/cycles/${cycleId}/configure`, request)
      .pipe(
        map((response) => this.extractData(response)),
        tap((cycle) => {
          this._isLoading.set(false);
          this._currentCycle.set(cycle);
        }),
        catchError((err) => this.handleError(err)),
      );
  }

  // ─── Open applications ────────────────────────────────────────────────────
  openApplications(cycleId: string): Observable<CycleDto> {
    return this.postTransition(cycleId, 'open-applications');
  }

  // ─── Close applications ───────────────────────────────────────────────────
  closeApplications(cycleId: string): Observable<CycleDto> {
    return this.postTransition(cycleId, 'close-applications');
  }

  // ─── Reopen applications ──────────────────────────────────────────────────
  reopenApplications(cycleId: string): Observable<CycleDto> {
    return this.postTransition(cycleId, 'reopen-applications');
  }

  // ─── Close cycle ──────────────────────────────────────────────────────────
  closeCycle(cycleId: string): Observable<CycleDto> {
    return this.postTransition(cycleId, 'close');
  }

  // ─── Extend dates ─────────────────────────────────────────────────────────
  extendDates(cycleId: string, request: ExtendCycleDatesRequest): Observable<CycleDto> {
    this._isLoading.set(true);
    this._error.set(null);

    return this.http
      .put<ApiResponse<CycleDto>>(`${this.apiUrl}/api/cycles/${cycleId}/extend-dates`, request)
      .pipe(
        map((response) => this.extractData(response)),
        tap((cycle) => {
          this._isLoading.set(false);
          this._currentCycle.set(cycle);
        }),
        catchError((err) => this.handleError(err)),
      );
  }

  // ─── Get dashboard state ──────────────────────────────────────────────────
  getDashboardState(department: string): Observable<AdminDashboardStateDto> {
    this._isLoading.set(true);
    this._error.set(null);

    const params = new HttpParams().set('department', department);

    return this.http
      .get<ApiResponse<AdminDashboardStateDto>>(`${this.apiUrl}/api/admin/dashboard-state`, { params })
      .pipe(
        map((response) => this.extractData(response)),
        tap((state) => {
          this._isLoading.set(false);
          this._dashboardState.set(state);
        }),
        catchError((err) => this.handleError(err)),
      );
  }

  // ─── Private helpers ──────────────────────────────────────────────────────
  private postTransition(cycleId: string, action: string): Observable<CycleDto> {
    this._isLoading.set(true);
    this._error.set(null);

    return this.http
      .post<ApiResponse<CycleDto>>(`${this.apiUrl}/api/cycles/${cycleId}/${action}`, {})
      .pipe(
        map((response) => this.extractData(response)),
        tap((cycle) => {
          this._isLoading.set(false);
          this._currentCycle.set(cycle);
        }),
        catchError((err) => this.handleError(err)),
      );
  }

  private extractData<T>(response: ApiResponse<T>): T {
    if (!response.success || !response.data) {
      throw response.error ?? { code: 'UNKNOWN', message: 'Ocurrió un error inesperado' };
    }
    return response.data;
  }

  private handleError(err: unknown): Observable<never> {
    this._isLoading.set(false);
    const apiError = this.extractApiError(err);
    this._error.set(apiError);
    return throwError(() => apiError);
  }

  private extractApiError(err: unknown): ApiError {
    if (err && typeof err === 'object' && 'code' in err) {
      return err as ApiError;
    }
    if (err instanceof HttpErrorResponse) {
      if (err.error?.error) {
        return err.error.error as ApiError;
      }
      if (err.status === 0) {
        return {
          code: 'NETWORK_ERROR',
          message: 'No se pudo conectar con el servidor. Verifica tu conexión a internet.',
        };
      }
      if (err.status >= 500) {
        return {
          code: 'SERVER_ERROR',
          message: 'Error del servidor. Por favor intenta más tarde.',
        };
      }
      return {
        code: 'UNKNOWN',
        message: err.message || 'Ocurrió un error inesperado.',
      };
    }
    return { code: 'UNKNOWN', message: 'Ocurrió un error inesperado.' };
  }
}
