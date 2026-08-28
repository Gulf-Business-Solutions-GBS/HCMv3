using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers the <c>ApprovedOnly</c> data filter -- the mechanism that lets a pending leave live in
/// <c>EmployeeAbsences</c> itself instead of a shadow <c>LeaveRequest</c> table.
///
/// These tests are the safety net for the whole single-table workflow design. If the filter stops working,
/// the symptom is not a failing screen: it is un-approved leave silently counting towards quota and being
/// paid by payroll. Nothing else in the system would notice.
///
/// Seeding goes through <c>UsingDbContext</c> deliberately: that writes outside an ABP unit of work, so no
/// filter applies and rows in any status can be planted. Reads go through <c>IRepository&lt;T&gt;</c>, which
/// is how the rest of the application queries and therefore where the filter must bite.
/// </summary>
// ReSharper disable once InconsistentNaming
public class ApprovedOnlyFilter_Tests : AppTestBase
{
    private readonly IRepository<EmployeeAbsence> _absenceRepository;
    private readonly IRepository<EmployeeAsset> _assetRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ApprovedOnlyFilter_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _absenceRepository = Resolve<IRepository<EmployeeAbsence>>();
        _assetRepository = Resolve<IRepository<EmployeeAsset>>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    private int CreateEmployee(string code)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
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
                TenantId = AbpSession.TenantId ?? 1,
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

    private int CreateAbsenceType()
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Annual",
                NameAr = "سنوي"
            }).Entity;
        });

        return absenceType.Id;
    }

    /// <summary>Plants an absence directly, bypassing the filter, so any workflow status can be seeded.</summary>
    private int CreateAbsence(int employeeId, int absenceTypeId, RequestStatus? workflowStatus, bool isDeleted = false)
    {
        EmployeeAbsence absence = null;
        UsingDbContext(context =>
        {
            absence = context.Set<EmployeeAbsence>().Add(new EmployeeAbsence
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                AbsenceTypeId = absenceTypeId,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 1, 5),
                TotalLeaveDays = 5,
                WorkflowStatus = workflowStatus,
                IsDeleted = isDeleted
            }).Entity;
        });

        return absence.Id;
    }

    /// <summary>
    /// Runs a read inside a unit of work, which is how every real query in the application runs -- an ABP
    /// data filter only exists within one. Outside a UoW the repository's context is already disposed, so
    /// there is no "unfiltered repository read" to test against in the first place.
    /// </summary>
    private async Task<TResult> InUnitOfWorkAsync<TResult>(Func<Task<TResult>> action)
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            var result = await action();
            await uow.CompleteAsync();

            return result;
        }
    }

    private Task<bool> IsVisibleAsync(int absenceId)
    {
        return InUnitOfWorkAsync(() => _absenceRepository.GetAll().AnyAsync(a => a.Id == absenceId));
    }

    [Theory]
    [InlineData(null)]                      // not workflow-managed -- every row that exists today
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Closed)]      // approved, then closed out by RequestMaster.AutoClose
    public async Task Approved_Rows_Should_Be_Visible(RequestStatus? status)
    {
        var employeeId = CreateEmployee("APR" + (status?.ToString() ?? "Null"));
        var absenceId = CreateAbsence(employeeId, CreateAbsenceType(), status);

        (await IsVisibleAsync(absenceId)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(RequestStatus.Draft)]       // saved but never submitted
    [InlineData(RequestStatus.Open)]        // in the approval chain right now
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Transferred)]
    public async Task Unapproved_Rows_Should_Be_Hidden(RequestStatus status)
    {
        var employeeId = CreateEmployee("UNA" + status);
        var absenceId = CreateAbsence(employeeId, CreateAbsenceType(), status);

        (await IsVisibleAsync(absenceId)).ShouldBeFalse();
    }

    [Fact]
    public async Task Existing_Rows_With_No_Workflow_Status_Should_Be_Unaffected()
    {
        // The backward-compatibility guarantee: every row in the database today has a null WorkflowStatus,
        // so turning this filter on must change nothing for any of them.
        var employeeId = CreateEmployee("APRCOMPAT");
        var absenceTypeId = CreateAbsenceType();

        var first = CreateAbsence(employeeId, absenceTypeId, null);
        var second = CreateAbsence(employeeId, absenceTypeId, null);

        var visible = await InUnitOfWorkAsync(() => _absenceRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId)
            .Select(a => a.Id)
            .ToListAsync());

        visible.ShouldContain(first);
        visible.ShouldContain(second);
    }

    [Fact]
    public async Task DisableFilter_Should_Reveal_Pending_Rows()
    {
        // How ESS, the approval inbox and the requester's own history see their in-flight requests.
        var employeeId = CreateEmployee("APRDISABLE");
        var absenceTypeId = CreateAbsenceType();

        var pendingId = CreateAbsence(employeeId, absenceTypeId, RequestStatus.Open);
        var approvedId = CreateAbsence(employeeId, absenceTypeId, RequestStatus.Approved);

        using (var uow = _unitOfWorkManager.Begin())
        {
            using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
            {
                var visible = await _absenceRepository.GetAll()
                    .Where(a => a.EmployeeId == employeeId)
                    .Select(a => a.Id)
                    .ToListAsync();

                visible.ShouldContain(pendingId);
                visible.ShouldContain(approvedId);
            }

            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task Filter_Should_Come_Back_On_After_The_Disable_Scope_Ends()
    {
        var employeeId = CreateEmployee("APRRESTORE");
        var pendingId = CreateAbsence(employeeId, CreateAbsenceType(), RequestStatus.Open);

        using (var uow = _unitOfWorkManager.Begin())
        {
            using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
            {
                (await _absenceRepository.GetAll().AnyAsync(a => a.Id == pendingId)).ShouldBeTrue();
            }

            (await _absenceRepository.GetAll().AnyAsync(a => a.Id == pendingId)).ShouldBeFalse();

            await uow.CompleteAsync();
        }
    }

    [Fact]
    public async Task Should_Still_Apply_Soft_Delete_Alongside_The_Workflow_Filter()
    {
        // The two filters are combined, not replaced -- an approved but deleted row stays hidden.
        var employeeId = CreateEmployee("APRSOFT");
        var deletedApprovedId = CreateAbsence(employeeId, CreateAbsenceType(), RequestStatus.Approved, isDeleted: true);

        (await IsVisibleAsync(deletedApprovedId)).ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Apply_To_Every_Workflow_Enabled_Entity_Not_Just_Leave()
    {
        // Proves the filter keys off the IWorkflowEnabledEntity interface rather than being wired per table,
        // which is what makes adding the 7th, 20th and 50th transaction type free.
        var employeeId = CreateEmployee("APRASSET");

        int assetId = 0;
        int pendingAssetId = 0;

        UsingDbContext(context =>
        {
            var asset = context.Set<Asset>().Add(new Asset
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Laptop",
                NameAr = "حاسوب"
            }).Entity;

            context.SaveChanges();

            assetId = asset.Id;
        });

        UsingDbContext(context =>
        {
            pendingAssetId = context.Set<EmployeeAsset>().Add(new EmployeeAsset
            {
                TenantId = AbpSession.TenantId ?? 1,
                EmployeeId = employeeId,
                AssetId = assetId,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                WorkflowStatus = RequestStatus.Open
            }).Entity.Id;
        });

        var visible = await InUnitOfWorkAsync(() =>
            _assetRepository.GetAll().AnyAsync(a => a.Id == pendingAssetId));

        visible.ShouldBeFalse();
    }
}
