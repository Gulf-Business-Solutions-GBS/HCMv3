import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeActionDto,
    EmployeeActionsServiceProxy,
    GetAllPersonalActionReasonsInput,
    GetAllPersonalActionsInput,
    PersonalActionDto,
    PersonalActionReasonDto,
    PersonalActionReasonsServiceProxy,
    PersonalActionsServiceProxy,
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
    selector: 'createOrEditEmployeeActionModal',
    templateUrl: './create-or-edit-employee-action-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditEmployeeActionModalComponent extends AppComponentBase {
    private _service = inject(EmployeeActionsServiceProxy);
    private _personalActionsService = inject(PersonalActionsServiceProxy);
    private _personalActionReasonsService = inject(PersonalActionReasonsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeActionDto>(new CreateOrEditEmployeeActionDto());
    readonly personalActions = signal<PersonalActionDto[]>([]);
    readonly personalActionReasons = signal<PersonalActionReasonDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._personalActionsService
            .getAll(new GetAllPersonalActionsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.personalActions.set(result.items));

        this._personalActionReasonsService
            .getAll(new GetAllPersonalActionReasonsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.personalActionReasons.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeActionDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeActionForEdit(id).subscribe((result) => {
                this.record.set(result.employeeAction);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeAction_PersonalActionId')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeActionDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeActionDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
