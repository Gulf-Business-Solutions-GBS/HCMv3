// SQL for the first-half GovtForms reports, rewritten for the HCMv3 schema.
//
// MULTI-TENANCY: these run through DevExpress SqlDataSource as RAW SQL, so EF Core's per-tenant
// global query filter does NOT apply. Every query filters TenantId explicitly via @TenantId, which
// is supplied as the hidden PrmTenantId report parameter populated from AbpSession.TenantId by the
// caller (HcmReportBase.EnsureHcmParameters / ApplyTenant). Removing an "AND TenantId = @TenantId"
// silently leaks rows across tenants.
//
// SOFT DELETE: the legacy HCM10 corpus contains zero references to TenantId or IsDeleted. Every
// table touched below is an ABP FullAuditedEntity carrying both, so both filters are ADDED on every
// join. Neither omission produces an error -- only wrong output.
//
// -------------------------------------------------------------------------------------------------
// WHY ONE SHARED MASTER QUERY
// -------------------------------------------------------------------------------------------------
// Twelve of the thirteen reports in this batch carried a byte-for-byte identical 55-line master
// query, differing only by which handful of extra columns each one bolted on (governorate names,
// the sponsor's nationality, passport number, residency number, the sponsor's unified number, an
// Arabic weekday). Rather than reproduce twelve near-clones, the union of those columns is
// projected once by EmployeeGovtFormQuery. A report that never binds a column is unaffected by its
// presence; the alternative is twelve places to get @TenantId wrong.
//
// The single exception is EmployeeGovtFormQueryWithArabicDay, used only by rptAbsenceWaiverRequest:
// it needs @PrmDate, and three of the twelve reports (Acknowledgment And Pledge, Declaration of
// Financial Rights, Personal Photo Submission) have no PrmDate report parameter to bind it to.
//
// -------------------------------------------------------------------------------------------------
// SCHEMA REWRITES vs the legacy SQL (non-mechanical ones only)
// -------------------------------------------------------------------------------------------------
//  1. Sponsor.CivilId -> *** FEATURE LOST ***. HCMv3's Sponsors table has no CivilId column at all
//     (Signatories does; Sponsors does not). Projected as NULL so that the two reports that bind
//     [Sponsor_CivilId] -- rptBusinessVisitVisaExtensionPledge and rptDeclarationFinRights --
//     render blank rather than fail. Needs a schema decision.
//  2. Sponsor.Governate (a raw FK OID in legacy) -> Sponsors.GovernorateId -> Governorates.
//     Projected as GovernateEn / GovernateAr, preserving the legacy misspelling of the alias
//     because rptAbsenceWaiverAuthorization's layout binds [GovernateAr] by that exact name.
//  3. SponsorSignatory.AuthorizedSignatory -> SponsorSignatories.SignatoryId.
//  4. Employee.PassportNum / Employee.VisaNum: neither column exists on HCMv3's Employees. The
//     passport number now comes from EmployeeDocuments joined to the passport DocumentType; the
//     visa number from EmployeeVisaInfos.VisaNum.
//  5. The legacy "LEFT JOIN X ON X.OID IN (SELECT TOP 1 OID FROM X WHERE ... ORDER BY ...)"
//     correlated-subquery idiom is rewritten as OUTER APPLY: same intent, correlation visible to
//     the optimiser, and no accidental cross-employee match (the legacy form's inner reference to
//     Employee.OID resolved against the *inner* alias in several of these queries -- a latent bug).
//  6. LEGACY FAN-OUT PRESERVED, DELIBERATELY. The Sponsor -> SponsorSignatory -> Signatory chain is
//     a plain LEFT JOIN, so a sponsor with N authorised signatories yields N master rows and the
//     government form prints N times. The reports declare a PrmAuthorisedSign parameter that is
//     never used to filter this query. That is a legacy defect, not a legacy quirk, but filtering
//     it here would change what the form prints -- flagged for a product decision rather than
//     silently "fixed".
//  7. AbpOrganizationUnits has DisplayName only -- no NameEn/NameAr pair and no unit-type concept.
//     Legacy queries that picked "the OrgLevel1..7 whose OrganizationUnitType is Division" collapse
//     to the employee's own single OrganizationUnitId, and the Arabic org name is unavailable.
//     *** SEMANTIC DEVIATION -- needs a product decision. *** Only the two Shoun forms and
//     rptEndorsmentForVisitVisa touch it, and only rptPayShounTerminationFormEn actually binds it.

namespace HCMv3.Reports.GovtForms
{
    /// <summary>Raw SQL used by the first-half GovtForms reports.</summary>
    public static class GovtFormsSqlFirstHalf
    {
        // -------------------------------------------------------------------------------------------
        // The shared master body, held once, WITHOUT a trailing semicolon so it can also be used as a
        // derived table by the Arabic-weekday variant below. Twelve of the thirteen reports in this
        // batch carried this query byte-for-byte identically in their .resx.
        // -------------------------------------------------------------------------------------------
        private const string EmployeeGovtFormBody = @"SELECT
      emp.Code                          AS EmpCode
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.JoiningDate
    , emp.CivilID
    , sep.LastDayInService
    , visa.CurrentFileNo
    , visa.ResidencyNum
    , sponsor.NameEn                    AS SponsorNameEn
    , sponsor.NameAr                    AS SponsorNameAr
    , gov.NameEn                        AS GovernateEn      -- legacy alias misspelling kept on purpose
    , gov.NameAr                        AS GovernateAr
    , CAST(NULL AS BIGINT)              AS Sponsor_CivilId  -- rewrite note 1: FEATURE LOST
    , sponsor.UnifiedNum
    , sponsor.BusinessOwnerNameEn
    , sponsor.BusinessOwnerNameAr
    , sig.NameEn                        AS SignatoryNameEn
    , sig.NameAr                        AS SignatoryNameAr
    , sig.CivilId                       AS Signatory_CivilId
    , bt.NameEn                         AS BusinessTypeEn
    , bt.NameAr                         AS BusinessTypeAr
    , nat.NameEn                        AS NationalityEn
    , nat.NameAr                        AS NationalityAr
    , sponsorNat.NameEn                 AS SponsorNationalityEn
    , sponsorNat.NameAr                 AS SponsorNationalityAr
    , gjt.NameEn                        AS GovtJobTitleEn
    , gjt.NameAr                        AS GovtJobTitleAr
    , empSponsor.ContractSalary
    , empSponsor.StartDate              AS GovtContractStartDate
    , empSponsor.EndDate                AS GovtContractEndDate
    , subAbs.TotalDays                  AS AnnualPaidLeaveDays
    , passport.DocumentNum              AS PassportNum      -- rewrite note 4

FROM Employees emp

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id
      AND orgAsg.TenantId   = @TenantId
      AND orgAsg.IsDeleted  = 0
      AND orgAsg.StartDate <= GETDATE()
      AND orgAsg.EndDate   >= GETDATE()

LEFT JOIN EmployeeSubGroups subGrp
       ON subGrp.Id        = orgAsg.EmployeeSubGroupId
      AND subGrp.TenantId  = @TenantId
      AND subGrp.IsDeleted = 0

LEFT JOIN EmployeeSubGroupAbsences subAbs
       ON subAbs.EmployeeSubGroupId = subGrp.Id
      AND subAbs.TenantId  = @TenantId
      AND subAbs.IsDeleted = 0
      AND subAbs.AbsenceTypeId IN (SELECT at.Id FROM AbsenceTypes at
                                    WHERE at.NameEn    = 'Annual Paid Leave'
                                      AND at.TenantId  = @TenantId
                                      AND at.IsDeleted = 0)

-- rewrite note 5
OUTER APPLY (SELECT TOP 1 es.SponsorId, es.GovtJobTitleId, es.ContractSalary, es.StartDate, es.EndDate
             FROM EmployeeSponsors es
             WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
             ORDER BY es.StartDate DESC) empSponsor

OUTER APPLY (SELECT TOP 1 s.LastDayInService
             FROM EmployeeSeparations s
             WHERE s.EmployeeId = emp.Id AND s.TenantId = @TenantId AND s.IsDeleted = 0
             ORDER BY s.SubmissionDate DESC) sep

OUTER APPLY (SELECT TOP 1 v.CurrentFileNo, v.ResidencyNum
             FROM EmployeeVisaInfos v
             WHERE v.EmployeeId = emp.Id AND v.TenantId = @TenantId AND v.IsDeleted = 0
             ORDER BY v.StartDate DESC) visa

OUTER APPLY (SELECT TOP 1 d.DocumentNum
             FROM EmployeeDocuments d
             INNER JOIN DocumentTypes dt
                     ON dt.Id = d.DocumentTypeId AND dt.TenantId = @TenantId AND dt.IsDeleted = 0
             WHERE d.EmployeeId = emp.Id AND d.TenantId = @TenantId AND d.IsDeleted = 0
               AND dt.NameEn LIKE '%passport%'
             ORDER BY d.ExpiryDate DESC) passport

LEFT JOIN Sponsors sponsor
       ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0

-- rewrite notes 3 and 6
LEFT JOIN SponsorSignatories ss
       ON ss.SponsorId = sponsor.Id AND ss.TenantId = @TenantId AND ss.IsDeleted = 0
LEFT JOIN Signatories sig
       ON sig.Id = ss.SignatoryId AND sig.TenantId = @TenantId AND sig.IsDeleted = 0

LEFT JOIN GovtJobTitles gjt
       ON gjt.Id = empSponsor.GovtJobTitleId AND gjt.TenantId = @TenantId AND gjt.IsDeleted = 0
LEFT JOIN Nationalities nat
       ON nat.Id = emp.NationalityId AND nat.TenantId = @TenantId AND nat.IsDeleted = 0
LEFT JOIN Nationalities sponsorNat
       ON sponsorNat.Id = sponsor.NationalityId AND sponsorNat.TenantId = @TenantId AND sponsorNat.IsDeleted = 0
LEFT JOIN BusinessTypes bt
       ON bt.Id = sponsor.BusinessTypeId AND bt.TenantId = @TenantId AND bt.IsDeleted = 0
LEFT JOIN Governorates gov
       ON gov.Id = sponsor.GovernorateId AND gov.TenantId = @TenantId AND gov.IsDeleted = 0

WHERE emp.Code      = @PrmEmpCode
  AND emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0";

        /// <summary>
        /// Shared master query -- one row per employee (see the fan-out note above). Union of every
        /// column the twelve template-family reports project.
        /// </summary>
        public const string EmployeeGovtFormQuery =
            "-- GovtForms :: shared employee master query (HCMv3 rewrite of the legacy 55-line template)\r\n"
            + EmployeeGovtFormBody;

        /// <summary>
        /// <see cref="EmployeeGovtFormQuery"/> plus the two @PrmDate-derived columns that only
        /// rptAbsenceWaiverRequest binds. Kept as a separate constant because three reports in the
        /// family have no PrmDate report parameter for a query parameter to bind against, and a
        /// CustomSqlQuery whose SQL names a parameter the report cannot supply fails at run time.
        /// </summary>
        public const string EmployeeGovtFormQueryWithArabicDay =
            "-- GovtForms :: shared employee master query + Arabic weekday (rptAbsenceWaiverRequest)\r\n"
            + "SELECT\r\n"
            + "      @PrmDate AS GivenDate\r\n"
            + "    , CASE DATEPART(WEEKDAY, @PrmDate)\r\n"
            + "        WHEN 1 THEN N'الأحد'\r\n"
            + "        WHEN 2 THEN N'الإثنين'\r\n"
            + "        WHEN 3 THEN N'الثلاثاء'\r\n"
            + "        WHEN 4 THEN N'الأربعاء'\r\n"
            + "        WHEN 5 THEN N'الخميس'\r\n"
            + "        WHEN 6 THEN N'الجمعة'\r\n"
            + "        WHEN 7 THEN N'السبت'\r\n"
            + "      END AS ArabicDay\r\n"
            + "    , govtForm.*\r\n"
            + "FROM (\r\n"
            + EmployeeGovtFormBody + "\r\n"
            + ") govtForm";

        /// <summary>
        /// Feeds the PrmAuthorisedSign dynamic look-up list on eleven of the twelve template-family
        /// reports. Byte-identical across all of them in the legacy source.
        /// </summary>
        public const string AuthSignatory = @"-- GovtForms :: Auth_Signatory (look-up list feeding the PrmAuthorisedSign parameter)
SELECT
      emp.Code   AS EmpCode
    , sig.Id     AS SignatoryOID
    , sig.NameAr
    , sig.NameEn
FROM Sponsors sponsor
INNER JOIN SponsorSignatories ss
        ON ss.SponsorId = sponsor.Id AND ss.TenantId = @TenantId AND ss.IsDeleted = 0
INNER JOIN Signatories sig
        ON sig.Id = ss.SignatoryId AND sig.TenantId = @TenantId AND sig.IsDeleted = 0
INNER JOIN EmployeeSponsors es
        ON es.SponsorId = sponsor.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
INNER JOIN Employees emp
        ON emp.Id = es.EmployeeId AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
WHERE sponsor.TenantId = @TenantId
  AND sponsor.IsDeleted = 0;";

        /// <summary>
        /// rptEndorsmentForVisitVisa's own master query (query name kept as
        /// <c>SalaryCertificate_LNDQuery</c> because the report's DataMember refers to it by name).
        /// See rewrite note 7 for the Division columns.
        /// </summary>
        public const string EndorsementForVisitVisaQuery = @"-- rptEndorsmentForVisitVisa :: SalaryCertificate_LNDQuery
SELECT
      emp.Id                       AS OID
    , emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.CivilID
    , emp.JoiningDate
    , passport.DocumentNum         AS PassportNum
    , nat.NameEn                   AS NationalityEn
    , nat.NameAr                   AS NationalityAr
    , jt.NameEn                    AS JobTitleEn
    , jt.NameAr                    AS JobTitleAr
    , gjt.NameEn                   AS GovtJobTitleEn
    , gjt.NameAr                   AS GovtJobTitleAr
    , basicPay.Amount
    , empBank.AccountNum
    , bank.NameEn                  AS BankEn
    , bank.NameAr                  AS BankAr
    , orgUnit.DisplayName          AS DivisionEn   -- rewrite note 7
    , orgUnit.DisplayName          AS DivisionAr   -- rewrite note 7: no Arabic org name in HCMv3
FROM Employees emp
LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
      AND orgAsg.StartDate <= @PrmDate AND orgAsg.EndDate >= @PrmDate
LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id = orgAsg.OrganizationUnitId AND orgUnit.TenantId = @TenantId AND orgUnit.IsDeleted = 0
LEFT JOIN JobTitles jt
       ON jt.Id = orgAsg.JobTitleId AND jt.TenantId = @TenantId AND jt.IsDeleted = 0
LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.EmployeeId = emp.Id AND empSponsor.TenantId = @TenantId AND empSponsor.IsDeleted = 0
      AND empSponsor.StartDate <= @PrmDate AND empSponsor.EndDate >= @PrmDate
LEFT JOIN GovtJobTitles gjt
       ON gjt.Id = empSponsor.GovtJobTitleId AND gjt.TenantId = @TenantId AND gjt.IsDeleted = 0
LEFT JOIN EmployeeBasicPays basicPay
       ON basicPay.EmployeeId = emp.Id AND basicPay.TenantId = @TenantId AND basicPay.IsDeleted = 0
      AND basicPay.StartDate <= @PrmDate AND basicPay.EndDate >= @PrmDate
LEFT JOIN EmployeeBanks empBank
       ON empBank.EmployeeId = emp.Id AND empBank.TenantId = @TenantId AND empBank.IsDeleted = 0
      AND empBank.StartDate <= @PrmDate AND empBank.EndDate >= @PrmDate
LEFT JOIN Banks bank
       ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0
LEFT JOIN Nationalities nat
       ON nat.Id = emp.NationalityId AND nat.TenantId = @TenantId AND nat.IsDeleted = 0
OUTER APPLY (SELECT TOP 1 d.DocumentNum
             FROM EmployeeDocuments d
             INNER JOIN DocumentTypes dt
                     ON dt.Id = d.DocumentTypeId AND dt.TenantId = @TenantId AND dt.IsDeleted = 0
             WHERE d.EmployeeId = emp.Id AND d.TenantId = @TenantId AND d.IsDeleted = 0
               AND dt.NameEn LIKE '%passport%'
             ORDER BY d.ExpiryDate DESC) passport
WHERE emp.Code = @PrmEmpCode
  AND emp.TenantId = @TenantId
  AND emp.IsDeleted = 0;";

        /// <summary>
        /// rptPayRequestShounRenewalEn's master query.
        ///
        /// Legacy oddities preserved: the column alias <c>Nalionality</c> is misspelled in the
        /// legacy SQL and the layout binds it by that name; the Gender literal is hard-coded Arabic.
        ///
        /// Rewrites: <c>emp.PassportNum</c> / <c>emp.VisaNum</c> do not exist on HCMv3's Employees
        /// (rewrite note 4); <c>Sponsor.FirstNameAr + SecondNameAr + ThirdNameAr</c> does not exist
        /// either -- Sponsors carries a single NameAr, which is used for OwnerName; the legacy
        /// <c>emp.Sponsor = empsponsor.OID</c> join compared an employee column to an employee-
        /// sponsor primary key and is replaced with the real FK. The legacy report also applied
        /// the employee filter client-side as <c>FilterString = ""[Code] = ?EmployeeCode""</c>,
        /// which over a raw SqlDataSource fetches EVERY employee (and, in HCMv3, every tenant)
        /// before discarding them; it is pushed into the WHERE clause here.
        /// </summary>
        public const string PayRequestShounRenewalQuery = @"-- rptPayRequestShounRenewalEn :: Query
SELECT
      emp.Code
    , emp.FullNameEn
    , emp.FirstNameAr
    , emp.SecondNameAr
    , emp.ThirdNameAr
    , emp.LastNameAr
    , religion.NameAr              AS religion
    , CASE WHEN emp.Gender = 0 THEN N'ذكر' ELSE N'أنثى' END AS Gender
    , emp.BirthDate
    , emp.CivilID
    , govjobtitle.NameAr           AS profession
    , nationality.NameAr           AS Nalionality      -- legacy misspelling: the layout binds this name
    , marstatus.NameAr             AS MaritalStatus
    , quali.NameAr                 AS Educationlevel
    , visainfo.TypeOfPassport
    , passport.DocumentNum         AS PassportNum
    , visainfo.VisaNum
    , empsponsor.ContractSalary
    , visainfo.EntryDate
    , visainfo.TerminationDate
    , visainfo.EndDate             AS VisaEndDate
    , sponsor.NameAr               AS OwnerName
    , visainfo.CurrentContractNo
    , govorg.DisplayName           AS Company
    , visainfo.AuthorizedSign
    , visainfo.CurrentLicenseNo
    , visainfo.CurrentResidencyPeriod
FROM Employees emp
LEFT JOIN Religions religion
       ON religion.Id = emp.ReligionId AND religion.TenantId = @TenantId AND religion.IsDeleted = 0
LEFT JOIN EmployeeSponsors empsponsor
       ON empsponsor.EmployeeId = emp.Id AND empsponsor.TenantId = @TenantId AND empsponsor.IsDeleted = 0
      AND empsponsor.StartDate <= GETDATE() AND empsponsor.EndDate >= GETDATE()
LEFT JOIN GovtJobTitles govjobtitle
       ON govjobtitle.Id = empsponsor.GovtJobTitleId AND govjobtitle.TenantId = @TenantId AND govjobtitle.IsDeleted = 0
LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
LEFT JOIN MaritalStatuses marstatus
       ON marstatus.Id = emp.MaritalStatusId AND marstatus.TenantId = @TenantId AND marstatus.IsDeleted = 0
LEFT JOIN Qualifications quali
       ON quali.Id = emp.QualificationId AND quali.TenantId = @TenantId AND quali.IsDeleted = 0
OUTER APPLY (SELECT TOP 1 v.TypeOfPassport, v.VisaNum, v.EntryDate, v.TerminationDate, v.EndDate,
                          v.CurrentContractNo, v.AuthorizedSign, v.CurrentLicenseNo, v.CurrentResidencyPeriod
             FROM EmployeeVisaInfos v
             WHERE v.EmployeeId = emp.Id AND v.TenantId = @TenantId AND v.IsDeleted = 0
             ORDER BY v.StartDate DESC) visainfo
OUTER APPLY (SELECT TOP 1 d.DocumentNum
             FROM EmployeeDocuments d
             INNER JOIN DocumentTypes dt
                     ON dt.Id = d.DocumentTypeId AND dt.TenantId = @TenantId AND dt.IsDeleted = 0
             WHERE d.EmployeeId = emp.Id AND d.TenantId = @TenantId AND d.IsDeleted = 0
               AND dt.NameEn LIKE '%passport%'
             ORDER BY d.ExpiryDate DESC) passport
LEFT JOIN Sponsors sponsor
       ON sponsor.Id = empsponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0
LEFT JOIN AbpOrganizationUnits govorg
       ON govorg.Id = sponsor.OrganizationUnitId AND govorg.TenantId = @TenantId AND govorg.IsDeleted = 0
WHERE emp.Code = @EmployeeCode
  AND emp.TenantId = @TenantId
  AND emp.IsDeleted = 0;";

        /// <summary>
        /// rptPayShounTerminationFormEn's master query. This report had NO SQL at all in the legacy
        /// source: it bound an XAF <c>DevExpress.Persistent.Base.ReportsV2.CollectionDataSource</c>
        /// over <c>HCM10.Module.BusinessObjects.PersonnelAdministration.Employee</c> and navigated
        /// XPO association properties (<c>OrgAssignment.OrganizationUnit.NameAr</c>) directly, with
        /// a client-side <c>FilterString = ""[Code] = ?EmployeeCode""</c>.
        ///
        /// A DevExpress-constructed report in HCMv3 has no XPO session and no DI container, so the
        /// collection data source is replaced by a SqlDataSource projecting exactly the four values
        /// the four labels bound: the Arabic full name (formerly the FullNameArCal calculated
        /// field), the organisation unit name, the joining date and the print date.
        /// </summary>
        public const string PayShounTerminationQuery = @"-- rptPayShounTerminationFormEn :: Query (replaces the XAF CollectionDataSource)
SELECT
      emp.Code
    , emp.FirstNameAr
    , emp.SecondNameAr
    , emp.ThirdNameAr
    , emp.LastNameAr
    , LTRIM(RTRIM(
        ISNULL(emp.FirstNameAr , '') + ' ' +
        ISNULL(emp.SecondNameAr, '') + ' ' +
        ISNULL(emp.ThirdNameAr , '') + ' ' +
        ISNULL(emp.LastNameAr  , '')))       AS FullNameArCal
    , emp.JoiningDate
    , orgUnit.DisplayName                    AS OrgUnitNameAr   -- rewrite note 7
    , CONVERT(VARCHAR(10), GETDATE(), 103)   AS PrintedDate
FROM Employees emp
LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
      AND orgAsg.StartDate <= GETDATE() AND orgAsg.EndDate >= GETDATE()
LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id = orgAsg.OrganizationUnitId AND orgUnit.TenantId = @TenantId AND orgUnit.IsDeleted = 0
WHERE emp.Code = @EmployeeCode
  AND emp.TenantId = @TenantId
  AND emp.IsDeleted = 0;";
    }
}
