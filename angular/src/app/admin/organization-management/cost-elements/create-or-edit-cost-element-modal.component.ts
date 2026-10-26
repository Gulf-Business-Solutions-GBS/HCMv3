import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CostElementTypesServiceProxy,
    CostElementsServiceProxy,
    CreateOrEditCostElementDto,
    GetAllSimpleLookupsInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditCostElementModal',
    templateUrl: './create-or-edit-cost-element-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCostElementModalComponent extends AppComponentBase {
    private _costElementsService = inject(CostElementsServiceProxy);
    private _costElementTypesService = inject(CostElementTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly costElement = signal<CreateOrEditCostElementDto>(new CreateOrEditCostElementDto());
    readonly costElementTypes = signal<SimpleLookupDto[]>([]);

    show(costElementId?: number): void {
        this.active.set(true);

        this._costElementTypesService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => {
                this.costElementTypes.set(result.items);
            });

        if (!costElementId) {
            this.costElement.set(new CreateOrEditCostElementDto());
            this.modal().show();
        } else {
            this._costElementsService.getCostElementForEdit(costElementId).subscribe((result) => {
                this.costElement.set(result.costElement);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CostElement_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._costElementsService
            .createOrEdit(this.costElement())
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

    updateField(field: keyof CreateOrEditCostElementDto, value: any): void {
        const current = this.costElement();
        (current as any)[field] = value;
        const next = new CreateOrEditCostElementDto();
        Object.assign(next, current);
        this.costElement.set(next);
    }
}
