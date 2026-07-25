import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditReportInfoDto,
    DomainTypeDto,
    DomainTypesServiceProxy,
    GetAllDomainTypesInput,
    GetAllReportTypesInput,
    ReportInfosServiceProxy,
    ReportTypeDto,
    ReportTypesServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditReportInfoModal',
    templateUrl: './create-or-edit-report-info-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditReportInfoModalComponent extends AppComponentBase {
    private _service = inject(ReportInfosServiceProxy);
    private _reportTypesService = inject(ReportTypesServiceProxy);
    private _domainTypesService = inject(DomainTypesServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditReportInfoDto>(new CreateOrEditReportInfoDto());
    readonly reportTypes = signal<ReportTypeDto[]>([]);
    readonly domainTypes = signal<DomainTypeDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._reportTypesService
            .getAll(new GetAllReportTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.reportTypes.set(result.items));

        this._domainTypesService
            .getAll(new GetAllDomainTypesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.domainTypes.set(result.items));

        if (!id) {
            const record = new CreateOrEditReportInfoDto();
            record.isActive = true;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getReportInfoForEdit(id).subscribe((result) => {
                this.record.set(result.reportInfo);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('ReportInfo_ReportName')?.focus();
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

    updateField(field: keyof CreateOrEditReportInfoDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditReportInfoDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
