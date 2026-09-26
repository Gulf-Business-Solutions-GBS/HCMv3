/* =====================================================================
   03 - DEMO DATA: two fictional employees, ready for payroll and workflow
   ---------------------------------------------------------------------
   Run in SSMS against HCMv3Db, AFTER 01 and 02.

   Everything created here is invented. Every master-data row is suffixed
   "(Demo)" so it is easy to spot in dropdowns and easy to delete later.
   The teardown script at the bottom removes all of it.

   Safe to re-run: nothing is duplicated on a second execution.

   Creates:
     - Supporting master data (currency, payroll period and area,
       department, job titles, employee group and sub-group, work
       location, contract type, nationality)
     - 2 employees
     - 2 ESS login accounts, sharing the admin password
     - Org assignment, basic pay, contract and payroll status for each

   Login accounts created (password is the SAME as your admin account,
   i.e. the default 123qwe unless you have already changed admin's):
     yusuf.demo
     maria.demo

   NOTE: the Arabic name fields use transliterated text rather than real
   Arabic script, purely to keep this file plain ASCII and immune to
   encoding problems. Replace them in the UI if Arabic display matters.
   ===================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @TenantId int = 1;          -- the Default tenant
DECLARE @Now datetime2 = SYSUTCDATETIME();
DECLARE @Forever datetime2 = '9999-12-31';

IF NOT EXISTS (SELECT 1 FROM AbpTenants WHERE Id = @TenantId)
BEGIN
    RAISERROR('Tenant %d not found. Check the tenant id at the top of this script.', 16, 1, @TenantId);
    RETURN;
END

BEGIN TRANSACTION;

/* ---------------------------------------------------------------------
   1. Currency, payroll period, payroll area
   --------------------------------------------------------------------- */
DECLARE @CurrencyId int, @PeriodId int, @PayrollAreaId int;

SELECT @CurrencyId = Id FROM Currencies WHERE TenantId=@TenantId AND NameEn='Kuwaiti Dinar (Demo)';
IF @CurrencyId IS NULL
BEGIN
    INSERT INTO Currencies (TenantId, NameEn, NameAr, DecimalCount, CreationTime, IsDeleted)
    VALUES (@TenantId, 'Kuwaiti Dinar (Demo)', 'Dinar Kuwaiti (Demo)', 3, @Now, 0);
    SET @CurrencyId = SCOPE_IDENTITY();
END

SELECT @PeriodId = Id FROM PayrollPeriods WHERE TenantId=@TenantId AND NameEn='Monthly (Demo)';
IF @PeriodId IS NULL
BEGIN
    INSERT INTO PayrollPeriods (TenantId, NameEn, NameAr, CreationTime, IsDeleted)
    VALUES (@TenantId, 'Monthly (Demo)', 'Shahri (Demo)', @Now, 0);
    SET @PeriodId = SCOPE_IDENTITY();
END

SELECT @PayrollAreaId = Id FROM PayrollAreas WHERE TenantId=@TenantId AND Name='Head Office Payroll (Demo)';
IF @PayrollAreaId IS NULL
BEGIN
    INSERT INTO PayrollAreas (TenantId, Name, PayrollPeriodId, CurrencyId, CutOffDay, CreationTime, IsDeleted)
    VALUES (@TenantId, 'Head Office Payroll (Demo)', @PeriodId, @CurrencyId, 25, @Now, 0);
    SET @PayrollAreaId = SCOPE_IDENTITY();
END

/* ---------------------------------------------------------------------
   2. Nationality, job titles, work location, contract type
   --------------------------------------------------------------------- */
DECLARE @NationalityId int, @JobAnalystId int, @JobOfficerId int,
        @WorkLocationId int, @ContractTypeId int;

SELECT @NationalityId = Id FROM Nationalities WHERE TenantId=@TenantId AND NameEn='Northmarch (Demo)';
IF @NationalityId IS NULL
BEGIN
    INSERT INTO Nationalities (TenantId, NameEn, NameAr, CreationTime, IsDeleted)
    VALUES (@TenantId, 'Northmarch (Demo)', 'Northmarch (Demo)', @Now, 0);
    SET @NationalityId = SCOPE_IDENTITY();
END

SELECT @JobAnalystId = Id FROM JobTitles WHERE TenantId=@TenantId AND NameEn='Senior Systems Analyst (Demo)';
IF @JobAnalystId IS NULL
BEGIN
    INSERT INTO JobTitles (TenantId, NameEn, NameAr, CreationTime, IsDeleted)
    VALUES (@TenantId, 'Senior Systems Analyst (Demo)', 'Muhallil Nuzum (Demo)', @Now, 0);
    SET @JobAnalystId = SCOPE_IDENTITY();
END

SELECT @JobOfficerId = Id FROM JobTitles WHERE TenantId=@TenantId AND NameEn='Payroll Officer (Demo)';
IF @JobOfficerId IS NULL
BEGIN
    INSERT INTO JobTitles (TenantId, NameEn, NameAr, CreationTime, IsDeleted)
    VALUES (@TenantId, 'Payroll Officer (Demo)', 'Masool Rawatib (Demo)', @Now, 0);
    SET @JobOfficerId = SCOPE_IDENTITY();
END

SELECT @WorkLocationId = Id FROM WorkLocations WHERE TenantId=@TenantId AND NameEn='Riverside Tower (Demo)';
IF @WorkLocationId IS NULL
BEGIN
    INSERT INTO WorkLocations (TenantId, NameEn, NameAr, Latitude, Longitude, GeoRange, IsActive, CreationTime, IsDeleted)
    VALUES (@TenantId, 'Riverside Tower (Demo)', 'Burj Riverside (Demo)', 29.3759, 47.9774, 250, 1, @Now, 0);
    SET @WorkLocationId = SCOPE_IDENTITY();
END

SELECT @ContractTypeId = Id FROM ContractTypes WHERE TenantId=@TenantId AND NameEn='Unlimited Term (Demo)';
IF @ContractTypeId IS NULL
BEGIN
    INSERT INTO ContractTypes (TenantId, NameEn, NameAr, CreationTime, IsDeleted)
    VALUES (@TenantId, 'Unlimited Term (Demo)', 'Aqd Ghayr Mahdood (Demo)', @Now, 0);
    SET @ContractTypeId = SCOPE_IDENTITY();
END

/* ---------------------------------------------------------------------
   3. Employee group and sub-group (the "grade" structure)
   --------------------------------------------------------------------- */
DECLARE @GroupId int, @SubGroupId int;

SELECT @GroupId = Id FROM EmployeeGroups WHERE TenantId=@TenantId AND NameEn='Professional Staff (Demo)';
IF @GroupId IS NULL
BEGIN
    INSERT INTO EmployeeGroups
        (TenantId, NameEn, NameAr, IndemnityMaxNumber, IndemnityMaxUnit, IndemnityMaxAccruePercentage, CreationTime, IsDeleted)
    VALUES
        (@TenantId, 'Professional Staff (Demo)', 'Muwazzafoon (Demo)', 18, 0, 100, @Now, 0);
    SET @GroupId = SCOPE_IDENTITY();
END

SELECT @SubGroupId = Id FROM EmployeeSubGroups WHERE TenantId=@TenantId AND NameEn='Grade B - Professional (Demo)';
IF @SubGroupId IS NULL
BEGIN
    INSERT INTO EmployeeSubGroups
        (TenantId, EmployeeGroupId, NameEn, NameAr, PayOvertime, AirTicketPayType,
         MaxHealthReimburseAmount, HealthInsurancePeriod, AutoPayAirTicket,
         MaxDependantAirTicketCount, MaxDependantHealthReimburseCount, CreationTime, IsDeleted)
    VALUES
        (@TenantId, @GroupId, 'Grade B - Professional (Demo)', 'Daraja B (Demo)', 1, 0,
         500, 12, 0, 2, 3, @Now, 0);
    SET @SubGroupId = SCOPE_IDENTITY();
END

/* ---------------------------------------------------------------------
   4. Department (organisation unit)
   --------------------------------------------------------------------- */
DECLARE @OrgUnitId bigint;

SELECT @OrgUnitId = Id FROM AbpOrganizationUnits
 WHERE TenantId=@TenantId AND DisplayName='Corporate Services (Demo)';
IF @OrgUnitId IS NULL
BEGIN
    DECLARE @OuCode nvarchar(95) =
        RIGHT('00000' + CAST(ISNULL((SELECT COUNT(*) FROM AbpOrganizationUnits WHERE TenantId=@TenantId AND ParentId IS NULL),0) + 1 AS varchar), 5);

    INSERT INTO AbpOrganizationUnits (TenantId, ParentId, Code, DisplayName, CreationTime, IsDeleted)
    VALUES (@TenantId, NULL, @OuCode, 'Corporate Services (Demo)', @Now, 0);
    SET @OrgUnitId = SCOPE_IDENTITY();
END

/* ---------------------------------------------------------------------
   5. ESS login accounts

      The password column is copied from the tenant's admin account, so
      these users sign in with exactly the same password as admin. There
      is no way to compute a valid password hash in plain SQL, and
      copying an existing one is the reliable alternative.
   --------------------------------------------------------------------- */
DECLARE @AdminPassword nvarchar(256);
SELECT TOP 1 @AdminPassword = Password
  FROM AbpUsers WHERE TenantId=@TenantId AND UserName='admin' AND IsDeleted=0;

IF @AdminPassword IS NULL
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('No admin user found for tenant %d - cannot create demo logins.', 16, 1, @TenantId);
    RETURN;
END

DECLARE @UserYusuf bigint, @UserMaria bigint;

SELECT @UserYusuf = Id FROM AbpUsers WHERE TenantId=@TenantId AND UserName='yusuf.demo';
IF @UserYusuf IS NULL
BEGIN
    INSERT INTO AbpUsers
        (TenantId, UserName, NormalizedUserName, Name, Surname,
         EmailAddress, NormalizedEmailAddress, Password,
         IsActive, IsEmailConfirmed, IsPhoneNumberConfirmed, IsTwoFactorEnabled,
         IsLockoutEnabled, ShouldChangePasswordOnNextLogin, AccessFailedCount,
         CreationTime, IsDeleted)
    VALUES
        (@TenantId, 'yusuf.demo', 'YUSUF.DEMO', 'Yusuf', 'Al-Rashed',
         'yusuf.demo@example.invalid', 'YUSUF.DEMO@EXAMPLE.INVALID', @AdminPassword,
         1, 1, 0, 0, 1, 0, 0, @Now, 0);
    SET @UserYusuf = SCOPE_IDENTITY();
END

SELECT @UserMaria = Id FROM AbpUsers WHERE TenantId=@TenantId AND UserName='maria.demo';
IF @UserMaria IS NULL
BEGIN
    INSERT INTO AbpUsers
        (TenantId, UserName, NormalizedUserName, Name, Surname,
         EmailAddress, NormalizedEmailAddress, Password,
         IsActive, IsEmailConfirmed, IsPhoneNumberConfirmed, IsTwoFactorEnabled,
         IsLockoutEnabled, ShouldChangePasswordOnNextLogin, AccessFailedCount,
         CreationTime, IsDeleted)
    VALUES
        (@TenantId, 'maria.demo', 'MARIA.DEMO', 'Maria', 'Fernandes',
         'maria.demo@example.invalid', 'MARIA.DEMO@EXAMPLE.INVALID', @AdminPassword,
         1, 1, 0, 0, 1, 0, 0, @Now, 0);
    SET @UserMaria = SCOPE_IDENTITY();
END

/* ---------------------------------------------------------------------
   6. The two employees
   --------------------------------------------------------------------- */
DECLARE @EmpYusuf int, @EmpMaria int;

SELECT @EmpYusuf = Id FROM Employees WHERE TenantId=@TenantId AND CivilID=290010112233;
IF @EmpYusuf IS NULL
BEGIN
    INSERT INTO Employees
        (TenantId, FirstNameEn, LastNameEn, FirstNameAr, LastNameAr,
         Status, BirthDate, Gender, NationalityId,
         Telephone, Mobile, PermanentTelephone, CivilID,
         EmploymentType, JoiningDate, ProbationDays, ProbationEndDate,
         UserId, CreationTime, IsDeleted)
    VALUES
        (@TenantId, 'Yusuf', 'Al-Rashed', 'Yusuf', 'Al-Rashed',
         1, '1989-04-14', 0, @NationalityId,
         22501100, 96550110022, 22501100, 290010112233,
         0, '2021-03-01', 90, '2021-05-30',
         @UserYusuf, @Now, 0);
    SET @EmpYusuf = SCOPE_IDENTITY();
END

SELECT @EmpMaria = Id FROM Employees WHERE TenantId=@TenantId AND CivilID=286020198877;
IF @EmpMaria IS NULL
BEGIN
    INSERT INTO Employees
        (TenantId, FirstNameEn, LastNameEn, FirstNameAr, LastNameAr,
         Status, BirthDate, Gender, NationalityId,
         Telephone, Mobile, PermanentTelephone, CivilID,
         EmploymentType, JoiningDate, ProbationDays, ProbationEndDate,
         UserId, CreationTime, IsDeleted)
    VALUES
        (@TenantId, 'Maria', 'Fernandes', 'Maria', 'Fernandes',
         1, '1986-11-02', 1, @NationalityId,
         22501101, 96550110033, 22501101, 286020198877,
         0, '2019-09-15', 90, '2019-12-14',
         @UserMaria, @Now, 0);
    SET @EmpMaria = SCOPE_IDENTITY();
END

/* ---------------------------------------------------------------------
   7. Org assignment, basic pay, contract, payroll status
   --------------------------------------------------------------------- */
DECLARE @Start datetime2 = '2024-01-01';

-- Yusuf: 1,250.000 per month
IF NOT EXISTS (SELECT 1 FROM EmployeeOrgAssignments WHERE EmployeeId=@EmpYusuf)
    INSERT INTO EmployeeOrgAssignments
        (TenantId, EmployeeId, OrganizationUnitId, JobTitleId, EmployeeGroupId,
         EmployeeSubGroupId, PayrollAreaId, WorkLocationId,
         StartDate, EndDate, CreationTime, IsDeleted)
    VALUES
        (@TenantId, @EmpYusuf, @OrgUnitId, @JobAnalystId, @GroupId,
         @SubGroupId, @PayrollAreaId, @WorkLocationId,
         @Start, @Forever, @Now, 0);

IF NOT EXISTS (SELECT 1 FROM EmployeeBasicPays WHERE EmployeeId=@EmpYusuf)
    INSERT INTO EmployeeBasicPays
        (TenantId, EmployeeId, StartDate, EndDate, Amount, PayType, PayInterval, CreationTime, IsDeleted)
    VALUES
        (@TenantId, @EmpYusuf, @Start, @Forever, 1250.000, 1, 0, @Now, 0);

IF NOT EXISTS (SELECT 1 FROM EmployeeContracts WHERE EmployeeId=@EmpYusuf)
    INSERT INTO EmployeeContracts
        (TenantId, EmployeeId, ContractTypeId, StartDate, EndDate, CreationTime, IsDeleted)
    VALUES
        (@TenantId, @EmpYusuf, @ContractTypeId, @Start, @Forever, @Now, 0);

-- Maria: 850.000 per month
IF NOT EXISTS (SELECT 1 FROM EmployeeOrgAssignments WHERE EmployeeId=@EmpMaria)
    INSERT INTO EmployeeOrgAssignments
        (TenantId, EmployeeId, OrganizationUnitId, JobTitleId, EmployeeGroupId,
         EmployeeSubGroupId, PayrollAreaId, WorkLocationId,
         StartDate, EndDate, CreationTime, IsDeleted)
    VALUES
        (@TenantId, @EmpMaria, @OrgUnitId, @JobOfficerId, @GroupId,
         @SubGroupId, @PayrollAreaId, @WorkLocationId,
         @Start, @Forever, @Now, 0);

IF NOT EXISTS (SELECT 1 FROM EmployeeBasicPays WHERE EmployeeId=@EmpMaria)
    INSERT INTO EmployeeBasicPays
        (TenantId, EmployeeId, StartDate, EndDate, Amount, PayType, PayInterval, CreationTime, IsDeleted)
    VALUES
        (@TenantId, @EmpMaria, @Start, @Forever, 850.000, 1, 0, @Now, 0);

IF NOT EXISTS (SELECT 1 FROM EmployeeContracts WHERE EmployeeId=@EmpMaria)
    INSERT INTO EmployeeContracts
        (TenantId, EmployeeId, ContractTypeId, StartDate, EndDate, CreationTime, IsDeleted)
    VALUES
        (@TenantId, @EmpMaria, @ContractTypeId, @Start, @Forever, @Now, 0);

-- Payroll status rows: these tell the payroll engine how far each
-- employee has been processed. Setting them to the period start means
-- payroll has never run, which is what you want for a fresh demo.
INSERT INTO EmployeePayrollStatuses
    (TenantId, EmployeeId, EarliestRetroAccountingDate, AccountedTo, RunPayrollUpto,
     EarliestMasterDataChange, DoNoAccountAfter, PersonLocked, PayrollCorrection,
     EarliestRecalculationDate, PersonalCalendarFrom, NextTimeEvaluationFrom,
     CreationTime, IsDeleted)
SELECT @TenantId, e.Id, @Start, @Start, @Forever,
       @Start, @Forever, 0, 0,
       @Start, @Start, @Start,
       @Now, 0
FROM (VALUES (@EmpYusuf), (@EmpMaria)) AS e(Id)
WHERE NOT EXISTS (SELECT 1 FROM EmployeePayrollStatuses s WHERE s.EmployeeId = e.Id);

COMMIT TRANSACTION;

/* ---------------------------------------------------------------------
   8. Confirmation
   --------------------------------------------------------------------- */
SELECT e.Id            AS EmployeeId,
       e.FirstNameEn + ' ' + e.LastNameEn AS EmployeeName,
       u.UserName      AS LoginName,
       j.NameEn        AS JobTitle,
       o.DisplayName   AS Department,
       sg.NameEn       AS Grade,
       bp.Amount       AS MonthlySalary,
       pa.Name         AS PayrollArea
FROM Employees e
LEFT JOIN AbpUsers u              ON u.Id  = e.UserId
LEFT JOIN EmployeeOrgAssignments a ON a.EmployeeId = e.Id
LEFT JOIN JobTitles j             ON j.Id  = a.JobTitleId
LEFT JOIN AbpOrganizationUnits o  ON o.Id  = a.OrganizationUnitId
LEFT JOIN EmployeeSubGroups sg    ON sg.Id = a.EmployeeSubGroupId
LEFT JOIN PayrollAreas pa         ON pa.Id = a.PayrollAreaId
LEFT JOIN EmployeeBasicPays bp    ON bp.EmployeeId = e.Id
WHERE e.CivilID IN (290010112233, 286020198877);

PRINT '';
PRINT '=====================================================';
PRINT ' Demo data ready.';
PRINT '';
PRINT ' Logins: yusuf.demo  /  maria.demo';
PRINT ' Password: same as your admin account.';
PRINT '=====================================================';
GO


/* =====================================================================
   TEARDOWN - removes everything this script created.
   Highlight from here down and run it only when you want the demo data
   gone. It leaves all other data untouched.
   =====================================================================

DECLARE @T int = 1;
DECLARE @Ids TABLE (Id int);
INSERT INTO @Ids SELECT Id FROM Employees WHERE CivilID IN (290010112233, 286020198877);

DELETE FROM EmployeePayrollStatuses WHERE EmployeeId IN (SELECT Id FROM @Ids);
DELETE FROM EmployeeContracts       WHERE EmployeeId IN (SELECT Id FROM @Ids);
DELETE FROM EmployeeBasicPays       WHERE EmployeeId IN (SELECT Id FROM @Ids);
DELETE FROM EmployeeOrgAssignments  WHERE EmployeeId IN (SELECT Id FROM @Ids);
DELETE FROM Employees               WHERE Id IN (SELECT Id FROM @Ids);
DELETE FROM AbpUsers                WHERE TenantId=@T AND UserName IN ('yusuf.demo','maria.demo');

DELETE FROM EmployeeSubGroups   WHERE TenantId=@T AND NameEn LIKE '%(Demo)';
DELETE FROM EmployeeGroups      WHERE TenantId=@T AND NameEn LIKE '%(Demo)';
DELETE FROM PayrollAreas        WHERE TenantId=@T AND Name   LIKE '%(Demo)';
DELETE FROM PayrollPeriods      WHERE TenantId=@T AND NameEn LIKE '%(Demo)';
DELETE FROM Currencies          WHERE TenantId=@T AND NameEn LIKE '%(Demo)';
DELETE FROM JobTitles           WHERE TenantId=@T AND NameEn LIKE '%(Demo)';
DELETE FROM WorkLocations       WHERE TenantId=@T AND NameEn LIKE '%(Demo)';
DELETE FROM ContractTypes       WHERE TenantId=@T AND NameEn LIKE '%(Demo)';
DELETE FROM Nationalities       WHERE TenantId=@T AND NameEn LIKE '%(Demo)';
DELETE FROM AbpOrganizationUnits WHERE TenantId=@T AND DisplayName LIKE '%(Demo)';

===================================================================== */
