using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The rest of legacy's Full &amp; Final save: the asset check, bonus pay, loans, the position, the login and the
/// sponsor visa quota (<see cref="IFullAndFinalExitService"/>), driven through the settlement service so the
/// order of legacy's steps is exercised too.
/// </summary>
// ReSharper disable once InconsistentNaming
public class FullAndFinalExitService_Tests : FullAndFinalTestBase
{
    private static readonly DateTime LastDay = new DateTime(2024, 6, 30);

    private readonly IFullAndFinalSettlementService _service;
    private readonly IFullAndFinalExitService _exitService;

    public FullAndFinalExitService_Tests()
    {
        _service = Resolve<IFullAndFinalSettlementService>();
        _exitService = Resolve<IFullAndFinalExitService>();
    }

    #region << Assets >>

    [Fact]
    public async Task Should_Refuse_While_An_Asset_Is_Still_Assigned()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedAsset(seeded.EmployeeId, new DateTime(2023, 1, 1), new DateTime(2025, 12, 31));

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.ValidateSaveAsync(Values(seeded.EmployeeId), null, null));

        ex.Message.ShouldBe("The employee has assets assigned. Please ensure all linked assets are cleared before processing the Full and Final payment.");
    }

    /// <summary>Legacy's window ends STRICTLY after the compensation date, so an asset returned that day is clear.</summary>
    [Fact]
    public async Task Should_Allow_An_Asset_Returned_On_The_Compensation_Date()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedAsset(seeded.EmployeeId, new DateTime(2023, 1, 1), LastDay);

        await Should.NotThrowAsync(() => _exitService.ValidateNoAssetsAsync(seeded.EmployeeId, LastDay));
    }

    #endregion

    #region << Bonus pay >>

    /// <summary>Legacy <c>SuggestedBonusPay</c>: the AVERAGE of past bonus payments, not their total.</summary>
    [Fact]
    public async Task Should_Suggest_The_Average_Of_Past_Bonus_Payments()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedBonusPayment(seeded.EmployeeId, new DateTime(2022, 1, 31), 300);
        SeedBonusPayment(seeded.EmployeeId, new DateTime(2023, 1, 31), 450);
        SeedBonusPayment(seeded.EmployeeId, new DateTime(2030, 1, 31), 9000); // after the last day: ignored

        (await _exitService.GetSuggestedBonusPayAsync(seeded.EmployeeId)).ShouldBe(375);
    }

    [Fact]
    public async Task Should_Refuse_More_Bonus_Than_Suggested()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedBonusPayment(seeded.EmployeeId, new DateTime(2023, 1, 31), 100);

        var values = Values(seeded.EmployeeId);
        values.CompensateBonusPay = 150;

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => _service.ValidateSaveAsync(values, null, null));

        ex.Message.ShouldBe("You cannot compensate BonusPay more than Suggested BonusPay.");
    }

    [Fact]
    public async Task Should_Pay_The_Bonus_As_A_Wage_1053_Payment()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedWageType("1053", "Bonus Pay");
        SeedWageType("1011", "Indemnity Pay Adjust");

        var values = Values(seeded.EmployeeId);
        values.CompensateBonusPay = 120;
        values.IndemnityPayAdjustment = 5; // see the pinned quirk below
        await _service.ApplyAsync(values, null);

        var payment = UsingDbContext(context => context.Set<EmployeePayment>()
            .Single(p => p.EmployeeId == seeded.EmployeeId && !p.IsDeleted && p.WageType.Code == "1053"));
        payment.Amount.ShouldBe(120);
        payment.PaymentDate.ShouldBe(LastDay);
    }

    /// <summary>
    /// LEGACY QUIRK, pinned: <c>AddBonusPay</c> gates its insert on the INDEMNITY PAY ADJUSTMENT (legacy line
    /// 1953), not on the bonus. With no adjustment the bonus is accepted and simply never paid.
    /// </summary>
    [Fact]
    public async Task Should_Not_Pay_The_Bonus_When_There_Is_No_Indemnity_Pay_Adjustment_As_Legacy_Does()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedWageType("1053", "Bonus Pay");

        var values = Values(seeded.EmployeeId);
        values.CompensateBonusPay = 120;
        await _service.ApplyAsync(values, null);

        UsingDbContext(context => context.Set<EmployeePayment>()
            .Any(p => p.EmployeeId == seeded.EmployeeId && !p.IsDeleted && p.WageType.Code == "1053").ShouldBeFalse());
    }

    #endregion

    #region << Loans >>

    [Fact]
    public async Task Should_Refuse_An_Entry_While_A_Loan_Is_Still_Open()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedLoan(seeded.EmployeeId, due: 600);

        var ex = await Should.ThrowAsync<UserFriendlyException>(
            () => _service.ValidateSaveAsync(Values(seeded.EmployeeId), null, null));

        ex.Message.ShouldContain("has pending loans");
    }

    [Fact]
    public async Task Should_Allow_The_Entry_When_It_Settles_The_Loans()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        SeedLoan(seeded.EmployeeId, due: 600);

        var values = Values(seeded.EmployeeId);
        values.SettleLoanInEndOfService = true;

        await Should.NotThrowAsync(() => _service.ValidateSaveAsync(values, null, null));
    }

    /// <summary>The balance is due less what was already recovered, and the settlement recovers exactly that.</summary>
    [Fact]
    public async Task Should_Recover_The_Remaining_Loan_Balance_On_The_Compensation_Date()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        var loanId = SeedLoan(seeded.EmployeeId, due: 600);
        SeedLoanRecovery(loanId, new DateTime(2024, 1, 31), 250, LoanRecoveryMethod.RecoveryThroughPayroll);

        var values = Values(seeded.EmployeeId);
        values.SettleLoanInEndOfService = true;
        await _service.ApplyAsync(values, null);

        UsingDbContext(context =>
        {
            var recovery = context.Set<EmployeeLoanRecovery>()
                .Single(r => r.EmployeeLoanId == loanId && !r.IsDeleted && r.LoanRecoveryMethod == LoanRecoveryMethod.RecoveryThroughEoS);
            recovery.RecoveryAmount.ShouldBe(350);
            recovery.RecoveryDate.ShouldBe(LastDay);

            context.Set<EmployeeLoan>().Find(loanId).BalanceDueAmount.ShouldBe(0);
        });
    }

    [Fact]
    public async Task Should_Undo_The_Loan_Settlement_On_Delete()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        var loanId = SeedLoan(seeded.EmployeeId, due: 600);

        var values = Values(seeded.EmployeeId);
        values.SettleLoanInEndOfService = true;
        await _service.ApplyAsync(values, null);

        await _service.UndoAsync(values);

        UsingDbContext(context =>
        {
            context.Set<EmployeeLoanRecovery>()
                .Any(r => r.EmployeeLoanId == loanId && !r.IsDeleted && r.LoanRecoveryMethod == LoanRecoveryMethod.RecoveryThroughEoS)
                .ShouldBeFalse();

            context.Set<EmployeeLoan>().Find(loanId).BalanceDueAmount.ShouldBe(600);
        });
    }

    #endregion

    #region << Position and login >>

    [Fact]
    public async Task Should_End_The_Position_Holding_On_The_Last_Day_In_Service()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        var relationshipId = SeedPositionHolder(seeded.EmployeeId, new DateTime(2020, 7, 1));

        await _service.ApplyAsync(Values(seeded.EmployeeId), null);

        UsingDbContext(context => context.Set<PositionRelationship>().Find(relationshipId).EndDate.ShouldBe(LastDay));
    }

    [Fact]
    public async Task Should_Switch_The_Login_Off_And_Back_On()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        var userId = LinkLogin(seeded.EmployeeId);

        await _service.ApplyAsync(Values(seeded.EmployeeId), null);
        UsingDbContext(context => context.Users.Single(u => u.Id == userId).IsActive.ShouldBeFalse());

        await _service.UndoAsync(Values(seeded.EmployeeId));
        UsingDbContext(context => context.Users.Single(u => u.Id == userId).IsActive.ShouldBeTrue());
    }

    #endregion

    #region << Sponsor / government job title quota >>

    [Fact]
    public async Task Should_Free_A_Visa_Place_When_The_Employee_Leaves()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        EnableSponsorQuota(true);
        var sponsorJobTitleId = SeedSponsorship(seeded.EmployeeId, citizen: false, accrued: 10, used: 4);

        await _service.ApplyAsync(Values(seeded.EmployeeId), null);

        UsingDbContext(context => context.Set<SponsorJobTitle>().Find(sponsorJobTitleId).NonCitizenQuotaUsed.ShouldBe(3));
    }

    [Fact]
    public async Task Should_Take_A_Visa_Place_Back_When_The_Settlement_Is_Deleted()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        EnableSponsorQuota(true);
        var sponsorJobTitleId = SeedSponsorship(seeded.EmployeeId, citizen: true, accrued: 10, used: 4);

        await _service.ApplyAsync(Values(seeded.EmployeeId), null);
        await _service.UndoAsync(Values(seeded.EmployeeId));

        UsingDbContext(context => context.Set<SponsorJobTitle>().Find(sponsorJobTitleId).CitizenQuotaUsed.ShouldBe(4));
    }

    [Fact]
    public async Task Should_Refuse_The_Delete_When_The_Sponsor_Has_No_Visa_Place_Left()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        EnableSponsorQuota(true);
        SeedSponsorship(seeded.EmployeeId, citizen: true, accrued: 1, used: 1);

        await _service.ApplyAsync(Values(seeded.EmployeeId), null); // frees one: used 1 -> 0
        UsingDbContext(context => context.Set<Employee>().Find(seeded.EmployeeId).Status = EmployeeStatus.Active);
        UsingDbContext(context => context.Set<SponsorJobTitle>().First().CitizenQuotaAccrued = 0);

        var ex = await Should.ThrowAsync<UserFriendlyException>(() => _exitService.UpdateSponsorQuotaAsync(seeded.EmployeeId));

        ex.Message.ShouldStartWith("Citizen GovtJobtitle quota not available.");
    }

    [Fact]
    public async Task Should_Leave_The_Quota_Alone_When_The_Setting_Is_Off()
    {
        var seeded = SeedLeaver(new DateTime(2020, 7, 1), LastDay, SeparationType.Termination);
        EnableSponsorQuota(false);
        var sponsorJobTitleId = SeedSponsorship(seeded.EmployeeId, citizen: false, accrued: 10, used: 4);

        await _service.ApplyAsync(Values(seeded.EmployeeId), null);

        UsingDbContext(context => context.Set<SponsorJobTitle>().Find(sponsorJobTitleId).NonCitizenQuotaUsed.ShouldBe(4));
    }

    #endregion

    #region << Seed helpers >>

    private static FullAndFinalValues Values(int employeeId) =>
        new FullAndFinalValues { EmployeeId = employeeId, CompensationDate = LastDay };

    private void SeedAsset(int employeeId, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            var asset = context.Set<Asset>().FirstOrDefault()
                        ?? context.Set<Asset>().Add(new Asset { TenantId = TenantId, Code = "A1", NameEn = "Laptop", NameAr = "حاسوب" }).Entity;
            context.SaveChanges();

            context.Set<EmployeeAsset>().Add(new EmployeeAsset
            {
                TenantId = TenantId, EmployeeId = employeeId, AssetId = asset.Id, StartDate = start, EndDate = end
            });
        });
    }

    private int SeedWageType(string code, string nameEn)
    {
        return UsingDbContext(context =>
        {
            var existing = context.Set<WageType>().FirstOrDefault(w => w.Code == code);
            if (existing != null)
            {
                return existing.Id;
            }

            var wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId, Code = code, NameEn = nameEn, NameAr = nameEn, PayrollOrder = 1, Status = true
            }).Entity;
            context.SaveChanges();
            return wageType.Id;
        });
    }

    private void SeedBonusPayment(int employeeId, DateTime paymentDate, double amount)
    {
        var wageTypeId = SeedWageType("1053", "Bonus Pay");

        UsingDbContext(context => context.Set<EmployeeNonPayrollTrans>().Add(new EmployeeNonPayrollTrans
        {
            TenantId = TenantId, EmployeeId = employeeId, WageTypeId = wageTypeId, PaymentDate = paymentDate, Amount = amount
        }));
    }

    private int SeedLoan(int employeeId, double due)
    {
        return UsingDbContext(context =>
        {
            var loanType = context.Set<LoanType>().FirstOrDefault()
                           ?? context.Set<LoanType>().Add(new LoanType { TenantId = TenantId, Code = "L1", NameEn = "Personal", NameAr = "شخصي" }).Entity;
            context.SaveChanges();

            var loan = context.Set<EmployeeLoan>().Add(new EmployeeLoan
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                LoanTypeId = loanType.Id,
                StartDate = new DateTime(2023, 1, 1),
                RecoveryStartDate = new DateTime(2023, 2, 1),
                EndDate = new DateTime(2026, 12, 31),
                ActualAmount = due,
                DueAmount = due,
                BalanceDueAmount = due,
                FixedAmount = 50
            }).Entity;
            context.SaveChanges();
            return loan.Id;
        });
    }

    private void SeedLoanRecovery(int loanId, DateTime date, double amount, LoanRecoveryMethod method)
    {
        UsingDbContext(context => context.Set<EmployeeLoanRecovery>().Add(new EmployeeLoanRecovery
        {
            TenantId = TenantId, EmployeeLoanId = loanId, RecoveryDate = date, RecoveryAmount = amount, LoanRecoveryMethod = method
        }));
    }

    private int SeedPositionHolder(int employeeId, DateTime startDate)
    {
        return UsingDbContext(context =>
        {
            var position = context.Set<Position>().FirstOrDefault()
                           ?? context.Set<Position>().Add(new Position { TenantId = TenantId, Code = "P1", NameEn = "Clerk", NameAr = "كاتب" }).Entity;
            context.SaveChanges();

            var relationship = context.Set<PositionRelationship>().Add(new PositionRelationship
            {
                TenantId = TenantId,
                PositionId = position.Id,
                EmployeeId = employeeId,
                RelationType = PositionRelationType.Holder,
                StartDate = startDate,
                EndDate = new DateTime(2099, 12, 31)
            }).Entity;
            context.SaveChanges();
            return relationship.Id;
        });
    }

    private long LinkLogin(int employeeId)
    {
        return UsingDbContext(context =>
        {
            var user = context.Users.First(u => u.TenantId == TenantId);
            context.Set<Employee>().Find(employeeId).UserId = user.Id;
            context.SaveChanges();
            return user.Id;
        });
    }

    private void EnableSponsorQuota(bool enabled)
    {
        UsingDbContext(context =>
        {
            var settings = context.ManagePersonnelSettingses.FirstOrDefault(s => s.TenantId == TenantId);
            if (settings == null)
            {
                context.ManagePersonnelSettingses.Add(new ManagePersonnelSettings { TenantId = TenantId, EnableSponsorGovtJobTitleQuota = enabled });
            }
            else
            {
                settings.EnableSponsorGovtJobTitleQuota = enabled;
            }
        });
    }

    /// <summary>A sponsorship in force today (legacy reads the quota against TODAY, not the compensation date).</summary>
    private int SeedSponsorship(int employeeId, bool citizen, int accrued, int used)
    {
        return UsingDbContext(context =>
        {
            var group = context.Set<NationalityGroup>().Add(new NationalityGroup
            {
                TenantId = TenantId, NameEn = citizen ? "Citizen" : "Expat", NameAr = citizen ? "مواطن" : "وافد"
            }).Entity;
            context.SaveChanges();

            var employee = context.Set<Employee>().Find(employeeId);
            context.Set<Nationality>().Find(employee.NationalityId).NationalityGroupId = group.Id;

            var businessType = context.Set<BusinessType>().Add(new BusinessType { TenantId = TenantId, Code = "B1", NameEn = "Trading", NameAr = "تجارة" }).Entity;
            var governorate = context.Set<Governorate>().Add(new Governorate { TenantId = TenantId, Code = "GV1", NameEn = "Capital", NameAr = "العاصمة" }).Entity;
            var jobCategory = context.Set<GovtJobCategory>().Add(new GovtJobCategory { TenantId = TenantId, Code = "C1", NameEn = "Admin", NameAr = "إداري" }).Entity;
            context.SaveChanges();

            var orgUnit = context.Set<Abp.Organizations.OrganizationUnit>().Add(new Abp.Organizations.OrganizationUnit
            {
                TenantId = TenantId, Code = "0000" + context.Set<Abp.Organizations.OrganizationUnit>().Count(), DisplayName = "Company"
            }).Entity;
            context.SaveChanges();

            var sponsor = context.Set<Sponsor>().Add(new Sponsor
            {
                TenantId = TenantId, Code = "S1", NameEn = "Sponsor", NameAr = "كفيل",
                NationalityId = employee.NationalityId, BusinessTypeId = businessType.Id, GovernorateId = governorate.Id,
                OrganizationUnitId = orgUnit.Id
            }).Entity;
            var jobTitle = context.Set<GovtJobTitle>().Add(new GovtJobTitle
            {
                TenantId = TenantId, Code = "G1", NameEn = "Clerk", NameAr = "كاتب", GovtJobCategoryId = jobCategory.Id
            }).Entity;
            context.SaveChanges();

            var currency = context.Set<Currency>().First(c => c.Code == "KWD");
            var visaType = context.Set<WorkVisaType>().Add(new WorkVisaType { TenantId = TenantId, Code = "V1", NameEn = "Work", NameAr = "عمل" }).Entity;
            context.SaveChanges();

            context.Set<EmployeeSponsor>().Add(new EmployeeSponsor
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                SponsorId = sponsor.Id,
                GovtJobTitleId = jobTitle.Id,
                SalaryCurrencyId = currency.Id,
                WorkVisaTypeId = visaType.Id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2099, 12, 31)
            });

            var sponsorJobTitle = context.Set<SponsorJobTitle>().Add(new SponsorJobTitle
            {
                TenantId = TenantId,
                SponsorId = sponsor.Id,
                GovtJobTitleId = jobTitle.Id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2099, 12, 31),
                CitizenQuotaAccrued = citizen ? accrued : 0,
                CitizenQuotaUsed = citizen ? used : 0,
                NonCitizenQuotaAccrued = citizen ? 0 : accrued,
                NonCitizenQuotaUsed = citizen ? 0 : used
            }).Entity;
            context.SaveChanges();
            return sponsorJobTitle.Id;
        });
    }

    #endregion
}
