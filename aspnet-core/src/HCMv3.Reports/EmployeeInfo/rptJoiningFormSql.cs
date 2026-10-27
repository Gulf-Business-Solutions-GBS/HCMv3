namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// Raw SQL used by rptJoiningForm (data source <c>EmpDiscReportDS</c>, query
    /// <c>SalaryCertificate_LNDQuery</c> -- legacy name kept, the layout binds to it).
    /// </summary>
    internal static class rptJoiningFormSql
    {
        /// <summary>
        /// HCMv3 rewrite. Mechanical renames plus TenantId/IsDeleted everywhere.
        ///
        /// NON-MECHANICAL:
        ///   1. Seven Division OrgLevel joins -> one ancestor walk (EmployeeInfoSqlFragments).
        ///   2. *** KNOWN HCMv3 DATA GAP, NOT A PORTING DEFECT. *** EmployeeBanks and
        ///      EmployeeSponsors are EMPTY in the HCMv3 database (0 rows), so BankEn/BankAr/
        ///      AccountNum/GovtJobTitleEn/GovtJobTitleAr legitimately render blank. All four joins
        ///      are LEFT, so the employee row still renders.
        ///   3. EmployeeSponsor was joined but only GovtJobTitle was projected from it; the join
        ///      is kept for exactly that.
        /// </summary>
        internal const string Query1 = @"-- rptJoiningForm :: SalaryCertificate_LNDQuery
SELECT
      emp.Id                AS OID          -- legacy column name kept; the layout binds ""OID""
    , emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.CivilID
    , emp.JoiningDate
    , nationality.NameEn    AS NationalityEn
    , nationality.NameAr    AS NationalityAr
    , jobTitle.NameEn       AS JobTitleEn
    , jobTitle.NameAr       AS JobTitleAr
    , govtJobTitle.NameEn   AS GovtJobTitleEn
    , govtJobTitle.NameAr   AS GovtJobTitleAr
    , basicPay.Amount
    , empBank.AccountNum
    , bank.NameEn           AS BankEn
    , bank.NameAr           AS BankAr
    , COALESCE(division.NameEn, ownOrgUnit.DisplayName)      AS DivisionEn
    , COALESCE(division.NameAr, ownOrgUnitProfile.NameAr)    AS DivisionAr

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

LEFT JOIN EmployeeSponsors empSponsor              -- see rewrite note 2: table is empty in HCMv3
       ON empSponsor.EmployeeId = emp.Id
      AND empSponsor.TenantId   = @TenantId
      AND empSponsor.IsDeleted  = 0
      AND empSponsor.StartDate <= @PrmDate
      AND empSponsor.EndDate   >= @PrmDate

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

LEFT JOIN EmployeeBanks empBank                    -- see rewrite note 2: table is empty in HCMv3
       ON empBank.EmployeeId = emp.Id
      AND empBank.TenantId   = @TenantId
      AND empBank.IsDeleted  = 0
      AND empBank.StartDate <= @PrmDate
      AND empBank.EndDate   >= @PrmDate

LEFT JOIN Banks bank
       ON bank.Id        = empBank.BankId
      AND bank.TenantId  = @TenantId
      AND bank.IsDeleted = 0
" + EmployeeInfoSqlFragments.OwnOrgUnit
  + EmployeeInfoSqlFragments.DivisionAncestor + @"
WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";
    }
}
