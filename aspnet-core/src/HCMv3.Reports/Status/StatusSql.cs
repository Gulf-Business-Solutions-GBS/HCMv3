// SQL for the HCMv3.Reports/Status family, rewritten for the HCMv3 schema.
//
// MULTI-TENANCY: raw SQL through DevExpress SqlDataSource bypasses EF Core's global tenant filter.
// Every query filters TenantId explicitly via @TenantId (the hidden PrmTenantId report parameter).
//
// SHARED REWRITES: table pluralisation, OID -> Id, <Entity> -> <Entity>Id, "AND TenantId = @TenantId
// AND IsDeleted = 0" on every join; the OrgLevel1..7 'Company' joins collapsed into one ancestor walk
// over AbpOrganizationUnits with the unit type read from OrgUnitProfiles (full note in BankSql.cs);
// CompanyLogo kept as a NULL placeholder: HCMv3 keeps logos in the file store, and HireQuery (the only
// query whose layout prints the logo; the departure subreport has no logo box, in legacy too) returns
// the employee unit as CompanylogoOrgUnitId, resolved through ICompanyLogoResolver; the Arabic company
// name taken from OrgUnitProfiles.NameAr because AbpOrganizationUnits carries only DisplayName.
//
// PERIOD ARITHMETIC: legacy wrote the last day of the month as
//     DATEADD(day,-1,DATEADD(month,@PrmMonth,DATEADD(year,@PrmYear-1900,0)))
// Written here as EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1)) -- identical, and readable.
// Note this report (unlike the Bank family) uses the PLAIN "in force on the last day" form; the
// two-disjunct variant is commented out in the legacy source.

namespace HCMv3.Reports.Status
{
    /// <summary>Raw SQL used by the HCMv3.Reports.Status reports.</summary>
    public static class StatusSql
    {
        /// <summary>Payroll-area combo, used by both the master report and the subreport.</summary>
        public const string PayrollAreaList = @"-- Legacy inline designer SQL: select ""PayrollArea"".""Code"", ""PayrollArea"".""Name"" from ""dbo"".""PayrollArea""
SELECT p.Code
     , p.Name
FROM PayrollAreas p
WHERE p.TenantId = @TenantId
  AND p.IsDeleted = 0
ORDER BY p.Name;";

        // -------------------------------------------------------------------------------------------
        // rptHireDeparture (master) -- employees HIRED in the selected month.
        // -------------------------------------------------------------------------------------------
        public const string HireQuery = @"-- rptHireDeparture :: master query. Employees whose JoiningDate falls in @PrmMonth/@PrmYear.
-- The DEPARTURES half of the same report lives in the srptHireDeparture subreport (SeparationQuery).
SELECT ROW_NUMBER() OVER (ORDER BY emp.Code) AS SrNo
     , emp.Code
     , emp.FullNameEn
     , emp.FullNameAr
     , nationality.NameEn               AS NationalityEn
     , nationality.NameAr               AS NationalitAr   -- legacy typo in the alias; kept, the layout binds to it
     , emp.JoiningDate
     , payrollArea.Name                 AS PayrollArea
     , jobTitle.NameEn                  AS JobTitleEn
     , jobTitle.NameAr                  AS JobTitleAr
     , workLocation.NameEn              AS WorkLocationEn
     , workLocation.NameAr              AS WorkLocationAr
     , visaInfo.OldCompany              AS ComingFrom
     , basicPay.Amount                  AS Salary
     , CASE WHEN emp.JoiningDate IS NOT NULL
            THEN 'Monthly Hire : ' + DATENAME(Month, emp.JoiningDate) + '-' + CONVERT(VARCHAR(10), @PrmYear)
            ELSE '' END                 AS HeaderText
     , country.NameEn                   AS CountryEn
     , country.NameAr                   AS CountryAr
     , company.DisplayName              AS CompanyEn
     , companyProfile.NameAr            AS CompanyAr
     , CAST(NULL AS VARBINARY(MAX))     AS Companylogo     -- placeholder, see header
     , orgAsg.OrganizationUnitId        AS CompanylogoOrgUnitId
FROM Employees emp

LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
      AND orgAsg.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND orgAsg.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
LEFT JOIN PayrollAreas payrollArea
       ON payrollArea.Id = orgAsg.PayrollAreaId AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0
LEFT JOIN Countries country
       ON country.Id = payrollArea.CountryId AND country.TenantId = @TenantId AND country.IsDeleted = 0
LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id = orgAsg.JobTitleId AND jobTitle.TenantId = @TenantId AND jobTitle.IsDeleted = 0
LEFT JOIN WorkLocations workLocation
       ON workLocation.Id = orgAsg.WorkLocationId AND workLocation.TenantId = @TenantId AND workLocation.IsDeleted = 0

LEFT JOIN EmployeeBasicPays basicPay
       ON basicPay.EmployeeId = emp.Id AND basicPay.TenantId = @TenantId AND basicPay.IsDeleted = 0
      AND basicPay.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND basicPay.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

LEFT JOIN EmployeeVisaInfos visaInfo
       ON visaInfo.EmployeeId = emp.Id AND visaInfo.TenantId = @TenantId AND visaInfo.IsDeleted = 0
      AND visaInfo.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND visaInfo.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))

LEFT JOIN AbpOrganizationUnits ou
       ON ou.Id = orgAsg.OrganizationUnitId AND ou.TenantId = @TenantId AND ou.IsDeleted = 0
OUTER APPLY (
    SELECT TOP 1 anc.Id, anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
      AND ou.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company
LEFT JOIN OrgUnitProfiles companyProfile
       ON companyProfile.OrganizationUnitId = company.Id AND companyProfile.TenantId = @TenantId AND companyProfile.IsDeleted = 0

WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND DATEPART(MONTH, emp.JoiningDate) = @PrmMonth
  AND DATEPART(YEAR,  emp.JoiningDate) = @PrmYear
  AND ((ISNULL(@PrmPayrollArea, '') <> '' AND payrollArea.Name = @PrmPayrollArea) OR ISNULL(@PrmPayrollArea, '') = '');";

        // -------------------------------------------------------------------------------------------
        // srptHireDeparture (subreport) -- employees who LEFT in the selected month.
        // -------------------------------------------------------------------------------------------
        public const string SeparationQuery = @"-- srptHireDeparture :: subreport query. The mirror of HireQuery, keyed on
-- EmployeeSeparations.LastDayInService instead of Employees.JoiningDate.
--
-- Note the EmployeeSeparations join is an INNER JOIN (the hire query has no such table), so an
-- employee with no separation record simply cannot appear. That is the intent, not an oversight.
--
-- KNOWN HCMv3 DATA GAP: EmployeeSeparations is EMPTY, so this subreport renders NO ROWS. That is the
-- data migration, not the SQL.
SELECT ROW_NUMBER() OVER (ORDER BY emp.Code) AS SrNo
     , emp.Code
     , emp.FullNameEn
     , emp.FullNameAr
     , nationality.NameEn               AS NationalityEn
     , nationality.NameAr               AS NationalitAr
     , emp.JoiningDate
     , payrollArea.Name                 AS PayrollArea
     , jobTitle.NameEn                  AS JobTitleEn
     , jobTitle.NameAr                  AS JobTitleAr
     , workLocation.NameEn              AS WorkLocationEn
     , workLocation.NameAr              AS WorkLocationAr
     , separationReason.NameEn          AS SeparationReason
     , separation.LastDayInService
     , CASE WHEN separation.LastDayInService IS NOT NULL
            THEN 'Monthly Departure : ' + DATENAME(Month, separation.LastDayInService) + '-' + CONVERT(VARCHAR(10), @PrmYear)
            ELSE '' END                 AS HeaderText
     , country.NameEn                   AS CountryEn
     , country.NameAr                   AS CountryAr
     , company.DisplayName              AS CompanyEn
     , companyProfile.NameAr            AS CompanyAr
     , CAST(NULL AS VARBINARY(MAX))     AS Companylogo
FROM Employees emp

LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments orgAsg
       ON orgAsg.EmployeeId = emp.Id AND orgAsg.TenantId = @TenantId AND orgAsg.IsDeleted = 0
      AND orgAsg.StartDate <= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
      AND orgAsg.EndDate   >= EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1))
LEFT JOIN PayrollAreas payrollArea
       ON payrollArea.Id = orgAsg.PayrollAreaId AND payrollArea.TenantId = @TenantId AND payrollArea.IsDeleted = 0
LEFT JOIN Countries country
       ON country.Id = payrollArea.CountryId AND country.TenantId = @TenantId AND country.IsDeleted = 0
LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id = orgAsg.JobTitleId AND jobTitle.TenantId = @TenantId AND jobTitle.IsDeleted = 0
LEFT JOIN WorkLocations workLocation
       ON workLocation.Id = orgAsg.WorkLocationId AND workLocation.TenantId = @TenantId AND workLocation.IsDeleted = 0

INNER JOIN EmployeeSeparations separation
        ON separation.EmployeeId = emp.Id AND separation.TenantId = @TenantId AND separation.IsDeleted = 0
LEFT JOIN SeparationReasons separationReason
       ON separationReason.Id = separation.SeparationReasonId AND separationReason.TenantId = @TenantId AND separationReason.IsDeleted = 0

LEFT JOIN AbpOrganizationUnits ou
       ON ou.Id = orgAsg.OrganizationUnitId AND ou.TenantId = @TenantId AND ou.IsDeleted = 0
OUTER APPLY (
    SELECT TOP 1 anc.Id, anc.DisplayName
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp       ON ancp.OrganizationUnitId = anc.Id      AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    INNER JOIN OrganizationUnitTypes anct ON anct.Id = ancp.OrganizationUnitTypeId AND anct.TenantId = @TenantId AND anct.IsDeleted = 0
    WHERE anc.TenantId = @TenantId AND anc.IsDeleted = 0
      AND ou.Code LIKE anc.Code + '%'
      AND anct.NameEn = 'Company'
    ORDER BY LEN(anc.Code)
) company
LEFT JOIN OrgUnitProfiles companyProfile
       ON companyProfile.OrganizationUnitId = company.Id AND companyProfile.TenantId = @TenantId AND companyProfile.IsDeleted = 0

WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND DATEPART(MONTH, separation.LastDayInService) = @PrmMonth
  AND DATEPART(YEAR,  separation.LastDayInService) = @PrmYear
  AND ((ISNULL(@PrmPayrollArea, '') <> '' AND payrollArea.Name = @PrmPayrollArea) OR ISNULL(@PrmPayrollArea, '') = '');";
    }
}
