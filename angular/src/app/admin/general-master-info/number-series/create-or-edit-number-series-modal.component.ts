import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    NumberSeriesServiceProxy,
    CreateOrEditNumberSeriesDto,
    EmploymentType,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditNumberSeriesModal',
    templateUrl: './create-or-edit-number-series-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditNumberSeriesModalComponent extends AppComponentBase {
    private _numberSeriesService = inject(NumberSeriesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly numberSeries = signal<CreateOrEditNumberSeriesDto>(new CreateOrEditNumberSeriesDto());

    /** Legacy NumberSeries.EmploymentType: one series per type, or a single ALL series for everyone. */
    readonly employmentTypes = [
        { value: EmploymentType.Permanent, name: 'Permanent' },
        { value: EmploymentType.Temporary, name: 'Temporary' },
        { value: EmploymentType.ALL, name: 'ALL' },
    ];

    show(numberSeriesId?: number): void {
        this.active.set(true);

        if (!numberSeriesId) {
            const record = new CreateOrEditNumberSeriesDto();
            record.employmentType = EmploymentType.Permanent;
            this.numberSeries.set(record);
            this.modal().show();
        } else {
            this._numberSeriesService.getNumberSeriesForEdit(numberSeriesId).subscribe((result) => {
                this.numberSeries.set(result.numberSeries);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('NumberSeries_Code')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this._numberSeriesService
            .createOrEdit(this.numberSeries())
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
    updateField(field: keyof CreateOrEditNumberSeriesDto, value: any): void {
        const current = this.numberSeries();
        (current as any)[field] = value;
        const next = new CreateOrEditNumberSeriesDto();
        Object.assign(next, current);
        this.numberSeries.set(next);
    }
}
