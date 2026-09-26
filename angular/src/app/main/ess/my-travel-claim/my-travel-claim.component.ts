import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    GetMyRequestsInput,
    MyTravelClaimDto,
    MyTravelClaimServiceProxy,
    RequestTravelClaimInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';
import { DateInputDirective } from '@shared/common/dates/date-input.directive';

/** Self-service travel expense claims. */
@Component({
    templateUrl: './my-travel-claim.component.html',
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
export class MyTravelClaimComponent extends MyRequestComponentBase<MyTravelClaimDto> implements OnInit {
    private readonly _service = inject(MyTravelClaimServiceProxy);

    form = { claimDate: '', claimAmount: null as number | null, comments: '' };

    ngOnInit(): void {
        this.reload();
        this.loadContext();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyTravelClaimDto[] }> {
        return this._service.getMyTravelClaims(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyTravelClaim(input);
    }

    protected resetForm(): void {
        this.form = { claimDate: '', claimAmount: null, comments: '' };
    }

    submit(): void {
        if (!this.form.claimDate || this.saving()) {
            return;
        }

        if (!this.form.claimAmount || this.form.claimAmount <= 0) {
            this.message.warn(this.l('ClaimAmountMustBeGreaterThanZero'));
            return;
        }

        const input = new RequestTravelClaimInput();
        input.claimDate = DateTime.fromISO(this.form.claimDate);
        input.claimAmount = this.form.claimAmount;
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestTravelClaim(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    protected detailTitle(): string {
        return this.l('EssMyTravelClaims');
    }

    protected detailFields(row: MyTravelClaimDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('ClaimDate'), value: this.detailDate(row.claimDate) },
            { label: this.l('ClaimAmount'), value: `${row.claimAmount ?? ''}` },
        ];
    }
}
