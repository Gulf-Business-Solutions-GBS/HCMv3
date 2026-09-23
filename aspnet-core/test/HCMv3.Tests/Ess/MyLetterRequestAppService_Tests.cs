using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Runtime.Validation;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Self-service employment letters -- the salary certificate an employee needs for a bank, a landlord or an
/// embassy.
///
/// <para>
/// The rule that is unique to this type, and therefore the one most worth protecting, is what approval buys.
/// Every other transaction type approves into a side effect: quota moves, a deduction is scheduled, an asset
/// becomes issued. Approving a letter writes nothing anywhere -- it UNLOCKS PRINTING, and the only place
/// that fact surfaces is the <c>CanPrint</c> flag on the list. If that flag were wrong in the permissive
/// direction, an unapproved salary certificate would be printed on company letterhead and handed to a bank.
/// So it is tested from both sides: false while the request is pending, true once it is approved.
/// </para>
///
/// <para>
/// The rest is the shared self-service contract, which must not have been bypassed on the way in: the row is
/// hidden from everyone else until it is approved, a user with no employee record is refused rather than
/// shown somebody's data, and the required addressee is genuinely required.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyLetterRequestAppService_Tests : AppTestBase
{
    private readonly IMyLetterRequestAppService _letters;

    public MyLetterRequestAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _letters = Resolve<IMyLetterRequestAppService>();
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

    /// <summary>A document type the company issues as a letter, i.e. one the request screen would offer.</summary>
    private int CreateLetterType(string nameEn = "Salary Certificate")
    {
        DocumentType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = "شهادة راتب",
                IsLetterType = true
            }).Entity;
        });

        return type.Id;
    }

    /// <summary>A document type the company merely records -- a passport. Not issuable as a letter.</summary>
    private int CreateNonLetterType()
    {
        DocumentType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Passport",
                NameAr = "جواز سفر",
                IsLetterType = false
            }).Entity;
        });

        return type.Id;
    }

    /// <summary>Turns letter requests on for approval, pointing them at a one-step chain with a real approver.</summary>
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
                Code = "LTR-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
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
                .First(t => t.Code == LetterRequestWorkflowTransactionHandler.LetterRequestType);
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    private static RequestLetterInput LetterInput(int documentTypeId) => new RequestLetterInput
    {
        DocumentTypeId = documentTypeId,
        EffectiveDate = DateTime.Today,
        AddressToEn = "The Manager, National Bank of Kuwait",
        AddressToAr = "المدير، بنك الكويت الوطني",
        Comments = "For a housing loan application"
    };

    // ------------------------------------------------------------------ hidden until approved

    [Fact]
    public async Task A_Submitted_Letter_Request_Should_Be_Hidden_Until_It_Is_Approved()
    {
        // The one-row design's premise: the pending request lives in the real table, so it must be the
        // ApprovedOnly filter -- not a shadow request table -- that keeps it out of everything else.
        CreateCurrentUserEmployee("LTR100");
        var approverId = EnableWorkflow("LTR100APP");
        var documentTypeId = CreateLetterType();

        var result = await _letters.RequestLetter(LetterInput(documentTypeId));
        result.SubmittedForApproval.ShouldBeTrue();

        var repository = Resolve<IRepository<EmployeeLetterRequest>>();
        var unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        // Read through a repository inside a unit of work, because that is where ABP data filters live. A
        // raw DbContext query would bypass ApprovedOnly and pass whatever the filter did.
        using (unitOfWorkManager.Begin())
        {
            repository.GetAll().Any(l => l.Id == result.TransactionId)
                .ShouldBeFalse("A pending letter request was visible to the rest of the system.");
        }

        // The employee still sees their own -- that is why they opened the page.
        var mine = await _letters.GetMyLetterRequests(ListInput());
        mine.Items.Select(i => i.Id).ShouldContain(result.TransactionId);

        await Resolve<IWorkflowApprovalService>()
            .ApproveAsync(result.WorkflowRequestId.Value, approverId, "Fine");

        using (unitOfWorkManager.Begin())
        {
            repository.GetAll().Any(l => l.Id == result.TransactionId)
                .ShouldBeTrue("An approved letter request never became visible.");
        }
    }

    // ------------------------------------------------------------------ CanPrint, both sides

    [Fact]
    public async Task CanPrint_Should_Be_False_While_The_Request_Is_Still_Pending()
    {
        // THE important one, in the direction that costs something to get wrong. Legacy refused outright --
        // "Request is not approved. You can't print." -- and if this flag were true here, an unapproved
        // salary certificate would go out on company letterhead to a bank.
        CreateCurrentUserEmployee("LTR200");
        EnableWorkflow("LTR200APP");
        var documentTypeId = CreateLetterType();

        var result = await _letters.RequestLetter(LetterInput(documentTypeId));

        var mine = await _letters.GetMyLetterRequests(ListInput());
        var row = mine.Items.Single(i => i.Id == result.TransactionId);

        row.WorkflowStatus.ShouldBe(RequestStatus.Open, "The request must still be in flight for this test " +
                                                       "to be testing anything.");
        row.CanPrint.ShouldBeFalse("A pending letter request offered a print button.");
    }

    [Fact]
    public async Task CanPrint_Should_Become_True_Once_The_Request_Is_Approved()
    {
        // The other side of the same rule, and the only thing approving a letter actually does. Without this
        // half, a CanPrint that simply returned false would pass the test above and never let anyone print.
        CreateCurrentUserEmployee("LTR210");
        var approverId = EnableWorkflow("LTR210APP");
        var documentTypeId = CreateLetterType();

        var result = await _letters.RequestLetter(LetterInput(documentTypeId));

        await Resolve<IWorkflowApprovalService>()
            .ApproveAsync(result.WorkflowRequestId.Value, approverId, "Approved");

        var mine = await _letters.GetMyLetterRequests(ListInput());
        var row = mine.Items.Single(i => i.Id == result.TransactionId);

        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        row.CanPrint.ShouldBeTrue("An approved letter request still refused to print.");
    }

    [Fact]
    public async Task CanPrint_Should_Be_False_For_A_Rejected_Request()
    {
        // Approved is the ONLY status that unlocks printing. Worth stating separately from "pending",
        // because a decided-but-refused request is the case a status check written as "not pending" would
        // wave through.
        CreateCurrentUserEmployee("LTR220");
        var approverId = EnableWorkflow("LTR220APP");
        var documentTypeId = CreateLetterType();

        var result = await _letters.RequestLetter(LetterInput(documentTypeId));

        await Resolve<IWorkflowApprovalService>()
            .RejectAsync(result.WorkflowRequestId.Value, approverId, "Not this time");

        var mine = await _letters.GetMyLetterRequests(ListInput());
        var row = mine.Items.Single(i => i.Id == result.TransactionId);

        row.WorkflowStatus.ShouldBe(RequestStatus.Rejected);
        row.CanPrint.ShouldBeFalse("A rejected letter request offered a print button.");
    }

    [Fact]
    public async Task CanPrint_Should_Be_False_For_A_Row_Recorded_With_Workflow_Off()
    {
        // Deliberate and worth pinning down, because it is the one place this flag disagrees with the rest
        // of the system: a null WorkflowStatus means "effective, not workflow-managed" everywhere else, and
        // every other query treats such a row as live. A letter is different -- nobody has signed it, and a
        // certificate leaves the building on company letterhead.
        CreateCurrentUserEmployee("LTR230");
        var documentTypeId = CreateLetterType();

        var result = await _letters.RequestLetter(LetterInput(documentTypeId));
        result.SubmittedForApproval.ShouldBeFalse("The type is seeded dormant, so nothing should have routed.");

        var mine = await _letters.GetMyLetterRequests(ListInput());
        var row = mine.Items.Single(i => i.Id == result.TransactionId);

        row.WorkflowStatus.ShouldBeNull();
        row.CanPrint.ShouldBeFalse();
    }

    // ------------------------------------------------------------------ the required addressee

    [Fact]
    public async Task A_Letter_With_No_English_Addressee_Should_Be_Refused()
    {
        // A certificate addressed to nobody is not usable by the bank or embassy that asked for it, so it
        // must not reach an approver in the first place.
        CreateCurrentUserEmployee("LTR300");
        var documentTypeId = CreateLetterType();

        var input = LetterInput(documentTypeId);
        input.AddressToEn = null;

        await Assert.ThrowsAsync<AbpValidationException>(() => _letters.RequestLetter(input));
    }

    [Fact]
    public async Task A_Letter_With_No_Arabic_Addressee_Should_Be_Refused()
    {
        // Both are required: the Arabic template prints this field, so a blank one produces a letter with an
        // empty header rather than one that quietly omits it.
        CreateCurrentUserEmployee("LTR310");
        var documentTypeId = CreateLetterType();

        var input = LetterInput(documentTypeId);
        input.AddressToAr = null;

        await Assert.ThrowsAsync<AbpValidationException>(() => _letters.RequestLetter(input));
    }

    [Fact]
    public async Task An_Addressee_Of_Only_Blanks_Should_Be_Refused()
    {
        // Legacy tested for null only, which let a space through and printed a certificate with an empty
        // "To:" line. ABP's method-invocation validator normalises the argument before the [Required] check,
        // so whitespace is refused before the service is entered.
        CreateCurrentUserEmployee("LTR320");
        var documentTypeId = CreateLetterType();

        var input = LetterInput(documentTypeId);
        input.AddressToEn = "   ";

        await Assert.ThrowsAsync<AbpValidationException>(() => _letters.RequestLetter(input));
    }

    [Fact]
    public void The_Handler_Should_Refuse_A_Letter_With_No_Addressee_Whoever_Raised_It()
    {
        // Driven through the handler directly, because the DTO annotations above only guard the ESS form.
        // The rule has to hold for anything that reaches the engine -- an HR-entered row later submitted for
        // approval, or an import -- so it is tested where it actually lives.
        var meId = CreateCurrentUserEmployee("LTR330");
        var documentTypeId = CreateLetterType();

        var letterId = 0;
        var requestId = 0;

        UsingDbContext(context =>
        {
            var letter = context.Set<EmployeeLetterRequest>().Add(new EmployeeLetterRequest
            {
                TenantId = TenantId,
                EmployeeId = meId,
                DocumentTypeId = documentTypeId,
                EffectiveDate = DateTime.Today,
                AddressToEn = "   ",
                AddressToAr = "   ",
                WorkflowStatus = RequestStatus.Draft
            }).Entity;

            context.SaveChanges();
            letterId = letter.Id;

            var request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = meId,
                RequestType = LetterRequestWorkflowTransactionHandler.LetterRequestType,
                TargetEntityId = letterId,
                RequestNumber = "LTR-TEST-00001",
                RequestStatus = RequestStatus.Draft,
                WorkflowStep = 0
            }).Entity;

            context.SaveChanges();
            requestId = request.Id;
        });

        var handler = Resolve<IWorkflowTransactionHandlerResolver>()
            .Resolve(LetterRequestWorkflowTransactionHandler.LetterRequestType);

        handler.ShouldNotBeNull("The letter request type has no handler, so it would route with no validation.");

        var unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        using (var uow = unitOfWorkManager.Begin())
        {
            var request = Resolve<IRepository<WorkflowRequest>>().Get(requestId);
            var context = new WorkflowTransactionContext(request, meId);

            Should.Throw<UserFriendlyException>(() => handler.ValidateBeforeSubmitAsync(context));

            uow.Complete();
        }
    }

    // ------------------------------------------------------------------ which document types are letters

    [Fact]
    public async Task A_Document_Type_That_Is_Not_A_Letter_Should_Be_Refused()
    {
        // The dropdown is filtered client-side, and a filtered dropdown is not a rule. Posting the id of a
        // passport must not produce a "letter" the company has no template for.
        CreateCurrentUserEmployee("LTR400");
        var passportTypeId = CreateNonLetterType();

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _letters.RequestLetter(LetterInput(passportTypeId)));
    }

    // ------------------------------------------------------------------ the shared contract

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        // Admin accounts are never linked to an employee, so this is the normal state of a user who has no
        // business in self-service -- not an edge case. Refusing beats quietly listing nothing, which would
        // read as "you have no letters".
        var documentTypeId = CreateLetterType();

        await Assert.ThrowsAsync<UserFriendlyException>(() => _letters.GetMyLetterRequests(ListInput()));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _letters.RequestLetter(LetterInput(documentTypeId)));
    }

    [Fact]
    public async Task A_Letter_Should_Be_Attributed_To_The_Signed_In_Employee_And_Numbered_With_Its_Own_Prefix()
    {
        // There is no employee field on the input at all, deliberately -- a self-service form must not be
        // able to request somebody else's salary certificate, which would disclose their pay.
        var meId = CreateCurrentUserEmployee("LTR500");
        CreateEmployee("LTR500OTHER");
        EnableWorkflow("LTR500APP");
        var documentTypeId = CreateLetterType();

        var result = await _letters.RequestLetter(LetterInput(documentTypeId));

        var row = UsingDbContext(c => c.Set<EmployeeLetterRequest>().IgnoreQueryFilters()
            .Single(l => l.Id == result.TransactionId));

        row.EmployeeId.ShouldBe(meId);
        row.AddressToEn.ShouldBe("The Manager, National Bank of Kuwait");

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value));
        request.RequestNumber.ShouldStartWith("LTR-");
    }

    [Fact]
    public async Task Should_Not_List_Somebody_Elses_Letters()
    {
        // Scoping matters more here than on most types: the letter names the employee and the certificate
        // discloses their salary.
        CreateCurrentUserEmployee("LTR510");
        var otherId = CreateEmployee("LTR510OTHER");
        var documentTypeId = CreateLetterType();

        UsingDbContext(context =>
        {
            context.Set<EmployeeLetterRequest>().Add(new EmployeeLetterRequest
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                DocumentTypeId = documentTypeId,
                EffectiveDate = DateTime.Today,
                AddressToEn = "Somebody else's bank",
                AddressToAr = "بنك آخر"
            });
        });

        var mine = await _letters.GetMyLetterRequests(ListInput());

        mine.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Withdrawing_Should_Remove_The_Row_And_Close_Its_Request()
    {
        // Closing the request matters as much as deleting the row: otherwise an approver is left with an
        // inbox entry pointing at a letter nobody wants any more.
        CreateCurrentUserEmployee("LTR520");
        EnableWorkflow("LTR520APP");
        var documentTypeId = CreateLetterType();

        var result = await _letters.RequestLetter(LetterInput(documentTypeId));

        await _letters.CancelMyLetterRequest(new EntityDto(result.TransactionId));

        UsingDbContext(c => c.Set<EmployeeLetterRequest>().IgnoreQueryFilters()
            .Any(l => l.Id == result.TransactionId && !l.IsDeleted)).ShouldBeFalse();

        UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value))
            .RequestStatus.ShouldBe(RequestStatus.Closed);
    }

    [Fact]
    public void Letter_Requests_Should_Be_Offered_As_Self_Service()
    {
        // The catalog row is what makes the tile appear at all, and the permission name on it has to match
        // the constant the service is decorated with or the entry silently never renders.
        var type = UsingDbContext(c => c.Set<WorkflowTransactionType>()
            .First(t => t.Code == LetterRequestWorkflowTransactionHandler.LetterRequestType));

        type.EssRoute.ShouldBe("/app/main/ess/letter");
        type.EssPermissionName.ShouldBe(HCMv3.Authorization.AppPermissions.Ess_MyLetterRequests);

        // Seeded dormant like every other type -- turning it on is an admin decision.
        type.IsWorkflowEnabled.ShouldBeFalse();
        type.IsEssEnabled.ShouldBeFalse();

        // Routed by document type, so a salary certificate can need a different signature from a
        // to-whom-it-may-concern letter.
        type.SubTypeSource.ShouldBe("DocumentType");
    }
}
