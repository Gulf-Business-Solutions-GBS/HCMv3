import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    EssColleagueLookupDto,
    EssLookupServiceProxy,
    EssSessionServiceProxy,
    GetMyRequestsInput,
    MyDelegationDto,
    MyDelegationServiceProxy,
    RequestDelegationInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Self-service delegation of approval duties: the employee names a colleague to cover for them over a date
 * range.
 *
 * <p>The colleague list comes from the self-service lookup service, which returns active colleagues in the
 * tenant -- a staff number and a name, and nothing else about them -- with the signed-in employee already
 * removed. It is deliberately not narrowed to the employee's own department: cover most often crosses one,
 * and the delegation still routes through the normal approval chain.</p>
 */
@Component({
    templateUrl: './my-delegation.component.html',
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
export class MyDelegationComponent extends MyRequestComponentBase<MyDelegationDto> implements OnInit {
    private readonly _service = inject(MyDelegationServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);
    private readonly _sessionService = inject(EssSessionServiceProxy);

    readonly colleagues = signal<EssColleagueLookupDto[]>([]);

    private myEmployeeId: number | null = null;

    form = { delegateToId: null as number | null, startDate: '', endDate: '', comments: '' };

    ngOnInit(): void {
        // The server already leaves the employee out of their own candidate list. This is kept only so the
        // submit guard below can name the rule in the employee's own language rather than surfacing a
        // server error, for the case where a stale form posts an id the list no longer offers.
        this._sessionService.getEssContext().subscribe({
            next: (context) => (this.myEmployeeId = context.employeeId ?? null),
            error: () => undefined,
        });

        this._lookups.getDelegationCandidates().subscribe({
            next: (result) => this.colleagues.set(result ?? []),
            error: () => this.colleagues.set([]),
        });

        this.reload();
        this.loadContext();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyDelegationDto[] }> {
        return this._service.getMyDelegations(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyDelegation(input);
    }

    protected resetForm(): void {
        this.form = { delegateToId: null, startDate: '', endDate: '', comments: '' };
    }

    submit(): void {
        if (this.saving()) {
            return;
        }

        // Each of these is refused by the server too. Checking here means the employee is told which field
        // is wrong while it is still in front of them, rather than after a round trip.
        if (!this.form.delegateToId) {
            this.message.warn(this.l('SelectEmployee'));
            return;
        }

        if (this.form.delegateToId === this.myEmployeeId) {
            this.message.warn(this.l('CannotDelegateToYourself'));
            return;
        }

        if (!this.form.startDate || !this.form.endDate) {
            this.message.warn(this.l('Required'));
            return;
        }

        if (this.form.endDate < this.form.startDate) {
            this.message.warn(this.l('EndDateCannotBeEarlierThanStartDate'));
            return;
        }

        const input = new RequestDelegationInput();
        input.delegateToId = this.form.delegateToId;
        input.startDate = DateTime.fromISO(this.form.startDate);
        input.endDate = DateTime.fromISO(this.form.endDate);
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestDelegation(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    colleagueName(employee: EssColleagueLookupDto): string {
        const name = this.pick(employee.fullNameEn, employee.fullNameAr);
        return employee.code ? `${employee.code} - ${name}` : name;
    }

    delegateName(row: MyDelegationDto): string {
        const name = this.pick(row.delegateToFullNameEn, row.delegateToFullNameAr);
        return row.delegateToCode ? `${row.delegateToCode} - ${name}` : name;
    }

    protected requestType(): string {
        return 'EmployeeDelegation';
    }

    protected detailTitle(): string {
        return this.l('EssMyDelegations');
    }

    protected detailFields(row: MyDelegationDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('DelegateTo'), value: this.delegateName(row) },
            { label: this.l('StartDate'), value: this.detailDate(row.startDate) },
            { label: this.l('EndDate'), value: this.detailDate(row.endDate) },
        ];
    }
}
