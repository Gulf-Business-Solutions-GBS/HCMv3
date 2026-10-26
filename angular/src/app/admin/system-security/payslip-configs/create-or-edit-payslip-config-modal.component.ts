import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPayslipConfigDto,
    EmailTemplateDto,
    EmailTemplatesServiceProxy,
    GetAllEmailTemplatesInput,
    PayslipConfigsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPayslipConfigModal',
    templateUrl: './create-or-edit-payslip-config-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPayslipConfigModalComponent extends AppComponentBase {
    private _service = inject(PayslipConfigsServiceProxy);
    private _emailTemplatesService = inject(EmailTemplatesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPayslipConfigDto>(new CreateOrEditPayslipConfigDto());
    readonly emailTemplates = signal<EmailTemplateDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._emailTemplatesService
            .getAll(
                new GetAllEmailTemplatesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.emailTemplates.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditPayslipConfigDto());
            this.modal().show();
        } else {
            this._service.getPayslipConfigForEdit(id).subscribe((result) => {
                this.record.set(result.payslipConfig);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PayslipConfig_EmailTemplateId')?.focus();
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

    updateField(field: keyof CreateOrEditPayslipConfigDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditPayslipConfigDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
