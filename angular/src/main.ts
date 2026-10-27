import {
    enableProdMode,
    Injector,
    APP_INITIALIZER,
    LOCALE_ID,
    DEFAULT_CURRENCY_CODE,
    importProvidersFrom,
    provideZonelessChangeDetection,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClientModule, HttpClientJsonpModule } from '@angular/common/http';
import { environment } from './environments/environment';
import { getRemoteServiceBaseUrl, appInitializerFactory, getCurrencyCode, getCurrentLanguage } from './root.module';
import { API_BASE_URL } from '@shared/service-proxies/service-proxies';
import { PlatformLocation } from '@angular/common';
import { NgxSpinnerModule } from 'ngx-spinner';
import { BrowserModule, bootstrapApplication } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
import { SERVICE_PROXY_PROVIDERS } from '@shared/service-proxies/service-proxy.providers';
import { withInterceptorsFromDi, provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { routes } from './root.routes';
import { RootComponent } from './root.component';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import Lara from '@primeuix/themes/lara';
import { ModalModule } from 'ngx-bootstrap/modal';
import { TabsModule } from 'ngx-bootstrap/tabs';
import { PopoverModule } from 'ngx-bootstrap/popover';
import { IMaskModule } from 'angular-imask';
import { NgScrollbarModule } from 'ngx-scrollbar';
import { ImpersonationService } from '@app/admin/users/impersonation.service';
import { LinkedAccountService } from '@app/shared/layout/linked-account.service';
import { UserNotificationHelper } from '@app/shared/layout/notifications/UserNotificationHelper';
import { ChatSignalrService } from '@app/shared/layout/chat/chat-signalr.service';
import { RouterSpinnerService } from '@app/router-spinner.service';

if (environment.production) {
    enableProdMode();
}

const bootstrap = () =>
    bootstrapApplication(RootComponent, {
        providers: [
            provideZonelessChangeDetection(),
            importProvidersFrom(
                BrowserModule,
                CommonModule,
                FormsModule,
                HttpClientModule,
                HttpClientJsonpModule,
                NgxSpinnerModule,
                ModalModule.forRoot(),
                TabsModule.forRoot(),
                PopoverModule.forRoot(),
                IMaskModule,
                NgScrollbarModule
            ),
            provideRouter(routes),
            ...SERVICE_PROXY_PROVIDERS,
            ImpersonationService,
            LinkedAccountService,
            UserNotificationHelper,
            ChatSignalrService,
            RouterSpinnerService,
            { provide: API_BASE_URL, useFactory: getRemoteServiceBaseUrl },
            {
                provide: APP_INITIALIZER,
                useFactory: appInitializerFactory,
                deps: [Injector, PlatformLocation],
                multi: true,
            },
            {
                provide: APP_INITIALIZER,
                useFactory: appModuleInitializerFactory,
                multi: true,
            },
            {
                provide: LOCALE_ID,
                useFactory: getCurrentLanguage,
            },
            {
                provide: DEFAULT_CURRENCY_CODE,
                useFactory: getCurrencyCode,
                deps: [Injector],
            },
            provideAnimations(),
            provideHttpClient(withInterceptorsFromDi()),
            provideAnimationsAsync(),
            providePrimeNG({
                theme: {
                    preset: Lara,
                    options: {
                        darkModeSelector: '[data-bs-theme="dark"]',
                    },
                },
            }),
        ],
    });

function appModuleInitializerFactory() {
    return () => {
        const url = new URL(location.href);
        const params = url.searchParams;

        if (params.has('t')) {
            params.delete('t');
            window.location.href = url.toString();
        }
    };
}

bootstrap();
