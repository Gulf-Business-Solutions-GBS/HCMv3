import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditRiskRegisterDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllSimpleLookupsInput,
    RiskActionsServiceProxy,
    RiskImpactsServiceProxy,
    RiskPossibilitiesServiceProxy,
    RiskRegistersServiceProxy,
    RiskStatusesServiceProxy,
    RiskTypesServiceProxy,
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
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditRiskRegisterModal',
    templateUrl: './create-or-edit-risk-register-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditRiskRegisterModalComponent extends AppComponentBase {
    private _service = inject(RiskRegistersServiceProxy);
    private _riskTypesService = inject(RiskTypesServiceProxy);
    private _riskImpactsService = inject(RiskImpactsServiceProxy);
    private _riskPossibilitiesService = inject(RiskPossibilitiesServiceProxy);
    private _riskStatusesService = inject(RiskStatusesServiceProxy);
    private _riskActionsService = inject(RiskActionsServiceProxy);
    private _safetyAreaAssetsService = inject(SafetyAreaAssetsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditRiskRegisterDto>(new CreateOrEditRiskRegisterDto());

    readonly riskTypes = signal<SimpleLookupDto[]>([]);
    readonly impacts = signal<SimpleLookupDto[]>([]);
    readonly possibilities = signal<SimpleLookupDto[]>([]);
    readonly statuses = signal<SimpleLookupDto[]>([]);
    readonly riskActions = signal<SimpleLookupDto[]>([]);
    readonly locations = signal<SimpleLookupDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditRiskRegisterDto());
            this.modal().show();
        } else {
            this._service.getRiskRegisterForEdit(id).subscribe((result) => {
                this.record.set(result.riskRegister);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        const lookupInput = () =>
            new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined });

        this._riskTypesService.getAll(lookupInput()).subscribe((result) => this.riskTypes.set(result.items));
        this._riskImpactsService.getAll(lookupInput()).subscribe((result) => this.impacts.set(result.items));
        this._riskPossibilitiesService
            .getAll(lookupInput())
            .subscribe((result) => this.possibilities.set(result.items));
        this._riskStatusesService.getAll(lookupInput()).subscribe((result) => this.statuses.set(result.items));
        this._riskActionsService.getAll(lookupInput()).subscribe((result) => this.riskActions.set(result.items));
        this._safetyAreaAssetsService.getAll(lookupInput()).subscribe((result) => this.locations.set(result.items));

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));
    }

    onShown(): void {
        document.getElementById('RiskRegister_ReportingDate')?.focus();
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

    updateField(field: keyof CreateOrEditRiskRegisterDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditRiskRegisterDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'reportingDate' | 'reassessmentDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }

    getTimeValue(field: keyof CreateOrEditRiskRegisterDto): string | undefined {
        const value = (this.record() as any)[field] as DateTime | undefined;
        return value?.toFormat('HH:mm:ss') ?? undefined;
    }

    updateTimeField(field: keyof CreateOrEditRiskRegisterDto, value: string): void {
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
