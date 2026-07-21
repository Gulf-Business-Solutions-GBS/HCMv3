import { Component, inject, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    MoveTenantsToAnotherEditionDto,
    ComboboxItemDto,
    CommonLookupServiceProxy,
    EditionServiceProxy,
} from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { AppConsts } from '@shared/AppConsts';
import { finalize } from 'rxjs/operators';
import { filter as _filter } from 'lodash-es';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';

import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'moveTenantsToAnotherEditionModal',
    templateUrl: './move-tenants-to-another-edition-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, RouterLink, ButtonBusyDirective, LocalizePipe],
})
export class MoveTenantsToAnotherEditionModalComponent extends AppComponentBase {
    private _editionService = inject(EditionServiceProxy);
    private _commonLookupService = inject(CommonLookupServiceProxy);

    readonly modal = viewChild<ModalDirective>('editModal');

    readonly active = signal<boolean>(false);
    readonly saving = signal<boolean>(false);
    readonly appBaseUrl = signal<string>('');
    readonly tenantCount = signal<number>(0);

    readonly moveTenantsInput = signal<MoveTenantsToAnotherEditionDto>(new MoveTenantsToAnotherEditionDto());
    readonly targetEditions = signal<ComboboxItemDto[]>([]);

    show(editionId: number): void {
        this.active.set(true);

        const input = new MoveTenantsToAnotherEditionDto();
        input.sourceEditionId = editionId;
        this.moveTenantsInput.set(input);

        this._commonLookupService.getEditionsForCombobox(undefined).subscribe((editionsResult) => {
            this.targetEditions.set(editionsResult.items.filter((item) => item.value !== editionId.toString()));
            this.modal().show();
        });

        this._editionService.getTenantCount(editionId).subscribe((editionCountResult) => {
            this.tenantCount.set(editionCountResult);
            this.appBaseUrl.set(AppConsts.appBaseUrl);
        });
    }

    save(): void {
        if (!this.canChangeEdition()) {
            abp.message.warn(this.l('SameEditionChangeErrorMessage'));
            return;
        }

        this.saving.set(true);
        this._editionService
            .moveTenantsToAnotherEdition(this.moveTenantsInput())
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('MoveTenantsToAnotherEditionStartedNotification'));
                this.close();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }

    updateTargetEditionId(value: string): void {
        const currentInput = this.moveTenantsInput();
        const newInput = new MoveTenantsToAnotherEditionDto();
        newInput.sourceEditionId = currentInput.sourceEditionId;
        newInput.targetEditionId = value ? parseInt(value) : undefined;
        this.moveTenantsInput.set(newInput);
    }

    private canChangeEdition(): boolean {
        const input = this.moveTenantsInput();
        return input.targetEditionId.toString() !== input.sourceEditionId.toString();
    }
}
