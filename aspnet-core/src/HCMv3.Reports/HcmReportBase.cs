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

                if (GetCurrentColumnValue(pathColumn) is string path && path.Length > 0
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
