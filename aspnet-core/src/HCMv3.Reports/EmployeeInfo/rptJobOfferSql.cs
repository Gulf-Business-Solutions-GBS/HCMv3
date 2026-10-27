namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// Raw SQL used by rptJobOffer (data source <c>EmpJobOfferDS</c>, query <c>Job_Offer_Query</c>).
    /// rptProbation_Letter carries a byte-identical query; see <see cref="rptProbation_LetterSql"/>,
    /// which reuses this constant rather than duplicating it.
    /// </summary>
    internal static class rptJobOfferSql
    {
        /// <summary>
        /// HCMv3 rewrite. Two employees are projected: <c>emp</c> is the offer recipient
        /// (@PrmEmpCode) and <c>signatory</c> is the HR person signing it (@PrmUser).
        ///
        /// MECHANICAL RENAMES as elsewhere, plus TenantId/IsDeleted on every join.
        ///
        /// NON-MECHANICAL:
        ///   1. Seven Company OrgLevel joins -> one ancestor walk (EmployeeInfoSqlFragments).
        ///   2. *** LEGACY BUG FIXED. *** All seven legacy Company_1..Company_7 joins compare
        ///      against OrgLevel1 -- Company_2 does NOT use OrgLevel2, and so on. Copy-paste
        ///      damage that made six of the seven joins dead weight. The ancestor walk has no
        ///      such fault.
        ///   3. *** TYPE TRAP. *** The layout declares PrmUser as typeof(int) while the SQL
        ///      compares it to Employee.Code. Legacy Employee.Code was compared with an int
        ///      parameter and SQL Server implicitly converted; HCMv3 Employees.Code is nvarchar,
        ///      so the same comparison would convert the whole COLUMN to int and fail on the
        ///      first non-numeric employee code in the table. @PrmUser is CAST to nvarchar here
        ///      instead, which is also sargable.
        ///   4. Legacy joined EmployeeOrgAssignment with no date predicate at all, so an employee
        ///      with several historical assignments produced several offer letters. A validity
        ///      predicate on @PrmDate is added -- the report already takes that parameter.
        ///   5. The legacy WHERE repeated "emp2.Code = @PrmUser", which the join already states.
        ///      Dropped.
        /// </summary>
        internal const string Query1 = @"-- rptJobOffer / rptProbation_Letter :: offer + signatory
SELECT
      emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.PermanentAddress
    , COALESCE(company.NameEn, ownOrgUnit.DisplayName)      AS CompanyEn
    , COALESCE(company.NameAr, ownOrgUnitProfile.NameAr)    AS CompanyAr
    , signatory.Code        AS UserCode
    , signatory.FullNameEn  AS UserFullNameEn
    , signatory.FullNameAr  AS UserFullNameAr
    , title.NameEn          AS TitleEn
    , title.NameAr          AS TitleAr
    , signatory.Mobile      AS Mobile
    , signatory.Email

FROM Employees emp

LEFT JOIN EmployeeOrgAssignments orgAssign
       ON orgAssign.EmployeeId = emp.Id
      AND orgAssign.TenantId   = @TenantId
      AND orgAssign.IsDeleted  = 0
      AND orgAssign.StartDate <= @PrmDate
      AND orgAssign.EndDate   >= @PrmDate

INNER JOIN Employees signatory
        ON signatory.TenantId  = @TenantId
       AND signatory.IsDeleted = 0
       AND signatory.Code      = CAST(@PrmUser AS NVARCHAR(50))   -- see rewrite note 3

INNER JOIN Titles title
        ON title.Id        = signatory.TitleId
       AND title.TenantId  = @TenantId
       AND title.IsDeleted = 0
" + EmployeeInfoSqlFragments.OwnOrgUnit
  + EmployeeInfoSqlFragments.CompanyAncestor + @"
WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";
    }

    /// <summary>
    /// Raw SQL used by rptProbation_Letter (data source <c>EmpProbationLetterDS</c>, query
    /// <c>Probation_Letter_Query</c>). The legacy query is byte-identical to rptJobOffer's, so
    /// this is an alias rather than a copy -- fixing one fixes both, which is the correct outcome
    /// given the two legacy files were plainly a copy-paste pair.
    /// </summary>
    internal static class rptProbation_LetterSql
    {
        internal const string Query1 = rptJobOfferSql.Query1;
    }
}
