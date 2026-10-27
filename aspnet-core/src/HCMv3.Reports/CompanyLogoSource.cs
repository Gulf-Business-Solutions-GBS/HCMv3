namespace HCMv3.Reports
{
    /// <summary>
    /// Which unit's logo a report's company-logo picture box shows -- the three ways the legacy reports chose it.
    /// The web host maps each to <c>HCMv3.OrganizationManagement.Services.ICompanyLogoResolver</c>; this project
    /// references only DevExpress, so it cannot call the resolver itself.
    /// </summary>
    public enum CompanyLogoSource
    {
        /// <summary>
        /// The company of the employee on the current row: legacy
        /// <c>CASE WHEN Company_1.OID IS NOT NULL THEN Company_1.logo ... END</c>, i.e. the top-most unit of type
        /// 'Company' on the employee's org assignment path. The row carries the employee's org unit id.
        /// </summary>
        EmployeeCompany = 0,

        /// <summary>
        /// One logo for the whole report, from the first unit of type 'Company'. Legacy rptPayslipAr
        /// (<c>WHERE OrganizationUnitType IN (SELECT OID FROM OrganizationUnitType WHERE NameEn = 'Company')</c>)
        /// and rptSubordPayslipEn/Ar (<c>WHERE OrgUnitType = 1</c>, the legacy enum value for Company).
        /// </summary>
        FirstCompanyUnit = 1,

        /// <summary>
        /// One logo for the whole report, from the first unit whose type code is level 1. Legacy rptPayrollSheet
        /// (<c>WHERE OrganizationUnitType.Code LIKE '%OrgLevel1%'</c>; legacy OrgLevelN is HCMv3 LevelN).
        /// </summary>
        FirstLevel1Unit = 2
    }

    /// <summary>One company-logo picture box as wired by <see cref="HcmReportBase"/>: what it shows and from where.</summary>
    public sealed class CompanyLogoBinding
    {
        public CompanyLogoBinding(string pictureBoxName, CompanyLogoSource source, string orgUnitIdColumn)
        {
            PictureBoxName = pictureBoxName;
            Source = source;
            OrgUnitIdColumn = orgUnitIdColumn;
        }

        public string PictureBoxName { get; }

        public CompanyLogoSource Source { get; }

        /// <summary>
        /// For <see cref="CompanyLogoSource.EmployeeCompany"/>: the column of the report's own query holding the
        /// employee's org unit id. Null for the report-wide sources.
        /// </summary>
        public string OrgUnitIdColumn { get; }
    }
}
