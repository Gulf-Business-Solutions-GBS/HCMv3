using System;
using System.Threading.Tasks;
using HCMv3.BusinessRules;
using HCMv3.HcmNotifications;
using HCMv3.HcmNotifications.Rules;
using HCMv3.HcmNotifications.Rules.Sources;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HcmNotifications;

/// <summary>
/// The Notification Master criterion, run through the business rule engine: what parses, what validates
/// against the source's whitelisted fields, and what matches. The criterion is stored exactly as the formula
/// editor produces it, so these tests use that stored form.
/// </summary>
// ReSharper disable once InconsistentNaming
public class NotificationCriterionEvaluator_Tests : HcmNotificationTestBase
{
    private readonly INotificationCriterionEvaluator _evaluator;
    private readonly INotificationRuleSource _documents;

    public NotificationCriterionEvaluator_Tests()
    {
        _evaluator = Resolve<INotificationCriterionEvaluator>();
        _documents = Resolve<INotificationRuleSourceRegistry>().Find(NotificationRuleSourceCodes.EmployeeDocument);
    }

    private bool Matches(string criterion, EmployeeDocument document)
    {
        var compiled = _evaluator.Compile(criterion);

        return _evaluator.Matches(
            compiled,
            _documents,
            new NotificationRuleCandidate { RecordId = document.Id, Record = document },
            Today,
            TenantId);
    }

    private static EmployeeDocument DocumentExpiringIn(int days) => new()
    {
        Id = 1,
        EmployeeId = 0,
        ExpiryDate = Today.AddDays(days),
        IssueDate = Today.AddYears(-1)
    };

    [Fact]
    public void Should_Match_Every_Record_When_The_Criterion_Is_Blank()
    {
        Matches(null, DocumentExpiringIn(400)).ShouldBeTrue();
        Matches("  ", DocumentExpiringIn(400)).ShouldBeTrue();
        _evaluator.Validate(_documents, null).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(30, true)]
    [InlineData(29, true)]
    [InlineData(0, true)]
    [InlineData(-5, true)]
    [InlineData(31, false)]
    public void Should_Match_A_Document_Expiring_Within_The_Window(int daysUntilExpiry, bool expected)
    {
        Matches(DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30), DocumentExpiringIn(daysUntilExpiry))
            .ShouldBe(expected);
    }

    [Fact]
    public void Should_Not_Match_A_Document_Whose_Expiry_Date_Was_Never_Set()
    {
        // 0001-01-01 is "never set" in this schema; DaysUntil treats it as empty rather than as 739,000 days
        // overdue, so it cannot satisfy "within 30 days" on the first run.
        var document = DocumentExpiringIn(0);
        document.ExpiryDate = default;

        Matches(DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30), document).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Follow_A_Reference_To_The_Employee_By_Key()
    {
        var activeId = CreateEmployee("CRIT-ACTIVE");
        var leaverId = CreateEmployee("CRIT-LEAVER", status: EmployeeStatus.Inactive);
        var criterion = FieldEquals(EmployeeDocumentNotificationRuleSource.Parameter, "Employee.Status", RuleDataType.Integer, "1");

        var activeMatches = await InUnitOfWorkAsync(() => Task.FromResult(
            Matches(criterion, new EmployeeDocument { Id = 1, EmployeeId = activeId })));
        var leaverMatches = await InUnitOfWorkAsync(() => Task.FromResult(
            Matches(criterion, new EmployeeDocument { Id = 2, EmployeeId = leaverId })));

        activeMatches.ShouldBeTrue();
        leaverMatches.ShouldBeFalse();
    }

    [Fact]
    public void Should_Reject_A_Field_The_Source_Does_Not_Expose()
    {
        // The whitelist is the security boundary: a criterion can only read what the source declared.
        var errors = _evaluator.Validate(
            _documents,
            FieldEquals(EmployeeDocumentNotificationRuleSource.Parameter, "TenantId", RuleDataType.Integer, "1"));

        errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Should_Reject_A_Parameter_Of_Another_Source()
    {
        var errors = _evaluator.Validate(
            _documents,
            DueWithin(LoanNotificationRuleSource.Parameter, "EndDate", 30));

        errors.ShouldNotBeEmpty();
    }

    [Fact]
    public void Should_Reject_Legacy_Xaf_Criteria_Text()
    {
        // Legacy criteria were XAF filter text. They cannot be imported as they are -- the tenant re-enters them.
        var errors = _evaluator.Validate(_documents, "[ExpiryDate] < LocalDateTimeToday()");

        errors.ShouldNotBeEmpty();
        Should.Throw<RuleDefinitionException>(() => _evaluator.Compile("[ExpiryDate] < LocalDateTimeToday()"));
    }

    [Fact]
    public void Should_Reject_A_Function_That_Does_Not_Exist()
    {
        var criterion = DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30)
            .Replace("DaysUntil", "DeleteEverything");

        _evaluator.Validate(_documents, criterion).ShouldNotBeEmpty();
    }

    [Fact]
    public void Should_Refuse_A_Type_That_Is_Not_A_Source()
    {
        _evaluator.Validate(null, null).ShouldNotBeEmpty();
    }

    [Fact]
    public void Should_Accept_A_Sound_Criterion()
    {
        _evaluator.Validate(_documents, DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30))
            .ShouldBeEmpty();
    }

    [Fact]
    public void Should_Measure_Days_From_The_Run_Date_Not_The_Clock()
    {
        var document = new EmployeeDocument { Id = 1, ExpiryDate = new DateTime(2030, 1, 31) };
        var criterion = _evaluator.Compile(DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30));
        var candidate = new NotificationRuleCandidate { RecordId = 1, Record = document };

        _evaluator.Matches(criterion, _documents, candidate, new DateTime(2030, 1, 1), TenantId).ShouldBeTrue();
        _evaluator.Matches(criterion, _documents, candidate, new DateTime(2029, 12, 31), TenantId).ShouldBeFalse();
    }
}
