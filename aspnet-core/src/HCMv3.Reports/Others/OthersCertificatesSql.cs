// SQL for the "OthersCertificates" batch -- the seven single-employee letter/certificate documents
// under HCMv3.Reports.Others, plus the three subreports two of them embed.
//
// Held here (rather than in a .resx, where the legacy reports kept it) for the reason PayslipSql.cs
// and SalaryCertificateSql.cs both give: the SQL is the expensive, reviewable part of the port and
// must be diffable. It is a SEPARATE file from SalaryCertificateSql.cs purely so that two batches
// editing the Others folder in parallel cannot lose each other's work.
//
// MULTI-TENANCY: these run through DevExpress SqlDataSource as RAW SQL, so EF Core's per-tenant
// global query filter does NOT apply. Every query filters TenantId explicitly via @TenantId, which
// HcmReportBase.EnsureHcmParameters() binds to the hidden PrmTenantId report parameter. Removing an
// "AND TenantId = @TenantId" silently leaks rows across tenants. Soft-deleted tables additionally
// carry "AND IsDeleted = 0".
//
// ===================================================================================================
// SHARED REWRITE NOTES (apply to every query below)
//
// MECHANICAL RENAMES
//   Employee->Employees, Nationality->Nationalities, JobTitle->JobTitles,
//   EmployeeOrgAssignment->EmployeeOrgAssignments, EmployeeBasicPay(Detail)->EmployeeBasicPay(Detail)s,
//   EmployeeSponsor->EmployeeSponsors, Sponsor->Sponsors, EmployeeBank->EmployeeBanks, Bank->Banks,
//   EmployeeSeparation->EmployeeSeparations, Currency->Currencies, WageType->WageTypes,
//   Worklocation->WorkLocations, DutyPeriod->DutyPeriods, Duty->Duties,
//   EmployeeDutyPeriod->EmployeeDutyPeriods, EmployeeManager->EmployeeManagers,
//   EmployeeDisciplinary->EmployeeDisciplinaries, DisciplinaryType->DisciplinaryTypes,
//   OrganizationUnit->AbpOrganizationUnits.
//   OID -> Id ; <EntityName> -> <EntityName>Id (FK columns).
//   Legacy PK/FK aliases that the DESIGNER binds by name (OID, DisciplinaryType) are preserved as
//   SQL aliases so the layout files need no edit.
//
// *** SEMANTIC DEVIATION: the "Company" org level ***
//   Legacy EmployeeOrgAssignment carried seven flat FK columns OrgLevel1..OrgLevel7 and the reports
//   picked whichever one pointed at an OrganizationUnit whose OrganizationUnitType was named
//   'Company'. HCMv3 has a SINGLE EmployeeOrgAssignments.OrganizationUnitId into AbpOrganizationUnits,
//   an ABP materialised-path tree (Code / ParentId), and the unit TYPE lives on the HCMv3-side
//   OrgUnitProfiles.OrganizationUnitTypeId rather than on the ABP table.
//   The seven aliased joins therefore collapse to ONE ancestor walk: of the employee's unit and all
//   its ancestors (ancestor <=> the employee unit's Code starts with the ancestor's Code), take the
//   shallowest one whose profile type is named 'Company'. This follows Bank/BankSql.cs, which
//   established the pattern; it is a faithful translation of the legacy intent and strictly better
//   than the flat-column version. (PayslipSql.cs / SalaryCertificateSql.cs concluded the unit type
//   was "not expressible" and fell back to the employee's own unit -- those ports are worth
//   revisiting.)
//
// *** FEATURE LOST: Companylogo ***
//   Legacy sourced OrganizationUnit.logo. AbpOrganizationUnits has no Logo column and OrgUnitProfiles
//   does not carry one either, so every Companylogo column is returned as NULL VARBINARY(MAX). The
//   XRPictureBox bound to it prints empty. Same conclusion PayslipSql and BankSql reached.
//
// *** EMPLOYEE FILTER ***
//   Several legacy reports pulled EVERY employee and narrowed to one with a client-side report
//   FilterString (rptWarningLetter: "[Code] In (?PrmEmpCode)"; the two warning-letter subreports:
//   "[C_EmpCode] In (?EmpCode) ...", "[OID] = ?DiscType"). Those are replaced by real WHERE
//   predicates. This is not optional tidying: a client-side filter over a raw SqlDataSource fetches
//   every tenant's rows into the report engine before discarding them.
//   CONSEQUENCE: rptWarningLetter.PrmEmpCode loses MultiValue = true. The report prints one
//   employee's warning letter, which is what the layout does anyway.
//
// *** v23.2 CustomSqlQuery RESTRICTIONS ***
//   CustomSqlQuery rejects SQL that starts with DECLARE / WITH / BEGIN and rejects REPLACE()
//   anywhere. rptSalaryIncrementLetter's legacy query opened with
//       Declare @DtTemp as Datetime
//       Select @DtTemp = <first of @PrmYear/@PrmMonth>
//       Select @DtTemp = DATEADD(D,-1,DATEADD(M,1,@DtTemp))    -- i.e. LAST day of that month
//   which is exactly EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)). Every @DtTemp reference is
//   substituted with that expression; the result is provably equivalent and is a plain SELECT.
// ===================================================================================================

namespace HCMv3.Reports.Others
{
    /// <summary>Raw SQL used by the OthersCertificates batch of reports.</summary>
    public static class OthersCertificatesSql
    {
        // -------------------------------------------------------------------------------------------
        // rptExperienceCertificate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// rptExperienceCertificate :: Query.
        ///
        /// Bilingual experience certificate. The legacy query dated every effective-dated join off
        /// GETDATE() (NOT off PrmDate -- PrmDate only feeds a printed label), which is preserved.
        ///
        /// REWRITES beyond the mechanical ones:
        ///   * EmployeeSeparations was a plain LEFT JOIN on EmployeeId with no date narrowing, so an
        ///     employee with two separation records (separate + rejoin + separate again) fanned the
        ///     certificate out to two pages. Narrowed to the most recent row with OUTER APPLY TOP 1.
        ///     This is a legacy bug, not a feature.
        ///   * The basic-pay derived table is kept verbatim in shape (SUM of the detail lines of the
        ///     header in force today, grouped per employee) with tenant/soft-delete predicates added.
        /// </summary>
        public const string ExperienceCertificateQuery = @"-- rptExperienceCertificate :: Query
SELECT
      emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.JoiningDate
    , emp.CivilID
    , basicPay.Amount              AS Salary
    , nationality.NameEn           AS NationalityEn
    , nationality.NameAr           AS NationalityAr
    , jobTitle.NameEn              AS JobTitleEn
    , jobTitle.NameAr              AS JobTitleAr
    , sponsor.NameEn               AS SponsorEn
    , sponsor.NameAr               AS SponsorAr
    , bank.NameEn                  AS BankEn
    , bank.NameAr                  AS BankAr
    , empBank.AccountNum
    , empBank.IBAN
    , emp.JoiningDate              AS FromDate
    , CASE WHEN separation.LastDayInService IS NOT NULL
           THEN CAST(separation.LastDayInService AS VARCHAR)
           ELSE 'Till Date'
      END                          AS ToDateEn
    , CASE WHEN separation.LastDayInService IS NOT NULL
           THEN CAST(separation.LastDayInService AS VARCHAR)
           ELSE N'حتى الآن'
      END                          AS ToDateAr
FROM Employees emp

LEFT JOIN Nationalities nationality
       ON nationality.Id        = emp.NationalityId
      AND nationality.TenantId  = @TenantId
      AND nationality.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id
      AND orgAsg.TenantId   = @TenantId
      AND orgAsg.IsDeleted  = 0
      AND orgAsg.StartDate <= GETDATE()
      AND orgAsg.EndDate   >= GETDATE()

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAsg.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

LEFT JOIN (
    SELECT bp.EmployeeId, SUM(bpd.Amount) AS Amount
    FROM EmployeeBasicPays bp
    LEFT JOIN EmployeeBasicPayDetails bpd
           ON bpd.EmployeeBasicPayId = bp.Id
          AND bpd.TenantId  = @TenantId
          AND bpd.IsDeleted = 0
    WHERE bp.TenantId  = @TenantId
      AND bp.IsDeleted = 0
      AND bp.StartDate <= GETDATE()
      AND bp.EndDate   >= GETDATE()
    GROUP BY bp.EmployeeId
) basicPay ON basicPay.EmployeeId = emp.Id

LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.EmployeeId = emp.Id
      AND empSponsor.TenantId   = @TenantId
      AND empSponsor.IsDeleted  = 0
      AND empSponsor.StartDate <= GETDATE()
      AND empSponsor.EndDate   >= GETDATE()

LEFT JOIN Sponsors sponsor
       ON sponsor.Id        = empSponsor.SponsorId
      AND sponsor.TenantId  = @TenantId
      AND sponsor.IsDeleted = 0

LEFT JOIN EmployeeBanks empBank
       ON empBank.EmployeeId = emp.Id
      AND empBank.TenantId   = @TenantId
      AND empBank.IsDeleted  = 0
      AND empBank.StartDate <= GETDATE()
      AND empBank.EndDate   >= GETDATE()

LEFT JOIN Banks bank
       ON bank.Id        = empBank.BankId
      AND bank.TenantId  = @TenantId
      AND bank.IsDeleted = 0

-- legacy plain LEFT JOIN fanned the certificate out once per separation record; narrowed to the latest
OUTER APPLY (
    SELECT TOP 1 sep.LastDayInService
    FROM EmployeeSeparations sep
    WHERE sep.EmployeeId = emp.Id
      AND sep.TenantId   = @TenantId
      AND sep.IsDeleted  = 0
    ORDER BY sep.LastDayInService DESC
) separation

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        // -------------------------------------------------------------------------------------------
        // rptSalaryContinuousCertificate / rptSalaryContinuousCertificateAr
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// rptSalaryContinuousCertificate :: Query (English).
        ///
        /// REWRITES beyond the mechanical ones:
        ///   * The seven Company_1..Company_7 org joins collapse to the ancestor walk (see header).
        ///   * Legacy joined EmployeeBasicPay unrestricted and then narrowed in the WHERE clause
        ///     ("And EBP.StartDate &lt;= @PrmDate And EBP.EndDate &gt;= @PrmDate"). Because the join is
        ///     the only reason a row exists, that WHERE turned the whole LEFT JOIN chain into an INNER
        ///     one -- an employee with no basic pay on @PrmDate got NO certificate at all. Preserved
        ///     exactly: the predicates stay in the WHERE clause, not on the join.
        ///   * The "select distinct EmployeeBasicPay, Currency from EmployeeBasicPayDetail" derived
        ///     table is kept verbatim in shape. It is genuinely multi-row when a header mixes wage
        ///     types priced in different currencies, and the legacy SELECT DISTINCT at the top level
        ///     collapsed the duplicates. SELECT DISTINCT is therefore kept too.
        ///   * CurrencyCode is ADDED (legacy Currency.Code). It replaces the XPO round-trip the legacy
        ///     BeforePrint made to build the lblMonthlySalary sentence; see
        ///     rptSalaryContinuousCertificate.Detail_BeforePrint.
        /// </summary>
        public const string SalaryContinuousCertificateEnQuery = @"-- rptSalaryContinuousCertificate :: Query
SELECT DISTINCT
      emp.FullNameEn
    , emp.JoiningDate
    , emp.ProbationEndDate
    , jobTitle.NameEn              AS Jobtitle
    , emp.CivilID
    , empBank.AccountNum
    , empBank.IBAN
    , empBank.NameEn               AS EmpBank
    , bp.Amount
    , cur.CurrencyShortFormEn
    , cur.NameEn                   AS CurrencyName
    , cur.Code                     AS CurrencyCode
    , company.DisplayName          AS CompanyEn
    , CAST(NULL AS VARBINARY(MAX)) AS Companylogo   -- FEATURE LOST: no Logo column in HCMv3
FROM Employees emp

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id
      AND orgAsg.TenantId   = @TenantId
      AND orgAsg.IsDeleted  = 0
      AND orgAsg.StartDate <= @PrmDate
      AND orgAsg.EndDate   >= @PrmDate

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAsg.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

LEFT JOIN (
    SELECT b.EmployeeId, b.AccountNum, b.IBAN, c.NameEn, b.StartDate, b.EndDate
    FROM EmployeeBanks b
    LEFT JOIN Banks c
           ON c.Id        = b.BankId
          AND c.TenantId  = @TenantId
          AND c.IsDeleted = 0
    WHERE b.TenantId  = @TenantId
      AND b.IsDeleted = 0
) empBank
       ON empBank.EmployeeId = emp.Id
      AND empBank.StartDate <= @PrmDate
      AND empBank.EndDate   >= @PrmDate

LEFT JOIN EmployeeBasicPays bp
       ON bp.EmployeeId = emp.Id
      AND bp.TenantId   = @TenantId
      AND bp.IsDeleted  = 0

LEFT JOIN (
    SELECT DISTINCT bpd.EmployeeBasicPayId, bpd.CurrencyId
    FROM EmployeeBasicPayDetails bpd
    WHERE bpd.TenantId  = @TenantId
      AND bpd.IsDeleted = 0
) bpDetail ON bpDetail.EmployeeBasicPayId = bp.Id

LEFT JOIN Currencies cur
       ON cur.Id        = bpDetail.CurrencyId
      AND cur.TenantId  = @TenantId
      AND cur.IsDeleted = 0

-- the seven OrgLevel1..7 'Company' joins, collapsed to one ancestor walk
LEFT JOIN AbpOrganizationUnits ou
       ON ou.Id        = orgAsg.OrganizationUnitId
      AND ou.TenantId  = @TenantId
      AND ou.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 anc.Id, anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId  = @TenantId
      AND anc.IsDeleted = 0
      AND ou.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode
  AND bp.StartDate <= @PrmDate
  AND bp.EndDate   >= @PrmDate;";

        /// <summary>
        /// rptSalaryContinuousCertificateAr :: Query (Arabic).
        ///
        /// Identical in shape to <see cref="SalaryContinuousCertificateEnQuery"/>; the differences are
        /// the legacy ones -- Arabic name / job title / bank / company / currency columns, and no
        /// CurrencyShortFormEn. CurrencyCode is added here too, for the same code-behind reason.
        /// </summary>
        public const string SalaryContinuousCertificateArQuery = @"-- rptSalaryContinuousCertificateAr :: Query
SELECT DISTINCT
      emp.FullNameAr
    , emp.JoiningDate
    , emp.ProbationEndDate
    , jobTitle.NameAr              AS Jobtitle
    , emp.CivilID
    , empBank.AccountNum
    , empBank.IBAN
    , empBank.NameAr               AS EmpBank
    , bp.Amount
    , cur.NameAr                   AS CurrencyName
    , cur.Code                     AS CurrencyCode
    , company.DisplayName          AS CompanyAr
    , CAST(NULL AS VARBINARY(MAX)) AS Companylogo   -- FEATURE LOST: no Logo column in HCMv3
FROM Employees emp

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id
      AND orgAsg.TenantId   = @TenantId
      AND orgAsg.IsDeleted  = 0
      AND orgAsg.StartDate <= @PrmDate
      AND orgAsg.EndDate   >= @PrmDate

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAsg.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

LEFT JOIN (
    SELECT b.EmployeeId, b.AccountNum, b.IBAN, c.NameAr, b.StartDate, b.EndDate
    FROM EmployeeBanks b
    LEFT JOIN Banks c
           ON c.Id        = b.BankId
          AND c.TenantId  = @TenantId
          AND c.IsDeleted = 0
    WHERE b.TenantId  = @TenantId
      AND b.IsDeleted = 0
) empBank
       ON empBank.EmployeeId = emp.Id
      AND empBank.StartDate <= @PrmDate
      AND empBank.EndDate   >= @PrmDate

LEFT JOIN EmployeeBasicPays bp
       ON bp.EmployeeId = emp.Id
      AND bp.TenantId   = @TenantId
      AND bp.IsDeleted  = 0

LEFT JOIN (
    SELECT DISTINCT bpd.EmployeeBasicPayId, bpd.CurrencyId
    FROM EmployeeBasicPayDetails bpd
    WHERE bpd.TenantId  = @TenantId
      AND bpd.IsDeleted = 0
) bpDetail ON bpDetail.EmployeeBasicPayId = bp.Id

LEFT JOIN Currencies cur
       ON cur.Id        = bpDetail.CurrencyId
      AND cur.TenantId  = @TenantId
      AND cur.IsDeleted = 0

-- the seven OrgLevel1..7 'Company' joins, collapsed to one ancestor walk
LEFT JOIN AbpOrganizationUnits ou
       ON ou.Id        = orgAsg.OrganizationUnitId
      AND ou.TenantId  = @TenantId
      AND ou.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 anc.Id, anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId  = @TenantId
      AND anc.IsDeleted = 0
      AND ou.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode
  AND bp.StartDate <= @PrmDate
  AND bp.EndDate   >= @PrmDate;";

        // -------------------------------------------------------------------------------------------
        // rptWhomsoeverCertificate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// rptWhomsoeverCertificate :: Query.
        ///
        /// "To whom it may concern" certificate: identity, salary, sponsor, bank and the employee's
        /// daily duty window.
        ///
        /// REWRITES beyond the mechanical ones:
        ///   * The legacy CONVERT(Datetime, convert(varchar, @PrmDate, 111)) wrapper on every date
        ///     comparison is a hand-rolled "strip the time part". Kept verbatim -- it changes results
        ///     for an effective-dated row that starts later the same day, and reproducing the legacy
        ///     print exactly matters more than tidiness here.
        ///   * Duties.StartTime / EndTime are <c>time</c> in HCMv3 (they were <c>datetime</c> in the
        ///     legacy schema). The columns are still projected as FromDate / ToDate under the legacy
        ///     aliases, and the labels bound to them carry no TextFormatString, so they render as the
        ///     time-of-day string -- which is what the certificate wants.
        /// </summary>
        public const string WhomsoeverCertificateQuery = @"-- rptWhomsoeverCertificate :: Query
SELECT
      emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.JoiningDate
    , emp.CivilID
    , basicPay.Amount              AS Salary
    , jobTitle.NameEn              AS JobTitleEn
    , jobTitle.NameAr              AS JobTitleAr
    , sponsor.NameEn               AS SponsorEn
    , sponsor.NameAr               AS SponsorAr
    , bank.NameEn                  AS BankEn
    , bank.NameAr                  AS BankAr
    , empBank.AccountNum
    , empBank.IBAN
    , duty.StartTime               AS FromDate
    , duty.EndTime                 AS ToDate
FROM Employees emp

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id
      AND orgAsg.TenantId   = @TenantId
      AND orgAsg.IsDeleted  = 0
      AND orgAsg.StartDate <= CONVERT(Datetime, CONVERT(varchar, @PrmDate, 111))
      AND orgAsg.EndDate   >= CONVERT(Datetime, CONVERT(varchar, @PrmDate, 111))

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAsg.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

LEFT JOIN (
    SELECT bp.EmployeeId, SUM(bpd.Amount) AS Amount
    FROM EmployeeBasicPays bp
    LEFT JOIN EmployeeBasicPayDetails bpd
           ON bpd.EmployeeBasicPayId = bp.Id
          AND bpd.TenantId  = @TenantId
          AND bpd.IsDeleted = 0
    WHERE bp.TenantId  = @TenantId
      AND bp.IsDeleted = 0
      AND bp.StartDate <= CONVERT(Datetime, CONVERT(varchar, @PrmDate, 111))
      AND bp.EndDate   >= CONVERT(Datetime, CONVERT(varchar, @PrmDate, 111))
    GROUP BY bp.EmployeeId
) basicPay ON basicPay.EmployeeId = emp.Id

LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.EmployeeId = emp.Id
      AND empSponsor.TenantId   = @TenantId
      AND empSponsor.IsDeleted  = 0
      AND empSponsor.StartDate <= CONVERT(Datetime, CONVERT(varchar, @PrmDate, 111))
      AND empSponsor.EndDate   >= CONVERT(Datetime, CONVERT(varchar, @PrmDate, 111))

LEFT JOIN Sponsors sponsor
       ON sponsor.Id        = empSponsor.SponsorId
      AND sponsor.TenantId  = @TenantId
      AND sponsor.IsDeleted = 0

LEFT JOIN EmployeeBanks empBank
       ON empBank.EmployeeId = emp.Id
      AND empBank.TenantId   = @TenantId
      AND empBank.IsDeleted  = 0
      AND empBank.StartDate <= CONVERT(Datetime, CONVERT(varchar, @PrmDate, 111))
      AND empBank.EndDate   >= CONVERT(Datetime, CONVERT(varchar, @PrmDate, 111))

LEFT JOIN Banks bank
       ON bank.Id        = empBank.BankId
      AND bank.TenantId  = @TenantId
      AND bank.IsDeleted = 0

LEFT JOIN EmployeeDutyPeriods empDutyPeriod
       ON empDutyPeriod.EmployeeId = emp.Id
      AND empDutyPeriod.TenantId   = @TenantId
      AND empDutyPeriod.IsDeleted  = 0
      AND empDutyPeriod.StartDate <= CONVERT(Datetime, CONVERT(varchar, @PrmDate, 111))
      AND empDutyPeriod.EndDate   >= CONVERT(Datetime, CONVERT(varchar, @PrmDate, 111))

LEFT JOIN DutyPeriods dutyPeriod
       ON dutyPeriod.Id        = empDutyPeriod.DutyPeriodId
      AND dutyPeriod.TenantId  = @TenantId
      AND dutyPeriod.IsDeleted = 0

-- First non-off, non-rest day of the week, Monday-first. Verbatim legacy precedence.
LEFT JOIN (
    SELECT
          dp.Id AS DutyPeriodId
        , CASE
            WHEN mon.Id IS NOT NULL THEN mon.StartTime
            WHEN tue.Id IS NOT NULL THEN tue.StartTime
            WHEN wed.Id IS NOT NULL THEN wed.StartTime
            WHEN thu.Id IS NOT NULL THEN thu.StartTime
            WHEN fri.Id IS NOT NULL THEN fri.StartTime
            WHEN sat.Id IS NOT NULL THEN sat.StartTime
            WHEN sun.Id IS NOT NULL THEN sun.StartTime
          END AS StartTime
        , CASE
            WHEN mon.Id IS NOT NULL THEN mon.EndTime
            WHEN tue.Id IS NOT NULL THEN tue.EndTime
            WHEN wed.Id IS NOT NULL THEN wed.EndTime
            WHEN thu.Id IS NOT NULL THEN thu.EndTime
            WHEN fri.Id IS NOT NULL THEN fri.EndTime
            WHEN sat.Id IS NOT NULL THEN sat.EndTime
            WHEN sun.Id IS NOT NULL THEN sun.EndTime
          END AS EndTime
    FROM DutyPeriods dp
    LEFT JOIN Duties mon ON mon.Id = dp.MondayDutyId    AND mon.TenantId = @TenantId AND mon.IsDeleted = 0 AND mon.IsWeeklyOff = 0 AND mon.IsRestDay = 0
    LEFT JOIN Duties tue ON tue.Id = dp.TuesdayDutyId   AND tue.TenantId = @TenantId AND tue.IsDeleted = 0 AND tue.IsWeeklyOff = 0 AND tue.IsRestDay = 0
    LEFT JOIN Duties wed ON wed.Id = dp.WednesdayDutyId AND wed.TenantId = @TenantId AND wed.IsDeleted = 0 AND wed.IsWeeklyOff = 0 AND wed.IsRestDay = 0
    LEFT JOIN Duties thu ON thu.Id = dp.ThursdayDutyId  AND thu.TenantId = @TenantId AND thu.IsDeleted = 0 AND thu.IsWeeklyOff = 0 AND thu.IsRestDay = 0
    LEFT JOIN Duties fri ON fri.Id = dp.FridayDutyId    AND fri.TenantId = @TenantId AND fri.IsDeleted = 0 AND fri.IsWeeklyOff = 0 AND fri.IsRestDay = 0
    LEFT JOIN Duties sat ON sat.Id = dp.SaturdayDutyId  AND sat.TenantId = @TenantId AND sat.IsDeleted = 0 AND sat.IsWeeklyOff = 0 AND sat.IsRestDay = 0
    LEFT JOIN Duties sun ON sun.Id = dp.SundayDutyId    AND sun.TenantId = @TenantId AND sun.IsDeleted = 0 AND sun.IsWeeklyOff = 0 AND sun.IsRestDay = 0
    WHERE dp.TenantId  = @TenantId
      AND dp.IsDeleted = 0
) duty ON duty.DutyPeriodId = dutyPeriod.Id

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        // -------------------------------------------------------------------------------------------
        // rptEmployeeConfirmationLetter
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// rptEmployeeConfirmationLetter :: Query.
        ///
        /// Probation-confirmation letter. The lightest query in the batch: identity, probation end,
        /// designation and the company letterhead line. The seven Company_1..7 joins collapse to the
        /// ancestor walk (see header); Companylogo is NULL (FEATURE LOST).
        /// </summary>
        public const string EmployeeConfirmationLetterQuery = @"-- rptEmployeeConfirmationLetter :: Query
SELECT
      emp.FullNameEn
    , emp.JoiningDate
    , emp.ProbationEndDate
    , jobTitle.NameEn              AS Jobtitle
    , company.DisplayName          AS CompanyEn
    , CAST(NULL AS VARBINARY(MAX)) AS Companylogo   -- FEATURE LOST: no Logo column in HCMv3
FROM Employees emp

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id
      AND orgAsg.TenantId   = @TenantId
      AND orgAsg.IsDeleted  = 0
      AND orgAsg.StartDate <= GETDATE()
      AND orgAsg.EndDate   >= GETDATE()

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAsg.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

-- the seven OrgLevel1..7 'Company' joins, collapsed to one ancestor walk
LEFT JOIN AbpOrganizationUnits ou
       ON ou.Id        = orgAsg.OrganizationUnitId
      AND ou.TenantId  = @TenantId
      AND ou.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 anc.Id, anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId  = @TenantId
      AND anc.IsDeleted = 0
      AND ou.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        // -------------------------------------------------------------------------------------------
        // rptSalaryIncrementLetter (+ srptEmpProfileWageDetailDatewise)
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// rptSalaryIncrementLetter :: EmpProfQuery.
        ///
        /// Finds employees whose job title OR basic pay changed inside (@PrmMonth, @PrmYear), and
        /// projects the before/after pair.
        ///
        /// REWRITES beyond the mechanical ones:
        ///   * @DtTemp is substituted with EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) -- see the
        ///     v23.2 CustomSqlQuery note in the file header. Same value, no DECLARE.
        ///   * The PayrollArea join projected nothing and is dropped (same call BankSql made).
        ///   * The seven Company_1..7 joins collapse to the ancestor walk; Companylogo is NULL.
        ///
        /// LEGACY BEHAVIOUR KEPT DELIBERATELY:
        ///   * <c>Employee.Code = CASE WHEN @PrmEmpCode = '' THEN Employee.Code ELSE @PrmEmpCode END</c>
        ///     -- an empty code means "every employee", which is how this letter is run in bulk.
        ///     Unlike the other reports in this batch it is therefore NOT single-employee, and the
        ///     legacy ORDER BY Employee.Code is kept.
        ///   * <c>Employee.JoiningDate != Curr_EmployeeBasicPay.StartDate</c> -- suppresses the letter
        ///     for the initial hire package.
        ///   * The self-joins that find the PREVIOUS row use the legacy
        ///     "EndDate &gt;= DATEADD(day,-1,Curr.StartDate) AND StartDate &lt;= DATEADD(day,-1,Curr.StartDate)"
        ///     shape verbatim.
        /// </summary>
        public const string SalaryIncrementLetterQuery = @"-- rptSalaryIncrementLetter :: EmpProfQuery
SELECT DISTINCT
      emp.FullNameEn
    , emp.Code
    , emp.JoiningDate
    , emp.ProbationEndDate
    , emp.BirthDate
    , emp.CivilID
    , CASE WHEN emp.Gender = 0 THEN 'M' WHEN emp.Gender = 1 THEN 'F' ELSE ' ' END AS Gender
    , emp.Telephone
    , empManager.FullNameEn        AS EmployeeManager

    , currBasicPay.StartDate       AS OrgAssignmentEffectivedate
    , currJobTitle.NameEn          AS Curr_Jobtitle
    , prevJobTitle.NameEn          AS Prev_Jobtitle

    , currBasicPay.StartDate       AS BasicPayEffectivedate
    , currBasicPay.Amount          AS Curr_EmployeeBasicPay
    , prevBasicPay.Amount          AS Prev_EmployeeBasicPay
    , prevBasicPay.StartDate       AS Prev_BasicPayEffectivedate
    , ISNULL(currency.Code,   'KWD')          AS CurrencyCode
    , ISNULL(currency.NameEn, 'Kuwaiti Dinar') AS CurrencyName
    , CAST(NULL AS VARBINARY(MAX)) AS Companylogo   -- FEATURE LOST: no Logo column in HCMv3
FROM Employees emp

-- ============================== Job Title ==============================
LEFT JOIN EmployeeOrgAssignments currOrgAsg
       ON currOrgAsg.EmployeeId = emp.Id
      AND currOrgAsg.TenantId   = @TenantId
      AND currOrgAsg.IsDeleted  = 0
      AND currOrgAsg.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND currOrgAsg.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

LEFT JOIN JobTitles currJobTitle
       ON currJobTitle.Id        = currOrgAsg.JobTitleId
      AND currJobTitle.TenantId  = @TenantId
      AND currJobTitle.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments prevOrgAsg
       ON prevOrgAsg.EmployeeId = emp.Id
      AND prevOrgAsg.TenantId   = @TenantId
      AND prevOrgAsg.IsDeleted  = 0
      AND prevOrgAsg.EndDate   >= DATEADD(Day, -1, currOrgAsg.StartDate)
      AND prevOrgAsg.StartDate <= DATEADD(Day, -1, currOrgAsg.StartDate)

LEFT JOIN JobTitles prevJobTitle
       ON prevJobTitle.Id        = prevOrgAsg.JobTitleId
      AND prevJobTitle.TenantId  = @TenantId
      AND prevJobTitle.IsDeleted = 0

-- ============================== Basic Pay ==============================
LEFT JOIN EmployeeBasicPays currBasicPay
       ON currBasicPay.EmployeeId = emp.Id
      AND currBasicPay.TenantId   = @TenantId
      AND currBasicPay.IsDeleted  = 0
      AND currBasicPay.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND currBasicPay.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

LEFT JOIN EmployeeBasicPays prevBasicPay
       ON prevBasicPay.EmployeeId = emp.Id
      AND prevBasicPay.TenantId   = @TenantId
      AND prevBasicPay.IsDeleted  = 0
      AND prevBasicPay.EndDate   >= DATEADD(Day, -1, currBasicPay.StartDate)
      AND prevBasicPay.StartDate <= DATEADD(Day, -1, currBasicPay.StartDate)

-- Currency of the current basic-pay header's wage-type '1000' (Basic Salary) line. ADDED by the port:
-- it replaces the XPO round-trip the legacy code-behind made in GetSalaryAmountText() to build the
-- lblMonthlySalary sentence. Pinned to Code '1000' rather than the first detail row (which is
-- order-undefined) so the letter is reproducible -- same call SalaryCertificateSql made. Code is
-- nvarchar in HCMv3, so the literal MUST stay quoted.
OUTER APPLY (
    SELECT TOP 1 cur.Code, cur.NameEn
    FROM EmployeeBasicPayDetails bpd
    JOIN WageTypes wt
      ON wt.Id = bpd.WageTypeId AND wt.TenantId = @TenantId AND wt.IsDeleted = 0 AND wt.Code = '1000'
    LEFT JOIN Currencies cur
      ON cur.Id = bpd.CurrencyId AND cur.TenantId = @TenantId AND cur.IsDeleted = 0
    WHERE bpd.EmployeeBasicPayId = currBasicPay.Id
      AND bpd.TenantId  = @TenantId
      AND bpd.IsDeleted = 0
) currency

-- ============================== Manager ================================
LEFT JOIN EmployeeManagers empMgrAsg
       ON empMgrAsg.EmployeeId = emp.Id
      AND empMgrAsg.TenantId   = @TenantId
      AND empMgrAsg.IsDeleted  = 0
      AND empMgrAsg.StartDate <= @PrmDate
      AND empMgrAsg.EndDate   >= @PrmDate

LEFT JOIN Employees empManager
       ON empManager.Id        = empMgrAsg.ManagerId
      AND empManager.TenantId  = @TenantId
      AND empManager.IsDeleted = 0

-- the seven OrgLevel1..7 'Company' joins, collapsed to one ancestor walk
LEFT JOIN AbpOrganizationUnits ou
       ON ou.Id        = currOrgAsg.OrganizationUnitId
      AND ou.TenantId  = @TenantId
      AND ou.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 anc.Id, anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId  = @TenantId
      AND anc.IsDeleted = 0
      AND ou.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code = CASE WHEN @PrmEmpCode = '' THEN emp.Code ELSE @PrmEmpCode END
  AND emp.JoiningDate <> currBasicPay.StartDate
  AND (
        (   currJobTitle.Id <> prevJobTitle.Id
        AND DATEPART(Month, currOrgAsg.StartDate) = @PrmMonth
        AND DATEPART(Year,  currOrgAsg.StartDate) = @PrmYear )
     OR (   currBasicPay.Amount <> prevBasicPay.Amount
        AND DATEPART(Month, currBasicPay.StartDate) = @PrmMonth
        AND DATEPART(Year,  currBasicPay.StartDate) = @PrmYear )
      )
ORDER BY emp.Code;";

        /// <summary>
        /// rptSalaryIncrementLetter :: EmpSalaryDetail, and srptEmpProfileWageDetailDatewise :: EmpProfQuery.
        ///
        /// The two legacy files carried byte-identical SQL (the parent's copy had a commented-out
        /// "D.PreviousAmount" line, the only difference). One constant serves both.
        ///
        /// The correlated scalar "(select CurrencyShortFormEn from Currency where OID = B.Currency)"
        /// becomes a LEFT JOIN so the tenant predicate can be applied to it; the alias Currency is
        /// preserved because the subreport binds [Currency] by name.
        /// </summary>
        public const string EmployeeWageDetailDatewiseQuery = @"-- rptSalaryIncrementLetter :: EmpSalaryDetail / srptEmpProfileWageDetailDatewise :: EmpProfQuery
SELECT
      emp.Code                     AS [Employee Code]
    , emp.FullNameEn               AS [Employee Name]
    , bp.StartDate
    , bp.EndDate
    , wageType.NameEn
    , bpd.Amount
    , cur.CurrencyShortFormEn      AS Currency
FROM Employees emp

LEFT JOIN EmployeeBasicPays bp
       ON bp.EmployeeId = emp.Id
      AND bp.TenantId   = @TenantId
      AND bp.IsDeleted  = 0

LEFT JOIN EmployeeBasicPayDetails bpd
       ON bpd.EmployeeBasicPayId = bp.Id
      AND bpd.TenantId  = @TenantId
      AND bpd.IsDeleted = 0

LEFT JOIN WageTypes wageType
       ON wageType.Id        = bpd.WageTypeId
      AND wageType.TenantId  = @TenantId
      AND wageType.IsDeleted = 0

LEFT JOIN Currencies cur
       ON cur.Id        = bpd.CurrencyId
      AND cur.TenantId  = @TenantId
      AND cur.IsDeleted = 0

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode
  AND bp.StartDate  = @PrmDate;";

        // -------------------------------------------------------------------------------------------
        // rptWarningLetter (+ srptWarningLetterCount, srptWarningLetterTotalCount)
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// rptWarningLetter :: EmpDiscReportQuery.
        ///
        /// *** SCHEMA CHANGE: EmployeeDisciplinaryDetail no longer exists. ***
        ///   Legacy modelled a disciplinary event as a header (EmployeeDisciplinary) with N type
        ///   lines (EmployeeDisciplinaryDetail -> DisciplinaryType). HCMv3 collapsed that to ONE
        ///   type per event: EmployeeDisciplinaries.DisciplinaryTypeId. The two-hop join therefore
        ///   becomes a single join, and the legacy report's ability to print several types for one
        ///   event disappears with the data model, not with this port. Note the legacy
        ///   DiscCountQuery in this same file ALREADY read empdisc.DisciplinaryType directly, so the
        ///   two legacy queries disagreed with each other; HCMv3 makes them agree.
        ///
        /// REWRITES beyond the mechanical ones:
        ///   * The two correlated "TOP 1 ... ORDER BY EndDate DESC" scalar subqueries for work
        ///     location and job title become one OUTER APPLY over the latest org assignment. Same
        ///     "most recent assignment" intent, correlation visible to the optimiser, and -- unlike
        ///     the legacy form -- the row stays tied to the employee (the legacy form joined on the
        ///     value alone, so any work location sharing that id matched).
        ///   * The report's client-side FilterString "[Code] In (?PrmEmpCode)" becomes
        ///     "emp.Code = @PrmEmpCode". See the EMPLOYEE FILTER note in the file header.
        /// </summary>
        public const string WarningLetterQuery = @"-- rptWarningLetter :: EmpDiscReportQuery
SELECT
      emp.Code
    , emp.FullNameAr               AS [Emp Name]
    , emp.CivilID
    , workLoc.NameAr               AS [OrgUnit]
    , jobTitle.NameAr              AS [Job Title]
    , empDisc.DisciplinaryDate
    , discType.Id                  AS [Disc Oid]
    , discType.NameAr              AS [Disc type]
FROM Employees emp

OUTER APPLY (
    SELECT TOP 1 orgAsg.WorkLocationId, orgAsg.JobTitleId
    FROM EmployeeOrgAssignments orgAsg
    WHERE orgAsg.EmployeeId = emp.Id
      AND orgAsg.TenantId   = @TenantId
      AND orgAsg.IsDeleted  = 0
    ORDER BY orgAsg.EndDate DESC
) latestOrgAsg

LEFT JOIN WorkLocations workLoc
       ON workLoc.Id        = latestOrgAsg.WorkLocationId
      AND workLoc.TenantId  = @TenantId
      AND workLoc.IsDeleted = 0

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = latestOrgAsg.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

LEFT JOIN EmployeeDisciplinaries empDisc
       ON empDisc.EmployeeId = emp.Id
      AND empDisc.TenantId   = @TenantId
      AND empDisc.IsDeleted  = 0

-- HCMv3 has no EmployeeDisciplinaryDetail; the type hangs off the header
LEFT JOIN DisciplinaryTypes discType
       ON discType.Id        = empDisc.DisciplinaryTypeId
      AND discType.TenantId  = @TenantId
      AND discType.IsDeleted = 0

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        /// <summary>
        /// rptWarningLetter :: DiscTypeQuery, and srptWarningLetterCount :: Query /
        /// srptWarningLetterTotalCount :: DiscQuery -- the disciplinary-type look-up list behind the
        /// PrmDiscType / DiscType combo.
        ///
        /// <c>Id</c> is aliased <c>OID</c> because the parameters' DynamicListLookUpSettings bind
        /// ValueMember = "OID"; keeping the alias means the designer files need no edit.
        /// </summary>
        public const string DisciplinaryTypeListQuery = @"-- rptWarningLetter :: DiscTypeQuery (disciplinary-type look-up)
SELECT
      discType.Id   AS OID
    , discType.Code
    , discType.NameAr
FROM DisciplinaryTypes discType
WHERE discType.TenantId  = @TenantId
  AND discType.IsDeleted = 0
ORDER BY discType.NameAr;";

        /// <summary>
        /// rptWarningLetter :: DiscCountQuery.
        ///
        /// Nothing in the layout binds this query -- it is a designer leftover -- but it is part of
        /// the data source and is kept, tenant-scoped.
        /// </summary>
        public const string DisciplinaryCountQuery = @"-- rptWarningLetter :: DiscCountQuery
SELECT
      COUNT(empDisc.DisciplinaryTypeId) AS [Count]
    , empDisc.DisciplinaryTypeId        AS countDiscTypeOid
    , empDisc.EmployeeId                AS countEmpOid
    , emp.Code                          AS empcode
FROM EmployeeDisciplinaries empDisc
LEFT JOIN Employees emp
       ON emp.Id        = empDisc.EmployeeId
      AND emp.TenantId  = @TenantId
      AND emp.IsDeleted = 0
WHERE empDisc.TenantId  = @TenantId
  AND empDisc.IsDeleted = 0
GROUP BY empDisc.DisciplinaryTypeId, empDisc.EmployeeId, emp.Code;";

        /// <summary>
        /// srptWarningLetterTotalCount :: Query.
        ///
        /// How many warnings of the selected type this employee already has, and when the last one
        /// was. Legacy grouped over EmployeeDisciplinaryDetail, which no longer exists (see
        /// <see cref="WarningLetterQuery"/>); it groups over EmployeeDisciplinaries instead.
        ///
        /// The subreport's client-side FilterString
        /// <c>"[C_EmpCode] In (?EmpCode) And [DisciplinaryType] = ?DiscType"</c> becomes WHERE
        /// predicates. The alias <c>DisciplinaryType</c> is preserved even though the column is now
        /// <c>DisciplinaryTypeId</c>, so nothing downstream has to change.
        ///
        /// LEGACY DEFECT REPRODUCED: the GROUP BY includes DisciplinaryDate, so "Count" is the number
        /// of warnings of that type on ONE DAY, not the running total the label claims
        /// ("Count: N | Last Committed on: ..."). Left as-is -- correcting it would change what the
        /// document prints. The correct form would group by employee and type only and take
        /// MAX(DisciplinaryDate).
        /// </summary>
        public const string WarningLetterTotalCountQuery = @"-- srptWarningLetterTotalCount :: Query
SELECT
      COUNT(empDisc.DisciplinaryTypeId) AS [Count]
    , emp.Code                          AS C_EmpCode
    , empDisc.DisciplinaryDate
    , empDisc.DisciplinaryTypeId        AS DisciplinaryType
FROM EmployeeDisciplinaries empDisc
LEFT JOIN Employees emp
       ON emp.Id        = empDisc.EmployeeId
      AND emp.TenantId  = @TenantId
      AND emp.IsDeleted = 0
WHERE empDisc.TenantId  = @TenantId
  AND empDisc.IsDeleted = 0
  AND emp.Code = @EmpCode
  AND empDisc.DisciplinaryTypeId = @DiscType
GROUP BY emp.Code, empDisc.DisciplinaryDate, empDisc.DisciplinaryTypeId;";

        /// <summary>
        /// srptWarningLetterCount :: Query -- the single disciplinary type the user picked, whose
        /// Arabic name the subreport prints. The legacy version listed every type and narrowed with
        /// the client-side FilterString "[OID] = ?DiscType"; that predicate moves into the SQL.
        /// </summary>
        public const string WarningLetterSelectedTypeQuery = @"-- srptWarningLetterCount :: Query
SELECT
      discType.Id   AS OID
    , discType.Code
    , discType.NameAr
FROM DisciplinaryTypes discType
WHERE discType.TenantId  = @TenantId
  AND discType.IsDeleted = 0
  AND discType.Id        = @DiscType;";
    }

    /// <summary>
    /// Non-SQL binary/RTF assets for the OthersCertificates batch.
    ///
    /// WHY THIS LIVES HERE. In the legacy reports these sat in each report's <c>.resx</c>. The
    /// bitmaps cannot come over that way: HCMv3.Reports multi-targets net8.0 as well as
    /// net8.0-windows, and a .resx entry typed <c>System.Drawing.Bitmap</c> forces the resource
    /// compiler to bind a Windows-only type on both flavours (see the same note on
    /// <c>Others/SalaryCertificateBackground.cs</c>, <c>GovtForms/GovtFormsImageStore.cs</c> and
    /// <c>EmployeeInfo/EmployeeInfoResources.cs</c>). The images ship as raw embedded files,
    /// byte-identical to what was base64-encoded in each legacy .resx (mimetype
    /// <c>bytearray.base64</c>, i.e. the file itself, not a serialised object), and are attached in
    /// each report's constructor. RTF strings are plain strings and are carried over verbatim.
    ///
    /// It shares a file with <see cref="OthersCertificatesSql"/> only to honour this batch's
    /// file-creation budget; there is no other relationship.
    ///
    /// SIDE EFFECT: the Visual Studio report designer shows empty picture boxes, because the images
    /// are no longer assigned inside InitializeComponent. The rendered document is unaffected.
    /// </summary>
    internal static class OthersCertificatesResources
    {
        internal const string SalaryContinuousLetterheadResource =
            "HCMv3.Reports.Others.rptSalaryContinuousCertificateLetterhead.png";

        internal const string SalaryContinuousSignatureResource =
            "HCMv3.Reports.Others.rptSalaryContinuousCertificateSignature.png";

        internal const string WhomsoeverBackgroundResource =
            "HCMv3.Reports.Others.rptWhomsoeverCertificateBackground.jpg";

        internal const string WarningLetterBackgroundResource =
            "HCMv3.Reports.Others.rptWarningLetterBackground.jpg";

        // XRRichText.SerializableRtfString is base64-encoded UTF-16 RTF. Carried over byte-for-byte
        // from the legacy .resx entries -- do not re-encode, DevExpress round-trips this string.

        /// <summary>
        /// Signature block ("____________ / Dr. Khalid Al Sabti / Chief Executive Officer").
        /// rptSalaryContinuousCertificate.xrRichText1 and rptEmployeeConfirmationLetter.xrRichText1
        /// carried BYTE-IDENTICAL values in their two .resx files; one constant serves both.
        /// </summary>
        internal const string SignatureRtf =
            "ewBcAHIAdABmADEAXABhAG4AcwBpAGMAcABnADEAMgA1ADIADQAKAHsADQAKAFwAZgBvAG4AdAB0AGIAbAANAAoAewBcAGYAMAAgAFQAaQBtAGUAcwAgAE4AZQB3ACAAUgBvAG0AYQBuADsAfQANAAoAewBcAGYAMQAgAFQAYQBoAG8AbQBhADsAfQANAAoAfQANAAoAewANAAoAXABjAG8AbABvAHIAdABiAGwADQAKADsADQAKAFwAcgBlAGQAMABcAGcAcgBlAGUAbgAwAFwAYgBsAHUAZQAyADUANQA7AA0ACgBcAHIAZQBkADAAXABnAHIAZQBlAG4AMABcAGIAbAB1AGUAMAA7AA0ACgB9AA0ACgBcAG4AbwB1AGkAYwBvAG0AcABhAHQAXABzAHAAbAB5AHQAdwBuAGkAbgBlAFwAaAB0AG0AYQB1AHQAcwBwAHsAXABwAGEAcgBkAFwAcABsAGEAaQBuAFwAcQBsAHsAXABiAFwAZgAxAFwAZgBzADIANABcAGMAZgAyACAAXwBfAF8AXwBfAF8AXwBfAF8AXwBfAF8AXwBfAF8AXwBfAH0AXABiAFwAZgAxAFwAZgBzADIANABcAHAAYQByAFwAcABhAHIAZABcAHAAbABhAGkAbgBcAHEAbAB7AFwAYgBcAGYAMQBcAGYAcwAyADQAXABjAGYAMgAgAEQAcgAuACAASwBoAGEAbABpAGQAIABBAGwAIABTAGEAYgB0AGkAfQBcAGIAXABmADEAXABmAHMAMgA0AFwAcABhAHIAXABwAGEAcgBkAFwAcABsAGEAaQBuAFwAcQBsAHsAXABiAFwAZgAxAFwAZgBzADIANABcAGMAZgAyACAAQwBoAGkAZQBmACAARQB4AGUAYwB1AHQAaQB2AGUAIABPAGYAZgBpAGMAZQByAH0AXABiAFwAZgAxAFwAZgBzADIANABcAHAAYQByAFwAcABhAHIAZABcAHAAbABhAGkAbgBcAHEAbABcAGYAcwAyADQAXABwAGEAcgB9AA0ACgB9AA0ACgA=";

        /// <summary>
        /// Distribution footer ("cc : Dept. Head / : Payroll / P/F").
        /// rptEmployeeConfirmationLetter.xrRichText2.
        /// </summary>
        internal const string ConfirmationLetterCopyToRtf =
            "ewBcAHIAdABmADEAXABhAG4AcwBpAGMAcABnADEAMgA1ADIADQAKAHsADQAKAFwAZgBvAG4AdAB0AGIAbAANAAoAewBcAGYAMAAgAFQAaQBtAGUAcwAgAE4AZQB3ACAAUgBvAG0AYQBuADsAfQANAAoAewBcAGYAMQAgAFQAYQBoAG8AbQBhADsAfQANAAoAfQANAAoAewANAAoAXABjAG8AbABvAHIAdABiAGwADQAKADsADQAKAFwAcgBlAGQAMABcAGcAcgBlAGUAbgAwAFwAYgBsAHUAZQAyADUANQA7AA0ACgBcAHIAZQBkADAAXABnAHIAZQBlAG4AMABcAGIAbAB1AGUAMAA7AA0ACgB9AA0ACgBcAG4AbwB1AGkAYwBvAG0AcABhAHQAXABzAHAAbAB5AHQAdwBuAGkAbgBlAFwAaAB0AG0AYQB1AHQAcwBwAHsAXABwAGEAcgBkAFwAcABsAGEAaQBuAFwAcQBsAHsAXABmADEAXABmAHMAMQA2AFwAYwBmADIAIABjAGMAIAA6ACAARABlAHAAdAAuACAAIABIAGUAYQBkACAAIAAgACAAIAB9AFwAZgAxAFwAZgBzADEANgBcAHAAYQByAFwAcABhAHIAZABcAHAAbABhAGkAbgBcAHEAbAB7AFwAZgAxAFwAZgBzADEANgBcAGMAZgAyACAAIAAgACAAIAA6ACAAIABQAGEAeQByAG8AbABsACAALwAgAFAALwBGAH0AXABmADEAXABmAHMAMQA2AFwAcABhAHIAXABwAGEAcgBkAFwAcABsAGEAaQBuAFwAcQBsAFwAZgBzADIANABcAHAAYQByAH0ADQAKAH0ADQAKAA==";

        /// <summary>
        /// Returns an embedded image, or <c>null</c> if it cannot be loaded. Never throws: a missing
        /// letterhead is a cosmetic defect, not a reason to fail a personnel document.
        /// </summary>
        internal static System.Drawing.Image LoadImage(string resourceName)
        {
            try
            {
                var assembly = typeof(OthersCertificatesResources).Assembly;
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    // GDI+ keeps a reference to the backing stream for the lifetime of the Image, so
                    // the manifest stream is copied into a MemoryStream that is deliberately NOT
                    // disposed -- disposing it would make the image throw on first draw.
                    var buffer = new System.IO.MemoryStream();
                    stream.CopyTo(buffer);
                    buffer.Position = 0;
                    return System.Drawing.Image.FromStream(buffer, useEmbeddedColorManagement: false, validateImageData: false);
                }
            }
            catch (System.Exception)
            {
                return null;
            }
        }
    }
}
