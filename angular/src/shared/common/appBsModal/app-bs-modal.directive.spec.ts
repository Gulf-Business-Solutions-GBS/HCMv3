import { Component, signal, viewChild } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { AppBsModalDirective } from './app-bs-modal.directive';

/**
 * A stand-in for a real add/edit dialog: the same markup every screen uses -- appBsModal on the outer div, the form
 * only rendered once the dialog is active, a text field first and the buttons last.
 */
@Component({
    standalone: true,
    imports: [AppBsModalDirective, FormsModule],
    template: `
        <div appBsModal #modal="bs-modal" class="modal fade" tabindex="-1" role="dialog" [config]="config">
            <div class="modal-dialog">
                <div class="modal-content">
                    @if (active()) {
                        <form>
                            <input type="hidden" name="id" />
                            <input id="Code" type="text" class="form-control" name="code" [(ngModel)]="code" />
                            <input id="Name" type="text" class="form-control" name="name" [(ngModel)]="name" />
                            <button type="submit">Save</button>
                        </form>
                    }
                </div>
            </div>
        </div>
    `,
})
class HostComponent {
    readonly modal = viewChild.required<AppBsModalDirective>('modal');

    /** Held, not written inline: a fresh object each pass reads as a changed value and trips Angular's dev check. */
    readonly config = { backdrop: 'static' as const, animated: false };

    /** A signal, as the real dialogs use: this application runs without Zone.js, so a plain field is not tracked. */
    readonly active = signal(false);
    code = 'C1';
    name = 'First record';
}

describe('AppBsModalDirective', () => {
    let fixture: ComponentFixture<HostComponent>;
    let host: HostComponent;

    beforeEach(async () => {
        await TestBed.configureTestingModule({ imports: [HostComponent] }).compileComponents();

        fixture = TestBed.createComponent(HostComponent);
        host = fixture.componentInstance;

        // Left to update itself, as it does in the application: the dialog rewrites its own tabindex while opening,
        // and a hand-driven change-detection pass reports that as a changed expression.
        fixture.autoDetectChanges(true);
        await fixture.whenStable();
    });

    afterEach(() => {
        fixture.destroy();
    });

    /** The dialog focuses its field a moment after it is shown; this waits for that to happen. */
    function settle(): Promise<void> {
        return new Promise((resolve) => setTimeout(resolve, 600));
    }

    /** Opens the dialog the way a screen does: make it active, then show it. */
    async function open(): Promise<void> {
        host.active.set(true);
        await fixture.whenStable();
        host.modal().show();
    }

    it('puts the cursor in the first field when the dialog opens', async () => {
        await open();
        await settle();

        expect(document.activeElement?.id).toBe('Code');
    });

    it('selects what is already in that field, so typing replaces it', async () => {
        await open();
        await settle();

        const field = document.activeElement as HTMLInputElement;
        expect(field.selectionStart).toBe(0);
        expect(field.selectionEnd).toBe(host.code.length);
    });

    it('leaves the focus alone when the screen chose a field itself', async () => {
        await open();

        // What the screens do in their own onShown: focus a particular field.
        (document.getElementById('Name') as HTMLInputElement).focus();

        await settle();

        expect(document.activeElement?.id).toBe('Name');
    });

    it('never lands on a button', async () => {
        await open();
        await settle();

        expect(document.activeElement?.tagName).not.toBe('BUTTON');
    });

    it('leaves a dialog that is not inside another one where it is', async () => {
        const element = (host.modal() as any)._element.nativeElement as HTMLElement;
        const parent = element.parentElement;

        await open();

        expect(element.parentElement).toBe(parent);
    });
});

/**
 * A child record's dialog declared inside its parent's dialog -- as the loan's installment dialog sits inside the
 * loan dialog. Left in place, its backdrop (always at the end of the page) covered it.
 */
@Component({
    standalone: true,
    imports: [AppBsModalDirective],
    template: `
        <div appBsModal #outer="bs-modal" class="modal fade" tabindex="-1" role="dialog" [config]="config">
            <div class="modal-dialog">
                <div class="modal-content">
                    @if (childShown()) {
                        <div appBsModal #inner="bs-modal" id="InnerDialog" class="modal fade" tabindex="-1" role="dialog" [config]="config">
                            <div class="modal-dialog"><div class="modal-content"><input id="InnerField" type="text" /></div></div>
                        </div>
                    }
                </div>
            </div>
        </div>
    `,
})
class NestedHostComponent {
    readonly outer = viewChild.required<AppBsModalDirective>('outer');
    readonly inner = viewChild<AppBsModalDirective>('inner');
    readonly config = { backdrop: 'static' as const, animated: false };
    readonly childShown = signal(true);
}

describe('AppBsModalDirective inside another dialog', () => {
    let fixture: ComponentFixture<NestedHostComponent>;
    let host: NestedHostComponent;

    beforeEach(async () => {
        await TestBed.configureTestingModule({ imports: [NestedHostComponent] }).compileComponents();

        fixture = TestBed.createComponent(NestedHostComponent);
        host = fixture.componentInstance;
        fixture.autoDetectChanges(true);
        await fixture.whenStable();
    });

    afterEach(() => {
        fixture.destroy();
        document.querySelectorAll('.modal-backdrop').forEach((b) => b.remove());
    });

    it('moves to the end of the page when it opens, so it sits above its own backdrop', async () => {
        host.outer().show();
        host.inner()!.show();
        await fixture.whenStable();

        expect(document.getElementById('InnerDialog')?.parentElement).toBe(document.body);
    });

    it('is taken away with its component once it has been moved', async () => {
        host.outer().show();
        host.inner()!.show();
        await fixture.whenStable();

        host.childShown.set(false);
        await fixture.whenStable();

        expect(document.getElementById('InnerDialog')).toBeNull();
    });
});
