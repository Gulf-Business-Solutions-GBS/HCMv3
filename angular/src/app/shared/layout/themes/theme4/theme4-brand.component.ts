import { Component, ViewEncapsulation, inject, input, OnInit, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LogoService } from '@app/shared/common/logo/logo.service';

@Component({
    templateUrl: './theme4-brand.component.html',
    selector: 'theme4-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [],
})
export class Theme4BrandComponent extends AppComponentBase implements OnInit, OnDestroy {
    private _logoService = inject(LogoService);
    private _destroy$ = new Subject<void>();

    readonly customStyle = input('h-55px');

    defaultSmallLogoUrl: string;
    tenantSmallLogoUrl: string;

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
        this.defaultSmallLogoUrl = this._logoService.getDefaultLogoUrl(null, true);
        this.tenantSmallLogoUrl = this._logoService.getLogoUrl(null, true);
    }
}
