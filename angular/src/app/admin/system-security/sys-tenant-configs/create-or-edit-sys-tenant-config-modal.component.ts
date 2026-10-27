import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSysTenantConfigDto,
    SysTenantConfigsServiceProxy,
    TenantConfigCategory,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditSysTenantConfigModal',
    templateUrl: './create-or-edit-sys-tenant-config-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditSysTenantConfigModalComponent extends AppComponentBase {
    private _service = inject(SysTenantConfigsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditSysTenantConfigDto>(new CreateOrEditSysTenantConfigDto());

    readonly categories = [
        { value: TenantConfigCategory.Employee, name: 'Employee' },
        { value: TenantConfigCategory.Payroll, name: 'Payroll' },
        { value: TenantConfigCategory.TimeAttendance, name: 'TimeAttendance' },
        { value: TenantConfigCategory.Formatting, name: 'Formatting' },
        { value: TenantConfigCategory.Report, name: 'Report' },
        { value: TenantConfigCategory.Geolocation, name: 'Geolocation' },
    ];

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditSysTenantConfigDto();
            record.configCategory = TenantConfigCategory.Employee;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getSysTenantConfigForEdit(id).subscribe((result) => {
                this.record.set(result.sysTenantConfig);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SysTenantConfig_Key')?.focus();
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

    updateField(field: keyof CreateOrEditSysTenantConfigDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditSysTenantConfigDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditSysTenantConfigDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
