import { Component, ViewEncapsulation, inject, OnInit, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LogoService } from '@app/shared/common/logo/logo.service';

@Component({
    templateUrl: './theme11-brand.component.html',
    selector: 'theme11-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [],
})
export class Theme11BrandComponent extends AppComponentBase implements OnInit, OnDestroy {
    private _logoService = inject(LogoService);
    private _destroy$ = new Subject<void>();

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

    private updateLogoUrls(): void {
        this.tenantLogoUrl = this._logoService.getLogoUrl();
        this.defaultLogoUrl = this._logoService.getDefaultLogoUrl();
    }
}
