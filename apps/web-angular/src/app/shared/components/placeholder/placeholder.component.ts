import { ChangeDetectionStrategy, Component, InputSignal, input } from '@angular/core';

/**
 * Generic placeholder component used during development before real feature
 * implementations exist. Displays the page title and a "Coming Soon" note.
 */
@Component({
  selector: 'app-placeholder',
  standalone: true,
  template: `
    <div class="placeholder-page">
      <div class="placeholder-page__icon" aria-hidden="true">
        <i class="pi pi-wrench"></i>
      </div>
      <h1 class="placeholder-page__title">{{ title() }}</h1>
      <p class="placeholder-page__subtitle">Próximamente</p>
      <p class="placeholder-page__note">
        Esta sección está en construcción. Se implementará en una próxima fase.
      </p>
    </div>
  `,
  styles: [`
    .placeholder-page {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: 400px;
      text-align: center;
      padding: 2rem;
      gap: 0.75rem;

      &__icon {
        width: 64px;
        height: 64px;
        border-radius: 16px;
        background: #eef2ff;
        display: flex;
        align-items: center;
        justify-content: center;
        margin-bottom: 0.5rem;

        .pi {
          font-size: 1.5rem;
          color: #6366f1;
        }
      }

      &__title {
        font-size: 1.5rem;
        font-weight: 600;
        color: #1e293b;
        margin: 0;
      }

      &__subtitle {
        font-size: 0.875rem;
        font-weight: 500;
        color: #6366f1;
        background: #eef2ff;
        padding: 0.25rem 0.75rem;
        border-radius: 9999px;
        margin: 0;
      }

      &__note {
        font-size: 0.875rem;
        color: #94a3b8;
        max-width: 400px;
        margin: 0;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlaceholderComponent {
  readonly title: InputSignal<string> = input<string>('En Construcción');
}
