using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Services;

namespace HCMv3.ProjectManagement.Services;

/// <summary>
/// ============================ REPORTED GAP -- READ THIS ============================
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
/// <para>This port's <see cref="HCMv3.GeneralMasterInfo.OvertimeType"/> <b>deliberately did not port the
/// legacy <c>WageType</c> FK</b> ("the legacy <c>WageType</c> FK is not ported (Payroll module out of
/// scope)" -- see that entity's XML doc). Without it the mapping cannot be reconstructed from any other
/// ported field.</para>
///
/// <para><b>Consequence, stated plainly:</b> with the default <see cref="NullProjectCostOvertimeWageTypeResolver"/>
/// the overtime set is EMPTY, so a project wage type that legacy would have costed by overtime hours falls
/// through to legacy's Earning/Deduction working-day pro-ration branch instead. For a tenant that maps wage
/// types to overtime types, the costed amount WILL differ from legacy. The engine is left runnable rather
/// than made to throw for every employee, because throwing would make the whole screen unusable for the
/// (majority) tenants with no overtime wage types; <see cref="ProjectCostEngineService"/> logs a warning
/// naming this file once per run.</para>
///
/// <para><b>To close the gap:</b> add the <c>WageTypeId</c> FK back to
/// <see cref="HCMv3.GeneralMasterInfo.OvertimeType"/> (entity + DbSet + migration) and register an
/// implementation of this interface. ABP will inject it into <see cref="ProjectCostEngineService"/>
/// automatically and the legacy branch becomes live with no other change.</para>
/// ==================================================================================
/// </summary>
public interface IProjectCostOvertimeWageTypeResolver : IDomainService
{
    /// <summary><c>false</c> for the null implementation; used only to emit the once-per-run warning.</summary>
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
/// Default no-op resolver. See <see cref="IProjectCostOvertimeWageTypeResolver"/> for why this returns an
/// empty set rather than throwing.
/// </summary>
public class NullProjectCostOvertimeWageTypeResolver : IProjectCostOvertimeWageTypeResolver, ISingletonDependency
{
    public bool IsAvailable => false;

    public Task<List<int>> GetOvertimeWageTypeIdsAsync()
    {
        return Task.FromResult(new List<int>());
    }

    public Task<ProjectCostOvertimeTypeInfo> GetOvertimeTypeForWageTypeAsync(int wageTypeId)
    {
        return Task.FromResult<ProjectCostOvertimeTypeInfo>(null);
    }
}
