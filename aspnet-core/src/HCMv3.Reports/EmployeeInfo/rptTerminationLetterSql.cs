namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>Raw SQL used by rptTerminationLetter (data source <c>EmpDiscReportDS</c>, query <c>EmpDetail</c>).</summary>
    internal static class rptTerminationLetterSql
    {
        /// <summary>
        /// Legacy query, verbatim:
        /// <code>select Code, FullNameEn, FullNameAr, GETDATE() PrintDate from Employee where Code = @PrmEmpCode</code>
        /// A purely MECHANICAL rewrite -- Employee -> Employees -- plus the two ABP predicates the
        /// legacy corpus has no concept of (TenantId, IsDeleted).
        /// </summary>
        internal const string Query1 = @"-- rptTerminationLetter :: EmpDetail
SELECT
      emp.Code
    , emp.FullNameEn
    , emp.FullNameAr
    , GETDATE() AS PrintDate
FROM Employees emp
WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code      = @PrmEmpCode;";
    }
}
