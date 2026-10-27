/* tslint:disable:no-unused-variable */

import { APP_BASE_HREF } from '@angular/common';
import { TestBed, waitForAsync } from '@angular/core/testing';
import { API_BASE_URL } from '@shared/service-proxies/service-proxies';
import { AppComponent } from './app.component';
import { LOCALE_ID, provideZonelessChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { SERVICE_PROXY_PROVIDERS } from '@shared/service-proxies/service-proxy.providers';

export function getRemoteServiceBaseUrl(): string {
    return 'https://localhost:44301';
}

describe('App: HCMv3', () => {
    // Remove freezeui loading animation
    (window as any).FreezeUI = function () {};
    (window as any).UnFreezeUI = function () {};

    beforeEach(waitForAsync(() => {
        TestBed.configureTestingModule({
            imports: [AppComponent],
            providers: [
                provideZonelessChangeDetection(),
                provideRouter([]),
                provideHttpClient(),
                provideAnimations(),
                ...SERVICE_PROXY_PROVIDERS,
                { provide: API_BASE_URL, useValue: getRemoteServiceBaseUrl() },
                { provide: APP_BASE_HREF, useValue: '/' },
                { provide: LOCALE_ID, useValue: 'en' },
            ],
        }).compileComponents();
    }));

    it('should create the app', waitForAsync(() => {
        const fixture = TestBed.createComponent(AppComponent);
        const app = fixture.debugElement.componentInstance;
        expect(app).toBeTruthy();
    }));
});
