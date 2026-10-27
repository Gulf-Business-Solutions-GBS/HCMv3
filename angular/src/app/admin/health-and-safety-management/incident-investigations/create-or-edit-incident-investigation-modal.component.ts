import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditIncidentInvestigationDto,
    GetAllIncidentRegistersInput,
    GetAllSimpleLookupsInput,
    IncidentInvestigationsServiceProxy,
    IncidentRegisterDto,
    IncidentRegistersServiceProxy,
    InvestigationRootCausesServiceProxy,
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
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditIncidentInvestigationModal',
    templateUrl: './create-or-edit-incident-investigation-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditIncidentInvestigationModalComponent extends AppComponentBase {
    private _service = inject(IncidentInvestigationsServiceProxy);
    private _incidentRegistersService = inject(IncidentRegistersServiceProxy);
    private _rootCausesService = inject(InvestigationRootCausesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditIncidentInvestigationDto>(new CreateOrEditIncidentInvestigationDto());

    readonly incidentRegisters = signal<IncidentRegisterDto[]>([]);
    readonly rootCauses = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditIncidentInvestigationDto());
            this.modal().show();
        } else {
            this._service.getIncidentInvestigationForEdit(id).subscribe((result) => {
                this.record.set(result.incidentInvestigation);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._incidentRegistersService
            .getAll(
                new GetAllIncidentRegistersInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                    incidentTypeId: undefined,
                    statusId: undefined,
                })
            )
            .subscribe((result) => this.incidentRegisters.set(result.items));

        this._rootCausesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.rootCauses.set(result.items));
    }

    onShown(): void {
        document.getElementById('IncidentInvestigation_InvestigationDate')?.focus();
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

    updateField(field: keyof CreateOrEditIncidentInvestigationDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditIncidentInvestigationDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'investigationDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    getTimeValue(field: keyof CreateOrEditIncidentInvestigationDto): string | undefined {
        const value = (this.record() as any)[field] as DateTime | undefined;
        return value?.toFormat('HH:mm:ss') ?? undefined;
    }

    updateTimeField(field: keyof CreateOrEditIncidentInvestigationDto, value: string): void {
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
