import { NgControl } from '@angular/forms';
import { Directive, ElementRef, HostListener, inject } from '@angular/core';

@Directive({ selector: 'input[nullValue]' })
export class NullDefaultValueDirective {
    private el = inject(ElementRef);
    private control = inject(NgControl);

    @HostListener('input', ['$event.target'])
    onEvent(target: HTMLInputElement) {
        this.control.viewToModelUpdate(target.value === '' ? null : target.value);
    }
}
