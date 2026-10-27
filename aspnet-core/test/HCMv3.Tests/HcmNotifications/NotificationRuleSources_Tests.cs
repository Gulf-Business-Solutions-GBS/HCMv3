using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Organizations;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.HcmNotifications;
using HCMv3.HcmNotifications.Rules;
using HCMv3.HcmNotifications.Rules.Sources;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HcmNotifications;

/// <summary>
/// Every rule source: that it is registered (Castle's name-suffix trap would hide a misnamed one), that it has
/// a Zero notification definition, that its sub type really filters, that its criterion fields work against
/// a real record, and that records a rule has logged are left out.
/// </summary>
// ReSharper disable once InconsistentNaming
public class NotificationRuleSources_Tests : HcmNotificationTestBase
{
    private readonly INotificationRuleSourceRegistry _registry;
    private readonly INotificationCriterionEvaluator _evaluator;

    public NotificationRuleSources_Tests()
    {
        _registry = Resolve<INotificationRuleSourceRegistry>();
        _evaluator = Resolve<INotificationCriterionEvaluator>();
    }

    // ------------------------------------------------------------------------------------------------------
    // Registration
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public void Should_Register_Exactly_The_Declared_Sources()
    {
        _registry.GetAll().Select(s => s.Code)
            .ShouldBe(NotificationRuleSourceCodes.All);
    }

    [Theory]
    [InlineData(NotificationRuleSourceCodes.EmployeeDocument, EmployeeDocumentNotificationRuleSource.Parameter, true)]
    [InlineData(NotificationRuleSourceCodes.CompanyDocument, CompanyDocumentNotificationRuleSource.Parameter, true)]
    [InlineData(NotificationRuleSourceCodes.Leave, LeaveNotificationRuleSource.Parameter, true)]
    [InlineData(NotificationRuleSourceCodes.Disciplinary, DisciplinaryNotificationRuleSource.Parameter, true)]
    [InlineData(NotificationRuleSourceCodes.Employee, EmployeeNotificationRuleSource.Parameter, false)]
    [InlineData(NotificationRuleSourceCodes.Loan, LoanNotificationRuleSource.Parameter, true)]
    public void Should_Declare_Its_Parameter_And_Sub_Type(string code, string parameter, bool hasSubType)
    {
        var source = _registry.Find(code);

        source.ShouldNotBeNull();
        source.ParameterName.ShouldBe(parameter);
        source.Criterion.FindParameter(parameter).ShouldNotBeNull();
        (source.SubTypeDisplayNameKey != null).ShouldBe(hasSubType);
    }

    [Fact]
    public void Should_Give_Every_Source_A_Notification_Definition_Name()
    {
        foreach (var source in _registry.GetAll())
        {
            HcmNotificationNames.All.ShouldContain(HcmNotificationNames.ForRuleSource(source.Code));
        }
    }

    [Fact]
    public void Should_Find_A_Source_Whatever_The_Case()
    {
        _registry.Find("employeedocument").ShouldNotBeNull();
        _registry.Find("AbsenceRequest").ShouldBeNull();
        _registry.Find(null).ShouldBeNull();
    }

    // ------------------------------------------------------------------------------------------------------
    // Each source against a real record
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Employee_Document_Source_Should_Filter_By_Document_Type_And_Read_Expiry()
    {
        var employeeId = CreateEmployee("SRC-DOC");
        var passport = CreateDocumentType("SRC-PASS");
        var visa = CreateDocumentType("SRC-VISA");
        var passportId = CreateEmployeeDocument(employeeId, passport, Today.AddDays(10));
        var visaId = CreateEmployeeDocument(employeeId, visa, Today.AddDays(10));

        var matched = await MatchingIdsAsync(NotificationRuleSourceCodes.EmployeeDocument, passport,
            DueWithin(EmployeeDocumentNotificationRuleSource.Parameter, "ExpiryDate", 30));

        matched.ShouldContain(passportId);
        matched.ShouldNotContain(visaId);
    }

    [Fact]
    public async Task Employee_Document_Source_Should_Offer_Employee_Document_Types_Only()
    {
        var employeeType = CreateDocumentType("SRC-EMPTYPE", belongsTo: DocumentBelongsTo.Employee);
        var companyType = CreateDocumentType("SRC-COTYPE", belongsTo: DocumentBelongsTo.Organization);

        var subTypes = await InUnitOfWorkAsync(() => _registry.Find(NotificationRuleSourceCodes.EmployeeDocument).GetSubTypesAsync());

        subTypes.Select(s => s.Id).ShouldContain(employeeType);
        subTypes.Select(s => s.Id).ShouldNotContain(companyType);
    }

    [Fact]
    public async Task Company_Document_Source_Should_Read_Expiry_And_Offer_Company_Types()
    {
        var licence = CreateDocumentType("SRC-LIC", belongsTo: DocumentBelongsTo.Organization);
        var unitId = CreateOrganizationUnit("SRC-CO");
        var documentId = UsingDbContext(context =>
        {
            var document = context.Set<OrganizationDocument>().Add(new OrganizationDocument
            {
                TenantId = TenantId,
                OrganizationUnitId = unitId,
                DocumentTypeId = licence,
                DocumentNum = "LIC-1",
                IssueDate = Today.AddYears(-1),
                ExpiryDate = Today.AddDays(5)
            }).Entity;
            context.SaveChanges();
            return document.Id;
        });

        var matched = await MatchingIdsAsync(NotificationRuleSourceCodes.CompanyDocument, licence,
            DueWithin(CompanyDocumentNotificationRuleSource.Parameter, "ExpiryDate", 7));
        var subTypes = await InUnitOfWorkAsync(() => _registry.Find(NotificationRuleSourceCodes.CompanyDocument).GetSubTypesAsync());

        matched.ShouldContain(documentId);
        subTypes.Select(s => s.Id).ShouldContain(licence);
    }

    [Fact]
    public async Task Leave_Source_Should_Filter_By_Absence_Type_And_Read_Start_Date()
    {
        var employeeId = CreateEmployee("SRC-LEAVE");
        var annual = CreateAbsenceType("SRC-AL");
        var sick = CreateAbsenceType("SRC-SL");
        var annualLeave = CreateLeave(employeeId, annual, Today.AddDays(3));
        var sickLeave = CreateLeave(employeeId, sick, Today.AddDays(3));

        var matched = await MatchingIdsAsync(NotificationRuleSourceCodes.Leave, annual,
            DueWithin(LeaveNotificationRuleSource.Parameter, "StartDate", 7));

        matched.ShouldContain(annualLeave);
        matched.ShouldNotContain(sickLeave);
    }

    [Fact]
    public async Task Disciplinary_Source_Should_Filter_By_Disciplinary_Type()
    {
        var employeeId = CreateEmployee("SRC-DISC");
        var warning = CreateDisciplinaryType("SRC-WARN");
        var other = CreateDisciplinaryType("SRC-OTHER");
        var warningId = CreateDisciplinary(employeeId, warning);
        var otherId = CreateDisciplinary(employeeId, other);

        var matched = await MatchingIdsAsync(NotificationRuleSourceCodes.Disciplinary, warning, null);

        matched.ShouldContain(warningId);
        matched.ShouldNotContain(otherId);
    }

    [Fact]
    public async Task Employee_Source_Should_Read_Probation_End_Date()
    {
        var endingSoon = CreateEmployee("SRC-PROB1", probationEndDate: Today.AddDays(10));
        var endingLater = CreateEmployee("SRC-PROB2", probationEndDate: Today.AddDays(90));

        var criterion = DueWithin(EmployeeNotificationRuleSource.Parameter, "ProbationEndDate", 14);
        var matched = await MatchingIdsAsync(NotificationRuleSourceCodes.Employee, null, criterion);

        matched.ShouldContain(endingSoon);
        matched.ShouldNotContain(endingLater);
    }

    [Fact]
    public async Task Loan_Source_Should_Filter_By_Loan_Type_And_Read_End_Date()
    {
        var employeeId = CreateEmployee("SRC-LOAN");
        var personal = CreateLoanType("SRC-PL");
        var housing = CreateLoanType("SRC-HL");
        var personalLoan = CreateLoan(employeeId, personal, Today.AddDays(20));
        var housingLoan = CreateLoan(employeeId, housing, Today.AddDays(20));

        var matched = await MatchingIdsAsync(NotificationRuleSourceCodes.Loan, personal,
            DueWithin(LoanNotificationRuleSource.Parameter, "EndDate", 30));

        matched.ShouldContain(personalLoan);
        matched.ShouldNotContain(housingLoan);
    }

    // ------------------------------------------------------------------------------------------------------
    // Paging and the sent log
    // ------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task Should_Leave_Out_Records_The_Rule_Has_Logged_But_Not_Those_Another_Rule_Logged()
    {
        var employeeId = CreateEmployee("SRC-LOG");
        var type = CreateDocumentType("SRC-LOGT");
        var loggedByThisRule = CreateEmployeeDocument(employeeId, type, Today.AddDays(5));
        var loggedByOtherRule = CreateEmployeeDocument(employeeId, type, Today.AddDays(6));
        var fresh = CreateEmployeeDocument(employeeId, type, Today.AddDays(7));

        var configId = CreateNotificationConfig();
        var thisRule = CreateRule(NotificationRuleSourceCodes.EmployeeDocument, configId);
        var otherRule = CreateRule(NotificationRuleSourceCodes.EmployeeDocument, configId, DueWithin("Document", "ExpiryDate", 1));

        UsingDbContext(context =>
        {
            context.Set<NotificationSentLog>().Add(Log(thisRule, loggedByThisRule));
            context.Set<NotificationSentLog>().Add(Log(otherRule, loggedByOtherRule));
            context.SaveChanges();
        });

        var source = _registry.Find(NotificationRuleSourceCodes.EmployeeDocument);
        var page = await InUnitOfWorkAsync(() => source.GetCandidatesAsync(new NotificationRuleCandidateQuery
        {
            SubTypeId = type,
            ExcludeLoggedForRuleId = thisRule
        }));

        var ids = page.Select(c => c.RecordId).ToList();
        ids.ShouldNotContain(loggedByThisRule);
        ids.ShouldContain(loggedByOtherRule);
        ids.ShouldContain(fresh);
    }

    [Fact]
    public async Task Should_Page_In_Id_Order()
    {
        var employeeId = CreateEmployee("SRC-PAGE");
        var type = CreateDocumentType("SRC-PAGET");
        var first = CreateEmployeeDocument(employeeId, type, Today.AddDays(1));
        var second = CreateEmployeeDocument(employeeId, type, Today.AddDays(2));
        var third = CreateEmployeeDocument(employeeId, type, Today.AddDays(3));

        var source = _registry.Find(NotificationRuleSourceCodes.EmployeeDocument);

        var page1 = await InUnitOfWorkAsync(() => source.GetCandidatesAsync(new NotificationRuleCandidateQuery { SubTypeId = type, MaxCount = 2 }));
        var page2 = await InUnitOfWorkAsync(() => source.GetCandidatesAsync(new NotificationRuleCandidateQuery
        {
            SubTypeId = type,
            MaxCount = 2,
            AfterRecordId = page1[^1].RecordId
        }));

        page1.Select(c => c.RecordId).ShouldBe(new long[] { first, second });
        page2.Select(c => c.RecordId).ShouldBe(new long[] { third });
        page1[0].SubjectDate.ShouldBe(Today.AddDays(1));
        page1[0].Label.ShouldContain("SRC-PAGE");
    }

    // ------------------------------------------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------------------------------------------

    private async Task<long[]> MatchingIdsAsync(string sourceCode, int? subTypeId, string criterion)
    {
        var source = _registry.Find(sourceCode);
        _evaluator.Validate(source, criterion).ShouldBeEmpty();
        var compiled = _evaluator.Compile(criterion);

        return await InUnitOfWorkAsync(async () =>
        {
            var page = await source.GetCandidatesAsync(new NotificationRuleCandidateQuery { SubTypeId = subTypeId, MaxCount = 1000 });
            return page.Where(c => _evaluator.Matches(compiled, source, c, Today, TenantId)).Select(c => c.RecordId).ToArray();
        });
    }

    private NotificationSentLog Log(int ruleId, int recordId) => new()
    {
        TenantId = TenantId,
        NotificationMasterId = ruleId,
        NotificationName = HcmNotificationNames.ForRuleSource(NotificationRuleSourceCodes.EmployeeDocument),
        SourceCode = NotificationRuleSourceCodes.EmployeeDocument,
        RecordId = recordId,
        SentDate = Today,
        WasSent = true,
        IsFinal = true
    };

    private long CreateOrganizationUnit(string code)
    {
        return UsingDbContext(context =>
        {
            var unit = context.Set<OrganizationUnit>().Add(new OrganizationUnit(TenantId, code, null) { Code = "9" + Math.Abs(code.GetHashCode() % 10000).ToString("D4") }).Entity;
            context.SaveChanges();
            return unit.Id;
        });
    }

    private int CreateAbsenceType(string code)
    {
        return UsingDbContext(context =>
        {
            var type = context.Set<AbsenceType>().Add(new AbsenceType { TenantId = TenantId, Code = code, NameEn = code, NameAr = code }).Entity;
            context.SaveChanges();
            return type.Id;
        });
    }

    private int CreateLeave(int employeeId, int absenceTypeId, DateTime startDate)
    {
        return UsingDbContext(context =>
        {
            var leave = context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                StartDate = startDate,
                EndDate = startDate.AddDays(4)
            }).Entity;
            context.SaveChanges();
            return leave.Id;
        });
    }

    private int CreateDisciplinaryType(string code)
    {
        return UsingDbContext(context =>
        {
            var type = context.Set<DisciplinaryType>().Add(new DisciplinaryType { TenantId = TenantId, Code = code, NameEn = code, NameAr = code }).Entity;
            context.SaveChanges();
            return type.Id;
        });
    }

    private int CreateDisciplinary(int employeeId, int disciplinaryTypeId)
    {
        return UsingDbContext(context =>
        {
            var record = context.Set<EmployeeDisciplinary>().Add(new EmployeeDisciplinary
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DisciplinaryTypeId = disciplinaryTypeId,
                DisciplinaryDate = Today
            }).Entity;
            context.SaveChanges();
            return record.Id;
        });
    }

    private int CreateLoanType(string code)
    {
        return UsingDbContext(context =>
        {
            var type = context.Set<LoanType>().Add(new LoanType { TenantId = TenantId, Code = code, NameEn = code, NameAr = code }).Entity;
            context.SaveChanges();
            return type.Id;
        });
    }

    private int CreateLoan(int employeeId, int loanTypeId, DateTime endDate)
    {
        return UsingDbContext(context =>
        {
            var loan = context.Set<EmployeeLoan>().Add(new EmployeeLoan
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                LoanTypeId = loanTypeId,
                StartDate = Today.AddMonths(-6),
                RecoveryStartDate = Today.AddMonths(-5),
                EndDate = endDate,
                ActualAmount = 1000,
                DueAmount = 1000
            }).Entity;
            context.SaveChanges();
            return loan.Id;
        });
    }
}
