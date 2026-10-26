import { Component, input, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditSimpleLookupDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../../shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '../../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { ISimpleLookupServiceProxy } from './simple-lookup.types';

@Component({
    selector: 'simpleLookupModal',
    templateUrl: './simple-lookup-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class SimpleLookupModalComponent extends AppComponentBase {
    readonly service = input.required<ISimpleLookupServiceProxy>();
    readonly titleKey = input.required<string>();

    readonly modalSave = output<void>();

    readonly modal = viewChild<ModalDirective>('createOrEditModal');

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly item = signal<CreateOrEditSimpleLookupDto>(new CreateOrEditSimpleLookupDto());

    show(id?: number): void {
        this.active.set(true);

        if (!id) {
            this.item.set(new CreateOrEditSimpleLookupDto());
            this.modal().show();
        } else {
            this.service()
                .getForEdit(id)
                .subscribe((result) => {
                    this.item.set(result);
                    this.modal().show();
                });
        }
    }

    onShown(): void {
        document.getElementById('SimpleLookup_NameEn')?.focus();
    }

    save(): void {
        this.saving.set(true);
        this.service()
            .createOrEdit(this.item())
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

    updateField(field: keyof CreateOrEditSimpleLookupDto, value: any): void {
        const current = this.item();
        (current as any)[field] = value;
        const next = new CreateOrEditSimpleLookupDto();
        Object.assign(next, current);
        this.item.set(next);
    }
}
