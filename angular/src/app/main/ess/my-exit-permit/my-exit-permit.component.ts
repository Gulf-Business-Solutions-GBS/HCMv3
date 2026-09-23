import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    GetMyRequestsInput,
    MyExitPermitDto,
    MyExitPermitServiceProxy,
    RequestExitPermitInput,
    TravelReason,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { WorkflowHistoryModalComponent } from '../../../shared/common/workflow-activity/workflow-history-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Self-service exit permits.
 *
 * <p>The permit itself is obtained from the government, not from this system, so the number is keyed in
 * rather than generated -- the employee already has the document in hand when they reach this screen.</p>
 */
@Component({
    templateUrl: './my-exit-permit.component.html',
    animations: [appModuleAnimation()],
    imports: [CommonModule, FormsModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, WorkflowHistoryModalComponent, HelpPageDirective],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyExitPermitComponent extends MyRequestComponentBase<MyExitPermitDto> implements OnInit {
    private readonly _service = inject(MyExitPermitServiceProxy);

    /** Listed rather than derived, so the order on screen is a decision rather than an accident. */
    readonly travelReasons: TravelReason[] = [TravelReason.PlannedLeave, TravelReason.EmergencyLeave, TravelReason.PersonalTravel];

    form = {
        externalRequestNumber: '',
        travelReason: TravelReason.PlannedLeave,
        exitDate: '',
        startDate: '',
        comments: '',
    };

    ngOnInit(): void {
        this.reload();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyExitPermitDto[] }> {
        return this._service.getMyExitPermits(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyExitPermit(input);
    }

    protected resetForm(): void {
        this.form = {
            externalRequestNumber: '',
            travelReason: TravelReason.PlannedLeave,
            exitDate: '',
            startDate: '',
            comments: '',
        };
    }

    submit(): void {
        if (this.saving()) {
            return;
        }

        // Refused here as well as by the server, so the employee is told before the round trip.
        if (!this.form.externalRequestNumber?.trim()) {
            this.message.warn(this.l('ExitPermitNumberIsRequired'));
            return;
        }

        if (!this.form.exitDate) {
            this.message.warn(this.l('ExitDateIsRequired'));
            return;
        }

        if (!this.form.startDate) {
            this.message.warn(this.l('StartDateIsRequired'));
            return;
        }

        const input = new RequestExitPermitInput();
        input.externalRequestNumber = this.form.externalRequestNumber.trim();
        input.travelReason = this.form.travelReason;
        input.exitDate = DateTime.fromISO(this.form.exitDate);
        input.startDate = DateTime.fromISO(this.form.startDate);
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestExitPermit(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    travelReasonLabel(reason: TravelReason | undefined): string {
        if (reason === undefined || reason === null) {
            return '';
        }

        return this.l('TravelReason_' + TravelReason[reason]);
    }
}
