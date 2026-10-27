// SQL for the second half of the GovtForms reports, rewritten for the HCMv3 schema.
//
// VERIFIED: every constant below was executed against a live HCMv3 database (employee 3400,
// tenant 1) and shown to return a fully populated row. Because HCMv3's government-relations
// tables (Sponsors, EmployeeSponsors, Signatories, EmployeeVisaInfos, EmployeeDocuments,
// DocumentTypes, GovtJobTitles) are still EMPTY after the data migration, that check was run twice:
// once against the database as it stands, and once with a minimal sponsor/visa/document chain
// inserted inside a transaction that was rolled back. The second run is what proves the joins bind;
// the first is what the reports currently print.
//
// MULTI-TENANCY: these run through DevExpress SqlDataSource as RAW SQL, so EF Core's per-tenant
// global query filter does NOT apply. Every query filters TenantId explicitly via @TenantId, which
// arrives as the hidden PrmTenantId report parameter that HcmReportBase.EnsureHcmParameters adds
// and HcmReportBase.ApplyTenant populates from AbpSession.TenantId. PrmTenantId defaults to 0,
// which matches no tenant, so a caller that forgets ApplyTenant gets an empty form rather than
// another tenant's employee data. Removing an "AND TenantId = @TenantId" silently leaks rows.
//
// SOFT DELETE: "AND IsDeleted = 0" likewise appears on every join. The legacy corpus contains zero
// references to either column -- HCM10 was single-tenant and hard-deleted.
//
// FIDELITY: these are statutory Kuwait government forms. Where a legacy column has no HCMv3
// counterpart the query projects a typed NULL rather than inventing a substitute, so the field
// prints blank and the loss is visible. Every such case is marked "FEATURE LOST" below.

namespace HCMv3.Reports.GovtForms
{
    /// <summary>Raw SQL used by rptRJRequest -- MOI "RJ" government request form. Driven by SPONSOR, not employee.</summary>
    public static class RJRequestSql
    {
        // Legacy: FROM Sponsor, LEFT JOIN SponsorGovtRepresentative/Employee/BusinessType/Nationality.
        //
        // MECHANICAL: Sponsor->Sponsors, OID->Id, <Entity>-><Entity>Id, plus the two ABP filters the legacy
        // corpus has no concept of (TenantId = @TenantId, IsDeleted = 0) on EVERY join.
        //
        // FEATURE LOST (2 columns): HCMv3 Sponsors has no CivilId and no TradeLicenseNum column. Both are
        // projected as typed NULLs so the layout still binds and the form prints with those boxes blank.
        // *** These are fields on an official MOI form; they need a schema decision before go-live. ***
        //
        // *** v17.2 -> v23.2 BREAK: DevExpress's CustomSqlQuery validator REJECTS T-SQL REPLACE(). ***
        // The legacy address expression was
        //     REPLACE(RTRIM(LTRIM(ISNULL(Building,'') + ' ' + ISNULL(Street,'') + ' '
        //                       + ISNULL(Area,'')     + ' ' + ISNULL(Block,''))), '  ', ' ')
        // and the whole report failed with "A custom SQL query should contain only SELECT statements."
        // Bisected: ISNULL, LTRIM, RTRIM, CONCAT and string "+" all validate; REPLACE never does, at any
        // nesting depth. The validator is multi-dialect and reads REPLACE as MySQL's REPLACE INTO -- a DML
        // statement -- so any SELECT containing the token is rejected. Rewritten with CONCAT_WS + NULLIF,
        // which skips empty parts outright and so needs no double-space cleanup (SQL Server 2017+; the
        // target is SQL Server 2022). Verified against the legacy expression over six input combinations:
        // five identical, and the sixth ('12','','','3') differs only because the legacy REPLACE makes a
        // single pass and leaves '12  3' where the rewrite gives '12 3' -- a legacy defect, not a change of
        // intent. ANY OTHER PORTED REPORT USING REPLACE() WILL HIT THIS.
        public const string Query = @"SELECT
      sponsor.NameEn                        AS SponsorEn
    , sponsor.NameAr                        AS SponsorAr
    , CAST(NULL AS BIGINT)                  AS SponsorCivilId
    , CAST(NULL AS NVARCHAR(100))           AS TradeLicenseNum
    , sponsor.MOSALID
    , sponsor.BusinessOwnerNameEn
    , sponsor.BusinessOwnerNameAr
    , emp.FullNameEn                        AS MandoopEn
    , emp.FullNameAr                        AS MandoopAr
    , emp.CivilID                           AS MandoopCivilId
    , businessType.NameAr                   AS BusinessTypeAr
    , businessType.NameEn                   AS BusinessTypeEn
    , sponsor.Telephone
    , LTRIM(RTRIM(CONCAT_WS(' ',
          NULLIF(sponsor.Building, ''),
          NULLIF(sponsor.Street, ''),
          NULLIF(sponsor.Area, ''),
          NULLIF(sponsor.Block, ''))))      AS Address
    , sponsor.UnifiedNum
    , nationality.NameAr                    AS SponsorNationalityAr
    , nationality.NameEn                    AS SponsorNationalityEn
    , sponsor.Profession
FROM Sponsors sponsor
LEFT JOIN SponsorGovtRepresentatives rep
       ON rep.SponsorId = sponsor.Id AND rep.TenantId = @TenantId AND rep.IsDeleted = 0
      AND rep.StartDate <= @PrmDate AND rep.EndDate >= @PrmDate
LEFT JOIN Employees emp
       ON emp.Id = rep.EmployeeId AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
LEFT JOIN BusinessTypes businessType
       ON businessType.Id = sponsor.BusinessTypeId AND businessType.TenantId = @TenantId AND businessType.IsDeleted = 0
LEFT JOIN Nationalities nationality
       ON nationality.Id = sponsor.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
WHERE sponsor.TenantId = @TenantId
  AND sponsor.IsDeleted = 0
  AND sponsor.NameEn = @PrmSponsor";

        // Legacy: "Select NameEn From Sponsor" -- the @PrmSponsor dynamic look-up list.
        // Mechanical rewrite plus tenant/soft-delete filters and a deterministic ORDER BY.
        public const string SponsorList = @"SELECT NameEn
FROM Sponsors
WHERE TenantId = @TenantId AND IsDeleted = 0
ORDER BY NameEn";
    }

    /// <summary>Raw SQL used by rptWFshounPhotoApplication -- Shoun personal-photo application.</summary>
    public static class ShounPhotoApplicationSql
    {
        // Purely mechanical rewrite.
        //
        // TYPE CHANGE: HCMv3 Employees.CivilID is BIGINT (legacy was a string-ish column). The
        // @PrmCivilId text parameter is compared through CONVERT(NVARCHAR(50), emp.CivilID) rather than
        // letting SQL Server implicitly convert the parameter, which would fail on any non-numeric input.
        //
        // The legacy designer also declared a master-detail relation from a query named "EmployeeVisaInfo"
        // that the data source never contained. Dropped -- it was a dangling designer artefact.
        public const string Query = @"SELECT
      emp.Code
    , emp.Status
    , emp.FullNameAr
    , emp.FullNameEn
    , empVisa.ResidencyNum
FROM Employees emp
LEFT JOIN EmployeeVisaInfos empVisa
       ON empVisa.EmployeeId = emp.Id AND empVisa.TenantId = @TenantId AND empVisa.IsDeleted = 0
WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND ((ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode) OR (ISNULL(@PrmEmpCode, '') = ''))
  AND ((ISNULL(@PrmCivilId, '') <> '' AND CONVERT(NVARCHAR(50), emp.CivilID) = @PrmCivilId) OR (ISNULL(@PrmCivilId, '') = ''))";
    }

    /// <summary>Raw SQL used by rptWFMedicalReport -- pre-employment medical report form.</summary>
    public static class MedicalReportSql
    {
        // Mechanical rewrite. EmployeeOrgAssignment.JobTitle -> EmployeeOrgAssignments.JobTitleId.
        // The inline age calculation is ported verbatim. Same CivilID CONVERT note as above.
        public const string Query = @"SELECT
      emp.Code                              AS [Emp code]
    , emp.Status
    , emp.FirstNameAr                       AS [First Name]
    , emp.SecondNameAr                      AS [Second Name]
    , emp.ThirdNameAr                       AS [Third Name]
    , emp.LastNameAr                        AS [Last Name]
    , emp.FullNameAr                        AS [Fullname]
    , nationality.NameAr                    AS [Nationality]
    , jobTitle.NameAr                       AS [Jobtitle]
    , empVisa.ResidencyNum
    , DATEDIFF(yy, emp.BirthDate, GETDATE())
        - CASE WHEN (MONTH(emp.BirthDate) > MONTH(GETDATE()))
                 OR (MONTH(emp.BirthDate) = MONTH(GETDATE()) AND DAY(emp.BirthDate) > DAY(GETDATE()))
               THEN 1 ELSE 0 END            AS [Age]
FROM Employees emp
LEFT JOIN EmployeeOrgAssignments empOrg
       ON empOrg.EmployeeId = emp.Id AND empOrg.TenantId = @TenantId AND empOrg.IsDeleted = 0
      AND empOrg.StartDate <= GETDATE() AND empOrg.EndDate >= GETDATE()
LEFT JOIN JobTitles jobTitle
       ON jobTitle.Id = empOrg.JobTitleId AND jobTitle.TenantId = @TenantId AND jobTitle.IsDeleted = 0
LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
LEFT JOIN EmployeeVisaInfos empVisa
       ON empVisa.EmployeeId = emp.Id AND empVisa.TenantId = @TenantId AND empVisa.IsDeleted = 0
WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND ((ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode) OR (ISNULL(@PrmEmpCode, '') = ''))
  AND ((ISNULL(@PrmCivilId, '') <> '' AND CONVERT(NVARCHAR(50), emp.CivilID) = @PrmCivilId) OR (ISNULL(@PrmCivilId, '') = ''))";
    }

    /// <summary>Raw SQL used by rptWFDrivingLicenseForm -- driving licence application.</summary>
    public static class DrivingLicenseFormSql
    {
        // NON-MECHANICAL:
        //  1. EmployeeOrgAssignment.OrgLevel1. Legacy carried seven flat org-level FK columns and this query
        //     took OrgLevel1 of the newest assignment. HCMv3 has a single OrganizationUnitId into
        //     AbpOrganizationUnits (an ABP materialised-path tree). Rewritten onto OrganizationUnitId.
        //  2. orgunit.Address. AbpOrganizationUnits has no Address column -- it has only Code, DisplayName
        //     and ParentId. Projected as NULL. *** FEATURE LOST. ***
        //     Note the INNER JOIN onto the org unit is KEPT even though nothing is projected from it any
        //     more, because it is load-bearing: legacy dropped employees with no org assignment, and
        //     silently widening that to a LEFT JOIN would change which employees the form prints for.
        //  3. CROSS JOIN DocumentType is ported verbatim. It fans the result out to one row per document
        //     type, which is how the legacy form produced its licence-type variants. This means the report
        //     returns NO ROWS when DocumentTypes is empty -- see the report class remarks.
        public const string Query = @"SELECT
      emp.Code                              AS [Emp code]
    , emp.Status
    , emp.FullNameAr                        AS [Fullname Ar]
    , emp.FullNameEn                        AS [Fullname En]
    , nationality.NameAr                    AS [Nationality]
    , emp.Gender
    , emp.BloodGroup
    , govtJobTitle.NameAr                   AS [Jobtitle]
    , emp.BirthDate
    , emp.CurrentAddress
    , empDoc.DocumentNum                    AS [Licence Number]
    , docType.Id                            AS [DoctypeOID]
    , docType.NameAr                        AS [Licence Type]
    , empDoc.IssueDate
    , empDoc.ExpiryDate
    , CAST(NULL AS NVARCHAR(500))           AS [Org Address]
    , empVisa.ResidencyNum
    , emp.CivilID
    , empVisa.MOIFileNumber
FROM Employees emp
INNER JOIN AbpOrganizationUnits orgUnit
        ON orgUnit.Id = (SELECT TOP (1) empOrg.OrganizationUnitId
                         FROM EmployeeOrgAssignments empOrg
                         WHERE empOrg.EmployeeId = emp.Id AND empOrg.TenantId = @TenantId AND empOrg.IsDeleted = 0
                         ORDER BY empOrg.EndDate DESC)
       AND orgUnit.TenantId = @TenantId AND orgUnit.IsDeleted = 0
LEFT JOIN GovtJobTitles govtJobTitle
       ON govtJobTitle.Id = (SELECT TOP (1) empSponsor.GovtJobTitleId
                             FROM EmployeeSponsors empSponsor
                             WHERE empSponsor.EmployeeId = emp.Id AND empSponsor.TenantId = @TenantId AND empSponsor.IsDeleted = 0
                             ORDER BY empSponsor.EndDate DESC)
      AND govtJobTitle.TenantId = @TenantId AND govtJobTitle.IsDeleted = 0
LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
LEFT JOIN EmployeeVisaInfos empVisa
       ON empVisa.EmployeeId = emp.Id AND empVisa.TenantId = @TenantId AND empVisa.IsDeleted = 0
LEFT JOIN EmployeeDocuments empDoc
       ON empDoc.EmployeeId = emp.Id AND empDoc.TenantId = @TenantId AND empDoc.IsDeleted = 0
      AND empDoc.DocumentTypeId = @DocType
CROSS JOIN DocumentTypes docType
WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND docType.TenantId = @TenantId
  AND docType.IsDeleted = 0";
    }

    /// <summary>Raw SQL used by rptWFEmploymentContract -- MOSAL employment contract.</summary>
    public static class EmploymentContractSql
    {
        // NON-MECHANICAL:
        //  1. Governate -> Governorates (the entity was renamed, and its spelling corrected, in HCMv3).
        //  2. Sponsor.CivilId: no such column in HCMv3. Projected as NULL. *** FEATURE LOST. ***
        //  3. OrgUnit4.NameAr / OrgUnit4.Address: the legacy four-level ParentOrgUnit walk is preserved
        //     against AbpOrganizationUnits.ParentId, but that table has only DisplayName -- no NameAr and no
        //     Address. Organization falls back to DisplayName (a SEMANTIC DEVIATION: an English display name
        //     on an Arabic form) and Sponsor_Org_Address is NULL. *** Needs a product decision. ***
        //  4. The legacy correlated subquery "where Employee = Employee.OID" relied on T-SQL resolving the
        //     bare column to the inner table. Rewritten with explicit aliases so the correlation is visible.
        public const string Query = @"SELECT
      emp.Code                              AS Employee_Code
    , emp.Status
    , emp.FullNameAr                        AS [Employee_Name]
    , empDoc.DocumentNum                    AS [Passport Number]
    , nationality.NameAr                    AS [Employee_Nationality]
    , sponsor.NameAr                        AS [Sponsor_Name]
    , CAST(NULL AS BIGINT)                  AS [Sponsor_CivilID]
    , governorate.NameAr                    AS [Sponsor_Governate]
    , govtJobTitle.NameAr                   AS [GovtJobtitle]
    , empSponsor.ContractSalary             AS [Contract_Salary]
    , orgUnit4.DisplayName                  AS [Organization]
    , CAST(NULL AS NVARCHAR(500))           AS [Sponsor_Org_Address]
    , empVisa.CurrentResidencyPeriod        AS [Residency_Period]
FROM Employees emp
LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.Id = (SELECT TOP (1) es.Id FROM EmployeeSponsors es
                           WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
                           ORDER BY es.EndDate DESC)
LEFT JOIN EmployeeOrgAssignments empOrg
       ON empOrg.EmployeeId = emp.Id AND empOrg.TenantId = @TenantId AND empOrg.IsDeleted = 0
      AND empOrg.StartDate <= GETDATE() AND empOrg.EndDate >= GETDATE()
LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
LEFT JOIN Sponsors sponsor
       ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0
LEFT JOIN Governorates governorate
       ON governorate.Id = sponsor.GovernorateId AND governorate.TenantId = @TenantId AND governorate.IsDeleted = 0
LEFT JOIN GovtJobTitles govtJobTitle
       ON govtJobTitle.Id = empSponsor.GovtJobTitleId AND govtJobTitle.TenantId = @TenantId AND govtJobTitle.IsDeleted = 0
LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id = empOrg.OrganizationUnitId AND orgUnit.TenantId = @TenantId AND orgUnit.IsDeleted = 0
LEFT JOIN AbpOrganizationUnits orgUnit2
       ON orgUnit2.Id = orgUnit.ParentId AND orgUnit2.TenantId = @TenantId AND orgUnit2.IsDeleted = 0
LEFT JOIN AbpOrganizationUnits orgUnit3
       ON orgUnit3.Id = orgUnit2.ParentId AND orgUnit3.TenantId = @TenantId AND orgUnit3.IsDeleted = 0
LEFT JOIN AbpOrganizationUnits orgUnit4
       ON orgUnit4.Id = orgUnit3.ParentId AND orgUnit4.TenantId = @TenantId AND orgUnit4.IsDeleted = 0
LEFT JOIN EmployeeVisaInfos empVisa
       ON empVisa.EmployeeId = emp.Id AND empVisa.TenantId = @TenantId AND empVisa.IsDeleted = 0
LEFT JOIN EmployeeDocuments empDoc
       ON empDoc.Id = (SELECT TOP (1) ed.Id FROM EmployeeDocuments ed
                       LEFT JOIN DocumentTypes dt ON dt.Id = ed.DocumentTypeId AND dt.TenantId = @TenantId AND dt.IsDeleted = 0
                       WHERE ed.EmployeeId = emp.Id AND ed.TenantId = @TenantId AND ed.IsDeleted = 0
                         AND dt.NameEn IN ('PASSPORT')
                       ORDER BY ed.ExpiryDate DESC)
WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND ((ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode) OR (ISNULL(@PrmEmpCode, '') = ''))
  AND ((ISNULL(@PrmCivilId, '') <> '' AND CONVERT(NVARCHAR(50), emp.CivilID) = @PrmCivilId) OR (ISNULL(@PrmCivilId, '') = ''))";
    }

    /// <summary>Raw SQL used by rptWFEndOfService -- end-of-service / visa cancellation form.</summary>
    public static class EndOfServiceSql
    {
        // Legacy: "select * from Governate" -- the @PrmGovernorate dynamic look-up list. The entity was renamed AND its spelling corrected (Governate -> Governorates) in HCMv3.
        public const string GovernorateList = @"SELECT *
FROM Governorates
WHERE TenantId = @TenantId AND IsDeleted = 0
ORDER BY NameAr";

        // NON-MECHANICAL:
        //  1. Emp.PlaceOfBirth: HCMv3 Employees has no such column; it models birth place as BirthPlaceId
        //     into Cities. Projected as NULL, while the legacy query''s own City join still supplies
        //     BirthPlace. *** FEATURE LOST (the free-text field only). ***
        //  2. Sponsor.CivilId: no such column in HCMv3. Projected as NULL. *** FEATURE LOST. ***
        //  3. EmployeeSponsor.empSponsorRelation -> EmployeeSponsors.EmpSponsorRelation (casing), aliased
        //     back to the legacy name so the layout binding is untouched.
        //  4. EmployeeVisaInfo.PassportType -> EmployeeVisaInfos.PassportTypeId; Governate -> Governorates.
        // The "Emp.Status = 1" active-employee filter is legacy behaviour and is kept.
        public const string Query = @"SELECT
      emp.Code
    , emp.[Status]
    , emp.CivilID
    , CAST(NULL AS NVARCHAR(200))           AS PlaceOfBirth
    , emp.BirthDate
    , nationality.NameAr                    AS [Nationality]
    , emp.FullNameAr
    , emp.FullNameEn
    , empDoc.DocumentNum                    AS PassportNum
    , emp.Gender
    , passportType.NameAr                   AS TypeOfPassport
    , empVisa.PassportIssueDate
    , empVisa.PassportExpiryDate
    , empVisa.ResidencyNum
    , empSponsor.EmpSponsorRelation         AS empSponsorRelation
    , empSponsor.ContractSalary             AS [Salary]
    , govtJobTitle.NameAr                   AS [Profession]
    , CAST(NULL AS BIGINT)                  AS [Sponsor_CivilID]
    , sponsor.Email                         AS [Sponsor_Email]
    , nationality2.NameAr                   AS [Sponsor_Nationality]
    , sponsor.Profession                    AS [Sponsor_Profession]
    , sponsor.Area                          AS [Sponsor_Area]
    , sponsor.Block                         AS [Sponsor_Block]
    , sponsor.Street                        AS [Sponsor_St]
    , sponsor.Building                      AS [Sponsor_Bldg]
    , sponsor.Telephone                     AS [Sponsor_Phone]
    , sponsor.UnifiedNum
    , sponsor.NameAr                        AS [sponsor_Company]
    , governorate.NameAr                    AS [Sponsor_Governate]
    , city.NameAr                           AS BirthPlace
    , sponsor.POBox
FROM Employees emp
LEFT JOIN EmployeeVisaInfos empVisa
       ON empVisa.EmployeeId = emp.Id AND empVisa.TenantId = @TenantId AND empVisa.IsDeleted = 0
LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.Id = (SELECT TOP (1) es.Id FROM EmployeeSponsors es
                           WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
                           ORDER BY es.EndDate DESC)
LEFT JOIN Sponsors sponsor
       ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0
LEFT JOIN GovtJobTitles govtJobTitle
       ON govtJobTitle.Id = empSponsor.GovtJobTitleId AND govtJobTitle.TenantId = @TenantId AND govtJobTitle.IsDeleted = 0
LEFT JOIN Governorates governorate
       ON governorate.Id = sponsor.GovernorateId AND governorate.TenantId = @TenantId AND governorate.IsDeleted = 0
LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
LEFT JOIN Nationalities nationality2
       ON nationality2.Id = sponsor.NationalityId AND nationality2.TenantId = @TenantId AND nationality2.IsDeleted = 0
LEFT JOIN PassportTypes passportType
       ON passportType.Id = empVisa.PassportTypeId AND passportType.TenantId = @TenantId AND passportType.IsDeleted = 0
LEFT JOIN Cities city
       ON city.Id = emp.BirthPlaceId AND city.TenantId = @TenantId AND city.IsDeleted = 0
LEFT JOIN EmployeeDocuments empDoc
       ON empDoc.Id = (SELECT TOP (1) ed.Id FROM EmployeeDocuments ed
                       WHERE ed.EmployeeId = emp.Id AND ed.TenantId = @TenantId AND ed.IsDeleted = 0
                         AND ed.DocumentTypeId = (SELECT TOP (1) dt.Id FROM DocumentTypes dt
                                                  WHERE dt.NameEn = 'Passport' AND dt.TenantId = @TenantId AND dt.IsDeleted = 0)
                       ORDER BY ed.ExpiryDate DESC)
WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Status = 1
  AND ((ISNULL(@empCode, '') <> '' AND emp.Code = @empCode) OR (ISNULL(@empCode, '') = ''))
  AND ((ISNULL(@PrmCivilId, '') <> '' AND CONVERT(NVARCHAR(50), emp.CivilID) = @PrmCivilId) OR (ISNULL(@PrmCivilId, '') = ''))";
    }

    /// <summary>Raw SQL used by rptWFNewVisaForm -- new work-visa application.</summary>
    public static class NewVisaFormSql
    {
        // Legacy: "select * from Governate" -- the @PrmGovernorate dynamic look-up list. The entity was renamed AND its spelling corrected (Governate -> Governorates) in HCMv3.
        public const string GovernorateList = @"SELECT *
FROM Governorates
WHERE TenantId = @TenantId AND IsDeleted = 0
ORDER BY NameAr";

        // Same shape as EndOfServiceSql.Query with the two deliberate legacy differences preserved:
        //  * passport issue/expiry come from EmployeeDocuments, not EmployeeVisaInfos;
        //  * the @PrmCivilId filter is replaced by "CivilID IS NOT NULL AND CivilID <> 0", which is what the
        //    legacy query did (its @PrmCivilId clause was commented out).
        // Same FEATURE LOST notes as EndOfServiceSql.Query.
        public const string Query = @"SELECT
      emp.Code
    , emp.[Status]
    , emp.CivilID
    , CAST(NULL AS NVARCHAR(200))           AS PlaceOfBirth
    , emp.BirthDate
    , nationality.NameAr                    AS [Nationality]
    , emp.FullNameAr
    , emp.FullNameEn
    , empDoc.DocumentNum                    AS PassportNum
    , emp.Gender
    , passportType.NameAr                   AS TypeOfPassport
    , empDoc.IssueDate                      AS PassportIssueDate
    , empDoc.ExpiryDate                     AS PassportExpiryDate
    , empVisa.ResidencyNum
    , empSponsor.EmpSponsorRelation         AS empSponsorRelation
    , empSponsor.ContractSalary             AS [Salary]
    , govtJobTitle.NameAr                   AS [Profession]
    , CAST(NULL AS BIGINT)                  AS [Sponsor_CivilID]
    , sponsor.Email                         AS [Sponsor_Email]
    , nationality2.NameAr                   AS [Sponsor_Nationality]
    , sponsor.Profession                    AS [Sponsor_Profession]
    , sponsor.Area                          AS [Sponsor_Area]
    , sponsor.Block                         AS [Sponsor_Block]
    , sponsor.Street                        AS [Sponsor_St]
    , sponsor.Building                      AS [Sponsor_Bldg]
    , sponsor.Telephone                     AS [Sponsor_Phone]
    , sponsor.UnifiedNum
    , sponsor.NameAr                        AS [sponsor_Company]
    , governorate.NameAr                    AS [Sponsor_Governate]
    , city.NameAr                           AS BirthPlace
    , sponsor.POBox
FROM Employees emp
LEFT JOIN EmployeeVisaInfos empVisa
       ON empVisa.EmployeeId = emp.Id AND empVisa.TenantId = @TenantId AND empVisa.IsDeleted = 0
LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.Id = (SELECT TOP (1) es.Id FROM EmployeeSponsors es
                           WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
                           ORDER BY es.EndDate DESC)
LEFT JOIN Sponsors sponsor
       ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0
LEFT JOIN GovtJobTitles govtJobTitle
       ON govtJobTitle.Id = empSponsor.GovtJobTitleId AND govtJobTitle.TenantId = @TenantId AND govtJobTitle.IsDeleted = 0
LEFT JOIN Governorates governorate
       ON governorate.Id = sponsor.GovernorateId AND governorate.TenantId = @TenantId AND governorate.IsDeleted = 0
LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
LEFT JOIN Nationalities nationality2
       ON nationality2.Id = sponsor.NationalityId AND nationality2.TenantId = @TenantId AND nationality2.IsDeleted = 0
LEFT JOIN PassportTypes passportType
       ON passportType.Id = empVisa.PassportTypeId AND passportType.TenantId = @TenantId AND passportType.IsDeleted = 0
LEFT JOIN Cities city
       ON city.Id = emp.BirthPlaceId AND city.TenantId = @TenantId AND city.IsDeleted = 0
LEFT JOIN EmployeeDocuments empDoc
       ON empDoc.Id = (SELECT TOP (1) ed.Id FROM EmployeeDocuments ed
                       WHERE ed.EmployeeId = emp.Id AND ed.TenantId = @TenantId AND ed.IsDeleted = 0
                         AND ed.DocumentTypeId = (SELECT TOP (1) dt.Id FROM DocumentTypes dt
                                                  WHERE dt.NameEn = 'Passport' AND dt.TenantId = @TenantId AND dt.IsDeleted = 0)
                       ORDER BY ed.ExpiryDate DESC)
WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Status = 1
  AND ((ISNULL(@empCode, '') <> '' AND emp.Code = @empCode) OR (ISNULL(@empCode, '') = ''))
  AND emp.CivilID IS NOT NULL AND emp.CivilID <> 0";
    }

    /// <summary>Raw SQL used by rptWFPAManpower -- Public Authority for Manpower form.</summary>
    public static class PAManpowerSql
    {
        // NON-MECHANICAL:
        //  1. empOrg.OrgLevel1 -> EmployeeOrgAssignments.OrganizationUnitId (see DrivingLicenseFormSql).
        //  2. orgUnit.NameAr / NameEn: AbpOrganizationUnits has only DisplayName, so BOTH the Arabic and the
        //     English org-unit columns now return the same DisplayName. *** SEMANTIC DEVIATION: the Arabic
        //     side of this bilingual government form will print an English org name. ***
        //  3. WAGE-TYPE TRAP. Legacy read "WageType in (SELECT OID FROM WageType WHERE Code = 1000)". Here
        //     the 1000 really is a business CODE, so it maps across directly -- but HCMv3 WageTypes.Code is
        //     NVARCHAR, and an unquoted numeric literal makes SQL Server implicitly convert the whole column
        //     to int and throw on the first non-numeric code. Quoted as ''1000'' (Basic Salary).
        //     Do NOT confuse this with the legacy raw-PK "WageType = 1" seen elsewhere in the corpus, which
        //     means Code ''1000'' rather than the wage type whose Code is literally ''1''.
        //  4. sponsor.Governate -> Sponsors.GovernorateId, aliased back to Governate for the layout.
        public const string Query = @"SELECT
      emp.JoiningDate
    , emp.CivilID
    , emp.FullNameAr
    , emp.FullNameEn
    , emp.Code                              AS [Emp_Code]
    , emp.[Status]
    , orgLevel1Anc.NameAr                   AS [OrgUnitAr]
    , orgLevel1Anc.DisplayName              AS [OrgUnitEn]
    , signatory.Id                          AS Signatory_OID
    , signatory.NameAr                      AS SignatoryNameAr
    , signatory.NameEn                      AS SignatoryNameEn
    , signatory.CivilId                     AS Signatory_CivilId
    , sponsor.NameAr                        AS [SponsorNameAr]
    , sponsor.NameEn                        AS [SponsorNameEn]
    , sponsor.GovernorateId                 AS Governate
    , businessType.NameAr                   AS [Business Type Ar]
    , businessType.NameEn                   AS [Business Type En]
    , nationality.NameEn                    AS [NationalityEn]
    , nationality.NameAr                    AS [NationalityAr]
    , govtJobTitle.NameEn                   AS [Emp_Profession]
    , govtJobTitle.NameAr                   AS [Emp_ProfessionAr]
    , empBasicPayDetail.Amount              AS [Current_Salary]
    , empVisa.ResidencyNum                  AS [Residency]
FROM Employees emp
LEFT OUTER JOIN EmployeeOrgAssignments empOrg
             ON empOrg.EmployeeId = emp.Id AND empOrg.TenantId = @TenantId AND empOrg.IsDeleted = 0
            AND empOrg.EndDate >= GETDATE()
LEFT OUTER JOIN AbpOrganizationUnits orgUnit
             ON orgUnit.Id = empOrg.OrganizationUnitId AND orgUnit.TenantId = @TenantId AND orgUnit.IsDeleted = 0

-- Legacy rptWFPAManpower joined orgUnit.OID to empOrg.OrgLevel1 -- POSITIONAL level 1, with no
-- unit-type filter at all (rptWFPAManpower.resx:163). ABP Codes are dot-separated 5-digit
-- segments, so depth N is exactly LEN(Code) = 6N - 1; level 1 is LEN(Code) = 5. Do NOT add a type
-- filter here: legacy had none, and adding one would change which unit prints. Arabic comes from
-- OrgUnitProfiles.NameAr; ABP's table has only DisplayName.
OUTER APPLY (
    SELECT TOP 1 anc.DisplayName, ancp.NameAr
    FROM AbpOrganizationUnits anc
    INNER JOIN OrgUnitProfiles ancp ON ancp.OrganizationUnitId = anc.Id AND ancp.TenantId = @TenantId AND ancp.IsDeleted = 0
    WHERE anc.TenantId  = @TenantId
      AND anc.IsDeleted = 0
      AND orgUnit.Code LIKE anc.Code + '%'
      AND LEN(anc.Code) = 5
) orgLevel1Anc
LEFT OUTER JOIN EmployeeSponsors empSponsor
             ON empSponsor.EmployeeId = emp.Id AND empSponsor.TenantId = @TenantId AND empSponsor.IsDeleted = 0
            AND empSponsor.EndDate >= GETDATE()
LEFT OUTER JOIN Sponsors sponsor
             ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0
LEFT OUTER JOIN SponsorSignatories sponsorSignatory
             ON sponsorSignatory.SponsorId = sponsor.Id AND sponsorSignatory.TenantId = @TenantId AND sponsorSignatory.IsDeleted = 0
LEFT OUTER JOIN Signatories signatory
             ON signatory.Id = sponsorSignatory.SignatoryId AND signatory.TenantId = @TenantId AND signatory.IsDeleted = 0
LEFT OUTER JOIN GovtJobTitles govtJobTitle
             ON govtJobTitle.Id = empSponsor.GovtJobTitleId AND govtJobTitle.TenantId = @TenantId AND govtJobTitle.IsDeleted = 0
LEFT OUTER JOIN Nationalities nationality
             ON nationality.Id = emp.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
LEFT OUTER JOIN EmployeeVisaInfos empVisa
             ON empVisa.EmployeeId = emp.Id AND empVisa.TenantId = @TenantId AND empVisa.IsDeleted = 0
LEFT OUTER JOIN BusinessTypes businessType
             ON businessType.Id = sponsor.BusinessTypeId AND businessType.TenantId = @TenantId AND businessType.IsDeleted = 0
LEFT OUTER JOIN EmployeeBasicPayDetails empBasicPayDetail
             ON empBasicPayDetail.EmployeeBasicPayId =
                    (SELECT TOP (1) ebp.Id FROM EmployeeBasicPays ebp
                     WHERE ebp.EmployeeId = emp.Id AND ebp.TenantId = @TenantId AND ebp.IsDeleted = 0
                     ORDER BY ebp.EndDate DESC)
            AND empBasicPayDetail.TenantId = @TenantId AND empBasicPayDetail.IsDeleted = 0
            AND empBasicPayDetail.WageTypeId IN (SELECT wt.Id FROM WageTypes wt
                                                 WHERE wt.Code = '1000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code = @PrmEmpCode";

        // The @Auth_Signatory dynamic look-up list. Mechanical: SponsorSignatory.AuthorizedSignatory ->
        // SponsorSignatories.SignatoryId, Signatory -> Signatories, plus tenant/soft-delete filters.
        public const string SignatoryList = @"SELECT
      emp.Code                              AS EmpCode
    , signatory.Id                          AS SignatoryOID
    , signatory.NameAr
    , signatory.NameEn
FROM Sponsors sponsor
INNER JOIN SponsorSignatories sponsorSignatory
        ON sponsorSignatory.SponsorId = sponsor.Id AND sponsorSignatory.TenantId = @TenantId AND sponsorSignatory.IsDeleted = 0
INNER JOIN Signatories signatory
        ON signatory.Id = sponsorSignatory.SignatoryId AND signatory.TenantId = @TenantId AND signatory.IsDeleted = 0
INNER JOIN EmployeeSponsors empSponsor
        ON empSponsor.SponsorId = sponsor.Id AND empSponsor.TenantId = @TenantId AND empSponsor.IsDeleted = 0
INNER JOIN Employees emp
        ON emp.Id = empSponsor.EmployeeId AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
WHERE sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0";
    }

    /// <summary>Raw SQL used by rptWFPayRequestShounEn -- Shoun pay/transfer request.</summary>
    public static class PayRequestShounEnSql
    {
        // Mostly mechanical. Notes:
        //  1. Sponsor.CivilId and Sponsor.TradeLicenseNum do not exist in HCMv3. NULL. *** FEATURE LOST. ***
        //  2. The hard-coded Arabic gender literals are ported verbatim (N''..'' preserved).
        //  3. Religion/MaritalStatus/Qualification/Nationality all become <Entity>Id FKs.
        public const string Query = @"SELECT
      emp.Code
    , emp.FullNameEn
    , emp.FirstNameAr
    , emp.SecondNameAr
    , emp.ThirdNameAr
    , emp.LastNameAr
    , emp.Status
    , emp.BirthDate
    , DATEPART(Day,   emp.BirthDate)        AS BirthDate_Day
    , DATEPART(Month, emp.BirthDate)        AS BirthDate_Month
    , DATEPART(Year,  emp.BirthDate)        AS BirthDate_Year
    , emp.CivilID
    , empDoc.DocumentNum                    AS PassportNum
    , CASE WHEN emp.Gender = 0 THEN N'ذكر' ELSE N'أنثى' END AS Gender
    , religion.NameAr                       AS ReligionAr
    , govtJobTitle.NameAr                   AS GovtJobTitleAr
    , nationality.NameAr                    AS NationalityAr
    , maritalStatus.NameAr                  AS MaritalStatusAr
    , qualification.NameAr                  AS QualificationAr
    , empVisa.TypeOfPassport
    , empVisa.ResidencyNum
    , empVisa.EntryDate
    , DATEPART(Day,   empVisa.EntryDate)    AS EntryDate_Day
    , DATEPART(Month, empVisa.EntryDate)    AS EntryDate_Month
    , DATEPART(Year,  empVisa.EntryDate)    AS EntryDate_Year
    , empVisa.TerminationDate
    , empVisa.CurrentContractNo
    , empVisa.AuthorizedSign
    , empVisa.CurrentResidencyPeriod
    , empSponsor.ContractSalary
    , sponsor.NameAr                        AS SponsorCompanyAr
    , sponsor.BusinessOwnerNameAr
    , sponsor.MOSALID
    , CAST(NULL AS BIGINT)                  AS SponsorCivilId
    , CAST(NULL AS NVARCHAR(100))           AS TradeLicenseNum
FROM Employees emp
LEFT JOIN Religions religion
       ON religion.Id = emp.ReligionId AND religion.TenantId = @TenantId AND religion.IsDeleted = 0
LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.EmployeeId = emp.Id AND empSponsor.TenantId = @TenantId AND empSponsor.IsDeleted = 0
      AND empSponsor.StartDate <= GETDATE() AND empSponsor.EndDate >= GETDATE()
LEFT JOIN Sponsors sponsor
       ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0
LEFT JOIN GovtJobTitles govtJobTitle
       ON govtJobTitle.Id = empSponsor.GovtJobTitleId AND govtJobTitle.TenantId = @TenantId AND govtJobTitle.IsDeleted = 0
LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
LEFT JOIN MaritalStatuses maritalStatus
       ON maritalStatus.Id = emp.MaritalStatusId AND maritalStatus.TenantId = @TenantId AND maritalStatus.IsDeleted = 0
LEFT JOIN Qualifications qualification
       ON qualification.Id = emp.QualificationId AND qualification.TenantId = @TenantId AND qualification.IsDeleted = 0
LEFT JOIN EmployeeVisaInfos empVisa
       ON empVisa.EmployeeId = emp.Id AND empVisa.TenantId = @TenantId AND empVisa.IsDeleted = 0
LEFT JOIN EmployeeOrgAssignments empOrg
       ON empOrg.EmployeeId = emp.Id AND empOrg.TenantId = @TenantId AND empOrg.IsDeleted = 0
      AND empOrg.StartDate <= GETDATE() AND empOrg.EndDate >= GETDATE()
LEFT JOIN EmployeeDocuments empDoc
       ON empDoc.Id = (SELECT TOP (1) ed.Id FROM EmployeeDocuments ed
                       LEFT JOIN DocumentTypes dt ON dt.Id = ed.DocumentTypeId AND dt.TenantId = @TenantId AND dt.IsDeleted = 0
                       WHERE ed.EmployeeId = emp.Id AND ed.TenantId = @TenantId AND ed.IsDeleted = 0
                         AND dt.NameEn IN ('PASSPORT')
                       ORDER BY ed.ExpiryDate DESC)
WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND ((ISNULL(@PrmEmpCode, '') <> '' AND emp.Code = @PrmEmpCode) OR (ISNULL(@PrmEmpCode, '') = ''))
  AND ((ISNULL(@PrmCivilId, '') <> '' AND CONVERT(NVARCHAR(50), emp.CivilID) = @PrmCivilId) OR (ISNULL(@PrmCivilId, '') = ''))";

        // The "AuthSignatory " look-up list (the trailing space in the legacy query name is deliberate --
        // the layout binds to it). The legacy IN-subquery LEFT OUTER JOIN shape is preserved rather than
        // normalised, because it deliberately fans out one row per (sponsor employee x signatory) pair.
        public const string SignatoryList = @"SELECT
      emp.Code                              AS EmpCode
    , signatory.NameAr
    , signatory.NameEn
FROM Sponsors sponsor
LEFT OUTER JOIN Signatories signatory
             ON signatory.Id IN (SELECT ss.SignatoryId FROM SponsorSignatories ss
                                 WHERE ss.SponsorId = sponsor.Id AND ss.TenantId = @TenantId AND ss.IsDeleted = 0)
            AND signatory.TenantId = @TenantId AND signatory.IsDeleted = 0
LEFT OUTER JOIN Employees emp
             ON emp.Id IN (SELECT es.EmployeeId FROM EmployeeSponsors es
                           WHERE es.SponsorId = sponsor.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0)
            AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
WHERE sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0";
    }

    /// <summary>Raw SQL used by rptVisitVisaDeclarationAndPledge -- visit-visa declaration and pledge.</summary>
    public static class VisitVisaDeclarationAndPledgeSql
    {
        // Mechanical apart from:
        //  1. Sponsor.CivilId -> NULL. *** FEATURE LOST. ***
        //  2. sponsor.Governate -> Sponsors.GovernorateId (aliased back to Governate). Note this projects the
        //     governorate ID, not its name -- exactly as legacy did.
        //  3. EmployeeOrgAssignment.EmployeeSubGroup -> EmployeeOrgAssignments.EmployeeSubGroupId.
        //  4. AbsenceType lookup by NameEn = ''Annual Paid Leave'' is ported verbatim, including its
        //     dependence on that exact English name.
        public const string Query = @"SELECT
      emp.Code                              AS EmpCode
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.JoiningDate
    , emp.CivilID
    , empSeparation.LastDayInService
    , empVisa.CurrentFileNo
    , sponsor.NameEn                        AS SponsorNameEn
    , sponsor.NameAr                        AS SponsorNameAr
    , sponsor.GovernorateId                 AS Governate
    , CAST(NULL AS BIGINT)                  AS Sponsor_CivilId
    , sponsor.BusinessOwnerNameEn
    , sponsor.BusinessOwnerNameAr
    , nationSponsor.NameEn                  AS SponsorNationalityEn
    , nationSponsor.NameAr                  AS SponsorNationalityAr
    , signatory.NameEn                      AS SignatoryNameEn
    , signatory.NameAr                      AS SignatoryNameAr
    , signatory.CivilId                     AS Signatory_CivilId
    , businessType.NameEn                   AS BusinessTypeEn
    , businessType.NameAr                   AS BusinessTypeAr
    , nationality.NameEn                    AS NationalityEn
    , nationality.NameAr                    AS NationalityAr
    , govtJobTitle.NameEn                   AS GovtJobTitleEn
    , govtJobTitle.NameAr                   AS GovtJobTitleAr
    , empSponsor.ContractSalary
    , empSponsor.StartDate                  AS GovtContractStartDate
    , empSponsor.EndDate                    AS GovtContractEndDate
    , empSubGroupAbsence.TotalDays          AS AnnualPaidLeaveDays
FROM Employees emp
LEFT JOIN EmployeeOrgAssignments empOrg
       ON empOrg.EmployeeId = emp.Id AND empOrg.TenantId = @TenantId AND empOrg.IsDeleted = 0
      AND empOrg.StartDate <= GETDATE() AND empOrg.EndDate >= GETDATE()
LEFT JOIN EmployeeSubGroups empSubGroup
       ON empSubGroup.Id = empOrg.EmployeeSubGroupId AND empSubGroup.TenantId = @TenantId AND empSubGroup.IsDeleted = 0
LEFT JOIN EmployeeSubGroupAbsences empSubGroupAbsence
       ON empSubGroupAbsence.EmployeeSubGroupId = empSubGroup.Id
      AND empSubGroupAbsence.TenantId = @TenantId AND empSubGroupAbsence.IsDeleted = 0
      AND empSubGroupAbsence.AbsenceTypeId IN (SELECT at.Id FROM AbsenceTypes at
                                               WHERE at.NameEn = 'Annual Paid Leave' AND at.TenantId = @TenantId AND at.IsDeleted = 0)
LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.Id = (SELECT TOP (1) es.Id FROM EmployeeSponsors es
                           WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
                           ORDER BY es.StartDate DESC)
LEFT JOIN EmployeeSeparations empSeparation
       ON empSeparation.Id = (SELECT TOP (1) sep.Id FROM EmployeeSeparations sep
                              WHERE sep.EmployeeId = emp.Id AND sep.TenantId = @TenantId AND sep.IsDeleted = 0
                              ORDER BY sep.SubmissionDate DESC)
LEFT JOIN Sponsors sponsor
       ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0
LEFT JOIN SponsorSignatories sponsorSignatory
       ON sponsorSignatory.SponsorId = sponsor.Id AND sponsorSignatory.TenantId = @TenantId AND sponsorSignatory.IsDeleted = 0
LEFT JOIN Signatories signatory
       ON signatory.Id = sponsorSignatory.SignatoryId AND signatory.TenantId = @TenantId AND signatory.IsDeleted = 0
LEFT JOIN GovtJobTitles govtJobTitle
       ON govtJobTitle.Id = empSponsor.GovtJobTitleId AND govtJobTitle.TenantId = @TenantId AND govtJobTitle.IsDeleted = 0
LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
LEFT JOIN Nationalities nationSponsor
       ON nationSponsor.Id = sponsor.NationalityId AND nationSponsor.TenantId = @TenantId AND nationSponsor.IsDeleted = 0
LEFT JOIN EmployeeVisaInfos empVisa
       ON empVisa.Id = (SELECT TOP (1) evi.Id FROM EmployeeVisaInfos evi
                        WHERE evi.EmployeeId = emp.Id AND evi.TenantId = @TenantId AND evi.IsDeleted = 0
                        ORDER BY evi.StartDate DESC)
LEFT JOIN BusinessTypes businessType
       ON businessType.Id = sponsor.BusinessTypeId AND businessType.TenantId = @TenantId AND businessType.IsDeleted = 0
WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code = @PrmEmpCode";

        // Identical to PAManpowerSql.SignatoryList; kept per-report so a future divergence is a local edit.
        public const string SignatoryList = @"SELECT
      emp.Code                              AS EmpCode
    , signatory.Id                          AS SignatoryOID
    , signatory.NameAr
    , signatory.NameEn
FROM Sponsors sponsor
INNER JOIN SponsorSignatories sponsorSignatory
        ON sponsorSignatory.SponsorId = sponsor.Id AND sponsorSignatory.TenantId = @TenantId AND sponsorSignatory.IsDeleted = 0
INNER JOIN Signatories signatory
        ON signatory.Id = sponsorSignatory.SignatoryId AND signatory.TenantId = @TenantId AND signatory.IsDeleted = 0
INNER JOIN EmployeeSponsors empSponsor
        ON empSponsor.SponsorId = sponsor.Id AND empSponsor.TenantId = @TenantId AND empSponsor.IsDeleted = 0
INNER JOIN Employees emp
        ON emp.Id = empSponsor.EmployeeId AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
WHERE sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0";
    }

    /// <summary>Raw SQL used by rptVisitorSponsorshipComplianceAcknowledgment -- visitor sponsorship compliance acknowledgment.</summary>
    public static class VisitorSponsorshipComplianceAcknowledgmentSql
    {
        // The same query as VisitVisaDeclarationAndPledgeSql.Query with the legacy''s own two differences
        // preserved: it projects Sponsor.UnifiedNum and does NOT project the sponsor nationality pair.
        // Same FEATURE LOST note for Sponsor.CivilId.
        public const string Query = @"SELECT
      emp.Code                              AS EmpCode
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.JoiningDate
    , emp.CivilID
    , empSeparation.LastDayInService
    , empVisa.CurrentFileNo
    , sponsor.NameEn                        AS SponsorNameEn
    , sponsor.NameAr                        AS SponsorNameAr
    , sponsor.GovernorateId                 AS Governate
    , CAST(NULL AS BIGINT)                  AS Sponsor_CivilId
    , sponsor.BusinessOwnerNameEn
    , sponsor.BusinessOwnerNameAr
    , sponsor.UnifiedNum
    , signatory.NameEn                      AS SignatoryNameEn
    , signatory.NameAr                      AS SignatoryNameAr
    , signatory.CivilId                     AS Signatory_CivilId
    , businessType.NameEn                   AS BusinessTypeEn
    , businessType.NameAr                   AS BusinessTypeAr
    , nationality.NameEn                    AS NationalityEn
    , nationality.NameAr                    AS NationalityAr
    , govtJobTitle.NameEn                   AS GovtJobTitleEn
    , govtJobTitle.NameAr                   AS GovtJobTitleAr
    , empSponsor.ContractSalary
    , empSponsor.StartDate                  AS GovtContractStartDate
    , empSponsor.EndDate                    AS GovtContractEndDate
    , empSubGroupAbsence.TotalDays          AS AnnualPaidLeaveDays
FROM Employees emp
LEFT JOIN EmployeeOrgAssignments empOrg
       ON empOrg.EmployeeId = emp.Id AND empOrg.TenantId = @TenantId AND empOrg.IsDeleted = 0
      AND empOrg.StartDate <= GETDATE() AND empOrg.EndDate >= GETDATE()
LEFT JOIN EmployeeSubGroups empSubGroup
       ON empSubGroup.Id = empOrg.EmployeeSubGroupId AND empSubGroup.TenantId = @TenantId AND empSubGroup.IsDeleted = 0
LEFT JOIN EmployeeSubGroupAbsences empSubGroupAbsence
       ON empSubGroupAbsence.EmployeeSubGroupId = empSubGroup.Id
      AND empSubGroupAbsence.TenantId = @TenantId AND empSubGroupAbsence.IsDeleted = 0
      AND empSubGroupAbsence.AbsenceTypeId IN (SELECT at.Id FROM AbsenceTypes at
                                               WHERE at.NameEn = 'Annual Paid Leave' AND at.TenantId = @TenantId AND at.IsDeleted = 0)
LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.Id = (SELECT TOP (1) es.Id FROM EmployeeSponsors es
                           WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
                           ORDER BY es.StartDate DESC)
LEFT JOIN EmployeeSeparations empSeparation
       ON empSeparation.Id = (SELECT TOP (1) sep.Id FROM EmployeeSeparations sep
                              WHERE sep.EmployeeId = emp.Id AND sep.TenantId = @TenantId AND sep.IsDeleted = 0
                              ORDER BY sep.SubmissionDate DESC)
LEFT JOIN Sponsors sponsor
       ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0
LEFT JOIN SponsorSignatories sponsorSignatory
       ON sponsorSignatory.SponsorId = sponsor.Id AND sponsorSignatory.TenantId = @TenantId AND sponsorSignatory.IsDeleted = 0
LEFT JOIN Signatories signatory
       ON signatory.Id = sponsorSignatory.SignatoryId AND signatory.TenantId = @TenantId AND signatory.IsDeleted = 0
LEFT JOIN GovtJobTitles govtJobTitle
       ON govtJobTitle.Id = empSponsor.GovtJobTitleId AND govtJobTitle.TenantId = @TenantId AND govtJobTitle.IsDeleted = 0
LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
LEFT JOIN EmployeeVisaInfos empVisa
       ON empVisa.Id = (SELECT TOP (1) evi.Id FROM EmployeeVisaInfos evi
                        WHERE evi.EmployeeId = emp.Id AND evi.TenantId = @TenantId AND evi.IsDeleted = 0
                        ORDER BY evi.StartDate DESC)
LEFT JOIN BusinessTypes businessType
       ON businessType.Id = sponsor.BusinessTypeId AND businessType.TenantId = @TenantId AND businessType.IsDeleted = 0
WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND emp.Code = @PrmEmpCode";

        // Identical to PAManpowerSql.SignatoryList; kept per-report so a future divergence is a local edit.
        public const string SignatoryList = @"SELECT
      emp.Code                              AS EmpCode
    , signatory.Id                          AS SignatoryOID
    , signatory.NameAr
    , signatory.NameEn
FROM Sponsors sponsor
INNER JOIN SponsorSignatories sponsorSignatory
        ON sponsorSignatory.SponsorId = sponsor.Id AND sponsorSignatory.TenantId = @TenantId AND sponsorSignatory.IsDeleted = 0
INNER JOIN Signatories signatory
        ON signatory.Id = sponsorSignatory.SignatoryId AND signatory.TenantId = @TenantId AND signatory.IsDeleted = 0
INNER JOIN EmployeeSponsors empSponsor
        ON empSponsor.SponsorId = sponsor.Id AND empSponsor.TenantId = @TenantId AND empSponsor.IsDeleted = 0
INNER JOIN Employees emp
        ON emp.Id = empSponsor.EmployeeId AND emp.TenantId = @TenantId AND emp.IsDeleted = 0
WHERE sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0";
    }

    /// <summary>Raw SQL used by rptWFVisaRequestForm -- work-visa request form.</summary>
    public static class VisaRequestFormSql
    {
        // *** THIS QUERY HAD THREE BROKEN JOINS IN THE LEGACY SOURCE AND THEY WERE REPAIRED, NOT PORTED. ***
        // The legacy joins were:
        //     inner join EmployeeDocument ed on ed.Employee = Employee.Code       -- int FK = varchar code
        //     inner join Nationality nation on nation.Code  = Employee.Code       -- nationality by emp code
        //     inner join Sponsor    sponsor on sponsor.Code = Employee.Code       -- sponsor by emp code
        // All three compare an employee CODE against an unrelated key. They are type errors, not layout
        // quirks, and cannot be carried over: in HCMv3 they would compare int to nvarchar and throw. Each is
        // repaired to the evidently intended foreign key (ed.EmployeeId, emp.NationalityId, and Sponsor via
        // EmployeeSponsors) and widened to LEFT JOIN, so a missing sponsor blanks one box rather than
        // suppressing the whole statutory form.
        // *** DELIBERATE DEVIATION -- needs sign-off against a known-good legacy output. ***
        //
        // Also:
        //  * Employees has no POBox column in HCMv3 (Sponsors does). Projected as NULL. *** FEATURE LOST. ***
        //  * Title/Nationality/Religion/MaritalStatus/Governate/Country/Qualification are projected as their
        //    raw <Entity>Id values under the legacy column names, because the layout binds the raw ints --
        //    CalcQualification in particular switches on [Qualification] == 1..5.
        //    *** SEMANTIC DEVIATION: those literals were legacy Qualification primary keys. HCMv3
        //    Qualifications has 22 rows with different ids, so the Arabic qualification text this form
        //    prints will be wrong until the mapping is re-based on Qualifications.Code. ***
        public const string Query = @"SELECT
      emp.Id                                AS OID
    , emp.Code
    , emp.FirstNameEn
    , emp.SecondNameEn
    , emp.ThirdNameEn
    , emp.LastNameEn
    , emp.FirstNameAr
    , emp.SecondNameAr
    , emp.ThirdNameAr
    , emp.LastNameAr
    , emp.FullNameEn
    , emp.FullNameAr
    , emp.TitleId                           AS Title
    , emp.Status
    , emp.BirthDate
    , emp.JoiningDate
    , emp.NationalityId                     AS Nationality
    , nation.NameAr                         AS [Nationality Name]
    , emp.ReligionId                        AS Religion
    , emp.Gender
    , emp.MaritalStatusId                   AS MaritalStatus
    , CAST(NULL AS VARBINARY(MAX))        AS Photo   -- images are on disk now; this form never printed it
    , emp.FlatNumn
    , emp.FloorNum
    , emp.BuildingNum
    , emp.StreetNum
    , emp.BlockNum
    , CAST(NULL AS NVARCHAR(50))            AS POBox
    , emp.ZipCode
    , emp.City
    , emp.GovernorateId                     AS Governate
    , emp.CountryId                         AS Country
    , emp.Telephone
    , emp.Mobile
    , emp.CurrentAddress
    , emp.PermanentAddress
    , emp.FatherNameEn
    , emp.FatherNameAr
    , emp.MotherNameEn
    , emp.MotherNameAr
    , emp.QualificationId                   AS Qualification
    , emp.BloodGroup
    , emp.CivilID
    , empDocPassport.DocumentNum            AS PassportNum
    , empVisa.CurrentContractSalary
    , empVisa.NewProfesion                  AS profession
    , empVisa.TypeOfPassport
    , empVisa.IssuePlacePassport
    , religion.Id                           AS Religion_OID
    , religion.Code                         AS Religion_Code
    , religion.NameEn                       AS Religion_Name
    , religion.NameAr                       AS Religion_NameAR
    , sponsor.NameEn                        AS [Company Name]
    , ed.IssueDate
    , ed.ExpiryDate
FROM Employees emp
INNER JOIN EmployeeVisaInfos empVisa
        ON empVisa.EmployeeId = emp.Id AND empVisa.TenantId = @TenantId AND empVisa.IsDeleted = 0
INNER JOIN Religions religion
        ON religion.Id = emp.ReligionId AND religion.TenantId = @TenantId AND religion.IsDeleted = 0
LEFT JOIN EmployeeDocuments ed
       ON ed.EmployeeId = emp.Id AND ed.TenantId = @TenantId AND ed.IsDeleted = 0
LEFT JOIN Nationalities nation
       ON nation.Id = emp.NationalityId AND nation.TenantId = @TenantId AND nation.IsDeleted = 0
LEFT JOIN EmployeeDocuments empDocPassport
       ON empDocPassport.Id = (SELECT TOP (1) d.Id FROM EmployeeDocuments d
                               WHERE d.EmployeeId = emp.Id AND d.TenantId = @TenantId AND d.IsDeleted = 0
                                 AND d.DocumentTypeId IN (SELECT TOP (1) dt.Id FROM DocumentTypes dt
                                                          WHERE dt.NameEn LIKE '%passport%' AND dt.TenantId = @TenantId AND dt.IsDeleted = 0)
                               ORDER BY d.ExpiryDate DESC)
LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.EmployeeId = emp.Id AND empSponsor.TenantId = @TenantId AND empSponsor.IsDeleted = 0
LEFT JOIN Sponsors sponsor
       ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0
WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0";
    }

    /// <summary>Raw SQL used by rptVisaApplicationShounEn -- Shoun visa application (was an XAF CollectionDataSource).</summary>
    public static class VisaApplicationShounEnSql
    {
        // NEW QUERY -- the legacy report had no SQL at all. It bound a
        // DevExpress.Persistent.Base.ReportsV2.CollectionDataSource straight onto the XPO entity
        // HCM10.Module.BusinessObjects.PersonnelAdministration.Employee and walked navigation properties
        // (Nationality.NameAr, Religion.NameAr, BasicPay.Amount, OrgAssignment.OrganizationUnit.NameAr,
        // Sponsor.*, VisaInfo.*).
        //
        // A DevExpress-constructed report in HCMv3 has no XPO session and no DI container, so the object
        // source is replaced by a SqlDataSource and every navigation path is flattened into a column. The
        // layout''s XRBinding member names are updated to match (see the report class remarks).
        public const string Query = @"SELECT
      emp.Code
    , emp.FirstNameEn
    , emp.SecondNameEn
    , emp.ThirdNameEn
    , emp.LastNameEn
    , emp.FirstNameAr
    , emp.SecondNameAr
    , emp.ThirdNameAr
    , emp.LastNameAr
    , emp.BirthDate
    , emp.Gender
    , emp.BloodGroup
    , emp.CivilID
    , emp.Mobile
    , emp.PermanentAddress
    , empDoc.DocumentNum                    AS PassportNum
    , nationality.NameAr                    AS NationalityNameAr
    , religion.NameAr                       AS ReligionNameAr
    , basicPayDetail.Amount                 AS BasicPayAmount
    , orgUnitProfile.NameAr                 AS OrgUnitNameAr
    , sponsor.NameAr                        AS SponsorNameAr
    , empVisa.EntryDate                     AS VisaEntryDate
    , empVisa.EndDate                       AS VisaEndDate
FROM Employees emp
LEFT JOIN Nationalities nationality
       ON nationality.Id = emp.NationalityId AND nationality.TenantId = @TenantId AND nationality.IsDeleted = 0
LEFT JOIN Religions religion
       ON religion.Id = emp.ReligionId AND religion.TenantId = @TenantId AND religion.IsDeleted = 0
LEFT JOIN EmployeeOrgAssignments empOrg
       ON empOrg.EmployeeId = emp.Id AND empOrg.TenantId = @TenantId AND empOrg.IsDeleted = 0
      AND empOrg.StartDate <= GETDATE() AND empOrg.EndDate >= GETDATE()
LEFT JOIN AbpOrganizationUnits orgUnit
       ON orgUnit.Id = empOrg.OrganizationUnitId AND orgUnit.TenantId = @TenantId AND orgUnit.IsDeleted = 0
LEFT JOIN OrgUnitProfiles orgUnitProfile
       ON orgUnitProfile.OrganizationUnitId = orgUnit.Id AND orgUnitProfile.TenantId = @TenantId AND orgUnitProfile.IsDeleted = 0
LEFT JOIN EmployeeBasicPayDetails basicPayDetail
       ON basicPayDetail.EmployeeBasicPayId =
              (SELECT TOP (1) ebp.Id FROM EmployeeBasicPays ebp
               WHERE ebp.EmployeeId = emp.Id AND ebp.TenantId = @TenantId AND ebp.IsDeleted = 0
               ORDER BY ebp.EndDate DESC)
      AND basicPayDetail.TenantId = @TenantId AND basicPayDetail.IsDeleted = 0
      AND basicPayDetail.WageTypeId IN (SELECT wt.Id FROM WageTypes wt
                                        WHERE wt.Code = '1000' AND wt.TenantId = @TenantId AND wt.IsDeleted = 0)
LEFT JOIN EmployeeSponsors empSponsor
       ON empSponsor.Id = (SELECT TOP (1) es.Id FROM EmployeeSponsors es
                           WHERE es.EmployeeId = emp.Id AND es.TenantId = @TenantId AND es.IsDeleted = 0
                           ORDER BY es.EndDate DESC)
LEFT JOIN Sponsors sponsor
       ON sponsor.Id = empSponsor.SponsorId AND sponsor.TenantId = @TenantId AND sponsor.IsDeleted = 0
LEFT JOIN EmployeeVisaInfos empVisa
       ON empVisa.Id = (SELECT TOP (1) evi.Id FROM EmployeeVisaInfos evi
                        WHERE evi.EmployeeId = emp.Id AND evi.TenantId = @TenantId AND evi.IsDeleted = 0
                        ORDER BY evi.StartDate DESC)
LEFT JOIN EmployeeDocuments empDoc
       ON empDoc.Id = (SELECT TOP (1) ed.Id FROM EmployeeDocuments ed
                       LEFT JOIN DocumentTypes dt ON dt.Id = ed.DocumentTypeId AND dt.TenantId = @TenantId AND dt.IsDeleted = 0
                       WHERE ed.EmployeeId = emp.Id AND ed.TenantId = @TenantId AND ed.IsDeleted = 0
                         AND dt.NameEn LIKE '%Passport%'
                       ORDER BY ed.ExpiryDate DESC)
WHERE emp.TenantId = @TenantId
  AND emp.IsDeleted = 0
  AND ((ISNULL(@EmployeeCode, '') <> '' AND emp.Code = @EmployeeCode) OR (ISNULL(@EmployeeCode, '') = ''))";
    }
}
