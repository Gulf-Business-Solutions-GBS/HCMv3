import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CopyLoanTypeInput,
    LoanTypeDto,
    LoanTypesServiceProxy,
    WageTypeCopyOption,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

/** Copy Loan Type (legacy "Copy LoanType" popup): new names, and whether to reuse or copy the wage type. */
@Component({
    selector: 'copyLoanTypeModal',
    templateUrl: './copy-loan-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CopyLoanTypeModalComponent extends AppComponentBase {
    private _loanTypeService = inject(LoanTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('copyModal');
    readonly modalSave = output<void>();

    readonly WageTypeCopyOption = WageTypeCopyOption;

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly source = signal<LoanTypeDto | undefined>(undefined);
    readonly nameEn = signal<string>('');
    readonly nameAr = signal<string>('');
    readonly wageTypeOption = signal<WageTypeCopyOption>(WageTypeCopyOption.Link);
    readonly wageTypeName = signal<string>('');

    show(source: LoanTypeDto): void {
        this.source.set(source);
        this.nameEn.set('');
        this.nameAr.set('');
        this.wageTypeOption.set(WageTypeCopyOption.Link);
        this.wageTypeName.set('');
        this.active.set(true);
        this.modal().show();
    }

    onShown(): void {
        document.getElementById('CopyLoanType_NameEn')?.focus();
    }

    /** The "copy the wage type" choice only means something when the source has one. */
    hasWageType(): boolean {
        return !!this.source()?.wageTypeId;
    }

    createsWageType(): boolean {
        return this.hasWageType() && this.wageTypeOption() === WageTypeCopyOption.CopyAndCreate;
    }

    save(): void {
        this.saving.set(true);
        this._loanTypeService
            .copy(
                new CopyLoanTypeInput({
                    loanTypeId: this.source().id,
                    nameEn: this.nameEn(),
                    nameAr: this.nameAr(),
                    wageTypeOption: this.createsWageType() ? WageTypeCopyOption.CopyAndCreate : WageTypeCopyOption.Link,
                    wageTypeName: this.createsWageType() ? this.wageTypeName() : undefined,
                })
            )
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.success(this.l('LoanTypeCopied'));
                this.close();
                this.modalSave.emit();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }
}
