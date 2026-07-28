/**
 * TEMPORARY local mirrors of the DTOs in
 * `HCMv3.Application.Shared/PayrollManagement/Dto/BankFileDtos.cs`.
 *
 * Delete this file once `angular/nswag/refresh.bat` has been run -- the generated
 * `service-proxies.ts` will contain classes of exactly these shapes and the component should import those
 * instead. See ./bank-file-api.service.ts for the full switch-over checklist.
 */

export interface BankFileLookupItemDto {
    id: number;
    displayText: string;
    code: string;
}

export interface BankFileSponsorBankDto extends BankFileLookupItemDto {
    accountNumber: string;
}

export interface BankFileFormatDto {
    code: string;
    nameEn: string;
    /** Payer bank code the format belongs to, e.g. "CBK" for "CBK-T7". */
    payerBankCode: string;
    /** True when the format emits fixed-width text rather than a spreadsheet grid. */
    isTextFormat: boolean;
    /** False when the BankFile lookup row has not been seeded for this tenant yet. */
    isSeeded: boolean;
}

export interface BankFileLookupsOutput {
    payrollAreas: BankFileLookupItemDto[];
    sponsors: BankFileLookupItemDto[];
    sponsorBanks: BankFileSponsorBankDto[];
    paymentBanks: BankFileLookupItemDto[];
    employeeBanks: BankFileLookupItemDto[];
    formats: BankFileFormatDto[];
}

export interface BankFileOutput {
    fileName: string;
    base64Content: string;
    contentType: string;
    formatCode: string;
    recordCount: number;
    totalAmount: number;
}

export interface GenerateBankFileInput {
    payrollAreaId?: number;
    sponsorId?: number;
    sponsorBankId?: number;
    mosalId?: number;
    employeeCodes?: string;
    organizationUnitId?: number;
    employeeBankId?: number;
    paymentBankId: number;
    /** "R" = Regular, "O" = OffCycle, anything else = both (legacy cutoff-day branch). */
    payrollRunType: string;
    year: number;
    month: number;
    startDate: Date;
    endDate: Date;
    splitBankFiles: boolean;
}
