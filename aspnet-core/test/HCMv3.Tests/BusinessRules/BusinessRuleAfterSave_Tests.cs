using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.UI;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Definition;
using HCMv3.BusinessRules.Dto;
using HCMv3.BusinessRules.Records;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.BusinessRules;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.BusinessRules;

/// <summary>
/// AfterSave: once a leave is saved, a rule may create a record in another table or update a field on one,
/// inside the same transaction, so a failure saves nothing.
///
/// <para>
/// The first half proves it does what the business asked. The second half proves the limits hold -- no other
/// tenant's rows, no system tables or columns, no approval bypass, no runaway chains -- because a feature that
/// lets configured rules write arbitrary tables is only as safe as the tests that pin those limits down.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class BusinessRuleAfterSave_Tests : BusinessRuleIntegrationTestBase
{
    // ---------------------------------------------------------------- what it does

    [Fact]
    public async Task Should_Create_A_Record_In_Another_Table_After_A_Leave_Is_Saved()
    {
        await ArmAsync("AS_CREATE", RulePurpose.Evaluate, CreateNationality("AS_CREATE"), RuleEventType.AfterSave);

        await InsertAbsenceAsync(totalLeaveDays: 3);

        var created = UsingDbContext(context => context.Set<Nationality>().SingleOrDefault(n => n.Code == "AS_CREATE"));
        created.ShouldNotBeNull();
        created.NameEn.ShouldBe("Created by rule");
    }

    [Fact]
    public async Task Should_Create_The_Record_In_The_Tenant_Of_The_Save()
    {
        await ArmAsync("AS_TENANT", RulePurpose.Evaluate, CreateNationality("AS_TENANT"), RuleEventType.AfterSave);

        await InsertAbsenceAsync(totalLeaveDays: 3);

        UsingDbContext(context => context.Set<Nationality>().IgnoreQueryFilters().Single(n => n.Code == "AS_TENANT").TenantId)
            .ShouldBe(AbpSession.TenantId.Value);
    }

    [Fact]
    public async Task Should_Update_A_Field_In_Another_Table_After_A_Leave_Is_Saved()
    {
        // The row is found through a value the rule already holds -- the leave's own employee.
        await ArmAsync(
            "AS_UPDATE",
            RulePurpose.Evaluate,
            Rule(new RuleUpdateRecordAction
            {
                Table = "Employee",
                RecordId = LeaveField("EmployeeId"),
                Values = new List<RuleRecordFieldValue> { Value("Email", Const("followup@gbskw.com", RuleDataType.String)) }
            }),
            RuleEventType.AfterSave);

        await InsertAbsenceAsync(totalLeaveDays: 3);

        UsingDbContext(context => context.Set<Employee>().Single(e => e.Id == EmployeeId).Email).ShouldBe("followup@gbskw.com");
    }

    [Fact]
    public async Task Should_Offer_AfterSave_On_Leave()
    {
        var metadata = await RulesAppService.GetRuleMetadata();

        metadata.BaseObjects.Single(b => b.Code == EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode)
            .SupportedEvents.ShouldContain(RuleEventType.AfterSave);
    }

    // ---------------------------------------------------------------- "raise an error and do nothing"

    [Fact]
    public async Task Should_Save_Nothing_When_An_AfterSave_Rule_Raises_An_Error()
    {
        await ArmAsync("AS_RAISE", RulePurpose.Validate, AlwaysRaise(), RuleEventType.AfterSave);

        await Should.ThrowAsync<UserFriendlyException>(() => InsertAbsenceAsync(totalLeaveDays: 3));

        StoredAbsenceCount().ShouldBe(0);
    }

    [Fact]
    public async Task Should_Save_Nothing_When_A_Follow_Up_Change_Fails()
    {
        // The leave saves, then its follow-up names a record that does not exist. Both must be undone.
        await ArmAsync(
            "AS_MISSING_ROW",
            RulePurpose.Evaluate,
            Rule(new RuleUpdateRecordAction
            {
                Table = "Employee",
                RecordId = Const(987654321, RuleDataType.Integer),
                Values = new List<RuleRecordFieldValue> { Value("Email", Const("x@gbskw.com", RuleDataType.String)) }
            }),
            RuleEventType.AfterSave);

        await Should.ThrowAsync<UserFriendlyException>(() => InsertAbsenceAsync(totalLeaveDays: 3));

        StoredAbsenceCount().ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Keep_A_Created_Record_When_A_Later_Rule_Fails()
    {
        // All-or-nothing across rules as well as within one: a follow-up staged by one rule must not survive a
        // later rule refusing the save.
        await ArmAsync("AS_CREATE_FIRST", RulePurpose.Evaluate, CreateNationality("AS_CREATE_FIRST"), RuleEventType.AfterSave);
        await ArmAsync("AS_THEN_RAISE", RulePurpose.Validate, AlwaysRaise(), RuleEventType.AfterSave);

        await Should.ThrowAsync<UserFriendlyException>(() => InsertAbsenceAsync(totalLeaveDays: 3));

        UsingDbContext(context => context.Set<Nationality>().IgnoreQueryFilters().Any(n => n.Code == "AS_CREATE_FIRST"))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Stop_A_Rule_That_Keeps_Triggering_Itself()
    {
        // The rule updates the very leave it runs on, which saves the leave again, which runs the rule again.
        await ArmAsync(
            "AS_LOOP",
            RulePurpose.Evaluate,
            Rule(new RuleUpdateRecordAction
            {
                Table = "EmployeeAbsence",
                RecordId = LeaveField("Id"),
                Values = new List<RuleRecordFieldValue>
                {
                    Value("TotalLeaveDays", new RuleArithmeticExpression
                    {
                        Operator = RuleArithmeticOperator.Add,
                        Left = LeaveField("TotalLeaveDays"),
                        Right = Const(1, RuleDataType.Decimal)
                    })
                }
            }),
            RuleEventType.AfterSave);

        var exception = await Should.ThrowAsync<UserFriendlyException>(() => InsertAbsenceAsync(totalLeaveDays: 3));

        // The loop guard, specifically -- not some other error that happened to abort the save first.
        exception.Message.ShouldBe(Text("BusinessRulesKeptTriggeringEachOther"));
        StoredAbsenceCount().ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_Run_AfterSave_Rules_When_A_Leave_Is_Deleted()
    {
        // ABP could plausibly report a soft delete as an update; if it did, deleting a leave would create records.
        await ArmAsync("AS_ON_DELETE", RulePurpose.Evaluate, CreateNationality("AS_ON_DELETE"), RuleEventType.AfterSave);
        var absenceId = CreateAbsence(totalLeaveDays: 3);

        await DeleteAbsenceAsync(absenceId);

        UsingDbContext(context => context.Set<Nationality>().IgnoreQueryFilters().Any(n => n.Code == "AS_ON_DELETE"))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Write_Anything_When_A_Rule_Is_Tested()
    {
        // The Test rule button is an authoring dry run. It must never create the record it previews.
        var output = await RulesAppService.TestBusinessRule(new TestBusinessRuleInput
        {
            BaseObject = EmployeeAbsenceRuleBaseObjectProvider.BaseObjectCode,
            Purpose = RulePurpose.Evaluate,
            DefinitionJson = RuleDefinitionSerializer.Serialize(CreateNationality("AS_DRY_RUN"))
        });

        output.Failures.ShouldBeEmpty();
        UsingDbContext(context => context.Set<Nationality>().IgnoreQueryFilters().Any(n => n.Code == "AS_DRY_RUN"))
            .ShouldBeFalse();
    }

    // ---------------------------------------------------------------- the limits

    [Fact]
    public async Task Should_Not_Update_Another_Tenants_Record()
    {
        var otherTenantEmployeeId = CreateEmployee(tenantId: 2);
        var emailBefore = OtherTenantEmail(otherTenantEmployeeId);

        await ArmAsync(
            "AS_OTHER_TENANT",
            RulePurpose.Evaluate,
            Rule(new RuleUpdateRecordAction
            {
                Table = "Employee",
                RecordId = Const(otherTenantEmployeeId, RuleDataType.Integer),
                Values = new List<RuleRecordFieldValue> { Value("Email", Const("stolen@gbskw.com", RuleDataType.String)) }
            }),
            RuleEventType.AfterSave);

        var exception = await Should.ThrowAsync<UserFriendlyException>(() => InsertAbsenceAsync(totalLeaveDays: 3));

        // Refused because the row is invisible to this tenant -- reported as not found, never as forbidden, so a
        // rule cannot use the difference to learn that another company's record exists.
        exception.Message.ShouldContain("Record " + otherTenantEmployeeId + " was not found.");
        OtherTenantEmail(otherTenantEmployeeId).ShouldBe(emailBefore);
    }

    [Theory]
    [InlineData("User")]
    [InlineData("Role")]
    [InlineData("UserRole")]
    [InlineData("Tenant")]
    [InlineData("BusinessRule")]
    public void Should_Not_Expose_System_Tables(string table)
    {
        Resolve<IRuleRecordSchema>().FindTable(table).ShouldBeNull();
    }

    [Fact]
    public void Should_Expose_Business_Tables_Without_Their_System_Columns()
    {
        var employee = Resolve<IRuleRecordSchema>().FindTable("Employee");

        employee.ShouldNotBeNull();
        employee.FindColumn("Email").ShouldNotBeNull();

        foreach (var systemColumn in new[] { "Id", "TenantId", "CreationTime", "IsDeleted", "LastModifierUserId" })
        {
            employee.FindColumn(systemColumn).ShouldBeNull();
        }
    }

    [Theory]
    [InlineData("WorkflowStatus")]
    [InlineData("WorkflowRequestId")]
    public void Should_Not_Expose_The_Approval_Status(string column)
    {
        // Setting WorkflowStatus to Approved would skip the approval chain.
        Resolve<IRuleRecordSchema>().FindTable("EmployeeAbsence").FindColumn(column).ShouldBeNull();
    }

    [Fact]
    public async Task Should_Refuse_Saving_A_Rule_That_Writes_A_System_Table()
    {
        await Should.ThrowAsync<UserFriendlyException>(() => RulesAppService.CreateOrEditBusinessRule(RuleInput(
            "AS_SYSTEM_TABLE",
            RulePurpose.Evaluate,
            Rule(new RuleCreateRecordAction
            {
                Table = "UserRole",
                Values = new List<RuleRecordFieldValue> { Value("RoleId", Const(1, RuleDataType.Integer)) }
            }))));
    }

    [Fact]
    public async Task Should_Refuse_Saving_A_Rule_That_Sets_A_System_Column()
    {
        await Should.ThrowAsync<UserFriendlyException>(() => RulesAppService.CreateOrEditBusinessRule(RuleInput(
            "AS_SYSTEM_COLUMN",
            RulePurpose.Evaluate,
            Rule(new RuleUpdateRecordAction
            {
                Table = "Employee",
                RecordId = LeaveField("EmployeeId"),
                Values = new List<RuleRecordFieldValue> { Value("TenantId", Const(2, RuleDataType.Integer)) }
            }))));
    }

    [Fact]
    public async Task Should_Refuse_Arming_A_Record_Action_On_BeforeSave()
    {
        // Only AfterSave runs inside a transaction that already holds the saved record.
        var ruleId = await CreateRuleAsync("AS_ON_BEFORESAVE", RulePurpose.Evaluate, CreateNationality("AS_ON_BEFORESAVE"));

        await Should.ThrowAsync<UserFriendlyException>(() => RulesAppService.CreateOrEditAssignment(
            new CreateOrEditBusinessRuleAssignmentDto { BusinessRuleId = ruleId, EventType = RuleEventType.BeforeSave, IsActive = true }));
    }

    [Fact]
    public async Task Should_Refuse_A_Record_Action_In_A_Validation_Rule()
    {
        // Validation rules run with writes off, so the action would silently do nothing.
        await Should.ThrowAsync<UserFriendlyException>(() => RulesAppService.CreateOrEditBusinessRule(
            RuleInput("AS_VALIDATE_PURPOSE", RulePurpose.Validate, CreateNationality("AS_VALIDATE_PURPOSE"))));
    }

    [Fact]
    public async Task Should_List_Writable_Tables_For_The_Rule_Builder()
    {
        var tables = await RulesAppService.GetRuleRecordTables();

        tables.ShouldContain(t => t.Name == "Employee");
        tables.ShouldNotContain(t => t.Name == "User");
    }

    // ---------------------------------------------------------------- helpers

    private static RuleDefinition CreateNationality(string code)
    {
        return Rule(new RuleCreateRecordAction
        {
            Table = "Nationality",
            Values = new List<RuleRecordFieldValue>
            {
                Value("Code", Const(code, RuleDataType.String)),
                Value("NameEn", Const("Created by rule", RuleDataType.String)),
                Value("NameAr", Const("أنشأتها قاعدة", RuleDataType.String))
            }
        });
    }

    private string Text(string key)
    {
        return Resolve<ILocalizationManager>().GetString(HCMv3Consts.LocalizationSourceName, key);
    }

    private string OtherTenantEmail(int employeeId)
    {
        return UsingDbContext(2, context => context.Set<Employee>().IgnoreQueryFilters().Single(e => e.Id == employeeId).Email);
    }
}
