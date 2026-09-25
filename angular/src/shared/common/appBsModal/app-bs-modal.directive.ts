import { Directive } from '@angular/core';
import { ModalDirective } from 'ngx-bootstrap/modal';

@Directive({
    selector: '[appBsModal]',
    exportAs: 'bs-modal',
})
export class AppBsModalDirective extends ModalDirective {
    /**
     * Fields worth starting in: the first one a person would type into. Hidden, disabled and read-only fields are
     * skipped, and so are the buttons -- landing on Save would be worse than landing nowhere.
     */
    private static readonly FirstFieldSelector = [
        'input:not([type=hidden]):not([type=checkbox]):not([type=radio]):not([disabled]):not([readonly])',
        'select:not([disabled])',
        'textarea:not([disabled]):not([readonly])',
        '.p-autocomplete-input',
        '.p-select',
    ].join(', ');

    /** Subscribed once, on the first show: the base class's constructor takes arguments this cannot supply. */
    private focusHooked = false;

    showElement(): void {
        // Hooked FIRST, before anything else can run. A dialog configured without animation raises "shown" from
        // inside the call below, so subscribing afterwards would miss it -- and the stacking step after it reaches
        // for a backdrop element, which a dialog without one does not have.
        if (!this.focusHooked) {
            this.focusHooked = true;

            // On shown, not on show: the dialog is in the page and its animation has finished, which is the first
            // moment a field can actually take the focus.
            this.onShown.subscribe(() => this.focusFirstField());
        }

        super.showElement();
        this.setZIndexes();
    }

    /**
     * Puts the cursor in the first field when a record opens, so typing can start straight away and it is obvious
     * where the cursor is. Without it the dialog opened with the focus still on the page behind, and the first Tab
     * went back to the header search rather than into the form.
     *
     * Retried a few times because a form often renders after its data arrives -- the dialog can be on screen a
     * moment before the fields are. A modal that focuses a particular field itself (several do, in their own
     * onShown) wins: this only acts while nothing inside the dialog has the focus.
     */
    private focusFirstField(attempt = 0): void {
        const modal = (this as any)._element?.nativeElement as HTMLElement;
        if (!modal || attempt > 8) {
            return;
        }

        window.setTimeout(() => {
            // Only a FIELD already holding the focus means somebody chose it. The dialog itself takes the focus
            // when it opens -- that is the state this is here to improve on, not a reason to stand down.
            const active = document.activeElement;
            if (active && modal.contains(active) && active.matches('input, select, textarea, [contenteditable]')) {
                return;
            }

            const first = Array.from(modal.querySelectorAll<HTMLElement>(AppBsModalDirective.FirstFieldSelector)).find(
                (field) => field.offsetParent !== null
            );

            if (!first) {
                this.focusFirstField(attempt + 1);
                return;
            }

            first.focus();

            // A text field starts with its content selected, so typing replaces what is there rather than adding
            // to it -- the usual expectation when a form opens on a value.
            if (first instanceof HTMLInputElement && first.type === 'text') {
                first.select();
            }
        }, 120);
    }

    setZIndexes(): void {
        let newZIndex = this.setAndGetModalZIndex();
        this.setBackDropZIndex(newZIndex - 1);
    }

    setAndGetModalZIndex(): number {
        let modalBaseZIndex = 1050;
        let modalsLength = document.querySelectorAll('.modal.fade.show').length;

        let newZIndex = modalBaseZIndex + modalsLength * 2;

        (this as any)._element.nativeElement.style.zIndex = newZIndex.toString();
        return newZIndex;
    }

    setBackDropZIndex(zindex: number): void {
        let modalBackdrops = document.querySelectorAll('.modal-backdrop.fade.show');

        // A dialog configured without a backdrop has none to stack, and reaching for one threw.
        const backdrop = modalBackdrops[modalBackdrops.length - 1] as HTMLElement | undefined;
        if (backdrop) {
            backdrop.style.zIndex = zindex.toString();
        }
    }
}
