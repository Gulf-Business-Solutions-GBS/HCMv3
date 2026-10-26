import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditTicketPaxTypeDto, TicketPaxTypesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditTicketPaxTypeModal',
    templateUrl: './create-or-edit-ticket-pax-type-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditTicketPaxTypeModalComponent extends AppComponentBase {
    private _ticketPaxTypesService = inject(TicketPaxTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly ticketPaxType = signal<CreateOrEditTicketPaxTypeDto>(new CreateOrEditTicketPaxTypeDto());

    show(ticketPaxTypeId?: number): void {
        this.active.set(true);

        if (!ticketPaxTypeId) {
            this.ticketPaxType.set(new CreateOrEditTicketPaxTypeDto());
            this.modal().show();
        } else {
            this._ticketPaxTypesService.getTicketPaxTypeForEdit(ticketPaxTypeId).subscribe((result) => {
                this.ticketPaxType.set(result.ticketPaxType);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TicketPaxType_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._ticketPaxTypesService
            .createOrEdit(this.ticketPaxType())
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

    updateField(field: keyof CreateOrEditTicketPaxTypeDto, value: any): void {
        const current = this.ticketPaxType();
        (current as any)[field] = value;
        const next = new CreateOrEditTicketPaxTypeDto();
        Object.assign(next, current);
        this.ticketPaxType.set(next);
    }
}
