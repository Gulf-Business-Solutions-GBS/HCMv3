import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditReportDetailDto,
    GetAllReportMastersInput,
    GetAllReportSettingsInput,
    ReportDetailsServiceProxy,
    ReportMasterDto,
    ReportMastersServiceProxy,
    ReportSettingDto,
    ReportSettingsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditReportDetailModal',
    templateUrl: './create-or-edit-report-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditReportDetailModalComponent extends AppComponentBase {
    private _service = inject(ReportDetailsServiceProxy);
    private _reportMastersService = inject(ReportMastersServiceProxy);
    private _reportSettingsService = inject(ReportSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditReportDetailDto>(new CreateOrEditReportDetailDto());
    readonly reportMasters = signal<ReportMasterDto[]>([]);
    readonly reportSettings = signal<ReportSettingDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._reportMastersService
            .getAll(new GetAllReportMastersInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.reportMasters.set(result.items));

        this._reportSettingsService
            .getAll(new GetAllReportSettingsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.reportSettings.set(result.items));

        if (!id) {
            const record = new CreateOrEditReportDetailDto();
            record.drillDownLevel = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getReportDetailForEdit(id).subscribe((result) => {
                this.record.set(result.reportDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('ReportDetail_ReportMasterId')?.focus();
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

    updateField(field: keyof CreateOrEditReportDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditReportDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
