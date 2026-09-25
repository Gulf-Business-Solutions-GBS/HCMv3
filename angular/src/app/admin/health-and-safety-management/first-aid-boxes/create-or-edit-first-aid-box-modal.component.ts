import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditFirstAidBoxDto,
    EmployeeDto,
    EmployeesServiceProxy,
    FirstAidBoxesServiceProxy,
    GetAllEmployeesInput,
    GetAllSimpleLookupsInput,
    SafetyAreaAssetsServiceProxy,
    SimpleLookupDto,
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
    selector: 'createOrEditFirstAidBoxModal',
    templateUrl: './create-or-edit-first-aid-box-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditFirstAidBoxModalComponent extends AppComponentBase {
    private _service = inject(FirstAidBoxesServiceProxy);
    private _safetyAreaAssetsService = inject(SafetyAreaAssetsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditFirstAidBoxDto>(new CreateOrEditFirstAidBoxDto());

    readonly locations = signal<SimpleLookupDto[]>([]);
    readonly employees = signal<EmployeeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);
        this.loadLookups();

        if (!id) {
            this.record.set(new CreateOrEditFirstAidBoxDto());
            this.modal().show();
        } else {
            this._service.getFirstAidBoxForEdit(id).subscribe((result) => {
                this.record.set(result.firstAidBox);
                this.modal().show();
            });
        }
    }

    private loadLookups(): void {
        this._safetyAreaAssetsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.locations.set(result.items));

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.employees.set(result.items));
    }

    onShown(): void {
        document.getElementById('FirstAidBox_Code')?.focus();
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

    updateField(field: keyof CreateOrEditFirstAidBoxDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditFirstAidBoxDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: 'allocationDate', value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
