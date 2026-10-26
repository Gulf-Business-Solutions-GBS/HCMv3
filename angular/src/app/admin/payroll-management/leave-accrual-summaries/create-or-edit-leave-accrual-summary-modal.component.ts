import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AbsenceTypeDto,
    AbsenceTypesServiceProxy,
    CreateOrEditLeaveAccrualSummaryDto,
    GetAllAbsenceTypesInput,
    LeaveAccrualSummariesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditLeaveAccrualSummaryModal',
    templateUrl: './create-or-edit-leave-accrual-summary-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditLeaveAccrualSummaryModalComponent extends AppComponentBase {
    private _leaveAccrualSummariesService = inject(LeaveAccrualSummariesServiceProxy);
    private _absenceTypesService = inject(AbsenceTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly leaveAccrualSummary = signal<CreateOrEditLeaveAccrualSummaryDto>(new CreateOrEditLeaveAccrualSummaryDto());

    readonly absenceTypes = signal<AbsenceTypeDto[]>([]);

    show(leaveAccrualSummaryId?: number): void {
        this.active.set(true);

        this._absenceTypesService
            .getAll(
                new GetAllAbsenceTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.absenceTypes.set(result.items));

        if (!leaveAccrualSummaryId) {
            this.leaveAccrualSummary.set(new CreateOrEditLeaveAccrualSummaryDto());
            this.modal().show();
        } else {
            this._leaveAccrualSummariesService
                .getLeaveAccrualSummaryForEdit(leaveAccrualSummaryId)
                .subscribe((result) => {
                    this.leaveAccrualSummary.set(result.leaveAccrualSummary);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('LeaveAccrualSummary_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._leaveAccrualSummariesService
            .createOrEdit(this.leaveAccrualSummary())
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

    updateField(field: keyof CreateOrEditLeaveAccrualSummaryDto, value: any): void {
        const current = this.leaveAccrualSummary();
        (current as any)[field] = value;
        const next = new CreateOrEditLeaveAccrualSummaryDto();
        Object.assign(next, current);
        this.leaveAccrualSummary.set(next);
    }
}
