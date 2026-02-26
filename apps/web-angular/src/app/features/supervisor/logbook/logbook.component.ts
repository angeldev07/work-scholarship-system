import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-logbook',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Bitácora" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LogbookComponent {}
