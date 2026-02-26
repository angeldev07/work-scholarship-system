import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-badges',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Escarapelas" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BadgesComponent {}
