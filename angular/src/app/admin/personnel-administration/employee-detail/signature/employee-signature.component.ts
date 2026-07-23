import { Component, effect, inject, input, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditEmployeeSignatureDto,
    EmployeeSignaturesServiceProxy,
    GetAllEmployeeSignaturesInput,
} from '@shared/service-proxies/service-proxies';
import { finalize } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';

@Component({
    selector: 'employee-signature',
    templateUrl: './employee-signature.component.html',
    imports: [FormsModule, ButtonBusyDirective, LocalizePipe, PermissionPipe, BusyIfDirective],
})
export class EmployeeSignatureComponent extends AppComponentBase {
    private _service = inject(EmployeeSignaturesServiceProxy);

    readonly employeeId = input<number>();

    readonly loading = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditEmployeeSignatureDto>(new CreateOrEditEmployeeSignatureDto());

    constructor() {
        super();
        effect(() => {
            if (this.employeeId()) {
                this.load();
            }
        });
    }

    load(): void {
        this.loading.set(true);
        this._service
            .getAll(
                new GetAllEmployeeSignaturesInput({
                    employeeId: this.employeeId(),
                    maxResultCount: 1,
                    skipCount: 0,
                    sorting: undefined,
                })
            )
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => {
                if (result.items.length > 0) {
                    const item = result.items[0];
                    const record = new CreateOrEditEmployeeSignatureDto();
                    record.id = item.id;
                    record.employeeId = item.employeeId;
                    record.signature = item.signature;
                    this.record.set(record);
                } else {
                    const record = new CreateOrEditEmployeeSignatureDto();
                    record.employeeId = this.employeeId();
                    this.record.set(record);
                }
            });
    }

    onSignatureSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        const file = input.files && input.files.length ? input.files[0] : null;
        if (!file) {
            return;
        }

        const reader = new FileReader();
        reader.onload = () => {
            const current = this.record();
            const next = new CreateOrEditEmployeeSignatureDto();
            Object.assign(next, current);
            next.signature = reader.result as string;
            this.record.set(next);
        };
        reader.readAsDataURL(file);
    }

    save(): void {
        this.saving.set(true);
        this._service
            .createOrEdit(this.record())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.load();
            });
    }
}
