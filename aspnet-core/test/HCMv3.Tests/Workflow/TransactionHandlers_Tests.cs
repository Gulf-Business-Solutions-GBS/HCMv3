using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers the five handlers added alongside the leave reference implementation: assets, documents, loans,
/// overtime and tickets.
///
/// <para>
/// Every one of these validations shares a single trap, and it is the reason they are tested rather than
/// assumed. Each has to see rows that are still awaiting approval, and those rows are exactly what the
/// <c>ApprovedOnly</c> filter hides. Forget to disable the filter and the check silently passes on every
/// pending row: two people are issued the same laptop, the same overtime day is claimed twice, a second loan
/// is stacked on the first. Nothing errors -- the duplicate is simply approved.
/// </para>
///
/// <para>
/// So for each handler the pending case is asserted explicitly, not just the approved one.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class TransactionHandlers_Tests : AppTestBase
{
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public TransactionHandlers_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateEmployee(string code)
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
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int CreateAsset(string nameEn = "Laptop", string nameAr = "حاسوب")
    {
        Asset asset = null;
        UsingDbContext(context =>
        {
            asset = context.Set<Asset>().Add(new Asset
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameAr
            }).Entity;
        });

        return asset.Id;
    }

    private int CreateDocumentType(string nameEn = "Passport", string nameAr = "جواز")
    {
        DocumentType documentType = null;
        UsingDbContext(context =>
        {
            documentType = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameAr
            }).Entity;
        });

        return documentType.Id;
    }

    private int CreateLoanType(string nameEn = "Housing", string nameAr = "سكن")
    {
        LoanType loanType = null;
        UsingDbContext(context =>
        {
            loanType = context.Set<LoanType>().Add(new LoanType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameAr
            }).Entity;
        });

        return loanType.Id;
    }

    private int CreateOvertimeType(string nameEn = "Weekday", string nameAr = "عادي")
    {
        OvertimeType overtimeType = null;
        UsingDbContext(context =>
        {
            overtimeType = context.Set<OvertimeType>().Add(new OvertimeType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameAr
            }).Entity;
        });

        return overtimeType.Id;
    }

    private int CreateTicketType(string nameEn = "Annual Ticket", string nameAr = "تذكرة سنوية")
    {
        TicketType ticketType = null;
        UsingDbContext(context =>
        {
            ticketType = context.Set<TicketType>().Add(new TicketType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameAr
            }).Entity;
        });

        return ticketType.Id;
    }

    private WorkflowTransactionContext CreateContext(int employeeId, string requestType, int? targetId)
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                RequestType = requestType,
                TargetEntityId = targetId,
                RequestNumber = "REQ-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                RequestStatus = RequestStatus.Draft
            }).Entity;
        });

        return new WorkflowTransactionContext(request, employeeId);
    }

    private async Task<T> InUnitOfWorkAsync<T>(Func<Task<T>> action)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            var result = await action();
            await uow.CompleteAsync();

            return result;
        }
    }

    private async Task InUnitOfWorkAsync(Func<Task> action)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            await action();
            await uow.CompleteAsync();
        }
    }

    // ----------------------------------------------------------------- assets

    private int CreateAssetAssignment(int employeeId, int assetId, DateTime start, DateTime end, RequestStatus? status)
    {
        EmployeeAsset assignment = null;
        UsingDbContext(context =>
        {
            assignment = context.Set<EmployeeAsset>().Add(new EmployeeAsset
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                AssetId = assetId,
                StartDate = start,
                EndDate = end,
                WorkflowStatus = status
            }).Entity;
        });

        return assignment.Id;
    }

    [Theory]
    [InlineData(null)]                      // entered directly by HR, no workflow
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Open)]        // the case the ApprovedOnly filter would hide
    public async Task An_Asset_Should_Not_Be_Issued_Twice_Over_The_Same_Dates(RequestStatus? existingStatus)
    {
        var handler = Resolve<AssetWorkflowTransactionHandler>();
        var holderId = CreateEmployee("AST1" + existingStatus);
        var claimantId = CreateEmployee("AST2" + existingStatus);
        var assetId = CreateAsset();

        CreateAssetAssignment(holderId, assetId, new DateTime(2026, 1, 1), new DateTime(2026, 6, 30), existingStatus);

        var contested = CreateAssetAssignment(claimantId, assetId, new DateTime(2026, 3, 1), new DateTime(2026, 9, 30), RequestStatus.Draft);
        var context = CreateContext(claimantId, AssetWorkflowTransactionHandler.AssetRequestType, contested);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    [Theory]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Draft)]
    public async Task An_Abandoned_Asset_Request_Should_Not_Block_A_New_One(RequestStatus? existingStatus)
    {
        // Neither a rejected nor an unsubmitted request represents a claim on the asset.
        var handler = Resolve<AssetWorkflowTransactionHandler>();
        var holderId = CreateEmployee("AST3" + existingStatus);
        var claimantId = CreateEmployee("AST4" + existingStatus);
        var assetId = CreateAsset();

        CreateAssetAssignment(holderId, assetId, new DateTime(2026, 1, 1), new DateTime(2026, 6, 30), existingStatus);

        var wanted = CreateAssetAssignment(claimantId, assetId, new DateTime(2026, 3, 1), new DateTime(2026, 9, 30), RequestStatus.Draft);
        var context = CreateContext(claimantId, AssetWorkflowTransactionHandler.AssetRequestType, wanted);

        await InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context));
    }

    [Fact]
    public async Task A_Non_Overlapping_Asset_Assignment_Should_Be_Allowed()
    {
        var handler = Resolve<AssetWorkflowTransactionHandler>();
        var holderId = CreateEmployee("AST5");
        var claimantId = CreateEmployee("AST6");
        var assetId = CreateAsset();

        CreateAssetAssignment(holderId, assetId, new DateTime(2026, 1, 1), new DateTime(2026, 2, 28), RequestStatus.Approved);

        var later = CreateAssetAssignment(claimantId, assetId, new DateTime(2026, 3, 1), new DateTime(2026, 9, 30), RequestStatus.Draft);
        var context = CreateContext(claimantId, AssetWorkflowTransactionHandler.AssetRequestType, later);

        await InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context));
    }

    [Fact]
    public async Task Approving_An_Asset_Request_Should_Stamp_The_Assignment_Row()
    {
        // Proves the inherited status bookkeeping works through a second entity type, not only leave. That
        // column is what the ApprovedOnly filter reads, so a handler that failed to write it would leave the
        // assignment invisible to the rest of the system after approval.
        var handler = Resolve<AssetWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("AST7");
        var assetId = CreateAsset();
        var assignmentId = CreateAssetAssignment(employeeId, assetId, new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), RequestStatus.Open);

        var context = CreateContext(employeeId, AssetWorkflowTransactionHandler.AssetRequestType, assignmentId);

        await InUnitOfWorkAsync(() => handler.OnApprovedAsync(context));

        var stored = UsingDbContext(c => c.Set<EmployeeAsset>().IgnoreQueryFilters().Single(a => a.Id == assignmentId));
        stored.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        stored.WorkflowRequestId.ShouldBe(context.Request.Id);
    }

    [Fact]
    public async Task An_Asset_Summary_Should_Name_The_Asset()
    {
        var handler = Resolve<AssetWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("AST8");
        var assetId = CreateAsset("Dell Latitude", "ديل");
        var assignmentId = CreateAssetAssignment(employeeId, assetId, new DateTime(2026, 5, 4), new DateTime(2026, 12, 31), RequestStatus.Draft);

        var context = CreateContext(employeeId, AssetWorkflowTransactionHandler.AssetRequestType, assignmentId);

        var summary = await InUnitOfWorkAsync(() => handler.BuildSummaryAsync(context));

        summary.SummaryEn.ShouldContain("Dell Latitude");
        summary.SummaryEn.ShouldContain("04 May 2026");
        summary.SummaryAr.ShouldContain("ديل");
    }

    // -------------------------------------------------------------- documents

    private int CreateDocument(int employeeId, int documentTypeId, string number, DateTime issue, DateTime expiry)
    {
        EmployeeDocument document = null;
        UsingDbContext(context =>
        {
            document = context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DocumentTypeId = documentTypeId,
                DocumentNum = number,
                IssueDate = issue,
                ExpiryDate = expiry,
                WorkflowStatus = RequestStatus.Draft
            }).Entity;
        });

        return document.Id;
    }

    [Fact]
    public async Task A_Document_Expiring_Before_It_Was_Issued_Should_Be_Refused()
    {
        var handler = Resolve<DocumentWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("DOC1");
        var typeId = CreateDocumentType();
        var documentId = CreateDocument(employeeId, typeId, "P123", new DateTime(2026, 6, 1), new DateTime(2026, 1, 1));

        var context = CreateContext(employeeId, DocumentWorkflowTransactionHandler.DocumentRequestType, documentId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    [Fact]
    public async Task A_Document_With_No_Number_Should_Be_Refused()
    {
        var handler = Resolve<DocumentWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("DOC2");
        var typeId = CreateDocumentType();
        var documentId = CreateDocument(employeeId, typeId, "   ", new DateTime(2026, 1, 1), new DateTime(2030, 1, 1));

        var context = CreateContext(employeeId, DocumentWorkflowTransactionHandler.DocumentRequestType, documentId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    [Fact]
    public async Task A_Document_Summary_Should_Carry_The_Number_And_Expiry()
    {
        var handler = Resolve<DocumentWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("DOC3");
        var typeId = CreateDocumentType("Passport", "جواز سفر");
        var documentId = CreateDocument(employeeId, typeId, "P998877", new DateTime(2026, 1, 1), new DateTime(2031, 3, 9));

        var context = CreateContext(employeeId, DocumentWorkflowTransactionHandler.DocumentRequestType, documentId);

        var summary = await InUnitOfWorkAsync(() => handler.BuildSummaryAsync(context));

        summary.SummaryEn.ShouldContain("P998877");
        summary.SummaryEn.ShouldContain("09 Mar 2031");
    }

    // ------------------------------------------------------------------ loans

    private int CreateLoan(
        int employeeId,
        int loanTypeId,
        double amount,
        double balanceDue,
        RequestStatus? status,
        DateTime? start = null,
        DateTime? recoveryStart = null)
    {
        var startDate = start ?? new DateTime(2026, 1, 1);

        EmployeeLoan loan = null;
        UsingDbContext(context =>
        {
            loan = context.Set<EmployeeLoan>().Add(new EmployeeLoan
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                LoanTypeId = loanTypeId,
                StartDate = startDate,
                RecoveryStartDate = recoveryStart ?? startDate,
                EndDate = startDate.AddYears(1),
                ActualAmount = amount,
                BalanceDueAmount = balanceDue,
                WorkflowStatus = status
            }).Entity;
        });

        return loan.Id;
    }

    [Fact]
    public async Task A_Loan_For_Nothing_Should_Be_Refused()
    {
        var handler = Resolve<LoanWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("LOAN1");
        var typeId = CreateLoanType();
        var loanId = CreateLoan(employeeId, typeId, amount: 0, balanceDue: 0, status: RequestStatus.Draft);

        var context = CreateContext(employeeId, LoanWorkflowTransactionHandler.LoanRequestType, loanId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    [Fact]
    public async Task A_Loan_Recovering_Before_It_Starts_Should_Be_Refused()
    {
        var handler = Resolve<LoanWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("LOAN2");
        var typeId = CreateLoanType();

        var loanId = CreateLoan(
            employeeId,
            typeId,
            amount: 1000,
            balanceDue: 0,
            status: RequestStatus.Draft,
            start: new DateTime(2026, 6, 1),
            recoveryStart: new DateTime(2026, 1, 1));

        var context = CreateContext(employeeId, LoanWorkflowTransactionHandler.LoanRequestType, loanId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    [Fact]
    public async Task A_Loan_Still_Awaiting_Approval_Should_Block_A_Second_One()
    {
        // The case the ApprovedOnly filter would hide: without disabling it the pending loan is invisible
        // and an employee could push several past different approvers before any is decided.
        var handler = Resolve<LoanWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("LOAN3");
        var typeId = CreateLoanType();

        CreateLoan(employeeId, typeId, amount: 5000, balanceDue: 0, status: RequestStatus.Open);

        var secondId = CreateLoan(employeeId, typeId, amount: 3000, balanceDue: 0, status: RequestStatus.Draft);
        var context = CreateContext(employeeId, LoanWorkflowTransactionHandler.LoanRequestType, secondId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    [Fact]
    public async Task An_Approved_Loan_Still_Being_Repaid_Should_Block_A_Second_One()
    {
        var handler = Resolve<LoanWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("LOAN4");
        var typeId = CreateLoanType();

        CreateLoan(employeeId, typeId, amount: 5000, balanceDue: 1200, status: RequestStatus.Approved);

        var secondId = CreateLoan(employeeId, typeId, amount: 3000, balanceDue: 0, status: RequestStatus.Draft);
        var context = CreateContext(employeeId, LoanWorkflowTransactionHandler.LoanRequestType, secondId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    [Fact]
    public async Task A_Fully_Repaid_Loan_Should_Not_Block_A_New_One()
    {
        var handler = Resolve<LoanWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("LOAN5");
        var typeId = CreateLoanType();

        CreateLoan(employeeId, typeId, amount: 5000, balanceDue: 0, status: RequestStatus.Approved);

        var secondId = CreateLoan(employeeId, typeId, amount: 3000, balanceDue: 0, status: RequestStatus.Draft);
        var context = CreateContext(employeeId, LoanWorkflowTransactionHandler.LoanRequestType, secondId);

        await InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context));
    }

    [Fact]
    public async Task A_Loan_Of_A_Different_Type_Should_Not_Block()
    {
        var handler = Resolve<LoanWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("LOAN6");
        var housingId = CreateLoanType("Housing");
        var carId = CreateLoanType("Car");

        CreateLoan(employeeId, housingId, amount: 5000, balanceDue: 4000, status: RequestStatus.Approved);

        var carLoanId = CreateLoan(employeeId, carId, amount: 3000, balanceDue: 0, status: RequestStatus.Draft);
        var context = CreateContext(employeeId, LoanWorkflowTransactionHandler.LoanRequestType, carLoanId);

        await InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context));
    }

    [Fact]
    public async Task A_Loan_Summary_Should_Carry_The_Amount()
    {
        var handler = Resolve<LoanWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("LOAN7");
        var typeId = CreateLoanType("Housing", "قرض سكن");
        var loanId = CreateLoan(employeeId, typeId, amount: 4500, balanceDue: 0, status: RequestStatus.Draft);

        var context = CreateContext(employeeId, LoanWorkflowTransactionHandler.LoanRequestType, loanId);

        var summary = await InUnitOfWorkAsync(() => handler.BuildSummaryAsync(context));

        summary.SummaryEn.ShouldContain("Housing");
        summary.SummaryEn.ShouldContain("4500");
        summary.SummaryAr.ShouldContain("قرض سكن");
    }

    // --------------------------------------------------------------- overtime

    private int CreateOvertime(int employeeId, int typeId, DateTime on, double hours, RequestStatus? status)
    {
        EmployeeOvertime overtime = null;
        UsingDbContext(context =>
        {
            overtime = context.Set<EmployeeOvertime>().Add(new EmployeeOvertime
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                OvertimeTypeId = typeId,
                EffectiveDate = on,
                OvertimeHours = hours,
                WorkflowStatus = status
            }).Entity;
        });

        return overtime.Id;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(25)]
    public async Task Implausible_Overtime_Hours_Should_Be_Refused(double hours)
    {
        var handler = Resolve<OvertimeWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("OT1" + hours);
        var typeId = CreateOvertimeType();
        var overtimeId = CreateOvertime(employeeId, typeId, new DateTime(2026, 4, 1), hours, RequestStatus.Draft);

        var context = CreateContext(employeeId, OvertimeWorkflowTransactionHandler.OvertimeRequestType, overtimeId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Open)]
    public async Task The_Same_Overtime_Day_Should_Not_Be_Claimed_Twice(RequestStatus? existingStatus)
    {
        var handler = Resolve<OvertimeWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("OT2" + existingStatus);
        var typeId = CreateOvertimeType();
        var day = new DateTime(2026, 4, 1);

        CreateOvertime(employeeId, typeId, day, 3, existingStatus);

        var duplicateId = CreateOvertime(employeeId, typeId, day, 2, RequestStatus.Draft);
        var context = CreateContext(employeeId, OvertimeWorkflowTransactionHandler.OvertimeRequestType, duplicateId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    [Fact]
    public async Task Overtime_On_A_Different_Day_Should_Be_Allowed()
    {
        var handler = Resolve<OvertimeWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("OT3");
        var typeId = CreateOvertimeType();

        CreateOvertime(employeeId, typeId, new DateTime(2026, 4, 1), 3, RequestStatus.Approved);

        var nextDayId = CreateOvertime(employeeId, typeId, new DateTime(2026, 4, 2), 2, RequestStatus.Draft);
        var context = CreateContext(employeeId, OvertimeWorkflowTransactionHandler.OvertimeRequestType, nextDayId);

        await InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context));
    }

    [Fact]
    public async Task An_Overtime_Summary_Should_Carry_Hours_And_Date()
    {
        var handler = Resolve<OvertimeWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("OT4");
        var typeId = CreateOvertimeType("Weekday", "أيام العمل");
        var overtimeId = CreateOvertime(employeeId, typeId, new DateTime(2026, 4, 7), 3.5, RequestStatus.Draft);

        var context = CreateContext(employeeId, OvertimeWorkflowTransactionHandler.OvertimeRequestType, overtimeId);

        var summary = await InUnitOfWorkAsync(() => handler.BuildSummaryAsync(context));

        summary.SummaryEn.ShouldContain("3.5");
        summary.SummaryEn.ShouldContain("07 Apr 2026");
    }

    // ---------------------------------------------------------------- tickets

    private int CreateTicket(int employeeId, int typeId, DateTime on, double amount, RequestStatus? status)
    {
        EmployeeTicket ticket = null;
        UsingDbContext(context =>
        {
            ticket = context.Set<EmployeeTicket>().Add(new EmployeeTicket
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                TicketTypeId = typeId,
                TicketDate = on,
                PaymentDate = on,
                Amount = amount,
                WorkflowStatus = status
            }).Entity;
        });

        return ticket.Id;
    }

    [Fact]
    public async Task A_Negative_Ticket_Amount_Should_Be_Refused()
    {
        var handler = Resolve<TicketWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("TKT1");
        var typeId = CreateTicketType();
        var ticketId = CreateTicket(employeeId, typeId, new DateTime(2026, 7, 1), -50, RequestStatus.Draft);

        var context = CreateContext(employeeId, TicketWorkflowTransactionHandler.TicketRequestType, ticketId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Open)]
    public async Task A_Second_Ticket_In_The_Same_Year_Should_Be_Refused(RequestStatus? existingStatus)
    {
        var handler = Resolve<TicketWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("TKT2" + existingStatus);
        var typeId = CreateTicketType();

        CreateTicket(employeeId, typeId, new DateTime(2026, 2, 1), 300, existingStatus);

        var secondId = CreateTicket(employeeId, typeId, new DateTime(2026, 11, 1), 300, RequestStatus.Draft);
        var context = CreateContext(employeeId, TicketWorkflowTransactionHandler.TicketRequestType, secondId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context)));
    }

    [Fact]
    public async Task A_Ticket_In_The_Following_Year_Should_Be_Allowed()
    {
        var handler = Resolve<TicketWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("TKT3");
        var typeId = CreateTicketType();

        CreateTicket(employeeId, typeId, new DateTime(2026, 2, 1), 300, RequestStatus.Approved);

        var nextYearId = CreateTicket(employeeId, typeId, new DateTime(2027, 2, 1), 300, RequestStatus.Draft);
        var context = CreateContext(employeeId, TicketWorkflowTransactionHandler.TicketRequestType, nextYearId);

        await InUnitOfWorkAsync(() => handler.ValidateBeforeSubmitAsync(context));
    }

    [Fact]
    public async Task A_Ticket_Summary_Should_Name_The_Type_And_Date()
    {
        var handler = Resolve<TicketWorkflowTransactionHandler>();
        var employeeId = CreateEmployee("TKT4");
        var typeId = CreateTicketType("Annual Ticket", "تذكرة سنوية");
        var ticketId = CreateTicket(employeeId, typeId, new DateTime(2026, 7, 21), 425, RequestStatus.Draft);

        var context = CreateContext(employeeId, TicketWorkflowTransactionHandler.TicketRequestType, ticketId);

        var summary = await InUnitOfWorkAsync(() => handler.BuildSummaryAsync(context));

        summary.SummaryEn.ShouldContain("Annual Ticket");
        summary.SummaryEn.ShouldContain("21 Jul 2026");
        summary.SummaryAr.ShouldContain("تذكرة سنوية");
    }

    // ----------------------------------------------------------------- shared

    [Fact]
    public async Task Every_Handler_Should_Tolerate_A_Request_With_No_Transaction_Row()
    {
        // A request can legitimately exist before its transaction row does. None of the handlers may throw
        // in that state, or the engine breaks on a half-built request rather than reporting it.
        var employeeId = CreateEmployee("NULLTARGET");

        var handlers = new (IWorkflowTransactionHandler Handler, string Type)[]
        {
            (Resolve<AssetWorkflowTransactionHandler>(), AssetWorkflowTransactionHandler.AssetRequestType),
            (Resolve<DocumentWorkflowTransactionHandler>(), DocumentWorkflowTransactionHandler.DocumentRequestType),
            (Resolve<LoanWorkflowTransactionHandler>(), LoanWorkflowTransactionHandler.LoanRequestType),
            (Resolve<OvertimeWorkflowTransactionHandler>(), OvertimeWorkflowTransactionHandler.OvertimeRequestType),
            (Resolve<TicketWorkflowTransactionHandler>(), TicketWorkflowTransactionHandler.TicketRequestType)
        };

        foreach (var entry in handlers)
        {
            var context = CreateContext(employeeId, entry.Type, null);

            await InUnitOfWorkAsync(() => entry.Handler.ValidateBeforeSubmitAsync(context));

            var summary = await InUnitOfWorkAsync(() => entry.Handler.BuildSummaryAsync(context));
            summary.ShouldBeNull(entry.Handler.GetType().Name + " invented a summary for a request with no row.");
        }
    }
}
