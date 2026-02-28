import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { LocationsStepComponent } from './locations-step.component';
import { LocationRow } from '../wizard.models';
import { MOCK_LOCATIONS } from '../mock-data';

describe('LocationsStepComponent', () => {
  let component: LocationsStepComponent;
  let fixture: ComponentFixture<LocationsStepComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LocationsStepComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();

    fixture = TestBed.createComponent(LocationsStepComponent);
    component = fixture.componentInstance;
  });

  describe('Rendering', () => {
    it('should create', () => {
      fixture.detectChanges();
      expect(component).toBeTruthy();
    });

    it('should initialize rows from MOCK_LOCATIONS when no initialRows given', () => {
      fixture.detectChanges();
      expect(component.rows().length).toBe(MOCK_LOCATIONS.length);
    });

    it('should initialize rows from initialRows input when provided', () => {
      const initial: LocationRow[] = [
        { locationId: 'x-1', locationName: 'Custom', building: 'B', isActive: false, scholarshipsAvailable: 5 },
      ];
      fixture.componentRef.setInput('initialRows', initial);
      fixture.detectChanges();
      expect(component.rows().length).toBe(1);
      expect(component.rows()[0].locationId).toBe('x-1');
    });

    it('should display all location names', () => {
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;
      MOCK_LOCATIONS.forEach((loc) => {
        expect(compiled.textContent).toContain(loc.name);
      });
    });

    it('should display summary with active count and total scholarships', () => {
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('Ubicaciones activas');
      expect(compiled.textContent).toContain('Total becas asignadas');
    });
  });

  describe('Computed signals', () => {
    it('should compute totalScholarships only from active rows', () => {
      fixture.detectChanges();

      component.rows.set([
        { locationId: 'a', locationName: 'A', building: 'B', isActive: true, scholarshipsAvailable: 3 },
        { locationId: 'b', locationName: 'B', building: 'C', isActive: false, scholarshipsAvailable: 5 },
        { locationId: 'c', locationName: 'C', building: 'D', isActive: true, scholarshipsAvailable: 2 },
      ]);

      expect(component.totalScholarships()).toBe(5);
    });

    it('should compute activeCount correctly', () => {
      fixture.detectChanges();

      component.rows.set([
        { locationId: 'a', locationName: 'A', building: 'B', isActive: true, scholarshipsAvailable: 2 },
        { locationId: 'b', locationName: 'B', building: 'C', isActive: false, scholarshipsAvailable: 2 },
      ]);

      expect(component.activeCount()).toBe(1);
    });

    it('should show warning message when no active locations', () => {
      fixture.detectChanges();
      component.rows.set([
        { locationId: 'a', locationName: 'A', building: 'B', isActive: false, scholarshipsAvailable: 0 },
      ]);
      fixture.detectChanges();

      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('Activa al menos una ubicación');
    });
  });

  describe('Interactions', () => {
    it('should update isActive when onToggle is called', () => {
      fixture.detectChanges();
      component.rows.set([
        { locationId: 'a', locationName: 'A', building: 'B', isActive: true, scholarshipsAvailable: 2 },
      ]);

      component.onToggle(0, false);
      expect(component.rows()[0].isActive).toBeFalse();
    });

    it('should update scholarshipsAvailable when onScholarshipsChange is called', () => {
      fixture.detectChanges();
      component.rows.set([
        { locationId: 'a', locationName: 'A', building: 'B', isActive: true, scholarshipsAvailable: 2 },
      ]);

      component.onScholarshipsChange(0, 7);
      expect(component.rows()[0].scholarshipsAvailable).toBe(7);
    });

    it('should default scholarshipsAvailable to 0 on null input', () => {
      fixture.detectChanges();
      component.rows.set([
        { locationId: 'a', locationName: 'A', building: 'B', isActive: true, scholarshipsAvailable: 2 },
      ]);

      component.onScholarshipsChange(0, null);
      expect(component.rows()[0].scholarshipsAvailable).toBe(0);
    });

    it('should emit rowsChange when toggling a location', () => {
      fixture.detectChanges();
      const emitted: LocationRow[][] = [];
      fixture.componentInstance.rowsChange.subscribe((rows) => emitted.push(rows));

      component.rows.set([
        { locationId: 'a', locationName: 'A', building: 'B', isActive: true, scholarshipsAvailable: 2 },
      ]);
      component.onToggle(0, false);

      expect(emitted.length).toBeGreaterThan(0);
      expect(emitted[emitted.length - 1][0].isActive).toBeFalse();
    });

    it('should emit rowsChange when scholarships change', () => {
      fixture.detectChanges();
      const emitted: LocationRow[][] = [];
      fixture.componentInstance.rowsChange.subscribe((rows) => emitted.push(rows));

      component.rows.set([
        { locationId: 'a', locationName: 'A', building: 'B', isActive: true, scholarshipsAvailable: 2 },
      ]);
      component.onScholarshipsChange(0, 5);

      expect(emitted[emitted.length - 1][0].scholarshipsAvailable).toBe(5);
    });
  });

  describe('Edge Cases', () => {
    it('should not mutate initialRows input', () => {
      const initial: LocationRow[] = [
        { locationId: 'x-1', locationName: 'A', building: 'B', isActive: true, scholarshipsAvailable: 2 },
      ];
      fixture.componentRef.setInput('initialRows', initial);
      fixture.detectChanges();

      component.onToggle(0, false);

      // Original array must be untouched
      expect(initial[0].isActive).toBeTrue();
    });
  });
});
