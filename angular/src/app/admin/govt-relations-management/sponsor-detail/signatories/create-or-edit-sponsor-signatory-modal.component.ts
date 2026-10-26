import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditSponsorSignatoryDto,
    GetAllSignatoriesInput,
    SignatoriesServiceProxy,
    SignatoryDto,
    SponsorSignatoriesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditSponsorSignatoryModal',
    templateUrl: './create-or-edit-sponsor-signatory-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditSponsorSignatoryModalComponent extends AppComponentBase {
    private _service = inject(SponsorSignatoriesServiceProxy);
    private _signatoriesService = inject(SignatoriesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditSponsorSignatoryDto>(new CreateOrEditSponsorSignatoryDto());
    readonly signatories = signal<SignatoryDto[]>([]);

    show(sponsorId: number, id?: number): void {
        this.active.set(true);

        this._signatoriesService
            .getAll(
                new GetAllSignatoriesInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.signatories.set(result.items));

        if (!id) {
            const record = new CreateOrEditSponsorSignatoryDto();
            record.sponsorId = sponsorId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getSponsorSignatoryForEdit(id).subscribe((result) => {
                this.record.set(result.sponsorSignatory);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('SponsorSignatory_SignatoryId')?.focus();
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

    updateField(field: keyof CreateOrEditSponsorSignatoryDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditSponsorSignatoryDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
