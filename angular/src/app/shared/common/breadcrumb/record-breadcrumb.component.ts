import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';

export interface BreadcrumbStep {
    /** Already-localized text, or a localization key when `isKey` is set. */
    label: string;

    /** True when `label` is a localization key rather than literal text. */
    isKey?: boolean;

    /** Omitted for the final step, which is where you already are. */
    route?: string;
}

/**
 * Breadcrumb for RECORD screens only.
 *
 * DELIBERATELY NOT ON EVERY SCREEN. On a list screen a breadcrumb would repeat the sub-header title
 * that is already there -- a row of chrome saying nothing. It earns its place only where you are
 * several levels into a record and the page title alone cannot tell you where you are:
 *
 *     Personnel Administration > Ahmad AlSalem > Loans
 *
 * The last step carries no route because it is the page you are on; making it a link that reloads the
 * same page is a small lie about what clicking does.
 */
@Component({
    selector: 'record-breadcrumb',
    templateUrl: './record-breadcrumb.component.html',
    styleUrl: './record-breadcrumb.component.css',
    imports: [RouterLink, LocalizePipe],
})
export class RecordBreadcrumbComponent extends AppComponentBase {
    readonly steps = input<BreadcrumbStep[]>([]);
}
