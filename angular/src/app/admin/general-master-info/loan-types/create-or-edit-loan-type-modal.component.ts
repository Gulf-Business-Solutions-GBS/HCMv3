import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    LoanTypesServiceProxy,
    CreateOrEditLoanTypeDto,
    LoanDeductionWageTypeDto,



} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditLoanTypeModal',
    templateUrl: './create-or-edit-loan-type-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditLoanTypeModalComponent extends AppComponentBase {
    private _loanTypeService = inject(LoanTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly loanType = signal<CreateOrEditLoanTypeDto>(new CreateOrEditLoanTypeDto());
    readonly wageTypes = signal<LoanDeductionWageTypeDto[]>([]);

    show(loanTypeId?: number): void {
        this.active.set(true);

        if (!loanTypeId) {
            this.loanType.set(new CreateOrEditLoanTypeDto());
            this.loadWageTypes(undefined);
            this.modal().show();
        } else {
            this._loanTypeService.getLoanTypeForEdit(loanTypeId).subscribe((result) => {
                this.loanType.set(result.loanType);
                this.loadWageTypes(result.loanType.wageTypeId);
                this.modal().show();
            });
        }
    }

    /** The deduction wage types legacy offered; the loan type's current one is always listed. */
    private loadWageTypes(currentWageTypeId: number | undefined): void {
        this._loanTypeService
            .getDeductionWageTypes(currentWageTypeId)
            .subscribe((result) => this.wageTypes.set(result.items));
    }

    onShown(): void {
        document.getElementById('LoanType_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._loanTypeService
            .createOrEdit(this.loanType())
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

    // Signal-friendly field updaters (keep template bindings immutable).
    updateField(field: keyof CreateOrEditLoanTypeDto, value: any): void {
        const current = this.loanType();
        (current as any)[field] = value;
        const next = new CreateOrEditLoanTypeDto();
        Object.assign(next, current);
        this.loanType.set(next);
    }
}
