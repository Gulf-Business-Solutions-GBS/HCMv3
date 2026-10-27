import { Component, ViewEncapsulation, DOCUMENT, inject, input, OnInit, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LogoService } from '@app/shared/common/logo/logo.service';

@Component({
    templateUrl: './theme3-brand.component.html',
    selector: 'theme3-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [],
})
export class Theme3BrandComponent extends AppComponentBase implements OnInit, OnDestroy {
    private document = inject<Document>(DOCUMENT);
    private _logoService = inject(LogoService);
    private _destroy$ = new Subject<void>();

    readonly logoSize = input('');

    defaultLogoUrl: string;
    tenantLogoUrl: string;

    constructor() {
        super();
        this.updateLogoUrls();
    }

    ngOnInit(): void {
        this._logoService.logoChanged$.pipe(takeUntil(this._destroy$)).subscribe(() => {
            this.updateLogoUrls();
        });
    }

    ngOnDestroy(): void {
        this._destroy$.next();
        this._destroy$.complete();
    }

    clickTopbarToggle(): void {
        this.document.body.classList.toggle('topbar-mobile-on');
    }

    clickLeftAsideHideToggle(): void {
        this.document.body.classList.toggle('header-menu-wrapper-on');
    }

    private updateLogoUrls(): void {
        this.defaultLogoUrl = this._logoService.getDefaultLogoUrl();
        this.tenantLogoUrl = this._logoService.getLogoUrl();
    }
}
