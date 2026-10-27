import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPAPeerReviewDto,
    EmployeeDto,
    EmployeesServiceProxy,
    GetAllEmployeesInput,
    GetAllKpiInfosInput,
    GetAllPARatingsInput,
    KpiInfoDto,
    KpiInfosServiceProxy,
    PAPeerReviewsServiceProxy,
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
    selector: 'createOrEditPAPeerReviewModal',
    templateUrl: './create-or-edit-pa-peer-review-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPAPeerReviewModalComponent extends AppComponentBase {
    private _service = inject(PAPeerReviewsServiceProxy);
    private _employeesService = inject(EmployeesServiceProxy);
    private _kpiInfosService = inject(KpiInfosServiceProxy);
    private _paRatingsService = inject(PARatingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPAPeerReviewDto>(new CreateOrEditPAPeerReviewDto());
    readonly employees = signal<EmployeeDto[]>([]);
    readonly kpiInfos = signal<KpiInfoDto[]>([]);
    readonly paRatings = signal<PARatingDto[]>([]);

    show(paFormId: number, id?: number): void {
        this.active.set(true);

        this._employeesService
            .getAll(
                new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.employees.set(result.items));

        this._kpiInfosService
            .getAll(
                new GetAllKpiInfosInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                    kpiCategoryId: undefined,
                })
            )
            .subscribe((result) => this.kpiInfos.set(result.items));

        this._paRatingsService
            .getAll(
                new GetAllPARatingsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined })
            )
            .subscribe((result) => this.paRatings.set(result.items));

        if (!id) {
            const record = new CreateOrEditPAPeerReviewDto();
            record.paFormId = paFormId;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getPAPeerReviewForEdit(id).subscribe((result) => {
                this.record.set(result.paPeerReview);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PAPeerReview_PeerId')?.focus();
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

    employeeLabel(employee: EmployeeDto): string {
        return `${employee.code} - ${employee.fullNameEn}`;
    }

    updateField(field: keyof CreateOrEditPAPeerReviewDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditPAPeerReviewDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
