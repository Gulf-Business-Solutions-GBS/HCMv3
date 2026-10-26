import { Component, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    CreateOrEditCVLanguageDto,
    CVLanguagesServiceProxy,
    GetAllSimpleLookupsInput,
    LanguageInfosServiceProxy,
    ProficiencyType,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '@shared/common/appBsModal/app-bs-modal.directive';
import { FormsModule } from '@angular/forms';
import { ValidationMessagesComponent } from '@shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '@shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createOrEditCvLanguageModal',
    templateUrl: './create-or-edit-cv-language-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, ValidationMessagesComponent, ButtonBusyDirective, LocalizePipe],
})
export class CreateOrEditCvLanguageModalComponent extends AppComponentBase {
    private _service = inject(CVLanguagesServiceProxy);
    private _languageInfosService = inject(LanguageInfosServiceProxy);

    readonly modal = viewChild<ModalDirective>('createOrEditModal');
    readonly modalSave = output<void>();

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly record = signal<CreateOrEditCVLanguageDto>(new CreateOrEditCVLanguageDto());

    readonly languageInfos = signal<SimpleLookupDto[]>([]);

    readonly proficiencyLevels = [
        { value: ProficiencyType.Novice, name: 'Novice' },
        { value: ProficiencyType.Intermediate, name: 'Intermediate' },
        { value: ProficiencyType.Advanced, name: 'Advanced' },
    ];

    show(applicantCVId: number, id?: number): void {
        this.active.set(true);

        this._languageInfosService
            .getAll(
                new GetAllSimpleLookupsInput({
                    maxResultCount: 1000,
                    skipCount: 0,
                    sorting: undefined,
                    filter: undefined,
                })
            )
            .subscribe((result) => this.languageInfos.set(result.items));

        if (!id) {
            const record = new CreateOrEditCVLanguageDto();
            record.applicantCVId = applicantCVId;
            record.proficiency = ProficiencyType.Novice;
            this.record.set(record);
            this.modal().show();
        } else {
            this._service.getCVLanguageForEdit(id).subscribe((result) => {
                this.record.set(result.cvLanguage);
                this.modal().show();
            });
        }
    }

    onShown(): void {
        document.getElementById('CVLanguage_LanguageInfoId')?.focus();
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

    updateField(field: keyof CreateOrEditCVLanguageDto, value: any): void {
        const current = this.record();
        (current as any)[field] = value;
        const next = new CreateOrEditCVLanguageDto();
        Object.assign(next, current);
        this.record.set(next);
    }
}
