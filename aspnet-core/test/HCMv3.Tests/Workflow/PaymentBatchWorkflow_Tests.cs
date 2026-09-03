using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// The HR payment batch -- one wage type, many employees, one approval (legacy
/// <c>Workflow.Payroll.PaymentRequest</c>).
///
/// <para>
/// <b>What these tests are actually for.</b> Legacy created the <c>EmployeePayment</c> rows from
/// <c>PaymentRequest.OnSaving</c>, inside <c>if (RequestStatus == Approved)</c> -- on EVERY save of an
/// already-approved request, with nothing recorded anywhere that would have let it notice they had already
/// been made. Opening an approved batch and saving it again paid everybody in it a second time. Real money.
/// The duplication tests below exist to keep that defect out, and are named so the intent survives anybody
/// who later wonders why the payments are not simply created wherever the batch is saved.
/// </para>
///
/// <para>
/// The other two rules under test -- no employee twice in one batch, no zero amount -- were DEFINED in legacy
/// (<c>PaymentRequestDetail.PerformValidation</c>) and never invoked from anywhere, so neither has ever run
/// in production. They run now.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class PaymentBatchWorkflow_Tests : AppTestBase
{
    private readonly IEmployeePaymentBatchesAppService _paymentBatches;
    private readonly PaymentBatchWorkflowTransactionHandler _handler;
    private readonly IWorkflowApprovalService _approvalService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public PaymentBatchWorkflow_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _paymentBatches = Resolve<IEmployeePaymentBatchesAppService>();
        _handler = Resolve<PaymentBatchWorkflowTransactionHandler>();
        _approvalService = Resolve<IWorkflowApprovalService>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    #region Fixtures

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

    /// <summary>
    /// The batch has no subject employee of its own, so it is routed against the person who RAISED it. The
    /// signed-in test admin therefore has to be linked to an employee record, exactly as a real HR user is.
    /// </summary>
    private int CreateRequesterEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    /// <summary>
    /// A wage type that passes legacy's selectable-for-payment predicate: active, an Earning, not part of
    /// basic pay, part of calculated pay, not an arrear type, not code 9000, and not claimed by any overtime,
    /// absence or loan type.
    /// </summary>
    private int CreatePayableWageType(string code = null)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            wageType = context.Set<WageType>().Add(new WageType
            {
                TenantId = TenantId,
                Code = code ?? Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Goodwill Payment",
                NameAr = "دفعة",
                Status = true,
                BillingType = BillingType.Earning,
                IncludeInBasicPay = false,
                IncludeInCalculatedPay = true,
                IncludeInArrearPay = false,
                PayrollOrder = 10
            }).Entity;
        });

        return wageType.Id;
    }

    private int CreateCurrency(string code = "KWD")
    {
        Currency currency = null;
        UsingDbContext(context =>
        {
            currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = code + Guid.NewGuid().ToString("N").Substring(0, 4),
                NameEn = "Kuwaiti Dinar",
                NameAr = "دينار كويتي",
                DecimalCount = 3
            }).Entity;
        });

        return currency.Id;
    }

    /// <summary>Turns the type on for approval with a real approver holding the step's role.</summary>
    private int EnableWorkflow(string approverCode)
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
                Code = "PB-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
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

            var type = context.Set<WorkflowTransactionType>()
                .First(t => t.Code == PaymentBatchWorkflowTransactionHandler.PaymentBatchRequestType);
            type.IsWorkflowEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private static CreateOrEditEmployeePaymentBatchDto BatchFor(
        int wageTypeId,
        int? currencyId,
        params (int EmployeeId, double Amount)[] lines)
    {
        return new CreateOrEditEmployeePaymentBatchDto
        {
            WageTypeId = wageTypeId,
            CurrencyId = currencyId,
            PaymentDate = new DateTime(2026, 6, 25),
            UnitType = UnitType.Amount,
            Comments = "Eid goodwill payment",
            Details = lines.Select(l => new CreateOrEditEmployeePaymentBatchDetailDto
            {
                EmployeeId = l.EmployeeId,
                Amount = l.Amount
            }).ToList()
        };
    }

    private List<EmployeePayment> PaymentsForBatch(int batchId) =>
        UsingDbContext(c => c.Set<EmployeePayment>()
            .IgnoreQueryFilters()
            .Where(p => p.EmployeePaymentBatchId == batchId)
            .ToList());

    private EmployeePaymentBatch BatchRow(int batchId) =>
        UsingDbContext(c => c.Set<EmployeePaymentBatch>().IgnoreQueryFilters().Single(b => b.Id == batchId));

    private WorkflowRequest RequestForBatch(int batchId) =>
        UsingDbContext(c => c.Set<WorkflowRequest>().Single(
            r => r.RequestType == PaymentBatchWorkflowTransactionHandler.PaymentBatchRequestType
                 && r.TargetEntityId == batchId));

    private async Task InUnitOfWorkAsync(Func<Task> action)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            await action();
            await uow.CompleteAsync();
        }
    }

    #endregion

    #region Submit

    [Fact]
    public async Task Submitting_Should_Create_The_Batch_Hidden_By_The_ApprovedOnly_Filter()
    {
        // Until somebody approves it, a batch is a proposal. Payroll must not be able to see it at all, and
        // it must certainly not have produced any payments.
        var requesterId = CreateRequesterEmployee("PB100REQ");
        var firstId = CreateEmployee("PB100A");
        var secondId = CreateEmployee("PB100B");
        var wageTypeId = CreatePayableWageType();
        var currencyId = CreateCurrency();
        EnableWorkflow("PB100APP");

        var batchId = await _paymentBatches.CreateEmployeePaymentBatch(
            BatchFor(wageTypeId, currencyId, (firstId, 100), (secondId, 250)));

        // Hidden: an ordinary query with the filter on cannot see it.
        var visible = UsingDbContext(c => c.Set<EmployeePaymentBatch>().Any(b => b.Id == batchId));
        visible.ShouldBeFalse("A batch awaiting approval was visible to ordinary queries.");

        // But it exists, in flight, with its lines.
        BatchRow(batchId).WorkflowStatus.ShouldBe(RequestStatus.Open);

        var lineCount = UsingDbContext(c => c.Set<EmployeePaymentBatchDetail>()
            .Count(d => d.EmployeePaymentBatchId == batchId));
        lineCount.ShouldBe(2);

        // And nothing has been paid.
        PaymentsForBatch(batchId).ShouldBeEmpty("Payments were created before anybody approved the batch.");

        // Routed against the person who raised it -- a batch has no single subject employee.
        RequestForBatch(batchId).EmployeeId.ShouldBe(requesterId);
    }

    [Fact]
    public async Task The_Summary_Should_Say_What_Is_Paid_To_How_Many_People_For_How_Much()
    {
        // A single approval here moves an arbitrary amount of money to an arbitrary number of people. An
        // approver shown only "Payment Batch" cannot tell 350 from 350,000 without opening it.
        CreateRequesterEmployee("PB110REQ");
        var firstId = CreateEmployee("PB110A");
        var secondId = CreateEmployee("PB110B");
        var wageTypeId = CreatePayableWageType();
        EnableWorkflow("PB110APP");

        var batchId = await _paymentBatches.CreateEmployeePaymentBatch(
            BatchFor(wageTypeId, null, (firstId, 100), (secondId, 250)));

        var summary = RequestForBatch(batchId).SummaryEn;

        summary.ShouldContain("Goodwill Payment");
        summary.ShouldContain("2 employee(s)");
        summary.ShouldContain("350");
        summary.ShouldContain("25 Jun 2026");
    }

    #endregion

    #region Approval

    [Fact]
    public async Task Approving_Should_Write_Exactly_One_Payment_Per_Line_With_The_Batch_Header_Values()
    {
        var requesterId = CreateRequesterEmployee("PB200REQ");
        var firstId = CreateEmployee("PB200A");
        var secondId = CreateEmployee("PB200B");
        var wageTypeId = CreatePayableWageType();
        var currencyId = CreateCurrency();
        var approverId = EnableWorkflow("PB200APP");

        var batchId = await _paymentBatches.CreateEmployeePaymentBatch(
            BatchFor(wageTypeId, currencyId, (firstId, 100.5), (secondId, 250.25)));

        await _approvalService.ApproveAsync(RequestForBatch(batchId).Id, approverId, "Agreed");

        var payments = PaymentsForBatch(batchId);
        payments.Count.ShouldBe(2);

        var first = payments.Single(p => p.EmployeeId == firstId);
        first.Amount.ShouldBe(100.5);
        first.WageTypeId.ShouldBe(wageTypeId);
        first.CurrencyId.ShouldBe(currencyId);
        first.PaymentDate.ShouldBe(new DateTime(2026, 6, 25));
        first.UnitType.ShouldBe(UnitType.Amount);
        first.EmployeePaymentBatchId.ShouldBe(batchId);

        var second = payments.Single(p => p.EmployeeId == secondId);
        second.Amount.ShouldBe(250.25);

        // The batch itself is now effective, so payroll can see it.
        BatchRow(batchId).WorkflowStatus.ShouldBe(RequestStatus.Approved);
        UsingDbContext(c => c.Set<EmployeePaymentBatch>().Any(b => b.Id == batchId)).ShouldBeTrue();

        // The requester is unpaid -- they raised the batch, they are not on it.
        payments.ShouldNotContain(p => p.EmployeeId == requesterId);
    }

    [Fact]
    public async Task Approving_Should_Round_The_Amount_To_Three_Decimals()
    {
        // KEPT FROM LEGACY DELIBERATELY. Math.Round(amount, 3) is not tidying-up: the base currency is the
        // Kuwaiti dinar, which is divided into 1000 fils, so three decimals is the actual precision of the
        // money. Rounding to two would discard fils on every line.
        CreateRequesterEmployee("PB210REQ");
        var employeeId = CreateEmployee("PB210A");
        var wageTypeId = CreatePayableWageType();
        var approverId = EnableWorkflow("PB210APP");

        var batchId = await _paymentBatches.CreateEmployeePaymentBatch(
            BatchFor(wageTypeId, null, (employeeId, 100.123456)));

        await _approvalService.ApproveAsync(RequestForBatch(batchId).Id, approverId, "Agreed");

        PaymentsForBatch(batchId).ShouldHaveSingleItem().Amount.ShouldBe(100.123);
    }

    [Fact]
    public async Task Rejecting_Should_Write_No_Payments_At_All()
    {
        var requesterId = CreateRequesterEmployee("PB220REQ");
        var employeeId = CreateEmployee("PB220A");
        var wageTypeId = CreatePayableWageType();
        var approverId = EnableWorkflow("PB220APP");

        var batchId = await _paymentBatches.CreateEmployeePaymentBatch(
            BatchFor(wageTypeId, null, (employeeId, 500)));

        await _approvalService.RejectAsync(RequestForBatch(batchId).Id, approverId, "Not budgeted");

        PaymentsForBatch(batchId).ShouldBeEmpty("A rejected batch paid somebody.");
        BatchRow(batchId).WorkflowStatus.ShouldBe(RequestStatus.Rejected);

        // And it stays invisible, so nothing downstream ever sees it.
        UsingDbContext(c => c.Set<EmployeePaymentBatch>().Any(b => b.Id == batchId)).ShouldBeFalse();
        requesterId.ShouldBeGreaterThan(0);
    }

    #endregion

    #region The legacy defect: paying twice

    [Fact]
    public async Task Legacy_Bug_Approving_Twice_Must_Not_Pay_Anybody_Twice()
    {
        // THE test. Legacy created the payments from OnSaving whenever an approved request was saved, so
        // re-saving an approved batch paid everybody in it again. If this fails, that defect is back.
        CreateRequesterEmployee("PB300REQ");
        var employeeId = CreateEmployee("PB300A");
        var wageTypeId = CreatePayableWageType();
        var approverId = EnableWorkflow("PB300APP");

        var batchId = await _paymentBatches.CreateEmployeePaymentBatch(
            BatchFor(wageTypeId, null, (employeeId, 750)));

        var requestId = RequestForBatch(batchId).Id;
        await _approvalService.ApproveAsync(requestId, approverId, "Agreed");

        PaymentsForBatch(batchId).Count.ShouldBe(1);

        // Re-run the approval hook directly -- a retry, a replayed message, or somebody re-saving the row.
        await InUnitOfWorkAsync(() => _handler.OnApprovedAsync(ContextFor(requestId)));

        PaymentsForBatch(batchId).Count.ShouldBe(1, "Approving the batch a second time paid everybody again.");
    }

    [Fact]
    public async Task An_Amendment_Must_Not_Pay_Anybody_Again()
    {
        // Same guard from the other direction, and the reason LeaveWorkflowTransactionHandler carries the
        // identical `if (context.IsAmendment) return;` line: an amendment targets a batch that has ALREADY
        // paid out. Running the creation branch for one would pay the whole batch a second time.
        CreateRequesterEmployee("PB310REQ");
        var employeeId = CreateEmployee("PB310A");
        var wageTypeId = CreatePayableWageType();
        var approverId = EnableWorkflow("PB310APP");

        var batchId = await _paymentBatches.CreateEmployeePaymentBatch(
            BatchFor(wageTypeId, null, (employeeId, 400)));

        var requestId = RequestForBatch(batchId).Id;
        await _approvalService.ApproveAsync(requestId, approverId, "Agreed");

        UsingDbContext(c =>
        {
            var request = c.Set<WorkflowRequest>().Single(r => r.Id == requestId);
            request.IsAmendment = true;
        });

        // The base class routes an approved amendment to ApplyAmendmentAsync, which this handler has not
        // opted into -- so it refuses loudly rather than silently creating a second set of payments.
        await Should.ThrowAsync<UserFriendlyException>(
            () => InUnitOfWorkAsync(() => _handler.OnApprovedAsync(ContextFor(requestId))));

        PaymentsForBatch(batchId).Count.ShouldBe(1, "An amendment paid the batch a second time.");
    }

    private WorkflowTransactionContext ContextFor(int requestId)
    {
        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == requestId));
        return new WorkflowTransactionContext(request, request.EmployeeId ?? 0);
    }

    #endregion

    #region The rules legacy defined and never ran

    [Fact]
    public async Task Should_Refuse_The_Same_Employee_Twice_In_One_Batch()
    {
        // legacy PaymentRequestDetail.PerformValidation() contains this check and NOTHING CALLS IT, so in
        // production a batch could pay the same person twice in one document.
        CreateRequesterEmployee("PB400REQ");
        var employeeId = CreateEmployee("PB400A");
        var wageTypeId = CreatePayableWageType();
        EnableWorkflow("PB400APP");

        var exception = await Should.ThrowAsync<UserFriendlyException>(
            () => _paymentBatches.CreateEmployeePaymentBatch(
                BatchFor(wageTypeId, null, (employeeId, 100), (employeeId, 200))));

        exception.Message.ShouldContain("PB400A");
    }

    [Fact]
    public async Task Should_Refuse_A_Zero_Amount()
    {
        // The other half of the never-invoked validation. A zero line produces an EmployeePayment worth
        // nothing, which is noise on a payslip and a reconciliation question later.
        CreateRequesterEmployee("PB410REQ");
        var employeeId = CreateEmployee("PB410A");
        var wageTypeId = CreatePayableWageType();
        EnableWorkflow("PB410APP");

        await Should.ThrowAsync<UserFriendlyException>(
            () => _paymentBatches.CreateEmployeePaymentBatch(
                BatchFor(wageTypeId, null, (employeeId, 0))));
    }

    [Fact]
    public async Task Should_Refuse_A_Batch_With_No_Lines()
    {
        // An addition, not a port: legacy would happily route a batch with no lines, so an approver could be
        // asked to approve nothing and the approval would create nothing.
        CreateRequesterEmployee("PB420REQ");
        var wageTypeId = CreatePayableWageType();
        EnableWorkflow("PB420APP");

        await Should.ThrowAsync<UserFriendlyException>(
            () => _paymentBatches.CreateEmployeePaymentBatch(BatchFor(wageTypeId, null)));
    }

    #endregion

    #region Wage type filtering (legacy PaymentRequest.PaymentWageType)

    [Fact]
    public async Task Selectable_Wage_Types_Should_Exclude_Everything_Legacy_Excluded()
    {
        var payableId = CreatePayableWageType();

        // Each of these fails exactly one clause of legacy's predicate.
        var inactiveId = CreateWageTypeWith(w => w.Status = false);
        var timeTypeId = CreateWageTypeWith(w => w.BillingType = BillingType.TimeType);
        var basicPayId = CreateWageTypeWith(w => w.IncludeInBasicPay = true);
        var notCalculatedId = CreateWageTypeWith(w => w.IncludeInCalculatedPay = false);
        var arrearId = CreateWageTypeWith(w => w.IncludeInArrearPay = true);
        var reservedId = CreateWageTypeWith(w => w.Code = "9000");

        // ...and these three are claimed by another engine, which writes them itself.
        var overtimeClaimedId = CreatePayableWageType();
        var absenceClaimedId = CreatePayableWageType();
        var loanClaimedId = CreatePayableWageType();
        ClaimByOvertimeType(overtimeClaimedId);
        ClaimByAbsenceType(absenceClaimedId);
        ClaimByLoanType(loanClaimedId);

        var selectable = (await _paymentBatches.GetSelectableWageTypes()).Select(w => w.Id).ToList();

        selectable.ShouldContain(payableId);
        selectable.ShouldNotContain(inactiveId);
        selectable.ShouldNotContain(timeTypeId);
        selectable.ShouldNotContain(basicPayId);
        selectable.ShouldNotContain(notCalculatedId);
        selectable.ShouldNotContain(arrearId);
        selectable.ShouldNotContain(reservedId);
        selectable.ShouldNotContain(overtimeClaimedId);
        selectable.ShouldNotContain(absenceClaimedId);
        selectable.ShouldNotContain(loanClaimedId);
    }

    [Fact]
    public async Task Should_Refuse_A_Wage_Type_That_Is_Not_Selectable()
    {
        // Legacy expressed this as a DataSourceProperty, so the rule existed only while a form was open and
        // nothing verified it on save. It is checked server-side here.
        CreateRequesterEmployee("PB510REQ");
        var employeeId = CreateEmployee("PB510A");
        var basicPayWageTypeId = CreateWageTypeWith(w => w.IncludeInBasicPay = true);
        EnableWorkflow("PB510APP");

        await Should.ThrowAsync<UserFriendlyException>(
            () => _paymentBatches.CreateEmployeePaymentBatch(
                BatchFor(basicPayWageTypeId, null, (employeeId, 100))));
    }

    private int CreateWageTypeWith(Action<WageType> mutate)
    {
        WageType wageType = null;
        UsingDbContext(context =>
        {
            var entity = new WageType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "WT",
                NameAr = "WT",
                Status = true,
                BillingType = BillingType.Earning,
                IncludeInBasicPay = false,
                IncludeInCalculatedPay = true,
                IncludeInArrearPay = false,
                PayrollOrder = 10
            };

            mutate(entity);

            wageType = context.Set<WageType>().Add(entity).Entity;
        });

        return wageType.Id;
    }

    private void ClaimByOvertimeType(int wageTypeId)
    {
        UsingDbContext(context => context.Set<OvertimeType>().Add(new OvertimeType
        {
            TenantId = TenantId,
            Code = Guid.NewGuid().ToString("N").Substring(0, 8),
            NameEn = "OT",
            NameAr = "OT",
            WageTypeId = wageTypeId
        }));
    }

    private void ClaimByAbsenceType(int wageTypeId)
    {
        UsingDbContext(context => context.Set<AbsenceType>().Add(new AbsenceType
        {
            TenantId = TenantId,
            Code = Guid.NewGuid().ToString("N").Substring(0, 8),
            NameEn = "AB",
            NameAr = "AB",
            WageTypeId = wageTypeId
        }));
    }

    private void ClaimByLoanType(int wageTypeId)
    {
        UsingDbContext(context => context.Set<LoanType>().Add(new LoanType
        {
            TenantId = TenantId,
            Code = Guid.NewGuid().ToString("N").Substring(0, 8),
            NameEn = "LN",
            NameAr = "LN",
            WageTypeId = wageTypeId
        }));
    }

    #endregion

    #region Editing

    [Fact]
    public async Task Should_Refuse_To_Change_A_Batch_That_Has_Been_Submitted()
    {
        // Editing a batch sitting in an approver's inbox changes what they are agreeing to underneath them.
        CreateRequesterEmployee("PB600REQ");
        var employeeId = CreateEmployee("PB600A");
        var wageTypeId = CreatePayableWageType();
        EnableWorkflow("PB600APP");

        var batchId = await _paymentBatches.CreateEmployeePaymentBatch(
            BatchFor(wageTypeId, null, (employeeId, 100)));

        var edit = BatchFor(wageTypeId, null, (employeeId, 999));
        edit.Id = batchId;

        await Should.ThrowAsync<UserFriendlyException>(
            () => _paymentBatches.UpdateEmployeePaymentBatch(edit));

        UsingDbContext(c => c.Set<EmployeePaymentBatchDetail>()
            .Single(d => d.EmployeePaymentBatchId == batchId).Amount).ShouldBe(100);
    }

    [Fact]
    public async Task Should_Refuse_To_Delete_A_Batch_That_Has_Already_Paid_Out()
    {
        CreateRequesterEmployee("PB610REQ");
        var employeeId = CreateEmployee("PB610A");
        var wageTypeId = CreatePayableWageType();
        var approverId = EnableWorkflow("PB610APP");

        var batchId = await _paymentBatches.CreateEmployeePaymentBatch(
            BatchFor(wageTypeId, null, (employeeId, 100)));

        await _approvalService.ApproveAsync(RequestForBatch(batchId).Id, approverId, "Agreed");

        await Should.ThrowAsync<UserFriendlyException>(
            () => _paymentBatches.Delete(new Abp.Application.Services.Dto.EntityDto(batchId)));

        PaymentsForBatch(batchId).Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetAll_Should_Show_A_Batch_Awaiting_Approval_With_Its_Line_Count_And_Total()
    {
        // This IS the screen where HR watches a batch move through approval, so it deliberately looks past
        // the ApprovedOnly filter. Every other reader keeps it on.
        CreateRequesterEmployee("PB620REQ");
        var firstId = CreateEmployee("PB620A");
        var secondId = CreateEmployee("PB620B");
        var wageTypeId = CreatePayableWageType();
        EnableWorkflow("PB620APP");

        var batchId = await _paymentBatches.CreateEmployeePaymentBatch(
            BatchFor(wageTypeId, null, (firstId, 120), (secondId, 80)));

        var result = await _paymentBatches.GetAll(new GetAllEmployeePaymentBatchesInput { WageTypeId = wageTypeId });

        var row = result.Items.ShouldHaveSingleItem();
        row.Id.ShouldBe(batchId);
        row.LineCount.ShouldBe(2);
        row.TotalAmount.ShouldBe(200);
        row.WorkflowStatus.ShouldBe(RequestStatus.Open);
    }

    #endregion

    #region Catalog and registration

    [Fact]
    public void Payment_Batch_Should_Not_Be_Offered_As_Self_Service()
    {
        // HR pays a group; an employee does not request a batch about themselves. A route here with no ESS
        // service behind it would be a dead tile.
        var type = UsingDbContext(c => c.Set<WorkflowTransactionType>()
            .First(t => t.Code == PaymentBatchWorkflowTransactionHandler.PaymentBatchRequestType));

        type.EssRoute.ShouldBeNull();
        type.EssPermissionName.ShouldBeNull();
        type.IsEssEnabled.ShouldBeFalse();
    }

    [Fact]
    public void The_Handler_Should_Be_Resolvable_By_Its_Request_Type()
    {
        Resolve<IWorkflowTransactionHandlerResolver>()
            .Resolve(PaymentBatchWorkflowTransactionHandler.PaymentBatchRequestType)
            .ShouldBeOfType<PaymentBatchWorkflowTransactionHandler>();
    }

    #endregion
}
