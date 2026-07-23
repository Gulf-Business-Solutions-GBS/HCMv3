import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTicketTypeDto,
    DeductionPeriod,
    GetAllWageTypesInput,
    LeaveBasisPeriod,
    QuotaEntitlement,
    TicketTypesServiceProxy,
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
    selector: 'createOrEditTicketTypeModal',
    templateUrl: './create-or-edit-ticket-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditTicketTypeModalComponent extends AppComponentBase {
    private _ticketTypesService = inject(TicketTypesServiceProxy);
    private _wageTypesService = inject(WageTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly ticketType = signal<CreateOrEditTicketTypeDto>(new CreateOrEditTicketTypeDto());
    readonly wageTypes = signal<WageTypeDto[]>([]);

    readonly ticketBasisPeriods = [
        { value: LeaveBasisPeriod.CalendarDays, name: 'CalendarDays' },
        { value: LeaveBasisPeriod.FixedDays, name: 'FixedDays' },
    ];

    readonly deductionPeriods = [
        { value: DeductionPeriod.AfterEntitlement, name: 'AfterEntitlement' },
        { value: DeductionPeriod.SameAsEntitlement, name: 'SameAsEntitlement' },
    ];

    readonly quotaEntitlements = [
        { value: QuotaEntitlement.Period, name: 'Period' },
        { value: QuotaEntitlement.Immediate, name: 'Immediate' },
    ];

    show(ticketTypeId?: number): void {
        this.active.set(true);

        this._wageTypesService
            .getAll(new GetAllWageTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.wageTypes.set(result.items));

        if (!ticketTypeId) {
            this.ticketType.set(new CreateOrEditTicketTypeDto());
            this.modal().show();
        } else {
            this._ticketTypesService.getTicketTypeForEdit(ticketTypeId).subscribe((result) => {
                this.ticketType.set(result.ticketType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TicketType_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._ticketTypesService
            .createOrEdit(this.ticketType())
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

    updateField(field: keyof CreateOrEditTicketTypeDto, value: any): void {
        const current = this.ticketType();
        (current as any)[field] = value;
        const next = new CreateOrEditTicketTypeDto();
        Object.assign(next, current);
        this.ticketType.set(next);
    }
}
