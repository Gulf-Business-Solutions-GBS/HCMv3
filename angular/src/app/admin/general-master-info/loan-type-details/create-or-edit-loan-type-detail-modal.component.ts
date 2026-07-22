import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    LoanTypeDetailsServiceProxy,
    CreateOrEditLoanTypeDetailDto,
    LoanTypeDto,
    GetAllLoanTypesInput,
    LoanTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditLoanTypeDetailModal',
    templateUrl: './create-or-edit-loan-type-detail-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditLoanTypeDetailModalComponent extends AppComponentBase {
    private _loanTypeDetailService = inject(LoanTypeDetailsServiceProxy);
    private _loanTypesService = inject(LoanTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly loanTypeDetail = signal<CreateOrEditLoanTypeDetailDto>(new CreateOrEditLoanTypeDetailDto());
    readonly loanTypes = signal<LoanTypeDto[]>([]);

    show(loanTypeDetailId?: number): void {
        this.active.set(true);

        this._loanTypesService
            .getAll(new GetAllLoanTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.loanTypes.set(result.items);
            });

        if (!loanTypeDetailId) {
            this.loanTypeDetail.set(new CreateOrEditLoanTypeDetailDto());
            this.modal().show();
        } else {
            this._loanTypeDetailService.getLoanTypeDetailForEdit(loanTypeDetailId).subscribe((result) => {
                this.loanTypeDetail.set(result.loanTypeDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('LoanTypeDetail_Percent')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._loanTypeDetailService
            .createOrEdit(this.loanTypeDetail())
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
    updateField(field: keyof CreateOrEditLoanTypeDetailDto, value: any): void {
        const current = this.loanTypeDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditLoanTypeDetailDto();
        Object.assign(next, current);
        this.loanTypeDetail.set(next);
    }
}
