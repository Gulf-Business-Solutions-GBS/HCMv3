import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditPrintReportDto, PrintReportsServiceProxy, ReportCategory } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { DateTime } from 'luxon';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

@Component({
    selector: 'createOrEditPrintReportModal',
    templateUrl: './create-or-edit-print-report-modal.component.html',
    imports: [
        DateInputDirective,
        AppBsModalDirective,
        FormsModule,
        ValidationMessagesComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditPrintReportModalComponent extends AppComponentBase {
    private _service = inject(PrintReportsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPrintReportDto>(new CreateOrEditPrintReportDto());

    readonly reportCategories = [
        { value: ReportCategory.Default, name: 'Default' },
        { value: ReportCategory.Custom, name: 'Custom' },
    ];

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            const record = new CreateOrEditPrintReportDto();
            record.isActive = true;
            record.reportCategory = ReportCategory.Default;
            record.customPercentage = 0;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getPrintReportForEdit(id).subscribe((result) => {
                this.record.set(result.printReport);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PrintReport_NameEn')?.focus();
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

    updateReportCreatedOn(value: string): void {
        const current = this.record();
        const next = new CreateOrEditPrintReportDto();
        Object.assign(next, current);
        next.reportCreatedOn = value ? DateTime.fromISO(value) : (undefined as any);
        this.record.set(next);
    }

    updateField(field: keyof CreateOrEditPrintReportDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditPrintReportDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
