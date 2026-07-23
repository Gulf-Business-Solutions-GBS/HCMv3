import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditSalaryRegisterDaysDetailDto, SalaryRegisterDaysDetailsServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditSalaryRegisterDaysDetailModal',
    templateUrl: './create-or-edit-salary-register-days-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSalaryRegisterDaysDetailModalComponent extends AppComponentBase {
    private _salaryRegisterDaysDetailsService = inject(SalaryRegisterDaysDetailsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly salaryRegisterDaysDetail = signal<CreateOrEditSalaryRegisterDaysDetailDto>(new CreateOrEditSalaryRegisterDaysDetailDto());

    show(salaryRegisterDaysDetailId?: number): void {
        this.active.set(true);

        if (!salaryRegisterDaysDetailId) {
            this.salaryRegisterDaysDetail.set(new CreateOrEditSalaryRegisterDaysDetailDto());
            this.modal().show();
        } else {
            this._salaryRegisterDaysDetailsService.getSalaryRegisterDaysDetailForEdit(salaryRegisterDaysDetailId).subscribe((result) => {
                this.salaryRegisterDaysDetail.set(result.salaryRegisterDaysDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SalaryRegisterDaysDetail_SalaryRegisterDetailId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._salaryRegisterDaysDetailsService
            .createOrEdit(this.salaryRegisterDaysDetail())
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

    updateField(field: keyof CreateOrEditSalaryRegisterDaysDetailDto, value: any): void {
        const current = this.salaryRegisterDaysDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditSalaryRegisterDaysDetailDto();
        Object.assign(next, current);
        this.salaryRegisterDaysDetail.set(next);
    }

    updatePeriod(value: string): void {
        this.updateField('period', value ? (DateTime.fromISO(value) as any) : (undefined as any));
    }
}
