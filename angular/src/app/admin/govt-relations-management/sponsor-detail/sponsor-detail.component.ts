import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { CreateOrEditSponsorDto, SponsorsServiceProxy } from '@shared/service-proxies/service-proxies';
import { TabsetComponent, TabDirective } from 'ngx-bootstrap/tabs';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SponsorJobTitlesComponent } from './job-titles/sponsor-job-titles.component';
import { SponsorSignatoriesComponent } from './signatories/sponsor-signatories.component';
import { SponsorGovtRepresentativesComponent } from './govt-representatives/sponsor-govt-representatives.component';
import { SponsorLicenseInfosComponent } from './license-infos/sponsor-license-infos.component';
import { SponsorBanksComponent } from './banks/sponsor-banks.component';

@Component({
    templateUrl: './sponsor-detail.component.html',
    animations: [appModuleAnimation()],
    imports: [
        SubHeaderComponent,
        TabsetComponent,
        TabDirective,
        LocalizePipe,
        SponsorJobTitlesComponent,
        SponsorSignatoriesComponent,
        SponsorGovtRepresentativesComponent,
        SponsorLicenseInfosComponent,
        SponsorBanksComponent,
    ],
})
export class SponsorDetailComponent extends AppComponentBase implements OnInit {
    private _route = inject(ActivatedRoute);
    private _sponsorsService = inject(SponsorsServiceProxy);

    readonly sponsorId = signal<number | undefined>(undefined);
    readonly sponsor = signal<CreateOrEditSponsorDto | undefined>(undefined);

    ngOnInit(): void {
        this._route.paramMap.subscribe((params) => {
            const id = Number(params.get('id'));
            if (id) {
                this.sponsorId.set(id);
                this.loadSponsor(id);
            }
        });
    }

    private loadSponsor(id: number): void {
        this._sponsorsService.getSponsorForEdit(id).subscribe((result) => {
            this.sponsor.set(result.sponsor);
        });
    }
}
