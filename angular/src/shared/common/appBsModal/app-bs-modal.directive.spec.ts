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
});
