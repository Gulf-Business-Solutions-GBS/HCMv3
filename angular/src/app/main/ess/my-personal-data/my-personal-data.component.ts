import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import {
    EntityDto,
    Gender,
    GetMyRequestsInput,
    MyPersonalDataChangeDto,
    MyPersonalDataChangeServiceProxy,
    PersonalDataChangeValuesDto,
    PersonalDataLookupsDto,
    RequestPersonalDataChangeInput,
    RequestStatus,
    SavePersonalDataChangeDraftInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { MenuItem } from 'primeng/api';
import { TieredMenu } from 'primeng/tieredmenu';
import { DateTime } from 'luxon';
import { Observable, firstValueFrom } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { EssList } from '../shared/ess-list';
import { EssListHeadComponent } from '../shared/ess-list-head.component';
import { EssListPagerComponent } from '../shared/ess-list-pager.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { AttachmentsComponent } from '../../../shared/common/attachments/attachments.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import {
    PERSONAL_DATA_FIELDS,
    PersonalDataFieldDef,
    PersonalDataFormComponent,
    PersonalDataFormValue,
    isPersonalDataFieldChanged,
    toPersonalDataFormValues,
} from './personal-data-form.component';

/**
 * Self-service personnel-file updates (legacy Personal Data Update Request).
 *
 * <p>Laid out like every other self-service request (the exit permit is the model): the list, a Details view
 * in the shared dialog, and the form as its own component (<c>personal-data-form</c>) with Save, Submit and
 * Cancel. The form doubles as the profile: while no request is being written it shows what the personnel
 * file says today, read-only.</p>
 *
 * <p><b>Only what changed is sent.</b> The form opens on the current values, and the server treats a missing
 * value as "leave this alone", so the request carries the fields the employee actually altered -- plus the
 * four names the server will not accept a request without. That is also what makes the approver's view
 * short: old value beside new, for those fields only.</p>
 */
@Component({
    templateUrl: './my-personal-data.component.html',
    animations: [appModuleAnimation()],
    imports: [
        CommonModule,
        SubHeaderComponent,
        LocalizePipe,
        EssRequestDetailModalComponent,
        EssListHeadComponent,
        EssListPagerComponent,
        AttachmentsComponent,
        HelpPageDirective,
        PersonalDataFormComponent,
        TieredMenu,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyPersonalDataComponent extends MyRequestComponentBase<MyPersonalDataChangeDto> implements OnInit {
    private readonly _service = inject(MyPersonalDataChangeServiceProxy);

    /** The form component, which holds the files chosen on it until the request exists to attach them to. */
    private readonly personalDataForm = viewChild<PersonalDataFormComponent>('personalDataForm');

    /** The row whose attachments are expanded in the list, if any. */
    readonly expandedId = signal<number | null>(null);

    /** The Actions menu's entries, for whichever row's button was pressed last. */
    readonly activeMenuItems = signal<MenuItem[]>([]);

    /** What the personnel file says today. */
    readonly current = signal<PersonalDataChangeValuesDto | undefined>(undefined);
    readonly currentLoading = signal(false);

    /** The choices behind the form's seven pickers, and the names the list shows for their ids. */
    readonly lookups = signal<PersonalDataLookupsDto | undefined>(undefined);

    /**
     * The form's data, written into by the form component through ngModel. Replaced with a new object
     * whenever the form is handed something else to show, because the form component is OnPush.
     */
    readonly form = signal<PersonalDataFormValue>(this.freshForm());

    /** A request is with an approver. Said on the screen, so a second one is not raised by accident. */
    readonly hasPendingRequest = computed(() => this.rows().some((row) => row.workflowStatus === RequestStatus.Open));

    /** The list's columns, for the shared heading (sorting), filter row and pager. */
    readonly list = new EssList<MyPersonalDataChangeDto>(this.rows, () => [
        {
            key: 'requestNumber',
            label: 'RequestNumber',
            text: (row) => row.requestNumber,

            // As a number where it is one, so 9 sorts before 10. A draft has no number yet and sorts as the newest.
            sort: (row) =>
                row.requestNumber ? Number(row.requestNumber) || row.requestNumber : Number.MAX_SAFE_INTEGER,
        },
        {
            key: 'changes',
            label: 'Changes',
            text: (row) =>
                this.proposedFields(row)
                    .map((change) => `${change.label}: ${change.value}`)
                    .join(' ') || this.l('NoData'),
        },
        { key: 'comments', label: 'Comments', text: (row) => row.comments },
        { key: 'status', label: 'Status', text: (row) => this.statusLabel(row), filter: 'select' },
    ]);

    ngOnInit(): void {
        this.loadCurrent();
        this.reload();

        this._service.getMyPersonalDataLookups().subscribe({
            next: (result) => this.lookups.set(result),
            error: () => undefined,
        });

        // Failure is silent on purpose: the name is decoration, and an unlinked account is already
        // explained by the panel reload() puts up.
        this._essSessionService.getEssContext().subscribe({
            next: (result) => {
                this.context.set(result);
                this.form.set({ ...this.form(), employeeDisplay: this.employeeDisplay() });
            },
            error: () => undefined,
        });
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyPersonalDataChangeDto[] }> {
        return this._service.getMyPersonalDataChanges(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyPersonalDataChange(input);
    }

    protected resetForm(): void {
        // Files chosen for whatever the form was open on before do not belong to a new request.
        this.personalDataForm()?.clearAttachments();

        this.form.set(this.freshForm());
    }

    /** Cancel, and the end of every save: the form goes back to showing the profile as it is on file. */
    override closeForm(): void {
        super.closeForm();

        this.form.set(this.freshForm());
    }

    private loadCurrent(): void {
        this.currentLoading.set(true);

        this._service
            .getMyPersonalDataForEdit()
            .pipe(finalize(() => this.currentLoading.set(false)))
            .subscribe({
                next: (result) => {
                    this.current.set(result.current);

                    if (!this.showForm()) {
                        this.form.set(this.freshForm());
                    }
                },
                // Same setup gap the list call reports; the page explains it once rather than twice.
                error: () => this.notLinked.set(true),
            });
    }

    /** The form opened on the personnel file as it stands, with nothing changed yet. */
    private freshForm(): PersonalDataFormValue {
        const current = this.current?.();

        return {
            employeeDisplay: this.context?.() ? this.employeeDisplay() : '',
            values: toPersonalDataFormValues(current),
            original: toPersonalDataFormValues(current),
            comments: '',
        };
    }

    showMenu(event: Event, row: MyPersonalDataChangeDto, menu: TieredMenu): void {
        this.activeMenuItems.set(this.menuItems(row));
        menu.toggle(event);
    }

    /**
     * Every action, on every row, in the same order: the ones that do not apply to the row are greyed out
     * rather than left out, so the menu always looks the same.
     */
    private menuItems(row: MyPersonalDataChangeDto): MenuItem[] {
        const item = (key: string, enabled: boolean, command: () => void): MenuItem => ({
            label: this.l(key),
            disabled: !enabled,
            command,
        });

        return [
            item('Attachments', true, () => this.toggleAttachments(row)),

            // A draft was never sent to anybody, so it is discarded rather than withdrawn.
            item(row.isDraft ? 'DiscardDraft' : 'WithdrawRequest', !!row.canCancel, () => this.cancel(row)),
        ];
    }

    /**
     * Save: files what is on the form as a draft and goes back to the list. Nothing is checked beyond there
     * being something to save; the names and the birth date are checked at Submit.
     */
    save(): void {
        if (this.saving() || !this.hasChanges()) {
            return;
        }

        void this.run(async () => {
            const result = await firstValueFrom(this._service.saveMyPersonalDataChangeDraft(this.draftInput()));

            // Attached before the form is closed, which destroys the component holding the files.
            await this.personalDataForm()?.attachPending(this.editingId() ?? result.transactionId);

            this.notifyDraftSaved();
        });
    }

    /**
     * Submit: sends it into the approval chain. A saved draft, or a request an approver sent back, is saved
     * first and then submitted, so what the approver sees is what is on the screen; a new request is created
     * and submitted in one call. Either way the files chosen on the form are attached to the record.
     */
    submit(): void {
        if (this.saving() || !this.hasChanges() || !this.isValid()) {
            return;
        }

        const draftId = this.editingId();

        if (draftId !== undefined) {
            void this.run(async () => {
                await firstValueFrom(this._service.saveMyPersonalDataChangeDraft(this.draftInput()));

                // While it is still the employee's own draft, so the approver finds the proof already there.
                await this.personalDataForm()?.attachPending(draftId);

                const result = await firstValueFrom(
                    this._service.submitMyPersonalDataChangeDraft(new EntityDto({ id: draftId }))
                );

                this.notifySubmitted(result.submittedForApproval);
            });

            return;
        }

        const input = new RequestPersonalDataChangeInput();
        this.fill(input);

        void this.run(async () => {
            const result = await firstValueFrom(this._service.requestPersonalDataChange(input));

            // The request exists now, so the files can be attached to it. Done before closing the form.
            await this.personalDataForm()?.attachPending(result.transactionId);

            this.notifySubmitted(result.submittedForApproval);
        });
    }

    /** Reopens a saved draft, or a request an approver sent back, in the same form it was filled in on. */
    continueDraft(row: MyPersonalDataChangeDto): void {
        // Raw id, not an EntityDto: NSwag flattens EntityDto on GET.
        this._service.getMyPersonalDataChangeDraftForEdit(row.id).subscribe((draft) => {
            const form = this.freshForm();
            const proposed = (draft.proposed ?? {}) as Record<string, any>;
            const shaped = toPersonalDataFormValues(draft.proposed);

            // The saved record, so the form lists the files it already has.
            form.id = row.id;

            // The personnel file, with the draft's own changes laid over it. A field the draft does not
            // propose is null there and keeps the value on file.
            for (const field of PERSONAL_DATA_FIELDS) {
                if (proposed[field.key] !== null && proposed[field.key] !== undefined) {
                    form.values[field.key] = shaped[field.key];
                }
            }

            form.comments = draft.comments ?? '';

            // Files chosen for whatever the form was open on before do not belong to this draft.
            this.personalDataForm()?.clearAttachments();

            this.form.set(form);
            this.editingId.set(row.id);
            this.showForm.set(true);
        });
    }

    private hasChanges(): boolean {
        const form = this.form();

        if (PERSONAL_DATA_FIELDS.some((field) => isPersonalDataFieldChanged(field, form))) {
            return true;
        }

        this.message.warn(this.l('NoPersonalDataChanged'));

        return false;
    }

    /** Refused here as well as by the server, so the employee is told before the round trip. */
    private isValid(): boolean {
        const form = this.form();

        for (const field of PERSONAL_DATA_FIELDS.filter((f) => f.required)) {
            if (!String(form.values[field.key] ?? '').trim()) {
                // FirstNameEnIsRequired, LastNameArIsRequired and so on: the server's own messages.
                this.message.warn(this.l(field.label + 'IsRequired'));
                return false;
            }
        }

        const birthDate = PERSONAL_DATA_FIELDS.find((f) => f.key === 'birthDate');

        // Today is refused as well as the future: nobody was born on a day that has not finished.
        if (
            birthDate &&
            isPersonalDataFieldChanged(birthDate, form) &&
            DateTime.fromISO(String(form.values['birthDate'])) >= DateTime.local().startOf('day')
        ) {
            this.message.warn(this.l('BirthDateCannotBeFutureOrCurrent'));
            return false;
        }

        return true;
    }

    private draftInput(): SavePersonalDataChangeDraftInput {
        const input = new SavePersonalDataChangeDraftInput();
        input.id = this.editingId();
        this.fill(input);

        return input;
    }

    /**
     * Puts the changed fields on the request, and the four names whether changed or not: the server refuses
     * a request without them, so a mobile-number change that carried no names would be rejected on submit.
     * Everything else is left off, which the server reads as "leave this alone".
     */
    private fill(input: RequestPersonalDataChangeInput): void {
        const form = this.form();
        const target = input as unknown as Record<string, any>;

        for (const field of PERSONAL_DATA_FIELDS) {
            if (!field.required && !isPersonalDataFieldChanged(field, form)) {
                continue;
            }

            const raw = form.values[field.key];

            switch (field.kind) {
                case 'date':
                    target[field.key] = DateTime.fromISO(String(raw));
                    break;
                case 'number':
                case 'lookup':
                case 'gender':
                    target[field.key] = Number(raw);
                    break;
                default:
                    target[field.key] = String(raw ?? '').trim();
            }
        }

        input.comments = form.comments;
    }

    /**
     * Runs one save or submit with the buttons disabled throughout -- the server call and the attaching of
     * the files after it. A refused call has already been explained by the error dialog, and leaves the form
     * open with everything still on it.
     */
    private async run(work: () => Promise<void>): Promise<void> {
        this.saving.set(true);

        try {
            await work();
        } catch {
            // Already reported to the employee.
        } finally {
            this.saving.set(false);
        }
    }

    toggleAttachments(row: MyPersonalDataChangeDto): void {
        this.expandedId.set(this.expandedId() === row.id ? null : row.id);
    }

    /**
     * What a request in the list changes, in words: the fields it proposes whose value differs from the
     * personnel file. The four names travel on every request and are not listed unless they really changed.
     */
    proposedFields(row: MyPersonalDataChangeDto): { label: string; value: string }[] {
        const proposed = (row.proposed ?? {}) as Record<string, any>;
        const shaped = toPersonalDataFormValues(row.proposed);
        const onFile = toPersonalDataFormValues(this.current());
        const result: { label: string; value: string }[] = [];

        for (const field of PERSONAL_DATA_FIELDS) {
            if (proposed[field.key] === null || proposed[field.key] === undefined) {
                continue;
            }

            if (
                !isPersonalDataFieldChanged(field, {
                    employeeDisplay: '',
                    values: shaped,
                    original: onFile,
                    comments: '',
                })
            ) {
                continue;
            }

            result.push({ label: this.l(field.label), value: this.display(field, shaped[field.key]) });
        }

        return result;
    }

    /** A form value in the reader's words: a picker's name, the gender as a word, a date as dd/MM/yyyy. */
    private display(field: PersonalDataFieldDef, value: any): string {
        if (value === null || value === undefined || value === '') {
            return '';
        }

        switch (field.kind) {
            case 'lookup': {
                const options: SimpleLookupDto[] = (field.lookup && this.lookups()?.[field.lookup]) || [];
                const option = options.find((o) => o.id === value);

                return option ? this.pick(option.nameEn, option.nameAr) : String(value);
            }
            case 'gender':
                return this.l(Gender[value as Gender]);
            case 'date': {
                const [year, month, day] = String(value).split('-');

                return `${day}/${month}/${year}`;
            }
            default:
                return String(value);
        }
    }

    protected requestType(): string {
        return 'EmployeePersonalDataChange';
    }

    protected detailTitle(): string {
        return this.l('EssMyProfile');
    }

    /**
     * Used only for a request with no workflow request behind it (workflow switched off for this type): the
     * dialog then has nothing to fetch. Otherwise it shows the request's own old-and-new table.
     */
    protected detailFields(row: MyPersonalDataChangeDto): EssRequestDetailField[] {
        return [{ label: this.l('Employee'), value: this.employeeDisplay() }, ...this.proposedFields(row)];
    }
}
