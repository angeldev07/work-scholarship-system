// ============================================================================
// MOCK DATA — Cycle Configuration Wizard
// Used while Location CRUD and User list endpoints are not yet implemented.
// ============================================================================

export interface MockLocation {
  id: string;
  name: string;
  building: string;
  capacity: number;
}

export interface MockSupervisor {
  id: string;
  fullName: string;
  email: string;
}

export const MOCK_LOCATIONS: MockLocation[] = [
  { id: 'loc-1', name: 'Sala de Lectura Principal', building: 'Edificio Central', capacity: 50 },
  { id: 'loc-2', name: 'Hemeroteca', building: 'Edificio Central', capacity: 30 },
  { id: 'loc-3', name: 'Sala de Cómputo', building: 'Edificio B', capacity: 40 },
  { id: 'loc-4', name: 'Archivo Histórico', building: 'Edificio C', capacity: 15 },
  { id: 'loc-5', name: 'Sala Audiovisual', building: 'Edificio B', capacity: 25 },
];

export const MOCK_SUPERVISORS: MockSupervisor[] = [
  { id: 'sup-1', fullName: 'María García López', email: 'maria.garcia@uni.edu' },
  { id: 'sup-2', fullName: 'Carlos Hernández', email: 'carlos.h@uni.edu' },
  { id: 'sup-3', fullName: 'Ana Martínez', email: 'ana.m@uni.edu' },
];
