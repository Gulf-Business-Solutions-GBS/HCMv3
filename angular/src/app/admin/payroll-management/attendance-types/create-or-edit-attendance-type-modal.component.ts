import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceTypeDto,
    AccrualPeriod,
    AttendanceTypesServiceProxy,
    CreateOrEditAttendanceTypeDto,
    GetAllAbsenceTypesInput,
    GetAllWageTypesInput,
    WageTypeDto,
    WageTypesServiceProxy,
    AbsenceTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditAttendanceTypeModal',
    templateUrl: './create-or-edit-attendance-type-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditAttendanceTypeModalComponent extends AppComponentBase {
    private _attendanceTypesService = inject(AttendanceTypesServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly attendanceType = signal<CreateOrEditAttendanceTypeDto>(new CreateOrEditAttendanceTypeDto());
    readonly wageTypes = signal<WageTypeDto[]>([]);
    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);

    readonly accrualPeriods = [
        { value: AccrualPeriod.NoAccrual, name: 'NoAccrual' },
        { value: AccrualPeriod.CalendarYear, name: 'CalendarYear' },
        { value: AccrualPeriod.FiscalYear, name: 'FiscalYear' },
        { value: AccrualPeriod.WorkYear, name: 'WorkYear' },
        { value: AccrualPeriod.BiAnnual, name: 'BiAnnual' },
        { value: AccrualPeriod.PerEmployment, name: 'PerEmployment' },
        { value: AccrualPeriod.Custom, name: 'Custom' },
    ];

    show(attendanceTypeId?: number): void {
        this.active.set(true);

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        this._absenceTypesService
            .getAll(new GetAllAbsenceTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.absenceTypes.set(result.items));

        if (!attendanceTypeId) {
            this.attendanceType.set(new CreateOrEditAttendanceTypeDto());
            this.modal().show();
        } else {
            this._attendanceTypesService.getAttendanceTypeForEdit(attendanceTypeId).subscribe((result) => {
                this.attendanceType.set(result.attendanceType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('AttendanceType_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._attendanceTypesService
            .createOrEdit(this.attendanceType())
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

    updateField(field: keyof CreateOrEditAttendanceTypeDto, value: any): void {
        const current = this.attendanceType();
        (current as any)[field] = value;
        const next = new CreateOrEditAttendanceTypeDto();
        Object.assign(next, current);
        this.attendanceType.set(next);
    }
}
