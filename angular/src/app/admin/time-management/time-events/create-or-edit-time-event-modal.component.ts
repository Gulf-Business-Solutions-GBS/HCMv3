import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AccessUnitDto,
    AccessUnitsServiceProxy,
    CreateOrEditTimeEventDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllAccessUnitsInput,
    GetAllEmployeesInput,
    GetAllSimpleLookupsInput,
    GetAllWorkLocationsInput,
    SimpleLookupDto,
    TimeEventsServiceProxy,
    TransSource,
    TransTypesServiceProxy,
    WorkLocationDto,
    WorkLocationsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditTimeEventModal',
    templateUrl: './create-or-edit-time-event-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditTimeEventModalComponent extends AppComponentBase {
    private _timeEventsService = inject(TimeEventsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _transTypesService = inject(TransTypesServiceProxy);
    private _accessUnitsService = inject(AccessUnitsServiceProxy);
    private _workLocationsService = inject(WorkLocationsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly timeEvent = signal<CreateOrEditTimeEventDto>(new CreateOrEditTimeEventDto());
    readonly employees = signal<EmployeeDto[]>([]);
    readonly transTypes = signal<SimpleLookupDto[]>([]);
    readonly accessUnits = signal<AccessUnitDto[]>([]);
    readonly workLocations = signal<WorkLocationDto[]>([]);

    readonly transSources = [
        { value: TransSource.Machine, name: 'Machine' },
        { value: TransSource.Manual, name: 'Manual' },
        { value: TransSource.System, name: 'System' },
        { value: TransSource.ESS, name: 'ESS' },
        { value: TransSource.MobileAPI, name: 'MobileAPI' },
        { value: TransSource.ExternalAPI, name: 'ExternalAPI' },
    ];

    show(timeEventId?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        this._transTypesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.transTypes.set(result.items));

        this._accessUnitsService
            .getAll(new GetAllAccessUnitsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.accessUnits.set(result.items));

        this._workLocationsService
            .getAll(new GetAllWorkLocationsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.workLocations.set(result.items));

        if (!timeEventId) {
            this.timeEvent.set(new CreateOrEditTimeEventDto());
            this.modal().show();
        } else {
            this._timeEventsService.getTimeEventForEdit(timeEventId).subscribe((result) => {
                this.timeEvent.set(result.timeEvent);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TimeEvent_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._timeEventsService
            .createOrEdit(this.timeEvent())
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

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    updateField(field: keyof CreateOrEditTimeEventDto, value: any): void {
        const current = this.timeEvent();
        (current as any)[field] = value;
        const next = new CreateOrEditTimeEventDto();
        Object.assign(next, current);
        this.timeEvent.set(next);
    }

    updateDateField(field: keyof CreateOrEditTimeEventDto, value: string): void {
        const current = this.timeEvent();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditTimeEventDto();
        Object.assign(next, current);
        this.timeEvent.set(next);
    }

    updateTimeField(field: keyof CreateOrEditTimeEventDto, value: string): void {
        const current = this.timeEvent();
        if (!value) {
            (current as any)[field] = undefined;
        } else {
            const base = current.transDate ?? DateTime.now();
            (current as any)[field] = DateTime.fromFormat(
                `${base.toFormat('yyyy-MM-dd')} ${value}`,
                'yyyy-MM-dd HH:mm'
            );
        }
        const next = new CreateOrEditTimeEventDto();
        Object.assign(next, current);
        this.timeEvent.set(next);
    }
}
