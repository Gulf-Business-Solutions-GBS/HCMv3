import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditTravelDetailCostDto,
    ExpenseTypesServiceProxy,
    GetAllSimpleLookupsInput,
    GetAllTravelDetailsInput,
    SimpleLookupDto,
    TravelDetailCostsServiceProxy,
    TravelDetailDto,
    TravelDetailsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditTravelDetailCostModal',
    templateUrl: './create-or-edit-travel-detail-cost-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditTravelDetailCostModalComponent extends AppComponentBase {
    private _service = inject(TravelDetailCostsServiceProxy);
    private _travelDetailsService = inject(TravelDetailsServiceProxy);
    private _expenseTypesService = inject(ExpenseTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditTravelDetailCostDto>(new CreateOrEditTravelDetailCostDto());
    readonly travelDetails = signal<TravelDetailDto[]>([]);
    readonly expenseTypes = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._travelDetailsService
            .getAll(
                new GetAllTravelDetailsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    travelId: undefined,
                })
            )
            .subscribe((result) => this.travelDetails.set(result.items));

        this._expenseTypesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.expenseTypes.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditTravelDetailCostDto());
            this.modal().show();
        } else {
            this._service.getTravelDetailCostForEdit(id).subscribe((result) => {
                this.record.set(result.travelDetailCost);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('TravelDetailCost_TravelDetailId')?.focus();
    }

    travelDetailLabel(item: TravelDetailDto): string {
        return `#${item.id} - ${item.departureNameEn} -> ${item.arrivalNameEn}`;
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

    updateField(field: keyof CreateOrEditTravelDetailCostDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditTravelDetailCostDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
