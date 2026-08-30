import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import {
    AssetDto,
    AssetsServiceProxy,
    EntityDto,
    GetAllAssetsInput,
    GetMyRequestsInput,
    MyAssetRequestDto,
    MyAssetServiceProxy,
    RequestAssetInput,
} from '@shared/service-proxies/service-proxies';
import { DateTime } from 'luxon';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { signal } from '@angular/core';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { WorkflowHistoryModalComponent } from '../../../shared/common/workflow-activity/workflow-history-modal.component';
import { MyRequestComponentBase } from '../my-request-component-base';

/** Self-service asset requests. Shares its list, status and withdraw behaviour with the other ESS screens. */
@Component({
    templateUrl: './my-asset.component.html',
    animations: [appModuleAnimation()],
    imports: [CommonModule, FormsModule, SubHeaderComponent, LocalizePipe, LuxonFormatPipe, WorkflowHistoryModalComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyAssetComponent extends MyRequestComponentBase<MyAssetRequestDto> implements OnInit {
    private readonly _service = inject(MyAssetServiceProxy);
    private readonly _assetsService = inject(AssetsServiceProxy);

    readonly assets = signal<AssetDto[]>([]);

    form = { assetId: null as number | null, startDate: '', endDate: '', comments: '' };

    ngOnInit(): void {
        this._assetsService
            .getAll(new GetAllAssetsInput({ maxResultCount: 200, skipCount: 0, sorting: undefined, filter: undefined }))
            .subscribe((result) => this.assets.set(result.items));

        this.reload();
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
