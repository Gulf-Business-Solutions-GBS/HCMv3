import { TestBed } from '@angular/core/testing';
import { EssSessionServiceProxy } from '@shared/service-proxies/service-proxies';
import { PermissionCheckerService } from 'abp-ng2-module';
import { Observable, of, throwError } from 'rxjs';
import { EssMenuCatalogService } from './ess-menu-catalog.service';

/**
 * The menu is not a security boundary -- every self-service endpoint is permission-checked and
 * employee-scoped on the server -- so what these cover is honesty rather than access: a menu entry for a
 * transaction the tenant has switched off is a link to a screen that no longer applies, and reads as a
 * fault rather than a setting.
 *
 * <p>The two behaviours that are easy to get backwards are covered deliberately: what happens before the
 * answer arrives, and what happens when it never does.</p>
 */
describe('EssMenuCatalogService', () => {
    let calls: number;
    let response: () => Observable<any>;

    function build(hasSelfService = true): EssMenuCatalogService {
        calls = 0;
        response = () => of({ availableTransactionTypes: [{ code: 'EmployeeAsset' }, { code: 'EmployeeLoan' }] });

        TestBed.resetTestingModule();
        TestBed.configureTestingModule({
            providers: [
                EssMenuCatalogService,
                {
                    provide: EssSessionServiceProxy,
                    useValue: {
                        getEssContext: () => {
                            calls++;
                            return response();
                        },
                    },
                },
                {
                    provide: PermissionCheckerService,
                    useValue: { isGranted: (name: string) => hasSelfService && name === 'Ess' },
                },
            ],
        });

        return TestBed.inject(EssMenuCatalogService);
    }

    it('shows a transaction the tenant has switched on', () => {
        const service = build();
        service.ensureLoaded();

        expect(service.isEnabled('EmployeeAsset')).toBe(true);
    });

    it('hides a transaction the tenant has switched off', () => {
        const service = build();
        service.ensureLoaded();

        expect(service.isEnabled('EmployeeTicket')).toBe(false);
    });

    // Showing an entry that should have been hidden is the milder mistake. The other way round the menu
    // would lose items on every page load and put them back a moment later, which looks broken.
    it('hides nothing before the answer has arrived', () => {
        const service = build();

        expect(service.isEnabled('EmployeeTicket')).toBe(true);
    });

    // A failed lookup must not empty somebody's menu. Without this the product would be worse off than
    // before the feature existed.
    it('hides nothing when the lookup fails', () => {
        const service = build();
        response = () => throwError(() => new Error('offline'));

        service.ensureLoaded();

        expect(service.isEnabled('EmployeeTicket')).toBe(true);
        expect(service.isEnabled('EmployeeAsset')).toBe(true);
    });

    // getMenu() runs on every menu render, so this is called constantly.
    it('asks the server once however often the menu is built', () => {
        const service = build();

        service.ensureLoaded();
        service.ensureLoaded();
        service.ensureLoaded();

        expect(calls).toBe(1);
    });

    // The server refuses the call and the HTTP layer shows that refusal as a dialog, so the host saw
    // "Required permissions are not granted" on every sign-in.
    it('does not ask the server for somebody without self service', () => {
        const service = build(false);

        service.ensureLoaded();
        service.ensureLoaded();

        expect(calls).toBe(0);
    });

    it('asks again after a reset', () => {
        const service = build();

        service.ensureLoaded();
        service.reset();
        service.ensureLoaded();

        expect(calls).toBe(2);
    });

    it('treats a context with no transaction types as everything switched off', () => {
        const service = build();
        response = () => of({ availableTransactionTypes: undefined });

        service.ensureLoaded();

        expect(service.isEnabled('EmployeeAsset')).toBe(false);
    });
});
