import { Directive, ElementRef, OnDestroy, OnInit, inject, input } from '@angular/core';
import { HelpService } from './help.service';

/**
 * Marks a screen as documented and hands the help drawer the element to read.
 *
 *   <div [helpPage]="'pa.employee.personal-info'"> … </div>
 *
 * The element matters as much as the id: everything the drawer knows about which fields are
 * currently on screen comes from querying inside it (DESIGN.md §8.1).
 */
@Directive({ selector: '[helpPage]' })
export class HelpPageDirective implements OnInit, OnDestroy {
    readonly helpPage = input.required<string>();

    private _el: ElementRef<HTMLElement> = inject(ElementRef);
    private _help = inject(HelpService);

    ngOnInit(): void {
        this._help.registerPage(this.helpPage(), this._el.nativeElement);
    }

    ngOnDestroy(): void {
        this._help.unregisterPage(this._el.nativeElement);
    }
}
