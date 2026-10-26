import { DateInputDirective } from '@shared/common/dates/date-input.directive';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    EntityDto,
    EssLookupServiceProxy,
    GetMyRequestsInput,
    MyAssetRequestDto,
    MyAssetServiceProxy,
    RequestAssetInput,
    SimpleLookupDto,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { signal } from '@angular/core';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { EssRequestDetailField, EssRequestDetailModalComponent } from '../shared/ess-request-detail-modal.component';
import { EssList } from '../shared/ess-list';
import { EssListHeadComponent } from '../shared/ess-list-head.component';
import { EssListPagerComponent } from '../shared/ess-list-pager.component';
import { MyRequestComponentBase } from '../my-request-component-base';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/** Self-service asset requests. Shares its list, status and withdraw behaviour with the other ESS screens. */
@Component({
    templateUrl: './my-asset.component.html',
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
export class MyAssetComponent extends MyRequestComponentBase<MyAssetRequestDto> implements OnInit {
    private readonly _service = inject(MyAssetServiceProxy);
    private readonly _lookups = inject(EssLookupServiceProxy);

    /** The list's columns, for the shared heading (sorting), filter row and pager. */
    readonly list = new EssList<MyAssetRequestDto>(this.rows, () => [
        { key: 'asset', label: 'Asset', text: (row) => this.assetName(row), filter: 'select' },
        {
            key: 'startDate',
            label: 'StartDate',
            text: (row) => this.detailDate(row.startDate),
            sort: (row) => row.startDate?.toMillis(),
        },
        {
            key: 'endDate',
            label: 'EndDate',
            text: (row) => this.detailDate(row.endDate),
            sort: (row) => row.endDate?.toMillis(),
        },
        { key: 'status', label: 'Status', text: (row) => this.statusLabel(row), filter: 'select' },
    ]);

    readonly assets = signal<SimpleLookupDto[]>([]);

    form = { assetId: null as number | null, startDate: '', endDate: '', comments: '' };

    ngOnInit(): void {
        this._lookups.getAssets().subscribe((result) => this.assets.set(result ?? []));

        this.reload();
        this.loadContext();
    }

    protected requestType(): string {
        return 'EmployeeAsset';
    }

    protected detailTitle(): string {
        return this.l('EssMyAssets');
    }

    protected detailFields(row: MyAssetRequestDto): EssRequestDetailField[] {
        return [
            { label: this.l('Employee'), value: this.employeeDisplay() },
            { label: this.l('Asset'), value: this.assetName(row) },
            { label: this.l('StartDate'), value: this.detailDate(row.startDate) },
            { label: this.l('EndDate'), value: this.detailDate(row.endDate) },
        ];
    }

    protected fetch(input: GetMyRequestsInput): Observable<{ items: MyAssetRequestDto[] }> {
        return this._service.getMyAssets(input);
    }

    protected cancelRequest(input: EntityDto): Observable<void> {
        return this._service.cancelMyAsset(input);
    }

    protected resetForm(): void {
        this.form = { assetId: null, startDate: '', endDate: '', comments: '' };
    }

    submit(): void {
        if (!this.form.assetId || !this.form.startDate || !this.form.endDate || this.saving()) {
            return;
        }

        const input = new RequestAssetInput();
        input.assetId = this.form.assetId;
        input.startDate = DateTime.fromISO(this.form.startDate);
        input.endDate = DateTime.fromISO(this.form.endDate);
        input.comments = this.form.comments;

        this.saving.set(true);

        this._service
            .requestAsset(input)
            .pipe(finalize(() => this.saving.set(false)))
            .subscribe((result) => this.notifySubmitted(result.submittedForApproval));
    }

    assetName(row: MyAssetRequestDto): string {
        return this.pick(row.assetNameEn, row.assetNameAr);
    }
}
