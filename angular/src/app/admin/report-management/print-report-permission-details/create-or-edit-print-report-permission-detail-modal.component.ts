import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPrintReportPermissionDetailDto,
    GetAllPrintReportPermissionsInput,
    GetAllPrintReportsInput,
    PrintReportDto,
    PrintReportPermissionDetailsServiceProxy,
    PrintReportPermissionDto,
    PrintReportPermissionsServiceProxy,
    PrintReportsServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPrintReportPermissionDetailModal',
    templateUrl: './create-or-edit-print-report-permission-detail-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPrintReportPermissionDetailModalComponent extends AppComponentBase {
    private _service = inject(PrintReportPermissionDetailsServiceProxy);
    private _printReportPermissionsService = inject(PrintReportPermissionsServiceProxy);
    private _printReportsService = inject(PrintReportsServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPrintReportPermissionDetailDto>(
        new CreateOrEditPrintReportPermissionDetailDto()
    );
    readonly printReportPermissions = signal<PrintReportPermissionDto[]>([]);
    readonly printReports = signal<PrintReportDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._printReportPermissionsService
            .getAll(new GetAllPrintReportPermissionsInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined }))
            .subscribe((result) => this.printReportPermissions.set(result.items));

        this._printReportsService
            .getAll(
                new GetAllPrintReportsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.printReports.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditPrintReportPermissionDetailDto());
            this.modal().show();
        } else {
            this._service.getPrintReportPermissionDetailForEdit(id).subscribe((result) => {
                this.record.set(result.printReportPermissionDetail);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PrintReportPermissionDetail_PrintReportPermissionId')?.focus();
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

    updateField(field: keyof CreateOrEditPrintReportPermissionDetailDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditPrintReportPermissionDetailDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
