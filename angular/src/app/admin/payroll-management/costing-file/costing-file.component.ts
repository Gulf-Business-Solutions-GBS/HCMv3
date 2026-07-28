import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { TableModule } from 'primeng/table';
import { Tooltip } from 'primeng/tooltip';
import { DatePicker } from 'primeng/datepicker';
import { Select } from 'primeng/select';
import { finalize } from 'rxjs/operators';
import { DateTime } from 'luxon';
import { SubHeaderComponent } from '../../../shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '../../../../shared/utils/busy-if.directive';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';

// PROXY REGENERATION REQUIRED.
// CostingFileServiceProxy / CostingRunInputDto / JournalVoucherLineDto / PayrollAreaLookupDto /
// CostingRunResultDto / JournalVoucherValidationResultDto / JournalVoucherPreviewDto do not exist in
// service-proxies.ts yet -- the backend for this screen (HCMv3.Application/PayrollManagement/
// CostingFileAppService.cs) was added in the same change, and NSwag regeneration was deliberately NOT run
// because several agents were editing service-proxies.ts concurrently. Run `angular/nswag/refresh.bat`
// against a running Web.Host once, then this file compiles as-is.
import {
    CostingFileServiceProxy,
    CostingRunInputDto,
    CostingRunResultDto,
    JournalVoucherLineDto,
    JournalVoucherValidationResultDto,
    PayrollAreaLookupDto,
    PayrollRunType,
} from '@shared/service-proxies/service-proxies';

/**
 * Cost allocation file screen. Ported from legacy
 * HCM10.Web/UserControls/PayrollManagement/uscCostingFile.ascx.
 *
 * Legacy's polling status panel (ASPxCallbackPanel + timer over a static LongOperationStatus, legacy
 * lines 2187-2460) is not reproduced: the backend runs the engine inline and returns the full
 * per-employee log in one response. See CostingFileAppService's class remarks.
 */
@Component({
    templateUrl: './costing-file.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, TableModule, Tooltip, DatePicker, Select, LocalizePipe, PermissionPipe],
})
export class CostingFileComponent extends AppComponentBase implements OnInit {
    private _costingFileService = inject(CostingFileServiceProxy);

    readonly payrollAreas = signal<PayrollAreaLookupDto[]>([]);
    readonly selectedPayrollAreaName = signal<string | undefined>(undefined);
    readonly employeeCode = signal<string>('');
    readonly payInDate = signal<Date | undefined>(undefined);
    readonly postingDate = signal<Date | undefined>(undefined);
    readonly payrollRunType = signal<PayrollRunType>(PayrollRunType.Regular);

    readonly lines = signal<JournalVoucherLineDto[]>([]);
    readonly totalDebit = signal<number>(0);
    readonly totalCredit = signal<number>(0);
    readonly isBalanced = signal<boolean>(true);

    readonly runResult = signal<CostingRunResultDto | undefined>(undefined);
    readonly busy = signal<boolean>(false);

    /** Regular / OffCycle, mirroring legacy BindPayrollRunType (uscCostingFile.ascx.cs line 1532). */
    readonly runTypeOptions = [
        { label: 'Regular', value: PayrollRunType.Regular },
        { label: 'OffCycle', value: PayrollRunType.OffCycle },
    ];

    /** Legacy blocks the run when the payroll area has no FICompany or FIBank (lines 1656-1666). */
    readonly selectedAreaIsFiConfigured = computed(() => {
        const name = this.selectedPayrollAreaName();
        const area = this.payrollAreas().find((a) => a.name === name);
        return !!area && area.isFiConfigured;
    });

    readonly canSubmit = computed(
        () => !this.busy() && !!this.selectedPayrollAreaName() && !!this.payInDate() && !!this.postingDate()
    );

    ngOnInit(): void {
        this._costingFileService.getPayrollAreasForLookup().subscribe((result) => {
            this.payrollAreas.set(result);
        });
    }

    private buildInput(): CostingRunInputDto {
        return new CostingRunInputDto({
            payrollAreaName: this.selectedPayrollAreaName(),
            employeeCode: this.employeeCode(),
            payInDate: DateTime.fromJSDate(this.payInDate()),
            postingDate: DateTime.fromJSDate(this.postingDate()),
            payrollRunType: this.payrollRunType(),
        });
    }

    /** Legacy "Generate Costing File" button. */
    generateCostingFile(): void {
        if (!this.canSubmit()) {
            return;
        }

        this.busy.set(true);
        this._costingFileService
            .generateCostingFile(this.buildInput())
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe((result) => {
                this.runResult.set(result);

                if (result.aborted) {
                    this.message.warn(this.l('CostingFileRunAborted'));
                    return;
                }

                this.notify.info(
                    this.l('CostingFileRunCompleted', result.successCount, result.failedCount, result.totalEmployees)
                );
            });
    }

    /** Loads the JV preview grid. */
    previewJournalVoucher(): void {
        if (!this.canSubmit()) {
            return;
        }

        this.busy.set(true);
        this._costingFileService
            .getJournalVoucherLines(this.buildInput())
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe((result) => {
                this.lines.set(result.lines ?? []);
                this.totalDebit.set(result.totalDebit);
                this.totalCredit.set(result.totalCredit);
                this.isBalanced.set(result.isBalanced);
            });
    }

    /** Legacy "Validate JV" button -> ExportGrid(onlyValidate: true). */
    validateJournalVoucher(): void {
        if (!this.canSubmit()) {
            return;
        }

        this.busy.set(true);
        this._costingFileService
            .validateJournalVoucher(this.buildInput())
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe((result: JournalVoucherValidationResultDto) => {
                this.totalDebit.set(result.totalDebit);
                this.totalCredit.set(result.totalCredit);
                this.isBalanced.set(result.isValid);

                if (result.isValid) {
                    this.notify.success(result.message);
                } else {
                    this.message.error(result.message);
                }
            });
    }

    /**
     * Legacy "Export" button. The backend returns the spreadsheet base64-encoded in the DTO (the
     * ReportPdfOutput pattern) rather than streaming it, so the download is assembled client-side.
     */
    exportCostingFile(): void {
        if (!this.canSubmit()) {
            return;
        }

        this.busy.set(true);
        this._costingFileService
            .exportCostingFile(this.buildInput())
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe((result) => {
                const binary = atob(result.base64Content);
                const bytes = new Uint8Array(binary.length);
                for (let i = 0; i < binary.length; i++) {
                    bytes[i] = binary.charCodeAt(i);
                }

                const blob = new Blob([bytes], {
                    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
                });

                const url = URL.createObjectURL(blob);
                const link = document.createElement('a');
                link.href = url;
                link.download = result.fileName;
                link.click();
                URL.revokeObjectURL(url);
            });
    }

    /** Legacy "Clear Status" button -> TruncateLog. */
    clearStatus(): void {
        this.busy.set(true);
        this._costingFileService
            .clearProcessStatus()
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe(() => {
                this.runResult.set(undefined);
                this.notify.success(this.l('SuccessfullyDeleted'));
            });
    }

    /** Legacy "Stop Costing File" button: raises the ProjectLog stop flag the engine loop polls. */
    requestStop(): void {
        this.busy.set(true);
        this._costingFileService
            .requestStop()
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe(() => {
                this.notify.info(this.l('CostingFileRunAborted'));
            });
    }
}
