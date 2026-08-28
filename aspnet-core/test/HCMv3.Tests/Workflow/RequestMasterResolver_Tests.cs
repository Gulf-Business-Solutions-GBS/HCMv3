using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers approval routing -- which <see cref="RequestMaster"/> governs a given request.
///
/// This replaces two pieces of legacy behaviour that were never portable as written: the inline
/// <c>RequestMaster.Code == RequestType</c> match the approval engine used to do (which ignored workflow
/// groups and sub-types entirely, so every employee got the same chain), and legacy
/// <c>WorkflowGeneralService.GetRequestMaster</c>'s fallback, a 28-case switch mapping type names to magic
/// codes. The order below is the legacy order; what changed is that the last step reads a catalog row
/// instead of a compiled-in constant, and that failures throw instead of returning null.
/// </summary>
// ReSharper disable once InconsistentNaming
public class RequestMasterResolver_Tests : AppTestBase
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRequestMasterResolver _resolver;

    public RequestMasterResolver_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _resolver = Resolve<IRequestMasterResolver>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private void SetWorkflowGroupEnabled(bool enabled)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<ManageWorkflowSettings>().FirstOrDefault();
            if (settings == null)
            {
                context.Set<ManageWorkflowSettings>().Add(new ManageWorkflowSettings
                {
                    TenantId = TenantId,
                    EnableWorkflowGroup = enabled
                });
            }
            else
            {
                settings.EnableWorkflowGroup = enabled;
            }
        });
    }

    private int CreateRequestMaster(string code)
    {
        RequestMaster master = null;
        UsingDbContext(context =>
        {
            master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code,
                NameAr = code,
                WorkflowSteps = 1
            }).Entity;
        });

        return master.Id;
    }

    /// <summary>
    /// Configures a transaction type, updating the seeded row when one exists rather than inserting a
    /// second.
    ///
    /// <c>WorkflowTransactionTypeBuilder</c> seeds the six pilot types into every database -- dormant, with
    /// workflow and ESS both off -- so a plain insert here trips the unique index on (TenantId, Code). That
    /// is the index working: one code must name one type, or routing is ambiguous. Upserting also makes
    /// these tests exercise the real production shape, where the row already exists and an admin turns it on
    /// and points it at an approval chain.
    /// </summary>
    private int CreateTransactionType(
        string code,
        int? defaultRequestMasterId,
        bool isWorkflowEnabled = true,
        bool isActive = true)
    {
        WorkflowTransactionType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<WorkflowTransactionType>().FirstOrDefault(t => t.Code == code);

            if (type == null)
            {
                type = context.Set<WorkflowTransactionType>().Add(new WorkflowTransactionType
                {
                    TenantId = TenantId,
                    Code = code,
                    NameEn = code,
                    NameAr = code
                }).Entity;
            }

            type.IsWorkflowEnabled = isWorkflowEnabled;
            type.IsEssEnabled = true;
            type.IsActive = isActive;
            type.DefaultRequestMasterId = defaultRequestMasterId;
        });

        return type.Id;
    }

    private int CreateWorkflowGroup(bool isDefault)
    {
        WorkflowGroup group = null;
        UsingDbContext(context =>
        {
            group = context.Set<WorkflowGroup>().Add(new WorkflowGroup
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "G",
                NameAr = "G",
                IsDefault = isDefault
            }).Entity;
        });

        return group.Id;
    }

    private void CreateGroupDetail(
        int workflowGroupId,
        int requestMasterId,
        string requestTypeName,
        int? requestSubTypeId = null,
        int? customSubTypeId = null)
    {
        UsingDbContext(context =>
        {
            context.Set<WorkflowGroupDetail>().Add(new WorkflowGroupDetail
            {
                TenantId = TenantId,
                WorkflowGroupId = workflowGroupId,
                RequestMasterId = requestMasterId,
                RequestTypeName = requestTypeName,
                RequestSubTypeId = requestSubTypeId,
                CustomSubTypeId = customSubTypeId
            });
        });
    }

    private int CreateEmployee(string code, int? workflowGroupId = null)
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

        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = InfiniteEndDate,
                WorkflowGroupId = workflowGroupId
            });
        });

        return employee.Id;
    }

    [Fact]
    public async Task Should_Fall_Back_To_The_Catalog_Default_When_Workflow_Groups_Are_Off()
    {
        // The common case, and the direct replacement for legacy's compiled-in
        // "case \"EmployeeAbsence\": recCode = \"4\";" switch -- now a catalog row.
        SetWorkflowGroupEnabled(false);

        var masterId = CreateRequestMaster("STD-LEAVE");
        CreateTransactionType("EmployeeAbsence", masterId);
        var employeeId = CreateEmployee("RMR100");

        var resolved = await _resolver.ResolveAsync("EmployeeAbsence", employeeId, requestSubTypeId: null);

        resolved.Id.ShouldBe(masterId);
    }

    [Fact]
    public async Task Should_Prefer_The_Employees_Workflow_Group_Over_The_Catalog_Default()
    {
        SetWorkflowGroupEnabled(true);

        var defaultMasterId = CreateRequestMaster("STD-LEAVE");
        var groupMasterId = CreateRequestMaster("EXEC-LEAVE");
        CreateTransactionType("EmployeeAbsence", defaultMasterId);

        var groupId = CreateWorkflowGroup(isDefault: false);
        CreateGroupDetail(groupId, groupMasterId, "EmployeeAbsence");

        var employeeId = CreateEmployee("RMR200", workflowGroupId: groupId);

        var resolved = await _resolver.ResolveAsync("EmployeeAbsence", employeeId, requestSubTypeId: null);

        resolved.Id.ShouldBe(groupMasterId);
    }

    [Fact]
    public async Task Should_Use_The_Default_Group_When_The_Employee_Has_No_Group()
    {
        SetWorkflowGroupEnabled(true);

        var catalogMasterId = CreateRequestMaster("STD-LEAVE");
        var defaultGroupMasterId = CreateRequestMaster("DEFAULT-GROUP-LEAVE");
        CreateTransactionType("EmployeeAbsence", catalogMasterId);

        var defaultGroupId = CreateWorkflowGroup(isDefault: true);
        CreateGroupDetail(defaultGroupId, defaultGroupMasterId, "EmployeeAbsence");

        var employeeId = CreateEmployee("RMR300", workflowGroupId: null);

        var resolved = await _resolver.ResolveAsync("EmployeeAbsence", employeeId, requestSubTypeId: null);

        resolved.Id.ShouldBe(defaultGroupMasterId);
    }

    [Fact]
    public async Task Should_Prefer_A_Sub_Type_Specific_Chain_Over_The_Groups_Catch_All()
    {
        // "Annual leave needs three approvals, sick leave needs one" -- the requirement the old inline
        // Code == RequestType match could not express at all.
        SetWorkflowGroupEnabled(true);

        var catalogMasterId = CreateRequestMaster("STD-LEAVE");
        var anyLeaveMasterId = CreateRequestMaster("ANY-LEAVE");
        var sickLeaveMasterId = CreateRequestMaster("SICK-LEAVE");
        CreateTransactionType("EmployeeAbsence", catalogMasterId);

        var groupId = CreateWorkflowGroup(isDefault: true);
        CreateGroupDetail(groupId, anyLeaveMasterId, "EmployeeAbsence");
        CreateGroupDetail(groupId, sickLeaveMasterId, "EmployeeAbsence", requestSubTypeId: 77);

        var employeeId = CreateEmployee("RMR400");

        var sick = await _resolver.ResolveAsync("EmployeeAbsence", employeeId, requestSubTypeId: 77);
        var annual = await _resolver.ResolveAsync("EmployeeAbsence", employeeId, requestSubTypeId: 88);

        sick.Id.ShouldBe(sickLeaveMasterId);
        annual.Id.ShouldBe(anyLeaveMasterId);
    }

    [Fact]
    public async Task Should_Prefer_The_Custom_Sub_Type_Match_Over_The_Sub_Type_Match()
    {
        SetWorkflowGroupEnabled(true);

        var catalogMasterId = CreateRequestMaster("STD-LEAVE");
        var subTypeMasterId = CreateRequestMaster("SUBTYPE-LEAVE");
        var customMasterId = CreateRequestMaster("CUSTOM-LEAVE");
        CreateTransactionType("EmployeeAbsence", catalogMasterId);

        var groupId = CreateWorkflowGroup(isDefault: true);
        CreateGroupDetail(groupId, subTypeMasterId, "EmployeeAbsence", requestSubTypeId: 5);
        CreateGroupDetail(groupId, customMasterId, "EmployeeAbsence", requestSubTypeId: 5, customSubTypeId: 9);

        var employeeId = CreateEmployee("RMR500");

        var resolved = await _resolver.ResolveAsync("EmployeeAbsence", employeeId, requestSubTypeId: 5, customSubTypeId: 9);

        resolved.Id.ShouldBe(customMasterId);
    }

    [Fact]
    public async Task Should_Fall_Back_To_The_Catalog_When_The_Group_Has_No_Entry_For_The_Type()
    {
        SetWorkflowGroupEnabled(true);

        var catalogMasterId = CreateRequestMaster("STD-ASSET");
        CreateTransactionType("EmployeeAsset", catalogMasterId);

        var groupId = CreateWorkflowGroup(isDefault: true);
        CreateGroupDetail(groupId, CreateRequestMaster("GROUP-LEAVE"), "EmployeeAbsence");

        var employeeId = CreateEmployee("RMR600");

        var resolved = await _resolver.ResolveAsync("EmployeeAsset", employeeId, requestSubTypeId: null);

        resolved.Id.ShouldBe(catalogMasterId);
    }

    [Fact]
    public async Task Should_Throw_When_The_Transaction_Type_Is_Not_Registered()
    {
        // Legacy swallowed this in a catch{} and returned null, so a typo surfaced much later as an
        // unrelated failure. It now fails where the misconfiguration is.
        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _resolver.ResolveAsync("NoSuchTransactionType", null, null));

        exception.Message.ShouldContain("NoSuchTransactionType");
    }

    [Fact]
    public async Task Should_Throw_When_The_Type_Is_Registered_But_Has_No_Configuration()
    {
        SetWorkflowGroupEnabled(false);
        CreateTransactionType("EmployeeDocument", defaultRequestMasterId: null);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _resolver.ResolveAsync("EmployeeDocument", null, null));
    }

    [Fact]
    public async Task Should_Throw_When_Workflow_Is_Disabled_For_The_Type()
    {
        var masterId = CreateRequestMaster("STD-OT");
        CreateTransactionType("EmployeeOvertime", masterId, isWorkflowEnabled: false);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _resolver.ResolveAsync("EmployeeOvertime", null, null));
    }

    [Fact]
    public async Task Should_Throw_When_The_Type_Is_Retired()
    {
        var masterId = CreateRequestMaster("STD-TICKET");
        CreateTransactionType("EmployeeTicket", masterId, isActive: false);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _resolver.ResolveAsync("EmployeeTicket", null, null));
    }

    [Fact]
    public async Task IsWorkflowEnabled_Should_Drive_Whether_A_Transaction_Is_Routed()
    {
        var masterId = CreateRequestMaster("STD-LOAN");
        CreateTransactionType("EmployeeLoan", masterId, isWorkflowEnabled: true);
        CreateTransactionType("EmployeeAsset", masterId, isWorkflowEnabled: false);

        (await _resolver.IsWorkflowEnabledAsync("EmployeeLoan")).ShouldBeTrue();
        (await _resolver.IsWorkflowEnabledAsync("EmployeeAsset")).ShouldBeFalse();
    }

    [Fact]
    public async Task IsWorkflowEnabled_Should_Be_False_For_An_Unregistered_Type()
    {
        // Saved directly rather than blocked: an uncatalogued type is not workflow-managed, and refusing
        // every save would be a worse failure than skipping approval nobody configured.
        (await _resolver.IsWorkflowEnabledAsync("NotInTheCatalog")).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Resolve_From_A_WorkflowRequest_Using_Its_Own_Type_And_Sub_Type()
    {
        SetWorkflowGroupEnabled(true);

        var catalogMasterId = CreateRequestMaster("STD-LEAVE");
        var sickMasterId = CreateRequestMaster("SICK-LEAVE");
        CreateTransactionType("EmployeeAbsence", catalogMasterId);

        var groupId = CreateWorkflowGroup(isDefault: true);
        CreateGroupDetail(groupId, sickMasterId, "EmployeeAbsence", requestSubTypeId: 42);

        var employeeId = CreateEmployee("RMR700");

        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                RequestType = "EmployeeAbsence",
                RequestSubTypeId = 42,
                RequestStatus = RequestStatus.Draft
            }).Entity;
        });

        var resolved = await _resolver.ResolveAsync(request);

        resolved.Id.ShouldBe(sickMasterId);
    }
}
