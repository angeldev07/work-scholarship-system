// ============================================================================
// ENUMS
// ============================================================================

export enum UserRole {
  NONE,
  ADMIN,
  SUPERVISOR,
  BECA,
}

export enum AuthProvider {
  Local = 'Local',
  Google = 'Google',
}

// ============================================================================
// USER TYPES
// ============================================================================

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  role: UserRole;
  photoUrl: string | null;
  isActive: boolean;
  lastLogin: string | null;
  authProvider: AuthProvider;
}

// ============================================================================
// AUTH REQUEST TYPES
// ============================================================================

export interface LoginRequest {
  email: string;
  password: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

// ============================================================================
// AUTH RESPONSE TYPES
// ============================================================================

export interface TokenResult {
  accessToken: string;
  expiresIn: number;
  tokenType: 'Bearer';
}

export interface LoginResponse {
  accessToken: string;
  expiresIn: number;
  tokenType: 'Bearer';
  user: UserDto;
}

export interface RefreshTokenResponse {
  accessToken: string;
  expiresIn: number;
  tokenType: 'Bearer';
}

// ============================================================================
// API WRAPPER TYPES (re-exported from api.models.ts)
// ============================================================================

import type { ApiError as _ApiError } from './api.models';
export type { ApiResponse, ApiError, ValidationError } from './api.models';

type ApiError = _ApiError;

// ============================================================================
// ERROR CODE CONSTANTS
// ============================================================================

export const AUTH_ERROR_CODES = {
  VALIDATION_ERROR: 'VALIDATION_ERROR',
  INVALID_CREDENTIALS: 'INVALID_CREDENTIALS',
  ACCOUNT_LOCKED: 'ACCOUNT_LOCKED',
  GOOGLE_ACCOUNT: 'GOOGLE_ACCOUNT',
  INVALID_REFRESH_TOKEN: 'INVALID_REFRESH_TOKEN',
  SESSION_EXPIRED: 'SESSION_EXPIRED',
  UNAUTHORIZED: 'UNAUTHORIZED',
  PASSWORD_MISMATCH: 'PASSWORD_MISMATCH',
  WEAK_PASSWORD: 'WEAK_PASSWORD',
  INVALID_TOKEN: 'INVALID_TOKEN',
  INVALID_CURRENT_PASSWORD: 'INVALID_CURRENT_PASSWORD',
  RATE_LIMIT_EXCEEDED: 'RATE_LIMIT_EXCEEDED',
} as const;

export type AuthErrorCode = (typeof AUTH_ERROR_CODES)[keyof typeof AUTH_ERROR_CODES];

// ============================================================================
// AUTH STATE TYPES
// ============================================================================

export interface AuthState {
  user: UserDto | null;
  accessToken: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: ApiError | null;
}
