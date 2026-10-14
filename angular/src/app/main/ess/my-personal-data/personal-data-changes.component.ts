import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { AttachmentsComponent } from '../../../shared/common/attachments/attachments.component';

/** One field a personal data update changes, already worded for reading. */
export interface PersonalDataChangeRow {
    label: string;
    oldValue: string;
    newValue: string;
}

export interface PersonalDataChangesValue {
    /** The request's own record, which is what its proof is attached to. */
    id?: number;

    /** "1042 - Ahmad Al Sayed": whose personnel file this changes. */
    employeeDisplay: string;

    changes: PersonalDataChangeRow[];
}

/**
 * A personal data update as its approver reads it: the fields it changes and nothing else, what each held
 * before beside what is proposed.
 *
 * <p>This is what the request's Details view and the approver are shown in place of the form. The form has
 * forty fields and a request usually changes two or three of them; shown the whole form, the approver has to
 * find those by eye. Shown this, they read down one short table and decide.</p>
 */
@Component({
    selector: 'personal-data-changes',
    template: `
        <div class="card card-custom gutter-b mb-5">
            <div class="card-body">
                <h5 class="card-title" data-help-loc="RequestDetail">{{ 'RequestDetail' | localize }}</h5>

                <div class="row g-4 mb-4">
                    <div class="col-12 col-md-6">
                        <label class="form-label" data-help-loc="Employee">{{ 'Employee' | localize }}</label>
                        <input type="text" class="form-control" [value]="value().employeeDisplay" readonly disabled />
                    </div>
                </div>

                <!-- Laid out the way SAP shows a data change: the field's name, the new value in green, and
                     underneath it the value it replaces, struck through. A field that was empty before has
                     nothing to strike through, so only the new value shows. -->
                <div class="pdc-list" data-help-loc="Changes">
                    @for (change of value().changes; track change.label) {
                        <div class="pdc-row">
                            <div class="pdc-label text-muted">{{ change.label }}</div>
                            <div class="pdc-value">
                                <div class="fw-bold text-success">{{ change.newValue || '–' }}</div>
                                @if (change.oldValue) {
                                    <div class="pdc-old">{{ change.oldValue }}</div>
                                }
                            </div>
                        </div>
                    } @empty {
                        <div class="text-muted text-center p-4">{{ 'NoData' | localize }}</div>
                    }
                </div>

                @if (value().id) {
                    <div class="mt-4">
                        <!-- The proof sent with the request, for whoever is reading it. -->
                        <app-attachments
                            entityType="EmployeePersonalDataChange"
                            [entityId]="value().id"
                            [readonly]="true"
                            [showLimitsHint]="false" />
                    </div>
                }
            </div>
        </div>
    `,
    styles: [
        `
            .pdc-row {
                display: grid;
                grid-template-columns: minmax(140px, 30%) 1fr;
                gap: 1rem;
                padding: 0.6rem 0;
                border-bottom: 1px solid var(--bs-border-color);
            }

            .pdc-row:last-child {
                border-bottom: 0;
            }

            .pdc-old {
                color: var(--bs-gray-700);
                font-size: 0.95em;
                text-decoration: line-through;
                text-decoration-thickness: 1px;
            }

            .pdc-value {
                overflow-wrap: anywhere;
            }
        `,
    ],
    imports: [CommonModule, LocalizePipe, AttachmentsComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonalDataChangesComponent extends AppComponentBase {
    /** Set by the request dialog on every form it hosts. Nothing here can be edited in any mode. */
    readonly mode = input<'create' | 'view' | 'approve'>('view');

    readonly value = input.required<PersonalDataChangesValue>();
}
