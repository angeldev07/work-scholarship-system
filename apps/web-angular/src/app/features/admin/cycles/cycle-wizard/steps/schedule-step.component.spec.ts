import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ScheduleStepComponent } from './schedule-step.component';
import { LocationRow, LocationScheduleMap, DAYS_OF_WEEK } from '../wizard.models';

const mockActiveLocations: LocationRow[] = [
  { locationId: 'loc-1', locationName: 'Sala Principal', building: 'Edificio A', isActive: true, scholarshipsAvailable: 3 },
  { locationId: 'loc-2', locationName: 'Hemeroteca', building: 'Edificio B', isActive: true, scholarshipsAvailable: 2 },
];

function makeTime(hours: number, minutes: number): Date {
  const d = new Date();
  d.setHours(hours, minutes, 0, 0);
  return d;
}

describe('ScheduleStepComponent', () => {
  let component: ScheduleStepComponent;
  let fixture: ComponentFixture<ScheduleStepComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ScheduleStepComponent],
      providers: [provideNoopAnimations()],
    }).compileComponents();

    fixture = TestBed.createComponent(ScheduleStepComponent);
    component = fixture.componentInstance;
  });

  describe('Rendering', () => {
    it('should create', () => {
      fixture.componentRef.setInput('activeLocations', mockActiveLocations);
      fixture.detectChanges();
      expect(component).toBeTruthy();
    });

    it('should show info message when no active locations', () => {
      fixture.componentRef.setInput('activeLocations', []);
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('No hay ubicaciones activas');
    });

    it('should display location names', () => {
      fixture.componentRef.setInput('activeLocations', mockActiveLocations);
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('Sala Principal');
      expect(compiled.textContent).toContain('Hemeroteca');
    });

    it('should show quick mode toggle', () => {
      fixture.componentRef.setInput('activeLocations', mockActiveLocations);
      fixture.detectChanges();
      const compiled = fixture.nativeElement as HTMLElement;
      expect(compiled.textContent).toContain('Modo rápido');
    });
  });

  describe('Initialization', () => {
    it('should start in quick mode by default', () => {
      fixture.componentRef.setInput('activeLocations', mockActiveLocations);
      fixture.detectChanges();
      expect(component.quickMode()).toBeTrue();
    });

    it('should create Mon-Fri slots for each location on init (quick mode)', () => {
      fixture.componentRef.setInput('activeLocations', mockActiveLocations);
      fixture.detectChanges();

      const slots1 = component.getSlotsFor('loc-1');
      expect(slots1.length).toBe(DAYS_OF_WEEK.length); // 5 days

      const slots2 = component.getSlotsFor('loc-2');
      expect(slots2.length).toBe(DAYS_OF_WEEK.length);
    });

    it('should use initialSchedules when provided', () => {
      const initial: LocationScheduleMap = {
        'loc-1': [{ dayOfWeek: 3, startTime: makeTime(8, 0), endTime: makeTime(12, 0), requiredScholars: 2 }],
        'loc-2': [{ dayOfWeek: 4, startTime: makeTime(14, 0), endTime: makeTime(18, 0), requiredScholars: 1 }],
      };
      fixture.componentRef.setInput('activeLocations', mockActiveLocations);
      fixture.componentRef.setInput('initialSchedules', initial);
      fixture.detectChanges();

      expect(component.getSlotsFor('loc-1').length).toBe(1);
      expect(component.getSlotsFor('loc-1')[0].dayOfWeek).toBe(3);
    });

    it('should not mutate initialSchedules when updating slots', () => {
      const initial: LocationScheduleMap = {
        'loc-1': [{ dayOfWeek: 1, startTime: makeTime(9, 0), endTime: makeTime(13, 0), requiredScholars: 1 }],
      };
      fixture.componentRef.setInput('activeLocations', [mockActiveLocations[0]]);
      fixture.componentRef.setInput('initialSchedules', initial);
      fixture.detectChanges();

      component.onSlotChange('loc-1', 0, 'requiredScholars', 5);

      expect(initial['loc-1'][0].requiredScholars).toBe(1); // Original untouched
    });
  });

  describe('Quick Mode', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('activeLocations', mockActiveLocations);
      fixture.detectChanges();
    });

    it('should toggle quickMode off', () => {
      component.onQuickModeToggle(false);
      expect(component.quickMode()).toBeFalse();
    });

    it('should re-apply template to all locations when quickMode is turned on', () => {
      component.onQuickModeToggle(false);
      // Manually clear slots
      component.scheduleMap.set({ 'loc-1': [], 'loc-2': [] });

      component.onQuickModeToggle(true);

      expect(component.getSlotsFor('loc-1').length).toBe(DAYS_OF_WEEK.length);
      expect(component.getSlotsFor('loc-2').length).toBe(DAYS_OF_WEEK.length);
    });

    it('should apply template change to all locations', () => {
      component.quickTemplate.set({ startTime: makeTime(7, 0), endTime: makeTime(11, 0), requiredScholars: 3 });
      component.onQuickTemplateChange();

      const slot = component.getSlotsFor('loc-1')[0];
      expect(slot.startTime?.getHours()).toBe(7);
      expect(slot.endTime?.getHours()).toBe(11);
      expect(slot.requiredScholars).toBe(3);
    });

    it('should not apply template when quickMode is off', () => {
      component.onQuickModeToggle(false);
      component.scheduleMap.set({
        'loc-1': [{ dayOfWeek: 1, startTime: makeTime(10, 0), endTime: makeTime(14, 0), requiredScholars: 2 }],
        'loc-2': [],
      });

      component.quickTemplate.set({ startTime: makeTime(6, 0), endTime: makeTime(10, 0), requiredScholars: 5 });
      component.onQuickTemplateChange();

      // Should NOT change because quickMode is off
      expect(component.getSlotsFor('loc-1')[0].startTime?.getHours()).toBe(10);
    });
  });

  describe('Slot Management', () => {
    beforeEach(() => {
      fixture.componentRef.setInput('activeLocations', mockActiveLocations);
      fixture.detectChanges();
      component.onQuickModeToggle(false);
      component.scheduleMap.set({ 'loc-1': [], 'loc-2': [] });
    });

    it('should add a slot for a location', () => {
      component.addSlot('loc-1');
      expect(component.getSlotsFor('loc-1').length).toBe(1);
    });

    it('should remove a slot from a location', () => {
      component.addSlot('loc-1');
      component.addSlot('loc-1');
      component.removeSlot('loc-1', 0);
      expect(component.getSlotsFor('loc-1').length).toBe(1);
    });

    it('should update slot field via onSlotChange', () => {
      component.addSlot('loc-1');
      component.onSlotChange('loc-1', 0, 'dayOfWeek', 3);
      expect(component.getSlotsFor('loc-1')[0].dayOfWeek).toBe(3);
    });

    it('should emit schedulesChange when slot is added', () => {
      const emitted: LocationScheduleMap[] = [];
      fixture.componentInstance.schedulesChange.subscribe((m) => emitted.push(m));

      component.addSlot('loc-1');
      expect(emitted.length).toBeGreaterThan(0);
      expect(emitted[emitted.length - 1]['loc-1'].length).toBe(1);
    });

    it('should emit schedulesChange when slot is removed', () => {
      component.addSlot('loc-1');

      const emitted: LocationScheduleMap[] = [];
      fixture.componentInstance.schedulesChange.subscribe((m) => emitted.push(m));

      component.removeSlot('loc-1', 0);
      expect(emitted[emitted.length - 1]['loc-1'].length).toBe(0);
    });
  });

  describe('Computed signals', () => {
    it('hasSchedules should be true when all active locations have valid slots', () => {
      fixture.componentRef.setInput('activeLocations', mockActiveLocations);
      fixture.detectChanges();

      // Default quick mode sets slots with times
      expect(component.hasSchedules()).toBeTrue();
    });

    it('hasSchedules should be false when a slot has null times', () => {
      fixture.componentRef.setInput('activeLocations', [mockActiveLocations[0]]);
      fixture.detectChanges();

      component.scheduleMap.set({
        'loc-1': [{ dayOfWeek: 1, startTime: null, endTime: null, requiredScholars: 1 }],
      });

      expect(component.hasSchedules()).toBeFalse();
    });

    it('hasSchedules should be false when a location has no slots', () => {
      fixture.componentRef.setInput('activeLocations', [mockActiveLocations[0]]);
      fixture.detectChanges();

      component.scheduleMap.set({ 'loc-1': [] });

      expect(component.hasSchedules()).toBeFalse();
    });
  });

  describe('Helper methods', () => {
    it('getDayLabel should return correct label', () => {
      fixture.componentRef.setInput('activeLocations', mockActiveLocations);
      fixture.detectChanges();

      expect(component.getDayLabel(1)).toBe('Lunes');
      expect(component.getDayLabel(5)).toBe('Viernes');
    });

    it('getDayLabel should return empty string for unknown day', () => {
      fixture.componentRef.setInput('activeLocations', mockActiveLocations);
      fixture.detectChanges();

      expect(component.getDayLabel(9)).toBe('');
    });

    it('getSlotsFor should return empty array for unknown locationId', () => {
      fixture.componentRef.setInput('activeLocations', mockActiveLocations);
      fixture.detectChanges();

      expect(component.getSlotsFor('unknown-loc')).toEqual([]);
    });
  });
});
