import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EmployeeDto,
    EmployeesServiceProxy,
    EntityDto,
    EssSessionServiceProxy,
    GetAllEmployeesInput,
    GetMyRequestsInput,
    MyDelegationDto,
    MyDelegationServiceProxy,
    RequestDelegationInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { WorkflowHistoryModalComponent } from '../../../shared/common/workflow-activity/workflow-history-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';

/**
 * Self-service delegation of approval duties: the employee names a colleague to cover for them over a date
 * range.
 *
 * <p>The colleague list comes from the general employee lookup because that is the only endpoint that
 * returns employees. It carries an HR permission, so a plain self-service account gets nothing back; the
 * screen reports that rather than presenting an empty dropdown that silently cannot be used.</p>
 */
@Component({
    templateUrl: './my-delegation.component.html',
    animations: [appModuleAnimation()],
    imports: [CommonModule, FormsModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, WorkflowHistoryModalComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyDelegationComponent extends MyRequestComponentBase<MyDelegationDto> implements OnInit {
    private readonly _service = inject(MyDelegationServiceProxy);
    private readonly _employeesService = inject(EmployeesServiceProxy);
    private readonly _sessionService = inject(EssSessionServiceProxy);

    readonly colleagues = signal<EmployeeDto[]>([]);

    /** True when the colleague lookup could not be read, so the form explains itself instead of failing on submit. */
    readonly colleagueLookupUnavailable = signal(false);

    private myEmployeeId: number | null = null;

    form = { delegateToId: null as number | null, startDate: '', endDate: '', comments: '' };

    ngOnInit(): void {
        // Needed only to keep the employee out of their own delegate list; a delegation to yourself is
        // refused by the server and is worth ruling out before it is offered.
        this._sessionService.getEssContext().subscribe({
            next: (context) => {
                this.myEmployeeId = context.employeeId ?? null;
                this.applySelfExclusion();
            },
            error: () => undefined,
        });

        this._employeesService
            .getAll(new GetAllEmployeesInput({ maxResultCount: 1000, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe({
                next: (result) => {
                    this.colleagues.set(result.items ?? []);
                    this.applySelfExclusion();
                },
                error: () => this.colleagueLookupUnavailable.set(true),
            });

        this.reload();
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

    colleagueName(employee: EmployeeDto): string {
        const name = this.pick(employee.fullNameEn, employee.fullNameAr);
        return employee.code ? `${employee.code} - ${name}` : name;
    }

    delegateName(row: MyDelegationDto): string {
        const name = this.pick(row.delegateToFullNameEn, row.delegateToFullNameAr);
        return row.delegateToCode ? `${row.delegateToCode} - ${name}` : name;
    }

    private applySelfExclusion(): void {
        if (this.myEmployeeId === null) {
            return;
        }

        this.colleagues.update((list) => list.filter((employee) => employee.id !== this.myEmployeeId));
    }
}
