import { Component, ElementRef, inject, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EditionServiceProxy, UpdateEditionDto } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { FeatureTreeComponent } from '../shared/feature-tree.component';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';

@Component({
    selector: 'editEditionModal',
    templateUrl: './edit-edition-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        TabsetComponent,
        TabDirective,
        NgClass,
        ValidationMessagesComponent,
        FeatureTreeComponent,
        ButtonBusyDirective,
    ],
})
export class EditEditionModalComponent extends AppComponentBase {
    private _editionService = inject(EditionServiceProxy);

    readonly modal = viewChild<ModalDirective>('editModal');
    readonly featureTree = viewChild<FeatureTreeComponent>('featureTree');
    readonly editionNameInput = viewChild<ElementRef>('editionNameInput');

    readonly modalSave = output<void>();

    readonly active = signal(false);
    readonly saving = signal(false);

    readonly edition = signal<UpdateEditionDto>(new UpdateEditionDto());

    show(editionId: number): void {
        this.active.set(true);

        this._editionService.getEditionForEdit(editionId).subscribe((editionResult) => {
            this.featureTree().setEditData(editionResult);
            const newEdition = new UpdateEditionDto();
            newEdition.edition = editionResult.edition;
            this.edition.set(newEdition);
            this.modal().show();
        });
    }

    onShown(): void {
        const input = this.editionNameInput();
        if (input?.nativeElement) {
            input.nativeElement.focus();
        }
    }

    updateEditionDisplayName(value: string): void {
        const current = this.edition();
        current.edition.displayName = value;
        const newEdition = new UpdateEditionDto();
        Object.assign(newEdition, current);
        this.edition.set(newEdition);
    }

    save(): void {
        const featureTree = this.featureTree();
        if (!featureTree.areAllValuesValid()) {
            this.message.warn(this.l('InvalidFeaturesWarning'));
            return;
        }

        const currentEdition = this.edition();
        const input = new UpdateEditionDto();
        input.edition = currentEdition.edition;
        input.featureValues = featureTree.getGrantedFeatures();

        this.saving.set(true);
        this._editionService
            .updateEdition(input)
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
