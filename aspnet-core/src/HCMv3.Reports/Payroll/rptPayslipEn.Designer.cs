

namespace HCMv3.Reports.Payroll
{
    partial class rptPayslipEn
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            // PORT NOTE: the legacy PayrollArea data member was a designer SelectQuery over the
            // table "PayrollArea" (columns Code, Name). A SelectQuery cannot express the mandatory
            // "TenantId = @TenantId AND IsDeleted = 0" predicate, so it is replaced by a
            // CustomSqlQuery carrying the rewritten SQL. Its data-member name is unchanged, so the
            // PrmPayrollArea look-up binding still resolves.
            DevExpress.DataAccess.Sql.CustomSqlQuery payrollAreaQuery = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery1 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter1 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter2 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter3 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter4 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter5 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter6 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter7 = new DevExpress.DataAccess.Sql.QueryParameter();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(rptPayslipEn));
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings1 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings2 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            DevExpress.XtraReports.UI.XRSummary xrSummary1 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings3 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            this.PayslipDS = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLabel13 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel14 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.srptPaySlipOvertimeHoursEn = new DevExpress.XtraReports.UI.XRSubreport();
            this.xrLabel35 = new DevExpress.XtraReports.UI.XRLabel();
            this.PanelSignature = new DevExpress.XtraReports.UI.XRPanel();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.srptPaySlipBasicPayEn = new DevExpress.XtraReports.UI.XRSubreport();
            this.xrLabel32 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel33 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel23 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel24 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel25 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel26 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel21 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel22 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel17 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel18 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel19 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel15 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel16 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel11 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel12 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel9 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel10 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel8 = new DevExpress.XtraReports.UI.XRLabel();
            this.srptPayslipLeaveEncashment = new DevExpress.XtraReports.UI.XRSubreport();
            this.PrmMonth = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmYear = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmPayrollRunType = new DevExpress.XtraReports.Parameters.Parameter();
            this.srptPayslipLeaveInfo = new DevExpress.XtraReports.UI.XRSubreport();
            this.srptPayslipIndemnityDaysSummaryEn = new DevExpress.XtraReports.UI.XRSubreport();
            this.srptPayslipWorkingDaysSummary = new DevExpress.XtraReports.UI.XRSubreport();
            this.srptPayslipAttendenceSummaryEn = new DevExpress.XtraReports.UI.XRSubreport();
            this.lblHeader = new DevExpress.XtraReports.UI.XRLabel();
            this.srptPayslipEarning = new DevExpress.XtraReports.UI.XRSubreport();
            this.srptPayslipDeduction = new DevExpress.XtraReports.UI.XRSubreport();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel31 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.PrmEmpCode = new DevExpress.XtraReports.Parameters.Parameter();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.LoggedUserNme = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.EngMainDataStyle = new DevExpress.XtraReports.UI.XRControlStyle();
            this.ENGDataStyle = new DevExpress.XtraReports.UI.XRControlStyle();
            this.ReportHeader = new DevExpress.XtraReports.UI.ReportHeaderBand();
            this.CalcDateTime = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcNetPay = new DevExpress.XtraReports.UI.CalculatedField();
            this.PageFooter = new DevExpress.XtraReports.UI.PageFooterBand();
            this.PrmSendMail = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmEncashment = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmPayrollArea = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmEmpStatus = new DevExpress.XtraReports.Parameters.Parameter();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // PayslipDS
            // 
            this.PayslipDS.ConnectionName = HCMv3.Reports.HcmReportBase.DesignTimeConnectionName;
            this.PayslipDS.ConnectionOptions.DbCommandTimeout = 0;
            this.PayslipDS.Name = "PayslipDS";
            payrollAreaQuery.Name = "PayrollArea";
            payrollAreaQuery.Sql = PayslipSql.PayrollArea;
            customSqlQuery1.Name = "PayslipOrgV2Query";
            queryParameter1.Name = "PrmEmpCode";
            queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmEmpCode]", typeof(string));
            queryParameter2.Name = "PrmMonth";
            queryParameter2.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter2.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmMonth]", typeof(int));
            queryParameter3.Name = "PrmYear";
            queryParameter3.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter3.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmYear]", typeof(int));
            queryParameter4.Name = "PrmPayrollRunType";
            queryParameter4.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter4.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmPayrollRunType]", typeof(int));
            queryParameter5.Name = "PrmPayrollArea";
            queryParameter5.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter5.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmPayrollArea]", typeof(string));
            queryParameter6.Name = "PrmEncashment";
            queryParameter6.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter6.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmEncashment]", typeof(bool));
            queryParameter7.Name = "PrmEmployeeStatus";
            queryParameter7.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter7.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmEmpStatus]", typeof(int));
            customSqlQuery1.Parameters.Add(queryParameter1);
            customSqlQuery1.Parameters.Add(queryParameter2);
            customSqlQuery1.Parameters.Add(queryParameter3);
            customSqlQuery1.Parameters.Add(queryParameter4);
            customSqlQuery1.Parameters.Add(queryParameter5);
            customSqlQuery1.Parameters.Add(queryParameter6);
            customSqlQuery1.Parameters.Add(queryParameter7);
            customSqlQuery1.Sql = PayslipSql.PayslipOrgV2Query;
            this.PayslipDS.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            payrollAreaQuery,
            customSqlQuery1});
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel13,
            this.xrLabel14,
            this.xrLabel2,
            this.srptPaySlipOvertimeHoursEn,
            this.xrLabel35,
            this.PanelSignature,
            this.srptPaySlipBasicPayEn,
            this.xrLabel32,
            this.xrLabel33,
            this.xrLabel23,
            this.xrLabel24,
            this.xrLabel25,
            this.xrLabel26,
            this.xrLabel21,
            this.xrLabel22,
            this.xrLabel17,
            this.xrLabel18,
            this.xrLabel19,
            this.xrLabel15,
            this.xrLabel16,
            this.xrLabel11,
            this.xrLabel12,
            this.xrLabel9,
            this.xrLabel10,
            this.xrLabel1,
            this.xrLabel8,
            this.srptPayslipLeaveEncashment,
            this.srptPayslipLeaveInfo,
            this.srptPayslipIndemnityDaysSummaryEn,
            this.srptPayslipWorkingDaysSummary,
            this.srptPayslipAttendenceSummaryEn,
            this.lblHeader,
            this.srptPayslipEarning,
            this.srptPayslipDeduction,
            this.xrLabel4,
            this.xrLabel31,
            this.xrPictureBox1});
            this.Detail.HeightF = 1075.539F;
            this.Detail.KeepTogether = true;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.PageBreak = DevExpress.XtraReports.UI.PageBreak.AfterBandExceptLastEntry;
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabel13
            // 
            this.xrLabel13.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel13.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel13.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel13.LocationFloat = new DevExpress.Utils.PointFloat(6.420425F, 209.6799F);
            this.xrLabel13.Name = "xrLabel13";
            this.xrLabel13.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel13.SizeF = new System.Drawing.SizeF(116.25F, 25.00002F);
            this.xrLabel13.StylePriority.UseBorderColor = false;
            this.xrLabel13.StylePriority.UseBorders = false;
            this.xrLabel13.StylePriority.UseFont = false;
            this.xrLabel13.StylePriority.UsePadding = false;
            this.xrLabel13.StylePriority.UseTextAlignment = false;
            this.xrLabel13.Text = "Salary Profile";
            this.xrLabel13.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel14
            // 
            this.xrLabel14.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel14.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel14.CanGrow = false;
            this.xrLabel14.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Salary Profile]")});
            this.xrLabel14.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel14.LocationFloat = new DevExpress.Utils.PointFloat(122.6704F, 209.6798F);
            this.xrLabel14.Name = "xrLabel14";
            this.xrLabel14.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel14.SizeF = new System.Drawing.SizeF(278.8974F, 25.00002F);
            this.xrLabel14.StylePriority.UseBorderColor = false;
            this.xrLabel14.StylePriority.UseBorders = false;
            this.xrLabel14.StylePriority.UseFont = false;
            this.xrLabel14.StylePriority.UsePadding = false;
            this.xrLabel14.StylePriority.UseTextAlignment = false;
            this.xrLabel14.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel2
            // 
            this.xrLabel2.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(6.420425F, 971.85F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(100F, 23.00006F);
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.Text = "Remarks :";
            // 
            // srptPaySlipOvertimeHoursEn
            // 
            this.srptPaySlipOvertimeHoursEn.LocationFloat = new DevExpress.Utils.PointFloat(6.420423F, 579.851F);
            this.srptPaySlipOvertimeHoursEn.Name = "srptPaySlipOvertimeHoursEn";
            this.srptPaySlipOvertimeHoursEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("empcode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPaySlipOvertimeHoursEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "PayslipOrgV2Query.InPeriod"));
            this.srptPaySlipOvertimeHoursEn.ReportSource = new HCMv3.Reports.Payroll.srptPaySlipOvertimeHoursEn();
            this.srptPaySlipOvertimeHoursEn.SizeF = new System.Drawing.SizeF(399.9991F, 23.66409F);
            this.srptPaySlipOvertimeHoursEn.Visible = false;
            // 
            // xrLabel35
            // 
            this.xrLabel35.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel35.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel35.CanGrow = false;
            this.xrLabel35.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Bank IBAN]")});
            this.xrLabel35.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel35.LocationFloat = new DevExpress.Utils.PointFloat(122.6704F, 284.6802F);
            this.xrLabel35.Name = "xrLabel35";
            this.xrLabel35.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel35.SizeF = new System.Drawing.SizeF(687.3299F, 25.00003F);
            this.xrLabel35.StylePriority.UseBorderColor = false;
            this.xrLabel35.StylePriority.UseBorders = false;
            this.xrLabel35.StylePriority.UseFont = false;
            this.xrLabel35.StylePriority.UsePadding = false;
            this.xrLabel35.StylePriority.UseTextAlignment = false;
            this.xrLabel35.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // PanelSignature
            // 
            this.PanelSignature.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel7,
            this.xrLabel6});
            this.PanelSignature.LocationFloat = new DevExpress.Utils.PointFloat(6.07F, 888.18F);
            this.PanelSignature.Name = "PanelSignature";
            this.PanelSignature.SizeF = new System.Drawing.SizeF(803.9303F, 39.16669F);
            // 
            // xrLabel7
            // 
            this.xrLabel7.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(449.99F, 10F);
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel7.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel7.StylePriority.UseFont = false;
            this.xrLabel7.StylePriority.UseTextAlignment = false;
            this.xrLabel7.Text = "Reviewed By:";
            this.xrLabel7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel6
            // 
            this.xrLabel6.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(0.3504248F, 10F);
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(104.1667F, 23F);
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UseTextAlignment = false;
            this.xrLabel6.Text = "Approved By:";
            this.xrLabel6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // srptPaySlipBasicPayEn
            // 
            this.srptPaySlipBasicPayEn.LocationFloat = new DevExpress.Utils.PointFloat(6.420423F, 603.5151F);
            this.srptPaySlipBasicPayEn.Name = "srptPaySlipBasicPayEn";
            this.srptPaySlipBasicPayEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("empcode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPaySlipBasicPayEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "PayslipOrgV2Query.ForPeriod"));
            this.srptPaySlipBasicPayEn.ReportSource = new HCMv3.Reports.Payroll.srptPaySlipBasicPayEn();
            this.srptPaySlipBasicPayEn.SizeF = new System.Drawing.SizeF(400F, 23.66409F);
            this.srptPaySlipBasicPayEn.Visible = false;
            // 
            // xrLabel32
            // 
            this.xrLabel32.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel32.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel32.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel32.LocationFloat = new DevExpress.Utils.PointFloat(401.5734F, 259.6802F);
            this.xrLabel32.Name = "xrLabel32";
            this.xrLabel32.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel32.SizeF = new System.Drawing.SizeF(147.2503F, 24.99997F);
            this.xrLabel32.StylePriority.UseBorderColor = false;
            this.xrLabel32.StylePriority.UseBorders = false;
            this.xrLabel32.StylePriority.UseFont = false;
            this.xrLabel32.StylePriority.UsePadding = false;
            this.xrLabel32.StylePriority.UseTextAlignment = false;
            this.xrLabel32.Text = "Passport Exp Date";
            this.xrLabel32.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel33
            // 
            this.xrLabel33.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel33.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel33.CanGrow = false;
            this.xrLabel33.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[PassportExpiryDate]")});
            this.xrLabel33.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel33.LocationFloat = new DevExpress.Utils.PointFloat(548.8281F, 259.6802F);
            this.xrLabel33.Name = "xrLabel33";
            this.xrLabel33.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel33.SizeF = new System.Drawing.SizeF(261.1715F, 25F);
            this.xrLabel33.StylePriority.UseBorderColor = false;
            this.xrLabel33.StylePriority.UseBorders = false;
            this.xrLabel33.StylePriority.UseFont = false;
            this.xrLabel33.StylePriority.UsePadding = false;
            this.xrLabel33.StylePriority.UseTextAlignment = false;
            this.xrLabel33.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel33.TextFormatString = "{0:dd-MM-yyyy}";
            // 
            // xrLabel23
            // 
            this.xrLabel23.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel23.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel23.CanGrow = false;
            this.xrLabel23.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Jobtitle]")});
            this.xrLabel23.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel23.LocationFloat = new DevExpress.Utils.PointFloat(548.8231F, 209.6799F);
            this.xrLabel23.Name = "xrLabel23";
            this.xrLabel23.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel23.SizeF = new System.Drawing.SizeF(261.1772F, 25.00002F);
            this.xrLabel23.StylePriority.UseBorderColor = false;
            this.xrLabel23.StylePriority.UseBorders = false;
            this.xrLabel23.StylePriority.UseFont = false;
            this.xrLabel23.StylePriority.UsePadding = false;
            this.xrLabel23.StylePriority.UseTextAlignment = false;
            this.xrLabel23.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel24
            // 
            this.xrLabel24.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel24.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel24.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel24.LocationFloat = new DevExpress.Utils.PointFloat(401.5683F, 209.68F);
            this.xrLabel24.Name = "xrLabel24";
            this.xrLabel24.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel24.SizeF = new System.Drawing.SizeF(147.255F, 25.00002F);
            this.xrLabel24.StylePriority.UseBorderColor = false;
            this.xrLabel24.StylePriority.UseBorders = false;
            this.xrLabel24.StylePriority.UseFont = false;
            this.xrLabel24.StylePriority.UsePadding = false;
            this.xrLabel24.StylePriority.UseTextAlignment = false;
            this.xrLabel24.Text = "Designation";
            this.xrLabel24.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel25
            // 
            this.xrLabel25.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel25.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel25.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel25.LocationFloat = new DevExpress.Utils.PointFloat(401.5681F, 234.68F);
            this.xrLabel25.Name = "xrLabel25";
            this.xrLabel25.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel25.SizeF = new System.Drawing.SizeF(147.255F, 25.00003F);
            this.xrLabel25.StylePriority.UseBorderColor = false;
            this.xrLabel25.StylePriority.UseBorders = false;
            this.xrLabel25.StylePriority.UseFont = false;
            this.xrLabel25.StylePriority.UsePadding = false;
            this.xrLabel25.StylePriority.UseTextAlignment = false;
            this.xrLabel25.Text = "Indemnity Start Date";
            this.xrLabel25.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel26
            // 
            this.xrLabel26.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel26.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel26.CanGrow = false;
            this.xrLabel26.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Joining Date]")});
            this.xrLabel26.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel26.LocationFloat = new DevExpress.Utils.PointFloat(548.8231F, 234.68F);
            this.xrLabel26.Name = "xrLabel26";
            this.xrLabel26.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel26.SizeF = new System.Drawing.SizeF(261.1772F, 25.00003F);
            this.xrLabel26.StylePriority.UseBorderColor = false;
            this.xrLabel26.StylePriority.UseBorders = false;
            this.xrLabel26.StylePriority.UseFont = false;
            this.xrLabel26.StylePriority.UsePadding = false;
            this.xrLabel26.StylePriority.UseTextAlignment = false;
            this.xrLabel26.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel26.TextFormatString = "{0:dd-MM-yyyy}";
            // 
            // xrLabel21
            // 
            this.xrLabel21.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel21.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel21.CanGrow = false;
            this.xrLabel21.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Grade]")});
            this.xrLabel21.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel21.LocationFloat = new DevExpress.Utils.PointFloat(548.8231F, 184.68F);
            this.xrLabel21.Name = "xrLabel21";
            this.xrLabel21.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel21.SizeF = new System.Drawing.SizeF(261.1772F, 25F);
            this.xrLabel21.StylePriority.UseBorderColor = false;
            this.xrLabel21.StylePriority.UseBorders = false;
            this.xrLabel21.StylePriority.UseFont = false;
            this.xrLabel21.StylePriority.UsePadding = false;
            this.xrLabel21.StylePriority.UseTextAlignment = false;
            this.xrLabel21.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel22
            // 
            this.xrLabel22.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel22.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel22.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel22.LocationFloat = new DevExpress.Utils.PointFloat(401.5683F, 184.68F);
            this.xrLabel22.Multiline = true;
            this.xrLabel22.Name = "xrLabel22";
            this.xrLabel22.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel22.SizeF = new System.Drawing.SizeF(147.2549F, 24.99998F);
            this.xrLabel22.StylePriority.UseBorderColor = false;
            this.xrLabel22.StylePriority.UseBorders = false;
            this.xrLabel22.StylePriority.UseFont = false;
            this.xrLabel22.StylePriority.UsePadding = false;
            this.xrLabel22.StylePriority.UseTextAlignment = false;
            this.xrLabel22.Text = "Grade";
            this.xrLabel22.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel17
            // 
            this.xrLabel17.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel17.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel17.CanGrow = false;
            this.xrLabel17.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Bank Name]")});
            this.xrLabel17.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel17.LocationFloat = new DevExpress.Utils.PointFloat(122.6704F, 259.6801F);
            this.xrLabel17.Name = "xrLabel17";
            this.xrLabel17.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel17.SizeF = new System.Drawing.SizeF(278.8979F, 25F);
            this.xrLabel17.StylePriority.UseBorderColor = false;
            this.xrLabel17.StylePriority.UseBorders = false;
            this.xrLabel17.StylePriority.UseFont = false;
            this.xrLabel17.StylePriority.UsePadding = false;
            this.xrLabel17.StylePriority.UseTextAlignment = false;
            this.xrLabel17.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel18
            // 
            this.xrLabel18.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel18.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel18.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel18.LocationFloat = new DevExpress.Utils.PointFloat(6.420423F, 259.6801F);
            this.xrLabel18.Name = "xrLabel18";
            this.xrLabel18.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel18.SizeF = new System.Drawing.SizeF(116.25F, 25F);
            this.xrLabel18.StylePriority.UseBorderColor = false;
            this.xrLabel18.StylePriority.UseBorders = false;
            this.xrLabel18.StylePriority.UseFont = false;
            this.xrLabel18.StylePriority.UsePadding = false;
            this.xrLabel18.StylePriority.UseTextAlignment = false;
            this.xrLabel18.Text = "Bank Name";
            this.xrLabel18.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel19
            // 
            this.xrLabel19.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel19.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel19.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel19.LocationFloat = new DevExpress.Utils.PointFloat(6.420423F, 284.6801F);
            this.xrLabel19.Name = "xrLabel19";
            this.xrLabel19.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel19.SizeF = new System.Drawing.SizeF(116.25F, 24.99997F);
            this.xrLabel19.StylePriority.UseBorderColor = false;
            this.xrLabel19.StylePriority.UseBorders = false;
            this.xrLabel19.StylePriority.UseFont = false;
            this.xrLabel19.StylePriority.UsePadding = false;
            this.xrLabel19.StylePriority.UseTextAlignment = false;
            this.xrLabel19.Text = "Bank Account";
            this.xrLabel19.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel15
            // 
            this.xrLabel15.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel15.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel15.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel15.LocationFloat = new DevExpress.Utils.PointFloat(6.419994F, 234.68F);
            this.xrLabel15.Name = "xrLabel15";
            this.xrLabel15.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel15.SizeF = new System.Drawing.SizeF(116.25F, 25.00003F);
            this.xrLabel15.StylePriority.UseBorderColor = false;
            this.xrLabel15.StylePriority.UseBorders = false;
            this.xrLabel15.StylePriority.UseFont = false;
            this.xrLabel15.StylePriority.UsePadding = false;
            this.xrLabel15.StylePriority.UseTextAlignment = false;
            this.xrLabel15.Text = "Joining Date";
            this.xrLabel15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel16
            // 
            this.xrLabel16.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel16.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel16.CanGrow = false;
            this.xrLabel16.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Joining Date]")});
            this.xrLabel16.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel16.LocationFloat = new DevExpress.Utils.PointFloat(122.6701F, 234.6799F);
            this.xrLabel16.Name = "xrLabel16";
            this.xrLabel16.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel16.SizeF = new System.Drawing.SizeF(278.8977F, 25.00002F);
            this.xrLabel16.StylePriority.UseBorderColor = false;
            this.xrLabel16.StylePriority.UseBorders = false;
            this.xrLabel16.StylePriority.UseFont = false;
            this.xrLabel16.StylePriority.UsePadding = false;
            this.xrLabel16.StylePriority.UseTextAlignment = false;
            this.xrLabel16.Text = "Period";
            this.xrLabel16.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel16.TextFormatString = "{0:dd-MM-yyyy}";
            // 
            // xrLabel11
            // 
            this.xrLabel11.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel11.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel11.CanGrow = false;
            this.xrLabel11.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Employee Name]")});
            this.xrLabel11.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel11.LocationFloat = new DevExpress.Utils.PointFloat(122.67F, 184.6799F);
            this.xrLabel11.Name = "xrLabel11";
            this.xrLabel11.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel11.SizeF = new System.Drawing.SizeF(278.8983F, 25.00002F);
            this.xrLabel11.StylePriority.UseBorderColor = false;
            this.xrLabel11.StylePriority.UseBorders = false;
            this.xrLabel11.StylePriority.UseFont = false;
            this.xrLabel11.StylePriority.UsePadding = false;
            this.xrLabel11.StylePriority.UseTextAlignment = false;
            this.xrLabel11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel12
            // 
            this.xrLabel12.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel12.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel12.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel12.LocationFloat = new DevExpress.Utils.PointFloat(6.42F, 184.68F);
            this.xrLabel12.Name = "xrLabel12";
            this.xrLabel12.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel12.SizeF = new System.Drawing.SizeF(116.25F, 25.00002F);
            this.xrLabel12.StylePriority.UseBorderColor = false;
            this.xrLabel12.StylePriority.UseBorders = false;
            this.xrLabel12.StylePriority.UseFont = false;
            this.xrLabel12.StylePriority.UsePadding = false;
            this.xrLabel12.StylePriority.UseTextAlignment = false;
            this.xrLabel12.Text = "Employee Name";
            this.xrLabel12.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel9
            // 
            this.xrLabel9.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel9.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel9.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel9.LocationFloat = new DevExpress.Utils.PointFloat(401.5683F, 159.68F);
            this.xrLabel9.Name = "xrLabel9";
            this.xrLabel9.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel9.SizeF = new System.Drawing.SizeF(147.255F, 25.00002F);
            this.xrLabel9.StylePriority.UseBorderColor = false;
            this.xrLabel9.StylePriority.UseBorders = false;
            this.xrLabel9.StylePriority.UseFont = false;
            this.xrLabel9.StylePriority.UsePadding = false;
            this.xrLabel9.StylePriority.UseTextAlignment = false;
            this.xrLabel9.Text = "OrgUnit";
            this.xrLabel9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel10
            // 
            this.xrLabel10.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel10.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel10.CanGrow = false;
            this.xrLabel10.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[OrgUnit]")});
            this.xrLabel10.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel10.LocationFloat = new DevExpress.Utils.PointFloat(548.8277F, 159.68F);
            this.xrLabel10.Name = "xrLabel10";
            this.xrLabel10.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel10.SizeF = new System.Drawing.SizeF(261.1726F, 25.00002F);
            this.xrLabel10.StylePriority.UseBorderColor = false;
            this.xrLabel10.StylePriority.UseBorders = false;
            this.xrLabel10.StylePriority.UseFont = false;
            this.xrLabel10.StylePriority.UsePadding = false;
            this.xrLabel10.StylePriority.UseTextAlignment = false;
            this.xrLabel10.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel1
            // 
            this.xrLabel1.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel1.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(6.42F, 159.68F);
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(116.25F, 24.99998F);
            this.xrLabel1.StylePriority.UseBorderColor = false;
            this.xrLabel1.StylePriority.UseBorders = false;
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UsePadding = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.Text = "Employee Code";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel8
            // 
            this.xrLabel8.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel8.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel8.CanGrow = false;
            this.xrLabel8.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Employee Code]")});
            this.xrLabel8.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel8.LocationFloat = new DevExpress.Utils.PointFloat(122.67F, 159.68F);
            this.xrLabel8.Name = "xrLabel8";
            this.xrLabel8.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 2, 0, 0, 100F);
            this.xrLabel8.SizeF = new System.Drawing.SizeF(278.8983F, 24.99998F);
            this.xrLabel8.StylePriority.UseBorderColor = false;
            this.xrLabel8.StylePriority.UseBorders = false;
            this.xrLabel8.StylePriority.UseFont = false;
            this.xrLabel8.StylePriority.UsePadding = false;
            this.xrLabel8.StylePriority.UseTextAlignment = false;
            this.xrLabel8.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // srptPayslipLeaveEncashment
            // 
            this.srptPayslipLeaveEncashment.LocationFloat = new DevExpress.Utils.PointFloat(6.420423F, 557.4648F);
            this.srptPayslipLeaveEncashment.Name = "srptPayslipLeaveEncashment";
            this.srptPayslipLeaveEncashment.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmEmpCode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPayslipLeaveEncashment.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmMonth", this.PrmMonth));
            this.srptPayslipLeaveEncashment.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmYear", this.PrmYear));
            this.srptPayslipLeaveEncashment.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmPayrollRunType", this.PrmPayrollRunType));
            this.srptPayslipLeaveEncashment.ReportSource = new HCMv3.Reports.Payroll.srptPaySlipLeaveEncashmentEn();
            this.srptPayslipLeaveEncashment.SizeF = new System.Drawing.SizeF(399.9991F, 22.38617F);
            this.srptPayslipLeaveEncashment.Visible = false;
            // 
            // PrmMonth
            // 
            this.PrmMonth.Description = "Month";
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(1, "January"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(2, "February"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(3, "March"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(4, "April"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(5, "May"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(6, "June"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(7, "July"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(8, "August"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(9, "September"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(10, "October"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(11, "November"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(12, "December"));
            this.PrmMonth.LookUpSettings = staticListLookUpSettings1;
            this.PrmMonth.Name = "PrmMonth";
            this.PrmMonth.Type = typeof(int);
            this.PrmMonth.ValueInfo = "1";
            // 
            // PrmYear
            // 
            this.PrmYear.Description = "Year";
            this.PrmYear.Name = "PrmYear";
            this.PrmYear.Type = typeof(int);
            this.PrmYear.ValueInfo = "2017";
            // 
            // PrmPayrollRunType
            // 
            this.PrmPayrollRunType.Description = "Run Type";
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(0, "Regular"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(1, "Off-Cycle"));
            this.PrmPayrollRunType.LookUpSettings = staticListLookUpSettings2;
            this.PrmPayrollRunType.Name = "PrmPayrollRunType";
            this.PrmPayrollRunType.Type = typeof(int);
            this.PrmPayrollRunType.ValueInfo = "0";
            // 
            // srptPayslipLeaveInfo
            // 
            this.srptPayslipLeaveInfo.LocationFloat = new DevExpress.Utils.PointFloat(6.420423F, 627.1792F);
            this.srptPayslipLeaveInfo.Name = "srptPayslipLeaveInfo";
            this.srptPayslipLeaveInfo.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmEmpCode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPayslipLeaveInfo.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmMonth", this.PrmMonth));
            this.srptPayslipLeaveInfo.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmYear", this.PrmYear));
            this.srptPayslipLeaveInfo.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmPayrollRunType", this.PrmPayrollRunType));
            this.srptPayslipLeaveInfo.ReportSource = new HCMv3.Reports.Payroll.srptPaySlipLeaveInfoEn();
            this.srptPayslipLeaveInfo.SizeF = new System.Drawing.SizeF(399.89F, 22.38617F);
            this.srptPayslipLeaveInfo.Visible = false;
            // 
            // srptPayslipIndemnityDaysSummaryEn
            // 
            this.srptPayslipIndemnityDaysSummaryEn.LocationFloat = new DevExpress.Utils.PointFloat(268F, 692.14F);
            this.srptPayslipIndemnityDaysSummaryEn.Name = "srptPayslipIndemnityDaysSummaryEn";
            this.srptPayslipIndemnityDaysSummaryEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("empcode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPayslipIndemnityDaysSummaryEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "PayslipOrgV2Query.ForPeriod"));
            this.srptPayslipIndemnityDaysSummaryEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "PayslipOrgV2Query.InPeriod"));
            this.srptPayslipIndemnityDaysSummaryEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("RunType", null, "PayslipOrgV2Query.PayrollRunType"));
            this.srptPayslipIndemnityDaysSummaryEn.ReportSource = new HCMv3.Reports.Payroll.srptPayslipIndemnityDaysSummaryEn();
            this.srptPayslipIndemnityDaysSummaryEn.SizeF = new System.Drawing.SizeF(290F, 140.5F);
            // 
            // srptPayslipWorkingDaysSummary
            // 
            this.srptPayslipWorkingDaysSummary.LocationFloat = new DevExpress.Utils.PointFloat(559.0603F, 692.14F);
            this.srptPayslipWorkingDaysSummary.Name = "srptPayslipWorkingDaysSummary";
            this.srptPayslipWorkingDaysSummary.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("empcode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPayslipWorkingDaysSummary.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "PayslipOrgV2Query.ForPeriod"));
            this.srptPayslipWorkingDaysSummary.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "PayslipOrgV2Query.InPeriod"));
            this.srptPayslipWorkingDaysSummary.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("RunType", null, "PayslipOrgV2Query.PayrollRunType"));
            this.srptPayslipWorkingDaysSummary.ReportSource = new HCMv3.Reports.Payroll.srptPayslipWorkingDaysSummaryEn();
            this.srptPayslipWorkingDaysSummary.SizeF = new System.Drawing.SizeF(250.94F, 140.5F);
            // 
            // srptPayslipAttendenceSummaryEn
            // 
            this.srptPayslipAttendenceSummaryEn.LocationFloat = new DevExpress.Utils.PointFloat(6.07F, 692.14F);
            this.srptPayslipAttendenceSummaryEn.Name = "srptPayslipAttendenceSummaryEn";
            this.srptPayslipAttendenceSummaryEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("EmpCode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPayslipAttendenceSummaryEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "PayslipOrgV2Query.ForPeriod"));
            this.srptPayslipAttendenceSummaryEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "PayslipOrgV2Query.InPeriod"));
            this.srptPayslipAttendenceSummaryEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("RunType", null, "PayslipOrgV2Query.PayrollRunType"));
            this.srptPayslipAttendenceSummaryEn.ReportSource = new HCMv3.Reports.Payroll.srptPayslipAttendenceSummaryEn();
            this.srptPayslipAttendenceSummaryEn.SizeF = new System.Drawing.SizeF(256.73F, 140.5F);
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(100)))), ((int)(((byte)(160)))));
            this.lblHeader.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[HeaderText]")});
            this.lblHeader.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.LocationFloat = new DevExpress.Utils.PointFloat(6.07F, 115F);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblHeader.SizeF = new System.Drawing.SizeF(803.9296F, 29.66F);
            this.lblHeader.StylePriority.UseBackColor = false;
            this.lblHeader.StylePriority.UseFont = false;
            this.lblHeader.StylePriority.UseForeColor = false;
            this.lblHeader.StylePriority.UseTextAlignment = false;
            this.lblHeader.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // srptPayslipEarning
            // 
            this.srptPayslipEarning.LocationFloat = new DevExpress.Utils.PointFloat(6.419525F, 325.3F);
            this.srptPayslipEarning.Name = "srptPayslipEarning";
            this.srptPayslipEarning.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("empcode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPayslipEarning.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "PayslipOrgV2Query.ForPeriod"));
            this.srptPayslipEarning.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "PayslipOrgV2Query.InPeriod"));
            this.srptPayslipEarning.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("RunType", null, "PayslipOrgV2Query.PayrollRunType"));
            this.srptPayslipEarning.ReportSource = new HCMv3.Reports.Payroll.srptPaySlipEarningEn();
            this.srptPayslipEarning.SizeF = new System.Drawing.SizeF(395.1488F, 221.986F);
            // 
            // srptPayslipDeduction
            // 
            this.srptPayslipDeduction.LocationFloat = new DevExpress.Utils.PointFloat(414.8503F, 325.3F);
            this.srptPayslipDeduction.Name = "srptPayslipDeduction";
            this.srptPayslipDeduction.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("EmpCode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPayslipDeduction.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "PayslipOrgV2Query.ForPeriod"));
            this.srptPayslipDeduction.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "PayslipOrgV2Query.InPeriod"));
            this.srptPayslipDeduction.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("RunType", null, "PayslipOrgV2Query.PayrollRunType"));
            this.srptPayslipDeduction.ReportSource = new HCMv3.Reports.Payroll.srptPaySlipDeductionEn();
            this.srptPayslipDeduction.SizeF = new System.Drawing.SizeF(395.15F, 220F);
            // 
            // xrLabel4
            // 
            this.xrLabel4.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Double;
            this.xrLabel4.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel4.BorderWidth = 2F;
            this.xrLabel4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Currency]+\' \'+ [CalcNetPay]")});
            this.xrLabel4.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(625.9016F, 614.89F);
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 5, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(184.0987F, 22.99994F);
            this.xrLabel4.StyleName = "ENGDataStyle";
            this.xrLabel4.StylePriority.UseBorderDashStyle = false;
            this.xrLabel4.StylePriority.UseBorders = false;
            this.xrLabel4.StylePriority.UseBorderWidth = false;
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UsePadding = false;
            this.xrLabel4.StylePriority.UseTextAlignment = false;
            xrSummary1.IgnoreNullValues = true;
            this.xrLabel4.Summary = xrSummary1;
            this.xrLabel4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel4.TextFormatString = " {0}";
            // 
            // xrLabel31
            // 
            this.xrLabel31.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Double;
            this.xrLabel31.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel31.BorderWidth = 2F;
            this.xrLabel31.LocationFloat = new DevExpress.Utils.PointFloat(456.06F, 614.89F);
            this.xrLabel31.Name = "xrLabel31";
            this.xrLabel31.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 5, 100F);
            this.xrLabel31.SizeF = new System.Drawing.SizeF(156.7107F, 22.99997F);
            this.xrLabel31.StyleName = "EngMainDataStyle";
            this.xrLabel31.StylePriority.UseBorderDashStyle = false;
            this.xrLabel31.StylePriority.UseBorders = false;
            this.xrLabel31.StylePriority.UseBorderWidth = false;
            this.xrLabel31.StylePriority.UsePadding = false;
            this.xrLabel31.StylePriority.UseTextAlignment = false;
            this.xrLabel31.Text = "NET AMOUNT PAID :";
            this.xrLabel31.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Image", "[CompanyLogo]")});
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(297.52F, 10F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(204.3078F, 94.37504F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            // 
            // PrmEmpCode
            // 
            this.PrmEmpCode.Description = "Employee Code";
            this.PrmEmpCode.Name = "PrmEmpCode";
            // 
            // xrLabel5
            // 
            this.xrLabel5.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(12.08F, 67F);
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.SizeF = new System.Drawing.SizeF(46.79168F, 23F);
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.StylePriority.UseTextAlignment = false;
            this.xrLabel5.Text = "User :";
            this.xrLabel5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // LoggedUserNme
            // 
            this.LoggedUserNme.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LoggedUserNme.LocationFloat = new DevExpress.Utils.PointFloat(58.87168F, 66.99999F);
            this.LoggedUserNme.Name = "LoggedUserNme";
            this.LoggedUserNme.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.LoggedUserNme.SizeF = new System.Drawing.SizeF(138.7661F, 23.00001F);
            this.LoggedUserNme.StylePriority.UseFont = false;
            this.LoggedUserNme.StylePriority.UseTextAlignment = false;
            this.LoggedUserNme.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel3
            // 
            this.xrLabel3.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CalcDateTime]")});
            this.xrLabel3.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(487.9401F, 66.99999F);
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.SizeF = new System.Drawing.SizeF(322.0595F, 23F);
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.StylePriority.UseTextAlignment = false;
            this.xrLabel3.Text = "xrLabel3";
            this.xrLabel3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel3.TextFormatString = "{0:dddd, dd\\/MMMM\\/yyyy hh:mm tt}";
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 0F;
            this.TopMargin.Name = "TopMargin";
            this.TopMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.TopMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.TopMargin.Visible = false;
            // 
            // BottomMargin
            // 
            this.BottomMargin.HeightF = 10F;
            this.BottomMargin.Name = "BottomMargin";
            this.BottomMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.BottomMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // EngMainDataStyle
            // 
            this.EngMainDataStyle.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.EngMainDataStyle.Name = "EngMainDataStyle";
            this.EngMainDataStyle.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.EngMainDataStyle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // ENGDataStyle
            // 
            this.ENGDataStyle.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ENGDataStyle.Name = "ENGDataStyle";
            this.ENGDataStyle.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.ENGDataStyle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // ReportHeader
            // 
            this.ReportHeader.HeightF = 1.666673F;
            this.ReportHeader.KeepTogether = true;
            this.ReportHeader.Name = "ReportHeader";
            // 
            // CalcDateTime
            // 
            this.CalcDateTime.DataMember = "PayslipOrgV2Query";
            this.CalcDateTime.Expression = "LocalDateTimeNow()";
            this.CalcDateTime.Name = "CalcDateTime";
            // 
            // CalcNetPay
            // 
            this.CalcNetPay.DataMember = "PayslipOrgV2Query";
            this.CalcNetPay.Expression = "Round([Net Pay],3)";
            this.CalcNetPay.Name = "CalcNetPay";
            // 
            // PageFooter
            // 
            this.PageFooter.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel5,
            this.LoggedUserNme,
            this.xrLabel3});
            this.PageFooter.HeightF = 100F;
            this.PageFooter.Name = "PageFooter";
            // 
            // PrmSendMail
            // 
            this.PrmSendMail.Description = "Send Email";
            this.PrmSendMail.Name = "PrmSendMail";
            this.PrmSendMail.Type = typeof(bool);
            this.PrmSendMail.ValueInfo = "False";
            // 
            // PrmEncashment
            // 
            this.PrmEncashment.Description = "Is Encashment Print";
            this.PrmEncashment.Name = "PrmEncashment";
            this.PrmEncashment.Type = typeof(bool);
            this.PrmEncashment.ValueInfo = "False";
            this.PrmEncashment.Visible = false;
            // 
            // PrmPayrollArea
            // 
            this.PrmPayrollArea.Description = "Payroll Area";
            dynamicListLookUpSettings1.DataAdapter = null;
            dynamicListLookUpSettings1.DataMember = "PayrollArea";
            dynamicListLookUpSettings1.DataSource = this.PayslipDS;
            dynamicListLookUpSettings1.DisplayMember = "Name";
            dynamicListLookUpSettings1.ValueMember = "Name";
            this.PrmPayrollArea.LookUpSettings = dynamicListLookUpSettings1;
            this.PrmPayrollArea.Name = "PrmPayrollArea";
            // 
            // PrmEmpStatus
            // 
            this.PrmEmpStatus.Description = "Employee Status";
            staticListLookUpSettings3.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(-1, "ALL"));
            staticListLookUpSettings3.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(0, "Inactive"));
            staticListLookUpSettings3.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(1, "Active"));
            this.PrmEmpStatus.LookUpSettings = staticListLookUpSettings3;
            this.PrmEmpStatus.Name = "PrmEmpStatus";
            this.PrmEmpStatus.Type = typeof(int);
            this.PrmEmpStatus.ValueInfo = "1";
            // 
            // rptPayslipEn
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin,
            this.ReportHeader,
            this.PageFooter});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.CalcDateTime,
            this.CalcNetPay});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.PayslipDS});
            this.DataMember = "PayslipOrgV2Query";
            this.DataSource = this.PayslipDS;
            this.Margins = new System.Drawing.Printing.Margins(5, 6, 0, 10);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.PrmPayrollArea,
            this.PrmEmpCode,
            this.PrmMonth,
            this.PrmYear,
            this.PrmPayrollRunType,
            this.PrmSendMail,
            this.PrmEncashment,
            this.PrmEmpStatus});
            // PORT NOTE: "this.Scripts.OnBeforePrint" removed. Scripts require the report to carry
            // compilable script source; the same handler is wired below as a real CLR event.
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.EngMainDataStyle,
            this.ENGDataStyle});
            this.Version = "23.2";
            this.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.rptPayslipEn_1_BeforePrint);
            // PORT NOTE: the legacy AfterPrint handler exported the document to PDF and SMTP-mailed
            // it. That behaviour already lives in HCMv3.Application (ReportsAppService
            // .SendPayslipEmails / BuildPayslipEmailBodyHtml) and must not be duplicated inside a
            // report that the Web Document Viewer renders on every page request.
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.XRControlStyle EngMainDataStyle;
        private DevExpress.XtraReports.UI.XRSubreport srptPayslipDeduction;
        private DevExpress.XtraReports.UI.XRControlStyle ENGDataStyle;
        private DevExpress.XtraReports.UI.XRLabel xrLabel31;
        private DevExpress.XtraReports.Parameters.Parameter PrmMonth;
        private DevExpress.XtraReports.Parameters.Parameter PrmYear;
        private DevExpress.XtraReports.UI.XRLabel xrLabel4;
        private DevExpress.DataAccess.Sql.SqlDataSource PayslipDS;
        private DevExpress.XtraReports.UI.XRSubreport srptPayslipEarning;
        private DevExpress.XtraReports.UI.XRSubreport srptPayslipAttendenceSummaryEn;
        private DevExpress.XtraReports.UI.XRSubreport srptPayslipIndemnityDaysSummaryEn;
        private DevExpress.XtraReports.UI.XRLabel lblHeader;
        private DevExpress.XtraReports.UI.XRSubreport srptPayslipWorkingDaysSummary;
        private DevExpress.XtraReports.UI.XRLabel xrLabel3;
        private DevExpress.XtraReports.UI.ReportHeaderBand ReportHeader;
        private DevExpress.XtraReports.UI.CalculatedField CalcDateTime;
        private DevExpress.XtraReports.UI.XRLabel LoggedUserNme;
        private DevExpress.XtraReports.UI.XRLabel xrLabel5;
        private DevExpress.XtraReports.UI.CalculatedField CalcNetPay;
        private DevExpress.XtraReports.UI.PageFooterBand PageFooter;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox1;
        private DevExpress.XtraReports.Parameters.Parameter PrmPayrollRunType;
        private DevExpress.XtraReports.Parameters.Parameter PrmSendMail;
        private DevExpress.XtraReports.UI.XRSubreport srptPayslipLeaveInfo;
        private DevExpress.XtraReports.Parameters.Parameter PrmEmpCode;
        private DevExpress.XtraReports.UI.XRSubreport srptPayslipLeaveEncashment;
        private DevExpress.XtraReports.Parameters.Parameter PrmEncashment;
        private DevExpress.XtraReports.Parameters.Parameter PrmPayrollArea;
        private DevExpress.XtraReports.UI.XRLabel xrLabel8;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel9;
        private DevExpress.XtraReports.UI.XRLabel xrLabel10;
        private DevExpress.XtraReports.UI.XRLabel xrLabel17;
        private DevExpress.XtraReports.UI.XRLabel xrLabel18;
        private DevExpress.XtraReports.UI.XRLabel xrLabel19;
        private DevExpress.XtraReports.UI.XRLabel xrLabel15;
        private DevExpress.XtraReports.UI.XRLabel xrLabel16;
        private DevExpress.XtraReports.UI.XRLabel xrLabel11;
        private DevExpress.XtraReports.UI.XRLabel xrLabel12;
        private DevExpress.XtraReports.UI.XRLabel xrLabel23;
        private DevExpress.XtraReports.UI.XRLabel xrLabel24;
        private DevExpress.XtraReports.UI.XRLabel xrLabel25;
        private DevExpress.XtraReports.UI.XRLabel xrLabel26;
        private DevExpress.XtraReports.UI.XRLabel xrLabel21;
        private DevExpress.XtraReports.UI.XRLabel xrLabel22;
        private DevExpress.XtraReports.UI.XRLabel xrLabel32;
        private DevExpress.XtraReports.UI.XRLabel xrLabel33;
        private DevExpress.XtraReports.UI.XRSubreport srptPaySlipBasicPayEn;
        private DevExpress.XtraReports.UI.XRPanel PanelSignature;
        private DevExpress.XtraReports.UI.XRLabel xrLabel7;
        private DevExpress.XtraReports.UI.XRLabel xrLabel6;
        private DevExpress.XtraReports.UI.XRLabel xrLabel35;
        private DevExpress.XtraReports.UI.XRSubreport srptPaySlipOvertimeHoursEn;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.XRLabel xrLabel13;
        private DevExpress.XtraReports.UI.XRLabel xrLabel14;
        private DevExpress.XtraReports.Parameters.Parameter PrmEmpStatus;
    }
}
