import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { CheckboxModule } from 'primeng/checkbox';
import { TableModule } from 'primeng/table';
import { finalize } from 'rxjs/operators';
import { BankFileApiService } from './bank-file-api.service';
import { BankFileFormatDto, BankFileLookupsOutput, BankFileOutput } from './bank-file.types';

/**
 * Bank salary-transfer file generation.
 *
 * Ported legacy screen: HCM10.Web/UserControls/PayrollManagement/uscBankFile2.ascx(.cs).
 *
 * SUPERSESSION NOTE: legacy shipped TWO code-behinds, uscBankFile.ascx.cs (5,856 lines) and
 * uscBankFile2.ascx.cs (3,357 lines). Their .ascx MARKUP is identical apart from the CodeBehind/Inherits
 * attributes, and the code-behinds are identical after CR normalisation except that uscBankFile.ascx.cs
 * carries a 2,499-line, fully commented-out duplicate of the generator (lines 2512-5010; verified to
 * contain zero non-blank, non-comment lines). Both declare the SAME 28 format codes and contain the SAME
 * 28 live format branches -- including CBK-T6, CBK-T7, BUR-G2 and BURBD-G1. So this ONE screen covers both.
 *
 * !!! AWAITING SERVICE PROXIES !!!  The backend is complete but `service-proxies.ts` was deliberately NOT
 * regenerated (concurrent agents). This component talks to the app service through the temporary
 * `BankFileApiService` shim; see that file for the post-NSwag switch-over checklist.
 */
@Component({
    templateUrl: './bank-file.component.html',
    animations: [appModuleAnimation()],
    imports: [SubHeaderComponent, BusyIfDirective, FormsModule, LocalizePipe, TableModule, CheckboxModule],
})
export class BankFileComponent extends AppComponentBase implements OnInit {
    private _api = inject(BankFileApiService);

    readonly loading = signal<boolean>(false);
    readonly generating = signal<boolean>(false);

    readonly lookups = signal<BankFileLookupsOutput | undefined>(undefined);
    readonly lastResult = signal<BankFileOutput | undefined>(undefined);

    // --- filter panel (mirrors the legacy control one-for-one) ---
    readonly payrollAreaId = signal<number | undefined>(undefined);
    readonly sponsorId = signal<number | undefined>(undefined);
    readonly sponsorBankId = signal<number | undefined>(undefined);
    readonly mosalId = signal<number | undefined>(undefined);
    readonly employeeCodes = signal<string>('');
    readonly employeeBankId = signal<number | undefined>(undefined);
    readonly paymentBankId = signal<number | undefined>(undefined);
    /** "R" = Regular, "O" = OffCycle, "A" = both (legacy's cutoff-day branch). */
    readonly payrollRunType = signal<string>('R');
    readonly splitBankFiles = signal<boolean>(false);

    readonly month = signal<number>(new Date().getMonth() === 0 ? 12 : new Date().getMonth());
    readonly year = signal<number>(
        new Date().getMonth() === 0 ? new Date().getFullYear() - 1 : new Date().getFullYear()
    );

    readonly years: number[] = Array.from({ length: 8 }, (_, i) => new Date().getFullYear() - 5 + i);
    readonly months: number[] = Array.from({ length: 12 }, (_, i) => i + 1);

    /** Period start/end derived from year+month, as the legacy screen did on payroll-area selection. */
    readonly startDate = computed(() => new Date(this.year(), this.month() - 1, 1));
    readonly endDate = computed(() => new Date(this.year(), this.month(), 0));

    /** Formats belonging to the currently selected payment bank -- purely informational. */
    readonly formatsForSelectedBank = computed<BankFileFormatDto[]>(() => {
        const all = this.lookups()?.formats ?? [];
        const bank = this.lookups()?.paymentBanks.find((b) => b.id === this.paymentBankId());
        if (!bank) {
            return [];
        }

        // "GB" and "GBK" share the GBK-* format family (legacy switch fall-through at :1303-1304).
        const key = bank.code === 'GB' ? 'GBK' : bank.code;
        return all.filter((f) => f.payerBankCode === key);
    });

    readonly canGenerate = computed(
        () =>
            !!this.paymentBankId() &&
            (!!this.payrollAreaId() || !!this.sponsorId() || !!this.mosalId() || !!this.employeeCodes().trim())
    );

    ngOnInit(): void {
        this.loadLookups();
    }

    loadLookups(): void {
        this.loading.set(true);
        this._api
            .get<BankFileLookupsOutput>('GetBankFileLookups')
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.lookups.set(result));
    }

    generate(): void {
        if (!this.canGenerate()) {
            this.message.warn(this.l('BankFileNoPayrollAreaSponsorMosalIdOrEmpCode'));
            return;
        }

        this.generating.set(true);
        this._api
            .post<BankFileOutput>('GenerateBankFile', {
                payrollAreaId: this.payrollAreaId(),
                sponsorId: this.sponsorId(),
                sponsorBankId: this.sponsorBankId(),
                mosalId: this.mosalId(),
                employeeCodes: this.employeeCodes(),
                employeeBankId: this.employeeBankId(),
                paymentBankId: this.paymentBankId(),
                payrollRunType: this.payrollRunType(),
                year: this.year(),
                month: this.month(),
                startDate: this.startDate(),
                endDate: this.endDate(),
                splitBankFiles: this.splitBankFiles(),
            })
            .pipe(finalize(() => this.generating.set(false)))
            .subscribe((result) => {
                this.lastResult.set(result);
                this.download(result);
                this.notify.success(this.l('BankFileGeneratedSuccessfully'));
            });
    }

    /** The backend returns the file inline as base64 (same pattern as the PDF reports). */
    private download(result: BankFileOutput): void {
        const binary = atob(result.base64Content);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }

        const url = URL.createObjectURL(new Blob([bytes], { type: result.contentType }));
        const link = document.createElement('a');
        link.href = url;
        link.download = result.fileName;
        link.click();
        URL.revokeObjectURL(url);
    }
}
