import { Component, computed, inject, OnInit, signal, viewChild } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { appModuleAnimation } from '@shared/animations/routerTransition';
import { AppComponentBase } from '@shared/common/app-component-base';
import { LocalizePipe } from '@shared/common/pipes/localize.pipe';
import { PermissionPipe } from '@shared/common/pipes/permission.pipe';
import { SubHeaderComponent } from '@app/shared/common/sub-header/sub-header.component';
import { BusyIfDirective } from '@shared/utils/busy-if.directive';
import { CheckboxModule } from 'primeng/checkbox';
import { TableModule } from 'primeng/table';
import { finalize } from 'rxjs/operators';
import {
    BankFileFormatDto,
    BankFileLookupsOutput,
    BankFileOutput,
    BankFilePeriodDto,
    BankFilePeriodType,
    BankFileSponsorBankDto,
    BankFilesServiceProxy,
    GenerateBankFileInput,
    GetBankFilePeriodInput,
    PaymentType,
} from '@shared/service-proxies/service-proxies';
import { BankFileLogModalComponent } from './bank-file-log-modal.component';
import { HelpPageDirective } from '@app/shared/help/help-page.directive';

/** Legacy Payment Type radios rdPaymentAll / rdPaymentBank / rdPaymentCash. */
type PaymentTypeChoice = 'ALL' | 'BANK' | 'CASH';

/**
 * Bank salary-transfer file generation.
 *
 * Ported from the LIVE legacy screen HCM10.Web/UserControls/PayrollManagement/uscBankFile.ascx(.cs). Its 28 file
 * layouts are those of uscBankFile2 line for line; what only the live screen has, and this one now has too:
 *  - the Current / Other period choice, with read-only start and end dates (SetCurrentDates / SetDates);
 *  - the ALL / BANK / CASH payment type radios;
 *  - the Sponsor (A/C) list re-filled for the chosen sponsor (BindSponsorBank);
 *  - "View Log": every run with who, when, status and filters, and the stored copy of the file.
 *
 * Dates are never sent from here: the server works them out from the payroll area, the period type and the
 * month / year, so a browser in Kuwait and a server on UTC cannot disagree about which day the period ends.
 */
@Component({
    templateUrl: './bank-file.component.html',
    animations: [appModuleAnimation()],
    imports: [
        HelpPageDirective,
        SubHeaderComponent,
        BusyIfDirective,
        FormsModule,
        LocalizePipe,
        PermissionPipe,
        TableModule,
        CheckboxModule,
        DecimalPipe,
        BankFileLogModalComponent,
    ],
})
export class BankFileComponent extends AppComponentBase implements OnInit {
    private _bankFileService = inject(BankFilesServiceProxy);

    readonly logModal = viewChild<BankFileLogModalComponent>('logModal');

    readonly BankFilePeriodType = BankFilePeriodType;

    readonly loading = signal<boolean>(false);
    readonly generating = signal<boolean>(false);
    readonly loadingPeriod = signal<boolean>(false);

    readonly lookups = signal<BankFileLookupsOutput | undefined>(undefined);
    readonly lastResult = signal<BankFileOutput | undefined>(undefined);

    // --- filter panel (mirrors the legacy control one-for-one) ---
    readonly payrollAreaId = signal<number | undefined>(undefined);
    readonly sponsorId = signal<number | undefined>(undefined);
    readonly sponsorBankId = signal<number | undefined>(undefined);
    readonly sponsorBanks = signal<BankFileSponsorBankDto[]>([]);
    readonly mosalId = signal<number | undefined>(undefined);
    readonly employeeCodes = signal<string>('');
    readonly employeeBankId = signal<number | undefined>(undefined);
    readonly paymentBankId = signal<number | undefined>(undefined);
    readonly paymentType = signal<PaymentTypeChoice>('ALL');
    /** "R" = Regular, "O" = OffCycle, "A" = both (legacy "RO", the cutoff-day branch). */
    readonly payrollRunType = signal<string>('R');
    readonly splitBankFiles = signal<boolean>(false);

    // --- period (legacy rdCurrent is checked by default) ---
    readonly periodType = signal<BankFilePeriodType>(BankFilePeriodType.Current);
    readonly month = signal<number | undefined>(undefined);
    readonly year = signal<number | undefined>(undefined);
    readonly period = signal<BankFilePeriodDto | undefined>(undefined);

    /** Formats belonging to the currently selected payment bank -- purely informational. */
    readonly formatsForSelectedBank = computed<BankFileFormatDto[]>(() => {
        const all = this.lookups()?.formats ?? [];
        const bank = this.lookups()?.paymentBanks?.find((b) => b.id === this.paymentBankId());
        if (!bank) {
            return [];
        }

        // "GB" and "GBK" share the GBK-* format family (legacy switch fall-through).
        const key = bank.code === 'GB' ? 'GBK' : bank.code;
        return all.filter((f) => f.payerBankCode === key);
    });

    readonly canGenerate = computed(
        () =>
            !!this.paymentBankId() &&
            !!this.period()?.hasPeriod &&
            (!!this.payrollAreaId() || !!this.sponsorId() || !!this.mosalId() || !!this.employeeCodes().trim())
    );

    ngOnInit(): void {
        this.loadLookups();
        this.loadPeriod();
    }

    loadLookups(): void {
        this.loading.set(true);
        this._bankFileService
            .getBankFileLookups()
            .pipe(finalize(() => this.loading.set(false)))
            .subscribe((result) => this.lookups.set(result));
    }

    // Legacy cmbPayrollArea SelectedIndexChanged -> CallbackPanelPeriod.PerformCallback().
    onPayrollAreaChange(id: number | undefined): void {
        this.payrollAreaId.set(id);
        this.loadPeriod();
    }

    // Legacy cmbSponsor SelectedIndexChanged -> CallbackPanelSponsor (BindSponsorBank) re-fills Sponsor (A/C).
    onSponsorChange(id: number | undefined): void {
        this.sponsorId.set(id);
        this.sponsorBankId.set(undefined);
        this.sponsorBanks.set([]);

        if (!id) {
            return;
        }

        this._bankFileService.getBankFileSponsorBanks(id).subscribe((banks) => this.sponsorBanks.set(banks ?? []));
    }

    onPeriodTypeChange(type: BankFilePeriodType): void {
        this.periodType.set(type);
        this.loadPeriod();
    }

    onMonthChange(value: number | undefined): void {
        this.month.set(value ? Number(value) : undefined);
        this.loadPeriod();
    }

    onYearChange(value: number | undefined): void {
        this.year.set(value ? Number(value) : undefined);
        this.loadPeriod();
    }

    /** Legacy ASPxCallbackPanelPeriod_Callback: Current -> SetCurrentDates, Other -> SetDates. */
    loadPeriod(): void {
        const input = new GetBankFilePeriodInput();
        input.payrollAreaId = this.payrollAreaId();
        input.periodType = this.periodType();
        if (this.periodType() === BankFilePeriodType.Other) {
            input.payrollMonth = this.month();
            input.payrollYear = this.year();
        }

        this.loadingPeriod.set(true);
        this._bankFileService
            .getBankFilePeriod(input)
            .pipe(finalize(() => this.loadingPeriod.set(false)))
            .subscribe((result) => {
                this.period.set(result);

                // Current fills the month / year boxes from the control record, as legacy did; Other keeps what
                // was typed, filling a blank box with what the server used (today's month / year).
                if (result.payrollMonth) {
                    this.month.set(result.payrollMonth);
                }
                if (result.payrollYear) {
                    this.year.set(result.payrollYear);
                }
            });
    }

    generate(): void {
        if (!this.payrollAreaId() && !this.sponsorId() && !this.mosalId() && !this.employeeCodes().trim()) {
            this.message.warn(this.l('BankFileNoPayrollAreaSponsorMosalIdOrEmpCode'));
            return;
        }

        if (!this.paymentBankId()) {
            this.message.warn(this.l('BankFilePaymentBankNotSelected'));
            return;
        }

        const period = this.period();
        if (!period?.hasPeriod) {
            this.message.warn(period?.message || this.l('PayrollRunPleaseSelectPayrollArea'));
            return;
        }

        // Built by assignment: the server DTO's optional members (paymentType, the dates) must stay unset.
        const input = new GenerateBankFileInput();
        input.payrollAreaId = this.payrollAreaId();
        input.sponsorId = this.sponsorId();
        input.sponsorBankId = this.sponsorBankId();
        input.mosalId = this.mosalId();
        input.employeeCodes = this.employeeCodes();
        input.employeeBankId = this.employeeBankId();
        input.paymentBankId = this.paymentBankId()!;
        input.paymentType = this.toPaymentType(this.paymentType());
        input.payrollRunType = this.payrollRunType();
        input.periodType = this.periodType();
        input.year = period.payrollYear;
        input.month = period.payrollMonth;
        input.splitBankFiles = this.splitBankFiles();

        this.generating.set(true);
        this._bankFileService
            .generateBankFile(input)
            .pipe(finalize(() => this.generating.set(false)))
            .subscribe((result) => {
                this.lastResult.set(result);
                this.download(result);
                this.notify.success(this.l('BankFileGeneratedSuccessfully'));
            });
    }

    viewLog(): void {
        this.logModal()?.show();
    }

    private toPaymentType(choice: PaymentTypeChoice): PaymentType | undefined {
        switch (choice) {
            case 'BANK':
                return PaymentType.Bank;
            case 'CASH':
                return PaymentType.Cash;
            default:
                return undefined;
        }
    }

    /** The backend returns the file inline as base64 (same pattern as the PDF reports). */
    private download(result: BankFileOutput): void {
        if (!result.base64Content) {
            return;
        }

        const binary = atob(result.base64Content);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }

        const url = URL.createObjectURL(new Blob([bytes], { type: result.contentType }));
        const link = document.createElement('a');
        link.href = url;
        link.download = result.fileName ?? 'bank-file';
        link.click();
        URL.revokeObjectURL(url);
    }
}
