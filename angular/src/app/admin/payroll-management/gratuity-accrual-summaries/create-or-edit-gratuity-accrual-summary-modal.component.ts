import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditGratuityAccrualSummaryDto,
    GratuityAccrualSummariesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditGratuityAccrualSummaryModal',
    templateUrl: './create-or-edit-gratuity-accrual-summary-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditGratuityAccrualSummaryModalComponent extends AppComponentBase {
    private _gratuityAccrualSummariesService = inject(GratuityAccrualSummariesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly gratuityAccrualSummary = signal<CreateOrEditGratuityAccrualSummaryDto>(
        new CreateOrEditGratuityAccrualSummaryDto()
    );

    show(gratuityAccrualSummaryId?: number): void {
        this.active.set(true);

        if (!gratuityAccrualSummaryId) {
            this.gratuityAccrualSummary.set(new CreateOrEditGratuityAccrualSummaryDto());
            this.modal().show();
        } else {
            this._gratuityAccrualSummariesService
                .getGratuityAccrualSummaryForEdit(gratuityAccrualSummaryId)
                .subscribe((result) => {
                    this.gratuityAccrualSummary.set(result.gratuityAccrualSummary);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('GratuityAccrualSummary_EmployeeId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._gratuityAccrualSummariesService
            .createOrEdit(this.gratuityAccrualSummary())
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

    updateField(field: keyof CreateOrEditGratuityAccrualSummaryDto, value: any): void {
        const current = this.gratuityAccrualSummary();
        (current as any)[field] = value;
        const next = new CreateOrEditGratuityAccrualSummaryDto();
        Object.assign(next, current);
        this.gratuityAccrualSummary.set(next);
    }
}
