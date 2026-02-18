import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div class="error-page" role="main" aria-labelledby="error-heading">
      <div class="error-page__content">
        <p class="error-page__code" aria-hidden="true">404</p>
        <h1 class="error-page__title" id="error-heading">Página no encontrada</h1>
        <p class="error-page__text">La página que buscas no existe o fue movida.</p>
        <a routerLink="/" class="error-page__link">Volver al inicio</a>
      </div>
    </div>
  `,
  styles: [`
    .error-page {
      display: flex; align-items: center; justify-content: center;
      min-height: 100dvh; padding: 2rem; background: #f8fafc;
    }
    .error-page__content { text-align: center; display: flex; flex-direction: column; gap: 1rem; align-items: center; }
    .error-page__code { font-size: 8rem; font-weight: 800; color: #e2e8f0; line-height: 1; }
    .error-page__title { font-size: 1.875rem; font-weight: 700; color: #1e293b; }
    .error-page__text { color: #64748b; }
    .error-page__link {
      display: inline-flex; padding: 0.75rem 1.5rem; background: #4f46e5; color: white;
      border-radius: 0.75rem; font-weight: 600; text-decoration: none; transition: all 200ms;
      &:hover { background: #4338ca; text-decoration: none; color: white; }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundComponent {}
