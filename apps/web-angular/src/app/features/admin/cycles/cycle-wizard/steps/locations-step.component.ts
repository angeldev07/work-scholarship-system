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
import { InputNumberModule } from 'primeng/inputnumber';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { MessageModule } from 'primeng/message';
import { MOCK_LOCATIONS } from '../mock-data';
import { LocationRow } from '../wizard.models';

@Component({
  selector: 'app-locations-step',
  standalone: true,
  imports: [
    FormsModule,
    CardModule,
    ButtonModule,
    InputNumberModule,
    ToggleSwitchModule,
    MessageModule,
  ],
  templateUrl: './locations-step.component.html',
  styleUrl: './locations-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LocationsStepComponent implements OnInit {
  /** Initial location rows pre-populated from a previous configuration. */
  readonly initialRows = input<LocationRow[]>([]);

  /** Emits the updated list of location rows when anything changes. */
  readonly rowsChange = output<LocationRow[]>();

  readonly rows = signal<LocationRow[]>([]);

  readonly totalScholarships = computed(() =>
    this.rows()
      .filter((r) => r.isActive)
      .reduce((sum, r) => sum + (r.scholarshipsAvailable || 0), 0),
  );

  readonly activeCount = computed(() => this.rows().filter((r) => r.isActive).length);

  ngOnInit(): void {
    const initial = this.initialRows();
    if (initial.length > 0) {
      this.rows.set(initial.map((r) => ({ ...r })));
    } else {
      this.rows.set(
        MOCK_LOCATIONS.map((loc) => ({
          locationId: loc.id,
          locationName: loc.name,
          building: loc.building,
          isActive: true,
          scholarshipsAvailable: 2,
        })),
      );
    }
    this.emit();
  }

  onToggle(index: number, value: boolean): void {
    this.rows.update((rows) => {
      const updated = [...rows];
      updated[index] = { ...updated[index], isActive: value };
      return updated;
    });
    this.emit();
  }

  onScholarshipsChange(index: number, value: number | null): void {
    this.rows.update((rows) => {
      const updated = [...rows];
      updated[index] = { ...updated[index], scholarshipsAvailable: value ?? 0 };
      return updated;
    });
    this.emit();
  }

  private emit(): void {
    this.rowsChange.emit(this.rows());
  }
}
