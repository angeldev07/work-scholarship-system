import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-scholars-list',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Mis Becas" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarsListComponent {}
