import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditReportSettingDetailDto,
    GetAllReportSettingsInput,
    ReportColumnAlignment,
    ReportColumnDataType,
    ReportSettingDetailsServiceProxy,
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
    selector: 'createOrEditReportSettingDetailModal',
    templateUrl: './create-or-edit-report-setting-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditReportSettingDetailModalComponent extends AppComponentBase {
    private _service = inject(ReportSettingDetailsServiceProxy);
    private _reportSettingsService = inject(ReportSettingsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditReportSettingDetailDto>(new CreateOrEditReportSettingDetailDto());
    readonly reportSettings = signal<ReportSettingDto[]>([]);

    readonly columnDataTypes = [
        { value: ReportColumnDataType.Int, name: 'Int' },
        { value: ReportColumnDataType.Double, name: 'Double' },
        { value: ReportColumnDataType.String, name: 'String' },
        { value: ReportColumnDataType.DateTime, name: 'DateTime' },
        { value: ReportColumnDataType.Boolean, name: 'Boolean' },
    ];

    readonly alignments = [
        { value: ReportColumnAlignment.Left, name: 'Left' },
        { value: ReportColumnAlignment.Center, name: 'Center' },
        { value: ReportColumnAlignment.Right, name: 'Right' },
    ];

    show(id?: number): void {
        this.active.set(true);

        this._reportSettingsService
            .getAll(
                new GetAllReportSettingsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.reportSettings.set(result.items));

        if (!id) {
            const record = new CreateOrEditReportSettingDetailDto();
            record.order = 0;
            record.width = 0;
            record.columnDataType = ReportColumnDataType.String;
            record.dataHAlignment = ReportColumnAlignment.Left;
            record.visibleInEn = true;
            record.visibleInAr = true;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getReportSettingDetailForEdit(id).subscribe((result) => {
                this.record.set(result.reportSettingDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('ReportSettingDetail_ReportSettingId')?.focus();
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

    updateField(field: keyof CreateOrEditReportSettingDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditReportSettingDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
