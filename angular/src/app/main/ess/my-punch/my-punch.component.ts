import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    EssLookupServiceProxy,
    GetMyRequestsInput,
    MyPunchRequestDto,
    MyPunchServiceProxy,
    RequestMissingPunchInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Self-service missing-punch reports.
 *
 * <p>The list is deliberately NOT a punch log. The server returns only the punches this employee asked for,
 * so the page answers "where has my missing-punch request got to" rather than "what did my badge record" --
 * the second question is a different screen with thousands of rows and no status to show.</p>
 */
@Component({
    templateUrl: './my-punch.component.html',
    animations: [appModuleAnimation()],
    imports: [
        DateInputDirective,
        CommonModule,
        FormsModule,
        SubHeaderComponent,
        LocalizePipe,
        LuxonFormatPipe,
        EssRequestDetailModalComponent,
        HelpPageDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyPunchComponent extends MyRequestComponentBase<MyPunchRequestDto> implements OnInit {
    private readonly _service = inject(MyPunchServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);

    readonly punchTypes = signal<SimpleLookupDto[]>([]);

    /** Time is a string because it is optional: an employee who forgot to badge in rarely knows the minute. */
    form = { transTypeId: null as number | null, transDate: '', transTime: '' };

    /** Bound to the date input's max, so the browser refuses a future day before the server has to. */
    readonly today = DateTime.local().toISODate();

    ngOnInit(): void {
        // An empty list now means one thing only -- HR has configured no punch types -- so the form's
        // "none available" notice still earns its place. It used to double as the symptom of a permission
        // failure, which is no longer possible: this lookup is readable by any self-service user.
        this._lookups.getPunchTypes().subscribe({
            next: (result) => this.punchTypes.set(result ?? []),
            error: () => this.punchTypes.set([]),
        });

        this.reload();
        this.loadContext();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyPunchRequestDto[] }> {
        return this._service.getMyPunches(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyPunch(input);
    }

    protected resetForm(): void {
        this.form = { transTypeId: null, transDate: '', transTime: '' };
    }

    submit(): void {
        if (this.saving()) {
            return;
        }

        if (!this.form.transTypeId) {
            this.message.warn(this.l('PunchTypeIsRequired'));
            return;
        }

        if (!this.form.transDate) {
            this.message.warn(this.l('PunchDateIsRequired'));
            return;
        }

        // Refused here as well as by the server: a missing punch is a claim about something that already
        // happened, and telling the employee now saves a round trip that can only fail.
        if (this.form.transDate > this.today) {
            this.message.warn(this.l('APunchCannotBeRequestedForAFutureDate'));
            return;
        }

        const input = new RequestMissingPunchInput();
        input.transTypeId = this.form.transTypeId;
        input.transDate = DateTime.fromISO(this.form.transDate);

        // Left out when they did not say, so the server stamps the rostered duty's start or end instead of
        // a time that would look like something the employee stated.
        input.transTime = this.form.transTime
            ? DateTime.fromISO(`${this.form.transDate}T${this.form.transTime}`)
            : (undefined as any);

        this.saving.set(true);

        this._service
            .requestMissingPunch(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    typeName(row: MyPunchRequestDto): string {
        return this.pick(row.transTypeNameEn, row.transTypeNameAr);
    }

    lookupName(type: SimpleLookupDto): string {
        return this.pick(type.nameEn, type.nameAr);
    }

    protected detailTitle(): string {
        return this.l('EssMyPunches');
    }

    protected detailFields(row: MyPunchRequestDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('PunchType'), value: this.typeName(row) },
            { label: this.l('PunchDate'), value: this.detailDate(row.transDate) },
            { label: this.l('PunchTime'), value: row.transTime?.toFormat('HH:mm') ?? '' },
        ];
    }
}
