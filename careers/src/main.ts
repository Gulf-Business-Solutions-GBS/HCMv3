import { provideZonelessChangeDetection } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter, withInMemoryScrolling } from '@angular/router';

import { AppComponent } from './app/app.component';
import { APP_ROUTES } from './app/app.routes';
import { AppConsts } from './app/core/app-consts';
import { authInterceptor } from './app/core/auth.interceptor';
import { CareersPreBootstrap } from './app/core/careers-pre-bootstrap';
import { API_BASE_URL } from './app/service-proxies/service-proxies';

/**
 * `CareersPreBootstrap.run()` MUST complete before `bootstrapApplication`: it resolves
 * `AppConsts.remoteServiceBaseUrl` (which is then handed to the NSwag proxies through `API_BASE_URL`) and
 * downloads the localization dictionary the `localize` pipe reads synchronously.
 */
CareersPreBootstrap.run().then(() => {
    bootstrapApplication(AppComponent, {
        providers: [
            // The admin SPA is zoneless (there is no zone.js in its dependency tree); the portal matches it,
            // which is also why every piece of component state here is a signal.
            provideZonelessChangeDetection(),
            provideRouter(
                APP_ROUTES,
                withInMemoryScrolling({ scrollPositionRestoration: 'top', anchorScrolling: 'enabled' })
            ),
            provideHttpClient(withInterceptors([authInterceptor])),
            { provide: API_BASE_URL, useValue: AppConsts.remoteServiceBaseUrl },
        ],
    }).catch((error) => console.error(error));
});
