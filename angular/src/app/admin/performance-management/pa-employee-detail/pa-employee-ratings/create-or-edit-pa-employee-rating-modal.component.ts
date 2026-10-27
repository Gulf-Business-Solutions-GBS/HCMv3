import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPAEmployeeRatingDto,
    GetAllPAEmployeeDetailsInput,
    GetAllPAEmployeeReviewersInput,
    GetAllPARatingsInput,
    PAEmployeeDetailDto,
    PAEmployeeDetailsServiceProxy,
    PAEmployeeRatingsServiceProxy,
    PAEmployeeReviewerDto,
    PAEmployeeReviewersServiceProxy,
    PARatingDto,
    PARatingsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPAEmployeeRatingModal',
    templateUrl: './create-or-edit-pa-employee-rating-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPAEmployeeRatingModalComponent extends AppComponentBase {
    private _service = inject(PAEmployeeRatingsServiceProxy);
    private _paEmployeeReviewersService = inject(PAEmployeeReviewersServiceProxy);
    private _paEmployeeDetailsService = inject(PAEmployeeDetailsServiceProxy);
    private _paRatingsService = inject(PARatingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPAEmployeeRatingDto>(new CreateOrEditPAEmployeeRatingDto());
    readonly paEmployeeReviewers = signal<PAEmployeeReviewerDto[]>([]);
    readonly paEmployeeDetails = signal<PAEmployeeDetailDto[]>([]);
    readonly paRatings = signal<PARatingDto[]>([]);

    show(paEmployeeId: number, id?: number): void {
        this.active.set(true);

        this._paEmployeeReviewersService
            .getAll(
                new GetAllPAEmployeeReviewersInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    paEmployeeId,
                })
            )
            .subscribe((result) => this.paEmployeeReviewers.set(result.items));

        this._paEmployeeDetailsService
            .getAll(
                new GetAllPAEmployeeDetailsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    paEmployeeId,
                })
            )
            .subscribe((result) => this.paEmployeeDetails.set(result.items));

        this._paRatingsService
            .getAll(
                new GetAllPARatingsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.paRatings.set(result.items));

        if (!id) {
            const record = new CreateOrEditPAEmployeeRatingDto();
            record.paEmployeeId = paEmployeeId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getPAEmployeeRatingForEdit(id).subscribe((result) => {
                this.record.set(result.paEmployeeRating);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PAEmployeeRating_PAEmployeeReviewerId')?.focus();
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

    updateField(field: keyof CreateOrEditPAEmployeeRatingDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditPAEmployeeRatingDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
