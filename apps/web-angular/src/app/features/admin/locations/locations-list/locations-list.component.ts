import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-locations-list',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Ubicaciones" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LocationsListComponent {}
