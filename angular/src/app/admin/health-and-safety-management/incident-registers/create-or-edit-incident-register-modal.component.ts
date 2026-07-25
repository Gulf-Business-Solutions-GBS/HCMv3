import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditIncidentRegisterDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllSimpleLookupsInput,
    IncidentNaturesServiceProxy,
    IncidentRegistersServiceProxy,
    IncidentSeveritiesServiceProxy,
    IncidentStatusesServiceProxy,
    IncidentTypesServiceProxy,
    SafetyAreaAssetsServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditIncidentRegisterModal',
    templateUrl: './create-or-edit-incident-register-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditIncidentRegisterModalComponent extends AppComponentBase {
    private _service = inject(IncidentRegistersServiceProxy);
    private _incidentTypesService = inject(IncidentTypesServiceProxy);
    private _incidentNaturesService = inject(IncidentNaturesServiceProxy);
    private _incidentSeveritiesService = inject(IncidentSeveritiesServiceProxy);
    private _incidentStatusesService = inject(IncidentStatusesServiceProxy);
    private _safetyAreaAssetsService = inject(SafetyAreaAssetsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditIncidentRegisterDto>(new CreateOrEditIncidentRegisterDto());

    readonly incidentTypes = signal<SimpleLookupDto[]>([]);
    readonly natures = signal<SimpleLookupDto[]>([]);
    readonly severities = signal<SimpleLookupDto[]>([]);
    readonly statuses = signal<SimpleLookupDto[]>([]);
    readonly locations = signal<SimpleLookupDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditIncidentRegisterDto());
            this.modal().show();
        } else {
            this._service.getIncidentRegisterForEdit(id).subscribe((result) => {
                this.record.set(result.incidentRegister);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        const lookupInput = () => new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined });

        this._incidentTypesService.getAll(lookupInput()).subscribe((result) => this.incidentTypes.set(result.items));
        this._incidentNaturesService.getAll(lookupInput()).subscribe((result) => this.natures.set(result.items));
        this._incidentSeveritiesService.getAll(lookupInput()).subscribe((result) => this.severities.set(result.items));
        this._incidentStatusesService.getAll(lookupInput()).subscribe((result) => this.statuses.set(result.items));
        this._safetyAreaAssetsService.getAll(lookupInput()).subscribe((result) => this.locations.set(result.items));

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));
    }

    onShown(): void {
        document.getElementById('IncidentRegister_IncidentDate')?.focus();
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

    updateField(field: keyof CreateOrEditIncidentRegisterDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditIncidentRegisterDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'incidentDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    getTimeValue(field: keyof CreateOrEditIncidentRegisterDto): string | undefined {
        const value = (this.record() as any)[field] as DateTime | undefined;
        return value?.toFormat('HH:mm:ss') ?? undefined;
    }

    updateTimeField(field: keyof CreateOrEditIncidentRegisterDto, value: string): void {
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
