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

// PROXY REGENERATION REQUIRED -- see the same note in costing-file.component.ts.
// PostJvServiceProxy and CostingFileServiceProxy (used here only for the payroll-area lookup, which the
// costing-file service already exposes) are generated from the backend added in this same change.
import {
    CostingFileServiceProxy,
    CostingRunInputDto,
    JournalVoucherLineDto,
    PayrollAreaLookupDto,
    PayrollRunType,
    PostJvServiceProxy,
} from '@shared/service-proxies/service-proxies';

/**
 * Journal voucher posting screen. Ported from legacy
 * HCM10.Web/UserControls/PayrollManagement/uscPostJV.ascx.
 *
 * The JV line set shown here comes from the SAME backend builder that feeds the costing-file screen
 * (HCMv3.PayrollManagement.Costing.IJournalVoucherBuilder). Legacy duplicated that logic as a private
 * ExportTable(...) in each screen; this port keeps one copy.
 */
@Component({
    templateUrl: './post-jv.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, TableModule, Tooltip, DatePicker, Select, LocalizePipe, PermissionPipe],
})
export class PostJvComponent extends AppComponentBase implements OnInit {
    private _postJvService = inject(PostJvServiceProxy);
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

    readonly postMessage = signal<string>('');
    readonly journalBatchNumbers = signal<string[]>([]);
    readonly busy = signal<boolean>(false);

    readonly runTypeOptions = [
        { label: 'Regular', value: PayrollRunType.Regular },
        { label: 'OffCycle', value: PayrollRunType.OffCycle },
    ];

    readonly selectedAreaIsFiConfigured = computed(() => {
        const name = this.selectedPayrollAreaName();
        const area = this.payrollAreas().find((a) => a.name === name);
        return !!area && area.isFiConfigured;
    });

    readonly canSubmit = computed(
        () => !this.busy() && !!this.selectedPayrollAreaName() && !!this.payInDate() && !!this.postingDate()
    );

    /**
     * Posting is irreversible in the external finance system, so it additionally requires the JV to have
     * been previewed AND to balance. Legacy left this to operator discipline (its Validate button was
     * advisory); this port makes it a hard gate on the button.
     */
    readonly canPost = computed(() => this.canSubmit() && this.lines().length > 0 && this.isBalanced());

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

    previewJournalVoucher(): void {
        if (!this.canSubmit()) {
            return;
        }

        this.busy.set(true);
        this._postJvService
            .getJournalVoucherLines(this.buildInput())
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe((result) => {
                this.lines.set(result.lines ?? []);
                this.totalDebit.set(result.totalDebit);
                this.totalCredit.set(result.totalCredit);
                this.isBalanced.set(result.isBalanced);
            });
    }

    validateJournalVoucher(): void {
        if (!this.canSubmit()) {
            return;
        }

        this.busy.set(true);
        this._postJvService
            .validateJournalVoucher(this.buildInput())
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe((result) => {
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

    /** Legacy "Post JV" button -> CreatePostJV -> PostTablewiseJV_D360. */
    postJournalVoucher(): void {
        if (!this.canPost()) {
            return;
        }

        this.message.confirm(this.l('PostJournalVoucher'), this.l('AreYouSure'), (isConfirmed) => {
            if (!isConfirmed) {
                return;
            }

            this.busy.set(true);
            this._postJvService
                .postJournalVoucher(this.buildInput())
                .pipe(finalize(() => this.busy.set(false)))
                .subscribe((result) => {
                    this.postMessage.set(result.message);
                    this.journalBatchNumbers.set(result.journalBatchNumbers ?? []);

                    if (result.success) {
                        this.notify.success(result.message);
                    } else {
                        this.message.warn(result.message);
                    }
                });
        });
    }

    clearStatus(): void {
        this.busy.set(true);
        this._postJvService
            .clearProcessStatus()
            .pipe(finalize(() => this.busy.set(false)))
            .subscribe(() => {
                this.postMessage.set('');
                this.journalBatchNumbers.set([]);
                this.notify.success(this.l('SuccessfullyDeleted'));
            });
    }
}
