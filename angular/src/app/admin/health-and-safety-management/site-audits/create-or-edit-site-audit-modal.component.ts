import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSiteAuditDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllSimpleLookupsInput,
    SafetyAreaAssetsServiceProxy,
    SimpleLookupDto,
    SiteAuditsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditSiteAuditModal',
    templateUrl: './create-or-edit-site-audit-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditSiteAuditModalComponent extends AppComponentBase {
    private _service = inject(SiteAuditsServiceProxy);
    private _safetyAreaAssetsService = inject(SafetyAreaAssetsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditSiteAuditDto>(new CreateOrEditSiteAuditDto());

    readonly locations = signal<SimpleLookupDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditSiteAuditDto());
            this.modal().show();
        } else {
            this._service.getSiteAuditForEdit(id).subscribe((result) => {
                this.record.set(result.siteAudit);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._safetyAreaAssetsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.locations.set(result.items));

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));
    }

    onShown(): void {
        document.getElementById('SiteAudit_AuditDate')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    updateField(field: keyof CreateOrEditSiteAuditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditSiteAuditDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'auditDate' | 'reassessmentDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    getTimeValue(field: keyof CreateOrEditSiteAuditDto): string | undefined {
        const value = (this.record() as any)[field] as DateTime | undefined;
        return value?.toFormat('HH:mm:ss') ?? undefined;
    }

    updateTimeField(field: keyof CreateOrEditSiteAuditDto, value: string): void {
        if (!value) {
            this.updateField(field, undefined as any);
            return;
        }
        const [hour, minute, second] = value.split(':').map((part) => Number(part));
        const current = (this.record() as any)[field] as DateTime | undefined;
        const base = current ?? DateTime.now();
        this.updateField(field, base.set({ hour, minute, second: second || 0, millisecond: 0 }));
    }
}
