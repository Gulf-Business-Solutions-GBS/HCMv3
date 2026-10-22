using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.Sql;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports
{
    /// <summary>
    /// HCMv3 port of the legacy XAF base report <c>HCM10.Module.Reports.MyXtraReport</c>.
    ///
    /// WHAT WAS DROPPED AND WHY (the legacy class is 962 lines; almost all of it was XAF/web glue):
    ///   * <c>SecuritySystem.CurrentUserName</c> in the constructor -- the logged-in user is now
    ///     pushed in as the <see cref="LoggedUserNameParameter"/> report parameter by the caller,
    ///     which is the layer that actually has an ABP session.
    ///   * <c>ChangeCaption()</c> (~300 lines) -- switched Arabic parameter captions per report type
    ///     via <c>WebApplication.Instance.CurrentAspectProvider</c> and hard-referenced dozens of
    ///     report classes that are not in scope. Localisation belongs in ABP localisation, not here.
    ///   * <c>SetBeforePrint()</c> / <c>SetBeforeLoad()</c> / <c>MyXtraReport_DataSourceDemanded</c> --
    ///     opened an <c>XPObjectSpaceProvider</c> against <c>HCM10ConnectionString</c> to stamp
    ///     report headers and apply row-level security to the employee-code parameter. RLS is an
    ///     authorization decision and now belongs in the app/Web.Host layer (ABP permissions +
    ///     <c>AbpSession</c>), not baked into a report.
    ///   * <c>AmountInArabicWords</c> / <c>ProcessArabicGroup</c> / <c>GetDigitFeminineStatus</c> --
    ///     NOT ported. They depend on legacy <c>CurrencyInfo</c> and the arabicOnes/arabicTens/...
    ///     lookup tables which live outside MyXtraReport.cs, and nothing in the English payslip
    ///     calls them. Port them with the first Arabic report that needs them.
    ///
    /// WHAT SURVIVED: the three self-contained formatting helpers, plus new plumbing for the two
    /// things every HCMv3 report needs and no legacy report had -- an explicit tenant filter and an
    /// externally supplied connection string.
    /// </summary>
    public class HcmReportBase : XtraReport
    {
        /// <summary>Name of the tenant report parameter every HCMv3 report must carry.</summary>
        public const string TenantParameterName = "PrmTenantId";

        /// <summary>Name of the (optional) logged-in-user report parameter.</summary>
        public const string LoggedUserNameParameter = "PrmLoggedUserName";

        /// <summary>
        /// Design-time connection name. Resolved at design time from the DevExpress data-connection
        /// store; at run time it is always overridden by <see cref="ApplyConnectionString"/>.
        /// </summary>
        /// <summary>
        /// Connection NAME carried by every HCMv3 report layout. Deliberately a name and not a
        /// connection string: the Web Report Designer serialises the layout and ships it to the
        /// browser, so a layout carrying CustomStringConnectionParameters would hand the database
        /// connection string to the client.
        ///
        /// Inside Web.Host the name is resolved server-side by
        /// <c>HCMv3.Web.Reporting.HcmReportConnectionProviderService</c>
        /// (a DevExpress <c>IConnectionProviderService</c>). Out-of-process callers -- tests,
        /// background jobs, the export harness -- have no such service and must instead call
        /// <see cref="ApplyConnectionString"/> before creating the document.
        /// </summary>
        public const string DesignTimeConnectionName = "HCMv3ConnectionString";

        /// <summary>
        /// Points every <see cref="SqlDataSource"/> in this report -- and, recursively, in every
        /// nested <see cref="XRSubreport"/> -- at the supplied ADO.NET connection string.
        /// </summary>
        /// <param name="adoConnectionString">A plain SQL Server ADO.NET connection string.</param>
        public void ApplyConnectionString(string adoConnectionString)
        {
            if (string.IsNullOrWhiteSpace(adoConnectionString))
            {
                throw new ArgumentException("A connection string is required.", nameof(adoConnectionString));
            }

            var parameters = ToDevExpressConnectionParameters(adoConnectionString);

            foreach (var report in SelfAndDescendants())
            {
                foreach (var dataSource in report.SqlDataSources())
                {
                    dataSource.ConnectionParameters = parameters;
                }
            }
        }

        /// <summary>
        /// Sets <see cref="TenantParameterName"/> on this report and every nested subreport.
        ///
        /// THIS IS NOT OPTIONAL. The queries run as raw SQL through <see cref="SqlDataSource"/>, so
        /// EF Core's per-tenant global query filter never sees them. Skipping this leaves the
        /// parameter at its design-time default and silently returns another tenant's payroll.
        /// </summary>
        public void ApplyTenant(int? tenantId)
        {
            // Host-tenant (null) reports would have to span tenants, which no payroll report should.
            var value = tenantId ?? throw new InvalidOperationException(
                "HCMv3 reports are tenant-scoped; they cannot be run from the host tenant.");

            SetParameterEverywhere(TenantParameterName, value);
        }

        /// <summary>
        /// Reads a stored image (employee photo and the like) from the file store: tenant id and relative
        /// path in, bytes out, null when missing. Set once at start-up by the web host, because this project
        /// references nothing but DevExpress and cannot reach the file store itself.
        /// </summary>
        public static Func<int, string, byte[]> StoredImageLoader { get; set; }

        /// <summary>
        /// Shows, in <paramref name="pictureBox"/>, the image whose relative path is in the current row's
        /// <paramref name="pathColumn"/>. Images are kept on disk, not in the database, so the report SQL can
        /// only return where the picture is.
        /// </summary>
        protected void BindStoredImage(XRPictureBox pictureBox, string pathColumn)
        {
            pictureBox.ExpressionBindings.Clear();
            pictureBox.BeforePrint += (_, _) =>
            {
                byte[] bytes = null;

                // A report whose SQL does not return the path column must print without a picture, not fail.
                object pathValue;
                try
                {
                    pathValue = GetCurrentColumnValue(pathColumn);
                }
                catch (Exception)
                {
                    pathValue = null;
                }

                if (pathValue is string path && path.Length > 0
                    && StoredImageLoader != null
                    && int.TryParse(Convert.ToString(Parameters[TenantParameterName]?.Value), out var tenantId))
                {
                    bytes = StoredImageLoader(tenantId, path);
                }

                pictureBox.Image = bytes == null
                    ? null
                    : System.Drawing.Image.FromStream(new System.IO.MemoryStream(bytes), false, false);
            };
        }

        /// <summary>
        /// Loads a company logo from the tenant file store: tenant id, which logo, and (for
        /// <see cref="CompanyLogoSource.EmployeeCompany"/>) the employee's org unit id in; image bytes out, null
        /// when there is none. Set once at start-up by the web host, which maps it to
        /// <c>ICompanyLogoResolver</c>; this project cannot reach the database or the file store itself.
        /// </summary>
        public static Func<int, CompanyLogoSource, long?, byte[]> CompanyLogoLoader { get; set; }

        /// <summary>
        /// Suffix of the column that carries the employee's org unit id for a logo: a picture box bound to
        /// <c>[Companylogo]</c> shows the company logo of the unit in column <c>CompanylogoOrgUnitId</c>.
        /// </summary>
        public const string LogoOrgUnitIdSuffix = "OrgUnitId";

        /// <summary>
        /// Matches the legacy logo bindings: <c>[Companylogo]</c>, <c>[CompanyLogo]</c>, <c>[logo]</c>, and the
        /// member-qualified <c>[Query].[logo]</c> form.
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex LogoBindingPattern = new(
            @"^\[(?:(?<member>[^\]]+)\]\.\[)?(?<column>[^\]]*logo)\]$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        private readonly List<CompanyLogoBinding> _companyLogoBindings = new();

        private readonly Dictionary<string, byte[]> _companyLogoCache = new(StringComparer.Ordinal);

        /// <summary>The company-logo picture boxes this report rewired, for checks and diagnostics.</summary>
        public IReadOnlyList<CompanyLogoBinding> CompanyLogoBindings => _companyLogoBindings;

        /// <summary>
        /// Where a picture box bound to ANOTHER query of the report (<c>[Query].[logo]</c>,
        /// <c>QueryLogo.logo</c>) takes its one report-wide logo from. Legacy rptPayslipAr and the two
        /// subordinate payslips used the first 'Company' unit; rptPayrollSheet overrides this.
        /// </summary>
        protected virtual CompanyLogoSource StandaloneLogoSource => CompanyLogoSource.FirstCompanyUnit;

        /// <summary>
        /// Legacy printed the company logo from <c>OrganizationUnit.logo</c>, an image column. HCMv3 keeps
        /// the logo on disk (<c>OrgUnitProfile.LogoPath</c>) and never in the database, so this rewires every
        /// picture box whose image is bound to a legacy logo column (see <see cref="LogoBindingPattern"/>) to
        /// load it through <see cref="CompanyLogoLoader"/> instead:
        /// <list type="bullet">
        ///   <item>bound to the report's own query (<c>[Companylogo]</c>): the company of the employee on the
        ///   current row, whose org unit id the SQL returns as <c>{column}</c>+<see cref="LogoOrgUnitIdSuffix"/>
        ///   (e.g. <c>CompanylogoOrgUnitId</c>) -- <see cref="CompanyLogoSource.EmployeeCompany"/>;</item>
        ///   <item>bound to another query (<c>[PayslipLogoQuery].[logo]</c>, or the old-style data binding
        ///   <c>QueryLogo.logo</c>): one logo for the report, <see cref="StandaloneLogoSource"/>.</item>
        /// </list>
        /// Called from <see cref="EnsureHcmParameters"/>, so every report gets it with no edit of its own.
        /// A missing column, unit, logo or loader leaves the box empty; it never fails the report.
        /// Idempotent: a rewired box has no logo binding left to match.
        /// </summary>
        protected void BindStoredLogos()
        {
            foreach (var pictureBox in AllControls(this).OfType<XRPictureBox>().ToList())
            {
                var expressionBinding = pictureBox.ExpressionBindings
                    .Cast<ExpressionBinding>()
                    .FirstOrDefault(b => IsImageProperty(b.PropertyName)
                                         && !string.IsNullOrWhiteSpace(b.Expression));
                if (expressionBinding != null)
                {
                    var match = LogoBindingPattern.Match(expressionBinding.Expression.Trim());
                    if (!match.Success)
                    {
                        continue;
                    }

                    var member = match.Groups["member"];
                    if (member.Success && !string.Equals(member.Value, DataMember, StringComparison.OrdinalIgnoreCase))
                    {
                        BindCompanyLogo(pictureBox, StandaloneLogoSource, null);
                    }
                    else
                    {
                        BindCompanyLogo(pictureBox, CompanyLogoSource.EmployeeCompany,
                            match.Groups["column"].Value + LogoOrgUnitIdSuffix);
                    }

                    continue;
                }

                // Old-style XRBinding("Image", null, "QueryLogo.logo") kept from legacy on the subordinate payslips.
                var dataBinding = pictureBox.DataBindings
                    .Cast<XRBinding>()
                    .FirstOrDefault(b => IsImageProperty(b.PropertyName)
                                         && b.DataMember != null
                                         && b.DataMember.EndsWith("logo", StringComparison.OrdinalIgnoreCase));
                if (dataBinding == null)
                {
                    continue;
                }

                var lastDot = dataBinding.DataMember.LastIndexOf('.');
                var bindingMember = lastDot > 0 ? dataBinding.DataMember.Substring(0, lastDot) : null;
                pictureBox.DataBindings.Remove(dataBinding);

                if (bindingMember != null && !string.Equals(bindingMember, DataMember, StringComparison.OrdinalIgnoreCase))
                {
                    BindCompanyLogo(pictureBox, StandaloneLogoSource, null);
                }
                else
                {
                    BindCompanyLogo(pictureBox, CompanyLogoSource.EmployeeCompany,
                        dataBinding.DataMember.Substring(lastDot + 1) + LogoOrgUnitIdSuffix);
                }
            }
        }

        /// <summary>
        /// A picture box's image binding is named <c>Image</c> in the designer files, but DevExpress renames it to
        /// <c>ImageSource</c> when it upgrades a layout saved by an older version (the EmployeeInfo reports still
        /// carry <c>Version = "17.2"</c>). Both mean the picture.
        /// </summary>
        private static bool IsImageProperty(string propertyName) =>
            string.Equals(propertyName, "Image", StringComparison.Ordinal)
            || string.Equals(propertyName, "ImageSource", StringComparison.Ordinal);

        private void BindCompanyLogo(XRPictureBox pictureBox, CompanyLogoSource source, string orgUnitIdColumn)
        {
            pictureBox.ExpressionBindings.Clear();
            _companyLogoBindings.Add(new CompanyLogoBinding(pictureBox.Name, source, orgUnitIdColumn));
            pictureBox.BeforePrint += (_, _) => pictureBox.Image = LoadCompanyLogoImage(source, orgUnitIdColumn);
        }

        /// <summary>
        /// The logo for the current row (or the report), or null. Every failure -- no loader, no tenant, the
        /// SQL not returning the column, no unit, no logo file, an undecodable file -- yields null, so the box
        /// prints empty instead of failing the whole report. One load per distinct unit per report run.
        /// </summary>
        protected System.Drawing.Image LoadCompanyLogoImage(CompanyLogoSource source, string orgUnitIdColumn)
        {
            var loader = CompanyLogoLoader;
            if (loader == null
                || !int.TryParse(Convert.ToString(Parameters[TenantParameterName]?.Value), out var tenantId)
                || tenantId <= 0)
            {
                return null;
            }

            long? orgUnitId = null;
            if (source == CompanyLogoSource.EmployeeCompany)
            {
                object value;
                try
                {
                    value = GetCurrentColumnValue(orgUnitIdColumn);
                }
                catch (Exception)
                {
                    value = null;
                }

                if (value == null || value is DBNull
                    || !long.TryParse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture), out var parsed))
                {
                    return null;
                }

                orgUnitId = parsed;
            }

            var key = tenantId + "|" + (int)source + "|" + orgUnitId;
            if (!_companyLogoCache.TryGetValue(key, out var bytes))
            {
                try
                {
                    bytes = loader(tenantId, source, orgUnitId);
                }
                catch (Exception)
                {
                    bytes = null;
                }

                _companyLogoCache[key] = bytes;
            }

            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            try
            {
                return System.Drawing.Image.FromStream(new System.IO.MemoryStream(bytes), false, false);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Sets the logged-in user name shown in the page footer, where the report has one.</summary>
        public void ApplyLoggedUserName(string userName)
        {
            SetParameterEverywhere(LoggedUserNameParameter, userName ?? string.Empty);
        }

        /// <summary>
        /// Adds the HCMv3-only plumbing that has no legacy counterpart: a hidden
        /// <see cref="TenantParameterName"/> report parameter, an optional
        /// <see cref="LoggedUserNameParameter"/>, and -- for every query whose SQL mentions
        /// <c>@TenantId</c> -- the matching <see cref="QueryParameter"/> bound to it.
        ///
        /// Called from each report's constructor AFTER InitializeComponent, deliberately rather
        /// than from the designer file: the Visual Studio report designer re-serialises
        /// InitializeComponent wholesale, and this plumbing must survive that.
        ///
        /// The tenant parameter defaults to 0, which matches no tenant. That is intentional: a
        /// caller that forgets <see cref="ApplyTenant"/> gets an empty report rather than another
        /// tenant's payroll.
        /// </summary>
        protected void EnsureHcmParameters(bool withLoggedUserName = false)
        {
            if (Parameters[TenantParameterName] == null)
            {
                Parameters.Add(new DevExpress.XtraReports.Parameters.Parameter
                {
                    Name = TenantParameterName,
                    Description = "Tenant",
                    Type = typeof(int),
                    Value = 0,
                    Visible = false
                });
            }

            if (withLoggedUserName && Parameters[LoggedUserNameParameter] == null)
            {
                Parameters.Add(new DevExpress.XtraReports.Parameters.Parameter
                {
                    Name = LoggedUserNameParameter,
                    Description = "Logged-in user",
                    Type = typeof(string),
                    Value = string.Empty,
                    Visible = false
                });
            }

            foreach (var dataSource in this.SqlDataSources())
            {
                foreach (SqlQuery query in dataSource.Queries)
                {
                    if (!(query is CustomSqlQuery customQuery))
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(customQuery.Sql) ||
                        customQuery.Sql.IndexOf("@TenantId", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        continue;
                    }

                    if (customQuery.Parameters.Any(p => string.Equals(p.Name, "TenantId", StringComparison.OrdinalIgnoreCase)))
                    {
                        continue;
                    }

                    customQuery.Parameters.Add(new QueryParameter
                    {
                        Name = "TenantId",
                        Type = typeof(DevExpress.DataAccess.Expression),
                        Value = new DevExpress.DataAccess.Expression("[Parameters." + TenantParameterName + "]", typeof(int))
                    });
                }
            }

            DataSourceDemanded -= ClampDateParametersOnDemand;
            DataSourceDemanded += ClampDateParametersOnDemand;

            BindStoredLogos();
        }

        private static CustomStringConnectionParameters ToDevExpressConnectionParameters(string adoConnectionString)
        {
            // DevExpress SqlDataSource connection strings are XPO-flavoured: the provider is named
            // inline rather than taken from a DbProviderFactory registration.
            var dxConnectionString = adoConnectionString.IndexOf("XpoProvider=", StringComparison.OrdinalIgnoreCase) >= 0
                ? adoConnectionString
                : "XpoProvider=MSSqlServer;" + adoConnectionString;

            return new CustomStringConnectionParameters(dxConnectionString);
        }

        private void ClampDateParametersOnDemand(object sender, EventArgs e)
        {
            ClampDateParameters();
        }

        /// <summary>
        /// Raises any DateTime report parameter still sitting at <see cref="DateTime.MinValue"/> to
        /// SQL Server's minimum datetime (1753-01-01).
        ///
        /// WHY: the payslip subreports take their ForPeriod/InPeriod from a ParameterBinding onto a
        /// master-query column. When the master query returns no rows -- an employee code that does
        /// not exist, a period with no payroll run, or a tenant mismatch -- the binding supplies
        /// nothing and the parameter falls back to DateTime.MinValue, which SqlClient rejects with
        /// "SqlDateTime overflow" instead of simply returning no rows. The legacy report had the
        /// same latent fault; it just was not reachable from a web viewer where the user types the
        /// employee code. Clamping turns a hard 500 into an empty report.
        /// </summary>
        protected void ClampDateParameters()
        {
            var sqlMinimum = System.Data.SqlTypes.SqlDateTime.MinValue.Value;

            foreach (DevExpress.XtraReports.Parameters.Parameter parameter in Parameters)
            {
                if (parameter.Value is DateTime value && value < sqlMinimum)
                {
                    parameter.Value = sqlMinimum;
                }
            }
        }

        /// <summary>Sets a parameter on this report and on every nested subreport that declares it.</summary>
        protected void SetParameterEverywhere(string parameterName, object value)
        {
            foreach (var report in SelfAndDescendants())
            {
                var parameter = report.Parameters[parameterName];
                if (parameter != null)
                {
                    parameter.Value = value;
                }
            }
        }

        /// <summary>This report plus every report reachable through nested <see cref="XRSubreport"/> controls.</summary>
        protected IEnumerable<XtraReport> SelfAndDescendants()
        {
            var pending = new Stack<XtraReport>();
            var seen = new HashSet<XtraReport>();
            pending.Push(this);

            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (current == null || !seen.Add(current))
                {
                    continue;
                }

                yield return current;

                foreach (var subreport in AllControls(current).OfType<XRSubreport>())
                {
                    if (subreport.ReportSource != null)
                    {
                        pending.Push(subreport.ReportSource);
                    }
                }
            }
        }

        private static IEnumerable<XRControl> AllControls(XtraReport report)
        {
            foreach (Band band in report.Bands)
            {
                foreach (var control in Descend(band))
                {
                    yield return control;
                }
            }
        }

        /// <summary>
        /// Legacy <c>MyXtraReport.SetBeforePrint</c>, "Set Decimal Rounding": every cell and label whose
        /// format string ends in three decimals (<c>.000}</c>) is re-formatted to
        /// <c>ManageReportSettings.DecimalInReports</c> decimals. Zero means the legacy default of three, and
        /// three leaves the layout untouched, exactly as legacy did. Only the report's own controls are
        /// touched, not its subreports, which is also what legacy's <c>AllControls&lt;T&gt;()</c> covered.
        /// </summary>
        public void ApplyDecimalInReports(int decimalInReports)
        {
            var decimals = decimalInReports <= 0 ? 3 : decimalInReports;
            if (decimals == 3)
            {
                return;
            }

            var replacement = "." + new string('0', decimals) + "}";

            foreach (var control in AllControls(this))
            {
                switch (control)
                {
                    case XRTableCell cell when cell.TextFormatString != null && cell.TextFormatString.Contains(".000}"):
                        cell.TextFormatString = cell.TextFormatString.Replace(".000}", replacement);
                        break;
                    case XRLabel label when label.TextFormatString != null && label.TextFormatString.Contains(".000}"):
                        label.TextFormatString = label.TextFormatString.Replace(".000}", replacement);
                        break;
                }
            }
        }

        private static IEnumerable<XRControl> Descend(XRControl root)
        {
            foreach (XRControl child in root.Controls)
            {
                yield return child;
                foreach (var grandChild in Descend(child))
                {
                    yield return grandChild;
                }
            }
        }

        #region Formatting helpers ported verbatim from MyXtraReport

        /// <summary>e.g. (6, 2024) -> "JUNE-2024". Ported verbatim from MyXtraReport.GetMonthNameYear.</summary>
        protected string GetMonthNameYear(object month, object year)
        {
            var monthNumber = Convert.ToInt16(month);
            var monthName = monthNumber >= 1 && monthNumber <= 12
                ? System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(monthNumber)
                : string.Empty;

            return (monthName + "-" + year).ToUpperInvariant();
        }

        /// <summary>
        /// Indian-numbering amount-in-words (Lakh/Crore). Ported verbatim from
        /// MyXtraReport.AmountInWords, quirks included: it truncates the fractional part and
        /// returns "" for zero and for anything at or above 1,000,000,000.
        /// </summary>
        protected string AmountInWords(double amount)
        {
            var n = (int)amount;

            if (n <= 0)
            {
                return "";
            }

            if (n <= 19)
            {
                var ones = new[]
                {
                    "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
                    "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen",
                    "Eighteen", "Nineteen"
                };
                return ones[n - 1] + " ";
            }

            if (n <= 99)
            {
                var tens = new[] { "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };
                return tens[n / 10 - 2] + " " + AmountInWords(n % 10);
            }

            if (n <= 199) return "One Hundred " + AmountInWords(n % 100);
            if (n <= 999) return AmountInWords(n / 100) + "Hundred " + AmountInWords(n % 100);
            if (n <= 1999) return "One Thousand " + AmountInWords(n % 1000);
            if (n <= 99999) return AmountInWords(n / 1000) + "Thousand " + AmountInWords(n % 1000);
            if (n <= 199999) return "One Lakh " + AmountInWords(n % 100000);
            if (n <= 9999999) return AmountInWords(n / 100000) + " Lakh " + AmountInWords(n % 100000);
            if (n <= 19999999) return "One Crore " + AmountInWords(n % 10000000);
            if (n <= 999999999) return AmountInWords(n / 10000000) + "Crore" + AmountInWords(n % 10000000);

            return "";
        }

        /// <summary>
        /// Inclusive service span as "y Years m Months d Days".
        ///
        /// The legacy version delegated to <c>HCM10.Module.BusinessObjects.Services.DateTimeService</c>,
        /// which is not being ported; the calendar-aware borrow arithmetic is inlined here and
        /// keeps the legacy end-date-inclusive semantics (<c>EndDate.AddDays(1)</c>).
        /// </summary>
        protected string GetYearMonthDay(DateTime startDate, DateTime endDate)
        {
            var from = startDate.Date;
            var to = endDate.Date.AddDays(1);

            if (to < from)
            {
                return "0 Years 0 Months 0 Days";
            }

            var years = to.Year - from.Year;
            var months = to.Month - from.Month;
            var days = to.Day - from.Day;

            if (days < 0)
            {
                months--;
                var previousMonth = to.AddMonths(-1);
                days += DateTime.DaysInMonth(previousMonth.Year, previousMonth.Month);
            }

            if (months < 0)
            {
                years--;
                months += 12;
            }

            return years + " Years " + months + " Months " + days + " Days";
        }

        #endregion
    }

    internal static class HcmReportExtensions
    {
        /// <summary>Every <see cref="SqlDataSource"/> held by a report's component/object storage.</summary>
        internal static IEnumerable<SqlDataSource> SqlDataSources(this XtraReport report)
        {
            foreach (var component in report.ComponentStorage)
            {
                if (component is SqlDataSource sqlDataSource)
                {
                    yield return sqlDataSource;
                }
            }

            if (report.DataSource is SqlDataSource reportDataSource)
            {
                yield return reportDataSource;
            }
        }
    }
}
