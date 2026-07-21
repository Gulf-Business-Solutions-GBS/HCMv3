import { Directive, ElementRef, OnInit, AfterViewInit, inject, input, effect } from '@angular/core';
import { AppLocalizationService } from '@app/shared/common/localization/app-localization.service';

@Directive({ selector: '[buttonBusy]' })
export class ButtonBusyDirective implements OnInit, AfterViewInit {
    private _element = inject(ElementRef);
    private _appLocalizationService = inject(AppLocalizationService);

    private _originalButtonInnerHtml: any;
    private _button: any;

    readonly busyText = input<string>(undefined);
    readonly buttonBusy = input<boolean>(false);

    constructor() {
        effect(() => {
            this.refreshState(this.buttonBusy());
        });
    }

    ngOnInit(): void {
        this._button = this._element.nativeElement;
    }

    ngAfterViewInit(): void {
        this._originalButtonInnerHtml = this._button.innerHTML;
    }

    refreshState(isBusy: boolean): void {
        if (!this._button) {
            return;
        }

        if (isBusy) {
            // disable button
            this._button.setAttribute('disabled', 'disabled');

            const busyText = this.busyText();
            this._button.innerHTML =
                '<i class="fa fa-spin fa-spinner"></i>' +
                '<span>' +
                (busyText ? busyText : this._appLocalizationService.l('ProcessingWithThreeDot')) +
                '</span>';

            this._button.setAttribute('_disabledBefore', true);
        } else {
            if (!this._button.getAttribute('_disabledBefore')) {
                return;
            }

            // enable button
            this._button.removeAttribute('disabled');
            this._button.innerHTML = this._originalButtonInnerHtml;
        }
    }
}
