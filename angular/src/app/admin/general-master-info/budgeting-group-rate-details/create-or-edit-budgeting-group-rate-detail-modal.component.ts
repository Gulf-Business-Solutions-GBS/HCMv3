import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    BudgetingGroupRateDetailsServiceProxy,
    CreateOrEditBudgetingGroupRateDetailDto,
    SimpleLookupDto,
    GetAllSimpleLookupsInput,
    BudgetingGroupsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { attachEffectiveDatedPrefill } from '@app/shared/common/effective-dated/attach-effective-dated-prefill';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditBudgetingGroupRateDetailModal',
    templateUrl: './create-or-edit-budgeting-group-rate-detail-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditBudgetingGroupRateDetailModalComponent extends AppComponentBase {
    private _budgetingGroupRateDetailService = inject(BudgetingGroupRateDetailsServiceProxy);
    private _budgetingGroupsService = inject(BudgetingGroupsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly budgetingGroupRateDetail = signal<CreateOrEditBudgetingGroupRateDetailDto>(new CreateOrEditBudgetingGroupRateDetailDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'BudgetingGroupRateDetail',
            record: this.budgetingGroupRateDetail,
            isNew: this.isNew,
            keys: () => ({ BudgetingGroupId: this.budgetingGroupRateDetail().budgetingGroupId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }
    readonly budgetingGroups = signal<SimpleLookupDto[]>([]);

    show(budgetingGroupRateDetailId?: number): void {
        this.isNew.set(!budgetingGroupRateDetailId);
        this.active.set(true);

        this._budgetingGroupsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => {
                this.budgetingGroups.set(result.items);
            });

        if (!budgetingGroupRateDetailId) {
            this.budgetingGroupRateDetail.set(new CreateOrEditBudgetingGroupRateDetailDto());
            this.modal().show();
        } else {
            this._budgetingGroupRateDetailService.getBudgetingGroupRateDetailForEdit(budgetingGroupRateDetailId).subscribe((result) => {
                this.budgetingGroupRateDetail.set(result.budgetingGroupRateDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('BudgetingGroupRateDetail_BonusRate')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._budgetingGroupRateDetailService
            .createOrEdit(this.budgetingGroupRateDetail())
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
    updateField(field: keyof CreateOrEditBudgetingGroupRateDetailDto, value: any): void {
        const current = this.budgetingGroupRateDetail();
        (current as any)[field] = value;
        const next = new CreateOrEditBudgetingGroupRateDetailDto();
        Object.assign(next, current);
        this.budgetingGroupRateDetail.set(next);
    }

    updateDateField(field: keyof CreateOrEditBudgetingGroupRateDetailDto, value: string): void {
        const current = this.budgetingGroupRateDetail();
        (current as any)[field] = value ? DateTime.fromISO(value) : (undefined as any);
        const next = new CreateOrEditBudgetingGroupRateDetailDto();
        Object.assign(next, current);
        this.budgetingGroupRateDetail.set(next);
    }
}
