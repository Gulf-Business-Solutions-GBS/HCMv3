import { Component, OnInit, inject, viewChild, signal } from '@angular/core';
import { ActivatedRoute, Params } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { ManagerComponent } from '@app/admin/dynamic-properties/dynamic-entity-properties/value/manager.component';
import { SubHeaderComponent } from '../../../../shared/common/sub-header/sub-header.component';
import { ManagerComponent as ManagerComponent_1 } from './manager.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

@Component({
    templateUrl: './dynamic-entity-property-value.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, ManagerComponent_1, LocalizePipe],
})
export class DynamicEntityPropertyValueComponent extends AppComponentBase implements OnInit {
    private _activatedRoute = inject(ActivatedRoute);

    readonly dynamicEntityPropertyValueManager = viewChild<ManagerComponent>('dynamicEntityPropertyValueManager');

    entityFullName = signal<string>('');
    entityId = signal<string>('');

    initialized = signal(false);

    ngOnInit() {
        this._activatedRoute.params.subscribe((params: Params) => {
            this.entityFullName.set(params['entityFullName']);
            this.entityId.set(params['rowId']);
            this.initialized.set(true);
        });
    }

    saveAll(): void {
        this.dynamicEntityPropertyValueManager().saveAll();
    }
}
