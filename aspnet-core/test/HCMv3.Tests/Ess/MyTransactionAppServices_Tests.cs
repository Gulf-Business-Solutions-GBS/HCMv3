using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Covers the five self-service transaction services built on <c>MyTransactionAppServiceBase</c>.
///
/// <para>
/// The behaviour worth protecting is the part they share, because that is what would otherwise drift
/// between five services written at the same time: the row must be created hidden, the employee must come
/// from the session rather than the form, and a write must re-check ownership rather than trusting that the
/// list query was scoped. Each is tested here at least once against a real service rather than the base in
/// isolation, so a service that bypassed the base would still be caught.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyTransactionAppServices_Tests : AppTestBase
{
    private readonly IMyAssetAppService _assets;
    private readonly IMyDocumentAppService _documents;
    private readonly IMyLoanAppService _loans;
    private readonly IMyOvertimeAppService _overtime;
    private readonly IMyTicketAppService _tickets;

    public MyTransactionAppServices_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _assets = Resolve<IMyAssetAppService>();
        _documents = Resolve<IMyDocumentAppService>();
        _loans = Resolve<IMyLoanAppService>();
        _overtime = Resolve<IMyOvertimeAppService>();
        _tickets = Resolve<IMyTicketAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateEmployee(string code, long? userId = null)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = code,
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
                FullNameAr = "ت ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    private int CreateAsset()
    {
        Asset asset = null;
        UsingDbContext(context =>
        {
            asset = context.Set<Asset>().Add(new Asset
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Laptop",
                NameAr = "Laptop"
            }).Entity;
        });

        return asset.Id;
    }

    private int CreateLoanType()
    {
        LoanType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<LoanType>().Add(new LoanType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Housing",
                NameAr = "Housing"
            }).Entity;
        });

        return type.Id;
    }

    private int CreateOvertimeType()
    {
        OvertimeType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<OvertimeType>().Add(new OvertimeType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Weekday",
                NameAr = "Weekday"
            }).Entity;
        });

        return type.Id;
    }

    private int CreateDocumentType()
    {
        DocumentType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Passport",
                NameAr = "Passport"
            }).Entity;
        });

        return type.Id;
    }

    private int CreateTicketType()
    {
        TicketType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<TicketType>().Add(new TicketType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Annual",
                NameAr = "Annual"
            }).Entity;
        });

        return type.Id;
    }

    /// <summary>Turns workflow on for one catalog type, pointing it at a one-step chain with a real approver.</summary>
    private int EnableWorkflowFor(string typeCode, string approverCode)
    {
        var approverId = CreateEmployee(approverCode);

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Approver",
                NameAr = "Approver"
            }).Entity;

            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = typeCode + "-STD",
                NameEn = "Standard",
                NameAr = "Standard",
                WorkflowSteps = 1
            }).Entity;

            context.SaveChanges();

            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 1,
                WorkflowRoleId = role.Id,
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });

            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = approverId,
                WorkflowRoleId = role.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == typeCode);
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    private static RequestAssetInput AssetInput(int assetId) => new RequestAssetInput
    {
        AssetId = assetId,
        StartDate = DateTime.Today.AddDays(1),
        EndDate = DateTime.Today.AddDays(30),
        Comments = "need a laptop"
    };

    // ---------------------------------------------------------------- the shared contract

    [Fact]
    public async Task Every_Service_Should_Refuse_A_User_With_No_Employee_Record()
    {
        // Admin accounts are never linked, so this is the state the shell must handle, not an edge case.
        await Assert.ThrowsAsync<UserFriendlyException>(() => _assets.GetMyAssets(ListInput()));
        await Assert.ThrowsAsync<UserFriendlyException>(() => _documents.GetMyDocuments(ListInput()));
        await Assert.ThrowsAsync<UserFriendlyException>(() => _loans.GetMyLoans(ListInput()));
        await Assert.ThrowsAsync<UserFriendlyException>(() => _overtime.GetMyOvertimes(ListInput()));
        await Assert.ThrowsAsync<UserFriendlyException>(() => _tickets.GetMyTickets(ListInput()));
    }

    [Fact]
    public async Task Should_Record_Immediately_When_Workflow_Is_Off_For_The_Type()
    {
        // The catalog seeds every type dormant, so this is the behaviour on a fresh install: the row is
        // effective at once and no request is raised. Turning workflow off has to remain a real option.
        CreateCurrentUserEmployee("MTX100");
        var assetId = CreateAsset();

        var result = await _assets.RequestAsset(AssetInput(assetId));

        result.SubmittedForApproval.ShouldBeFalse();
        result.WorkflowRequestId.ShouldBeNull();

        var row = UsingDbContext(c => c.Set<EmployeeAsset>().IgnoreQueryFilters().Single(a => a.Id == result.TransactionId));
        row.WorkflowStatus.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Submit_For_Approval_When_Workflow_Is_On()
    {
        var meId = CreateCurrentUserEmployee("MTX110");
        EnableWorkflowFor("EmployeeAsset", "MTX110APP");
        var assetId = CreateAsset();

        var result = await _assets.RequestAsset(AssetInput(assetId));

        result.SubmittedForApproval.ShouldBeTrue();
        result.WorkflowRequestId.ShouldNotBeNull();

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value));
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.EmployeeId.ShouldBe(meId);
        request.TargetEntityId.ShouldBe(result.TransactionId);

        // Routing needs the sub-type, or a chain configured per asset could never match.
        request.RequestSubTypeId.ShouldBe(assetId);
    }

    [Fact]
    public async Task A_Submitted_Row_Should_Be_Hidden_From_The_Rest_Of_The_System()
    {
        // The whole point of the single-table design. A pending request lives in the real table, so it must
        // be the ApprovedOnly filter that keeps payroll from seeing it.
        CreateCurrentUserEmployee("MTX120");
        EnableWorkflowFor("EmployeeAsset", "MTX120APP");
        var assetId = CreateAsset();

        var result = await _assets.RequestAsset(AssetInput(assetId));

        // Read through a repository inside a unit of work, because that is where ABP data filters live. A
        // raw DbContext query would bypass ApprovedOnly entirely and pass whatever the filter did.
        var repository = Resolve<Abp.Domain.Repositories.IRepository<EmployeeAsset>>();
        var unitOfWorkManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            var visibleToOthers = repository.GetAll().Any(a => a.Id == result.TransactionId);
            visibleToOthers.ShouldBeFalse("A pending request was visible to the rest of the system.");
        }

        // But the employee sees their own.
        var mine = await _assets.GetMyAssets(ListInput());
        mine.Items.Select(i => i.Id).ShouldContain(result.TransactionId);
    }

    [Fact]
    public async Task The_Request_Should_Be_Attributed_To_The_Signed_In_Employee()
    {
        // There is no employee field on the input at all, and that is deliberate -- a self-service form must
        // not be able to raise a request on somebody else's behalf.
        var meId = CreateCurrentUserEmployee("MTX130");
        CreateEmployee("MTX130OTHER");
        var assetId = CreateAsset();

        var result = await _assets.RequestAsset(AssetInput(assetId));

        var row = UsingDbContext(c => c.Set<EmployeeAsset>().IgnoreQueryFilters().Single(a => a.Id == result.TransactionId));
        row.EmployeeId.ShouldBe(meId);
    }

    [Fact]
    public async Task Should_Not_List_Somebody_Elses_Rows()
    {
        var meId = CreateCurrentUserEmployee("MTX140");
        var otherId = CreateEmployee("MTX140OTHER");
        var assetId = CreateAsset();

        UsingDbContext(context =>
        {
            context.Set<EmployeeAsset>().Add(new EmployeeAsset
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                AssetId = assetId,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(10)
            });
        });

        var mine = await _assets.GetMyAssets(ListInput());

        mine.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Withdrawing_Should_Remove_The_Row_And_Close_Its_Request()
    {
        // Closing the request matters as much as deleting the row: otherwise the approver is left with an
        // inbox entry pointing at something that no longer exists.
        CreateCurrentUserEmployee("MTX150");
        EnableWorkflowFor("EmployeeAsset", "MTX150APP");
        var assetId = CreateAsset();

        var result = await _assets.RequestAsset(AssetInput(assetId));

        await _assets.CancelMyAsset(new EntityDto(result.TransactionId));

        var rowExists = UsingDbContext(c => c.Set<EmployeeAsset>().IgnoreQueryFilters()
            .Any(a => a.Id == result.TransactionId && !a.IsDeleted));
        rowExists.ShouldBeFalse();

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value));
        request.RequestStatus.ShouldBe(RequestStatus.Closed);
    }

    [Fact]
    public async Task Should_Refuse_To_Withdraw_Somebody_Elses_Request()
    {
        // Ownership is re-checked on the write. A scoped list paired with an unscoped write is exactly how
        // one employee ends up withdrawing another's request.
        CreateCurrentUserEmployee("MTX160");
        var otherId = CreateEmployee("MTX160OTHER");
        var assetId = CreateAsset();

        EmployeeAsset theirs = null;
        UsingDbContext(context =>
        {
            theirs = context.Set<EmployeeAsset>().Add(new EmployeeAsset
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                AssetId = assetId,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(10),
                WorkflowStatus = RequestStatus.Open
            }).Entity;
        });

        var theirId = theirs.Id;

        await Assert.ThrowsAsync<AbpAuthorizationException>(() => _assets.CancelMyAsset(new EntityDto(theirId)));
    }

    [Fact]
    public async Task Should_Refuse_To_Withdraw_Something_Already_Approved()
    {
        // Once approved it is a real record others may have acted on -- changing it is an amendment.
        var meId = CreateCurrentUserEmployee("MTX170");
        var assetId = CreateAsset();

        EmployeeAsset approved = null;
        UsingDbContext(context =>
        {
            approved = context.Set<EmployeeAsset>().Add(new EmployeeAsset
            {
                TenantId = TenantId,
                EmployeeId = meId,
                AssetId = assetId,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(10),
                WorkflowStatus = RequestStatus.Approved
            }).Entity;
        });

        var approvedId = approved.Id;

        await Assert.ThrowsAsync<UserFriendlyException>(() => _assets.CancelMyAsset(new EntityDto(approvedId)));
    }

    // ---------------------------------------------------------------- per type

    [Fact]
    public async Task Each_Type_Should_Number_Its_Requests_With_Its_Own_Prefix()
    {
        // Shared numbering would make request numbers useless for telling at a glance what a request is.
        CreateCurrentUserEmployee("MTX200");
        EnableWorkflowFor("EmployeeLoan", "MTX200APP");
        var loanTypeId = CreateLoanType();

        var result = await _loans.RequestLoan(new RequestLoanInput
        {
            LoanTypeId = loanTypeId,
            ActualAmount = 1000,
            StartDate = DateTime.Today,
            RecoveryStartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(10)
        });

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value));
        request.RequestNumber.ShouldStartWith("LOAN-" + DateTime.Today.Year);
    }

    [Fact]
    public async Task A_Loan_Should_Start_Fully_Outstanding()
    {
        // Set on creation rather than left for the approver, so the figure is right even if nobody edits it.
        CreateCurrentUserEmployee("MTX210");
        var loanTypeId = CreateLoanType();

        var result = await _loans.RequestLoan(new RequestLoanInput
        {
            LoanTypeId = loanTypeId,
            ActualAmount = 1200,
            StartDate = DateTime.Today,
            RecoveryStartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(12)
        });

        var loan = UsingDbContext(c => c.Set<EmployeeLoan>().IgnoreQueryFilters().Single(l => l.Id == result.TransactionId));
        loan.DueAmount.ShouldBe(1200);
        loan.BalanceDueAmount.ShouldBe(1200);
    }

    [Fact]
    public async Task A_Loan_For_Nothing_Should_Be_Refused()
    {
        CreateCurrentUserEmployee("MTX220");
        var loanTypeId = CreateLoanType();

        await Assert.ThrowsAsync<UserFriendlyException>(() => _loans.RequestLoan(new RequestLoanInput
        {
            LoanTypeId = loanTypeId,
            ActualAmount = 0,
            StartDate = DateTime.Today,
            RecoveryStartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(6)
        }));
    }

    [Fact]
    public async Task A_Document_Without_A_Number_Should_Be_Refused()
    {
        CreateCurrentUserEmployee("MTX230");
        var typeId = CreateDocumentType();

        await Assert.ThrowsAsync<UserFriendlyException>(() => _documents.RequestDocument(new RequestDocumentInput
        {
            DocumentTypeId = typeId,
            DocumentNum = "   ",
            IssueDate = DateTime.Today,
            ExpiryDate = DateTime.Today.AddYears(5)
        }));
    }

    [Fact]
    public async Task An_Overtime_Claim_Should_Not_Pre_Approve_Its_Own_Hours()
    {
        // Defaulting approved hours to the claim would make an untouched record look like somebody's
        // decision. They stay zero until an approver sets them.
        CreateCurrentUserEmployee("MTX240");
        var typeId = CreateOvertimeType();

        var result = await _overtime.RequestOvertime(new RequestOvertimeInput
        {
            OvertimeTypeId = typeId,
            EffectiveDate = DateTime.Today.AddDays(-1),
            OvertimeHours = 5
        });

        var row = UsingDbContext(c => c.Set<EmployeeOvertime>().IgnoreQueryFilters().Single(o => o.Id == result.TransactionId));
        row.OvertimeHours.ShouldBe(5);
        row.ApprovedOvertimeHours.ShouldBe(0);
    }

    [Fact]
    public async Task An_Overtime_Claim_For_No_Hours_Should_Be_Refused()
    {
        CreateCurrentUserEmployee("MTX250");
        var typeId = CreateOvertimeType();

        await Assert.ThrowsAsync<UserFriendlyException>(() => _overtime.RequestOvertime(new RequestOvertimeInput
        {
            OvertimeTypeId = typeId,
            EffectiveDate = DateTime.Today.AddDays(-1),
            OvertimeHours = 0
        }));
    }

    [Fact]
    public async Task A_Ticket_Claim_For_A_Negative_Amount_Should_Be_Refused()
    {
        CreateCurrentUserEmployee("MTX260");
        var typeId = CreateTicketType();

        await Assert.ThrowsAsync<UserFriendlyException>(() => _tickets.RequestTicket(new RequestTicketInput
        {
            TicketTypeId = typeId,
            TicketDate = DateTime.Today,
            Amount = -1
        }));
    }

    [Fact]
    public async Task A_Ticket_Should_Default_Its_Payment_Date_Rather_Than_Landing_In_Year_One()
    {
        CreateCurrentUserEmployee("MTX270");
        var typeId = CreateTicketType();

        var ticketDate = DateTime.Today.AddDays(20);

        var result = await _tickets.RequestTicket(new RequestTicketInput
        {
            TicketTypeId = typeId,
            TicketDate = ticketDate,
            Amount = 0
        });

        var row = UsingDbContext(c => c.Set<EmployeeTicket>().IgnoreQueryFilters().Single(t => t.Id == result.TransactionId));
        row.PaymentDate.Date.ShouldBe(ticketDate.Date);
    }

    [Fact]
    public async Task Each_Service_Should_Return_Its_Own_Lookup_Names()
    {
        // Proves the Include is wired per type -- without it the screen shows blank rows.
        CreateCurrentUserEmployee("MTX280");
        var typeId = CreateLoanType();

        await _loans.RequestLoan(new RequestLoanInput
        {
            LoanTypeId = typeId,
            ActualAmount = 500,
            StartDate = DateTime.Today,
            RecoveryStartDate = DateTime.Today,
            EndDate = DateTime.Today.AddMonths(5)
        });

        var listed = await _loans.GetMyLoans(ListInput());

        listed.Items.Single().LoanTypeNameEn.ShouldBe("Housing");
    }
}
