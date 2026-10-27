import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditGatePassDto, GatePassesServiceProxy } from '@shared/service-proxies/service-proxies';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { GatePassMaterialsComponent } from './materials/gate-pass-materials.component';
import { GatePassPersonsComponent } from './persons/gate-pass-persons.component';
import { GatePassVehiclesComponent } from './vehicles/gate-pass-vehicles.component';

@Component({
    templateUrl: './gate-pass-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        LocalizePipe,
        GatePassMaterialsComponent,
        GatePassPersonsComponent,
        GatePassVehiclesComponent,
    ],
})
export class GatePassDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _gatePassesService = inject(GatePassesServiceProxy);

    readonly gatePassId = signal<number | undefined>(undefined);
    readonly gatePass = signal<CreateOrEditGatePassDto | undefined>(undefined);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.gatePassId.set(id);
                this.loadGatePass(id);
            }
        });
    }

    private loadGatePass(id: number): void {
        this._gatePassesService.getGatePassForEdit(id).subscribe((result) => {
            this.gatePass.set(result.gatePass);
        });
    }
}
