import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { SupervisorsStepComponent } from './supervisors-step.component';
import { LocationRow, SupervisorAssignmentMap } from '../wizard.models';
import { MOCK_SUPERVISORS } from '../mock-data';

const mockLocations: LocationRow[] = [
  { locationId: 'loc-1', locationName: 'Sala Principal', building: 'Edificio A', isActive: true, scholarshipsAvailable: 3 },
  { locationId: 'loc-2', locationName: 'Hemeroteca', building: 'Edificio B', isActive: true, scholarshipsAvailable: 2 },
];

describe('SupervisorsStepComponent', () => {
  let component: SupervisorsStepComponent;
  let fixture: ComponentFixture<SupervisorsStepComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SupervisorsStepComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();

    fixture = TestBed.createComponent(SupervisorsStepComponent);
    component = fixture.componentInstance;
  });

  describe('Rendering', () => {
    it('should create', () => {
      fixture.componentRef.setInput('activeLocations', mockLocations);
      fixture.detectChanges();
      expect(component).toBeTruthy();
    });

    it('should expose MOCK_SUPERVISORS', () => {
      fixture.componentRef.setInput('activeLocations', mockLocations);
      fixture.detectChanges();
      expect(component.supervisors.length).toBe(MOCK_SUPERVISORS.length);
    });

    it('should show location names in template', () => {
      fixture.componentRef.setInput('activeLocations', mockLocations);
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('Sala Principal');
      expect(compiled.textContent).toContain('Hemeroteca');
    });

    it('should show info message when no active locations', () => {
      fixture.componentRef.setInput('activeLocations', []);
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('No hay ubicaciones activas');
    });

    it('should show warning when not all supervisors assigned', () => {
      fixture.componentRef.setInput('activeLocations', mockLocations);
      fixture.detectChanges();
      // Default assignments are empty strings → not all assigned
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('Asigna un supervisor');
    });
  });

  describe('Initialization', () => {
    it('should initialize empty assignments map on init (no initialAssignments)', () => {
      fixture.componentRef.setInput('activeLocations', mockLocations);
      fixture.detectChanges();

      const map = component.assignments();
      expect(map['loc-1']).toBe('');
      expect(map['loc-2']).toBe('');
    });

    it('should use initialAssignments when provided', () => {
      const initial: SupervisorAssignmentMap = { 'loc-1': 'sup-1', 'loc-2': 'sup-2' };
      fixture.componentRef.setInput('activeLocations', mockLocations);
      fixture.componentRef.setInput('initialAssignments', initial);
      fixture.detectChanges();

      expect(component.assignments()['loc-1']).toBe('sup-1');
      expect(component.assignments()['loc-2']).toBe('sup-2');
    });
  });

  describe('Computed signals', () => {
    it('allAssigned should be false when any location lacks supervisor', () => {
      fixture.componentRef.setInput('activeLocations', mockLocations);
      fixture.detectChanges();
      // default: empty strings
      expect(component.allAssigned()).toBeFalse();
    });

    it('allAssigned should be true when all locations have supervisors', () => {
      fixture.componentRef.setInput('activeLocations', mockLocations);
      fixture.componentRef.setInput('initialAssignments', { 'loc-1': 'sup-1', 'loc-2': 'sup-2' });
      fixture.detectChanges();
      expect(component.allAssigned()).toBeTrue();
    });

    it('allAssigned should be true when activeLocations is empty', () => {
      fixture.componentRef.setInput('activeLocations', []);
      fixture.detectChanges();
      expect(component.allAssigned()).toBeTrue();
    });
  });

  describe('Interactions', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('activeLocations', mockLocations);
      fixture.detectChanges();
    });

    it('should update assignment when onSupervisorChange is called', () => {
      component.onSupervisorChange('loc-1', 'sup-2');
      expect(component.assignments()['loc-1']).toBe('sup-2');
    });

    it('should emit assignmentsChange when supervisor is changed', () => {
      const emitted: SupervisorAssignmentMap[] = [];
      fixture.componentInstance.assignmentsChange.subscribe((map) => emitted.push(map));

      component.onSupervisorChange('loc-1', 'sup-1');
      expect(emitted.length).toBeGreaterThan(0);
      expect(emitted[emitted.length - 1]['loc-1']).toBe('sup-1');
    });

    it('should not affect other assignments when one changes', () => {
      component.onSupervisorChange('loc-1', 'sup-1');
      component.onSupervisorChange('loc-2', 'sup-3');
      expect(component.assignments()['loc-1']).toBe('sup-1');
      expect(component.assignments()['loc-2']).toBe('sup-3');
    });

    it('getSupervisorLabel should return the supervisor fullName', () => {
      const label = component.getSupervisorLabel('sup-1');
      expect(label).toBe('María García López');
    });

    it('getSupervisorLabel should return empty string for unknown id', () => {
      const label = component.getSupervisorLabel('unknown-id');
      expect(label).toBe('');
    });
  });
});
