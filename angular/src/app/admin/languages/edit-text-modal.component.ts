import { Component, inject, output, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LanguageServiceProxy, UpdateLanguageTextInput } from '@shared/service-proxies/service-proxies';
import { find as _find } from 'lodash-es';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'editTextModal',
    templateUrl: './edit-text-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, NgClass, ButtonBusyDirective, LocalizePipe],
})
export class EditTextModalComponent extends AppComponentBase {
    private _languageService = inject(LanguageServiceProxy);

    readonly modal = viewChild<ModalDirective>('modal');

    readonly modalSave = output<any>();

    readonly model = signal<UpdateLanguageTextInput>(new UpdateLanguageTextInput());

    readonly baseText = signal<string>('');
    readonly baseLanguage = signal<abp.localization.ILanguageInfo | undefined>(undefined);
    readonly targetLanguage = signal<abp.localization.ILanguageInfo | undefined>(undefined);

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);

    show(
        baseLanguageName: string,
        targetLanguageName: string,
        sourceName: string,
        key: string,
        baseText: string,
        targetText: string
    ): void {
        const newModel = new UpdateLanguageTextInput();
        newModel.sourceName = sourceName;
        newModel.key = key;
        newModel.languageName = targetLanguageName;
        newModel.value = targetText;
        this.model.set(newModel);

        this.baseText.set(baseText);
        this.baseLanguage.set(_find(abp.localization.languages, (l) => l.name === baseLanguageName));
        this.targetLanguage.set(_find(abp.localization.languages, (l) => l.name === targetLanguageName));

        this.active.set(true);

        this.modal().show();
    }

    onShown(): void {
        document.getElementById('TargetLanguageDisplayName').focus();
    }

    save(): void {
        this.saving.set(true);
        this._languageService
            .updateLanguageText(this.model())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
                this.modalSave.emit(null);
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    updateModelValue(value: string): void {
        const currentModel = this.model();
        const newModel = new UpdateLanguageTextInput();
        Object.assign(newModel, currentModel);
        newModel.value = value;
        this.model.set(newModel);
    }

    private findLanguage(name: string): abp.localization.ILanguageInfo {
        return _find(abp.localization.languages, (l) => l.name === name);
    }
}
