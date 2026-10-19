namespace HCMv3.PerformanceManagement;

/// <summary>
/// Where Copy KPI takes an appraisal's KPI list from (ported from legacy XAF <c>KPIFrom</c>,
/// <c>HCM10.Module.BusinessObjects.PerformanceManagement.KPICopy</c>; same values).
/// </summary>
public enum KpiFrom
{
    /// <summary>A KPI-by-job-title set.</summary>
    JobTitle = 0,

    /// <summary>A KPI-by-organization-unit set.</summary>
    OrganizationUnit = 1,

    /// <summary>Another employee's appraisal in the same cycle.</summary>
    Employee = 2
}
