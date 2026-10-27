import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTempEmployeeBenefitDto,
    GetAllTempBenefitTypesInput,
    TempBenefitTypeDto,
    TempBenefitTypesServiceProxy,
    TempEmployeeBenefitsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditTempEmployeeBenefitModal',
    templateUrl: './create-or-edit-temp-employee-benefit-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditTempEmployeeBenefitModalComponent extends AppComponentBase {
    private _tempEmployeeBenefitsService = inject(TempEmployeeBenefitsServiceProxy);
    private _tempBenefitTypesService = inject(TempBenefitTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly tempEmployeeBenefit = signal<CreateOrEditTempEmployeeBenefitDto>(new CreateOrEditTempEmployeeBenefitDto());

    readonly tempBenefitTypes = signal<TempBenefitTypeDto[]>([]);

    show(tempEmployeeBenefitId?: number): void {
        this.active.set(true);

        this._tempBenefitTypesService
            .getAll(
                new GetAllTempBenefitTypesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.tempBenefitTypes.set(result.items));

        if (!tempEmployeeBenefitId) {
            this.tempEmployeeBenefit.set(new CreateOrEditTempEmployeeBenefitDto());
            this.modal().show();
        } else {
            this._tempEmployeeBenefitsService
                .getTempEmployeeBenefitForEdit(tempEmployeeBenefitId)
                .subscribe((result) => {
                    this.tempEmployeeBenefit.set(result.tempEmployeeBenefit);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('TempEmployeeBenefit_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._tempEmployeeBenefitsService
            .createOrEdit(this.tempEmployeeBenefit())
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

    updateField(field: keyof CreateOrEditTempEmployeeBenefitDto, value: any): void {
        const current = this.tempEmployeeBenefit();
        (current as any)[field] = value;
        const next = new CreateOrEditTempEmployeeBenefitDto();
        Object.assign(next, current);
        this.tempEmployeeBenefit.set(next);
    }
}
