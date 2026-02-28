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
import { SelectModule } from 'primeng/select';
import { MessageModule } from 'primeng/message';
import { MOCK_SUPERVISORS, MockSupervisor } from '../mock-data';
import { LocationRow, SupervisorAssignmentMap } from '../wizard.models';

@Component({
  selector: 'app-supervisors-step',
  standalone: true,
  imports: [FormsModule, CardModule, SelectModule, MessageModule],
  templateUrl: './supervisors-step.component.html',
  styleUrl: './supervisors-step.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupervisorsStepComponent implements OnInit {
  /** Active locations from the previous step. */
  readonly activeLocations = input<LocationRow[]>([]);

  /** Current supervisor assignments (locationId → supervisorId). */
  readonly initialAssignments = input<SupervisorAssignmentMap>({});

  /** Emits the updated assignment map when anything changes. */
  readonly assignmentsChange = output<SupervisorAssignmentMap>();

  readonly supervisors: MockSupervisor[] = MOCK_SUPERVISORS;

  readonly assignments = signal<SupervisorAssignmentMap>({});

  readonly allAssigned = computed(() => {
    const locs = this.activeLocations();
    if (locs.length === 0) return true;
    const map = this.assignments();
    return locs.every((l) => !!map[l.locationId]);
  });

  ngOnInit(): void {
    const initial = this.initialAssignments();
    if (Object.keys(initial).length > 0) {
      this.assignments.set({ ...initial });
    } else {
      // Pre-assign first supervisor to all locations as a sensible default
      const map: SupervisorAssignmentMap = {};
      this.activeLocations().forEach((loc) => {
        map[loc.locationId] = '';
      });
      this.assignments.set(map);
    }
    this.emit();
  }

  onSupervisorChange(locationId: string, supervisorId: string): void {
    this.assignments.update((map) => ({ ...map, [locationId]: supervisorId }));
    this.emit();
  }

  getSupervisorLabel(supervisorId: string): string {
    return this.supervisors.find((s) => s.id === supervisorId)?.fullName ?? '';
  }

  private emit(): void {
    this.assignmentsChange.emit(this.assignments());
  }
}
