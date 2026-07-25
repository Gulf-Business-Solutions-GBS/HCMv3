import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditFirstAidBoxDto, FirstAidBoxesServiceProxy } from '@shared/service-proxies/service-proxies';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { FirstAidBoxDetailsComponent } from './details/first-aid-box-details.component';

@Component({
    templateUrl: './first-aid-box-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, TabsetComponent, TabDirective, LocalizePipe, FirstAidBoxDetailsComponent],
})
export class FirstAidBoxDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _service = inject(FirstAidBoxesServiceProxy);

    readonly firstAidBoxId = signal<number | undefined>(undefined);
    readonly firstAidBox = signal<CreateOrEditFirstAidBoxDto | undefined>(undefined);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.firstAidBoxId.set(id);
                this.loadFirstAidBox(id);
            }
        });
    }

    private loadFirstAidBox(id: number): void {
        this._service.getFirstAidBoxForEdit(id).subscribe((result) => {
            this.firstAidBox.set(result.firstAidBox);
        });
    }
}
