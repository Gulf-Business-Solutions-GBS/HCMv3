import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CalculateExpectedPerDiemInput,
    CreateOrEditTravelDetailDto,
    EmployeeGroupDto,
    EmployeeGroupsServiceProxy,
    EmployeeSubGroupDto,
    EmployeeSubGroupsServiceProxy,
    GetAllEmployeeGroupsInput,
    GetAllEmployeeSubGroupsInput,
    GetAllSimpleLookupsInput,
    GetAllTravelsInput,
    HotelsServiceProxy,
    SimpleLookupDto,
    TravelAirlinesServiceProxy,
    TravelDestinationsServiceProxy,
    TravelDetailsServiceProxy,
    TravelDto,
    TravelsServiceProxy,
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
    selector: 'createOrEditTravelDetailModal',
    templateUrl: './create-or-edit-travel-detail-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditTravelDetailModalComponent extends AppComponentBase {
    private _service = inject(TravelDetailsServiceProxy);
    private _travelsService = inject(TravelsServiceProxy);
    private _travelDestinationsService = inject(TravelDestinationsServiceProxy);
    private _hotelsService = inject(HotelsServiceProxy);
    private _travelAirlinesService = inject(TravelAirlinesServiceProxy);
    private _employeeGroupsService = inject(EmployeeGroupsServiceProxy);
    private _employeeSubGroupsService = inject(EmployeeSubGroupsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly calculating = signal<boolean>(false);
    readonly record = signal<CreateOrEditTravelDetailDto>(new CreateOrEditTravelDetailDto());
    readonly travels = signal<TravelDto[]>([]);
    readonly destinations = signal<SimpleLookupDto[]>([]);
    readonly hotels = signal<SimpleLookupDto[]>([]);
    readonly travelAirlines = signal<SimpleLookupDto[]>([]);
    readonly employeeGroups = signal<EmployeeGroupDto[]>([]);
    readonly employeeSubGroups = signal<EmployeeSubGroupDto[]>([]);

    readonly calcEmployeeGroupId = signal<number | undefined>(undefined);
    readonly calcEmployeeSubGroupId = signal<number | undefined>(undefined);
    readonly perDiemRate = signal<number | undefined>(undefined);
    readonly perDiemAmount = signal<number | undefined>(undefined);

    show(id?: number): void {
        this.active.set(true);
        this.perDiemRate.set(undefined);
        this.perDiemAmount.set(undefined);
        this.calcEmployeeGroupId.set(undefined);
        this.calcEmployeeSubGroupId.set(undefined);

        this._travelsService
            .getAll(new GetAllTravelsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, employeeId: undefined, filter: undefined }))
            .subscribe((result) => this.travels.set(result.items));

        this._travelDestinationsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.destinations.set(result.items));

        this._hotelsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.hotels.set(result.items));

        this._travelAirlinesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.travelAirlines.set(result.items));

        this._employeeGroupsService
            .getAll(new GetAllEmployeeGroupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employeeGroups.set(result.items));

        this._employeeSubGroupsService
            .getAll(
                new GetAllEmployeeSubGroupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    employeeGroupId: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.employeeSubGroups.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditTravelDetailDto());
            this.modal().show();
        } else {
            this._service.getTravelDetailForEdit(id).subscribe((result) => {
                this.record.set(result.travelDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TravelDetail_TravelId')?.focus();
    }

    calculateExpectedPerDiem(): void {
        const id = this.record().id;
        const employeeGroupId = this.calcEmployeeGroupId();
        const employeeSubGroupId = this.calcEmployeeSubGroupId();
        if (!id || !employeeGroupId || !employeeSubGroupId) {
            return;
        }

        this.calculating.set(true);
        this._service
            .calculateExpectedPerDiem(
                new CalculateExpectedPerDiemInput({
                    travelDetailId: id,
                    employeeGroupId,
                    employeeSubGroupId,
                })
            )
            .pipe(finalize(() => this.calculating.set(false)))
            .subscribe((result) => {
                this.perDiemRate.set(result.perDiemRate);
                this.perDiemAmount.set(result.perDiemAmount);
            });
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

    updateField(field: keyof CreateOrEditTravelDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditTravelDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditTravelDetailDto, value: string): void {
        const current = this.record();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditTravelDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateTimeField(field: keyof CreateOrEditTravelDetailDto, dateField: keyof CreateOrEditTravelDetailDto, value: string): void {
        const current = this.record();
        if (!value) {
            (current as any)[field] = undefined;
        } else {
            const base = (current as any)[dateField] ?? DateTime.now();
            (current as any)[field] = DateTime.fromFormat(`${base.toFormat('yyyy-MM-dd')} ${value}`, 'yyyy-MM-dd HH:mm');
        }
        const next = new CreateOrEditTravelDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
