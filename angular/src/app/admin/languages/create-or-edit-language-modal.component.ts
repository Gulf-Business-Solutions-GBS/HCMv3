import { Component, ElementRef, inject, output, signal, viewChild, computed } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ApplicationLanguageEditDto,
    CreateOrUpdateLanguageInput,
    LanguageServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { SelectItem } from 'primeng/api';
import { map as _map } from 'lodash-es';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditLanguageModal',
    templateUrl: './create-or-edit-language-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        SelectModule,
        ValidationMessagesComponent,
        NgClass,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateOrEditLanguageModalComponent extends AppComponentBase {
    private readonly _languageService = inject(LanguageServiceProxy);
    private readonly _language = signal<ApplicationLanguageEditDto>(new ApplicationLanguageEditDto());

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly languageCombobox = viewChild<ElementRef>('languageCombobox');
    readonly iconCombobox = viewChild<ElementRef>('iconCombobox');

    readonly modalSave = output<void>();

    readonly active = signal(false);
    readonly saving = signal(false);

    readonly languageNamesSelectItems = signal<SelectItem[]>([]);
    readonly flagsSelectItems = signal<SelectItem[]>([]);

    readonly languageId = computed(() => this._language().id);
    readonly languageName = computed(() => this._language().name);

    get language(): ApplicationLanguageEditDto {
        return this._language();
    }

    set language(value: ApplicationLanguageEditDto) {
        this._language.set(value);
    }

    show(languageId?: number): void {
        this._languageService.getLanguageForEdit(languageId).subscribe((result) => {
            this._language.set(result.language);

            this.languageNamesSelectItems.set(
                _map(result.languageNames, function (language) {
                    return {
                        label: language.displayText,
                        value: language.value,
                    };
                })
            );

            this.flagsSelectItems.set(
                _map(result.flags, function (flag) {
                    return {
                        label: flag.displayText,
                        value: flag.value,
                    };
                })
            );

            if (!languageId) {
                this._language.update((lang) => {
                    lang.isEnabled = true;
                    return lang;
                });
            }

            this.active.set(true);
            this.modal().show();
        });
    }

    save(): void {
        let input = new CreateOrUpdateLanguageInput();
        input.language = this._language();

        this.saving.set(true);
        this._languageService
            .createOrUpdateLanguage(input)
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

    updateLanguageName(name: string): void {
        const currentLanguage = this.language;
        const newLanguage = new ApplicationLanguageEditDto();
        Object.assign(newLanguage, currentLanguage);
        newLanguage.name = name;
        this.language = newLanguage;
    }

    updateLanguageIcon(icon: string): void {
        const currentLanguage = this.language;
        const newLanguage = new ApplicationLanguageEditDto();
        Object.assign(newLanguage, currentLanguage);
        newLanguage.icon = icon;
        this.language = newLanguage;
    }

    updateLanguageIsEnabled(isEnabled: boolean): void {
        const currentLanguage = this.language;
        const newLanguage = new ApplicationLanguageEditDto();
        Object.assign(newLanguage, currentLanguage);
        newLanguage.isEnabled = isEnabled;
        this.language = newLanguage;
    }
}
