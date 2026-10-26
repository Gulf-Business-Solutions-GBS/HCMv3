import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditGatePassPersonDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GatePassPersonsServiceProxy,
    GatePassVendorDto,
    GatePassVendorsServiceProxy,
    GetAllEmployeesInput,
    GetAllGatePassVendorsInput,
    GetAllSimpleLookupsInput,
    PersonTypesServiceProxy,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditGatePassPersonModal',
    templateUrl: './create-or-edit-gate-pass-person-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditGatePassPersonModalComponent extends AppComponentBase {
    private _service = inject(GatePassPersonsServiceProxy);
    private _personTypesService = inject(PersonTypesServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _gatePassVendorsService = inject(GatePassVendorsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditGatePassPersonDto>(new CreateOrEditGatePassPersonDto());

    readonly personTypes = signal<SimpleLookupDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);
    readonly gatePassVendors = signal<GatePassVendorDto[]>([]);

    show(gatePassId: number, id?: number): void {
        this.active.set(true);

        this._personTypesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.personTypes.set(result.items));

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        this._gatePassVendorsService
            .getAll(
                new GetAllGatePassVendorsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.gatePassVendors.set(result.items));

        if (!id) {
            const record = new CreateOrEditGatePassPersonDto();
            record.gatePassId = gatePassId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getGatePassPersonForEdit(id).subscribe((result) => {
                this.record.set(result.gatePassPerson);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('GatePassPerson_PersonTypeId')?.focus();
    }

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
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

    updateField(field: keyof CreateOrEditGatePassPersonDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditGatePassPersonDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
