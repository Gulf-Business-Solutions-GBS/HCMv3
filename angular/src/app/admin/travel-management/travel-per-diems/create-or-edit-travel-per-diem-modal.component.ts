import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTravelPerDiemDto,
    EmployeeGroupDto,
    EmployeeGroupsServiceProxy,
    EmployeeSubGroupDto,
    EmployeeSubGroupsServiceProxy,
    GetAllEmployeeGroupsInput,
    GetAllEmployeeSubGroupsInput,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
    TravelDestinationsServiceProxy,
    TravelPerDiemsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditTravelPerDiemModal',
    templateUrl: './create-or-edit-travel-per-diem-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditTravelPerDiemModalComponent extends AppComponentBase {
    private _service = inject(TravelPerDiemsServiceProxy);
    private _travelDestinationsService = inject(TravelDestinationsServiceProxy);
    private _employeeGroupsService = inject(EmployeeGroupsServiceProxy);
    private _employeeSubGroupsService = inject(EmployeeSubGroupsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditTravelPerDiemDto>(new CreateOrEditTravelPerDiemDto());
    readonly destinations = signal<SimpleLookupDto[]>([]);
    readonly employeeGroups = signal<EmployeeGroupDto[]>([]);
    readonly employeeSubGroups = signal<EmployeeSubGroupDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._travelDestinationsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.destinations.set(result.items));

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
            this.record.set(new CreateOrEditTravelPerDiemDto());
            this.modal().show();
        } else {
            this._service.getTravelPerDiemForEdit(id).subscribe((result) => {
                this.record.set(result.travelPerDiem);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TravelPerDiem_FromDestinationId')?.focus();
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

    updateField(field: keyof CreateOrEditTravelPerDiemDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditTravelPerDiemDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
