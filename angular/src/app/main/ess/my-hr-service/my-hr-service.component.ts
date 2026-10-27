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
    MyHRServiceRequestDto,
    MyHRServiceServiceProxy,
    RequestHRServiceInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { EssList } from '../shared/ess-list';
import { EssListHeadComponent } from '../shared/ess-list-head.component';
import { EssListPagerComponent } from '../shared/ess-list-pager.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/**
 * Self-service HR service requests.
 *
 * <p>The amount is deliberately optional: most HR services carry no charge at all, so a blank is sent as
 * zero rather than being refused as a missing answer.</p>
 */
@Component({
    templateUrl: './my-hr-service.component.html',
    animations: [appModuleAnimation()],
    imports: [
        DateInputDirective,
        CommonModule,
        FormsModule,
        SubHeaderComponent,
        LocalizePipe,
        LuxonFormatPipe,
        EssRequestDetailModalComponent,
        EssListHeadComponent,
        EssListPagerComponent,
        HelpPageDirective,
    ],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyHRServiceComponent extends MyRequestComponentBase<MyHRServiceRequestDto> implements OnInit {
    private readonly _service = inject(MyHRServiceServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);

    /** The list's columns, for the shared heading (sorting), filter row and pager. */
    readonly list = new EssList<MyHRServiceRequestDto>(this.rows, () => [
        { key: 'type', label: 'HRServiceType', text: (row) => this.typeName(row), filter: 'select' },
        {
            key: 'serviceDate',
            label: 'ServiceDate',
            text: (row) => this.detailDate(row.serviceDate),
            sort: (row) => row.serviceDate?.toMillis(),
        },
        { key: 'amount', label: 'Amount', text: (row) => row.amount },
        { key: 'status', label: 'Status', text: (row) => this.statusLabel(row), filter: 'select' },
    ]);

    readonly serviceTypes = signal<SimpleLookupDto[]>([]);

    form = {
        hrServiceTypeId: null as number | null,
        serviceDate: '',
        amount: null as number | null,
        comments: '',
    };

    ngOnInit(): void {
        this._lookups.getHRServiceTypes().subscribe((result) => this.serviceTypes.set(result ?? []));

        this.reload();
        this.loadContext();
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyHRServiceRequestDto[] }> {
        return this._service.getMyHRServices(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyHRService(input);
    }

    protected resetForm(): void {
        this.form = {
            hrServiceTypeId: null,
            // Legacy defaulted this to today, and most requests are for "now" -- pre-filling it saves a
            // field the employee would otherwise have to fill in with the obvious answer.
            serviceDate: DateTime.local().toISODate() ?? '',
            amount: null,
            comments: '',
        };
    }

    submit(): void {
        if (this.saving()) {
            return;
        }

        // Refused here as well as by the server, so the employee is told before the round trip.
        if (!this.form.hrServiceTypeId) {
            this.message.warn(this.l('ServiceTypeIsRequired'));
            return;
        }

        if (!this.form.serviceDate) {
            this.message.warn(this.l('ServiceDateIsRequired'));
            return;
        }

        // Zero is a real answer here; a negative one is not.
        if (this.form.amount !== null && this.form.amount < 0) {
            this.message.warn(this.l('AmountMustBeGreaterThanZero'));
            return;
        }

        const input = new RequestHRServiceInput();
        input.hrServiceTypeId = this.form.hrServiceTypeId;
        input.serviceDate = DateTime.fromISO(this.form.serviceDate);
        input.amount = this.form.amount ?? 0;
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestHRService(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    typeName(row: MyHRServiceRequestDto): string {
        return this.pick(row.hrServiceTypeNameEn, row.hrServiceTypeNameAr);
    }

    lookupName(lookup: SimpleLookupDto): string {
        return this.pick(lookup.nameEn, lookup.nameAr);
    }

    protected requestType(): string {
        return 'EmployeeHRService';
    }

    protected detailTitle(): string {
        return this.l('EssMyHRServices');
    }

    protected detailFields(row: MyHRServiceRequestDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('HRServiceType'), value: this.typeName(row) },
            { label: this.l('ServiceDate'), value: this.detailDate(row.serviceDate) },
            { label: this.l('Amount'), value: `${row.amount ?? ''}` },
        ];
    }
}
