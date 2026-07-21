import { Component, ViewEncapsulation, inject, input, OnInit, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LogoService } from '@app/shared/common/logo/logo.service';

@Component({
    templateUrl: './theme13-brand.component.html',
    selector: 'theme13-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [],
})
export class Theme13BrandComponent extends AppComponentBase implements OnInit, OnDestroy {
    private _logoService = inject(LogoService);
    private _destroy$ = new Subject<void>();

    readonly anchorClass = input('');
    readonly imageClass = input('h-25px logo');

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

    triggerAsideToggleClickEvent(): void {
        abp.event.trigger('app.kt_aside_toggler.onClick');
    }

    private updateLogoUrls(): void {
        this.defaultLogoUrl = this._logoService.getDefaultLogoUrl('dark');
        this.tenantLogoUrl = this._logoService.getLogoUrl('dark');
    }
}
