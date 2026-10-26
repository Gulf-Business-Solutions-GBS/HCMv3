import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CountriesServiceProxy,
    GetAllSimpleLookupsInput,
    PayrollDriverEditDto,
    PayrollDriverServiceProxy,
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
    selector: 'createOrEditPayrollDriverModal',
    templateUrl: './create-or-edit-payroll-driver-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPayrollDriverModalComponent extends AppComponentBase {
    private _service = inject(PayrollDriverServiceProxy);
    private _countriesService = inject(CountriesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<PayrollDriverEditDto>(new PayrollDriverEditDto());
    readonly countries = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._countriesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.countries.set(result.items));

        if (!id) {
            this.record.set(new PayrollDriverEditDto());
            this.modal().show();
        } else {
            this._service.getPayrollDriverForEdit(id).subscribe((result) => {
                this.record.set(result);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PayrollDriver_CountryId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEditPayrollDriver(this.record())
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

    updateField(field: keyof PayrollDriverEditDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new PayrollDriverEditDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
