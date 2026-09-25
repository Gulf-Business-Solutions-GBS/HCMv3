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
    MyLetterRequestDto,
    MyLetterRequestServiceProxy,
    RequestLetterInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { WorkflowHistoryModalComponent } from '../../../shared/common/workflow-activity/workflow-history-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Self-service letter requests -- salary certificates, to-whom-it-may-concern letters, and the rest.
 *
 * <p>The type list is fetched with <c>onlyLetterTypes</c> set, so a passport or a residency permit never
 * appears as something that can be "requested as a letter". The server checks the same thing again: a
 * filtered dropdown is a convenience, not a rule.</p>
 */
@Component({
    templateUrl: './my-letter.component.html',
    animations: [appModuleAnimation()],
    imports: [
        DateInputDirective,
        CommonModule,
        FormsModule,
        SubHeaderComponent,
        LocalizePipe,
        LuxonFormatPipe,
        WorkflowHistoryModalComponent,
        HelpPageDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyLetterComponent extends MyRequestComponentBase<MyLetterRequestDto> implements OnInit {
    private readonly _service = inject(MyLetterRequestServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);

    readonly letterTypes = signal<SimpleLookupDto[]>([]);

    form = {
        documentTypeId: null as number | null,
        effectiveDate: '',
        addressToEn: '',
        addressToAr: '',
        comments: '',
    };

    ngOnInit(): void {
        this._lookups.getLetterTypes().subscribe((result) => this.letterTypes.set(result ?? []));

        this.reload();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyLetterRequestDto[] }> {
        return this._service.getMyLetterRequests(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyLetterRequest(input);
    }

    protected resetForm(): void {
        this.form = {
            documentTypeId: null,
            // The date the letter speaks as of, which is almost always today.
            effectiveDate: DateTime.local().toISODate() ?? '',
            addressToEn: '',
            addressToAr: '',
            comments: '',
        };
    }

    submit(): void {
        if (this.saving()) {
            return;
        }

        // Refused here as well as by the server, so the employee is told before the round trip.
        if (!this.form.documentTypeId) {
            this.message.warn(this.l('DocumentTypeIsRequired'));
            return;
        }

        if (!this.form.effectiveDate) {
            this.message.warn(this.l('EffectiveDateIsRequired'));
            return;
        }

        // Both addressees are required because both language templates print one; a letter missing the
        // Arabic addressee cannot be issued, and finding that out at print time is far too late.
        if (!this.form.addressToEn?.trim()) {
            this.message.warn(this.l('AddressToEnIsRequired'));
            return;
        }

        if (!this.form.addressToAr?.trim()) {
            this.message.warn(this.l('AddressToArIsRequired'));
            return;
        }

        const input = new RequestLetterInput();
        input.documentTypeId = this.form.documentTypeId;
        input.effectiveDate = DateTime.fromISO(this.form.effectiveDate);
        input.addressToEn = this.form.addressToEn.trim();
        input.addressToAr = this.form.addressToAr.trim();
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestLetter(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    typeName(row: MyLetterRequestDto): string {
        return this.pick(row.documentTypeNameEn, row.documentTypeNameAr);
    }

    addressee(row: MyLetterRequestDto): string {
        return this.pick(row.addressToEn, row.addressToAr);
    }

    letterTypeName(type: SimpleLookupDto): string {
        return this.pick(type.nameEn, type.nameAr);
    }
}
