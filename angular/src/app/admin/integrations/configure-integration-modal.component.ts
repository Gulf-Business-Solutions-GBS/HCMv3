import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    IntegrationCenterServiceProxy,
    IntegrationDetailDto,
    IntegrationFieldDto,
    IntegrationFieldType,
    SaveIntegrationSettingsInput,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { RouterLink } from '@angular/router';

/**
 * The configuration form for any integration.
 *
 * There is deliberately no integration-specific code here. The server sends the field list -- name,
 * label, type, whether it is required, whether it is a secret -- and this renders it. That is what makes
 * adding an integration a backend-only change: no new component, no new route, no edit to this file.
 */
@Component({
    selector: 'configureIntegrationModal',
    templateUrl: './configure-integration-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        ButtonBusyDirective,
        LocalizePipe,
        LuxonFormatPipe,
        PermissionPipe,
        RouterLink,
    ],
})
export class ConfigureIntegrationModalComponent extends AppComponentBase {
    private _service = inject(IntegrationCenterServiceProxy);

    readonly modal = viewChild<ModalDirective>('configureModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly loading = signal<boolean>(false);
    readonly detail = signal<IntegrationDetailDto>(null);

    /** Field key to the value bound to its input. Strings throughout, including booleans. */
    readonly values = signal<Record<string, string>>({});

    readonly fieldType = IntegrationFieldType;

    show(key: string): void {
        this.active.set(true);
        this.loading.set(true);
        this.detail.set(null);
        this.values.set({});

        this._service
            .getIntegrationDetail(key)
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((detail) => {
                this.detail.set(detail);
                this.values.set(this.buildInitialValues(detail.fields ?? []));
                this.modal().show();
            });
    }

    /**
     * A secret starts blank however it is stored: the server never sends it, and an empty box submitted
     * back means "keep what is saved". Everything else starts from its stored value, or from the field's
     * default when nothing is stored yet, so a fresh form arrives sensibly filled in.
     */
    private buildInitialValues(fields: IntegrationFieldDto[]): Record<string, string> {
        const values: Record<string, string> = {};

        for (const field of fields) {
            if (field.isSecret) {
                values[field.key] = '';
                continue;
            }

            values[field.key] = field.value ?? field.defaultValue ?? '';
        }

        return values;
    }

    setValue(key: string, value: string): void {
        this.values.update((current) => ({ ...current, [key]: value }));
    }

    setBoolean(key: string, checked: boolean): void {
        this.setValue(key, checked ? 'true' : 'false');
    }

    isChecked(key: string): boolean {
        return this.values()[key] === 'true';
    }

    inputType(field: IntegrationFieldDto): string {
        switch (field.type) {
            case IntegrationFieldType.Password:
                return 'password';
            case IntegrationFieldType.Number:
                return 'number';
            case IntegrationFieldType.Email:
                return 'email';
            case IntegrationFieldType.Url:
                return 'url';
            default:
                return 'text';
        }
    }

    /**
     * Placeholder for a secret that is already saved. Saying so matters: an empty box otherwise reads as
     * "no password set", and someone would retype a credential they did not need to.
     */
    secretPlaceholder(field: IntegrationFieldDto): string {
        return field.hasStoredValue ? this.l('IntegrationSecretStored') : '';
    }

    save(): void {
        const detail = this.detail();
        if (!detail) {
            return;
        }

        const input = new SaveIntegrationSettingsInput();
        input.key = detail.tile.key;
        input.values = this.values();

        this.saving.set(true);

        this._service
            .saveSettings(input)
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
}
