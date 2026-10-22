// SQL for the "Others / HR forms" report batch, rewritten for the HCMv3 schema.
//
// Reports covered: rptClearanceForm, rptFinalClearance_EOS, rptDisciplinaryForm,
// rptEmployeeWarningNotice, rptProbationaryReportForm, rptPayChange, rptEmploymentContract,
// rptEmploymentContractAr023, rptEmploymentContractEn023.
//
// Held here (rather than in a .resx, where the legacy reports kept it) for the same reason
// PayslipSql.cs / SalaryCertificateSql.cs give: the SQL is the expensive, reviewable part of the
// port and must be diffable.
//
// -------------------------------------------------------------------------------------------------
// MULTI-TENANCY -- READ BEFORE EDITING
// -------------------------------------------------------------------------------------------------
// These run through DevExpress SqlDataSource as RAW SQL, so EF Core's per-tenant global query filter
// does NOT apply. Every query filters TenantId explicitly via @TenantId, which is bound to the hidden
// PrmTenantId report parameter by HcmReportBase.EnsureHcmParameters(). Removing an
// "AND <alias>.TenantId = @TenantId" silently leaks rows across tenants. Every soft-deletable table
// also carries "AND <alias>.IsDeleted = 0".
//
// The legacy HCM10 corpus contains ZERO references to TenantId or IsDeleted -- HCM10 was
// single-tenant and hard-deleted -- so both predicates are ADDED, never copied.
//
// -------------------------------------------------------------------------------------------------
// MECHANICAL RENAMES applied throughout (legacy XPO -> HCMv3 EF Core)
// -------------------------------------------------------------------------------------------------
//   Employee            -> Employees              EmployeeOrgAssignment -> EmployeeOrgAssignments
//   JobTitle            -> JobTitles              WorkLocation          -> WorkLocations
//   Nationality         -> Nationalities          Title                 -> Titles
//   EmployeeBasicPay    -> EmployeeBasicPays      EmployeeBasicPayDetail-> EmployeeBasicPayDetails
//   WageType            -> WageTypes              Currency              -> Currencies
//   PayrollArea         -> PayrollAreas           CostCenter            -> CostCenters
//   EmployeeManager     -> EmployeeManagers       EmployeeSponsor       -> EmployeeSponsors
//   DisciplinaryType    -> DisciplinaryTypes      DisciplinaryRequest   -> EmployeeDisciplinaries
//   OrganizationUnit    -> AbpOrganizationUnits
//   OID -> Id ; <EntityName> (FK column) -> <EntityName>Id
//
// -------------------------------------------------------------------------------------------------
// NON-MECHANICAL REWRITES (each one is a decision, not a rename)
// -------------------------------------------------------------------------------------------------
//  R1. ORG-UNIT LEVELS -> ANCESTOR WALK. Legacy EmployeeOrgAssignment carried seven flat FK columns
//      (OrgLevel1..7) into OrganizationUnit, and every query here picked "whichever of the seven
//      points at a unit whose OrganizationUnitType is Company / Department / SubDepartment".
//      HCMv3 has a SINGLE EmployeeOrgAssignments.OrganizationUnitId into AbpOrganizationUnits, an
//      ABP materialised-path tree (Code / ParentId). The unit TYPE is not on the ABP table -- it
//      lives on the HCMv3-side OrgUnitProfiles.OrganizationUnitTypeId -> OrganizationUnitTypes.
//
//      So the seven aliased joins collapse to ONE ancestor walk: of the employee's unit and all its
//      ancestors (ancestor <=> the employee unit's Code starts with the ancestor's Code), take the
//      SHALLOWEST whose profile type is named 'Company' (resp. 'Department', 'SubDepartment').
//      This is the pattern Bank/BankSql.cs established; see its "Company / Division org levels"
//      note. It is a faithful translation of the legacy intent, not a fallback.
//
//      NOTE: PayslipSql.cs and SalaryCertificateSql.cs concluded the unit type was "not expressible"
//      and fell back to the employee's own unit. That conclusion is wrong and is NOT propagated
//      here. Those two files are being corrected centrally and are deliberately not touched by this
//      batch.
//
//      Arabic org names: AbpOrganizationUnits has only DisplayName, but OrgUnitProfiles carries a
//      NameAr, so the *Ar aliases below take OrgUnitProfiles.NameAr of the matched ancestor. No
//      feature is lost.
//
//  R2. COMPANY LOGO. Legacy read OrganizationUnit.logo, an image column; HCMv3 keeps logos in the
//      file store. "Companylogo" stays a NULL VARBINARY(MAX) placeholder; the queries whose layout
//      prints it also return the employee unit as CompanylogoOrgUnitId, resolved by HcmReportBase
//      through ICompanyLogoResolver with the legacy Company_1..7 rule. (Ar023 is a customer variant
//      with no logo box in HCMv3 and is left as is.)
//
//  R3. PASSPORT NUMBER. Employees has no PassportNum column in HCMv3. It comes from
//      EmployeeDocuments joined to the DocumentTypes row named 'Passport' -- exactly the
//      substitution GovtFormsSqlFirstHalf rewrite note 4 established.
//
//  R4. "LEFT JOIN X ON X.OID IN (SELECT TOP 1 ... ORDER BY ...)" correlated-subquery idiom is
//      rewritten as OUTER APPLY: same intent, correlation visible to the optimiser, and no
//      accidental cross-employee match (in the legacy form the inner reference resolved against
//      the INNER alias in several of these queries -- a latent bug).
//
//  R5. WAGE-TYPE CODE LITERALS ARE QUOTED. The legacy queries wrote WageType.Code IN (1002,1035).
//      HCMv3 WageTypes.Code is nvarchar; unquoted, SQL Server implicitly converts the whole column
//      to int and throws on the first non-numeric code in the tenant. All such literals are quoted.
//
//  R6. v23.2 CustomSqlQuery REJECTS Sql that starts with DECLARE / WITH / BEGIN, and rejects
//      REPLACE() anywhere in the text. The legacy ";With CTE1 As (...) Select ... from CTE1"
//      contract-wage query is rewritten as a plain SELECT over a derived table, and rptPayChange's
//      REPLACE(...,',,','') is rewritten as a CASE/LEFT that produces byte-identical output (see
//      the note on PayChangeQuery). The legacy commented-out "--DECLARE @Prm..." preambles are
//      dropped: they are dead text and one of them would have been the first token.
//
//  R7. CLIENT-SIDE FilterString -> SQL PREDICATE. rptClearanceForm carried
//      FilterString = "[Code] In (?PrmEmpCode)" over a raw SqlDataSource, i.e. it fetched EVERY
//      employee row to the report engine and then discarded all but one. Replaced by
//      "AND emp.Code = @PrmEmpCode" in SQL, as SalaryCertificateSql rewrite note 3 established.
//      This is not tidying: on a raw SqlDataSource a client-side filter runs AFTER the fetch.

namespace HCMv3.Reports.Others
{
    /// <summary>Raw SQL used by the Others/HR-forms report batch.</summary>
    public static class OthersHrFormsSql
    {
        // ===========================================================================================
        // rptClearanceForm
        // ===========================================================================================

        /// <summary>
        /// rptClearanceForm :: Query.
        ///
        /// Legacy picked DeptEn/DeptAr from whichever of OrgLevel1..7 was typed 'SubDepartment'; that
        /// becomes the ancestor walk of rewrite note R1, looking for 'SubDepartment' rather than
        /// 'Company'. ForReason is a literal empty string in the legacy query too -- the
        /// form is printed blank for the user to fill in by hand -- and LastWorkingDate is GETDATE().
        /// Both are kept verbatim.
        ///
        /// Employee filter added per rewrite note R7.
        /// </summary>
        public const string ClearanceFormQuery = @"-- rptClearanceForm :: Query
SELECT
      emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.JoiningDate
    , jobTitle.NameEn       AS JobTitleEn
    , jobTitle.NameAr       AS JobTitleAr
    , subDept.DisplayName   AS DeptEn      -- see rewrite note R1
    , subDept.NameAr        AS DeptAr      -- see rewrite note R1
    , ''                    AS ForReason   -- legacy: always blank, filled in by hand
    , GETDATE()             AS LastWorkingDate

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

LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id        = orgAsg.OrganizationUnitId
      AND orgUnit.TenantId  = @TenantId
      AND orgUnit.IsDeleted = 0

-- The seven OrgLevel1..7 'SubDepartment' joins, collapsed to one ancestor walk. See rewrite note R1.
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName, ancp.NameAr
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp
            ON ancp.OrganizationUnitId = anc.Id AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct
            ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId  = @TenantId
      AND anc.IsDeleted = 0
      AND orgUnit.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'SubDepartment'
    ORDER BY LEN(anc.Code)
) subDept

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        // ===========================================================================================
        // rptFinalClearance_EOS
        // ===========================================================================================

        /// <summary>
        /// rptFinalClearance_EOS :: SickLeaveFormQuery.
        ///
        /// The legacy query name ("SickLeaveFormQuery", data source "SickLeaveFormDS") is a
        /// copy-paste leftover from rptSickLeaveForm; it is kept because the report's DataMember
        /// binds it by that name.
        ///
        /// The legacy nationality join was a pointless self-join derived table
        /// ("SELECT DISTINCT Nationality, B.NameEn, B.NameAr FROM Employee A JOIN Nationality B ...")
        /// that fanned out over every employee sharing a nationality before DISTINCT collapsed it.
        /// Collapsed to a direct join on Employees.NationalityId -- same result, no fan-out. The
        /// legacy SELECT DISTINCT is kept because the org join can still multiply rows.
        /// </summary>
        public const string FinalClearanceEosQuery = @"-- rptFinalClearance_EOS :: SickLeaveFormQuery
SELECT DISTINCT
      emp.FullNameEn
    , emp.JoiningDate
    , emp.ProbationEndDate
    , emp.CivilID
    , nationality.NameEn            AS Nationality
    , company.DisplayName           AS CompanyEn                  -- see rewrite note R1
    , CAST(NULL AS VARBINARY(MAX))  AS Companylogo                -- see rewrite note R2: placeholder
    , orgAsg.OrganizationUnitId     AS CompanylogoOrgUnitId       -- see rewrite note R2

FROM Employees emp

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id
      AND orgAsg.TenantId   = @TenantId
      AND orgAsg.IsDeleted  = 0
      AND orgAsg.StartDate <= @PrmFormDate
      AND orgAsg.EndDate   >= @PrmFormDate

LEFT JOIN Nationalities nationality
       ON nationality.Id        = emp.NationalityId
      AND nationality.TenantId  = @TenantId
      AND nationality.IsDeleted = 0

LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id        = orgAsg.OrganizationUnitId
      AND orgUnit.TenantId  = @TenantId
      AND orgUnit.IsDeleted = 0

-- The seven OrgLevel1..7 'Company' joins, collapsed to one ancestor walk. See rewrite note R1.
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName, ancp.NameAr
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp
            ON ancp.OrganizationUnitId = anc.Id AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct
            ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId  = @TenantId
      AND anc.IsDeleted = 0
      AND orgUnit.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        // ===========================================================================================
        // rptDisciplinaryForm
        // ===========================================================================================

        /// <summary>
        /// rptDisciplinaryForm :: EmpDiscReportQuery.
        ///
        /// ENTITY SUBSTITUTION. The legacy report read
        /// <c>DisciplinaryRequest</c> -- a self-service workflow request, joined to
        /// <c>WorkflowRequest</c> for the employee. HCMv3 has no DisciplinaryRequest; the equivalent
        /// record is the admin-side <c>EmployeeDisciplinaries</c>, which carries EmployeeId directly
        /// (so the WorkflowRequest join disappears) plus DisciplinaryDate, DisciplinaryTime,
        /// DisciplinaryTypeId, WorkLocationId and DisciplinaryDeductionTypeId. @PrmOid now addresses
        /// EmployeeDisciplinaries.Id.
        ///
        /// TWO EXTRA COLUMNS replace XAF code-behind that opened an XPObjectSpaceProvider from inside
        /// the report (a DevExpress-constructed report has no DI container, so anything it needs must
        /// arrive through its data source):
        ///   DeductionTypeNameEn -- legacy read <c>rec.DisciplinaryDeductionType</c>, a free-text
        ///     string on the request, and compared it to 'Written warning' / 'Deduction 4 hours' /
        ///     ... to decide which stamp picture to show. HCMv3 normalises it into the
        ///     DisciplinaryDeductionTypes lookup, so the NameEn of that lookup row is projected and
        ///     the comparison happens in Detail_BeforePrint against the same literals.
        ///   PriorDeductionCount -- legacy read <c>rec.CurrentDeductionCount</c>, a counter column
        ///     maintained on the request row. HCMv3 has no such column.
        ///     *** DEVIATION: it is derived here as the number of EARLIER disciplinaries of the SAME
        ///     type for the same employee. That matches the intent ("this is the Nth offence") but is
        ///     not guaranteed to reproduce a legacy counter that could be edited by hand. Needs a
        ///     product decision. ***
        /// </summary>
        public const string DisciplinaryFormQuery = @"-- rptDisciplinaryForm :: EmpDiscReportQuery
SELECT
      disc.Id                   AS OID
    , emp.Code
    , emp.FullNameAr            AS EmpName
    , emp.CivilID
    , workLocation.NameAr       AS WorkLocation
    , jobTitle.NameAr           AS JobTitle
    , disc.DisciplinaryDate
    , disc.DisciplinaryTime
    , discType.NameAr           AS DiscTypeNameAr
    , disc.EmployeeId           AS Employee
    , deductionType.NameEn      AS DeductionTypeNameEn        -- replaces XAF code-behind, see summary
    , priorDisc.PriorCount      AS PriorDeductionCount        -- replaces XAF code-behind, see summary

FROM EmployeeDisciplinaries disc

LEFT JOIN Employees emp
       ON emp.Id        = disc.EmployeeId
      AND emp.TenantId  = @TenantId
      AND emp.IsDeleted = 0

LEFT JOIN WorkLocations workLocation
       ON workLocation.Id        = disc.WorkLocationId
      AND workLocation.TenantId  = @TenantId
      AND workLocation.IsDeleted = 0

LEFT JOIN DisciplinaryTypes discType
       ON discType.Id        = disc.DisciplinaryTypeId
      AND discType.TenantId  = @TenantId
      AND discType.IsDeleted = 0

LEFT JOIN DisciplinaryDeductionTypes deductionType
       ON deductionType.Id        = disc.DisciplinaryDeductionTypeId
      AND deductionType.TenantId  = @TenantId
      AND deductionType.IsDeleted = 0

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

OUTER APPLY (
    SELECT COUNT(*) AS PriorCount
    FROM EmployeeDisciplinaries prior
    WHERE prior.EmployeeId          = disc.EmployeeId
      AND prior.DisciplinaryTypeId  = disc.DisciplinaryTypeId
      AND prior.DisciplinaryDate    < disc.DisciplinaryDate
      AND prior.TenantId            = @TenantId
      AND prior.IsDeleted           = 0
) priorDisc

WHERE disc.TenantId  = @TenantId
  AND disc.IsDeleted = 0
  AND disc.Id        = @PrmOid;";

        // ===========================================================================================
        // rptEmployeeWarningNotice
        // ===========================================================================================

        /// <summary>
        /// rptEmployeeWarningNotice :: Query.
        ///
        /// Legacy picked CompanyEn/Companylogo from the 'Company'-typed OrgLevel and DepartmentEn
        /// from the 'Department'-typed one -- fourteen LEFT JOINs onto OrganizationUnit in all. They
        /// become TWO ancestor walks over the same tree, one per type; see rewrite note R1.
        /// CompanyEn and DepartmentEn therefore still resolve independently, as they did in the
        /// legacy print. The logo comes from CompanylogoOrgUnitId (rewrite note R2).
        ///
        /// The manager chain (EmployeeManager -> Employee AS EmpManager) is a straight rename.
        /// </summary>
        public const string EmployeeWarningNoticeQuery = @"-- rptEmployeeWarningNotice :: Query
SELECT
      emp.Code
    , emp.FullNameEn
    , empManager.FullNameEn         AS EmployeeManager
    , jobTitle.NameEn               AS Jobtitle
    , company.DisplayName           AS CompanyEn                 -- see rewrite note R1
    , CAST(NULL AS VARBINARY(MAX))  AS Companylogo               -- see rewrite note R2: placeholder
    , orgAsg.OrganizationUnitId     AS CompanylogoOrgUnitId       -- see rewrite note R2
    , department.DisplayName        AS DepartmentEn              -- see rewrite note R1

FROM Employees emp

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id
      AND orgAsg.TenantId   = @TenantId
      AND orgAsg.IsDeleted  = 0
      AND orgAsg.StartDate <= GETDATE()
      AND orgAsg.EndDate   >= GETDATE()

LEFT JOIN EmployeeManagers empManagerLink
       ON empManagerLink.EmployeeId = emp.Id
      AND empManagerLink.TenantId   = @TenantId
      AND empManagerLink.IsDeleted  = 0
      AND empManagerLink.StartDate <= GETDATE()
      AND empManagerLink.EndDate   >= GETDATE()

LEFT JOIN Employees empManager
       ON empManager.Id        = empManagerLink.ManagerId
      AND empManager.TenantId  = @TenantId
      AND empManager.IsDeleted = 0

LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id        = orgAsg.OrganizationUnitId
      AND orgUnit.TenantId  = @TenantId
      AND orgUnit.IsDeleted = 0

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAsg.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

-- The seven OrgLevel1..7 'Company' joins, collapsed to one ancestor walk. See rewrite note R1.
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName, ancp.NameAr
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp
            ON ancp.OrganizationUnitId = anc.Id AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct
            ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId  = @TenantId
      AND anc.IsDeleted = 0
      AND orgUnit.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company

-- ... and the seven 'Department' ones.
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName, ancp.NameAr
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp
            ON ancp.OrganizationUnitId = anc.Id AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct
            ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId  = @TenantId
      AND anc.IsDeleted = 0
      AND orgUnit.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Department'
    ORDER BY LEN(anc.Code)
) department

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        // ===========================================================================================
        // rptProbationaryReportForm
        // ===========================================================================================

        /// <summary>
        /// rptProbationaryReportForm :: EmpDiscReportQuery.
        ///
        /// The data-source and query names ("EmpDiscReportDS" / "EmpDiscReportQuery") are a
        /// copy-paste leftover from rptDisciplinaryForm in the legacy report; kept because the
        /// report's DataMember binds the query by that name.
        ///
        /// Straight rename, no semantic rewrite: this query never touched OrganizationUnit.
        /// </summary>
        public const string ProbationaryReportFormQuery = @"-- rptProbationaryReportForm :: EmpDiscReportQuery
SELECT
      emp.Id                AS OID
    , emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.CivilID
    , emp.JoiningDate
    , emp.ProbationDays
    , emp.ProbationEndDate
    , jobTitle.NameEn       AS JobTitleEn
    , jobTitle.NameAr       AS JobTitleAr
    , workLocation.NameEn   AS WorkLocationEn
    , workLocation.NameAr   AS WorkLocationAr

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

LEFT JOIN WorkLocations workLocation
       ON workLocation.Id        = orgAsg.WorkLocationId
      AND workLocation.TenantId  = @TenantId
      AND workLocation.IsDeleted = 0

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        // ===========================================================================================
        // rptPayChange (four queries)
        // ===========================================================================================

        /// <summary>
        /// rptPayChange :: PayrollSheetQuery.
        ///
        /// The one genuinely complex query in this batch: for every basic-pay header overlapping the
        /// requested window it finds the employee's PREVIOUS header, breaks both down into five wage
        /// buckets (basic / transport / housing / incharge / other, by WageTypes.Code), and reports
        /// only rows where the total actually moved.
        ///
        /// REWRITES beyond the mechanical renames:
        ///   * Wage-type Code literals are quoted -- see rewrite note R5. The legacy
        ///     "Code IN (1002,1035)" against HCMv3's nvarchar Code would force an int conversion of
        ///     the whole column and throw on the first non-numeric code in the tenant.
        ///   * The legacy per-bucket "SELECT TOP 1 OID FROM WageType WHERE Code IN (a,b)" is a
        ///     LEGACY BUG faithfully NOT reproduced: TOP 1 over a two-code IN list silently drops one
        ///     of the two wage types (which one depends on storage order), so the Transport and
        ///     Housing buckets under-reported whenever both codes were in use. The bucket subqueries
        ///     below match on the full code list, which is what the surrounding SUM() and the
        ///     "Prev_*" half of the same query already did. *** Behaviour change, deliberate; called
        ///     out under "Defects carried over" in the porting notes. ***
        ///   * REPLACE() is rejected outright by v23.2 CustomSqlQuery (rewrite note R6). The legacy
        ///     Remarks expression was REPLACE(parts + ',', ',,', ''). Each part ends in a comma and
        ///     none contains ',,', so the only match is the doubled comma at the very end; the
        ///     rewrite therefore strips one trailing character, and reproduces the legacy quirk that
        ///     an empty part list yields a bare ',' rather than ''.
        ///   * The previous-header lookup becomes an OUTER APPLY (rewrite note R4).
        ///
        /// Bound query parameters: @PrmStartDate, @PrmEndDate, @PrmPayrollArea, @PrmCostCenter,
        /// @PrmEmpCode, @PrmStatus, @PrmReportType.
        /// </summary>
        public const string PayChangeQuery = @"-- rptPayChange :: PayrollSheetQuery
SELECT
      emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.JoiningDate
    , jobTitle.NameEn                                   AS JobTitle
    , payrollArea.Name                                  AS PayrollArea
    , costCenter.Code                                   AS CostCenterCode
    , costCenter.Code + '-' + costCenter.NameEn         AS CostCenterEn
    , workLocation.Code                                 AS WorkLocationCode
    , workLocation.NameEn                               AS WorkLocationEn
    , basicPay.StartDate                                AS SalaryChangeDate
    , ISNULL(buckets.BasicAmount,     0)                AS BasicPay
    , ISNULL(buckets.TransportAmount, 0)                AS TransportAllowance
    , ISNULL(buckets.HousingAmount,   0)                AS HousingAllowance
    , ISNULL(buckets.InchargeAmount,  0)                AS InchargeAllowance
    , ISNULL(buckets.OtherAmount,     0)                AS OtherAllowance
    , ISNULL(basicPay.Amount,     0)                    AS TotalSalary
    , ISNULL(prevPay.Amount,      0)                    AS LastTotalSalary
    , ISNULL(basicPay.Amount, 0) - ISNULL(prevPay.Amount, 0) AS ChangeInSalary
    , CASE WHEN remarks.Parts = '' THEN ','
           ELSE LEFT(remarks.Parts, LEN(remarks.Parts) - 1)
      END                                               AS Remarks
    , basicPay.StartDate
    , orgAsg.StartDate
    , orgAsg.EndDate

FROM Employees emp

------------------------------ current EmployeeBasicPay ------------------------------
LEFT JOIN EmployeeBasicPays basicPay
       ON basicPay.EmployeeId = emp.Id
      AND basicPay.TenantId   = @TenantId
      AND basicPay.IsDeleted  = 0
      AND ((basicPay.StartDate <= @PrmStartDate AND @PrmStartDate     <= basicPay.EndDate)
        OR (basicPay.StartDate <= @PrmEndDate   AND @PrmEndDate       <= basicPay.EndDate)
        OR (@PrmStartDate <= basicPay.StartDate AND basicPay.StartDate <= @PrmEndDate)
        OR (@PrmStartDate <= basicPay.EndDate   AND basicPay.EndDate   <= @PrmEndDate))

-- Five wage buckets of the CURRENT header, in one pass. See the rewrite note about the legacy
-- ""SELECT TOP 1 OID FROM WageType WHERE Code IN (a,b)"" bug that this deliberately does not carry.
OUTER APPLY (
    SELECT
          SUM(CASE WHEN wt.Code IN ('1000')         THEN ISNULL(ebpd.Amount, 0) ELSE 0 END) AS BasicAmount
        , SUM(CASE WHEN wt.Code IN ('1002', '1035') THEN ISNULL(ebpd.Amount, 0) ELSE 0 END) AS TransportAmount
        , SUM(CASE WHEN wt.Code IN ('1001', '1048') THEN ISNULL(ebpd.Amount, 0) ELSE 0 END) AS HousingAmount
        , SUM(CASE WHEN wt.Code IN ('1026')         THEN ISNULL(ebpd.Amount, 0) ELSE 0 END) AS InchargeAmount
        , SUM(CASE WHEN wt.Code IN ('1018')         THEN ISNULL(ebpd.Amount, 0) ELSE 0 END) AS OtherAmount
    FROM EmployeeBasicPayDetails ebpd
    JOIN WageTypes wt
      ON wt.Id        = ebpd.WageTypeId
     AND wt.TenantId  = @TenantId
     AND wt.IsDeleted = 0
    WHERE ebpd.EmployeeBasicPayId = basicPay.Id
      AND ebpd.TenantId  = @TenantId
      AND ebpd.IsDeleted = 0
) buckets

------------------------------ previous EmployeeBasicPay -----------------------------
-- see rewrite note R4
OUTER APPLY (
    SELECT TOP 1 prev.Id, prev.Amount
    FROM EmployeeBasicPays prev
    WHERE prev.EmployeeId = basicPay.EmployeeId
      AND prev.StartDate  < basicPay.StartDate
      AND prev.TenantId   = @TenantId
      AND prev.IsDeleted  = 0
    ORDER BY prev.StartDate DESC
) prevPay

OUTER APPLY (
    SELECT
          SUM(CASE WHEN wt.Code IN ('1000')         THEN ISNULL(ebpd.Amount, 0) ELSE 0 END) AS BasicAmount
        , SUM(CASE WHEN wt.Code IN ('1002', '1035') THEN ISNULL(ebpd.Amount, 0) ELSE 0 END) AS TransportAmount
        , SUM(CASE WHEN wt.Code IN ('1001', '1048') THEN ISNULL(ebpd.Amount, 0) ELSE 0 END) AS HousingAmount
        , SUM(CASE WHEN wt.Code IN ('1026')         THEN ISNULL(ebpd.Amount, 0) ELSE 0 END) AS InchargeAmount
        , SUM(CASE WHEN wt.Code IN ('1018')         THEN ISNULL(ebpd.Amount, 0) ELSE 0 END) AS OtherAmount
    FROM EmployeeBasicPayDetails ebpd
    JOIN WageTypes wt
      ON wt.Id        = ebpd.WageTypeId
     AND wt.TenantId  = @TenantId
     AND wt.IsDeleted = 0
    WHERE ebpd.EmployeeBasicPayId = prevPay.Id
      AND ebpd.TenantId  = @TenantId
      AND ebpd.IsDeleted = 0
) prevBuckets

-- The Remarks part list, built once so the CASE above can strip its trailing comma without
-- repeating five comparisons. See the REPLACE() rewrite note.
CROSS APPLY (
    SELECT
        CASE WHEN ISNULL(buckets.BasicAmount,     0) - ISNULL(prevBuckets.BasicAmount,     0) <> 0 THEN ' BasicPay,'  ELSE '' END +
        CASE WHEN ISNULL(buckets.TransportAmount, 0) - ISNULL(prevBuckets.TransportAmount, 0) <> 0 THEN ' Transport,' ELSE '' END +
        CASE WHEN ISNULL(buckets.HousingAmount,   0) - ISNULL(prevBuckets.HousingAmount,   0) <> 0 THEN ' Housing,'   ELSE '' END +
        CASE WHEN ISNULL(buckets.InchargeAmount,  0) - ISNULL(prevBuckets.InchargeAmount,  0) <> 0 THEN ' Incharge,'  ELSE '' END +
        CASE WHEN ISNULL(buckets.OtherAmount,     0) - ISNULL(prevBuckets.OtherAmount,     0) <> 0 THEN ' Other,'     ELSE '' END
        AS Parts
) remarks

------------------------------ current org assignment --------------------------------
LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id
      AND orgAsg.TenantId   = @TenantId
      AND orgAsg.IsDeleted  = 0
      AND orgAsg.StartDate <= basicPay.EndDate
      AND orgAsg.EndDate   >= basicPay.EndDate

LEFT JOIN PayrollAreas payrollArea
       ON payrollArea.Id        = orgAsg.PayrollAreaId
      AND payrollArea.TenantId  = @TenantId
      AND payrollArea.IsDeleted = 0

LEFT JOIN CostCenters costCenter
       ON costCenter.Id        = orgAsg.CostCenterId
      AND costCenter.TenantId  = @TenantId
      AND costCenter.IsDeleted = 0

LEFT JOIN WorkLocations workLocation
       ON workLocation.Id        = orgAsg.WorkLocationId
      AND workLocation.TenantId  = @TenantId
      AND workLocation.IsDeleted = 0

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAsg.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0

  AND ((ISNULL(@PrmCostCenter, '') <> '' AND (costCenter.Code + '-' + costCenter.NameEn) = @PrmCostCenter)
    OR  (ISNULL(@PrmCostCenter, '') = ''))

  AND ISNULL(prevPay.Amount, 0) > 0                                              -- last total salary
  AND ISNULL(basicPay.Amount, 0) - ISNULL(prevPay.Amount, 0) <> 0                -- change in salary
  AND emp.Status = CASE WHEN @PrmStatus = -1 THEN emp.Status ELSE @PrmStatus END

  AND ((ISNULL(@PrmPayrollArea, '') <> '' AND payrollArea.Name = @PrmPayrollArea)
    OR  (ISNULL(@PrmPayrollArea, '') = ''))

  AND ((ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode)
    OR  (ISNULL(@PrmEmpCode, '') = ''))

  AND ((@PrmReportType = 2 AND basicPay.StartDate BETWEEN @PrmStartDate AND @PrmEndDate)
    OR (@PrmReportType = 1 AND basicPay.EndDate > GETDATE()))

ORDER BY emp.Id, basicPay.StartDate DESC;";

        /// <summary>
        /// rptPayChange :: dsCompany -- the (unbound) company look-up list.
        /// Legacy filtered OrganizationUnit by OrganizationUnitType.NameEn = 'Company'. The type
        /// moved off the ABP table onto OrgUnitProfiles, so the filter survives intact -- this is the
        /// same list Bank/BankSql.cs's CompanyList produces.
        /// </summary>
        public const string PayChangeCompanyQuery = @"-- rptPayChange :: dsCompany
SELECT orgUnit.DisplayName AS Company
FROM AbpOrganizationUnits orgUnit
INNER JOIN OrgUnitProfiles profile
        ON profile.OrganizationUnitId = orgUnit.Id
       AND profile.TenantId  = @TenantId
       AND profile.IsDeleted = 0
INNER JOIN OrganizationUnitTypes unitType
        ON unitType.Id       = profile.OrganizationUnitTypeId
       AND unitType.TenantId = @TenantId
       AND unitType.IsDeleted = 0
WHERE orgUnit.TenantId  = @TenantId
  AND orgUnit.IsDeleted = 0
  AND unitType.NameEn   = 'Company'
ORDER BY orgUnit.DisplayName;";

        /// <summary>
        /// rptPayChange :: dsCostCenter -- look-up list behind the PrmCostCenter parameter.
        /// Legacy aliases (OID / NameEn / NameAr) preserved so the parameter's
        /// DynamicListLookUpSettings in the designer file needs no edit.
        /// </summary>
        public const string PayChangeCostCenterQuery = @"-- rptPayChange :: dsCostCenter
SELECT
      costCenter.Id                                 AS OID
    , costCenter.Code + '-' + costCenter.NameEn     AS NameEn
    , costCenter.NameAr                             AS NameAr
FROM CostCenters costCenter
WHERE costCenter.TenantId  = @TenantId
  AND costCenter.IsDeleted = 0
ORDER BY costCenter.Code;";

        /// <summary>rptPayChange :: dsPayrollArea -- look-up list behind the PrmPayrollArea parameter.</summary>
        public const string PayChangePayrollAreaQuery = @"-- rptPayChange :: dsPayrollArea
SELECT payrollArea.Name
FROM PayrollAreas payrollArea
WHERE payrollArea.TenantId  = @TenantId
  AND payrollArea.IsDeleted = 0
ORDER BY payrollArea.Name;";

        // ===========================================================================================
        // rptEmploymentContract / rptEmploymentContractAr023 / rptEmploymentContractEn023
        // ===========================================================================================

        /// <summary>
        /// rptEmploymentContract :: SickLeaveFormQuery.
        ///
        /// (Query and data-source names are legacy copy-paste leftovers from rptSickLeaveForm; kept
        /// because DataMember binds them.)
        ///
        /// PassportNum now comes from EmployeeDocuments -- rewrite note R3. The nationality self-join
        /// derived table is collapsed to a direct join, as on rptFinalClearance_EOS.
        /// </summary>
        public const string EmploymentContractQuery = @"-- rptEmploymentContract :: SickLeaveFormQuery
SELECT DISTINCT
      emp.FullNameEn
    , emp.FullNameAr
    , emp.JoiningDate
    , passport.DocumentNum          AS PassportNum               -- see rewrite note R3
    , emp.ProbationEndDate
    , emp.CivilID
    , jobTitle.NameEn               AS Jobtitle
    , jobTitle.NameAr               AS JobtitleAr
    , nationality.NameEn            AS Nationality
    , nationality.NameAr            AS NationalityAr
    , company.DisplayName           AS CompanyEn                 -- see rewrite note R1
    , CAST(NULL AS VARBINARY(MAX))  AS Companylogo               -- see rewrite note R2: placeholder
    , orgAsg.OrganizationUnitId     AS CompanylogoOrgUnitId       -- see rewrite note R2

FROM Employees emp

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id
      AND orgAsg.TenantId   = @TenantId
      AND orgAsg.IsDeleted  = 0
      AND orgAsg.StartDate <= @PrmFormDate
      AND orgAsg.EndDate   >= @PrmFormDate

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAsg.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

LEFT JOIN Nationalities nationality
       ON nationality.Id        = emp.NationalityId
      AND nationality.TenantId  = @TenantId
      AND nationality.IsDeleted = 0

LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id        = orgAsg.OrganizationUnitId
      AND orgUnit.TenantId  = @TenantId
      AND orgUnit.IsDeleted = 0

-- The seven OrgLevel1..7 'Company' joins, collapsed to one ancestor walk. See rewrite note R1.
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName, ancp.NameAr
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp
            ON ancp.OrganizationUnitId = anc.Id AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct
            ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId  = @TenantId
      AND anc.IsDeleted = 0
      AND orgUnit.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company

-- see rewrite note R3. DocumentTypes has no TenantId-free 'Passport' constant, so the type row is
-- matched by name inside the tenant, exactly as GovtFormsSqlSecondHalf does.
OUTER APPLY (
    SELECT TOP 1 empDoc.DocumentNum
    FROM EmployeeDocuments empDoc
    JOIN DocumentTypes docType
      ON docType.Id        = empDoc.DocumentTypeId
     AND docType.TenantId  = @TenantId
     AND docType.IsDeleted = 0
     AND docType.NameEn    = 'Passport'
    WHERE empDoc.EmployeeId = emp.Id
      AND empDoc.TenantId   = @TenantId
      AND empDoc.IsDeleted  = 0
    ORDER BY empDoc.ExpiryDate DESC
) passport

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        /// <summary>
        /// rptEmploymentContract / rptEmploymentContractAr023 / rptEmploymentContractEn023 ::
        /// EmployeeWageType.
        ///
        /// Byte-identical in all three legacy .resx files, so it is held once here.
        ///
        /// v23.2 REWRITE (note R6): the legacy text opened with <c>;With CTE1 As(</c>. CustomSqlQuery
        /// rejects SQL starting with WITH outright, so the CTE becomes a derived table. Nothing else
        /// changes -- the wage buckets are still matched by WageTypes.NameEn LIKE patterns, which is
        /// how the legacy query did it (unlike rptPayChange, which matches on Code).
        ///
        /// LEGACY QUIRK KEPT: the bucket patterns are LIKE, not equality, and a wage type whose name
        /// matches none of them contributes to no bucket while still forcing a group row. Also kept:
        /// the grouping includes the currency short form, so an employee whose basic-pay lines are in
        /// two currencies yields two rows.
        /// </summary>
        public const string EmploymentContractWageQuery = @"-- rptEmploymentContract* :: EmployeeWageType
SELECT DISTINCT *
FROM (
    SELECT
          emp.Code                          AS EmployeeCode
        , emp.Id                            AS oid
        , ebp.StartDate
        , ebp.EndDate
        , SUM(CASE WHEN wt.NameEn LIKE 'Basic%Salary'                 THEN ebpd.Amount ELSE 0 END) AS BasicAllowance
        , SUM(CASE WHEN wt.NameEn LIKE 'Housing%Allowance'            THEN ebpd.Amount ELSE 0 END) AS HousingAllowance
        , SUM(CASE WHEN wt.NameEn LIKE 'Transport%Allowance'          THEN ebpd.Amount ELSE 0 END) AS TransportAllowance
        , SUM(CASE WHEN wt.NameEn LIKE 'Shift%Allowance'              THEN ebpd.Amount ELSE 0 END) AS ShiftAllowance
        , SUM(CASE WHEN wt.NameEn LIKE 'Additional%Duties%Allowance'  THEN ebpd.Amount ELSE 0 END) AS AdditionalDutiesAllowance
        , cur.CurrencyShortFormEn           AS Currency
    FROM WageTypes wt
    INNER JOIN EmployeeBasicPayDetails ebpd
            ON ebpd.WageTypeId = wt.Id
           AND ebpd.TenantId   = @TenantId
           AND ebpd.IsDeleted  = 0
    INNER JOIN EmployeeBasicPays ebp
            ON ebp.Id        = ebpd.EmployeeBasicPayId
           AND ebp.TenantId  = @TenantId
           AND ebp.IsDeleted = 0
    INNER JOIN Employees emp
            ON emp.Id        = ebp.EmployeeId
           AND emp.TenantId  = @TenantId
           AND emp.IsDeleted = 0
    LEFT JOIN Currencies cur
           ON cur.Id        = ebpd.CurrencyId
          AND cur.TenantId  = @TenantId
          AND cur.IsDeleted = 0
    WHERE wt.TenantId  = @TenantId
      AND wt.IsDeleted = 0
    GROUP BY emp.Code, emp.Id, ebp.StartDate, ebp.EndDate, cur.CurrencyShortFormEn
) CTE1
WHERE CTE1.EmployeeCode = @PrmEmpCode
  AND @PrmFormDate BETWEEN CTE1.StartDate AND CTE1.EndDate;";

        /// <summary>
        /// rptEmploymentContractAr023 :: SickLeaveFormQuery.
        ///
        /// Same shape as <see cref="EmploymentContractQuery"/> plus the employee's Title (Mr/Ms/Dr)
        /// in Arabic and a CompanyAr column. CompanyAr can only be the same DisplayName as CompanyEn
        /// (rewrite note R1) -- AbpOrganizationUnits has no Arabic name.
        ///
        /// ONE COLUMN ADDED over the legacy list: <c>Current_Salary</c>, replacing XAF code-behind.
        /// The legacy <c>BeforePrint</c> opened an XPObjectSpaceProvider and read
        /// <c>emp.BasicPay.First(StartDate &lt;= PrmFormDate &lt;= EndDate).BasicPayDetail.First(WageType.Code == "1000").Amount</c>
        /// to feed AmountInArabicWords. A DevExpress-constructed report has no DI container, so the
        /// amount is projected here instead and the sentence is composed in Detail_BeforePrint. The
        /// OUTER APPLY below is that same lookup, keyed on the same @PrmFormDate. Named
        /// <c>Current_Salary</c> to match the column the English 023 contract already uses.
        /// </summary>
        public const string EmploymentContractAr023Query = @"-- rptEmploymentContractAr023 :: SickLeaveFormQuery
SELECT DISTINCT
      title.NameAr                  AS [Title]
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.JoiningDate
    , passport.DocumentNum          AS PassportNum               -- see rewrite note R3
    , emp.ProbationEndDate
    , emp.CivilID
    , jobTitle.NameEn               AS Jobtitle
    , jobTitle.NameAr               AS JobtitleAr
    , nationality.NameEn            AS Nationality
    , nationality.NameAr            AS NationalityAr
    , company.DisplayName           AS CompanyEn                 -- see rewrite note R1
    , company.NameAr                AS CompanyAr                 -- see rewrite note R1
    , CAST(NULL AS VARBINARY(MAX))  AS Companylogo               -- see rewrite note R2: FEATURE LOST
    , basicSalary.Amount            AS Current_Salary            -- replaces XAF code-behind, see summary

FROM Employees emp

LEFT JOIN Titles title
       ON title.Id        = emp.TitleId
      AND title.TenantId  = @TenantId
      AND title.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id
      AND orgAsg.TenantId   = @TenantId
      AND orgAsg.IsDeleted  = 0
      AND orgAsg.StartDate <= @PrmFormDate
      AND orgAsg.EndDate   >= @PrmFormDate

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAsg.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

LEFT JOIN Nationalities nationality
       ON nationality.Id        = emp.NationalityId
      AND nationality.TenantId  = @TenantId
      AND nationality.IsDeleted = 0

LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id        = orgAsg.OrganizationUnitId
      AND orgUnit.TenantId  = @TenantId
      AND orgUnit.IsDeleted = 0

-- The seven OrgLevel1..7 'Company' joins, collapsed to one ancestor walk. See rewrite note R1.
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName, ancp.NameAr
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp
            ON ancp.OrganizationUnitId = anc.Id AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct
            ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId  = @TenantId
      AND anc.IsDeleted = 0
      AND orgUnit.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company

OUTER APPLY (
    SELECT TOP 1 empDoc.DocumentNum
    FROM EmployeeDocuments empDoc
    JOIN DocumentTypes docType
      ON docType.Id        = empDoc.DocumentTypeId
     AND docType.TenantId  = @TenantId
     AND docType.IsDeleted = 0
     AND docType.NameEn    = 'Passport'
    WHERE empDoc.EmployeeId = emp.Id
      AND empDoc.TenantId   = @TenantId
      AND empDoc.IsDeleted  = 0
    ORDER BY empDoc.ExpiryDate DESC
) passport

-- Wage-type '1000' (Basic Salary) line of the basic-pay header in force on @PrmFormDate -- the same
-- two-step lookup the legacy BeforePrint did through XPO. Code is quoted, see rewrite note R5.
OUTER APPLY (
    SELECT TOP 1 ebpd.Amount
    FROM EmployeeBasicPays ebp
    JOIN EmployeeBasicPayDetails ebpd
      ON ebpd.EmployeeBasicPayId = ebp.Id
     AND ebpd.TenantId  = @TenantId
     AND ebpd.IsDeleted = 0
    JOIN WageTypes wt
      ON wt.Id        = ebpd.WageTypeId
     AND wt.TenantId  = @TenantId
     AND wt.IsDeleted = 0
     AND wt.Code      = '1000'
    WHERE ebp.EmployeeId = emp.Id
      AND ebp.TenantId   = @TenantId
      AND ebp.IsDeleted  = 0
      AND ebp.StartDate <= @PrmFormDate
      AND ebp.EndDate   >= @PrmFormDate
    ORDER BY ebp.StartDate DESC
) basicSalary

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";

        /// <summary>
        /// rptEmploymentContractEn023 :: SickLeaveFormQuery.
        ///
        /// The English 023 contract differs from its Arabic twin in the legacy tree, not by port
        /// artefact: it formats JoiningDate as a long English date IN SQL, reads the employee's
        /// sponsor contract salary, and reads the wage-type '1000' amount as Current_Salary.
        ///
        /// DROPPED JOINS. The legacy query LEFT JOINed Sponsor -> SponsorSignatory -> Signatory and
        /// selected NOTHING from any of the three. Because SponsorSignatory is one-to-many, those
        /// joins multiplied the master row by the number of authorised signatories -- the same
        /// fan-out defect GovtFormsSqlFirstHalf rewrite note 6 records for the government forms,
        /// except that here not one column of the fanned-out tables is projected. They are dropped.
        /// *** Behaviour change, deliberate: it removes duplicate copies of the contract, nothing
        /// else. Called out under "Defects carried over" in the porting notes. ***
        ///
        /// The legacy employee filter is the tri-state
        /// "(@PrmEmpCode &lt;&gt; '' AND Code = @PrmEmpCode) OR @PrmEmpCode = ''", i.e. a blank code
        /// prints a contract for EVERY employee. Kept verbatim.
        /// </summary>
        public const string EmploymentContractEn023Query = @"-- rptEmploymentContractEn023 :: SickLeaveFormQuery
SELECT
      emp.Code                                                              AS EmpCode
    , emp.FullNameEn
    , emp.FullNameAr
    , FORMAT(emp.JoiningDate, 'dddd, MMMM dd, yyyy', 'en-US')               AS JoiningDate
    , emp.CivilID
    , title.NameAr                                                          AS TitleAr
    , title.NameEn                                                          AS TitleEn
    , company.DisplayName                                                   AS CompanyEn    -- rewrite note R1
    , company.NameAr                                                        AS CompanyAr    -- rewrite note R1
    , nationality.NameEn                                                    AS NationalityEn
    , nationality.NameAr                                                    AS NationalityAr
    , basicSalary.Amount                                                    AS Current_Salary
    , empSponsor.ContractSalary
    , jobTitle.NameEn                                                       AS JobTitle

FROM Employees emp

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id
      AND orgAsg.TenantId   = @TenantId
      AND orgAsg.IsDeleted  = 0
      AND orgAsg.StartDate <= GETDATE()
      AND orgAsg.EndDate   >= GETDATE()

LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.EmployeeId = emp.Id
      AND empSponsor.TenantId   = @TenantId
      AND empSponsor.IsDeleted  = 0
      AND empSponsor.StartDate <= GETDATE()
      AND empSponsor.EndDate   >= GETDATE()

LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id        = orgAsg.OrganizationUnitId
      AND orgUnit.TenantId  = @TenantId
      AND orgUnit.IsDeleted = 0

-- The seven OrgLevel1..7 'Company' joins, collapsed to one ancestor walk. See rewrite note R1.
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName, ancp.NameAr
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp
            ON ancp.OrganizationUnitId = anc.Id AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct
            ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId  = @TenantId
      AND anc.IsDeleted = 0
      AND orgUnit.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company

LEFT JOIN Titles title
       ON title.Id        = emp.TitleId
      AND title.TenantId  = @TenantId
      AND title.IsDeleted = 0

LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id        = orgAsg.JobTitleId
      AND jobTitle.TenantId  = @TenantId
      AND jobTitle.IsDeleted = 0

LEFT JOIN Nationalities nationality
       ON nationality.Id        = emp.NationalityId
      AND nationality.TenantId  = @TenantId
      AND nationality.IsDeleted = 0

-- Wage-type '1000' (Basic Salary) line of the basic-pay header current today. Code is quoted --
-- see rewrite note R5.
OUTER APPLY (
    SELECT TOP 1 ebpd.Amount
    FROM EmployeeBasicPays ebp
    JOIN EmployeeBasicPayDetails ebpd
      ON ebpd.EmployeeBasicPayId = ebp.Id
     AND ebpd.TenantId  = @TenantId
     AND ebpd.IsDeleted = 0
    JOIN WageTypes wt
      ON wt.Id        = ebpd.WageTypeId
     AND wt.TenantId  = @TenantId
     AND wt.IsDeleted = 0
     AND wt.Code      = '1000'
    WHERE ebp.EmployeeId = emp.Id
      AND ebp.TenantId   = @TenantId
      AND ebp.IsDeleted  = 0
      AND ebp.StartDate <= GETDATE()
      AND ebp.EndDate   >= GETDATE()
    ORDER BY ebp.StartDate DESC
) basicSalary

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND ((ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode)
    OR  (ISNULL(@PrmEmpCode, '') = ''));";
    }
}
