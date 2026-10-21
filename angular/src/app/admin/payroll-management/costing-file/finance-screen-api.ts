import { Observable } from 'rxjs';
import {
    CostingPeriodContextDto,
    CostingPeriodDatesDto,
    CostingProcessStatusDto,
    CostingRunInputDto,
    FinanceRunLogOutput,
    GetCostingFileCandidatesInput,
    GetFinanceRunLogInput,
    JournalVoucherPreviewDto,
    JournalVoucherValidationResultDto,
    PayrollAreaLookupDto,
    PayrollRunCandidatesOutput,
    ReportPdfOutput,
} from '@shared/service-proxies/service-proxies';

/**
 * What the Costing File and Post JV screens both call. CostingFileServiceProxy and PostJvServiceProxy each
 * expose these under their own permission (legacy had one screen for both), so either proxy can be handed to
 * the shared pieces below.
 *
 * PROXY REGENERATION REQUIRED: these methods and DTOs come from the backend changed in the same piece of work.
 */
export interface FinanceScreenApi {
    getPayrollAreasForLookup(): Observable<PayrollAreaLookupDto[]>;
    getCostingPeriodContext(payrollAreaName: string | undefined): Observable<CostingPeriodContextDto>;
    getCostingPeriodDates(
        payrollAreaName: string | undefined,
        payrollMonth: number | undefined,
        payrollYear: number | undefined
    ): Observable<CostingPeriodDatesDto>;
    getCostingFileCandidates(body: GetCostingFileCandidatesInput | undefined): Observable<PayrollRunCandidatesOutput>;
    getCostingFileCandidateCodes(body: GetCostingFileCandidatesInput | undefined): Observable<string[]>;
    getJournalVoucherLines(body: CostingRunInputDto | undefined): Observable<JournalVoucherPreviewDto>;
    validateJournalVoucher(body: CostingRunInputDto | undefined): Observable<JournalVoucherValidationResultDto>;
    getProcessStatus(): Observable<CostingProcessStatusDto>;
    getProcessLog(body: GetFinanceRunLogInput | undefined): Observable<FinanceRunLogOutput>;
    exportProcessLog(body: GetFinanceRunLogInput | undefined): Observable<ReportPdfOutput>;
    clearProcessStatus(): Observable<void>;
    requestStop(): Observable<void>;
}

/** The backend returns spreadsheets base64-encoded (the ReportPdfOutput pattern); this saves one. */
export function downloadBase64File(output: ReportPdfOutput): void {
    const binary = atob(output.base64Content);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        bytes[i] = binary.charCodeAt(i);
    }

    const blob = new Blob([bytes], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = output.fileName;
    link.click();
    URL.revokeObjectURL(url);
}
