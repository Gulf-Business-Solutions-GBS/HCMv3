import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditSiteAuditDto, SiteAuditsServiceProxy } from '@shared/service-proxies/service-proxies';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { SiteAuditDetailsComponent } from './details/site-audit-details.component';

@Component({
    templateUrl: './site-audit-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, TabsetComponent, TabDirective, LocalizePipe, LuxonFormatPipe, SiteAuditDetailsComponent],
})
export class SiteAuditDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _service = inject(SiteAuditsServiceProxy);

    readonly siteAuditId = signal<number | undefined>(undefined);
    readonly siteAudit = signal<CreateOrEditSiteAuditDto | undefined>(undefined);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.siteAuditId.set(id);
                this.loadSiteAudit(id);
            }
        });
    }

    private loadSiteAudit(id: number): void {
        this._service.getSiteAuditForEdit(id).subscribe((result) => {
            this.siteAudit.set(result.siteAudit);
        });
    }
}
