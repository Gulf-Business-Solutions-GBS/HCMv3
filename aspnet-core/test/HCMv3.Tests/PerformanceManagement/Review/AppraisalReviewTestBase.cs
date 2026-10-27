using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using Abp.Localization;
using HCMv3.GeneralMasterInfo;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Review;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace HCMv3.Tests.PerformanceManagement.Review;

/// <summary>
/// Shared set-up for the appraisal review tests: a private SQLite database, the real domain services over
/// it, and builders for the rows a review needs. Services are built by hand -- as everywhere in
/// PerformanceManagement's tests -- so <c>[AbpAuthorize]</c> is not exercised here, only the rules.
/// </summary>
public abstract class AppraisalReviewTestBase : AppTestBase
{
    protected readonly AppraisalReviewTestDbContext Context;

    private readonly SqliteConnection _connection;

    /// <summary>The employee the session is signed in as. Null is the unlinked admin.</summary>
    protected int? Me { get; set; }

    /// <summary>Who the scope provider says reports to <see cref="Me"/>.</summary>
    protected List<int> DirectReports { get; } = new();

    protected ManageAppraisalSettings AppraisalSettings { get; } = new()
    {
        AppraisalCycle = AppraisalCycle.Quarterly
    };

    /// <summary>What the routing resolver answers for every request and every appraisal.</summary>
    protected RequestMaster RoutedTo { get; set; }

    protected IWorkflowApprovalService ApprovalService { get; } = Substitute.For<IWorkflowApprovalService>();

    protected AppraisalReviewTestBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppraisalReviewTestDbContext>()
            .UseSqlite(_connection)
            .Options;

        Context = new AppraisalReviewTestDbContext(options);
        Context.Database.EnsureCreated();
    }

    protected int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- services

    protected IUnitOfWorkManager CreateFakeUnitOfWorkManager()
    {
        var activeUnitOfWork = Substitute.For<IActiveUnitOfWork>();
        activeUnitOfWork.SaveChangesAsync().Returns(_ => Context.SaveChangesAsync());

        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        unitOfWorkManager.Current.Returns(activeUnitOfWork);

        return unitOfWorkManager;
    }

    protected TService Wire<TService>(TService service) where TService : DomainService
    {
        service.LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>();
        service.UnitOfWorkManager = CreateFakeUnitOfWorkManager();
        return service;
    }

    protected TService WireApp<TService>(TService service) where TService : ApplicationService
    {
        service.AbpSession = AbpSession;
        service.LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>();
        service.UnitOfWorkManager = CreateFakeUnitOfWorkManager();
        return service;
    }

    protected ISysSettingsAccessor CreateSettingsAccessor()
    {
        var accessor = Substitute.For<ISysSettingsAccessor>();
        accessor.GetAppraisalSettingsAsync().Returns(_ => Task.FromResult(AppraisalSettings));
        return accessor;
    }

    protected IRequestMasterResolver CreateRequestMasterResolver()
    {
        var resolver = Substitute.For<IRequestMasterResolver>();
        resolver.ResolveAsync(Arg.Any<WorkflowRequest>()).Returns(_ => Task.FromResult(RoutedTo));
        resolver.ResolveAsync(
                Arg.Any<string>(),
                Arg.Any<int?>(),
                Arg.Any<int?>(),
                Arg.Any<int?>(),
                Arg.Any<WorkflowRequestAction>())
            .Returns(_ => Task.FromResult(RoutedTo));
        return resolver;
    }

    protected IEmployeeScopeProvider CreateScopeProvider()
    {
        var scope = Substitute.For<IEmployeeScopeProvider>();
        scope.GetCurrentEmployeeIdAsync().Returns(_ => Task.FromResult(Me));
        scope.GetRequiredCurrentEmployeeIdAsync().Returns(_ => Me.HasValue
            ? Task.FromResult(Me.Value)
            : throw new Abp.UI.UserFriendlyException("not linked"));
        scope.IsInScopeAsync(Arg.Any<int>(), Arg.Is(EmployeeScope.DirectReports), Arg.Any<DateTime?>())
            .Returns(call => Task.FromResult(DirectReports.Contains(call.ArgAt<int>(0))));
        scope.ApplyScopeAsync(Arg.Any<IQueryable<PAEmployee>>(), Arg.Is(EmployeeScope.DirectReports), Arg.Any<DateTime?>())
            .Returns(call =>
            {
                var reports = DirectReports.ToList();
                return Task.FromResult(call.ArgAt<IQueryable<PAEmployee>>(0).Where(a => reports.Contains(a.EmployeeId)));
            });
        return scope;
    }

    protected AppraisalStepSettingResolver CreateStepSettingResolver()
    {
        return Wire(new AppraisalStepSettingResolver(
            ReviewFakeRepository.For<AppraisalStepSetting>(Context),
            ReviewFakeRepository.For<AppraisalStepSettingRelationship>(Context),
            ReviewFakeRepository.For<WorkflowRequest>(Context),
            CreateRequestMasterResolver()));
    }

    protected AppraisalReviewerManager CreateReviewerManager()
    {
        return Wire(new AppraisalReviewerManager(
            ReviewFakeRepository.For<PAEmployeeReviewer>(Context),
            ReviewFakeRepository.For<PAEmployeeDetail>(Context),
            ReviewFakeRepository.For<PAEmployeeRating>(Context),
            ReviewFakeRepository.For<FormType>(Context),
            ReviewFakeRepository.For<EmployeeManager>(Context),
            ReviewFakeRepository.For<EmployeeRelationship>(Context)));
    }

    protected PAEmployeeScoreCalculator CreateScoreCalculator()
    {
        return Wire(new PAEmployeeScoreCalculator(
            ReviewFakeRepository.For<PAEmployeeRating>(Context),
            ReviewFakeRepository.For<PAEmployeeReviewer>(Context),
            ReviewFakeRepository.For<PAEmployee>(Context),
            ReviewFakeRepository.For<PARating>(Context),
            CreateSettingsAccessor()));
    }

    // ---------------------------------------------------------------- rows

    protected int CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
    {
        var nationality = new Nationality { TenantId = TenantId, NameEn = "N", NameAr = "N" };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();

        var employee = new Employee
        {
            TenantId = TenantId,
            Code = code,
            FirstNameEn = code,
            LastNameEn = "T",
            FirstNameAr = "ت",
            LastNameAr = "ت",
            FullNameEn = code + " T",
            FullNameAr = code + " ت",
            NationalityId = nationality.Id,
            JoiningDate = new DateTime(2020, 1, 1),
            Status = status
        };
        Context.Employees.Add(employee);
        Context.SaveChanges();
        return employee.Id;
    }

    protected int CreateFormType(int maxReviewerLimit = 0)
    {
        var formType = new FormType { TenantId = TenantId, Code = "360", NameEn = "360 Review", NameAr = "تقييم 360", MaxReviewerLimit = maxReviewerLimit };
        Context.FormTypes.Add(formType);
        Context.SaveChanges();
        return formType.Id;
    }

    protected int CreateCycle(string name = "2026")
    {
        var cycle = new PACycle { TenantId = TenantId, Code = name, NameEn = name, NameAr = name };
        Context.PACycles.Add(cycle);
        Context.SaveChanges();
        return cycle.Id;
    }

    protected PAEmployee CreateAppraisal(int employeeId, int? formTypeId = null, int? cycleId = null)
    {
        var appraisal = new PAEmployee
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            FormTypeId = formTypeId ?? CreateFormType(),
            PACycleId = cycleId ?? CreateCycle()
        };
        Context.PAEmployees.Add(appraisal);
        Context.SaveChanges();
        return appraisal;
    }

    /// <summary>A chain with the given steps, which is also what every resolver call now routes to.</summary>
    protected RequestMaster CreateRequestMaster(params int[] steps)
    {
        var master = new RequestMaster { TenantId = TenantId, Code = "APR-" + Guid.NewGuid().ToString("N")[..6], NameEn = "Appraisal", NameAr = "تقييم", WorkflowSteps = steps.Length };
        Context.RequestMasters.Add(master);
        Context.SaveChanges();

        foreach (var step in steps)
        {
            Context.RequestDetails.Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = step,
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = ApprovalRequire.Single
            });
        }

        Context.SaveChanges();
        RoutedTo = master;
        return master;
    }

    /// <summary>Puts the appraisal into the chain at <paramref name="step"/>.</summary>
    protected WorkflowRequest Route(PAEmployee appraisal, int step, RequestStatus status = RequestStatus.Open)
    {
        var request = new WorkflowRequest
        {
            TenantId = TenantId,
            EmployeeId = appraisal.EmployeeId,
            RequestType = "PAEmployee",
            RequestNumber = Guid.NewGuid().ToString("N")[..8],
            RequestStatus = status,
            WorkflowStep = step,
            TargetEntityId = appraisal.Id
        };
        Context.WorkflowRequests.Add(request);
        Context.SaveChanges();

        appraisal.WorkflowRequestId = request.Id;
        appraisal.WorkflowStatus = status;
        Context.SaveChanges();

        return request;
    }

    protected WorkFlowApprover AddApprover(WorkflowRequest request, int step, int approverId, bool isApproved = false)
    {
        var row = new WorkFlowApprover
        {
            TenantId = TenantId,
            WorkflowRequestId = request.Id,
            WorkflowStep = step,
            ApproverId = approverId,
            IsApproved = isApproved
        };
        Context.WorkFlowApprovers.Add(row);
        Context.SaveChanges();
        return row;
    }

    protected AppraisalStepSetting CreateStepSetting(RequestMaster master, int step, Action<AppraisalStepSetting> configure = null, params int[] relationshipTypeIds)
    {
        var setting = new AppraisalStepSetting { TenantId = TenantId, RequestMasterId = master.Id, Step = step };
        configure?.Invoke(setting);
        Context.AppraisalStepSettings.Add(setting);
        Context.SaveChanges();

        foreach (var typeId in relationshipTypeIds)
        {
            Context.AppraisalStepSettingRelationships.Add(new AppraisalStepSettingRelationship
            {
                TenantId = TenantId,
                AppraisalStepSettingId = setting.Id,
                RelationshipTypeId = typeId
            });
        }

        Context.SaveChanges();
        return setting;
    }

    protected int CreateKpi(string code)
    {
        var category = new KpiCategory { TenantId = TenantId, Code = "C" + code, NameEn = "Category " + code, NameAr = "فئة" };
        Context.KpiCategories.Add(category);
        Context.SaveChanges();

        var kpi = new KpiInfo { TenantId = TenantId, Code = code, NameEn = "KPI " + code, NameAr = "مؤشر " + code, KpiCategoryId = category.Id, Active = true };
        Context.KpiInfos.Add(kpi);
        Context.SaveChanges();
        return kpi.Id;
    }

    protected PAEmployeeDetail AddKpiLine(PAEmployee appraisal, int kpiInfoId, double weight = 50)
    {
        var detail = new PAEmployeeDetail { TenantId = TenantId, PAEmployeeId = appraisal.Id, KpiInfoId = kpiInfoId, Weight = weight, Target = "T" };
        Context.PAEmployeeDetails.Add(detail);
        Context.SaveChanges();
        return detail;
    }

    /// <summary>A reviewer with an empty line for every KPI already on the appraisal, as the reviewer rules make one.</summary>
    protected PAEmployeeReviewer AddReviewer(PAEmployee appraisal, int employeeId, bool isPeer, bool submitted = false, int? addedBy = null)
    {
        var reviewer = new PAEmployeeReviewer
        {
            TenantId = TenantId,
            PAEmployeeId = appraisal.Id,
            EmployeeId = employeeId,
            IsPeer = isPeer,
            ReviewSubmitted = submitted,
            PeerAddedById = addedBy
        };
        Context.PAEmployeeReviewers.Add(reviewer);
        Context.SaveChanges();

        foreach (var detail in Context.PAEmployeeDetails.Where(d => d.PAEmployeeId == appraisal.Id).ToList())
        {
            Context.PAEmployeeRatings.Add(new PAEmployeeRating
            {
                TenantId = TenantId,
                PAEmployeeId = appraisal.Id,
                PAEmployeeReviewerId = reviewer.Id,
                PAEmployeeDetailId = detail.Id
            });
        }

        Context.SaveChanges();
        return reviewer;
    }

    protected PARating CreateRating(string code, int maxLimit = 10, bool commentRequired = false, bool active = true)
    {
        var rating = new PARating { TenantId = TenantId, Code = code, NameEn = "Rating " + code, NameAr = "تقدير " + code, MaxLimit = maxLimit, IsCommentRequired = commentRequired, Status = active };
        Context.PARatings.Add(rating);
        Context.SaveChanges();
        return rating;
    }

    protected void SetManager(int employeeId, int managerId)
    {
        Context.EmployeeManagers.Add(new EmployeeManager
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            ManagerId = managerId,
            StartDate = DateTime.Today.AddYears(-1),
            EndDate = DateTime.Today.AddYears(1)
        });
        Context.SaveChanges();
    }

    protected int CreateRelationshipType(string name)
    {
        var type = new RelationshipType { TenantId = TenantId, Code = name, NameEn = name, NameAr = name };
        Context.RelationshipTypes.Add(type);
        Context.SaveChanges();
        return type.Id;
    }

    protected void Relate(int employeeId, int relatedEmployeeId, int relationshipTypeId, DateTime? start = null, DateTime? end = null)
    {
        Context.EmployeeRelationships.Add(new EmployeeRelationship
        {
            TenantId = TenantId,
            EmployeeId = employeeId,
            RelatedEmployeeId = relatedEmployeeId,
            RelationshipTypeId = relationshipTypeId,
            StartDate = start ?? DateTime.Today.AddYears(-1),
            EndDate = end ?? DateTime.Today.AddYears(1)
        });
        Context.SaveChanges();
    }

    protected List<PAEmployeeReviewer> ReviewersOf(PAEmployee appraisal)
    {
        return Context.PAEmployeeReviewers.AsNoTracking().Where(r => r.PAEmployeeId == appraisal.Id).OrderBy(r => r.Id).ToList();
    }
}
