import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditPrintReportPermissionDto,
    GetRolesInput,
    PrintReportPermissionsServiceProxy,
    RoleListDto,
    RoleServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditPrintReportPermissionModal',
    templateUrl: './create-or-edit-print-report-permission-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditPrintReportPermissionModalComponent extends AppComponentBase {
    private _service = inject(PrintReportPermissionsServiceProxy);
    private _roleService = inject(RoleServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditPrintReportPermissionDto>(new CreateOrEditPrintReportPermissionDto());
    readonly roles = signal<RoleListDto[]>([]);

    show(id?: number): void {
        this.active.set(true);

        this._roleService.getRoles(new GetRolesInput({ permissions: undefined })).subscribe((result) => this.roles.set(result.items));

        if (!id) {
            this.record.set(new CreateOrEditPrintReportPermissionDto());
            this.modal().show();
        } else {
            this._service.getPrintReportPermissionForEdit(id).subscribe((result) => {
                this.record.set(result.printReportPermission);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('PrintReportPermission_RoleId')?.focus();
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

    updateField(field: keyof CreateOrEditPrintReportPermissionDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditPrintReportPermissionDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
