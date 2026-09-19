import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    AirTicketPayType,
    CreateOrEditEmployeeSubGroupDto,
    CustomStartMonth,
    EmployeeGroupDto,
    EmployeeGroupsServiceProxy,
    EmployeeSubGroupsServiceProxy,
    GetAllEmployeeGroupsInput,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
    TicketClassesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditEmployeeSubGroupModal',
    templateUrl: './create-or-edit-employee-sub-group-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeSubGroupModalComponent extends AppComponentBase {
    private _service = inject(EmployeeSubGroupsServiceProxy);
    private _employeeGroupsService = inject(EmployeeGroupsServiceProxy);
    private _ticketClassesService = inject(TicketClassesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeSubGroupDto>(new CreateOrEditEmployeeSubGroupDto());
    readonly employeeGroups = signal<EmployeeGroupDto[]>([]);
    readonly ticketClasses = signal<SimpleLookupDto[]>([]);

    readonly airTicketPayTypes = [
        { value: AirTicketPayType.FareAmount, name: 'FareAmount' },
        { value: AirTicketPayType.FixAmount, name: 'FixAmount' },
        { value: AirTicketPayType.ActualAmount, name: 'ActualAmount' },
        { value: AirTicketPayType.ActualAmountWithinMaxlimit, name: 'ActualAmountWithinMaxlimit' },
    ];

    readonly customStartMonths = [
        { value: CustomStartMonth.None, name: 'None' },
        { value: CustomStartMonth.January, name: 'January' },
        { value: CustomStartMonth.February, name: 'February' },
        { value: CustomStartMonth.March, name: 'March' },
        { value: CustomStartMonth.April, name: 'April' },
        { value: CustomStartMonth.May, name: 'May' },
        { value: CustomStartMonth.June, name: 'June' },
        { value: CustomStartMonth.July, name: 'July' },
        { value: CustomStartMonth.August, name: 'August' },
        { value: CustomStartMonth.September, name: 'September' },
        { value: CustomStartMonth.October, name: 'October' },
        { value: CustomStartMonth.November, name: 'November' },
        { value: CustomStartMonth.December, name: 'December' },
    ];

    /** `employeeGroupId` presets the group when adding from inside that group's page. */
    show(id?: number, employeeGroupId?: number): void {
        this.active.set(true);

        this._employeeGroupsService
            .getAll(new GetAllEmployeeGroupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employeeGroups.set(result.items));

        this._ticketClassesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.ticketClasses.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeSubGroupDto();
            record.employeeGroupId = employeeGroupId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeSubGroupForEdit(id).subscribe((result) => {
                this.record.set(result.employeeSubGroup);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeSubGroup_NameEn')?.focus();
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

    updateField(field: keyof CreateOrEditEmployeeSubGroupDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeSubGroupDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
