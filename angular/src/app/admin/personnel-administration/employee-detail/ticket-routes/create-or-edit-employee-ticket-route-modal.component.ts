import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeTicketRouteDto,
    EmployeeTicketRoutesServiceProxy,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
    TicketRoutesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';

@Component({
    selector: 'createOrEditEmployeeTicketRouteModal',
    templateUrl: './create-or-edit-employee-ticket-route-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditEmployeeTicketRouteModalComponent extends AppComponentBase {
    private _service = inject(EmployeeTicketRoutesServiceProxy);
    private _ticketRoutesService = inject(TicketRoutesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeTicketRouteDto>(new CreateOrEditEmployeeTicketRouteDto());

    readonly ticketRoutes = signal<SimpleLookupDto[]>([]);

    show(employeeId: number, id?: number): void {
        this.active.set(true);

        this._ticketRoutesService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.ticketRoutes.set(result.items));

        if (!id) {
            const record = new CreateOrEditEmployeeTicketRouteDto();
            record.employeeId = employeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getEmployeeTicketRouteForEdit(id).subscribe((result) => {
                this.record.set(result.employeeTicketRoute);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('EmployeeTicketRoute_TicketRouteId')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
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

    updateField(field: keyof CreateOrEditEmployeeTicketRouteDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditEmployeeTicketRouteDto();
        Object.assign(next, current);
        this.record.set(next);
    }

    updateDateField(field: keyof CreateOrEditEmployeeTicketRouteDto, value: string): void {
        this.updateField(field, value ? DateTime.fromISO(value) : (undefined as any));
    }
}
