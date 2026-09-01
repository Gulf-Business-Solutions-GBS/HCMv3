using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.ManpowerPlanningManagement;
using HCMv3.ManpowerPlanningManagement.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.ManpowerPlanningManagement;

/// <summary>
/// The app service's own logic, with workflow switched off for the type -- the seeded default. Routing,
/// approval and the derived status are covered against the real engine in
/// <c>HCMv3.Tests.Workflow.ManpowerPlanningWorkflow_Tests</c>.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ManpowerPlanningsAppService_Tests : ManpowerPlanningManagementAppServiceTestBase
{
    /// <summary>
    /// Stands in for the real submitter with the transaction type NOT workflow-enabled: the row is inserted
    /// and left with a null <c>WorkflowStatus</c>, which is exactly what
    /// <c>AdminTransactionSubmitter</c> does when the catalog row is off.
    /// </summary>
    private sealed class NotRoutedSubmitter : IAdminTransactionSubmitter
    {
        public async Task<int> CreateAndSubmitAsync<TEntity>(
            IRepository<TEntity> repository,
            TEntity entity,
            string requestType,
            string requestNumberPrefix,
            int? subTypeId = null)
            where TEntity : class, IEntity<int>, IHasEmployeeId, IWorkflowEnabledEntity
        {
            entity.WorkflowStatus = null;
            await repository.InsertAsync(entity);
            return entity.Id;
        }
    }

    private HCMv3.ManpowerPlanningManagement.ManpowerPlanningsAppService CreateAppService()
    {
        return CreateService(() => new HCMv3.ManpowerPlanningManagement.ManpowerPlanningsAppService(
            FakeRepository.For<HCMv3.ManpowerPlanningManagement.ManpowerPlanning>(Context),
            new NotRoutedSubmitter()));
    }

    private int CreatePeriod(string name)
    {
        var entity = new ManpowerPlanningPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            Name = name,
            Active = true,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        Context.ManpowerPlanningPeriods.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ManpowerPlanning()
    {
        var employeeId = CreateEmployee("M1");
        var orgUnitId = CreateOrganizationUnit("Dept1");
        var periodId = CreatePeriod("2026");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditManpowerPlanningDto
        {
            EmployeeId = employeeId,
            OrganizationUnitId = orgUnitId,
            PeriodId = periodId,
            RequestComments = "Initial plan"
        });

        var result = await service.GetAll(new GetAllManpowerPlanningsInput());
        result.TotalCount.ShouldBe(1);

        // Not routed -> WorkflowStatus null -> the projection reads Draft.
        result.Items[0].Status.ShouldBe(ManpowerPlanningStatus.Draft);
    }

    [Fact]
    public async Task Should_Carry_Detail_Lines_Sent_With_The_Header()
    {
        // The lines have to be insertable in the same call, because once the type is routed the plan is
        // submitted the instant the header exists and the handler refuses an empty one.
        var employeeId = CreateEmployee("M1D");
        var orgUnitId = CreateOrganizationUnit("Dept1D");
        var periodId = CreatePeriod("2026-Lines");
        var jobTitleId = CreateJobTitle("JTLines");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditManpowerPlanningDto
        {
            EmployeeId = employeeId,
            OrganizationUnitId = orgUnitId,
            PeriodId = periodId,
            Details = new List<CreateOrEditManpowerPlanningDetailDto>
            {
                new CreateOrEditManpowerPlanningDetailDto { JobTitleId = jobTitleId, January = 1000, February = 500 }
            }
        });

        var plan = Context.ManpowerPlannings.Single(m => m.PeriodId == periodId);
        Context.ManpowerPlanningDetails.Count(d => d.ManpowerPlanningId == plan.Id).ShouldBe(1);
        plan.GrandTotalAmount.ShouldBe(1500);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_OrgUnit_And_Period()
    {
        var employeeId = CreateEmployee("M2");
        var orgUnitId = CreateOrganizationUnit("Dept2");
        var periodId = CreatePeriod("2026-B");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditManpowerPlanningDto
        {
            EmployeeId = employeeId,
            OrganizationUnitId = orgUnitId,
            PeriodId = periodId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditManpowerPlanningDto
            {
                EmployeeId = employeeId,
                OrganizationUnitId = orgUnitId,
                PeriodId = periodId
            }));
    }

    [Fact]
    public async Task A_Rejected_Plan_Should_Not_Block_A_Fresh_One_For_The_Same_Period()
    {
        // A department whose plan was declined must be able to raise a revised one, or a single rejection
        // locks that department out of the period permanently.
        var employeeId = CreateEmployee("M5");
        var orgUnitId = CreateOrganizationUnit("Dept5");
        var periodId = CreatePeriod("2026-E");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditManpowerPlanningDto
        {
            EmployeeId = employeeId,
            OrganizationUnitId = orgUnitId,
            PeriodId = periodId
        });

        var rejected = Context.ManpowerPlannings.Single(m => m.PeriodId == periodId);
        rejected.WorkflowStatus = RequestStatus.Rejected;
        Context.SaveChanges();

        await service.CreateOrEdit(new CreateOrEditManpowerPlanningDto
        {
            EmployeeId = employeeId,
            OrganizationUnitId = orgUnitId,
            PeriodId = periodId
        });

        Context.ManpowerPlannings.Count(m => m.PeriodId == periodId).ShouldBe(2);
    }

    [Fact]
    public void The_Derived_Status_Should_Follow_The_Engine_And_Nothing_Else()
    {
        // The mapping in one place, asserted directly: there is no setter, so there is nothing else that
        // could produce a different answer.
        ManpowerPlanning.FromWorkflowStatus(null).ShouldBe(ManpowerPlanningStatus.Draft);
        ManpowerPlanning.FromWorkflowStatus(RequestStatus.Draft).ShouldBe(ManpowerPlanningStatus.Draft);
        ManpowerPlanning.FromWorkflowStatus(RequestStatus.Open).ShouldBe(ManpowerPlanningStatus.Submitted);
        ManpowerPlanning.FromWorkflowStatus(RequestStatus.Approved).ShouldBe(ManpowerPlanningStatus.Approved);
        ManpowerPlanning.FromWorkflowStatus(RequestStatus.Closed).ShouldBe(ManpowerPlanningStatus.Approved);
        ManpowerPlanning.FromWorkflowStatus(RequestStatus.Rejected).ShouldBe(ManpowerPlanningStatus.Rejected);
    }
}
