-- ===================================================================================================
-- rptPayslipEn :: PayslipOrgV2Query  (MASTER query -- one row per employee/period)
-- HCMv3 rewrite of the legacy DevExpress CustomSqlQuery.
--
-- MULTI-TENANCY / SOFT DELETE
--   The legacy corpus contains ZERO references to TenantId or IsDeleted -- HCM10 was single-tenant
--   and hard-deleted. Every table touched below is an ABP FullAuditedEntity with both columns, so
--   @TenantId and "IsDeleted = 0" are ADDED to every single join. Omitting either silently leaks
--   rows across tenants / resurrects deleted records, and nothing in the report would show it.
--
-- MECHANICAL RENAMES applied throughout
--   Employee->Employees, SalaryRegister->SalaryRegisters, ...        (table pluralisation)
--   OID -> Id                                                        (PK)
--   <EntityName> -> <EntityName>Id                                   (FK columns)
--
-- NON-MECHANICAL REWRITES -- see PORTING-NOTES.md for the full list. Summary:
--   1. OrgLevel1..7 + OrganizationUnitType. Legacy EmployeeOrgAssignment carried seven flat FK
--      columns and picked the one whose OrganizationUnit had type 'Department' (or 'Company').
--      HCMv3 has a SINGLE OrganizationUnitId into AbpOrganizationUnits, which is an ABP
--      materialised-path tree (Code/ParentId) with NO unit-type concept at all. The 14 aliased
--      joins collapse to one; "which ancestor is the Department" is not expressible and the
--      employee's own unit is used instead.  *** SEMANTIC DEVIATION -- needs a product decision. ***
--   2. CompanyLogo. Sourced from OrganizationUnit.logo. AbpOrganizationUnits has no Logo column and
--      HCMv3 has no Company entity carrying one. Returned as NULL.  *** FEATURE LOST. ***
--   3. PayrollArea join. Legacy joined on a STRING: PayrollArea.Code = EmployeeOrgAssignment.PayrollArea.
--      HCMv3 models it properly as EmployeeOrgAssignments.PayrollAreaId -> PayrollAreas.Id.
--      The @PrmPayrollArea parameter still filters on PayrollAreas.Name for operator parity.
--   4. EmployeeBasicPayDetail.WageType = 1 -- a RAW LEGACY PRIMARY KEY, not a code. HRMS_RCF
--      WageType OID 1 = Code '1000' (Basic Salary). HCMv3 has an unrelated wage type whose *Code*
--      is '1' (Employee PIFSS Ded), so translating the literal 1 mechanically returns a deduction
--      instead of basic pay. Rewritten as WageTypes.Code = '1000'.
--   5. EmployeeVisaInfo / EmployeeGratuity joins dropped: no column from either is projected, and
--      as LEFT JOINs to 1:N tables they could only fan the master row out. Legacy bug, not ported.
-- ===================================================================================================

DECLARE @PaySlipDate DATE = EOMONTH(DATEFROMPARTS(@PrmYear, @PrmMonth, 1));

SELECT
      emp.Code                                  AS [Employee Code]
    , emp.Status
    , emp.CivilID
    , emp.Mobile
    , bank.NameEn                               AS [Bank Name]
    , bank.Code                                 AS BankCode
    , empBank.StartDate
    , empBank.IBAN                              AS [Bank IBAN]
    , empBank.AccountNum
    , orgUnit.DisplayName                       AS OrgUnit          -- see rewrite note 1
    , emp.FullNameEn                            AS [Employee Name]
    , subGroup.NameEn                           AS Grade
    , grp.NameEn                                AS [Salary Profile]
    , jobTitle.NameEn                           AS Jobtitle
    , emp.JoiningDate                           AS [Joining Date]
    , sr.ForPeriod
    , sr.InPeriod
    , sr.PayrollRunType
    , srdNet.Amount                             AS [Net Pay]
    , payrollArea.Name                          AS [Payroll Area]
    , payrollArea.Code                          AS [Payroll Area Code]
    , basicPayCurrency.CurrencyShortFormEn      AS Currency
    , CAST(NULL AS VARBINARY(MAX))              AS CompanyLogo      -- see rewrite note 2
    , UPPER(CASE
        WHEN sr.PayrollRunType = 0 THEN
            CASE WHEN @PrmEncashment = 1
                 THEN 'LEAVE ENCASHMENT PAYSLIP FOR THE MONTH : ' + DATENAME(Month, sr.ForPeriod) + '-' + CONVERT(VARCHAR(10), @PrmYear)
                 ELSE 'REGULAR PAYSLIP FOR THE MONTH : '          + DATENAME(Month, sr.ForPeriod) + '-' + CONVERT(VARCHAR(10), @PrmYear)
            END
        WHEN sr.PayrollRunType = 1 THEN
            CASE ISNULL(reason.NameEn, '')
                 WHEN 'Advance Leave'  THEN 'LEAVE SALARY PAYSLIP'
                 WHEN 'End Of Service' THEN 'END OF SERVICE SETTLEMENT'
            END
      END)                                      AS HeaderText
    , CASE WHEN sr.PayrollRunType = 1 AND ISNULL(reason.NameEn, '') = 'End Of Service'
           THEN separation.LastDayInService END AS LastDayInService
    , passport.DocumentNum                      AS PassportNo
    , passport.ExpiryDate                       AS PassportExpiryDate
    , civilId.ExpiryDate                        AS CivilIdExpiryDate
    , sponsor.NameEn                            AS SponsorEn

FROM Employees emp

INNER JOIN SalaryRegisters sr
        ON sr.EmployeeId = emp.Id
       AND sr.TenantId   = @TenantId
       AND sr.IsDeleted  = 0

-- Legacy: WageType IN (SELECT OID FROM WageType WHERE Code = 9000). The net-pay carrier row.
INNER JOIN SalaryRegisterDetails srdNet
        ON srdNet.SalaryRegisterId = sr.Id
       AND srdNet.TenantId         = @TenantId
       AND srdNet.IsDeleted        = 0
INNER JOIN WageTypes wtNet
        ON wtNet.Id       = srdNet.WageTypeId
       AND wtNet.Code     = '9000'
       AND wtNet.TenantId = @TenantId
       AND wtNet.IsDeleted = 0

LEFT JOIN EmployeeOrgAssignments eoa
       ON eoa.EmployeeId = emp.Id
      AND eoa.TenantId   = @TenantId
      AND eoa.IsDeleted  = 0
      AND @PaySlipDate BETWEEN eoa.StartDate AND eoa.EndDate
LEFT JOIN JobTitles      jobTitle     ON jobTitle.Id     = eoa.JobTitleId        AND jobTitle.TenantId     = @TenantId AND jobTitle.IsDeleted     = 0
LEFT JOIN PayrollAreas   payrollArea  ON payrollArea.Id  = eoa.PayrollAreaId     AND payrollArea.TenantId  = @TenantId AND payrollArea.IsDeleted  = 0
LEFT JOIN EmployeeGroups grp          ON grp.Id          = eoa.EmployeeGroupId   AND grp.TenantId          = @TenantId AND grp.IsDeleted          = 0
LEFT JOIN EmployeeSubGroups subGroup  ON subGroup.Id     = eoa.EmployeeSubGroupId AND subGroup.TenantId    = @TenantId AND subGroup.IsDeleted     = 0
LEFT JOIN AbpOrganizationUnits orgUnit ON orgUnit.Id     = eoa.OrganizationUnitId AND orgUnit.TenantId     = @TenantId AND orgUnit.IsDeleted      = 0

LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.EmployeeId = emp.Id
      AND empSponsor.TenantId   = @TenantId
      AND empSponsor.IsDeleted  = 0
      AND @PaySlipDate BETWEEN empSponsor.StartDate AND empSponsor.EndDate
LEFT JOIN Sponsors sponsor ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0

LEFT JOIN EmployeeBanks empBank
       ON empBank.EmployeeId = emp.Id
      AND empBank.TenantId   = @TenantId
      AND empBank.IsDeleted  = 0
      AND @PaySlipDate BETWEEN empBank.StartDate AND empBank.EndDate
LEFT JOIN Banks bank ON bank.Id = empBank.BankId AND bank.TenantId = @TenantId AND bank.IsDeleted = 0

-- Latest passport / civil-id document. Legacy used a correlated "OID IN (SELECT TOP 1 ...)"; kept as
-- an OUTER APPLY, which expresses the same "one most-recently-expiring document" intent and lets the
-- optimiser see the correlation.
OUTER APPLY (
    SELECT TOP 1 ed.DocumentNum, ed.ExpiryDate
    FROM EmployeeDocuments ed
    JOIN DocumentTypes dt ON dt.Id = ed.DocumentTypeId AND dt.TenantId = @TenantId AND dt.IsDeleted = 0
    WHERE ed.EmployeeId = emp.Id AND ed.TenantId = @TenantId AND ed.IsDeleted = 0
      AND dt.NameEn LIKE '%passport%'
    ORDER BY ed.ExpiryDate DESC
) passport
OUTER APPLY (
    SELECT TOP 1 ed.DocumentNum, ed.ExpiryDate
    FROM EmployeeDocuments ed
    JOIN DocumentTypes dt ON dt.Id = ed.DocumentTypeId AND dt.TenantId = @TenantId AND dt.IsDeleted = 0
    WHERE ed.EmployeeId = emp.Id AND ed.TenantId = @TenantId AND ed.IsDeleted = 0
      AND dt.Code = '2'
    ORDER BY ed.ExpiryDate DESC
) civilId

-- Currency of the employee's most recent BASIC PAY line -- see rewrite note 4.
OUTER APPLY (
    SELECT TOP 1 cur.CurrencyShortFormEn
    FROM EmployeeBasicPays ebp
    JOIN EmployeeBasicPayDetails ebpd ON ebpd.EmployeeBasicPayId = ebp.Id AND ebpd.TenantId = @TenantId AND ebpd.IsDeleted = 0
    JOIN WageTypes wtBasic ON wtBasic.Id = ebpd.WageTypeId AND wtBasic.Code = '1000' AND wtBasic.TenantId = @TenantId AND wtBasic.IsDeleted = 0
    LEFT JOIN Currencies cur ON cur.Id = ebpd.CurrencyId AND cur.TenantId = @TenantId AND cur.IsDeleted = 0
    WHERE ebp.EmployeeId = emp.Id AND ebp.TenantId = @TenantId AND ebp.IsDeleted = 0
    ORDER BY ebp.StartDate DESC
) basicPayCurrency

LEFT JOIN PayrollReasonTypes reason
       ON reason.Id = sr.PayrollReasonTypeId AND reason.TenantId = @TenantId AND reason.IsDeleted = 0

OUTER APPLY (
    SELECT TOP 1 es.LastDayInService
    FROM EmployeeSeparations es
    WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
    ORDER BY es.LastDayInService DESC
) separation

WHERE emp.TenantId  = @TenantId
  AND emp.IsDeleted = 0
  AND ((@PrmEmployeeStatus <> -1 AND emp.Status = @PrmEmployeeStatus) OR @PrmEmployeeStatus = -1)
  AND sr.InPeriod = sr.ForPeriod
  AND DATEPART(Month, sr.InPeriod) = @PrmMonth
  AND DATEPART(Year,  sr.InPeriod) = @PrmYear
  AND sr.PayrollRunType = @PrmPayrollRunType
  AND (
        (ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode)
     OR (ISNULL(@PrmEmpCode, '') =  '' AND ISNULL(@PrmPayrollArea, '') <> '' AND payrollArea.Name = @PrmPayrollArea)
      )
ORDER BY
    -- Legacy sort key preserved verbatim: numeric codes right-aligned zero-padded to 20,
    -- non-numeric left-aligned. (The legacy Replicate('',21) is a no-op; kept for byte parity.)
    CASE WHEN ISNUMERIC(emp.Code) = 1 THEN RIGHT(REPLICATE('0', 21) + emp.Code, 20)
         WHEN ISNUMERIC(emp.Code) = 0 THEN LEFT(emp.Code + REPLICATE('', 21), 20)
         ELSE emp.Code
    END;
