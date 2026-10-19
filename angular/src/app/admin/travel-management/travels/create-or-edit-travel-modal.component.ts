import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTravelDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllTravelTypesInput,
    TravelTypeDto,
    TravelApplicantLookupDto,
    TravelTypesServiceProxy,
    TravelsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditTravelModal',
    templateUrl: './create-or-edit-travel-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
        DecimalPipe,
    ],
})
export class CreateOrEditTravelModalComponent extends AppComponentBase {
    private _service = inject(TravelsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _travelTypesService = inject(TravelTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditTravelDto>(new CreateOrEditTravelDto());
    readonly employees = signal<EmployeeDto[]>([]);
    readonly applicants = signal<TravelApplicantLookupDto[]>([]);

    /**
     * Who the trip is for (legacy Travel.Employee / Travel.Applicant). Exactly one is sent; the server refuses
     * neither or both. An applicant's trip is recorded directly rather than sent for approval.
     */
    readonly partyType = signal<'employee' | 'applicant'>('employee');
    readonly travelTypes = signal<TravelTypeDto[]>([]);

    /** Read-only, totalled by the server from the legs and their costs (legacy Travel.TotalTravelCost). */
    readonly totalTravelCost = signal<number>(0);

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        this._service.getApplicantsForLookup().subscribe((result) => this.applicants.set(result.items));

        this._travelTypesService
            .getAll(new GetAllTravelTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.travelTypes.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditTravelDto());
            this.partyType.set('employee');
            this.totalTravelCost.set(0);
            this.modal().show();
        } else {
            this._service.getTravelForEdit(id).subscribe((result) => {
                this.record.set(result.travel);
                this.partyType.set(result.travel.applicantId ? 'applicant' : 'employee');
                this.totalTravelCost.set(result.totalTravelCost ?? 0);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('Travel_EmployeeId')?.focus();
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    applicantLabel(applicant: TravelApplicantLookupDto): string {
        return `${applicant.code} - ${applicant.fullNameEn}`;
    }

    /** Switching clears the other side, so the record never carries both. */
    setPartyType(type: 'employee' | 'applicant'): void {
        this.partyType.set(type);
        this.updateField(type === 'employee' ? 'applicantId' : 'employeeId', undefined);
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

    updateField(field: keyof CreateOrEditTravelDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditTravelDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditTravelDto, value: string): void {
        const current = this.record();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditTravelDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
