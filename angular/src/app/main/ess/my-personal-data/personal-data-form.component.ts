import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { ChangeDetectionStrategy, Component, computed, input, output, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { Gender, PersonalDataChangeValuesDto, PersonalDataLookupsDto, SimpleLookupDto } from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { AttachmentsComponent } from '../../../shared/common/attachments/attachments.component';

export type PersonalDataFieldKind = 'text' | 'textarea' | 'date' | 'number' | 'gender' | 'lookup';

/** Which list of <c>PersonalDataLookupsDto</c> a picker chooses from. */
export type PersonalDataLookupName =
    | 'titles'
    | 'nationalities'
    | 'religions'
    | 'maritalStatuses'
    | 'cities'
    | 'governorates'
    | 'countries';

export interface PersonalDataFieldDef {
    /** Property on PersonalDataChangeValuesDto / RequestPersonalDataChangeInput. */
    key: string;

    /** Localization key for the label. */
    label: string;

    kind: PersonalDataFieldKind;

    /** For a picker, the list it chooses from. */
    lookup?: PersonalDataLookupName;

    /** Column classes, when the field is wider than the standard quarter row. */
    col?: string;

    maxlength?: number;

    /** Arabic text, typed right to left whatever the language of the screen. */
    arabic?: boolean;

    /** One of the four names a request cannot be sent without. */
    required?: boolean;
}

export interface PersonalDataSectionDef {
    /** Localization key for the card heading. */
    label: string;
    fields: PersonalDataFieldDef[];
}

/**
 * The form's cards and fields, in the order the legacy Personal Data Update form has them: Personal Detail,
 * Contact Information, Other Information, Emergency Contact Information.
 */
export const PERSONAL_DATA_SECTIONS: PersonalDataSectionDef[] = [
    {
        label: 'PersonalDetail',
        fields: [
            { key: 'titleId', label: 'Title', kind: 'lookup', lookup: 'titles', col: 'col-12 col-md-6 col-xl-2' },
            { key: 'firstNameEn', label: 'FirstNameEn', kind: 'text', maxlength: 128, required: true },
            { key: 'secondNameEn', label: 'SecondNameEn', kind: 'text', maxlength: 128 },
            { key: 'thirdNameEn', label: 'ThirdNameEn', kind: 'text', maxlength: 128 },
            { key: 'lastNameEn', label: 'LastNameEn', kind: 'text', maxlength: 128, required: true },
            { key: 'firstNameAr', label: 'FirstNameAr', kind: 'text', maxlength: 128, arabic: true, required: true },
            { key: 'secondNameAr', label: 'SecondNameAr', kind: 'text', maxlength: 128, arabic: true },
            { key: 'thirdNameAr', label: 'ThirdNameAr', kind: 'text', maxlength: 128, arabic: true },
            { key: 'lastNameAr', label: 'LastNameAr', kind: 'text', maxlength: 128, arabic: true, required: true },
            { key: 'gender', label: 'Gender', kind: 'gender' },
            { key: 'bloodGroup', label: 'BloodGroup', kind: 'text', maxlength: 32 },
            { key: 'nationalityId', label: 'Nationality', kind: 'lookup', lookup: 'nationalities' },
            { key: 'maritalStatusId', label: 'MaritalStatus', kind: 'lookup', lookup: 'maritalStatuses' },
            { key: 'religionId', label: 'Religion', kind: 'lookup', lookup: 'religions' },
            { key: 'birthPlaceId', label: 'BirthPlace', kind: 'lookup', lookup: 'cities' },
            { key: 'birthDate', label: 'BirthDate', kind: 'date' },
        ],
    },
    {
        label: 'ContactInformation',
        fields: [
            { key: 'countryId', label: 'Country', kind: 'lookup', lookup: 'countries' },
            { key: 'governorateId', label: 'Governorate', kind: 'lookup', lookup: 'governorates' },
            { key: 'city', label: 'City', kind: 'text', maxlength: 128 },
            { key: 'blockNum', label: 'BlockNumber', kind: 'text', maxlength: 32 },
            { key: 'streetNum', label: 'StreetNumber', kind: 'text', maxlength: 32 },
            { key: 'buildingNum', label: 'BuildingNumber', kind: 'text', maxlength: 32 },
            { key: 'floorNum', label: 'FloorNumber', kind: 'text', maxlength: 32 },
            { key: 'flatNumn', label: 'FlatNumber', kind: 'text', maxlength: 32 },
            { key: 'paciNumber', label: 'PaciNumber', kind: 'text', maxlength: 32 },
            { key: 'zipCode', label: 'ZipCode', kind: 'text', maxlength: 32 },
            { key: 'telephone', label: 'Telephone', kind: 'number' },
            { key: 'mobile', label: 'Mobile', kind: 'number' },
            { key: 'email', label: 'Email', kind: 'text', maxlength: 256 },
            { key: 'alternateEmail', label: 'AlternateEmail', kind: 'text', maxlength: 256 },
            { key: 'currentAddress', label: 'CurrentAddress', kind: 'textarea', maxlength: 1000, col: 'col-12 col-lg-6' },
            { key: 'permanentAddress', label: 'PermanentAddress', kind: 'textarea', maxlength: 1000, col: 'col-12 col-lg-6' },
        ],
    },
    {
        label: 'OtherInformation',
        fields: [
            { key: 'criticalIllness', label: 'CriticalIllness', kind: 'text', maxlength: 1000 },
            { key: 'fatherNameEn', label: 'FatherNameEn', kind: 'text', maxlength: 128 },
            { key: 'fatherNameAr', label: 'FatherNameAr', kind: 'text', maxlength: 128, arabic: true },
            { key: 'motherNameEn', label: 'MotherNameEn', kind: 'text', maxlength: 128 },
            { key: 'motherNameAr', label: 'MotherNameAr', kind: 'text', maxlength: 128, arabic: true },
        ],
    },
    {
        label: 'EmergencyContactInformation',
        fields: [
            { key: 'emergencyContact', label: 'EmergencyContact', kind: 'text', maxlength: 256, col: 'col-12 col-lg-6' },
            { key: 'emergencyNumber', label: 'EmergencyNumber', kind: 'text', maxlength: 64 },
            { key: 'emergencyRelation', label: 'EmergencyRelation', kind: 'text', maxlength: 128 },
        ],
    },
];

/** Every field of the form, in screen order. */
export const PERSONAL_DATA_FIELDS: PersonalDataFieldDef[] = PERSONAL_DATA_SECTIONS.flatMap((section) => section.fields);

/**
 * The form's data. <c>values</c> is what the inputs are bound to; <c>original</c> is the personnel file as
 * it stands, in the same shape, which is what tells a changed field from an untouched one.
 *
 * <p>Text and dates are held as strings (a date as ISO yyyy-MM-dd, empty when unset); a picker holds the id
 * or null; a number holds what the number input gave, with "no number" as empty rather than zero.</p>
 */
export interface PersonalDataFormValue {
    /** The saved draft the form is open on, so the files it already has are listed. */
    id?: number;

    /** "1042 - Ahmad Al Sayed". Shown, never chosen. */
    employeeDisplay: string;

    values: Record<string, any>;
    original: Record<string, any>;
    comments: string;
}

/** The server's forty values in the shape the form's inputs hold them. Unset becomes empty, never undefined. */
export function toPersonalDataFormValues(source: PersonalDataChangeValuesDto | undefined): Record<string, any> {
    const raw = (source ?? {}) as Record<string, any>;
    const values: Record<string, any> = {};

    for (const field of PERSONAL_DATA_FIELDS) {
        const value = raw[field.key];

        switch (field.kind) {
            case 'date':
                // The year 1 is a date that was never set, not a date.
                values[field.key] = value && (value as DateTime).year > 1 ? (value as DateTime).toFormat('yyyy-MM-dd') : '';
                break;
            case 'lookup':
            case 'gender':
                values[field.key] = value ?? null;
                break;
            case 'number':
                // The personnel file stores "no number" as zero.
                values[field.key] = value ? value : null;
                break;
            default:
                values[field.key] = value ?? '';
        }
    }

    return values;
}

function comparable(value: any): string {
    return value === null || value === undefined ? '' : String(value).trim();
}

/**
 * Whether the field on the form differs from the personnel file.
 *
 * <p>Clearing a picker, a number or a date is not a change: the request cannot ask for those to be emptied
 * (a missing value means "leave it alone" to the server, as it did in legacy), so an emptied one simply
 * stays as it is on file. A text box can be blanked, and that is a change.</p>
 */
export function isPersonalDataFieldChanged(field: PersonalDataFieldDef, form: PersonalDataFormValue): boolean {
    const proposed = comparable(form.values[field.key]);

    if (!proposed && field.kind !== 'text' && field.kind !== 'textarea') {
        return false;
    }

    return proposed !== comparable(form.original[field.key]);
}

/**
 * The personal data update form: the legacy form's four cards, opened on what the personnel file says today
 * so the employee edits what is there instead of retyping it.
 *
 * <p>Laid out like every other self-service request (the exit permit is the model): its own component, with
 * Save, Submit and Cancel. In view mode the same cards are shown with every input disabled, which is how the
 * screen shows the employee their profile when no request is being written.</p>
 *
 * <p>A changed field is outlined in green, as legacy outlined it. The approver is not shown this form: they
 * are shown the changed fields only, old value beside new (<c>personal-data-changes</c>).</p>
 */
@Component({
    selector: 'personal-data-form',
    templateUrl: './personal-data-form.component.html',
    imports: [DateInputDirective, CommonModule, FormsModule, LocalizePipe, AttachmentsComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PersonalDataFormComponent extends AppComponentBase {
    readonly mode = input<'create' | 'view'>('create');

    /** The form's data. In create mode the host passes its own object and this writes into it through ngModel. */
    readonly value = input.required<PersonalDataFormValue>();

    /** The choices behind the seven pickers. */
    readonly lookups = input<PersonalDataLookupsDto | undefined>(undefined);

    /** Disabled while the host is saving, so a double Submit cannot raise two requests. */
    readonly saving = input(false);

    /** Save as draft: files the request without sending it to an approver. */
    readonly saved = output<void>();
    readonly submitted = output<void>();
    readonly cancelled = output<void>();

    readonly sections = PERSONAL_DATA_SECTIONS;

    readonly readOnly = computed(() => this.mode() !== 'create');

    readonly genders: Gender[] = [Gender.Male, Gender.Female];

    private readonly attachments = viewChild<AttachmentsComponent>('formAttachments');

    options(field: PersonalDataFieldDef): SimpleLookupDto[] {
        return (field.lookup && this.lookups()?.[field.lookup]) || [];
    }

    optionName(option: SimpleLookupDto): string {
        return this.isRtl ? option.nameAr || option.nameEn || '' : option.nameEn || '';
    }

    genderName(gender: Gender): string {
        return this.l(Gender[gender]);
    }

    /** Outlined only while the form is being written; the profile on its own has nothing to compare with. */
    isChanged(field: PersonalDataFieldDef): boolean {
        return !this.readOnly() && isPersonalDataFieldChanged(field, this.value());
    }

    /**
     * Attaches the files chosen on the form to the request the host has just saved. Nothing to do for a draft
     * being continued: its files were attached as they were chosen.
     */
    async attachPending(entityId: number): Promise<void> {
        await this.attachments()?.attachPending(entityId);
    }

    /** Drops the files chosen but not yet attached, when the form is handed a different request. */
    clearAttachments(): void {
        this.attachments()?.clearPending();
    }

    private get isRtl(): boolean {
        return document?.documentElement?.getAttribute('dir') === 'rtl';
    }
}
