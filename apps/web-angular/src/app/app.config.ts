import {
  APP_INITIALIZER,
  ApplicationConfig,
  inject,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter, withComponentInputBinding, withViewTransitions } from '@angular/router';
import { provideHttpClient, withInterceptors, withFetch } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import Aura from '@primeuix/themes/aura';
import { MessageService, ConfirmationService } from 'primeng/api';
import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { AuthService } from './core/services/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withComponentInputBinding(), withViewTransitions()),
    provideHttpClient(withInterceptors([authInterceptor]), withFetch()),
    provideAnimationsAsync(),

    // Restore session from refresh token cookie on app startup
    {
      provide: APP_INITIALIZER,
      useFactory: () => {
        const authService = inject(AuthService);
        return () => authService.initializeAuth();
      },
      multi: true,
    },

    // PrimeNG v20 configuration with Aura theme preset
    providePrimeNG({
      theme: {
        preset: Aura,
        options: {
          prefix: 'p',
          darkModeSelector: '.dark',
          cssLayer: false,
        },
      },
    }),

    // PrimeNG global services
    MessageService,
    ConfirmationService,
  ],
};
