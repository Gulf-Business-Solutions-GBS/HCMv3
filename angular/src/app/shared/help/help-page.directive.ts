import { Directive, ElementRef, OnDestroy, effect, inject, input } from '@angular/core';
import { HelpService } from './help.service';

/**
 * Marks a screen as documented and hands the help drawer the element to read.
 *
 *   <div [helpPage]="'pa.employee.personal-info'"> … </div>
 *
 * The element matters as much as the id: everything the drawer knows about which fields are
 * currently on screen comes from querying inside it (DESIGN.md §8.1).
 *
 * The id may change while the screen is open -- a screen that hosts more than one documented form
 * names whichever is in front -- and the drawer follows it.
 */
@Directive({ selector: '[helpPage]' })
export class HelpPageDirective implements OnDestroy {
    readonly helpPage = input.required<string>();

    private _el: ElementRef<HTMLElement> = inject(ElementRef);
    private _help = inject(HelpService);

    constructor() {
        effect(() => this._help.registerPage(this.helpPage(), this._el.nativeElement));
    }

    ngOnDestroy(): void {
        this._help.unregisterPage(this._el.nativeElement);
    }
}
