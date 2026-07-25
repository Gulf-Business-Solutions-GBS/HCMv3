import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditNineBoxMappingDto,
    GetAllSimpleLookupsInput,
    NineBoxMappingsServiceProxy,
    NineBoxRatingsServiceProxy,
    SimpleLookupDto,
    SuccessionRatingsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditNineBoxMappingModal',
    templateUrl: './create-or-edit-nine-box-mapping-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditNineBoxMappingModalComponent extends AppComponentBase {
    private _service = inject(NineBoxMappingsServiceProxy);
    private _successionRatingsService = inject(SuccessionRatingsServiceProxy);
    private _nineBoxRatingsService = inject(NineBoxRatingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditNineBoxMappingDto>(new CreateOrEditNineBoxMappingDto());

    readonly successionRatings = signal<SimpleLookupDto[]>([]);
    readonly nineBoxRatings = signal<SimpleLookupDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._successionRatingsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.successionRatings.set(result.items));

        this._nineBoxRatingsService
            .getAll(new GetAllSimpleLookupsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.nineBoxRatings.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditNineBoxMappingDto());
            this.modal().show();
        } else {
            this._service.getNineBoxMappingForEdit(id).subscribe((result) => {
                this.record.set(result.nineBoxMapping);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('NineBoxMapping_RatingId')?.focus();
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

    updateField(field: keyof CreateOrEditNineBoxMappingDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditNineBoxMappingDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
