using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class AbsenceDedRuleDetailsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private AbsenceDedRuleDetailsAppService CreateAppService()
    {
        return CreateService(() => new AbsenceDedRuleDetailsAppService(FakeRepository.For<AbsenceDedRuleDetail>(Context)));
    }

    private int CreateAbsenceDeductionRule(string code)
    {
        var entity = new AbsenceDeductionRule
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            RuleName = code
        };
        Context.AbsenceDeductionRules.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_AbsenceDedRuleDetail()
    {
        var ruleId = CreateAbsenceDeductionRule("ADR1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceDedRuleDetailDto
        {
            AbsenceDeductionRuleId = ruleId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31),
            DaysCount = 5,
            DeductionUnit = DeductionUnit.Fixed,
            DeductionNumber = 1
        });

        var result = await service.GetAll(new GetAllAbsenceDedRuleDetailsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].DaysCount.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var ruleId = CreateAbsenceDeductionRule("ADR2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsenceDedRuleDetailDto
            {
                AbsenceDeductionRuleId = ruleId,
                StartDate = new DateTime(2026, 1, 31),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }

    /// <summary>
    /// A rule's slabs are all in force at once -- the engine picks the first whose DaysCount covers the running
    /// count -- so different slabs must be able to share dates. Kuwait's art. 69 sick leave needs four.
    /// </summary>
    [Fact]
    public async Task Should_Allow_Different_Slabs_Over_The_Same_Dates()
    {
        var ruleId = CreateAbsenceDeductionRule("ADR4");
        var service = CreateAppService();

        foreach (var (days, deduction) in new[] { (10d, 25d), (20d, 50d), (30d, 75d), (60d, 100d) })
        {
            await service.CreateOrEdit(new CreateOrEditAbsenceDedRuleDetailDto
            {
                AbsenceDeductionRuleId = ruleId,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(9999, 12, 31),
                DaysCount = days,
                DeductionUnit = DeductionUnit.Percentage,
                DeductionNumber = deduction
            });
        }

        var result = await service.GetAll(new GetAllAbsenceDedRuleDetailsInput());
        result.TotalCount.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Throw_When_The_Same_Slab_Overlaps_Itself()
    {
        var ruleId = CreateAbsenceDeductionRule("ADR5");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceDedRuleDetailDto
        {
            AbsenceDeductionRuleId = ruleId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            DaysCount = 10
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsenceDedRuleDetailDto
            {
                AbsenceDeductionRuleId = ruleId,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2027, 5, 31),
                DaysCount = 10
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Date_Range()
    {
        var ruleId = CreateAbsenceDeductionRule("ADR3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAbsenceDedRuleDetailDto
        {
            AbsenceDeductionRuleId = ruleId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 1, 31)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAbsenceDedRuleDetailDto
            {
                AbsenceDeductionRuleId = ruleId,
                StartDate = new DateTime(2026, 1, 15),
                EndDate = new DateTime(2026, 2, 15)
            }));
    }
}
