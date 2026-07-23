import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSalaryRegisterDetailDto,
    GetAllWageTypesInput,
    SalaryRegisterDetailsServiceProxy,
    WageTypeDto,
    WageTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditSalaryRegisterDetailModal',
    templateUrl: './create-or-edit-salary-register-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSalaryRegisterDetailModalComponent extends AppComponentBase {
    private _salaryRegisterDetailsService = inject(SalaryRegisterDetailsServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly salaryRegisterDetail = signal<CreateOrEditSalaryRegisterDetailDto>(new CreateOrEditSalaryRegisterDetailDto());

    readonly wageTypes = signal<WageTypeDto[]>([]);

    show(salaryRegisterDetailId?: number): void {
        this.active.set(true);

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        if (!salaryRegisterDetailId) {
            this.salaryRegisterDetail.set(new CreateOrEditSalaryRegisterDetailDto());
            this.modal().show();
        } else {
            this._salaryRegisterDetailsService.getSalaryRegisterDetailForEdit(salaryRegisterDetailId).subscribe((result) => {
                this.salaryRegisterDetail.set(result.salaryRegisterDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SalaryRegisterDetail_SalaryRegisterId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._salaryRegisterDetailsService
            .createOrEdit(this.salaryRegisterDetail())
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

    updateField(field: keyof CreateOrEditSalaryRegisterDetailDto, value: any): void {
        const current = this.salaryRegisterDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditSalaryRegisterDetailDto();
        Object.assign(next, current);
        this.salaryRegisterDetail.set(next);
    }
}
