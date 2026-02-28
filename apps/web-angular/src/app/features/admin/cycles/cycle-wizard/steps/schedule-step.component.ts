import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  OnInit,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { InputNumberModule } from 'primeng/inputnumber';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { MessageModule } from 'primeng/message';
import { LocationRow, LocationScheduleMap, ScheduleSlotRow, DAYS_OF_WEEK } from '../wizard.models';

/** Quick-mode template shared across all locations (Mon-Fri same hours). */
interface QuickTemplate {
  startTime: Date | null;
  endTime: Date | null;
  requiredScholars: number;
}

@Component({
  selector: 'app-schedule-step',
  standalone: true,
  imports: [
    FormsModule,
    CardModule,
    ButtonModule,
    DatePickerModule,
    InputNumberModule,
    ToggleSwitchModule,
    MessageModule,
  ],
  templateUrl: './schedule-step.component.html',
  styleUrl: './schedule-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScheduleStepComponent implements OnInit {
  /** Active locations from the locations step. */
  readonly activeLocations = input<LocationRow[]>([]);

  /** Initial schedule map (locationId → slots). */
  readonly initialSchedules = input<LocationScheduleMap>({});

  /** Emits the updated schedule map when anything changes. */
  readonly schedulesChange = output<LocationScheduleMap>();

  readonly days = DAYS_OF_WEEK;

  /** Whether to use quick mode (apply Mon-Fri template to all locations). */
  readonly quickMode = signal(true);

  readonly quickTemplate = signal<QuickTemplate>({
    startTime: this.buildTime(9, 0),
    endTime: this.buildTime(13, 0),
    requiredScholars: 1,
  });

  readonly scheduleMap = signal<LocationScheduleMap>({});

  readonly hasSchedules = computed(() => {
    const map = this.scheduleMap();
    return this.activeLocations().every((loc) => {
      const slots = map[loc.locationId];
      return slots && slots.length > 0 && slots.every((s) => s.startTime && s.endTime);
    });
  });

  ngOnInit(): void {
    const initial = this.initialSchedules();
    if (Object.keys(initial).length > 0) {
      this.scheduleMap.set(this.deepClone(initial));
    } else {
      this.applyQuickTemplate();
    }
    this.emit();
  }

  onQuickModeToggle(enabled: boolean): void {
    this.quickMode.set(enabled);
    if (enabled) {
      this.applyQuickTemplate();
      this.emit();
    }
  }

  onQuickTemplateChange(): void {
    if (this.quickMode()) {
      this.applyQuickTemplate();
      this.emit();
    }
  }

  onQuickStartTimeChange(time: Date | null): void {
    this.quickTemplate.update((t) => ({ ...t, startTime: time }));
    this.onQuickTemplateChange();
  }

  onQuickEndTimeChange(time: Date | null): void {
    this.quickTemplate.update((t) => ({ ...t, endTime: time }));
    this.onQuickTemplateChange();
  }

  onQuickScholarsChange(value: number | null): void {
    this.quickTemplate.update((t) => ({ ...t, requiredScholars: value ?? 1 }));
    this.onQuickTemplateChange();
  }

  onSlotChange(locationId: string, index: number, field: keyof ScheduleSlotRow, value: unknown): void {
    this.scheduleMap.update((map) => {
      const slots = [...(map[locationId] ?? [])];
      slots[index] = { ...slots[index], [field]: value };
      return { ...map, [locationId]: slots };
    });
    this.emit();
  }

  addSlot(locationId: string): void {
    this.scheduleMap.update((map) => {
      const slots = [...(map[locationId] ?? [])];
      slots.push({
        dayOfWeek: 1,
        startTime: null,
        endTime: null,
        requiredScholars: 1,
      });
      return { ...map, [locationId]: slots };
    });
    this.emit();
  }

  removeSlot(locationId: string, index: number): void {
    this.scheduleMap.update((map) => {
      const slots = [...(map[locationId] ?? [])];
      slots.splice(index, 1);
      return { ...map, [locationId]: slots };
    });
    this.emit();
  }

  getSlotsFor(locationId: string): ScheduleSlotRow[] {
    return this.scheduleMap()[locationId] ?? [];
  }

  getDayLabel(dayOfWeek: number): string {
    return this.days.find((d) => d.value === dayOfWeek)?.label ?? '';
  }

  private applyQuickTemplate(): void {
    const tpl = this.quickTemplate();
    const map: LocationScheduleMap = {};
    this.activeLocations().forEach((loc) => {
      map[loc.locationId] = DAYS_OF_WEEK.map((d) => ({
        dayOfWeek: d.value,
        startTime: tpl.startTime ? new Date(tpl.startTime) : null,
        endTime: tpl.endTime ? new Date(tpl.endTime) : null,
        requiredScholars: tpl.requiredScholars,
      }));
    });
    this.scheduleMap.set(map);
  }

  private buildTime(hours: number, minutes: number): Date {
    const d = new Date();
    d.setHours(hours, minutes, 0, 0);
    return d;
  }

  private deepClone(map: LocationScheduleMap): LocationScheduleMap {
    const result: LocationScheduleMap = {};
    for (const [key, slots] of Object.entries(map)) {
      result[key] = slots.map((s) => ({
        ...s,
        startTime: s.startTime ? new Date(s.startTime) : null,
        endTime: s.endTime ? new Date(s.endTime) : null,
      }));
    }
    return result;
  }

  private emit(): void {
    this.schedulesChange.emit(this.scheduleMap());
  }
}
