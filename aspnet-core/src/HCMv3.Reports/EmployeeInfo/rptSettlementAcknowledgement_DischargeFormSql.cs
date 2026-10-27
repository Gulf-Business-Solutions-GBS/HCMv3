namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// Raw SQL used by rptSettlementAcknowledgement_DischargeForm (data source
    /// <c>sqlDataSource1</c>, query <c>Query</c>).
    /// </summary>
    internal static class rptSettlementAcknowledgement_DischargeFormSql
    {
        /// <summary>
        /// HCMv3 rewrite. The query builds two long acknowledgement paragraphs by concatenating
        /// employee facts into HTML, which the layout renders as rich text.
        ///
        /// MECHANICAL RENAMES plus TenantId/IsDeleted everywhere.
        ///
        /// NON-MECHANICAL:
        ///   1. *** THE RAW-LEGACY-PRIMARY-KEY TRAP, present here. *** Legacy filtered
        ///      <c>EmployeeBasicPayDetail where Wagetype = 1</c>. That 1 is a raw HRMS_RCF PRIMARY
        ///      KEY, not a code: OID 1 is Code '1000', Basic Salary. HCMv3 has an unrelated wage
        ///      type whose CODE is literally '1' (Employee PIFSS Ded, a deduction), so translating
        ///      the literal mechanically would print a deduction as the monthly salary and the
        ///      report would still render. Rewritten as WageTypes.Code = '1000', exactly as
        ///      PayslipSql documents.
        ///   2. Seven Company OrgLevel joins -> one ancestor walk (EmployeeInfoSqlFragments).
        ///   3. <c>Company_n.logo</c> was an image column; HCMv3 keeps logos in the file store.
        ///      <c>Companylogo</c> stays a NULL placeholder and the employee's org unit is returned as
        ///      <c>CompanylogoOrgUnitId</c>, which HcmReportBase hands to ICompanyLogoResolver (legacy
        ///      Company_1..7 rule).
        ///   4. <c>Employee.PassportNum</c> does not exist on HCMv3 Employees. The passport number
        ///      now comes from EmployeeDocuments (document type PASSPORT, latest expiry), which is
        ///      where HCMv3 models it.
        ///   5. Both paragraphs are string concatenations; a single NULL input made the WHOLE
        ///      paragraph NULL in the legacy query, so an employee with no separation record
        ///      printed a blank page. ISNULL guards are added around every interpolated value so
        ///      the sentence still renders with the gap visible.
        ///   6. <c>EmployeeSeparation</c> and <c>EmployeeManager</c> were joined without narrowing;
        ///      more than one row of either fanned the form out. Narrowed with OUTER APPLY /
        ///      date-validity, and the legacy DISTINCT crutch dropped.
        ///
        /// *** KNOWN HCMv3 DATA GAP, NOT A PORTING DEFECT. *** EmployeeSeparations,
        /// EmployeeManagers and EmployeeDocuments all have 0 rows, so LastDayInService,
        /// EmployeeManager and PassportNum render blank.
        /// </summary>
        /// <summary>
        /// PORT NOTE -- NEW v23.2 CONSTRAINT: the legacy query opened with <c>;WITH cte1 AS (...)</c>.
        /// DevExpress's <c>CustomSqlQuery</c> validator rejects a query that opens with
        /// <c>WITH</c>, exactly as it rejects a leading <c>DECLARE</c> (the constraint PayslipSql
        /// already documents: "A custom SQL query should contain only SELECT statements"). The
        /// failure surfaces only at CreateDocument() time as "Query ... failed to execute /
        /// failed to validate", never at build time. The CTE is therefore inlined as a derived
        /// table; semantics are identical.
        /// </summary>
        internal const string Query1 = HeadProjection + Body;

        private const string Body = @"
FROM (
    SELECT
          emp.FullNameEn
        , emp.Code
        , emp.JoiningDate
        , emp.ProbationEndDate
        , emp.BirthDate
        , CASE emp.Gender WHEN 0 THEN 'M' WHEN 1 THEN 'F' ELSE ' ' END AS Gender
        , passport.DocumentNum          AS PassportNum      -- see rewrite note 4
        , emp.CivilID
        , nationality.NameEn            AS Nationality
        , jobTitle.NameEn               AS Jobtitle
        , separation.LastDayInService
        , basicPayDetail.Amount
        , manager.FullNameEn            AS EmployeeManager
        , currency.CurrencyShortFormEn
        , currency.NameEn               AS CurrencyName
        , COALESCE(company.NameEn, ownOrgUnit.DisplayName)  AS CompanyEn
        , CAST(NULL AS VARBINARY(MAX))  AS Companylogo      -- see rewrite note 3
        , orgAssign.OrganizationUnitId  AS CompanylogoOrgUnitId

    FROM Employees emp

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

    LEFT JOIN Nationalities nationality
           ON nationality.Id        = emp.NationalityId
          AND nationality.TenantId  = @TenantId
          AND nationality.IsDeleted = 0

    LEFT JOIN EmployeeBasicPays basicPay
           ON basicPay.EmployeeId = emp.Id
          AND basicPay.TenantId   = @TenantId
          AND basicPay.IsDeleted  = 0
          AND basicPay.EndDate    = '9999-12-31'

    -- see rewrite note 1: '1000' (Basic Salary), NOT the legacy primary key 1
    LEFT JOIN EmployeeBasicPayDetails basicPayDetail
           ON basicPayDetail.EmployeeBasicPayId = basicPay.Id
          AND basicPayDetail.TenantId  = @TenantId
          AND basicPayDetail.IsDeleted = 0
          AND basicPayDetail.WageTypeId IN (
                  SELECT wt.Id FROM WageTypes wt
                  WHERE wt.TenantId = @TenantId AND wt.IsDeleted = 0 AND wt.Code = '1000')

    LEFT JOIN Currencies currency
           ON currency.Id        = basicPayDetail.CurrencyId
          AND currency.TenantId  = @TenantId
          AND currency.IsDeleted = 0

    LEFT JOIN EmployeeManagers empManager                 -- data gap: table is empty in HCMv3
           ON empManager.EmployeeId = emp.Id
          AND empManager.TenantId   = @TenantId
          AND empManager.IsDeleted  = 0
          AND empManager.StartDate <= @PrmDate
          AND empManager.EndDate   >= @PrmDate

    LEFT JOIN Employees manager
           ON manager.Id        = empManager.ManagerId
          AND manager.TenantId  = @TenantId
          AND manager.IsDeleted = 0

    -- see rewrite note 6
    OUTER APPLY (
        SELECT TOP 1 sep.LastDayInService
        FROM EmployeeSeparations sep
        WHERE sep.EmployeeId = emp.Id
          AND sep.TenantId   = @TenantId
          AND sep.IsDeleted  = 0
        ORDER BY sep.LastDayInService DESC
    ) separation

    -- see rewrite note 4
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
" + EmployeeInfoSqlFragments.OwnOrgUnit
  + EmployeeInfoSqlFragments.CompanyAncestor + @"
    WHERE emp.TenantId  = @TenantId
      AND emp.IsDeleted = 0
      AND (@PrmEmpCode = '' OR emp.Code = @PrmEmpCode)
) cte1;";

        /// <summary>The projected paragraphs, which sit in front of the derived table.</summary>
        private const string HeadProjection = @"-- rptSettlementAcknowledgement_DischargeForm :: Query
SELECT
      'I, the undersigned <b>' + ISNULL(FullNameEn, '') + '</b>, <b>' + ISNULL(Nationality, '')
    + '</b> Nationality, holder of Civil ID card No. <b>' + ISNULL(CAST(CivilID AS VARCHAR(15)), '')
    + '</b>, Passport No. <b>' + ISNULL(PassportNum, '')
    + '</b> was working with <b>' + ISNULL(CompanyEn, '')
    + '</b> from <b>' + ISNULL(CONVERT(VARCHAR(10), JoiningDate, 103), '')
    + '</b> to <b>' + ISNULL(CONVERT(VARCHAR(10), LastDayInService, 103), '')
    + '</b> , with monthly Salary of <b>' + ISNULL(CurrencyName, '') + ' '
    + ISNULL(CAST(Amount AS VARCHAR(15)), '')
    + '</b>. My employment contract with the company has ended on <b>'
    + ISNULL(CONVERT(VARCHAR(10), LastDayInService, 103), '') + '</b>.'                AS Para1

    , 'I declare that I have received all my labor indemnities i.e. Salaries, allowances, overtime, services termination and leave indemnities etc. with <b>'
    + ISNULL(CompanyEn, '') + '</b> for the period from <b>'
    + ISNULL(CONVERT(VARCHAR(10), JoiningDate, 103), '') + '</b> till <b>'
    + ISNULL(CONVERT(VARCHAR(10), LastDayInService, 103), '')
    + '</b> , and that I have no right to claim or to refer currently or in future to <b>'
    + ISNULL(CompanyEn, '') + '</b> with any labor indemnities for the period from <b>'
    + ISNULL(CONVERT(VARCHAR(10), JoiningDate, 103), '') + '</b> till <b>'
    + ISNULL(CONVERT(VARCHAR(10), LastDayInService, 103), '') + '</b>.'                AS Para2
    , *";
    }
}
