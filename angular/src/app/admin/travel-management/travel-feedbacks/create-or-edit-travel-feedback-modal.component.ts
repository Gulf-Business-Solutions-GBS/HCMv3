import { Component, computed, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTravelFeedbackDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllTravelsInput,
    TravelApplicantLookupDto,
    TravelDto,
    TravelFeedbacksServiceProxy,
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
import { RecordDirectlyCheckComponent } from '@app/shared/common/record-directly/record-directly-check.component';
import { AttachmentsComponent } from '@app/shared/common/attachments/attachments.component';

@Component({
    selector: 'createOrEditTravelFeedbackModal',
    templateUrl: './create-or-edit-travel-feedback-modal.component.html',
    imports: [
        RecordDirectlyCheckComponent,
        AttachmentsComponent,
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditTravelFeedbackModalComponent extends AppComponentBase {
    private _service = inject(TravelFeedbacksServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _travelsService = inject(TravelsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditTravelFeedbackDto>(new CreateOrEditTravelFeedbackDto());
    readonly employees = signal<EmployeeDto[]>([]);
    readonly travels = signal<TravelDto[]>([]);
    readonly applicants = signal<TravelApplicantLookupDto[]>([]);

    /**
     * Who the claim is for (legacy TravelFeedback.Employee / TravelFeedback.Applicant). Exactly one is sent; the
     * server refuses neither or both. An applicant's claim is recorded directly rather than sent for approval.
     */
    readonly partyType = signal<'employee' | 'applicant'>('employee');

    /** Legacy EmpTravel: only the chosen person's trips are offered. */
    readonly partyTravels = computed(() => {
        const record = this.record();
        return this.travels().filter((t) =>
            this.partyType() === 'employee'
                ? !t.applicantId && (!record.employeeId || t.employeeId === record.employeeId)
                : !!t.applicantId && (!record.applicantId || t.applicantId === record.applicantId)
        );
    });

    show(id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));

        this._travelsService
            .getAll(new GetAllTravelsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, employeeId: undefined, applicantId: undefined, filter: undefined }))
            .subscribe((result) => this.travels.set(result.items));

        this._travelsService.getApplicantsForLookup().subscribe((result) => this.applicants.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditTravelFeedbackDto());
            this.partyType.set('employee');
            this.modal().show();
        } else {
            this._service.getTravelFeedbackForEdit(id).subscribe((result) => {
                this.record.set(result.travelFeedback);
                this.partyType.set(result.travelFeedback.applicantId ? 'applicant' : 'employee');
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TravelFeedback_EmployeeId')?.focus();
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    applicantLabel(applicant: TravelApplicantLookupDto): string {
        return `${applicant.code} - ${applicant.fullNameEn}`;
    }

    travelLabel(travel: TravelDto): string {
        return `${travel.code} - ${travel.applicantId ? travel.applicantFullNameEn : travel.employeeFullNameEn}`;
    }

    /** Switching clears the other side and the trip, so the record never carries both or a stranger's trip. */
    setPartyType(type: 'employee' | 'applicant'): void {
        this.partyType.set(type);
        this.updateField(type === 'employee' ? 'applicantId' : 'employeeId', undefined);
        this.updateField('travelId', undefined);
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

    updateField(field: keyof CreateOrEditTravelFeedbackDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditTravelFeedbackDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditTravelFeedbackDto, value: string): void {
        const current = this.record();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditTravelFeedbackDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
