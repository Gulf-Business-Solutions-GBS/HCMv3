import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    EntityDto,
    Gender,
    GetMyRequestsInput,
    MyPersonalDataChangeDto,
    MyPersonalDataChangeServiceProxy,
    PersonalDataChangeValuesDto,
    RequestPersonalDataChangeInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { AttachmentsComponent } from '../../../shared/common/attachments/attachments.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

type PersonalDataFieldKind = 'text' | 'date' | 'number' | 'gender';

interface PersonalDataField {
    /** Property on PersonalDataChangeValuesDto / RequestPersonalDataChangeInput. */
    key: string;

    /** Localization key for the label. */
    label: string;

    kind: PersonalDataFieldKind;
}

interface PersonalDataSection {
    /** Localization key for the section heading. */
    label: string;
    fields: PersonalDataField[];
}

/**
 * Self-service personnel-file updates.
 *
 * <p><b>Not a form with forty inputs.</b> The server treats a null proposed value as "leave this alone", so
 * a request is a SET OF PROPOSED CHANGES rather than a whole record. The screen is built to match: every
 * field is listed with what the personnel file says today, and only the ones the employee explicitly picks
 * are sent. Retyping thirty-nine unchanged fields to correct one is how a screen manufactures exactly the
 * corruption this feature exists to control.</p>
 *
 * <p>The four name fields are the exception and are always sent, filled from the current record when the
 * employee did not touch them: the server refuses a request whose English and Arabic first and last names
 * are blank, so a mobile-number change that carried no names at all would be rejected on submit.</p>
 */
@Component({
    templateUrl: './my-personal-data.component.html',
    animations: [appModuleAnimation()],
    imports: [
        DateInputDirective,
        CommonModule,
        FormsModule,
        SubHeaderComponent,
        LocalizePipe,
        EssRequestDetailModalComponent,
        AttachmentsComponent,
        HelpPageDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyPersonalDataComponent extends MyRequestComponentBase<MyPersonalDataChangeDto> implements OnInit {
    private readonly _service = inject(MyPersonalDataChangeServiceProxy);

    /** Proof chosen in the form, attached once the request exists. */
    private readonly _formAttachments = viewChild<AttachmentsComponent>('formAttachments');

    /** The row whose attachments are expanded in the list, if any. */
    readonly expandedId = signal<number | null>(null);

    /**
     * The fields the employee may propose changes to.
     *
     * <p>The seven lookup fields the server also accepts -- title, nationality, religion, marital status,
     * birth place, governorate, country -- are NOT here. They arrive as bare numeric ids and every catalog
     * that could turn an id into a name, or offer the choices, sits behind an administration permission an
     * employee does not hold. Offering a numeric box would be worse than not offering the field.</p>
     */
    readonly sections: PersonalDataSection[] = [
        {
            label: 'Names',
            fields: [
                { key: 'firstNameEn', label: 'FirstNameEn', kind: 'text' },
                { key: 'secondNameEn', label: 'SecondNameEn', kind: 'text' },
                { key: 'thirdNameEn', label: 'ThirdNameEn', kind: 'text' },
                { key: 'lastNameEn', label: 'LastNameEn', kind: 'text' },
                { key: 'firstNameAr', label: 'FirstNameAr', kind: 'text' },
                { key: 'secondNameAr', label: 'SecondNameAr', kind: 'text' },
                { key: 'thirdNameAr', label: 'ThirdNameAr', kind: 'text' },
                { key: 'lastNameAr', label: 'LastNameAr', kind: 'text' },
            ],
        },
        {
            label: 'PersonalInfo',
            fields: [
                { key: 'birthDate', label: 'BirthDate', kind: 'date' },
                { key: 'gender', label: 'Gender', kind: 'gender' },
                { key: 'bloodGroup', label: 'BloodGroup', kind: 'text' },
                { key: 'criticalIllness', label: 'CriticalIllness', kind: 'text' },
            ],
        },
        {
            label: 'Contact',
            fields: [
                { key: 'telephone', label: 'Telephone', kind: 'number' },
                { key: 'mobile', label: 'Mobile', kind: 'number' },
                { key: 'email', label: 'Email', kind: 'text' },
                { key: 'alternateEmail', label: 'AlternateEmail', kind: 'text' },
                { key: 'paciNumber', label: 'PaciNumber', kind: 'text' },
            ],
        },
        {
            label: 'Address',
            fields: [
                { key: 'flatNumn', label: 'FlatNumber', kind: 'text' },
                { key: 'floorNum', label: 'FloorNumber', kind: 'text' },
                { key: 'buildingNum', label: 'BuildingNumber', kind: 'text' },
                { key: 'streetNum', label: 'StreetNumber', kind: 'text' },
                { key: 'blockNum', label: 'BlockNumber', kind: 'text' },
                { key: 'zipCode', label: 'ZipCode', kind: 'text' },
                { key: 'city', label: 'City', kind: 'text' },
                { key: 'currentAddress', label: 'CurrentAddress', kind: 'text' },
                { key: 'permanentAddress', label: 'PermanentAddress', kind: 'text' },
            ],
        },
        {
            label: 'ParentNames',
            fields: [
                { key: 'fatherNameEn', label: 'FatherNameEn', kind: 'text' },
                { key: 'fatherNameAr', label: 'FatherNameAr', kind: 'text' },
                { key: 'motherNameEn', label: 'MotherNameEn', kind: 'text' },
                { key: 'motherNameAr', label: 'MotherNameAr', kind: 'text' },
            ],
        },
        {
            label: 'EmergencyContactSection',
            fields: [
                { key: 'emergencyContact', label: 'EmergencyContact', kind: 'text' },
                { key: 'emergencyNumber', label: 'EmergencyNumber', kind: 'text' },
                { key: 'emergencyRelation', label: 'EmergencyRelation', kind: 'text' },
            ],
        },
    ];

    /** Sent on every request whatever the employee picked -- the server refuses a request without them. */
    private readonly mandatoryNameFields: { key: string; message: string }[] = [
        { key: 'firstNameEn', message: 'FirstNameEnIsRequired' },
        { key: 'firstNameAr', message: 'FirstNameArIsRequired' },
        { key: 'lastNameEn', message: 'LastNameEnIsRequired' },
        { key: 'lastNameAr', message: 'LastNameArIsRequired' },
    ];

    readonly current = signal<PersonalDataChangeValuesDto | undefined>(undefined);
    readonly hasPendingRequest = signal(false);
    readonly currentLoading = signal(false);

    /** Which fields the employee has chosen to change. Everything else is left out of the request. */
    selected: Record<string, boolean> = {};

    /** Proposed values, held as form strings until submit converts them. */
    edits: Record<string, any> = {};

    comments = '';

    readonly genderOptions = [
        { value: Gender.Male, label: 'Male' },
        { value: Gender.Female, label: 'Female' },
    ];

    ngOnInit(): void {
        this.loadCurrent();
        this.reload();
        this.loadContext();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyPersonalDataChangeDto[] }> {
        return this._service.getMyPersonalDataChanges(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyPersonalDataChange(input);
    }

    protected resetForm(): void {
        this.selected = {};
        this.edits = {};
        this.comments = '';
    }

    private loadCurrent(): void {
        this.currentLoading.set(true);

        this._service
            .getMyPersonalDataForEdit()
            .pipe(finalize(() => this.currentLoading.set(false)))
            .subscribe({
                next: (result) => {
                    this.current.set(result.current);
                    this.hasPendingRequest.set(result.hasPendingRequest);
                },
                // Same setup gap the list call reports; the page explains it once rather than twice.
                error: () => this.notLinked.set(true),
            });
    }

    /** Turning a field on seeds it with today's value, so the employee edits rather than retypes. */
    toggleField(field: PersonalDataField): void {
        if (this.selected[field.key]) {
            delete this.selected[field.key];
            delete this.edits[field.key];
            return;
        }

        this.selected[field.key] = true;
        this.edits[field.key] = this.rawCurrent(field);
    }

    isSelected(field: PersonalDataField): boolean {
        return !!this.selected[field.key];
    }

    selectedCount(): number {
        return Object.keys(this.selected).length;
    }

    /** What the personnel file says today, formatted for reading. */
    displayCurrent(field: PersonalDataField): string {
        return this.format(field, this.current() as any);
    }

    submit(): void {
        if (this.saving()) {
            return;
        }

        const current = this.current();

        if (!current) {
            return;
        }

        if (this.selectedCount() === 0) {
            this.message.warn(this.l('SelectAtLeastOneFieldToChange'));
            return;
        }

        const input = new RequestPersonalDataChangeInput();

        // The four names go on every request, taken from the current record when untouched. Without them
        // the server's own validation would refuse a change to anything else.
        for (const name of this.mandatoryNameFields) {
            const value = this.selected[name.key]
                ? String(this.edits[name.key] ?? '').trim()
                : String((current as any)[name.key] ?? '').trim();

            if (!value) {
                this.message.warn(this.l(name.message));
                return;
            }

            (input as any)[name.key] = value;
        }

        for (const section of this.sections) {
            for (const field of section.fields) {
                if (!this.selected[field.key]) {
                    continue;
                }

                const raw = this.edits[field.key];

                if (field.kind === 'date') {
                    if (!raw) {
                        this.message.warn(this.l('ProposedValueIsRequired', this.l(field.label)));
                        return;
                    }

                    const proposed = DateTime.fromISO(String(raw));

                    // Refused here as well as by the server: today is refused as well as the future,
                    // because nobody was born on a day that has not finished.
                    if (field.key === 'birthDate' && proposed >= DateTime.local().startOf('day')) {
                        this.message.warn(this.l('BirthDateCannotBeFutureOrCurrent'));
                        return;
                    }

                    (input as any)[field.key] = proposed;
                    continue;
                }

                if (field.kind === 'number') {
                    if (raw === '' || raw === null || raw === undefined) {
                        this.message.warn(this.l('ProposedValueIsRequired', this.l(field.label)));
                        return;
                    }

                    const numeric = Number(raw);

                    if (!isFinite(numeric)) {
                        this.message.warn(this.l('ProposedValueIsRequired', this.l(field.label)));
                        return;
                    }

                    (input as any)[field.key] = numeric;
                    continue;
                }

                if (field.kind === 'gender') {
                    if (raw === '' || raw === null || raw === undefined) {
                        this.message.warn(this.l('ProposedValueIsRequired', this.l(field.label)));
                        return;
                    }

                    (input as any)[field.key] = Number(raw) as Gender;
                    continue;
                }

                (input as any)[field.key] = String(raw ?? '').trim();
            }
        }

        input.comments = this.comments;

        this.saving.set(true);

        this._service.requestPersonalDataChange(input).subscribe({
            next: async (result) => {
                // Proof (legacy SampleImage) is attached to the request now that it exists, before the
                // form closes and takes the chosen files with it.
                try {
                    await this._formAttachments()?.attachPending(result.transactionId);
                } finally {
                    this.saving.set(false);
                }
                this.notifySubmitted(result.submittedForApproval);
                this.loadCurrent();
            },
            error: () => this.saving.set(false),
        });
    }

    toggleAttachments(row: MyPersonalDataChangeDto): void {
        this.expandedId.set(this.expandedId() === row.id ? null : row.id);
    }

    /**
     * The fields a submitted request actually proposes -- anything the employee did not pick came back null
     * and is not part of it, so listing it would misrepresent what the approver is being asked to agree to.
     */
    proposedFields(row: MyPersonalDataChangeDto): { label: string; value: string }[] {
        const proposed = row.proposed as any;

        if (!proposed) {
            return [];
        }

        const result: { label: string; value: string }[] = [];

        for (const section of this.sections) {
            for (const field of section.fields) {
                if (proposed[field.key] === null || proposed[field.key] === undefined) {
                    continue;
                }

                result.push({ label: field.label, value: this.format(field, proposed) });
            }
        }

        return result;
    }

    private rawCurrent(field: PersonalDataField): any {
        const current = this.current() as any;

        if (!current) {
            return '';
        }

        const value = current[field.key];

        if (value === null || value === undefined) {
            return field.kind === 'gender' ? '' : '';
        }

        if (field.kind === 'date') {
            return (value as DateTime).toISODate();
        }

        return value;
    }

    private format(field: PersonalDataField, source: any): string {
        if (!source) {
            return '';
        }

        const value = source[field.key];

        if (value === null || value === undefined) {
            return '';
        }

        if (field.kind === 'date') {
            return (value as DateTime).toISODate() ?? '';
        }

        if (field.kind === 'gender') {
            return this.l(value === Gender.Female ? 'Female' : 'Male');
        }

        return String(value);
    }

    protected detailTitle(): string {
        return this.l('EssMyProfile');
    }

    /**
     * The proposed changes themselves, one per row.
     *
     * <p>Unlike the other self-service screens this request has no fixed set of fields -- it is whatever the
     * employee chose to change -- so the dialog lists exactly what the table lists. The labels come back as
     * localization keys and are translated here, because the dialog renders its fields as plain text.</p>
     */
    protected detailFields(row: MyPersonalDataChangeDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            ...this.proposedFields(row).map((change) => ({ label: this.l(change.label), value: change.value })),
        ];
    }
}
