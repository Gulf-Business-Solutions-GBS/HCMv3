import { Component, effect, inject, input, output, signal, viewChild } from '@angular/core';
import { AppComponentBase } from '@shared/common/app-component-base';
import {
    EmployeeFundDetailDto,
    EmployeeFundDetailsServiceProxy,
    EmployeeFundExemptionDto,
    EmployeeFundExemptionsServiceProxy,
    FundPayType,
    GetAllEmployeeFundDetailsInput,
    GetAllEmployeeFundExemptionsInput,
} from '@shared/service-proxies/service-proxies';
import { TableModule } from 'primeng/table';
import { finalize } from 'rxjs/operators';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { LuxonFormatPipe } from '@shared/utils/luxon-format.pipe';
import { CreateOrEditEmployeeFundDetailModalComponent } from '../../employee-fund-details/create-or-edit-employee-fund-detail-modal.component';
import { CreateOrEditEmployeeFundExemptionModalComponent } from '../../employee-fund-exemptions/create-or-edit-employee-fund-exemption-modal.component';

type FundTab = 'details' | 'exemptions';

/**
 * A fund's Fund Detail and Fund Exemption rows as tabs under the fund -- the way legacy (LHRMS) showed the two
 * aggregated collections on the Fund form. The rows reuse the stand-alone detail / exemption dialogs, opened
 * with the fund fixed, so their rules are exactly the same.
 *
 * Saving or deleting a detail row re-totals the fund on the server (Amount, Paid Amount, Balance Amount), so
 * <see cref="changed"/> is raised for the form to fetch the new totals.
 *
 * Must not be placed inside another form: the dialogs it opens carry forms of their own.
 */
@Component({
    selector: 'employeeFundTabs',
    templateUrl: './employee-fund-tabs.component.html',
    imports: [
        BusyIfDirective,
        TableModule,
        LocalizePipe,
        PermissionPipe,
        LuxonFormatPipe,
        CreateOrEditEmployeeFundDetailModalComponent,
        CreateOrEditEmployeeFundExemptionModalComponent,
    ],
})
export class EmployeeFundTabsComponent extends AppComponentBase {
    private _detailsService = inject(EmployeeFundDetailsServiceProxy);
    private _exemptionsService = inject(EmployeeFundExemptionsServiceProxy);

    readonly fundId = input<number>();

    /**
     * For a fund not saved yet: saves it and gives its id, or undefined if it could not be saved. Called when
     * "Create new" is pressed on a tab of a new fund, as legacy let rows be added before the fund was saved.
     */
    readonly ensureFund = input<() => Promise<number | undefined>>();

    /** Raised when anything under the fund changed, so whoever shows the fund can refresh its totals. */
    readonly changed = output<void>();

    readonly detailModal = viewChild<CreateOrEditEmployeeFundDetailModalComponent>('detailModal');
    readonly exemptionModal = viewChild<CreateOrEditEmployeeFundExemptionModalComponent>('exemptionModal');

    readonly activeTab = signal<FundTab>('details');
    readonly details = signal<EmployeeFundDetailDto[]>([]);
    readonly exemptions = signal<EmployeeFundExemptionDto[]>([]);
    readonly loadingDetails = signal<boolean>(false);
    readonly loadingExemptions = signal<boolean>(false);

    private shownFundId: number | undefined;

    constructor() {
        super();
        effect(() => {
            const fundId = this.fundId();

            // Back to the first tab for a different fund -- but not when a new fund has just been saved from one
            // of its own tabs, where the tab in use must stay open.
            if (this.shownFundId && fundId !== this.shownFundId) {
                this.activeTab.set('details');
            }

            this.shownFundId = fundId;

            if (fundId) {
                this.loadDetails();
                this.loadExemptions();
            } else {
                this.details.set([]);
                this.exemptions.set([]);
            }
        });
    }

    /** The fund to add under: this one, or a new fund saved first. Undefined when that save did not happen. */
    private async resolveFundId(): Promise<number | undefined> {
        return this.fundId() ?? (await this.ensureFund()?.());
    }

    fundPayTypeName(value: FundPayType): string {
        return FundPayType[value] ?? '';
    }

    // ------------------------------------------------------------------ fund detail

    loadDetails(): void {
        const fundId = this.fundId();
        if (!fundId || !this.permission.isGranted('Pages.PersonnelAdministration.EmployeeFundDetails')) {
            return;
        }

        this.loadingDetails.set(true);
        this._detailsService
            .getAll(
                new GetAllEmployeeFundDetailsInput({
                    employeeFundId: fundId,
                    sorting: 'TransactionDate DESC',
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .pipe(finalize(() => this.loadingDetails.set(false)))
            .subscribe((result) => this.details.set(result.items));
    }

    async createDetail(): Promise<void> {
        const fundId = await this.resolveFundId();
        if (fundId) {
            this.detailModal()?.show(undefined, fundId);
        }
    }

    editDetail(record: EmployeeFundDetailDto): void {
        this.detailModal()?.show(record.id, this.fundId());
    }

    deleteDetail(record: EmployeeFundDetailDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._detailsService.delete(record.id).subscribe(() => {
                    this.notify.success(this.l('SuccessfullyDeleted'));
                    this.onDetailSaved();
                });
            }
        });
    }

    onDetailSaved(): void {
        this.loadDetails();
        this.changed.emit();
    }

    // ------------------------------------------------------------------ fund exemption

    loadExemptions(): void {
        const fundId = this.fundId();
        if (!fundId || !this.permission.isGranted('Pages.PersonnelAdministration.EmployeeFundExemptions')) {
            return;
        }

        this.loadingExemptions.set(true);
        this._exemptionsService
            .getAll(
                new GetAllEmployeeFundExemptionsInput({
                    employeeFundId: fundId,
                    sorting: 'StartDate DESC',
                    maxResultCount: 1000,
                    skipCount: 0,
                })
            )
            .pipe(finalize(() => this.loadingExemptions.set(false)))
            .subscribe((result) => this.exemptions.set(result.items));
    }

    async createExemption(): Promise<void> {
        const fundId = await this.resolveFundId();
        if (fundId) {
            this.exemptionModal()?.show(undefined, fundId);
        }
    }

    editExemption(record: EmployeeFundExemptionDto): void {
        this.exemptionModal()?.show(record.id, this.fundId());
    }

    deleteExemption(record: EmployeeFundExemptionDto): void {
        this.message.confirm(this.l('AreYouSure'), this.l('AreYouSure'), (isConfirmed) => {
            if (isConfirmed) {
                this._exemptionsService.delete(record.id).subscribe(() => {
                    this.notify.success(this.l('SuccessfullyDeleted'));
                    this.onExemptionSaved();
                });
            }
        });
    }

    onExemptionSaved(): void {
        this.loadExemptions();
        this.changed.emit();
    }
}
