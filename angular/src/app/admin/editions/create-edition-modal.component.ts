import { Component, ElementRef, inject, output, signal, viewChild } from '@angular/core';
import { AppEditionExpireAction } from '@shared/AppEnums';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    ComboboxItemDto,
    CommonLookupServiceProxy,
    CreateEditionDto,
    EditionServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { FeatureTreeComponent } from '../shared/feature-tree.component';
import { finalize, switchMap } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';
import { NgClass } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { ValidationMessagesComponent } from '../../../shared/utils/validation-messages.component';
import { IMaskDirective } from 'angular-imask';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'createEditionModal',
    templateUrl: './create-edition-modal.component.html',
    imports: [
        AppBsModalDirective,
        FormsModule,
        TabsetComponent,
        TabDirective,
        ValidationMessagesComponent,
        NgClass,
        IMaskDirective,
        FeatureTreeComponent,
        ButtonBusyDirective,
        LocalizePipe,
    ],
})
export class CreateEditionModalComponent extends AppComponentBase {
    private _editionService = inject(EditionServiceProxy);
    private _commonLookupService = inject(CommonLookupServiceProxy);

    readonly modal = viewChild<ModalDirective>('createModal');
    readonly featureTree = viewChild<FeatureTreeComponent>('featureTree');
    readonly editionNameInput = viewChild<ElementRef>('editionNameInput');

    readonly modalSave = output<void>();

    readonly active = signal(false);
    readonly saving = signal(false);
    readonly currencyMask = {
        mask: Number,
        scale: 2,
        signed: true,
        radix: '.',
    };

    readonly edition = signal<CreateEditionDto>(new CreateEditionDto());
    readonly expiringEditions = signal<ComboboxItemDto[]>([]);

    readonly expireAction = signal<AppEditionExpireAction>(AppEditionExpireAction.DeactiveTenant);
    readonly expireActionEnum: typeof AppEditionExpireAction = AppEditionExpireAction;
    readonly isFree = signal<boolean>(true);
    readonly isTrialActive = signal<boolean>(false);
    readonly isWaitingDayActive = signal<boolean>(false);

    show(): void {
        this.active.set(true);
        this.edition.set(new CreateEditionDto());
        this.isFree.set(true);
        this.isTrialActive.set(false);
        this.isWaitingDayActive.set(false);
        this.expireAction.set(AppEditionExpireAction.DeactiveTenant);

        this._commonLookupService
            .getEditionsForCombobox(true)
            .pipe(
                switchMap((editionsResult) => {
                    const editions = [
                        new ComboboxItemDto({ value: null, displayText: this.l('NotAssigned'), isSelected: true }),
                        ...editionsResult.items,
                    ];
                    this.expiringEditions.set(editions);
                    return this._editionService.getEditionForEdit(undefined);
                })
            )
            .subscribe((editionResult) => {
                this.featureTree().setEditData(editionResult);
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
        this.notifyEditionChanged(current);
    }

    updateMonthlyPrice(value: number): void {
        const current = this.edition();
        current.edition.monthlyPrice = value;
        this.notifyEditionChanged(current);
    }

    updateAnnualPrice(value: number): void {
        const current = this.edition();
        current.edition.annualPrice = value;
        this.notifyEditionChanged(current);
    }

    updateTrialDayCount(value: number): void {
        const current = this.edition();
        current.edition.trialDayCount = value;
        this.notifyEditionChanged(current);
    }

    updateWaitingDayAfterExpire(value: number): void {
        const current = this.edition();
        current.edition.waitingDayAfterExpire = value;
        this.notifyEditionChanged(current);
    }

    updateExpiringEditionId(value: number): void {
        const current = this.edition();
        current.edition.expiringEditionId = value;
        this.notifyEditionChanged(current);
    }

    resetPrices(): void {
        const current = this.edition();
        current.edition.annualPrice = undefined;
        current.edition.monthlyPrice = undefined;
        this.notifyEditionChanged(current);
    }

    removeExpiringEdition(): void {
        const current = this.edition();
        current.edition.expiringEditionId = null;
        this.notifyEditionChanged(current);
    }

    save(): void {
        const featureTree = this.featureTree();
        if (!featureTree.areAllValuesValid()) {
            this.message.warn(this.l('InvalidFeaturesWarning'));
            return;
        }

        const currentEdition = this.edition();
        const input = new CreateEditionDto();
        input.edition = currentEdition.edition;
        input.featureValues = featureTree.getGrantedFeatures();

        if (!this.isTrialActive()) {
            input.edition.trialDayCount = null;
        }

        this.saving.set(true);
        this._editionService
            .createEdition(input)
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

    private notifyEditionChanged(current: CreateEditionDto): void {
        const newEdition = new CreateEditionDto();
        Object.assign(newEdition, current);
        this.edition.set(newEdition);
    }
}
