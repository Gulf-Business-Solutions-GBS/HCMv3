import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { RecordDirectlyService } from './record-directly.service';

/**
 * The "record without approval" choice on an admin transaction form.
 *
 * <p>Shows nothing at all unless this user holds the bypass permission AND the catalog allows it for this
 * transaction type -- the same two gates the server enforces, so the form never offers a choice the save
 * would refuse. One component rather than the same block pasted into thirteen dialogs: the permission
 * name, the wording and the conditions then have one home.</p>
 *
 * <p>Ticking it changes what saving MEANS, so it sits in the body of the form next to the other fields
 * and not beside the Save button, where it would read as a second kind of save.</p>
 */
@Component({
    selector: 'record-directly-check',
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [LocalizePipe],
    template: `
        @if (available()) {
            <div class="separator separator-dashed my-5"></div>

            <div class="form-check form-switch form-check-custom form-check-solid mb-2">
                <input
                    [id]="inputId()"
                    type="checkbox"
                    class="form-check-input"
                    [checked]="!!value()"
                    (change)="onToggle($event)" />
                <label class="form-check-label" [for]="inputId()">{{ 'RecordDirectly' | localize }}</label>
            </div>

            <div class="form-text">{{ 'RecordDirectlyHint' | localize }}</div>
        }
    `,
})
export class RecordDirectlyCheckComponent {
    private _service = inject(RecordDirectlyService);

    /** The catalog Code of the transaction on this form, which is its entity class name. */
    readonly requestType = input.required<string>();

    readonly value = input<boolean | undefined>(false);
    readonly valueChange = output<boolean>();

    readonly available = signal<boolean>(false);

    constructor() {
        // Asked once when the component is created rather than on every change detection pass; the service
        // answers from a shared, replayed response after the first form of the session.
        queueMicrotask(() => {
            this._service.canRecordDirectly(this.requestType()).subscribe((can) => this.available.set(can));
        });
    }

    /** Unique per transaction type, so two of these on one page cannot share a label. */
    inputId(): string {
        return 'RecordDirectly_' + this.requestType();
    }

    onToggle(event: Event): void {
        this.valueChange.emit((event.target as HTMLInputElement).checked);
    }
}
