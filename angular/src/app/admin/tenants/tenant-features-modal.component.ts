import { Component, inject, viewChild, signal } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { EntityDto, TenantServiceProxy, UpdateTenantFeaturesInput } from '@shared/service-proxies/service-proxies';
import { ModalDirective } from 'ngx-bootstrap/modal';
import { FeatureTreeComponent } from '../shared/feature-tree.component';
import { finalize } from 'rxjs/operators';
import { AppBsModalDirective } from '../../../shared/common/appBsModal/app-bs-modal.directive';

import { FormsModule } from '@angular/forms';
import { ButtonBusyDirective } from '../../../shared/utils/button-busy.directive';
import { Tooltip } from 'primeng/tooltip';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    selector: 'tenantFeaturesModal',
    templateUrl: './tenant-features-modal.component.html',
    imports: [AppBsModalDirective, FormsModule, FeatureTreeComponent, ButtonBusyDirective, Tooltip, LocalizePipe],
})
export class TenantFeaturesModalComponent extends AppComponentBase {
    private _tenantService = inject(TenantServiceProxy);

    readonly modal = viewChild<ModalDirective>('tenantFeaturesModal');
    readonly featureTree = viewChild<FeatureTreeComponent>('featureTree');

    readonly active = signal(false);
    readonly saving = signal(false);

    readonly resettingFeatures = signal(false);
    tenantId: number;
    tenantName: string;
    featureEditData: any = null;

    show(tenantId: number, tenantName: string): void {
        this.tenantId = tenantId;
        this.tenantName = tenantName;
        this.active.set(true);
        this.loadFeatures();
        this.modal().show();
    }

    loadFeatures(): void {
        const self = this;
        self._tenantService.getTenantFeaturesForEdit(this.tenantId).subscribe((result) => {
            self.featureTree().setEditData(result);
        });
    }

    save(): void {
        const featureTree = this.featureTree();
        if (!featureTree.areAllValuesValid()) {
            this.message.warn(this.l('InvalidFeaturesWarning'));
            return;
        }

        const input = new UpdateTenantFeaturesInput();
        input.id = this.tenantId;
        input.featureValues = featureTree.getGrantedFeatures();

        this.saving.set(true);
        this._tenantService
            .updateTenantFeatures(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('SavedSuccessfully'));
                this.close();
            });
    }

    resetFeatures(): void {
        const input = new EntityDto();
        input.id = this.tenantId;

        this.resettingFeatures.set(true);
        this._tenantService
            .resetTenantSpecificFeatures(input)
            .pipe(finalize(() => this.resettingFeatures.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('ResetSuccessfully'));
                this.loadFeatures();
            });
    }

    close(): void {
        this.active.set(false);
        this.modal().hide();
    }
}
