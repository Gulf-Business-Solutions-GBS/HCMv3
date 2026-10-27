namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>Raw SQL used by rptFinalClearanceCertificate (data source <c>sqlDataSource1</c>, query <c>Query</c>).</summary>
    internal static class rptFinalClearanceCertificateSql
    {
        /// <summary>
        /// HCMv3 rewrite. Mechanical renames plus TenantId/IsDeleted everywhere.
        ///
        /// NON-MECHANICAL:
        ///   1. *** LEGACY BUG FIXED. *** The passport lookup was
        ///      <c>LEFT JOIN EmployeeDocument ON EmployeeDocument.OID IN
        ///      (SELECT TOP 1 ... WHERE Employee = Employee.OID ORDER BY ExpiryDate DESC)</c>.
        ///      Inside that subquery <c>Employee</c> resolves to the INNER EmployeeDocument.Employee
        ///      column, not the outer Employee table -- so the correlation is <c>x = x</c>, always
        ///      true, and the subquery returns the latest-expiring passport of ANY employee.
        ///      Rewritten as an OUTER APPLY whose correlation is explicit and visible to the
        ///      optimiser.
        ///   2. <c>Sponsor.CivilId</c> has no HCMv3 counterpart -- HCMv3 Sponsors carries
        ///      UnifiedNum / SocialSecurityNum / MOSALID but no civil ID. Returned as NULL.
        ///      *** FIELD LOST. ***
        ///   3. <c>EmployeeSeparation</c> was joined without picking a row; an employee with more
        ///      than one separation record fanned the certificate out. Narrowed to the latest by
        ///      LastDayInService with OUTER APPLY.
        ///
        /// *** KNOWN HCMv3 DATA GAP, NOT A PORTING DEFECT. *** EmployeeDocuments, EmployeeSponsors
        /// and EmployeeBanks are EMPTY (0 rows) and EmployeeSeparations has 0 rows, so PassportNum,
        /// Sponsor*, Bank*, AccountNum and LastDayInService legitimately render blank. Every one of
        /// those joins is LEFT/OUTER APPLY, so the employee row itself still renders.
        /// </summary>
        internal const string Query1 = @"-- rptFinalClearanceCertificate :: Query
SELECT
      emp.Id                        AS OID          -- legacy column name kept; the layout binds ""OID""
    , emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.CivilID
    , emp.JoiningDate
    , passport.DocumentNum          AS PassportNum
    , nationality.NameEn            AS NationalityEn
    , nationality.NameAr            AS NationalityAr
    , jobTitle.NameEn               AS JobTitleEn
    , jobTitle.NameAr               AS JobTitleAr
    , govtJobTitle.NameEn           AS GovtJobTitleEn
    , govtJobTitle.NameAr           AS GovtJobTitleAr
    , basicPay.Amount
    , empBank.AccountNum
    , bank.NameEn                   AS BankEn
    , bank.NameAr                   AS BankAr
    , sponsor.NameEn                AS SponsorEn
    , sponsor.NameAr                AS SponsorAr
    , CAST(NULL AS NVARCHAR(50))    AS SponsorCivilId   -- see rewrite note 2
    , sponsor.MOSALID
    , sponsor.BusinessOwnerNameEn
    , sponsor.BusinessOwnerNameAr
    , separation.LastDayInService

FROM Employees emp

LEFT JOIN Nationalities nationality
       ON nationality.Id        = emp.NationalityId
      AND nationality.TenantId  = @TenantId
      AND nationality.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments orgAssign
       ON orgAssign.EmployeeId = emp.Id
      AND orgAssign.TenantId   = @TenantId
      AND orgAssign.IsDeleted  = 0
      AND orgAssign.StartDate <= @PrmDate
      AND orgAssign.EndDate   >= @PrmDate

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAssign.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

LEFT JOIN EmployeeSponsors empSponsor              -- data gap: table is empty in HCMv3
       ON empSponsor.EmployeeId = emp.Id
      AND empSponsor.TenantId   = @TenantId
      AND empSponsor.IsDeleted  = 0
      AND empSponsor.StartDate <= @PrmDate
      AND empSponsor.EndDate   >= @PrmDate

LEFT JOIN Sponsors sponsor
       ON sponsor.Id        = empSponsor.SponsorId
      AND sponsor.TenantId  = @TenantId
      AND sponsor.IsDeleted = 0

LEFT JOIN GovtJobTitles govtJobTitle
       ON govtJobTitle.Id        = empSponsor.GovtJobTitleId
      AND govtJobTitle.TenantId  = @TenantId
      AND govtJobTitle.IsDeleted = 0

LEFT JOIN EmployeeBasicPays basicPay
       ON basicPay.EmployeeId = emp.Id
      AND basicPay.TenantId   = @TenantId
      AND basicPay.IsDeleted  = 0
      AND basicPay.StartDate <= @PrmDate
      AND basicPay.EndDate   >= @PrmDate

LEFT JOIN EmployeeBanks empBank                    -- data gap: table is empty in HCMv3
       ON empBank.EmployeeId = emp.Id
      AND empBank.TenantId   = @TenantId
      AND empBank.IsDeleted  = 0
      AND empBank.StartDate <= @PrmDate
      AND empBank.EndDate   >= @PrmDate

LEFT JOIN Banks bank
       ON bank.Id        = empBank.BankId
      AND bank.TenantId  = @TenantId
      AND bank.IsDeleted = 0

-- see rewrite note 3
OUTER APPLY (
    SELECT TOP 1 sep.LastDayInService
    FROM EmployeeSeparations sep
    WHERE sep.EmployeeId = emp.Id
      AND sep.TenantId   = @TenantId
      AND sep.IsDeleted  = 0
    ORDER BY sep.LastDayInService DESC
) separation

-- see rewrite note 1: the legacy correlated subquery was self-correlated and matched any employee
OUTER APPLY (
    SELECT TOP 1 doc.DocumentNum
    FROM EmployeeDocuments doc
    INNER JOIN DocumentTypes docType
            ON docType.Id        = doc.DocumentTypeId
           AND docType.TenantId  = @TenantId
           AND docType.IsDeleted = 0
           AND UPPER(docType.NameEn) = 'PASSPORT'
    WHERE doc.EmployeeId = emp.Id
      AND doc.TenantId   = @TenantId
      AND doc.IsDeleted  = 0
    ORDER BY doc.ExpiryDate DESC
) passport

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";
    }
}
