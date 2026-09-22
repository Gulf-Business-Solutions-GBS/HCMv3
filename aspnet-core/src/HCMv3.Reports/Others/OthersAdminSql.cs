// Raw SQL for the "OthersAdmin" wave of the Others report family:
//
//   rptEmployeeProfile      rptEmpWorklocation      rptLocationWiseEmp      rptHolidayCalendar
//   rptPassportInOut        rptPassportRequestForm  rptBusinessTrip
//
// (rptTravelDeskReport is BLOCKED -- see PORTING-NOTES-OthersAdmin.md.)
//
// Held here rather than in a .resx (where the legacy reports kept it) for the reason PayslipSql.cs
// and SalaryCertificateSql.cs give: the SQL is the expensive, reviewable part of the port and must
// be diffable. A .resx also cannot be reviewed in a pull request.
//
// ===========================================================================================
// MULTI-TENANCY -- READ BEFORE EDITING
// ===========================================================================================
// These queries run through DevExpress SqlDataSource as RAW SQL, so EF Core's per-tenant global
// query filter does NOT apply. Tenant isolation exists ONLY because it is written into the SQL
// below. Every reference to every tenant-owned table carries "AND <alias>.TenantId = @TenantId",
// and every soft-deleted table also carries "AND <alias>.IsDeleted = 0".
//
// @TenantId is bound to the hidden PrmTenantId report parameter by
// HcmReportBase.EnsureHcmParameters(), which keys off the literal text "@TenantId" appearing in
// the SQL. Spelling it any other way silently leaves the query unparameterised and it will fail
// at run time. Removing an "AND TenantId = @TenantId" silently leaks rows across tenants.
//
// A per-constant table-reference vs. tenant-predicate audit is in
// Others/PORTING-NOTES-OthersAdmin.md.
// ===========================================================================================

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// Raw SQL shared by the OthersAdmin batch of ported Others reports.
    ///
    /// ============================ SHARED REWRITE NOTES ============================
    ///
    /// MECHANICAL RENAMES (HCM10 XPO -> HCMv3 EF Core)
    ///   Employee-&gt;Employees, EmployeeOrgAssignment-&gt;EmployeeOrgAssignments,
    ///   JobTitle-&gt;JobTitles, Nationality-&gt;Nationalities, PayrollArea-&gt;PayrollAreas,
    ///   EmployeeGroup-&gt;EmployeeGroups, EmployeeSubGroup-&gt;EmployeeSubGroups,
    ///   EmployeeDocument-&gt;EmployeeDocuments, DocumentType-&gt;DocumentTypes,
    ///   EmployeeBank-&gt;EmployeeBanks, Bank-&gt;Banks, Currency-&gt;Currencies, WageType-&gt;WageTypes,
    ///   EmployeeBasicPay(Detail)-&gt;EmployeeBasicPay(Detail)s, WorkLocation-&gt;WorkLocations,
    ///   EmployeeSeparation-&gt;EmployeeSeparations, EmployeeVisaInfo-&gt;EmployeeVisaInfos,
    ///   Holiday/HolidayPeriod/HolidayCalendar-&gt;Holidays/HolidayPeriods/HolidayCalendars,
    ///   OrganizationUnit-&gt;AbpOrganizationUnits (+ OrgUnitProfiles for the Arabic name and the
    ///   unit type).
    ///   OID -&gt; Id ; &lt;EntityName&gt; FK column -&gt; &lt;EntityName&gt;Id.
    ///   + "AND TenantId = @TenantId" and "AND IsDeleted = 0" on EVERY table reference. There are
    ///     zero occurrences of either in the legacy corpus: HCM10 was single-tenant and
    ///     hard-deleted.
    ///
    /// REWRITE 1 -- THE ORG-UNIT / "DEPARTMENT" REWRITE.
    ///   Legacy <c>EmployeeOrgAssignment</c> carried seven flat FK columns OrgLevel1..OrgLevel7
    ///   into <c>OrganizationUnit</c>. Every report in this batch that shows a Department joins
    ///   OrganizationUnit seven times, each filtered on
    ///   <c>OrganizationUnitType.NameEn = 'Department'</c>, then picks the first non-null with a
    ///   seven-branch CASE.
    ///
    ///   HCMv3 has a SINGLE <c>EmployeeOrgAssignments.OrganizationUnitId</c> into
    ///   <c>AbpOrganizationUnits</c>, an ABP materialised-path tree (Code / ParentId, Code being
    ///   dot-separated 5-digit segments). The unit TYPE that ABP has no concept of is carried by
    ///   HCMv3's own <c>OrgUnitProfiles.OrganizationUnitTypeId</c> -&gt;
    ///   <c>OrganizationUnitTypes</c>.
    ///
    ///   The seven joins + CASE therefore become an ancestor walk: an ancestor is any unit whose
    ///   Code is a PREFIX of the employee's unit Code (a materialised path is a prefix by
    ///   construction, so no recursive CTE -- which <c>CustomSqlQuery</c> would reject anyway --
    ///   is needed), and <c>ORDER BY LEN(Code)</c> ASCENDING takes the SHALLOWEST such ancestor.
    ///   Ascending is what the legacy report SQL did: it joined OrganizationUnit once per
    ///   OrgLevel1..OrgLevel7 slot and picked with a CASE that tested OrgLevel1 first and fell
    ///   through to OrgLevel7, and OrgLevel1 is the top of the tree. See
    ///   EmployeeInfo/EmployeeInfoSqlFragments.cs for the full note and the legacy CASE verbatim.
    ///
    ///   <c>Bank/BankSql.cs</c> is the reference implementation of this walk and this file follows
    ///   it. The fragments are duplicated here rather than referenced so this batch stays
    ///   self-contained (Bank's live in another batch's folder).
    ///
    ///   NO OWN-UNIT FALLBACK. Earlier ports (PayslipSql, SalaryCertificateSql, and
    ///   EmployeeInfoSqlFragments) concluded the unit TYPE was not expressible in HCMv3 and
    ///   COALESCE'd to the employee's own unit. That conclusion is wrong -- the type lives on
    ///   <c>OrgUnitProfiles.OrganizationUnitTypeId</c> -- so the fallback is an unjustified
    ///   behaviour change and is NOT reproduced here. Where the legacy CASE would have yielded
    ///   NULL, so does this.
    ///
    ///   LEVEL-WISE lookups are expressible too, not just type-wise ones: ABP's Code is a
    ///   materialised path of five-digit segments, so a unit's DEPTH is a function of its code
    ///   length -- level N has <c>LEN(Code) = 6N - 1</c>. See <c>Level2Ancestor</c>, which is how
    ///   rptPassportRequestForm's positional <c>OrgLevel2</c> lookup is translated.
    ///
    ///   NAME COLUMNS: legacy OrganizationUnit had NameEn/NameAr. ABP's table has only
    ///   <c>DisplayName</c>, so English comes from DisplayName and Arabic from
    ///   <c>OrgUnitProfiles.NameAr</c>.
    ///
    /// REWRITE 2 -- CORRELATED "TOP 1" SUBQUERIES BECOME OUTER APPLY.
    ///   The legacy corpus repeatedly writes
    ///   <c>LEFT JOIN X ON X.OID IN (SELECT TOP 1 OID FROM X WHERE ... ORDER BY ...)</c>.
    ///   Rewritten as OUTER APPLY: same "most recent row" intent, correlation visible to the
    ///   optimiser, and -- unlike the legacy form -- the picked row actually stays tied to the
    ///   employee.
    ///
    /// REWRITE 3 -- LOOK-UP QUERIES BECOME CustomSqlQuery.
    ///   Two reports drove a parameter look-up from a designer <c>SelectQuery</c> (a table picker,
    ///   not text). A SelectQuery cannot carry a WHERE clause, so it cannot be tenant-filtered and
    ///   would have listed every tenant's work locations / holiday calendars in the parameter
    ///   panel. Each is re-expressed as a CustomSqlQuery with the tenant predicate, keeping the
    ///   legacy column ALIASES (notably <c>OID</c>) so the parameter's
    ///   DynamicListLookUpSettings.ValueMember needs no edit.
    ///
    /// v23.2 CONSTRAINT: <c>CustomSqlQuery</c> rejects SQL beginning with DECLARE / WITH /
    /// BEGIN..END and rejects REPLACE() anywhere. Nothing below uses any of them; the legacy
    /// "--DECLARE @Prm... = '91'" developer scratch lines are dropped rather than kept as
    /// comments, because a leading comment line still trips the DECLARE check on some builds.
    /// </summary>
    public static class OthersAdminSql
    {
        // =========================================================================================
        // Shared fragments. const so they concatenate at COMPILE time and each query below stays a
        // plain constant the designer file can assign straight to CustomSqlQuery.Sql.
        //
        // CALLING CONVENTION: any query using these must alias its EmployeeOrgAssignments row
        // source as "orgAssign" and its Employees row source as "emp".
        // =========================================================================================

        /// <summary>
        /// The employee's org assignment CURRENT AS OF today. Legacy predicate kept verbatim
        /// (StartDate &lt;= GETDATE() AND EndDate &gt;= GETDATE()); note it is a LEFT JOIN, so an
        /// employee with no current assignment still prints, with blank org columns.
        /// </summary>
        private const string CurrentOrgAssignment = @"
LEFT JOIN EmployeeOrgAssignments orgAssign
       ON orgAssign.EmployeeId = emp.Id
      AND orgAssign.TenantId   = @TenantId
      AND orgAssign.IsDeleted  = 0
      AND orgAssign.StartDate <= GETDATE()
      AND orgAssign.EndDate   >= GETDATE()
";

        /// <summary>
        /// See REWRITE 1. The 'Department'-typed ancestor of the employee's unit.
        ///
        /// ORDERING IS LOAD-BEARING: <c>ORDER BY LEN(ancestor.Code)</c> ASCENDING takes the
        /// SHALLOWEST such ancestor. That is what the legacy CASE did -- it tested OrgLevel1 first
        /// and fell through to OrgLevel7, and OrgLevel1 is the top of the tree. Ordering DESC
        /// would take the nearest ancestor instead and silently print a different unit on any
        /// tree deep enough to have two 'Department' units on one path. Matches
        /// <c>Bank/BankSql.cs</c>, which is the reference implementation of this walk.
        ///
        /// NO FALLBACK. If the employee's unit has no 'Department'-typed ancestor, this yields
        /// NULL and the label prints blank -- which is exactly what the legacy report did when all
        /// seven CASE branches were null. Earlier ports (PayslipSql, SalaryCertificateSql,
        /// EmployeeInfoSqlFragments) COALESCE'd to the employee's own unit as a workaround for the
        /// belief that the unit type was not expressible in HCMv3. It is expressible --
        /// <c>OrgUnitProfiles.OrganizationUnitTypeId</c> carries it -- so that fallback is a
        /// behaviour change with no justification and is deliberately NOT reproduced here.
        /// </summary>
        private const string DepartmentAncestor = @"
OUTER APPLY (
    SELECT TOP 1 ancestor.DisplayName AS NameEn, ancestorProfile.NameAr AS NameAr
    FROM AbpOrganizationUnits ownUnit
    INNER JOIN AbpOrganizationUnits ancestor
            ON ancestor.TenantId = @TenantId AND ancestor.IsDeleted = 0
           AND ownUnit.Code LIKE ancestor.Code + '%'
    INNER JOIN OrgUnitProfiles ancestorProfile
            ON ancestorProfile.OrganizationUnitId = ancestor.Id
           AND ancestorProfile.TenantId = @TenantId AND ancestorProfile.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes ancestorType
            ON ancestorType.Id = ancestorProfile.OrganizationUnitTypeId
           AND ancestorType.TenantId = @TenantId AND ancestorType.IsDeleted = 0
           AND ancestorType.NameEn = 'Department'
    WHERE ownUnit.Id = orgAssign.OrganizationUnitId
      AND ownUnit.TenantId = @TenantId AND ownUnit.IsDeleted = 0
    ORDER BY LEN(ancestor.Code)
) department
";

        /// <summary>
        /// The employee's ancestor at TREE LEVEL 2 -- i.e. the legacy <c>OrgLevel2</c>, which
        /// rptPassportRequestForm takes POSITIONALLY, with no unit-type filter at all.
        ///
        /// This is the level-wise counterpart of <see cref="DepartmentAncestor"/> and it is
        /// expressible for the same reason the type-wise walk is: ABP's <c>Code</c> is a
        /// materialised path of five-digit segments joined by dots ("00001.00002.00003"), so a
        /// unit's DEPTH is a function of its code length -- level N has
        /// <c>LEN(Code) = 6N - 1</c>, hence level 2 is <c>LEN(Code) = 11</c>.
        ///
        /// So this is a faithful translation of the legacy positional lookup, not an
        /// approximation: an ancestor whose code is 11 characters long IS the OrgLevel2 unit.
        /// If the employee sits at level 1 there is no level-2 ancestor and the column is NULL,
        /// which is what the legacy LEFT JOIN produced in the same situation.
        /// </summary>
        private const string Level2Ancestor = @"
OUTER APPLY (
    SELECT TOP 1 ancestor.DisplayName AS NameEn, ancestorProfile.NameAr AS NameAr
    FROM AbpOrganizationUnits ownUnit
    INNER JOIN AbpOrganizationUnits ancestor
            ON ancestor.TenantId = @TenantId AND ancestor.IsDeleted = 0
           AND ownUnit.Code LIKE ancestor.Code + '%'
           AND LEN(ancestor.Code) = 11              -- level 2: LEN(Code) = 6 * 2 - 1
    LEFT JOIN OrgUnitProfiles ancestorProfile
           ON ancestorProfile.OrganizationUnitId = ancestor.Id
          AND ancestorProfile.TenantId = @TenantId AND ancestorProfile.IsDeleted = 0
    WHERE ownUnit.Id = orgAssign.OrganizationUnitId
      AND ownUnit.TenantId = @TenantId AND ownUnit.IsDeleted = 0
    ORDER BY LEN(ancestor.Code)
) level2Unit
";

        /// <summary>
        /// Latest CIVIL ID / residency document and latest PASSPORT document, as
        /// <c>civilDoc</c> / <c>passportDoc</c>.
        ///
        /// LEGACY MATCHING KEPT AS-IS, quirks included: the document type is found by
        /// <c>DocumentTypes.NameEn LIKE '%civil%'</c> / <c>'%passport%'</c> -- a name match, not a
        /// code match -- and the legacy inner "TOP 1 ... ORDER BY ExpiryDate DESC" over
        /// DocumentType is preserved by the correlated TOP 1 here. If a tenant has two document
        /// types whose English name contains "passport", which one wins is arbitrary in both the
        /// legacy report and this port. Fixing that needs a product decision about a stable
        /// document-type code, so it is deliberately NOT fixed here.
        /// </summary>
        private const string LatestIdentityDocuments = @"
OUTER APPLY (
    SELECT TOP 1 empDoc.DocumentNum, empDoc.ExpiryDate
    FROM EmployeeDocuments empDoc
    INNER JOIN DocumentTypes docType
            ON docType.Id = empDoc.DocumentTypeId
           AND docType.TenantId = @TenantId AND docType.IsDeleted = 0
           AND docType.NameEn LIKE '%civil%'
    WHERE empDoc.EmployeeId = emp.Id
      AND empDoc.TenantId = @TenantId AND empDoc.IsDeleted = 0
    ORDER BY empDoc.ExpiryDate DESC
) civilDoc

OUTER APPLY (
    SELECT TOP 1 empDoc.DocumentNum, empDoc.ExpiryDate
    FROM EmployeeDocuments empDoc
    INNER JOIN DocumentTypes docType
            ON docType.Id = empDoc.DocumentTypeId
           AND docType.TenantId = @TenantId AND docType.IsDeleted = 0
           AND docType.NameEn LIKE '%passport%'
    WHERE empDoc.EmployeeId = emp.Id
      AND empDoc.TenantId = @TenantId AND empDoc.IsDeleted = 0
    ORDER BY empDoc.ExpiryDate DESC
) passportDoc
";

        /// <summary>
        /// The employee's open-ended bank record, as <c>empBank</c>.
        /// Legacy predicate <c>EmployeeBank.EndDate = '9999-12-31'</c> kept verbatim.
        /// TOP 1 added: the legacy derived table would have DUPLICATED the employee row if a
        /// tenant ever held two open-ended bank records. See "Defects" in the porting notes.
        /// </summary>
        private const string OpenEndedBank = @"
OUTER APPLY (
    SELECT TOP 1 eb.AccountNum, eb.IBAN, bank.NameEn
    FROM EmployeeBanks eb
    LEFT JOIN Banks bank
           ON bank.Id = eb.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0
    WHERE eb.EmployeeId = emp.Id
      AND eb.TenantId = @TenantId AND eb.IsDeleted = 0
      AND eb.EndDate = '9999-12-31'
    ORDER BY eb.StartDate DESC
) empBank
";

        /// <summary>
        /// Last increment date and the increment DELTA, as <c>empSal</c>.
        ///
        /// Legacy shape: the newest basic-pay header is the one whose EndDate is '9999-12-31';
        /// the one immediately before it is the header whose EndDate is the day before the newest
        /// header's StartDate. DiffAmount is (sum of the current header's detail lines) minus
        /// (sum of the previous header's detail lines). That arithmetic is reproduced exactly.
        ///
        /// LEGACY QUIRK KEPT: <c>MaxDate</c> takes MAX(EndDate) and MAX(StartDate) INDEPENDENTLY,
        /// so on data where the newest header is not also the latest-starting one the two come
        /// from different rows. Reproduced rather than "fixed" -- on real data (one open-ended
        /// header per employee) they always agree.
        ///
        /// DEFECT NOT CARRIED OVER (documented in the porting notes): the legacy derived table
        /// grouped by currency and joined back on employee CODE, so an employee with basic-pay
        /// lines in two currencies produced two rows and printed the whole profile page twice.
        /// TOP 1 + ORDER BY makes it deterministic and single-row.
        /// </summary>
        private const string LastIncrement = @"
OUTER APPLY (
    SELECT TOP 1
           maxDate.StartDate AS LastIncrementDate
         , SUM(CASE WHEN bp.EndDate = '9999-12-31' THEN bpd.Amount END)
         - SUM(CASE WHEN bp.EndDate = DATEADD(DAY, -1, maxDate.StartDate) THEN bpd.Amount END) AS DiffAmount
         , cur.CurrencyShortFormEn
    FROM EmployeeBasicPays bp
    INNER JOIN EmployeeBasicPayDetails bpd
            ON bpd.EmployeeBasicPayId = bp.Id
           AND bpd.TenantId = @TenantId AND bpd.IsDeleted = 0
    LEFT JOIN Currencies cur
           ON cur.Id = bpd.CurrencyId AND cur.TenantId = @TenantId AND cur.IsDeleted = 0
    CROSS APPLY (
        SELECT MAX(m.EndDate) AS EndDate, MAX(m.StartDate) AS StartDate
        FROM EmployeeBasicPays m
        WHERE m.EmployeeId = emp.Id AND m.TenantId = @TenantId AND m.IsDeleted = 0
    ) maxDate
    WHERE bp.EmployeeId = emp.Id AND bp.TenantId = @TenantId AND bp.IsDeleted = 0
    GROUP BY maxDate.StartDate, cur.CurrencyShortFormEn
    ORDER BY maxDate.StartDate DESC, cur.CurrencyShortFormEn
) empSal
";

        // =========================================================================================
        // rptEmployeeProfile
        // =========================================================================================

        /// <summary>
        /// rptEmployeeProfile :: EmpProfQuery -- the employee dossier's master row.
        ///
        /// DROPPED FROM THE LEGACY SQL: a derived table aliased <c>EmpPassport</c> that joined
        /// <c>EmployeeDocument.DocumentType = DocumentType.Code</c> (an FK compared to a business
        /// code -- a legacy bug) and was then never projected by this query. Nothing in the layout
        /// binds it. It is dropped rather than reproduced; the passport columns this report DOES
        /// print come from <c>passportDoc</c> above, exactly as in the legacy SQL.
        ///
        /// LEGACY QUIRK KEPT: the <c>Nation</c> derived table
        /// (<c>SELECT DISTINCT Nationality, NameEn, NameAr FROM Employee JOIN Nationality</c>) is
        /// just a LEFT JOIN to Nationalities written the long way round, and is collapsed to one.
        /// The projected value is unchanged.
        /// </summary>
        public const string EmployeeProfileQuery = @"-- rptEmployeeProfile :: EmpProfQuery
SELECT
      emp.Code
    , emp.PhotoPath                     -- image on disk, see HcmReportBase.BindStoredImage
    , emp.Photo                         AS LegacyPhoto   -- only until the background mover empties it
    , emp.FullNameEn
    , department.NameEn         AS DepartmentEn   -- see REWRITE 1; NULL when no Department ancestor
    , department.NameAr         AS DepartmentAr   -- see REWRITE 1; NULL when no Department ancestor
    , orgAssign.StartDate       AS [Org Start Date]
    , orgAssign.EndDate         AS [Org End Date]
    , jobTitle.NameEn           AS Jobtitle
    , payrollArea.Name          AS PayrollArea
    , empGroup.NameEn           AS EmployeeGroup
    , empSubGroup.NameEn        AS EmployeeSubGroup
    , emp.Mobile
    , emp.Email
    , emp.JoiningDate
    , emp.FullNameAr
    , jobTitle.NameAr           AS JobtitleAr
    , emp.CivilID
    , nationality.NameEn        AS Nationality
    , civilDoc.DocumentNum      AS ResidencyNum
    , civilDoc.ExpiryDate       AS ResidancyExpiry        -- legacy spelling kept: a bound column name
    , passportDoc.DocumentNum   AS PassportNum
    , passportDoc.ExpiryDate    AS ExpiryDate
    , empBank.AccountNum
    , empBank.IBAN
    , empBank.NameEn            AS EmpBank
    , empSal.LastIncrementDate
    , empSal.DiffAmount
    , empSal.CurrencyShortFormEn
FROM Employees emp
" + CurrentOrgAssignment + @"
LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id = orgAssign.JobTitleId
      AND jobTitle.TenantId = @TenantId AND jobTitle.IsDeleted = 0

LEFT JOIN PayrollAreas payrollArea
       ON payrollArea.Id = orgAssign.PayrollAreaId
      AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0

LEFT JOIN EmployeeGroups empGroup
       ON empGroup.Id = orgAssign.EmployeeGroupId
      AND empGroup.TenantId = @TenantId AND empGroup.IsDeleted = 0

LEFT JOIN EmployeeSubGroups empSubGroup
       ON empSubGroup.Id = orgAssign.EmployeeSubGroupId
      AND empSubGroup.TenantId = @TenantId AND empSubGroup.IsDeleted = 0

LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId
      AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
" + DepartmentAncestor + LatestIdentityDocuments + OpenEndedBank + LastIncrement + @"
WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        /// <summary>
        /// rptEmployeeProfile :: EmpSalaryDetail, rptBusinessTrip :: EmpSalaryDetail, and
        /// srptEmpProfileWageDetail :: EmpProfQuery.
        ///
        /// All three legacy texts are the same query with cosmetic differences (the parent
        /// reports resolved the currency with a scalar subquery, the subreport with a LEFT JOIN;
        /// the results are identical). Unified into one constant.
        ///
        /// Current basic-pay lines: the open-ended header (EndDate '9999-12-31') and its wage-type
        /// detail rows. <c>PreviousAmount</c> is the total of the immediately preceding header --
        /// same "day before the newest header's StartDate" rule as <see cref="LastIncrement"/>,
        /// and it is repeated on EVERY detail row, which is how the legacy report printed it.
        ///
        /// PARAMETER DROPPED: the legacy subreport declared a <c>PrmDate</c> QueryParameter that
        /// the SQL never referenced. It is not carried over -- an unreferenced QueryParameter is
        /// dead weight and v23.2 is stricter about them. PrmDate remains a REPORT parameter on the
        /// subreport, because the parent binds it.
        /// </summary>
        public const string EmployeeWageDetailQuery = @"-- rptEmployeeProfile / rptBusinessTrip / srptEmpProfileWageDetail :: current basic-pay lines
SELECT
      emp.Code        AS [Employee Code]
    , emp.FullNameEn  AS [Employee Name]
    , bp.StartDate
    , bp.EndDate
    , wageType.NameEn
    , bpd.Amount
    , cur.CurrencyShortFormEn AS Currency
    , previous.PreviousAmount
FROM Employees emp
INNER JOIN EmployeeBasicPays bp
        ON bp.EmployeeId = emp.Id
       AND bp.TenantId = @TenantId AND bp.IsDeleted = 0
INNER JOIN EmployeeBasicPayDetails bpd
        ON bpd.EmployeeBasicPayId = bp.Id
       AND bpd.TenantId = @TenantId AND bpd.IsDeleted = 0
INNER JOIN WageTypes wageType
        ON wageType.Id = bpd.WageTypeId
       AND wageType.TenantId = @TenantId AND wageType.IsDeleted = 0
LEFT JOIN Currencies cur
       ON cur.Id = bpd.CurrencyId
      AND cur.TenantId = @TenantId AND cur.IsDeleted = 0

OUTER APPLY (
    SELECT SUM(CASE WHEN pbp.EndDate = DATEADD(DAY, -1, maxDate.StartDate)
                    THEN pbpd.Amount END) AS PreviousAmount
    FROM EmployeeBasicPays pbp
    INNER JOIN EmployeeBasicPayDetails pbpd
            ON pbpd.EmployeeBasicPayId = pbp.Id
           AND pbpd.TenantId = @TenantId AND pbpd.IsDeleted = 0
    CROSS APPLY (
        SELECT MAX(m.EndDate) AS EndDate, MAX(m.StartDate) AS StartDate
        FROM EmployeeBasicPays m
        WHERE m.EmployeeId = emp.Id AND m.TenantId = @TenantId AND m.IsDeleted = 0
    ) maxDate
    WHERE pbp.EmployeeId = emp.Id AND pbp.TenantId = @TenantId AND pbp.IsDeleted = 0
) previous

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode
  AND bp.EndDate    = '9999-12-31';";

        // =========================================================================================
        // rptBusinessTrip
        // =========================================================================================

        /// <summary>
        /// rptBusinessTrip :: EmpProfQuery.
        ///
        /// The legacy text is rptEmployeeProfile's query with three differences, all preserved:
        ///   * no PayrollArea / EmployeeGroup / EmployeeSubGroup columns;
        ///   * ResidencyNum and ResidancyExpiry come from <c>EmployeeVisaInfo</c> (the residency
        ///     record) rather than from the civil-ID document;
        ///   * PassportNum / ExpiryDate come from a derived table restricted to a CURRENTLY VALID
        ///     passport (GETDATE() BETWEEN IssueDate AND ExpiryDate).
        ///
        /// DEFECT NOT CARRIED OVER: that legacy passport derived table joined
        /// <c>EmployeeDocument.DocumentType = DocumentType.Code</c> -- an integer FK compared to a
        /// business code column -- and then filtered <c>DocumentType.Code = 1</c>. In HCMv3
        /// <c>DocumentTypes.Code</c> is nvarchar, so reproducing that join literally would make
        /// SQL Server implicitly convert the whole column to int and throw on the first
        /// non-numeric code. It is rewritten to the correct FK join plus the same
        /// name-based type match the rest of this file uses, keeping the "currently valid"
        /// predicate. See the porting notes.
        ///
        /// LEGACY QUIRK KEPT: <c>EmployeeVisaInfos</c> is joined with no date restriction and no
        /// TOP 1 in the legacy SQL, so several residency rows would duplicate the page. TOP 1
        /// ordered by EndDate DESC is applied here for the same reason as
        /// <see cref="OpenEndedBank"/>.
        /// </summary>
        public const string BusinessTripQuery = @"-- rptBusinessTrip :: EmpProfQuery
SELECT
      emp.Code
    , emp.PhotoPath                     -- image on disk, see HcmReportBase.BindStoredImage
    , emp.Photo                         AS LegacyPhoto   -- only until the background mover empties it
    , emp.FullNameEn
    , department.NameEn         AS DepartmentEn   -- see REWRITE 1; NULL when no Department ancestor
    , department.NameAr         AS DepartmentAr   -- see REWRITE 1; NULL when no Department ancestor
    , orgAssign.StartDate       AS [Org Start Date]
    , orgAssign.EndDate         AS [Org End Date]
    , jobTitle.NameEn           AS Jobtitle
    , emp.Mobile
    , emp.Email
    , emp.JoiningDate
    , emp.FullNameAr
    , jobTitle.NameAr           AS JobtitleAr
    , emp.CivilID
    , nationality.NameEn        AS Nationality
    , visaInfo.ResidencyNum
    , visaInfo.PassportExpiryDate AS ResidancyExpiry        -- legacy spelling kept: a bound column name
    , validPassport.DocumentNum AS PassportNum
    , validPassport.ExpiryDate  AS ExpiryDate
    , empBank.AccountNum
    , empBank.IBAN
    , empBank.NameEn            AS EmpBank
    , empSal.LastIncrementDate
    , empSal.DiffAmount
    , empSal.CurrencyShortFormEn
FROM Employees emp
" + CurrentOrgAssignment + @"
LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id = orgAssign.JobTitleId
      AND jobTitle.TenantId = @TenantId AND jobTitle.IsDeleted = 0

LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId
      AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
" + DepartmentAncestor + @"
OUTER APPLY (
    SELECT TOP 1 evi.ResidencyNum, evi.PassportExpiryDate
    FROM EmployeeVisaInfos evi
    WHERE evi.EmployeeId = emp.Id
      AND evi.TenantId = @TenantId AND evi.IsDeleted = 0
    ORDER BY evi.EndDate DESC
) visaInfo

OUTER APPLY (
    SELECT TOP 1 empDoc.DocumentNum, empDoc.ExpiryDate
    FROM EmployeeDocuments empDoc
    INNER JOIN DocumentTypes docType
            ON docType.Id = empDoc.DocumentTypeId
           AND docType.TenantId = @TenantId AND docType.IsDeleted = 0
           AND docType.NameEn LIKE '%passport%'
    WHERE empDoc.EmployeeId = emp.Id
      AND empDoc.TenantId = @TenantId AND empDoc.IsDeleted = 0
      AND GETDATE() BETWEEN empDoc.IssueDate AND empDoc.ExpiryDate
    ORDER BY empDoc.ExpiryDate DESC
) validPassport
" + OpenEndedBank + LastIncrement + @"
WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        // =========================================================================================
        // rptEmpWorklocation / rptLocationWiseEmp
        // =========================================================================================

        /// <summary>
        /// rptEmpWorklocation :: EmpWorklocationQuery -- active employees, optionally narrowed to
        /// one work location.
        ///
        /// The optional-parameter idiom is the legacy one, translated: legacy wrote
        /// <c>ISNULL(@PrmWorkLocation, '') != ''</c> against an INT parameter, where SQL Server
        /// converts '' to 0, so "no filter" is signalled by 0. Written as an explicit
        /// <c>&lt;&gt; 0</c> here; behaviour is identical and the intent is now readable.
        /// </summary>
        public const string EmpWorklocationQuery = @"-- rptEmpWorklocation :: EmpWorklocationQuery
SELECT
      emp.Id            AS OID
    , emp.Code
    , emp.FullNameAr
    , emp.FullNameEn
    , emp.CivilID
    , jobTitle.NameAr     AS JobTitleAr
    , jobTitle.NameEn     AS JobTitleEn
    , nationality.NameAr  AS NationalityAr
    , nationality.NameEn  AS NationalityEn
    , workLocation.Id     AS WL_OID
    , workLocation.NameEn AS WL_en
    , workLocation.NameAr AS WL_Ar
    , separation.LastDayInService
FROM Employees emp
" + CurrentOrgAssignment + @"
LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id = orgAssign.JobTitleId
      AND jobTitle.TenantId = @TenantId AND jobTitle.IsDeleted = 0

LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId
      AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0

LEFT JOIN WorkLocations workLocation
       ON workLocation.Id = orgAssign.WorkLocationId
      AND workLocation.TenantId = @TenantId AND workLocation.IsDeleted = 0

-- legacy: LEFT JOIN EmployeeSeparation ON OID IN (SELECT TOP 1 ... ORDER BY SubmissionDate DESC)
OUTER APPLY (
    SELECT TOP 1 sep.LastDayInService
    FROM EmployeeSeparations sep
    WHERE sep.EmployeeId = emp.Id
      AND sep.TenantId = @TenantId AND sep.IsDeleted = 0
    ORDER BY sep.SubmissionDate DESC
) separation

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Status    = 1                     -- EmployeeStatus.Active
  AND (   (ISNULL(@PrmWorkLocation, 0) <> 0 AND workLocation.Id = @PrmWorkLocation)
       OR (ISNULL(@PrmWorkLocation, 0) =  0));";

        /// <summary>
        /// rptLocationWiseEmp :: EmpWorklocationQuery -- active employees at ONE work location.
        ///
        /// *** FEATURE PARTIALLY LOST -- SPONSOR. *** Legacy read the sponsor from
        /// <c>WorkLocation.Sponsor</c>. HCMv3's <c>WorkLocations</c> has NO sponsor column and no
        /// HCMv3 entity links a work location to a sponsor, so "the sponsor of this work location"
        /// is not expressible.
        ///
        /// Only ONE of the three legacy sponsor columns is actually bound by the layout --
        /// <c>TradeLicenseNum</c>, printed in the page header. It is sourced instead from the
        /// EMPLOYEE's own sponsor record (<c>EmployeeSponsors</c> -&gt; <c>Sponsors</c> -&gt;
        /// <c>SponsorLicenseInfos</c>), which is the nearest true equivalent: on this report every
        /// row is one work location's staff, who in practice share a sponsor. It is a SEMANTIC
        /// DEVIATION and needs a product decision -- see the porting notes.
        ///
        /// <c>SponsorEn</c> / <c>SponsorAr</c> are projected from the same sponsor record for
        /// column-shape fidelity; nothing in the layout binds them.
        ///
        /// Also note <c>TradeLicenseNum</c> lives on <c>SponsorLicenseInfos</c> in HCMv3, not on
        /// <c>Sponsors</c>; the newest licence (by LicenseExpiryDate) is taken.
        /// </summary>
        public const string LocationWiseEmpQuery = @"-- rptLocationWiseEmp :: EmpWorklocationQuery
SELECT
      sponsor.NameEn          AS SponsorEn
    , sponsor.NameAr          AS SponsorAr
    , sponsor.TradeLicenseNum
    , workLocation.NameEn     AS WorkLocationEn
    , workLocation.NameAr     AS WorkLocationAr
    , emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.CivilID
    , nationality.NameEn      AS NationalityEn
    , nationality.NameAr      AS NationalityAr
FROM Employees emp

LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId
      AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
" + CurrentOrgAssignment + @"
LEFT JOIN WorkLocations workLocation
       ON workLocation.Id = orgAssign.WorkLocationId
      AND workLocation.TenantId = @TenantId AND workLocation.IsDeleted = 0

-- SEMANTIC DEVIATION: the employee's sponsor, not the work location's. See the summary above.
OUTER APPLY (
    SELECT TOP 1 s.NameEn, s.NameAr, lic.TradeLicenseNum
    FROM EmployeeSponsors es
    INNER JOIN Sponsors s
            ON s.Id = es.SponsorId AND s.TenantId = @TenantId AND s.IsDeleted = 0
    OUTER APPLY (
        SELECT TOP 1 sli.TradeLicenseNum
        FROM SponsorLicenseInfos sli
        WHERE sli.SponsorId = s.Id AND sli.TenantId = @TenantId AND sli.IsDeleted = 0
        ORDER BY sli.LicenseExpiryDate DESC
    ) lic
    WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
    ORDER BY es.EndDate DESC
) sponsor

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Status    = 1                     -- EmployeeStatus.Active
  AND workLocation.Id = @PrmWorkLocation;";

        /// <summary>
        /// Work-location look-up behind the PrmWorkLocation parameter on BOTH work-location
        /// reports.
        ///
        /// See REWRITE 3: this replaces nothing -- the legacy text was already a CustomSqlQuery --
        /// but the tenant predicate is new and mandatory, or the parameter drop-down would list
        /// every tenant's work locations. The <c>OID</c> alias is kept because the parameter's
        /// DynamicListLookUpSettings.ValueMember is "OID".
        /// </summary>
        public const string WorkLocationLookupQuery = @"-- rptEmpWorklocation / rptLocationWiseEmp :: WorkLocation look-up
SELECT
      workLocation.Id AS OID
    , workLocation.Code
    , workLocation.NameEn
FROM WorkLocations workLocation
WHERE workLocation.TenantId  = @TenantId
  AND workLocation.IsDeleted = 0
ORDER BY workLocation.NameEn;";

        // =========================================================================================
        // rptHolidayCalendar
        // =========================================================================================

        /// <summary>
        /// rptHolidayCalendar :: Query -- every holiday date in the tenant, with its holiday and
        /// calendar names.
        ///
        /// LEGACY BEHAVIOUR KEPT: the report narrows to a date range and one calendar with a
        /// CLIENT-SIDE <c>FilterString</c>, not in SQL. That is normally worth fixing (see
        /// SalaryCertificateSql rewrite note 3), but here it is deliberately left alone: the two
        /// date bounds and the calendar are all report parameters the user changes in the viewer,
        /// the result set is one row per holiday date per tenant (tens of rows, not payroll
        /// volumes), and the tenant predicate below already prevents any cross-tenant fetch.
        ///
        /// <c>HolidayCalendar</c> is projected as the raw FK id, matching the legacy column name,
        /// because the FilterString compares it to the ?Calendar parameter's value.
        /// </summary>
        public const string HolidayCalendarQuery = @"-- rptHolidayCalendar :: Query
SELECT
      hPeriod.HolidayDate
    , holiday.HolidayCalendarId AS HolidayCalendar
    , holiday.NameEn            AS HolidayEn
    , holiday.NameAr            AS HolidayAr
    , hCalendar.NameEn          AS CalenderEn
    , hCalendar.NameAr          AS CalenderAr
FROM HolidayPeriods hPeriod
LEFT JOIN Holidays holiday
       ON holiday.Id = hPeriod.HolidayId
      AND holiday.TenantId = @TenantId AND holiday.IsDeleted = 0
LEFT JOIN HolidayCalendars hCalendar
       ON hCalendar.Id = holiday.HolidayCalendarId
      AND hCalendar.TenantId = @TenantId AND hCalendar.IsDeleted = 0
WHERE hPeriod.TenantId  = @TenantId
  AND hPeriod.IsDeleted = 0
ORDER BY hPeriod.HolidayDate;";

        /// <summary>
        /// rptHolidayCalendar :: QueryHoliday -- look-up behind the Calendar parameter.
        ///
        /// See REWRITE 3. The legacy version was a designer <c>SelectQuery</c> over the
        /// HolidayCalendar TABLE, which cannot carry a WHERE clause and would therefore have
        /// listed every tenant's calendars in the parameter panel. Re-expressed as a
        /// CustomSqlQuery with the tenant predicate; the four legacy column names (OID, Code,
        /// NameEn, NameAr) are preserved so the parameter's DynamicListLookUpSettings needs no
        /// edit.
        /// </summary>
        public const string HolidayCalendarLookupQuery = @"-- rptHolidayCalendar :: QueryHoliday look-up
SELECT
      hCalendar.Id AS OID
    , hCalendar.Code
    , hCalendar.NameEn
    , hCalendar.NameAr
FROM HolidayCalendars hCalendar
WHERE hCalendar.TenantId  = @TenantId
  AND hCalendar.IsDeleted = 0
ORDER BY hCalendar.NameEn;";

        // =========================================================================================
        // rptPassportInOut / rptPassportRequestForm
        // =========================================================================================

        /// <summary>
        /// rptPassportInOut :: SalaryCertificate_LNDQuery (legacy query name kept -- it is a
        /// copy-paste leftover, but it is the DataMember the layout binds to).
        ///
        /// This is a pre-printed hand-off form: the SQL supplies the employee's identity block and
        /// the in/out movement rows are written on paper. Note the org assignment here is resolved
        /// AS OF <c>@PrmDate</c>, not as of today -- that is the legacy behaviour and is kept, so
        /// this report does NOT use the shared <see cref="CurrentOrgAssignment"/> fragment.
        ///
        /// LEGACY QUIRK KEPT: the passport document sub-select matches
        /// <c>DocumentType.NameEn IN ('PASSPORT')</c> -- an exact, case-insensitive-by-collation
        /// match, unlike the LIKE '%passport%' the other reports in this batch use. Left as-is:
        /// aligning them would change which document a tenant's report picks.
        /// </summary>
        public const string PassportInOutQuery = @"-- rptPassportInOut :: SalaryCertificate_LNDQuery
SELECT
      emp.Id   AS OID
    , emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.CivilID
    , passportDoc.DocumentNum AS PassportNum
    , emp.JoiningDate
    , nationality.NameEn AS NationalityEn
    , nationality.NameAr AS NationalityAr
    , jobTitle.NameEn    AS JobTitleEn
    , jobTitle.NameAr    AS JobTitleAr
FROM Employees emp

LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId
      AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0

-- legacy: org assignment as of @PrmDate, NOT as of today
LEFT JOIN EmployeeOrgAssignments orgAssign
       ON orgAssign.EmployeeId = emp.Id
      AND orgAssign.TenantId   = @TenantId
      AND orgAssign.IsDeleted  = 0
      AND orgAssign.StartDate <= @PrmDate
      AND orgAssign.EndDate   >= @PrmDate

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id = orgAssign.JobTitleId
      AND jobTitle.TenantId = @TenantId AND jobTitle.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 empDoc.DocumentNum
    FROM EmployeeDocuments empDoc
    LEFT JOIN DocumentTypes docType
           ON docType.Id = empDoc.DocumentTypeId
          AND docType.TenantId = @TenantId AND docType.IsDeleted = 0
    WHERE empDoc.EmployeeId = emp.Id
      AND empDoc.TenantId = @TenantId AND empDoc.IsDeleted = 0
      AND docType.NameEn IN ('PASSPORT')
    ORDER BY empDoc.ExpiryDate DESC
) passportDoc

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        /// <summary>
        /// rptPassportRequestForm :: Query.
        ///
        /// LEGACY BEHAVIOUR KEPT: this report has no WHERE on the employee at all -- it selects
        /// every employee and the layout narrows to the chosen one(s) with the client-side
        /// <c>FilterString "[Code] In (?PrmEmpCode)"</c>. PrmEmpCode is a MULTI-VALUE parameter,
        /// which cannot be expressed as a single SQL parameter without dynamic SQL, so unlike the
        /// salary certificates this filter is NOT pushed into SQL. The tenant predicate below
        /// bounds the fetch to one tenant, so this is a performance characteristic, not a leak.
        ///
        /// DEPARTMENT: legacy took <c>OrgLevel2</c> with NO unit-type filter -- i.e. "whatever
        /// sits at level 2", a positional convention this report alone relies on. That translates
        /// exactly, not approximately: ABP's <c>Code</c> is a materialised path of five-digit
        /// segments, so level 2 is the ancestor whose <c>LEN(Code) = 11</c>. See
        /// <c>Level2Ancestor</c>. No own-unit fallback -- an employee sitting at level 1 has no
        /// level-2 ancestor and gets NULL, exactly as the legacy LEFT JOIN did.
        ///
        /// The legacy join to OrgLevel1 was dead (never projected) and is dropped.
        /// </summary>
        public const string PassportRequestFormQuery = @"-- rptPassportRequestForm :: Query
SELECT
      emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.JoiningDate
    , jobTitle.NameEn            AS JobTitleEn
    , jobTitle.NameAr            AS JobTitleAr
    , level2Unit.NameEn          AS DeptEn     -- legacy OrgLevel2; see the DEPARTMENT note above
    , level2Unit.NameAr          AS DeptAr     -- legacy OrgLevel2; see the DEPARTMENT note above
FROM Employees emp
" + CurrentOrgAssignment + @"
LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id = orgAssign.JobTitleId
      AND jobTitle.TenantId = @TenantId AND jobTitle.IsDeleted = 0
" + Level2Ancestor + @"
WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0;";
    }
}
