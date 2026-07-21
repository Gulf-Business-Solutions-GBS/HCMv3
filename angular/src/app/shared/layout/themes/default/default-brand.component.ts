import { Component, ViewEncapsulation } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import { DefaultLogoComponent } from './default-logo.component';

@Component({
    templateUrl: './default-brand.component.html',
    selector: 'default-brand',
    encapsulation: ViewEncapsulation.None,
    imports: [DefaultLogoComponent],
})
export class DefaultBrandComponent extends AppComponentBase {}
