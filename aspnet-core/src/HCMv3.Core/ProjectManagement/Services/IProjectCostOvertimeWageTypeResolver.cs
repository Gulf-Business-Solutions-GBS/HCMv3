using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ProjectManagement.Services;

/// <summary>
/// Legacy <c>uscProjectCosting.RunProjectCalculation</c> (lines 727-729 and 809-818) needs the mapping
/// <c>OvertimeType -&gt; WageType</c> twice:
/// <list type="number">
/// <item><description>to build the set of wage-type ids that are OVERTIME wage types, so that such a wage
/// type is costed as <c>ApprovedOvertimeHours * OvertimeType.OvertimeRate * SalaryRegisterDetail.Rate</c>
/// instead of being pro-rated by working days;</description></item>
/// <item><description>to find the <c>OvertimeType</c> (and its <c>OvertimeRate</c>) for a given wage
/// type.</description></item>
/// </list>
///
/// <para><b>GAP CLOSED (wave P8).</b> <see cref="HCMv3.GeneralMasterInfo.OvertimeType"/> now carries the
/// legacy <c>WageTypeId</c> FK again, and <see cref="ProjectCostOvertimeWageTypeResolver"/> implements this
/// interface against it. The costing branch at legacy lines 809-818 is live. The interface is retained
/// (rather than inlining the queries) so <see cref="ProjectCostEngineService"/> stays testable without a
/// GeneralMasterInfo repository.</para>
/// </summary>
public interface IProjectCostOvertimeWageTypeResolver : IDomainService
{
    /// <summary>
    /// <c>true</c> for the real implementation. Kept because <see cref="ProjectCostEngineService"/> still
    /// warns once per run if some future test double returns <c>false</c>.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>Legacy line 727-729: ids of every WageType referenced by an OvertimeType.</summary>
    Task<List<int>> GetOvertimeWageTypeIdsAsync();

    /// <summary>
    /// Legacy line 812: the OvertimeType id + rate for the OvertimeType whose WageType is
    /// <paramref name="wageTypeId"/>, or <c>null</c> when there is none.
    /// </summary>
    Task<ProjectCostOvertimeTypeInfo> GetOvertimeTypeForWageTypeAsync(int wageTypeId);
}

/// <summary>Legacy's anonymous <c>new { r.Oid, r.OvertimeRate }</c> (line 812).</summary>
public class ProjectCostOvertimeTypeInfo
{
    public int OvertimeTypeId { get; set; }

    public double OvertimeRate { get; set; }
}

/// <summary>
/// The real resolver (wave P8), reading <c>OvertimeType.WageTypeId</c>.
///
/// <para><c>HCMv3.Core</c> has no EF Core reference, so both queries are plain synchronous LINQ over
/// <c>IRepository&lt;T&gt;.GetAll()</c>; the async signatures are kept for interface consistency, exactly
/// like <c>AccrualService</c> and <c>PayrollRegisterStore</c>.</para>
/// </summary>
public class ProjectCostOvertimeWageTypeResolver : IProjectCostOvertimeWageTypeResolver, ITransientDependency
{
    private readonly IRepository<OvertimeType> _overtimeTypeRepository;

    public ProjectCostOvertimeWageTypeResolver(IRepository<OvertimeType> overtimeTypeRepository)
    {
        _overtimeTypeRepository = overtimeTypeRepository;
    }

    public bool IsAvailable => true;

    /// <summary>
    /// Legacy lines 727-729:
    /// <c>GetObjectsQuery&lt;OvertimeType&gt;().Where(r =&gt; r.WageType != null).Select(r =&gt; r.WageType.Oid)</c>.
    /// </summary>
    public Task<List<int>> GetOvertimeWageTypeIdsAsync()
    {
        var ids = _overtimeTypeRepository.GetAll()
            .Where(r => r.WageTypeId != null)
            .Select(r => r.WageTypeId.Value)
            .ToList();

        return Task.FromResult(ids);
    }

    /// <summary>
    /// Legacy line 812:
    /// <c>GetObjectsQuery&lt;OvertimeType&gt;().Where(r =&gt; r.WageType == projWageType.WageType)
    /// .Select(r =&gt; new { r.Oid, r.OvertimeRate }).FirstOrDefault()</c>. Legacy takes the FIRST match when
    /// several OvertimeTypes share a wage type -- preserved, NOT "improved" into a sum or an error.
    /// </summary>
    public Task<ProjectCostOvertimeTypeInfo> GetOvertimeTypeForWageTypeAsync(int wageTypeId)
    {
        var info = _overtimeTypeRepository.GetAll()
            .Where(r => r.WageTypeId == wageTypeId)
            .Select(r => new ProjectCostOvertimeTypeInfo
            {
                OvertimeTypeId = r.Id,
                OvertimeRate = r.OvertimeRate
            })
            .FirstOrDefault();

        return Task.FromResult(info);
    }
}
