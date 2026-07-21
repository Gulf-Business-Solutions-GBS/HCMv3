import { AfterViewInit, Directive, ElementRef, inject } from '@angular/core';

@Directive({ selector: '[autoFocus]' })
export class AutoFocusDirective implements AfterViewInit {
    private _element = inject(ElementRef);

    ngAfterViewInit(): void {
        this._element.nativeElement.focus();
    }
}
