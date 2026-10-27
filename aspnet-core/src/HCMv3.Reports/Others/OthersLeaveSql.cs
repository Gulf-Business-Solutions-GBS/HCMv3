// SQL for the OthersLeave batch, rewritten for the HCMv3 schema.
//
// Held here (rather than in a .resx, where the legacy reports kept it) for exactly the reason
// PayslipSql.cs and SalaryCertificateSql.cs give: the SQL is the expensive, reviewable part of the
// port and must be diffable.
//
// MULTI-TENANCY: these run through DevExpress SqlDataSource as RAW SQL, so EF Core's per-tenant
// global query filter does NOT apply. Every table reference filters TenantId explicitly via
// @TenantId, which is bound to the hidden PrmTenantId report parameter by
// HcmReportBase.EnsureHcmParameters(). Removing an "AND TenantId = @TenantId" silently leaks rows
// across tenants. See PORTING-NOTES-OthersLeave.md for the per-query table-ref/predicate audit.

namespace HCMv3.Reports.Others
{
    /// <summary>Raw SQL used by the ported OthersLeave reports.</summary>
    public static class OthersLeaveSql
    {
        // ===================================================================================================
        // Shared rewrite notes (apply to EVERY query below)
        //
        // MECHANICAL RENAMES
        //   Employee->Employees, EmployeeOrgAssignment->EmployeeOrgAssignments,
        //   EmployeeAbsence->EmployeeAbsences, AbsenceType->AbsenceTypes,
        //   EmployeeAbsAdjustment->EmployeeAbsAdjustments,
        //   EmployeeAbsQuotaForfeit->EmployeeAbsQuotaForfeits,
        //   TimeTransfer->TimeTransfers, TimeTransferType->TimeTransferTypes,
        //   TimeRegister->TimeRegisters, ManageTimeSettings->ManageTimeSettingses,
        //   EmployeeSubGroup(Absence)->EmployeeSubGroup(Absence)s,
        //   SalaryRegister(Detail)->SalaryRegister(Detail)s, WageType->WageTypes,
        //   PayrollArea->PayrollAreas, WorkLocation->WorkLocations, Sponsor->Sponsors,
        //   EmployeeSponsor->EmployeeSponsors, GovtJobTitle->GovtJobTitles,
        //   Governate->Governorates (note the legacy spelling), OrganizationUnit->AbpOrganizationUnits
        //   OID -> Id ; <EntityName> -> <EntityName>Id (FK columns)
        //   + "AND TenantId = @TenantId" and "AND IsDeleted = 0" on EVERY table reference (zero
        //     occurrences in the legacy corpus -- HCM10 was single-tenant and hard-deleted).
        //
        // NON-MECHANICAL REWRITES
        //   1. Company_1..Company_7. Legacy EmployeeOrgAssignment carried seven flat FK columns
        //      (OrgLevel1..7) into OrganizationUnit, and each of these reports walked all seven
        //      looking for the first whose OrganizationUnitType was named 'Company'. HCMv3 has a
        //      SINGLE EmployeeOrgAssignments.OrganizationUnitId into AbpOrganizationUnits, an ABP
        //      materialised-path tree (Code/ParentId) with NO unit-type concept, so "the ancestor
        //      whose type is Company" is not expressible. The employee's own unit is used instead.
        //      *** SEMANTIC DEVIATION -- the same one PayslipSql.PayslipOrgV2Query and
        //      SalaryCertificateSql carry; needs the same product decision. ***
        //   2. Companylogo. Legacy read OrganizationUnit.logo, an image column; HCMv3 keeps logos in
        //      the file store. Companylogo stays a typed NULL placeholder and each query returns the
        //      employee's org unit as CompanylogoOrgUnitId. HcmReportBase rewires the layouts'
        //      "[Companylogo]" picture box to ICompanyLogoResolver, which applies the legacy
        //      Company_1..7 rule (top-most 'Company' unit on the employee's path) -- so the logo
        //      follows legacy even though CompanyEn here still uses the employee's own unit (note 1).
        //      CompanyAr is likewise projected from the same DisplayName as CompanyEn:
        //      AbpOrganizationUnits has one name column, not a NameEn/NameAr pair.
        //   3. EMPLOYEE FILTER. Three of these reports pulled EVERY employee in the database and
        //      then narrowed to one with a client-side report FilterString ("[Code] In (?PrmEmpCode)").
        //      That is replaced by an "AND emp.Code = @PrmEmpCode" predicate in SQL and the
        //      FilterString is dropped from the layout. This is not optional tidying: a client-side
        //      filter over a raw SqlDataSource fetches every tenant's rows to the report engine
        //      before discarding them.
        //   4. "most recent row" joins. Legacy joined dimension tables through uncorrelated
        //      "TOP 1 ... ORDER BY EndDate DESC" scalar subqueries, or through a plain LEFT JOIN
        //      that fans out one row per historical assignment. Both are rewritten as OUTER APPLY:
        //      same "most recent assignment" intent, correlation visible to the optimiser, and the
        //      row actually stays tied to the employee.
        //   5. AbsenceTypes.Code / WageTypes.Code are nvarchar in HCMv3. Legacy compared them to
        //      bare integers (Code = 3, Code = '1'); unquoted, SQL Server would implicitly convert
        //      the whole column to int and throw on the first non-numeric code. All such literals
        //      are quoted.
        //
        // LOOKUP TABLES WITH NO TenantId COLUMN: none. Every table touched below is tenant-owned
        // and soft-deleted, including AbpOrganizationUnits (TenantId is nullable there, so the
        // predicate additionally excludes host-tenant units -- which is correct for these reports).
        // ===================================================================================================

        /// <summary>
        /// rptAnnualLeaveSheet :: LeaveAccrualQuery.
        ///
        /// The annual-leave ledger for one employee up to an as-on date: opening balance, monthly
        /// accrual, vacation transactions / encashments / adjustments, and monthly forfeits, with a
        /// running balance. Straight de-XAF-ing -- the five UNION ALL legs, their ordering columns
        /// (OrderNo, RecordNo) and the SUM() OVER running balance are carried over unchanged.
        ///
        /// The legacy query already had a server-side employee filter, so rewrite note 3 does not
        /// apply here; the report's own WHERE Employee.Code = @PrmEmpCode is kept as-is.
        ///
        /// v23.2 NOTE: the legacy text began with a "--Declare" comment block. CustomSqlQuery rejects
        /// SQL that starts with DECLARE; the comments are removed rather than merely commented out so
        /// there is no chance of the leading-keyword check tripping.
        /// </summary>
        public const string AnnualLeaveSheetQuery = @"-- rptAnnualLeaveSheet :: LeaveAccrualQuery
SELECT
      emp.Code
    , emp.FullNameEn
    , subGroupAbsence.MaxLimit
    , A.TransactionType
    , A.FromDate
    , A.ToDate
    , A.ALCount
    , SUM(A.ALCount) OVER (ORDER BY emp.Code, A.OrderNo, A.FromDate, A.RecordNo) AS Balance
    , A.Comments
    , orgUnit.DisplayName                AS CompanyEn      -- see rewrite note 1
    , orgUnit.DisplayName                AS CompanyAr      -- see rewrite note 2
    , CAST(NULL AS VARBINARY(MAX))       AS Companylogo    -- placeholder, see rewrite note 2
    , orgAssignment.OrganizationUnitId   AS CompanylogoOrgUnitId
FROM (

    -- ---------------------------------------------------------------- OPENING BALANCE
    SELECT
          0 AS OrderNo
        , ROW_NUMBER() OVER (ORDER BY tt.TransferDate) AS RecordNo
        , tt.EmployeeId AS EmployeeId
        , 'OPENING BALANCE' AS TransactionType
        , tt.TransferDate AS FromDate
        , tt.TransferDate AS ToDate
        , CONVERT(DECIMAL(10,6), tt.TransferCount) AS ALCount
        , tt.Comments
    FROM TimeTransfers tt
    LEFT JOIN TimeTransferTypes ttt
           ON ttt.Id        = tt.TransferTypeId
          AND ttt.TenantId  = @TenantId
          AND ttt.IsDeleted = 0
    WHERE tt.TenantId  = @TenantId
      AND tt.IsDeleted = 0
      AND ttt.NameEn = 'Annual Leave Opening Balance'
      AND tt.TransferDate >= ISNULL((SELECT TOP 1 mts.EarliestEvaluationDate FROM ManageTimeSettingses mts WHERE mts.TenantId = @TenantId AND mts.IsDeleted = 0), CONVERT(DateTime, '1800-01-01'))
      AND tt.TransferDate <= @PrmAsOnDate
      AND tt.EmployeeId IN (SELECT TOP 1 e.Id FROM Employees e WHERE e.Code = @PrmEmpCode AND e.TenantId = @TenantId AND e.IsDeleted = 0)

    UNION ALL --================================================================================================

    -- ---------------------------------------------------------------- MONTHLY ACCRUAL
    SELECT
          1 AS OrderNo
        , ROW_NUMBER() OVER (ORDER BY MIN(tr.DayDate)) AS RecordNo
        , tr.EmployeeId AS EmployeeId
        , 'MONTHLY ACCRUAL' AS TransactionType
        , MIN(tr.DayDate) AS FromDate
        , MAX(tr.DayDate) AS ToDate
        , CONVERT(DECIMAL(10,6), SUM(tr.AnnualLeaveAccrualPerDay) - ISNULL(Opening.TransferCount, 0)) AS ALCount
        , 'MONTHLY ACCRUAL' AS Comments
    FROM TimeRegisters tr
    LEFT JOIN (
            SELECT tt.EmployeeId, SUM(tt.TransferCount) AS TransferCount
            FROM TimeTransfers tt
            LEFT JOIN TimeTransferTypes ttt
                   ON ttt.Id        = tt.TransferTypeId
                  AND ttt.TenantId  = @TenantId
                  AND ttt.IsDeleted = 0
            WHERE tt.TenantId  = @TenantId
              AND tt.IsDeleted = 0
              AND ttt.NameEn = 'Annual Leave Opening Balance'
              AND tt.TransferDate >= ISNULL((SELECT TOP 1 mts.EarliestEvaluationDate FROM ManageTimeSettingses mts WHERE mts.TenantId = @TenantId AND mts.IsDeleted = 0), CONVERT(DateTime, '1800-01-01'))
              AND tt.TransferDate <= @PrmAsOnDate
            GROUP BY tt.EmployeeId
        ) Opening ON Opening.EmployeeId = tr.EmployeeId
    WHERE tr.TenantId  = @TenantId
      AND tr.IsDeleted = 0
      AND tr.DayDate >= ISNULL((SELECT TOP 1 mts.EarliestEvaluationDate FROM ManageTimeSettingses mts WHERE mts.TenantId = @TenantId AND mts.IsDeleted = 0), CONVERT(DateTime, '1800-01-01'))
      AND tr.DayDate <= @PrmAsOnDate
      AND tr.EmployeeId IN (SELECT TOP 1 e.Id FROM Employees e WHERE e.Code = @PrmEmpCode AND e.TenantId = @TenantId AND e.IsDeleted = 0)
    GROUP BY tr.EmployeeId, Opening.TransferCount

    UNION ALL --================================================================================================

    -- ---------------------------------------------------------------- VACATION TRANSACTIONS
    SELECT
          2 AS OrderNo
        , ROW_NUMBER() OVER (ORDER BY InnerTbl.FromDate) AS RecordNo
        , InnerTbl.EmployeeId
        , InnerTbl.TransactionType
        , InnerTbl.FromDate
        , InnerTbl.ToDate
        , CONVERT(DECIMAL(10,6), InnerTbl.ALCount) AS ALCount
        , InnerTbl.Comments
    FROM (
            SELECT
                  ea.EmployeeId
                , 'VACATION TRANSACTION' AS TransactionType
                , ea.StartDate AS FromDate
                , ea.EndDate   AS ToDate
                , (-1) * ea.TotalLeaveDays AS ALCount
                , ea.Comments
            FROM EmployeeAbsences ea
            LEFT JOIN AbsenceTypes at
                   ON at.Id        = ea.AbsenceTypeId
                  AND at.TenantId  = @TenantId
                  AND at.IsDeleted = 0
            WHERE ea.TenantId  = @TenantId
              AND ea.IsDeleted = 0
              AND (at.Code = '1' OR at.QuotaType = 1)          -- see rewrite note 5
              AND ea.StartDate >= ISNULL((SELECT TOP 1 mts.EarliestEvaluationDate FROM ManageTimeSettingses mts WHERE mts.TenantId = @TenantId AND mts.IsDeleted = 0), CONVERT(DateTime, '1800-01-01'))
              AND ea.EndDate <= @PrmAsOnDate
              AND ea.EmployeeId IN (SELECT TOP 1 e.Id FROM Employees e WHERE e.Code = @PrmEmpCode AND e.TenantId = @TenantId AND e.IsDeleted = 0)

            UNION ALL

            -- Leave days already taken inside a vacation that straddles the as-on date.
            SELECT
                  tr.EmployeeId
                , 'VACATION TRANSACTION' AS TransactionType
                , MIN(tr.DayDate) AS FromDate
                , MAX(tr.DayDate) AS ToDate
                , (-1) * SUM(tr.AnnualLeaveDay) AS ALCount
                , 'Taken leave days till AsonDate' AS Comments
            FROM TimeRegisters tr
            WHERE tr.TenantId  = @TenantId
              AND tr.IsDeleted = 0
              AND tr.DayDate >= ISNULL((
                        SELECT TOP 1 ea.StartDate
                        FROM EmployeeAbsences ea
                        LEFT JOIN AbsenceTypes at
                               ON at.Id        = ea.AbsenceTypeId
                              AND at.TenantId  = @TenantId
                              AND at.IsDeleted = 0
                        WHERE ea.TenantId  = @TenantId
                          AND ea.IsDeleted = 0
                          AND (at.Code = '1' OR at.QuotaType = 1)
                          AND ea.EmployeeId IN (SELECT e.Id FROM Employees e WHERE e.Code = @PrmEmpCode AND e.TenantId = @TenantId AND e.IsDeleted = 0)
                          AND ea.StartDate <= @PrmAsOnDate
                          AND ea.EndDate   >  @PrmAsOnDate
                        ORDER BY ea.StartDate), '9999-12-31')
              AND tr.DayDate <= @PrmAsOnDate
              AND tr.EmployeeId IN (SELECT TOP 1 e.Id FROM Employees e WHERE e.Code = @PrmEmpCode AND e.TenantId = @TenantId AND e.IsDeleted = 0)
            GROUP BY tr.EmployeeId

            UNION ALL

            SELECT
                  eaa.EmployeeId
                , 'VACATION ENCASHMENT' AS TransactionType
                , eaa.EffectiveDate AS FromDate
                , eaa.EffectiveDate AS ToDate
                , (-1) * eaa.QuotaAdjusted AS ALCount
                , eaa.Comments
            FROM EmployeeAbsAdjustments eaa
            LEFT JOIN AbsenceTypes at
                   ON at.Id        = eaa.AbsenceTypeId
                  AND at.TenantId  = @TenantId
                  AND at.IsDeleted = 0
            WHERE eaa.TenantId  = @TenantId
              AND eaa.IsDeleted = 0
              AND (at.Code = '1' OR at.QuotaType = 1)
              AND eaa.AdjustmentType = 0
              AND eaa.EffectiveDate >= ISNULL((SELECT TOP 1 mts.EarliestEvaluationDate FROM ManageTimeSettingses mts WHERE mts.TenantId = @TenantId AND mts.IsDeleted = 0), CONVERT(DateTime, '1800-01-01'))
              AND eaa.EffectiveDate <= @PrmAsOnDate
              AND eaa.EmployeeId IN (SELECT TOP 1 e.Id FROM Employees e WHERE e.Code = @PrmEmpCode AND e.TenantId = @TenantId AND e.IsDeleted = 0)

            UNION ALL

            SELECT
                  eaa.EmployeeId
                , 'VACATION ADJUSTMENT' AS TransactionType
                , eaa.EffectiveDate AS FromDate
                , eaa.EffectiveDate AS ToDate
                , (-1) * eaa.QuotaAdjusted AS ALCount
                , eaa.Comments
            FROM EmployeeAbsAdjustments eaa
            LEFT JOIN AbsenceTypes at
                   ON at.Id        = eaa.AbsenceTypeId
                  AND at.TenantId  = @TenantId
                  AND at.IsDeleted = 0
            WHERE eaa.TenantId  = @TenantId
              AND eaa.IsDeleted = 0
              AND (at.Code = '1' OR at.QuotaType = 1)
              AND (eaa.AdjustmentType = 1 OR eaa.AdjustmentType = 2)
              AND eaa.EffectiveDate >= ISNULL((SELECT TOP 1 mts.EarliestEvaluationDate FROM ManageTimeSettingses mts WHERE mts.TenantId = @TenantId AND mts.IsDeleted = 0), CONVERT(DateTime, '1800-01-01'))
              AND eaa.EffectiveDate <= @PrmAsOnDate
              AND eaa.EmployeeId IN (SELECT TOP 1 e.Id FROM Employees e WHERE e.Code = @PrmEmpCode AND e.TenantId = @TenantId AND e.IsDeleted = 0)
        ) InnerTbl

    UNION ALL --================================================================================================

    -- ---------------------------------------------------------------- MONTHLY FORFEIT
    SELECT
          3 AS OrderNo
        , ROW_NUMBER() OVER (ORDER BY MIN(tr.DayDate)) AS RecordNo
        , tr.EmployeeId AS EmployeeId
        , 'MONTHLY FORFEIT' AS TransactionType
        , MIN(tr.DayDate) AS FromDate
        , MAX(tr.DayDate) AS ToDate
        , (-1) * CONVERT(DECIMAL(10,6), ISNULL(QuotaForfeitTbl.QuotaForfeited, 0)) AS ALCount
        , 'MONTHLY FORFEIT' AS Comments
    FROM TimeRegisters tr
    LEFT JOIN (
            SELECT eaqf.EmployeeId, SUM(eaqf.QuotaForfeited) AS QuotaForfeited
            FROM EmployeeAbsQuotaForfeits eaqf
            LEFT JOIN AbsenceTypes at
                   ON at.Id        = eaqf.AbsenceTypeId
                  AND at.TenantId  = @TenantId
                  AND at.IsDeleted = 0
            WHERE eaqf.TenantId  = @TenantId
              AND eaqf.IsDeleted = 0
              AND (at.Code = '1' OR at.QuotaType = 1)
              AND eaqf.EffectiveDate >= ISNULL((SELECT TOP 1 mts.EarliestEvaluationDate FROM ManageTimeSettingses mts WHERE mts.TenantId = @TenantId AND mts.IsDeleted = 0), CONVERT(DateTime, '1800-01-01'))
              AND eaqf.EffectiveDate <= @PrmAsOnDate
            GROUP BY eaqf.EmployeeId
        ) QuotaForfeitTbl ON QuotaForfeitTbl.EmployeeId = tr.EmployeeId
    WHERE tr.TenantId  = @TenantId
      AND tr.IsDeleted = 0
      AND tr.DayDate >= ISNULL((SELECT TOP 1 mts.EarliestEvaluationDate FROM ManageTimeSettingses mts WHERE mts.TenantId = @TenantId AND mts.IsDeleted = 0), CONVERT(DateTime, '1800-01-01'))
      -- End of month, so that forfeit entries (always dated month-end) are included.
      AND tr.DayDate <= EOMONTH(@PrmAsOnDate)
      AND tr.EmployeeId IN (SELECT TOP 1 e.Id FROM Employees e WHERE e.Code = @PrmEmpCode AND e.TenantId = @TenantId AND e.IsDeleted = 0)
      -- Take forfeit entries only when ForfeitRule = Monthly.
      AND (SELECT TOP 1 mts.ForfeitRule FROM ManageTimeSettingses mts WHERE mts.TenantId = @TenantId AND mts.IsDeleted = 0) = 1
    GROUP BY tr.EmployeeId, QuotaForfeitTbl.QuotaForfeited

) A

LEFT JOIN Employees emp
       ON emp.Id        = A.EmployeeId
      AND emp.TenantId  = @TenantId
      AND emp.IsDeleted = 0

-- see rewrite note 4: the legacy LEFT JOIN on a date range could match several historical
-- assignments and fan the ledger out; TOP 1 pins it to one.
OUTER APPLY (
    SELECT TOP 1 eoa.OrganizationUnitId, eoa.EmployeeSubGroupId
    FROM EmployeeOrgAssignments eoa
    WHERE eoa.EmployeeId = emp.Id
      AND eoa.TenantId   = @TenantId
      AND eoa.IsDeleted  = 0
      AND eoa.StartDate <= @PrmAsOnDate
      AND eoa.EndDate   >= @PrmAsOnDate
    ORDER BY eoa.EndDate DESC
) orgAssignment

LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id        = orgAssignment.OrganizationUnitId
      AND orgUnit.TenantId  = @TenantId
      AND orgUnit.IsDeleted = 0

LEFT JOIN EmployeeSubGroups subGroup
       ON subGroup.Id        = orgAssignment.EmployeeSubGroupId
      AND subGroup.TenantId  = @TenantId
      AND subGroup.IsDeleted = 0

LEFT JOIN EmployeeSubGroupAbsences subGroupAbsence
       ON subGroupAbsence.EmployeeSubGroupId = subGroup.Id
      AND subGroupAbsence.TenantId  = @TenantId
      AND subGroupAbsence.IsDeleted = 0
      AND subGroupAbsence.AbsenceTypeId = (SELECT TOP 1 at.Id FROM AbsenceTypes at WHERE at.Code = '1' AND at.TenantId = @TenantId AND at.IsDeleted = 0)

WHERE emp.Code = @PrmEmpCode
ORDER BY emp.Code, A.OrderNo, A.FromDate, A.RecordNo;";

        /// <summary>
        /// rptSickLeaveForm :: SickLeaveFormQuery -- the employee header block printed over the
        /// scanned Ministry of Health sick-leave form.
        ///
        /// The column ALIASES with spaces ("Work Location", "Sponsor", "Governorate") are kept
        /// verbatim: the layout's ExpressionBindings reference them by those exact names.
        ///
        /// Legacy joined EmployeeOrgAssignment and EmployeeSponsor with a plain LEFT JOIN on
        /// EmployeeId, so an employee with N assignments and M sponsor records printed N*M copies of
        /// the form. Rewritten as OUTER APPLY TOP 1 (rewrite note 4) -- this is a single-page
        /// government form and must print once.
        /// </summary>
        public const string SickLeaveFormQuery = @"-- rptSickLeaveForm :: SickLeaveFormQuery
SELECT
      emp.Code
    , emp.FullNameAr
    , emp.CivilID
    , workLocation.NameAr        AS [Work Location]
    , sponsor.NameAr             AS [Sponsor]
    , emp.City                   AS Area
    , governorate.NameAr         AS [Governorate]
    , govtJobTitle.NameAr        AS [GovtJobtitle]
FROM Employees emp

-- see rewrite note 4
OUTER APPLY (
    SELECT TOP 1 eoa.WorkLocationId
    FROM EmployeeOrgAssignments eoa
    WHERE eoa.EmployeeId = emp.Id AND eoa.TenantId = @TenantId AND eoa.IsDeleted = 0
    ORDER BY eoa.EndDate DESC
) orgAssignment

LEFT JOIN WorkLocations workLocation
       ON workLocation.Id        = orgAssignment.WorkLocationId
      AND workLocation.TenantId  = @TenantId
      AND workLocation.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 es.SponsorId, es.GovtJobTitleId
    FROM EmployeeSponsors es
    WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
    ORDER BY es.EndDate DESC
) sponsorAssignment

LEFT JOIN Sponsors sponsor
       ON sponsor.Id        = sponsorAssignment.SponsorId
      AND sponsor.TenantId  = @TenantId
      AND sponsor.IsDeleted = 0

LEFT JOIN GovtJobTitles govtJobTitle
       ON govtJobTitle.Id        = sponsorAssignment.GovtJobTitleId
      AND govtJobTitle.TenantId  = @TenantId
      AND govtJobTitle.IsDeleted = 0

-- Legacy 'Governate' spelling; HCMv3 spells the table and the FK correctly.
LEFT JOIN Governorates governorate
       ON governorate.Id        = emp.GovernorateId
      AND governorate.TenantId  = @TenantId
      AND governorate.IsDeleted = 0

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";     // see rewrite note 3

        /// <summary>
        /// srptSickLeaveForm :: Query -- the sick-leave history table printed inside
        /// rptSickLeaveForm.
        ///
        /// AbsenceTypes.Code = '3' is the legacy "sick leave" code, quoted per rewrite note 5. The
        /// join stays an INNER JOIN, as in the legacy query: an employee with no sick leave prints
        /// an empty subreport.
        /// </summary>
        public const string SickLeaveFormAbsencesQuery = @"-- srptSickLeaveForm :: Query
SELECT
      emp.FullNameAr
    , emp.Code
    , absenceType.NameAr AS [Leave Type]
    , empAbsence.StartDate
    , empAbsence.EndDate
FROM Employees emp

LEFT JOIN EmployeeAbsences empAbsence
       ON empAbsence.EmployeeId = emp.Id
      AND empAbsence.TenantId   = @TenantId
      AND empAbsence.IsDeleted  = 0

INNER JOIN AbsenceTypes absenceType
        ON absenceType.Id        = empAbsence.AbsenceTypeId
       AND absenceType.TenantId  = @TenantId
       AND absenceType.IsDeleted = 0
       AND absenceType.Code      = '3'        -- see rewrite note 5

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";     // see rewrite note 3

        /// <summary>
        /// rptSickLeaveMedicalForm :: SickLeaveFormQuery.
        ///
        /// Employee header for the medical-referral form. The legacy SELECT DISTINCT is kept: with
        /// the Company_1..7 walk collapsed to a single unit (rewrite note 1) it is now redundant,
        /// but a date-bounded org assignment can still legitimately return more than one row and the
        /// legacy print deduplicated them.
        /// </summary>
        public const string SickLeaveMedicalFormQuery = @"-- rptSickLeaveMedicalForm :: SickLeaveFormQuery
SELECT DISTINCT
      emp.FullNameEn
    , emp.FullNameAr
    , emp.JoiningDate
    , emp.ProbationEndDate
    , emp.CivilID
    , orgUnit.DisplayName          AS CompanyEn       -- see rewrite note 1
    , CAST(NULL AS VARBINARY(MAX)) AS Companylogo     -- placeholder, see rewrite note 2
    , eoa.OrganizationUnitId       AS CompanylogoOrgUnitId
FROM Employees emp

LEFT JOIN EmployeeOrgAssignments eoa
       ON eoa.EmployeeId = emp.Id
      AND eoa.TenantId   = @TenantId
      AND eoa.IsDeleted  = 0
      AND eoa.StartDate <= @PrmFormDate
      AND eoa.EndDate   >= @PrmFormDate

LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id        = eoa.OrganizationUnitId
      AND orgUnit.TenantId  = @TenantId
      AND orgUnit.IsDeleted = 0

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        /// <summary>
        /// rptMonthlyLeaveAccrual :: LeaveAccrualQuery.
        ///
        /// One row per payroll period, reading the annual-leave quota wage types off the salary
        /// register: 4021 opening, 4010 accrual, 4000 used, 5000 used-arrear, 4030 adjusted,
        /// 4022 closing. The PIVOT and the HasLastRegister / TransferCount opening-balance
        /// correction in the outer SELECT are carried over unchanged.
        ///
        /// PIVOT NOTE: the pivot source projects exactly (Number, WageType, SalaryRegisterId) so
        /// that PIVOT's implicit GROUP BY is over SalaryRegisterId alone. Tenant predicates
        /// therefore live in its WHERE clause and must NOT be added to its SELECT list -- doing so
        /// would silently change the grouping.
        /// </summary>
        public const string MonthlyLeaveAccrualQuery = @"-- rptMonthlyLeaveAccrual :: LeaveAccrualQuery
SELECT
      A.PayrollArea
    , A.CompanyEn
    , A.CompanyAr
    , A.Companylogo
    , A.CompanylogoOrgUnitId
    , A.Employee
    , A.Code
    , A.Year
    , A.MonthCount
    , A.Month
    , A.HasLastRegister
    , A.TransferCount
    , CASE WHEN A.HasLastRegister = 0 THEN A.TransferCount
           ELSE A.OpeningBalance END AS OpeningBalance
    , CASE WHEN A.HasLastRegister = 0 THEN A.QuotaAccrual - A.TransferCount
           ELSE A.QuotaAccrual END AS QuotaAccrual
    , A.QuotaUsed
    , A.QuotaUsedArrear
    , A.QuotaAdjusted
    , A.ClosingBalance
FROM (
    SELECT
          payrollArea.Name             AS PayrollArea
        , orgUnit.DisplayName          AS CompanyEn      -- see rewrite note 1
        , orgUnit.DisplayName          AS CompanyAr      -- see rewrite note 2
        , CAST(NULL AS VARBINARY(MAX)) AS Companylogo    -- placeholder, see rewrite note 2
        , eoa.OrganizationUnitId       AS CompanylogoOrgUnitId
        , emp.FullNameEn               AS Employee
        , emp.Code
        , YEAR(salaryRegister.ForPeriod)  AS Year
        , MONTH(salaryRegister.ForPeriod) AS MonthCount
        , UPPER(DATENAME(MONTH, salaryRegister.ForPeriod)) AS Month
        , CASE WHEN EXISTS (SELECT TOP 1 1
                            FROM SalaryRegisters lastSalaryRegister
                            WHERE lastSalaryRegister.EmployeeId = emp.Id
                              AND lastSalaryRegister.TenantId   = @TenantId
                              AND lastSalaryRegister.IsDeleted  = 0
                              AND lastSalaryRegister.ForPeriod  < salaryRegister.ForPeriod)
               THEN 1 ELSE 0 END AS HasLastRegister
        , ISNULL((SELECT SUM(tt.TransferCount)
                  FROM TimeTransfers tt
                  LEFT JOIN TimeTransferTypes ttt
                         ON ttt.Id        = tt.TransferTypeId
                        AND ttt.TenantId  = @TenantId
                        AND ttt.IsDeleted = 0
                  WHERE tt.EmployeeId = emp.Id
                    AND tt.TenantId   = @TenantId
                    AND tt.IsDeleted  = 0
                    AND ttt.NameEn IN ('Annual Leave Opening Balance')
                    AND tt.TransferDate >= DATEADD(MONTH, DATEDIFF(MONTH, 0, salaryRegister.ForPeriod), 0)
                    AND tt.TransferDate <= DATEADD(SECOND, -1, DATEADD(MONTH, 1, DATEADD(MONTH, DATEDIFF(MONTH, 0, salaryRegister.ForPeriod), 0)))
                 ), 0) AS TransferCount
        , salaryRegisterDetail.OpeningBalance
        , salaryRegisterDetail.QuotaAccrual
        , salaryRegisterDetail.QuotaUsed
        , salaryRegisterDetail.QuotaUsedArrear
        , salaryRegisterDetail.QuotaAdjusted
        , salaryRegisterDetail.ClosingBalance
    FROM Employees emp

    LEFT JOIN EmployeeOrgAssignments eoa
           ON eoa.EmployeeId = emp.Id
          AND eoa.TenantId   = @TenantId
          AND eoa.IsDeleted  = 0
          AND eoa.StartDate <= GETDATE()
          AND eoa.EndDate   >= GETDATE()

    LEFT JOIN PayrollAreas payrollArea
           ON payrollArea.Id        = eoa.PayrollAreaId
          AND payrollArea.TenantId  = @TenantId
          AND payrollArea.IsDeleted = 0

    LEFT JOIN AbpOrganizationUnits orgUnit
           ON orgUnit.Id        = eoa.OrganizationUnitId
          AND orgUnit.TenantId  = @TenantId
          AND orgUnit.IsDeleted = 0

    LEFT JOIN SalaryRegisters salaryRegister
           ON salaryRegister.EmployeeId = emp.Id
          AND salaryRegister.TenantId   = @TenantId
          AND salaryRegister.IsDeleted  = 0
          AND salaryRegister.PayrollRunType = 0          -- PayrollRunType.Regular
          AND salaryRegister.InPeriod = salaryRegister.ForPeriod

    LEFT JOIN (
        SELECT SalaryRegisterId
             , [4021] AS OpeningBalance
             , [4010] AS QuotaAccrual
             , [4000] AS QuotaUsed
             , [5000] AS QuotaUsedArrear
             , [4030] AS QuotaAdjusted
             , [4022] AS ClosingBalance
        FROM (
            SELECT srd.Number, wt.Code AS WageType, srd.SalaryRegisterId
            FROM SalaryRegisterDetails srd
            LEFT JOIN WageTypes wt
                   ON wt.Id        = srd.WageTypeId
                  AND wt.TenantId  = @TenantId
                  AND wt.IsDeleted = 0
            WHERE srd.TenantId  = @TenantId
              AND srd.IsDeleted = 0
        ) p
        PIVOT (
            AVG(Number)
            FOR WageType IN ([4021], [5021], [4010], [5010], [4000], [5000], [4030], [5030], [4022], [5022])
        ) AS pvt
    ) salaryRegisterDetail ON salaryRegisterDetail.SalaryRegisterId = salaryRegister.Id

    WHERE emp.TenantId  = @TenantId
      AND emp.IsDeleted = 0
      AND emp.Code      = @PrmEmpCode
) A
ORDER BY A.Code, A.Year, A.MonthCount;";

        /// <summary>
        /// rptMonthlyIndemnityAccrual :: LeaveAccrualQuery.
        ///
        /// Same shape as <see cref="MonthlyLeaveAccrualQuery"/> but over the indemnity quota wage
        /// types: 4004 accrual, 4005 used, 4025 total quota, 4006 closing. OpeningBalance is derived
        /// as TotalQuota - QuotaAccrual and QuotaAdjusted is a hard-coded 0, exactly as in the
        /// legacy query (the HasLastRegister / TransferCount correction is commented out there and
        /// is not carried over -- see the "Defects carried over" section of the porting notes).
        ///
        /// See the PIVOT note on <see cref="MonthlyLeaveAccrualQuery"/>.
        /// </summary>
        public const string MonthlyIndemnityAccrualQuery = @"-- rptMonthlyIndemnityAccrual :: LeaveAccrualQuery
SELECT
      A.PayrollArea
    , A.CompanyEn
    , A.CompanyAr
    , A.Companylogo
    , A.CompanylogoOrgUnitId
    , A.Employee
    , A.Code
    , A.Year
    , A.MonthCount
    , A.Month
    , A.OpeningBalance
    , A.QuotaAccrual
    , A.QuotaUsed
    , A.QuotaAdjusted
    , A.ClosingBalance
FROM (
    SELECT
          payrollArea.Name             AS PayrollArea
        , orgUnit.DisplayName          AS CompanyEn      -- see rewrite note 1
        , orgUnit.DisplayName          AS CompanyAr      -- see rewrite note 2
        , CAST(NULL AS VARBINARY(MAX)) AS Companylogo    -- placeholder, see rewrite note 2
        , eoa.OrganizationUnitId       AS CompanylogoOrgUnitId
        , emp.FullNameEn               AS Employee
        , emp.Code
        , YEAR(salaryRegister.ForPeriod)  AS Year
        , MONTH(salaryRegister.ForPeriod) AS MonthCount
        , DATENAME(MONTH, salaryRegister.ForPeriod) AS Month
        , ISNULL(salaryRegisterDetail.TotalQuota, 0) - ISNULL(salaryRegisterDetail.QuotaAccrual, 0) AS OpeningBalance
        , ISNULL(salaryRegisterDetail.QuotaAccrual, 0)   AS QuotaAccrual
        , ISNULL(salaryRegisterDetail.QuotaUsed, 0)      AS QuotaUsed
        , 0                                              AS QuotaAdjusted
        , ISNULL(salaryRegisterDetail.ClosingBalance, 0) AS ClosingBalance
    FROM Employees emp

    LEFT JOIN EmployeeOrgAssignments eoa
           ON eoa.EmployeeId = emp.Id
          AND eoa.TenantId   = @TenantId
          AND eoa.IsDeleted  = 0
          AND eoa.StartDate <= GETDATE()
          AND eoa.EndDate   >= GETDATE()

    LEFT JOIN PayrollAreas payrollArea
           ON payrollArea.Id        = eoa.PayrollAreaId
          AND payrollArea.TenantId  = @TenantId
          AND payrollArea.IsDeleted = 0

    LEFT JOIN AbpOrganizationUnits orgUnit
           ON orgUnit.Id        = eoa.OrganizationUnitId
          AND orgUnit.TenantId  = @TenantId
          AND orgUnit.IsDeleted = 0

    LEFT JOIN SalaryRegisters salaryRegister
           ON salaryRegister.EmployeeId = emp.Id
          AND salaryRegister.TenantId   = @TenantId
          AND salaryRegister.IsDeleted  = 0
          AND salaryRegister.PayrollRunType = 0          -- PayrollRunType.Regular
          AND salaryRegister.InPeriod = salaryRegister.ForPeriod

    LEFT JOIN (
        SELECT SalaryRegisterId
             , [4004] AS QuotaAccrual
             , [4005] AS QuotaUsed
             , [4025] AS TotalQuota
             , [4006] AS ClosingBalance
        FROM (
            SELECT srd.Number, wt.Code AS WageType, srd.SalaryRegisterId
            FROM SalaryRegisterDetails srd
            LEFT JOIN WageTypes wt
                   ON wt.Id        = srd.WageTypeId
                  AND wt.TenantId  = @TenantId
                  AND wt.IsDeleted = 0
            WHERE srd.TenantId  = @TenantId
              AND srd.IsDeleted = 0
        ) p
        PIVOT (
            AVG(Number)
            FOR WageType IN ([4025], [5025], [4004], [5004], [4005], [5005], [4006], [5006])
        ) AS pvt
    ) salaryRegisterDetail ON salaryRegisterDetail.SalaryRegisterId = salaryRegister.Id

    WHERE emp.TenantId  = @TenantId
      AND emp.IsDeleted = 0
      AND emp.Code      = @PrmEmpCode
) A
ORDER BY A.Code, A.Year, A.MonthCount;";
    }
}
