import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditTempBenefitTypeDto, TempBenefitTypesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditTempBenefitTypeModal',
    templateUrl: './create-or-edit-temp-benefit-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditTempBenefitTypeModalComponent extends AppComponentBase {
    private _tempBenefitTypesService = inject(TempBenefitTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly tempBenefitType = signal<CreateOrEditTempBenefitTypeDto>(new CreateOrEditTempBenefitTypeDto());

    readonly textFields: (keyof CreateOrEditTempBenefitTypeDto)[] = [
        'ticket',
        'simCard',
        'phone',
        'incentive',
        'bonusSIP',
        'uniform',
        'medicalInsurance',
        'leaveDays',
        'medicalLeave',
        'maternityLeave',
        'staffDiscount',
        'specialAllowance',
        'managerAllowance',
        'companyAsset',
        'schoolAllowance',
        'noticePeriod',
        'indemnity',
        'retirement',
        'minSalary',
        'midSalary',
        'maxSalary',
        'transportationAllowance',
        'housingAllowance',
        'otherAllowance',
    ];

    show(tempBenefitTypeId?: number): void {
        this.active.set(true);

        if (!tempBenefitTypeId) {
            this.tempBenefitType.set(new CreateOrEditTempBenefitTypeDto());
            this.modal().show();
        } else {
            this._tempBenefitTypesService.getTempBenefitTypeForEdit(tempBenefitTypeId).subscribe((result) => {
                this.tempBenefitType.set(result.tempBenefitType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TempBenefitType_Grade')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._tempBenefitTypesService
            .createOrEdit(this.tempBenefitType())
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

    getFieldValue(field: keyof CreateOrEditTempBenefitTypeDto): string | undefined {
        return (this.tempBenefitType() as any)[field];
    }

    fieldLabel(field: string): string {
        return field.charAt(0).toUpperCase() + field.slice(1);
    }

    updateField(field: keyof CreateOrEditTempBenefitTypeDto, value: any): void {
        const current = this.tempBenefitType();
        (current as any)[field] = value;
        const next = new CreateOrEditTempBenefitTypeDto();
        Object.assign(next, current);
        this.tempBenefitType.set(next);
    }
}
