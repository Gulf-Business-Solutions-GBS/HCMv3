import { Component, ViewEncapsulation, inject, input, OnInit, OnDestroy } from '@angular/core';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LogoService } from '@app/shared/common/logo/logo.service';

@Component({
    templateUrl: './theme7-brand.component.html',
    selector: 'theme7-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [],
})
export class Theme7BrandComponent extends AppComponentBase implements OnInit, OnDestroy {
    private _logoService = inject(LogoService);
    private _destroy$ = new Subject<void>();

    readonly imageClass = input('h-35px');

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
        this.defaultLogoUrl = this._logoService.getDefaultLogoUrl();
        this.tenantLogoUrl = this._logoService.getLogoUrl();
    }
}
