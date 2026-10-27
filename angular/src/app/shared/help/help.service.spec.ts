import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { PermissionCheckerService } from 'abp-ng2-module';
import { HelpMetadataServiceProxy } from '@shared/service-proxies/service-proxies';
import { HelpService } from './help.service';

/**
 * What these cover is the one thing that cannot be checked by reading the compiled help: whether
 * pressing F1 inside an open dialog finds the page that documents that dialog.
 *
 * Dialogs are not annotated -- nothing marks a create/edit form as documented -- so the service has
 * to recognise one from the ASP.NET Zero ids its inputs already carry. That recognition is worth a
 * test because it fails silently: a dialog it cannot name simply answers F1 with the screen behind
 * it, or with nothing, which is exactly what the feature looked like before it existed.
 */
describe('HelpService (F1 inside a dialog)', () => {
    let service: HelpService;
    let http: HttpTestingController;
    const added: HTMLElement[] = [];

    const INDEX = {
        dom: {
            Employee_Code: ['pa.employee.personal-info', 'pa.employee.edit'],
            Employee_FirstNameEn: ['pa.employee.edit'],
            Employee_LastNameEn: ['pa.employee.edit'],
            Nationality_Code: ['gmi.nationality.edit'],
        },
    };

    /** A dialog as Bootstrap leaves one when it is open, with the ids the real form carries. */
    function openDialog(ids: string[], zIndex = 1052): HTMLElement {
        const modal = document.createElement('div');
        modal.className = 'modal fade show';
        modal.style.zIndex = String(zIndex);
        for (const id of ids) {
            const input = document.createElement('input');
            input.id = id;
            modal.appendChild(input);
        }
        document.body.appendChild(modal);
        added.push(modal);
        return modal;
    }

    function pressF1(over: Element): void {
        over.dispatchEvent(new MouseEvent('mouseover', { bubbles: true }));
        document.dispatchEvent(new KeyboardEvent('keydown', { key: 'F1', bubbles: true }));
    }

    /** The service opens across two awaited fetches; let both land before asserting. */
    async function settle(): Promise<void> {
        await new Promise((resolve) => setTimeout(resolve));
    }

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [
                HelpService,
                provideHttpClient(),
                provideHttpClientTesting(),
                { provide: PermissionCheckerService, useValue: { isGranted: () => false } },
                { provide: HelpMetadataServiceProxy, useValue: {} },
            ],
        });
        service = TestBed.inject(HelpService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => {
        for (const el of added.splice(0)) el.remove();
        service.close();
        http.verify();
    });

    it('opens the page that documents the dialog, with no page registered behind it', async () => {
        const dialog = openDialog(['Employee_Code', 'Employee_FirstNameEn', 'Employee_LastNameEn']);

        pressF1(dialog.querySelector('#Employee_FirstNameEn')!);
        await settle();
        http.expectOne('assets/help/index.json').flush(INDEX);
        await settle();

        const request = http.expectOne('assets/help/en/pa.employee.edit.json');
        request.flush({
            helpId: 'pa.employee.edit',
            lang: 'en',
            title: 'Employee',
            entity: 'HCMv3.PersonnelAdministration.Employee',
            overviewHtml: '',
            aliasIndex: { 'dom:Employee_FirstNameEn': 'Employee.FirstNameEn' },
            fields: [],
        });
        await settle();

        expect(service.isOpen()).toBeTrue();
        expect(service.content()?.helpId).toBe('pa.employee.edit');
        expect(service.activeFieldId()).toBe('Employee.FirstNameEn');
    });

    it('prefers the page that matches most of the dialog, not the first that matches one field', async () => {
        // Employee_Code alone belongs to both the read view and the edit form; the rest decide it.
        const dialog = openDialog(['Employee_Code', 'Employee_FirstNameEn', 'Employee_LastNameEn']);

        pressF1(dialog);
        await settle();
        http.expectOne('assets/help/index.json').flush(INDEX);
        await settle();

        http.expectOne('assets/help/en/pa.employee.edit.json').flush({
            helpId: 'pa.employee.edit',
            lang: 'en',
            title: 'Employee',
            entity: 'HCMv3.PersonnelAdministration.Employee',
            overviewHtml: '',
            aliasIndex: {},
            fields: [],
        });
        await settle();

        expect(service.content()?.helpId).toBe('pa.employee.edit');
    });

    it('answers for the screen behind when the dialog is not documented', async () => {
        const host = document.createElement('div');
        host.innerHTML = '<span data-help-loc="Code">x</span>';
        document.body.appendChild(host);
        added.push(host);
        service.registerPage('pa.employee.personal-info', host);

        const dialog = openDialog(['SomeUndocumented_Thing']);

        pressF1(dialog);
        await settle();
        http.expectOne('assets/help/index.json').flush(INDEX);
        await settle();

        http.expectOne('assets/help/en/pa.employee.personal-info.json').flush({
            helpId: 'pa.employee.personal-info',
            lang: 'en',
            title: 'Personal Information',
            entity: 'HCMv3.PersonnelAdministration.Employee',
            overviewHtml: '',
            aliasIndex: {},
            fields: [],
        });
        await settle();

        expect(service.content()?.helpId).toBe('pa.employee.personal-info');
    });

    it('goes back to the screen once the dialog has closed', async () => {
        const host = document.createElement('div');
        document.body.appendChild(host);
        added.push(host);
        service.registerPage('pa.employee.personal-info', host);

        const dialog = openDialog(['Employee_FirstNameEn']);
        pressF1(dialog);
        await settle();
        http.expectOne('assets/help/index.json').flush(INDEX);
        await settle();
        http.expectOne('assets/help/en/pa.employee.edit.json').flush({
            helpId: 'pa.employee.edit',
            lang: 'en',
            title: 'Employee',
            entity: 'HCMv3.PersonnelAdministration.Employee',
            overviewHtml: '',
            aliasIndex: {},
            fields: [],
        });
        await settle();
        service.close();

        dialog.remove();
        pressF1(host);
        await settle();

        http.expectOne('assets/help/en/pa.employee.personal-info.json').flush({
            helpId: 'pa.employee.personal-info',
            lang: 'en',
            title: 'Personal Information',
            entity: 'HCMv3.PersonnelAdministration.Employee',
            overviewHtml: '',
            aliasIndex: {},
            fields: [],
        });
        await settle();

        expect(service.content()?.helpId).toBe('pa.employee.personal-info');
    });
});
