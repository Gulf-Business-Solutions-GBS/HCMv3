import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditKpiCategoryDto, KpiCategoriesServiceProxy } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditKpiCategoryModal',
    templateUrl: './create-or-edit-kpi-category-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditKpiCategoryModalComponent extends AppComponentBase {
    private _service = inject(KpiCategoriesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly kpiCategory = signal<CreateOrEditKpiCategoryDto>(new CreateOrEditKpiCategoryDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            this.kpiCategory.set(new CreateOrEditKpiCategoryDto());
            this.modal().show();
        } else {
            this._service.getKpiCategoryForEdit(id).subscribe((result) => {
                this.kpiCategory.set(result.kpiCategory);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('KpiCategory_Code')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.kpiCategory())
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

    updateField(field: keyof CreateOrEditKpiCategoryDto, value: any): void {
        const current = this.kpiCategory();
        (current as any)[field] = value;
        const next = new CreateOrEditKpiCategoryDto();
        Object.assign(next, current);
        this.kpiCategory.set(next);
    }
}
