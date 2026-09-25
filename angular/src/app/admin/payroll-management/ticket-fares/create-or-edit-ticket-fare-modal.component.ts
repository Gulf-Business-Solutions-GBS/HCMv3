import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTicketFareDto,
    GetAllSimpleLookupsInput,
    GetAllTicketPaxTypesInput,
    SimpleLookupDto,
    TicketClassesServiceProxy,
    TicketFaresServiceProxy,
    TicketPaxTypeDto,
    TicketPaxTypesServiceProxy,
    TicketRoutesServiceProxy,
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
    selector: 'createOrEditTicketFareModal',
    templateUrl: './create-or-edit-ticket-fare-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditTicketFareModalComponent extends AppComponentBase {
    private _ticketFaresService = inject(TicketFaresServiceProxy);
    private _ticketRoutesService = inject(TicketRoutesServiceProxy);
    private _ticketPaxTypesService = inject(TicketPaxTypesServiceProxy);
    private _ticketClassesService = inject(TicketClassesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly ticketFare = signal<CreateOrEditTicketFareDto>(new CreateOrEditTicketFareDto());

    /** Adding, not editing: only a new record is filled in from the one in force on the date typed. */
    readonly isNew = signal<boolean>(false);

    constructor() {
        super();

        attachEffectiveDatedPrefill({
            recordType: 'TicketFare',
            record: this.ticketFare,
            isNew: this.isNew,
            keys: () => ({ TicketRouteId: this.ticketFare().ticketRouteId, TicketPaxTypeId: this.ticketFare().ticketPaxTypeId, TicketClassId: this.ticketFare().ticketClassId }),
            onPrefilled: (sourceDate) => this.notify.info(this.l('PrefilledFrom', sourceDate.toFormat('dd/MM/yyyy'))),
        });
    }

    readonly ticketRoutes = signal<SimpleLookupDto[]>([]);
    readonly ticketPaxTypes = signal<TicketPaxTypeDto[]>([]);
    readonly ticketClasses = signal<SimpleLookupDto[]>([]);

    show(ticketFareId?: number): void {
        this.isNew.set(!ticketFareId);
        this.active.set(true);

        this._ticketRoutesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.ticketRoutes.set(result.items));

        this._ticketPaxTypesService
            .getAll(new GetAllTicketPaxTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.ticketPaxTypes.set(result.items));

        this._ticketClassesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.ticketClasses.set(result.items));

        if (!ticketFareId) {
            this.ticketFare.set(new CreateOrEditTicketFareDto());
            this.modal().show();
        } else {
            this._ticketFaresService.getTicketFareForEdit(ticketFareId).subscribe((result) => {
                this.ticketFare.set(result.ticketFare);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TicketFare_TicketRouteId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._ticketFaresService
            .createOrEdit(this.ticketFare())
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

    updateField(field: keyof CreateOrEditTicketFareDto, value: any): void {
        const current = this.ticketFare();
        (current as any)[field] = value;
        const next = new CreateOrEditTicketFareDto();
        Object.assign(next, current);
        this.ticketFare.set(next);
    }

    updateStartDate(value: string): void {
        this.updateField('startDate', value ? DateTime.fromISO(value) : (undefined as any));
    }
}
