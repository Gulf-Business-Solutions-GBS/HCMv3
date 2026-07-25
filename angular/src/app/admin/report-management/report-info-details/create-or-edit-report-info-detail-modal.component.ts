import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditReportInfoDetailDto,
    GetAllReportInfosInput,
    ReportInfoDetailsServiceProxy,
    ReportInfoDto,
    ReportInfosServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditReportInfoDetailModal',
    templateUrl: './create-or-edit-report-info-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditReportInfoDetailModalComponent extends AppComponentBase {
    private _service = inject(ReportInfoDetailsServiceProxy);
    private _reportInfosService = inject(ReportInfosServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditReportInfoDetailDto>(new CreateOrEditReportInfoDetailDto());
    readonly reportInfos = signal<ReportInfoDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._reportInfosService
            .getAll(new GetAllReportInfosInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.reportInfos.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditReportInfoDetailDto());
            this.modal().show();
        } else {
            this._service.getReportInfoDetailForEdit(id).subscribe((result) => {
                this.record.set(result.reportInfoDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('ReportInfoDetail_ReportInfoId')?.focus();
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

    updateField(field: keyof CreateOrEditReportInfoDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditReportInfoDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
