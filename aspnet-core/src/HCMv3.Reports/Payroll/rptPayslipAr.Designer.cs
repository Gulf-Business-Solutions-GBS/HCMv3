

namespace HCMv3.Reports.Payroll
{
    partial class rptPayslipAr
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
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery1 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.QueryParameter[] masterParameters1 = BuildMasterQueryParameters();
            DevExpress.DataAccess.Sql.QueryParameter[] masterParameters3 = BuildMasterQueryParameters();
            DevExpress.DataAccess.Sql.CustomSqlQuery payrollAreaQuery = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery2 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery3 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings1 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            DevExpress.XtraReports.UI.XRSummary xrSummary1 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings2 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings3 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            this.PayslipDS = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLabel12 = new DevExpress.XtraReports.UI.XRLabel();
            this.LoggedUserNme = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel33 = new DevExpress.XtraReports.UI.XRLabel();
            this.PanelSignature = new DevExpress.XtraReports.UI.XRPanel();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel9 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.srptPayslipAttendenceSummaryAr = new DevExpress.XtraReports.UI.XRSubreport();
            this.PrmPayrollRunType = new DevExpress.XtraReports.Parameters.Parameter();
            this.srptPayslipIndemnityDaysSummaryAr = new DevExpress.XtraReports.UI.XRSubreport();
            this.srptPayslipWorkingDaysSummaryAr = new DevExpress.XtraReports.UI.XRSubreport();
            this.xrLabel31 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.srptPaySlipEarningAr = new DevExpress.XtraReports.UI.XRSubreport();
            this.srptPaySlipDeductionAr = new DevExpress.XtraReports.UI.XRSubreport();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel10 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTable1 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow1 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell1 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell2 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell3 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell4 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow2 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell5 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell6 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell7 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell8 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow3 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell9 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell10 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell11 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell12 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow4 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell13 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell14 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell15 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell16 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow5 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell17 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell18 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell19 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell20 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow6 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell23 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell24 = new DevExpress.XtraReports.UI.XRTableCell();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.Ar_MainDataStyle = new DevExpress.XtraReports.UI.XRControlStyle();
            this.Ar_DataStyle = new DevExpress.XtraReports.UI.XRControlStyle();
            this.CalcInPeriodMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcInPeriodYear = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcForPeriodMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcForPeriodYear = new DevExpress.XtraReports.UI.CalculatedField();
            this.PrmPayrollArea = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmPeriodMonth = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmPeriodYear = new DevExpress.XtraReports.Parameters.Parameter();
            this.CalcArMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcNetPay = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcDateTime = new DevExpress.XtraReports.UI.CalculatedField();
            this.PrmforPayroll = new DevExpress.XtraReports.Parameters.Parameter();
            this.ReportHeader = new DevExpress.XtraReports.UI.ReportHeaderBand();
            this.CalcJoiningDateMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcJoingDateMonthNumber = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcPassportMonthNum = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcPassportMonthAr = new DevExpress.XtraReports.UI.CalculatedField();
            this.PrmempStatus = new DevExpress.XtraReports.Parameters.Parameter();
            this.CalcHeader = new DevExpress.XtraReports.UI.CalculatedField();
            this.PrmSendMail = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmEmployeeCode = new DevExpress.XtraReports.Parameters.Parameter();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // PayslipDS
            // 
            this.PayslipDS.ConnectionName = HCMv3.Reports.HcmReportBase.DesignTimeConnectionName;
            this.PayslipDS.Name = "PayslipDS";
            customSqlQuery1.Name = "PayslipQuery";
            customSqlQuery1.Sql = PayslipArSql.PayslipQuery;
            customSqlQuery1.Parameters.AddRange(masterParameters1);
            payrollAreaQuery.Name = "PayrollArea";
            payrollAreaQuery.Sql = PayslipArSql.PayrollArea;
            customSqlQuery2.Name = "PayslipLogoQuery";
            customSqlQuery2.Sql = PayslipArSql.PayslipLogoQuery;
            customSqlQuery3.Name = "PayslipOrgV2Query";
            customSqlQuery3.Sql = PayslipArSql.PayslipOrgV2Query;
            customSqlQuery3.Parameters.AddRange(masterParameters3);
            this.PayslipDS.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1,
            payrollAreaQuery,
            customSqlQuery2,
            customSqlQuery3});
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel12,
            this.LoggedUserNme,
            this.xrLabel33,
            this.PanelSignature,
            this.xrPictureBox1,
            this.xrLabel2,
            this.srptPayslipAttendenceSummaryAr,
            this.srptPayslipIndemnityDaysSummaryAr,
            this.srptPayslipWorkingDaysSummaryAr,
            this.xrLabel31,
            this.xrLabel4,
            this.srptPaySlipEarningAr,
            this.srptPaySlipDeductionAr,
            this.xrLabel1,
            this.xrLabel10,
            this.xrTable1});
            this.Detail.HeightF = 1045.75F;
            this.Detail.KeepTogether = true;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabel12
            // 
            this.xrLabel12.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrLabel12.LocationFloat = new DevExpress.Utils.PointFloat(688.1558F, 1000.833F);
            this.xrLabel12.Name = "xrLabel12";
            this.xrLabel12.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel12.SizeF = new System.Drawing.SizeF(104.1667F, 23F);
            this.xrLabel12.StylePriority.UseFont = false;
            this.xrLabel12.StylePriority.UseTextAlignment = false;
            this.xrLabel12.Text = " : المستعمل";
            this.xrLabel12.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // LoggedUserNme
            // 
            this.LoggedUserNme.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LoggedUserNme.LocationFloat = new DevExpress.Utils.PointFloat(338.4314F, 1000.833F);
            this.LoggedUserNme.Name = "LoggedUserNme";
            this.LoggedUserNme.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.LoggedUserNme.SizeF = new System.Drawing.SizeF(349.7244F, 23F);
            this.LoggedUserNme.StylePriority.UseFont = false;
            // 
            // xrLabel33
            // 
            this.xrLabel33.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CalcDateTime]")});
            this.xrLabel33.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel33.LocationFloat = new DevExpress.Utils.PointFloat(5.322507F, 1000.833F);
            this.xrLabel33.Name = "xrLabel33";
            this.xrLabel33.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel33.SizeF = new System.Drawing.SizeF(307.4762F, 23F);
            this.xrLabel33.StylePriority.UseFont = false;
            this.xrLabel33.StylePriority.UseTextAlignment = false;
            this.xrLabel33.Text = "xrLabel3";
            this.xrLabel33.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel33.TextFormatString = "{0:dddd, d MMMM, yyyy hh:mm tt}";
            this.xrLabel33.Visible = false;
            // 
            // PanelSignature
            // 
            this.PanelSignature.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel6,
            this.xrLabel9});
            this.PanelSignature.LocationFloat = new DevExpress.Utils.PointFloat(5.322507F, 832.375F);
            this.PanelSignature.Name = "PanelSignature";
            this.PanelSignature.SizeF = new System.Drawing.SizeF(801.6775F, 36.66663F);
            // 
            // xrLabel6
            // 
            this.xrLabel6.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(682.8333F, 6.166687F);
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(104.1667F, 23F);
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UseTextAlignment = false;
            this.xrLabel6.Text = " : موافق عليه";
            this.xrLabel6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel9
            // 
            this.xrLabel9.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrLabel9.LocationFloat = new DevExpress.Utils.PointFloat(231.504F, 6.166687F);
            this.xrLabel9.Name = "xrLabel9";
            this.xrLabel9.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel9.SizeF = new System.Drawing.SizeF(180F, 22.99998F);
            this.xrLabel9.StylePriority.UseFont = false;
            this.xrLabel9.StylePriority.UseTextAlignment = false;
            this.xrLabel9.Text = " : تمت مراجعته من قبل";
            this.xrLabel9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.BorderColor = System.Drawing.Color.Transparent;
            this.xrPictureBox1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Image", "[PayslipLogoQuery].[logo]")});
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(298.3771F, 0F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(164.6478F, 84.37502F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            this.xrPictureBox1.StylePriority.UseBorderColor = false;
            // 
            // xrLabel2
            // 
            this.xrLabel2.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(688.1558F, 919.6667F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(104.1667F, 22.99994F);
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = ": ملاحظات";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // srptPayslipAttendenceSummaryAr
            // 
            this.srptPayslipAttendenceSummaryAr.LocationFloat = new DevExpress.Utils.PointFloat(549.4713F, 649.2084F);
            this.srptPayslipAttendenceSummaryAr.Name = "srptPayslipAttendenceSummaryAr";
            this.srptPayslipAttendenceSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "PayslipOrgV2Query.InPeriod"));
            this.srptPayslipAttendenceSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "PayslipOrgV2Query.ForPeriod"));
            this.srptPayslipAttendenceSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("empcode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPayslipAttendenceSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PayrollRuntype", this.PrmPayrollRunType));
            this.srptPayslipAttendenceSummaryAr.ReportSource = new HCMv3.Reports.Payroll.srptPayslipAttendenceSummaryAr();
            this.srptPayslipAttendenceSummaryAr.SizeF = new System.Drawing.SizeF(260.6569F, 140.4999F);
            // 
            // PrmPayrollRunType
            // 
            this.PrmPayrollRunType.Description = "Run Type";
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(0, "Regular"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(1, "Off-Cycle"));
            this.PrmPayrollRunType.LookUpSettings = staticListLookUpSettings1;
            this.PrmPayrollRunType.Name = "PrmPayrollRunType";
            this.PrmPayrollRunType.Type = typeof(int);
            this.PrmPayrollRunType.ValueInfo = "0";
            // 
            // srptPayslipIndemnityDaysSummaryAr
            // 
            this.srptPayslipIndemnityDaysSummaryAr.LocationFloat = new DevExpress.Utils.PointFloat(276.3026F, 649.2084F);
            this.srptPayslipIndemnityDaysSummaryAr.Name = "srptPayslipIndemnityDaysSummaryAr";
            this.srptPayslipIndemnityDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "PayslipOrgV2Query.InPeriod"));
            this.srptPayslipIndemnityDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "PayslipOrgV2Query.ForPeriod"));
            this.srptPayslipIndemnityDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("EmpCode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPayslipIndemnityDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PayrollRunType", this.PrmPayrollRunType));
            this.srptPayslipIndemnityDaysSummaryAr.ReportSource = new HCMv3.Reports.Payroll.srptPayslipIndemnityDaysSummaryAr();
            this.srptPayslipIndemnityDaysSummaryAr.SizeF = new System.Drawing.SizeF(256.905F, 140.4999F);
            // 
            // srptPayslipWorkingDaysSummaryAr
            // 
            this.srptPayslipWorkingDaysSummaryAr.LocationFloat = new DevExpress.Utils.PointFloat(5.322504F, 649.2084F);
            this.srptPayslipWorkingDaysSummaryAr.Name = "srptPayslipWorkingDaysSummaryAr";
            this.srptPayslipWorkingDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "PayslipOrgV2Query.ForPeriod"));
            this.srptPayslipWorkingDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "PayslipOrgV2Query.InPeriod"));
            this.srptPayslipWorkingDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("empcode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPayslipWorkingDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PayrollRunType", this.PrmPayrollRunType));
            this.srptPayslipWorkingDaysSummaryAr.ReportSource = new HCMv3.Reports.Payroll.srptPayslipWorkingDaysSummaryAr();
            this.srptPayslipWorkingDaysSummaryAr.SizeF = new System.Drawing.SizeF(248.0124F, 140.4999F);
            // 
            // xrLabel31
            // 
            this.xrLabel31.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel31.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel31.BorderWidth = 2F;
            this.xrLabel31.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrLabel31.LocationFloat = new DevExpress.Utils.PointFloat(111.6393F, 599.7917F);
            this.xrLabel31.Name = "xrLabel31";
            this.xrLabel31.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 5, 100F);
            this.xrLabel31.SizeF = new System.Drawing.SizeF(141.6956F, 22.99994F);
            this.xrLabel31.StylePriority.UseBorderDashStyle = false;
            this.xrLabel31.StylePriority.UseBorders = false;
            this.xrLabel31.StylePriority.UseBorderWidth = false;
            this.xrLabel31.StylePriority.UseFont = false;
            this.xrLabel31.StylePriority.UsePadding = false;
            this.xrLabel31.StylePriority.UseTextAlignment = false;
            this.xrLabel31.Text = "صافي المبلغ المدفوع";
            this.xrLabel31.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel4
            // 
            this.xrLabel4.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel4.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel4.BorderWidth = 2F;
            this.xrLabel4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CalcNetPay]")});
            this.xrLabel4.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(25.1944F, 599.7917F);
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 7, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(86.44498F, 23F);
            this.xrLabel4.StylePriority.UseBorderDashStyle = false;
            this.xrLabel4.StylePriority.UseBorders = false;
            this.xrLabel4.StylePriority.UseBorderWidth = false;
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UsePadding = false;
            this.xrLabel4.StylePriority.UseTextAlignment = false;
            xrSummary1.IgnoreNullValues = true;
            this.xrLabel4.Summary = xrSummary1;
            this.xrLabel4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel4.TextFormatString = "{0:0.000}";
            // 
            // srptPaySlipEarningAr
            // 
            this.srptPaySlipEarningAr.LocationFloat = new DevExpress.Utils.PointFloat(416.239F, 298.4167F);
            this.srptPaySlipEarningAr.Name = "srptPaySlipEarningAr";
            this.srptPaySlipEarningAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "PayslipOrgV2Query.InPeriod"));
            this.srptPaySlipEarningAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "PayslipOrgV2Query.ForPeriod"));
            this.srptPaySlipEarningAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("EmployeeCode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPaySlipEarningAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PayrollRunType", this.PrmPayrollRunType));
            this.srptPaySlipEarningAr.ReportSource = new HCMv3.Reports.Payroll.srptPaySlipEarningAr();
            this.srptPaySlipEarningAr.SizeF = new System.Drawing.SizeF(386.7657F, 292.7916F);
            // 
            // srptPaySlipDeductionAr
            // 
            this.srptPaySlipDeductionAr.LocationFloat = new DevExpress.Utils.PointFloat(12.99996F, 298.4168F);
            this.srptPaySlipDeductionAr.Name = "srptPaySlipDeductionAr";
            this.srptPaySlipDeductionAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "PayslipOrgV2Query.InPeriod"));
            this.srptPaySlipDeductionAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "PayslipOrgV2Query.ForPeriod"));
            this.srptPaySlipDeductionAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("EmployeeCode", null, "PayslipOrgV2Query.Employee Code"));
            this.srptPaySlipDeductionAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PayrollRunType", this.PrmPayrollRunType));
            this.srptPaySlipDeductionAr.ReportSource = new HCMv3.Reports.Payroll.srptPaySlipDeductionAr();
            this.srptPaySlipDeductionAr.SizeF = new System.Drawing.SizeF(372.7854F, 292.7915F);
            // 
            // xrLabel1
            // 
            this.xrLabel1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CalcHeader]")});
            this.xrLabel1.Font = new System.Drawing.Font("Arial", 13.8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel1.ForeColor = System.Drawing.Color.White;
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(10F, 90.375F);
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(793.0048F, 23F);
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UseForeColor = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.Text = ":قسيمة الدفع للشهر";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel10
            // 
            this.xrLabel10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(100)))), ((int)(((byte)(160)))));
            this.xrLabel10.ForeColor = System.Drawing.Color.White;
            this.xrLabel10.LocationFloat = new DevExpress.Utils.PointFloat(8.040571F, 88.37502F);
            this.xrLabel10.Name = "xrLabel10";
            this.xrLabel10.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel10.SizeF = new System.Drawing.SizeF(797.1282F, 29.66463F);
            this.xrLabel10.StylePriority.UseBackColor = false;
            this.xrLabel10.StylePriority.UseForeColor = false;
            this.xrLabel10.StylePriority.UseTextAlignment = false;
            this.xrLabel10.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrTable1
            // 
            this.xrTable1.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable1.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTable1.LocationFloat = new DevExpress.Utils.PointFloat(8.040565F, 126.5004F);
            this.xrTable1.Name = "xrTable1";
            this.xrTable1.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow1,
            this.xrTableRow2,
            this.xrTableRow3,
            this.xrTableRow4,
            this.xrTableRow5,
            this.xrTableRow6});
            this.xrTable1.SizeF = new System.Drawing.SizeF(794.9641F, 166.3547F);
            this.xrTable1.StylePriority.UseBorders = false;
            this.xrTable1.StylePriority.UseFont = false;
            // 
            // xrTableRow1
            // 
            this.xrTableRow1.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell1,
            this.xrTableCell2,
            this.xrTableCell3,
            this.xrTableCell4});
            this.xrTableRow1.Name = "xrTableRow1";
            this.xrTableRow1.Weight = 1D;
            // 
            // xrTableCell1
            // 
            this.xrTableCell1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[OrgUnit]")});
            this.xrTableCell1.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell1.Name = "xrTableCell1";
            this.xrTableCell1.StylePriority.UseFont = false;
            this.xrTableCell1.StylePriority.UseTextAlignment = false;
            this.xrTableCell1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell1.Weight = 1.3498072737535753D;
            // 
            // xrTableCell2
            // 
            this.xrTableCell2.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell2.Name = "xrTableCell2";
            this.xrTableCell2.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 5, 0, 0, 100F);
            this.xrTableCell2.StylePriority.UseFont = false;
            this.xrTableCell2.StylePriority.UsePadding = false;
            this.xrTableCell2.StylePriority.UseTextAlignment = false;
            this.xrTableCell2.Text = "وحدة التنظيم    ";
            this.xrTableCell2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell2.Weight = 0.65019272624642477D;
            // 
            // xrTableCell3
            // 
            this.xrTableCell3.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Employee Code]")});
            this.xrTableCell3.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell3.Name = "xrTableCell3";
            this.xrTableCell3.StylePriority.UseFont = false;
            this.xrTableCell3.StylePriority.UseTextAlignment = false;
            this.xrTableCell3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell3.Weight = 1.332755354214902D;
            // 
            // xrTableCell4
            // 
            this.xrTableCell4.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell4.Name = "xrTableCell4";
            this.xrTableCell4.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 5, 0, 0, 100F);
            this.xrTableCell4.StylePriority.UseFont = false;
            this.xrTableCell4.StylePriority.UsePadding = false;
            this.xrTableCell4.StylePriority.UseTextAlignment = false;
            this.xrTableCell4.Text = "رمز الموظف";
            this.xrTableCell4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell4.Weight = 0.667244645785098D;
            // 
            // xrTableRow2
            // 
            this.xrTableRow2.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell5,
            this.xrTableCell6,
            this.xrTableCell7,
            this.xrTableCell8});
            this.xrTableRow2.Name = "xrTableRow2";
            this.xrTableRow2.Weight = 1D;
            // 
            // xrTableCell5
            // 
            this.xrTableCell5.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Grade]")});
            this.xrTableCell5.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell5.Name = "xrTableCell5";
            this.xrTableCell5.StylePriority.UseFont = false;
            this.xrTableCell5.StylePriority.UseTextAlignment = false;
            this.xrTableCell5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell5.Weight = 1.3498072737535751D;
            // 
            // xrTableCell6
            // 
            this.xrTableCell6.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell6.Name = "xrTableCell6";
            this.xrTableCell6.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 5, 0, 0, 100F);
            this.xrTableCell6.StylePriority.UseFont = false;
            this.xrTableCell6.StylePriority.UsePadding = false;
            this.xrTableCell6.StylePriority.UseTextAlignment = false;
            this.xrTableCell6.Text = "درجة";
            this.xrTableCell6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell6.Weight = 0.65019272624642488D;
            // 
            // xrTableCell7
            // 
            this.xrTableCell7.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Employee Name]")});
            this.xrTableCell7.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell7.Name = "xrTableCell7";
            this.xrTableCell7.StylePriority.UseFont = false;
            this.xrTableCell7.StylePriority.UseTextAlignment = false;
            this.xrTableCell7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell7.Weight = 1.3327550471059124D;
            // 
            // xrTableCell8
            // 
            this.xrTableCell8.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell8.Multiline = true;
            this.xrTableCell8.Name = "xrTableCell8";
            this.xrTableCell8.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 5, 0, 0, 100F);
            this.xrTableCell8.StylePriority.UseFont = false;
            this.xrTableCell8.StylePriority.UsePadding = false;
            this.xrTableCell8.StylePriority.UseTextAlignment = false;
            this.xrTableCell8.Text = "اسم الموظف\r\n";
            this.xrTableCell8.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell8.Weight = 0.66724495289408758D;
            // 
            // xrTableRow3
            // 
            this.xrTableRow3.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell9,
            this.xrTableCell10,
            this.xrTableCell11,
            this.xrTableCell12});
            this.xrTableRow3.Name = "xrTableRow3";
            this.xrTableRow3.Weight = 1D;
            // 
            // xrTableCell9
            // 
            this.xrTableCell9.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[PayslipQuery].[Designation]")});
            this.xrTableCell9.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell9.Name = "xrTableCell9";
            this.xrTableCell9.StylePriority.UseFont = false;
            this.xrTableCell9.StylePriority.UseTextAlignment = false;
            this.xrTableCell9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell9.Weight = 1.3498071201990802D;
            // 
            // xrTableCell10
            // 
            this.xrTableCell10.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell10.Name = "xrTableCell10";
            this.xrTableCell10.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 5, 0, 0, 100F);
            this.xrTableCell10.StylePriority.UseFont = false;
            this.xrTableCell10.StylePriority.UsePadding = false;
            this.xrTableCell10.StylePriority.UseTextAlignment = false;
            this.xrTableCell10.Text = "التعيين";
            this.xrTableCell10.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell10.Weight = 0.65019287980091967D;
            // 
            // xrTableCell11
            // 
            this.xrTableCell11.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Salary Profile]")});
            this.xrTableCell11.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell11.Name = "xrTableCell11";
            this.xrTableCell11.StylePriority.UseFont = false;
            this.xrTableCell11.StylePriority.UseTextAlignment = false;
            this.xrTableCell11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell11.Weight = 1.3327550471059122D;
            // 
            // xrTableCell12
            // 
            this.xrTableCell12.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell12.Multiline = true;
            this.xrTableCell12.Name = "xrTableCell12";
            this.xrTableCell12.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 5, 0, 0, 100F);
            this.xrTableCell12.StylePriority.UseFont = false;
            this.xrTableCell12.StylePriority.UsePadding = false;
            this.xrTableCell12.StylePriority.UseTextAlignment = false;
            this.xrTableCell12.Text = "الملف الراتب\r\n";
            this.xrTableCell12.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell12.Weight = 0.66724495289408758D;
            // 
            // xrTableRow4
            // 
            this.xrTableRow4.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell13,
            this.xrTableCell14,
            this.xrTableCell15,
            this.xrTableCell16});
            this.xrTableRow4.Name = "xrTableRow4";
            this.xrTableRow4.Weight = 1D;
            // 
            // xrTableCell13
            // 
            this.xrTableCell13.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Joining Date].[Year]+ \'/\' + [CalcJoiningDateMonth] + \'/\' + [Joining Date].[Day]")});
            this.xrTableCell13.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell13.Name = "xrTableCell13";
            this.xrTableCell13.StylePriority.UseFont = false;
            this.xrTableCell13.StylePriority.UseTextAlignment = false;
            this.xrTableCell13.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell13.Weight = 1.3498071201990802D;
            // 
            // xrTableCell14
            // 
            this.xrTableCell14.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell14.Multiline = true;
            this.xrTableCell14.Name = "xrTableCell14";
            this.xrTableCell14.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 5, 0, 0, 100F);
            this.xrTableCell14.StylePriority.UseFont = false;
            this.xrTableCell14.StylePriority.UsePadding = false;
            this.xrTableCell14.StylePriority.UseTextAlignment = false;
            this.xrTableCell14.Text = "التعويض التسجيل\r\n";
            this.xrTableCell14.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell14.Weight = 0.65019287980091967D;
            // 
            // xrTableCell15
            // 
            this.xrTableCell15.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Joining Date].[Year]+ \'/\' + [CalcJoiningDateMonth] + \'/\' + [Joining Date].[Day]")});
            this.xrTableCell15.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell15.Name = "xrTableCell15";
            this.xrTableCell15.StylePriority.UseFont = false;
            this.xrTableCell15.StylePriority.UseTextAlignment = false;
            this.xrTableCell15.Text = "xrTableCell15";
            this.xrTableCell15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell15.Weight = 1.3327550471059124D;
            // 
            // xrTableCell16
            // 
            this.xrTableCell16.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell16.Multiline = true;
            this.xrTableCell16.Name = "xrTableCell16";
            this.xrTableCell16.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 5, 0, 0, 100F);
            this.xrTableCell16.StylePriority.UseFont = false;
            this.xrTableCell16.StylePriority.UsePadding = false;
            this.xrTableCell16.StylePriority.UseTextAlignment = false;
            this.xrTableCell16.Text = "تاريخ الانضمام\r\n";
            this.xrTableCell16.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell16.Weight = 0.66724495289408758D;
            // 
            // xrTableRow5
            // 
            this.xrTableRow5.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell17,
            this.xrTableCell18,
            this.xrTableCell19,
            this.xrTableCell20});
            this.xrTableRow5.Name = "xrTableRow5";
            this.xrTableRow5.Weight = 1D;
            // 
            // xrTableCell17
            // 
            this.xrTableCell17.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[PassportExpiryDate].[Year] + \'/\' + [CalcPassportMonthAr]+ \'/\' + [PassportExpiryD" +
                    "ate].[Day]")});
            this.xrTableCell17.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell17.Name = "xrTableCell17";
            this.xrTableCell17.StylePriority.UseFont = false;
            this.xrTableCell17.StylePriority.UseTextAlignment = false;
            this.xrTableCell17.Text = "xrTableCell17";
            this.xrTableCell17.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell17.Weight = 1.3498071201990802D;
            // 
            // xrTableCell18
            // 
            this.xrTableCell18.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell18.Multiline = true;
            this.xrTableCell18.Name = "xrTableCell18";
            this.xrTableCell18.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 5, 0, 0, 100F);
            this.xrTableCell18.StylePriority.UseFont = false;
            this.xrTableCell18.StylePriority.UsePadding = false;
            this.xrTableCell18.StylePriority.UseTextAlignment = false;
            this.xrTableCell18.Text = "انتهاء جواز السفر\r\n";
            this.xrTableCell18.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell18.Weight = 0.65019287980091967D;
            // 
            // xrTableCell19
            // 
            this.xrTableCell19.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Bank Name]")});
            this.xrTableCell19.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell19.Name = "xrTableCell19";
            this.xrTableCell19.StylePriority.UseFont = false;
            this.xrTableCell19.StylePriority.UseTextAlignment = false;
            this.xrTableCell19.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell19.Weight = 1.3327550471059124D;
            // 
            // xrTableCell20
            // 
            this.xrTableCell20.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell20.Multiline = true;
            this.xrTableCell20.Name = "xrTableCell20";
            this.xrTableCell20.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 5, 0, 0, 100F);
            this.xrTableCell20.StylePriority.UseFont = false;
            this.xrTableCell20.StylePriority.UsePadding = false;
            this.xrTableCell20.StylePriority.UseTextAlignment = false;
            this.xrTableCell20.Text = "اسم البنك\r\n";
            this.xrTableCell20.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell20.Weight = 0.66724495289408758D;
            // 
            // xrTableRow6
            // 
            this.xrTableRow6.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell23,
            this.xrTableCell24});
            this.xrTableRow6.Name = "xrTableRow6";
            this.xrTableRow6.Weight = 1D;
            // 
            // xrTableCell23
            // 
            this.xrTableCell23.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Bank IBAN]")});
            this.xrTableCell23.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell23.Name = "xrTableCell23";
            this.xrTableCell23.StylePriority.UseFont = false;
            this.xrTableCell23.StylePriority.UseTextAlignment = false;
            this.xrTableCell23.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell23.Weight = 3.3327550471059126D;
            // 
            // xrTableCell24
            // 
            this.xrTableCell24.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell24.Name = "xrTableCell24";
            this.xrTableCell24.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 5, 0, 0, 100F);
            this.xrTableCell24.StylePriority.UseFont = false;
            this.xrTableCell24.StylePriority.UsePadding = false;
            this.xrTableCell24.StylePriority.UseTextAlignment = false;
            this.xrTableCell24.Text = "حساب البنك";
            this.xrTableCell24.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell24.Weight = 0.66724495289408758D;
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 0F;
            this.TopMargin.Name = "TopMargin";
            this.TopMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.TopMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // BottomMargin
            // 
            this.BottomMargin.HeightF = 3.746592F;
            this.BottomMargin.Name = "BottomMargin";
            this.BottomMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.BottomMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // Ar_MainDataStyle
            // 
            this.Ar_MainDataStyle.Font = new System.Drawing.Font("Tahoma", 10.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Ar_MainDataStyle.Name = "Ar_MainDataStyle";
            this.Ar_MainDataStyle.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Ar_MainDataStyle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // Ar_DataStyle
            // 
            this.Ar_DataStyle.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Ar_DataStyle.Name = "Ar_DataStyle";
            this.Ar_DataStyle.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Ar_DataStyle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // CalcInPeriodMonth
            // 
            this.CalcInPeriodMonth.DataMember = "PayslipOrgV2Query";
            this.CalcInPeriodMonth.Expression = "GetMonth([InPeriod])";
            this.CalcInPeriodMonth.Name = "CalcInPeriodMonth";
            // 
            // CalcInPeriodYear
            // 
            this.CalcInPeriodYear.DataMember = "PayslipOrgV2Query";
            this.CalcInPeriodYear.Expression = "GetYear([InPeriod])";
            this.CalcInPeriodYear.Name = "CalcInPeriodYear";
            // 
            // CalcForPeriodMonth
            // 
            this.CalcForPeriodMonth.DataMember = "PayslipOrgV2Query";
            this.CalcForPeriodMonth.Expression = "GetMonth([ForPeriod])";
            this.CalcForPeriodMonth.Name = "CalcForPeriodMonth";
            // 
            // CalcForPeriodYear
            // 
            this.CalcForPeriodYear.DataMember = "PayslipOrgV2Query";
            this.CalcForPeriodYear.Expression = "GetYear([ForPeriod])";
            this.CalcForPeriodYear.Name = "CalcForPeriodYear";
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
            // PrmPeriodMonth
            // 
            this.PrmPeriodMonth.Description = "Period Month";
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(1, "يناير"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(2, "فبراير"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(3, "مارس"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(4, "أبريل"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(5, "مايو"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(6, "يونيو"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(7, "يوليو"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(8, "أغسطس"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(9, "سبتمبر"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(10, "أكتوبر"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(11, "نوفمبر"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(12, "ديسمبر"));
            this.PrmPeriodMonth.LookUpSettings = staticListLookUpSettings2;
            this.PrmPeriodMonth.Name = "PrmPeriodMonth";
            this.PrmPeriodMonth.Type = typeof(int);
            this.PrmPeriodMonth.ValueInfo = "1";
            // 
            // PrmPeriodYear
            // 
            this.PrmPeriodYear.Description = "Period Year";
            this.PrmPeriodYear.Name = "PrmPeriodYear";
            this.PrmPeriodYear.Type = typeof(int);
            this.PrmPeriodYear.ValueInfo = "2024";
            // 
            // CalcArMonth
            // 
            this.CalcArMonth.DataMember = "PayslipOrgV2Query";
            this.CalcArMonth.Expression = PayslipArSql.CalcArMonthExpression;
            this.CalcArMonth.Name = "CalcArMonth";
            // 
            // CalcNetPay
            // 
            this.CalcNetPay.DataMember = "PayslipOrgV2Query";
            this.CalcNetPay.DisplayName = "CalcNetPay";
            this.CalcNetPay.Expression = "Round([Net Pay],3)";
            this.CalcNetPay.Name = "CalcNetPay";
            // 
            // CalcDateTime
            // 
            this.CalcDateTime.DataMember = "PayslipOrgV2Query";
            this.CalcDateTime.Expression = "LocalDateTimeNow()";
            this.CalcDateTime.Name = "CalcDateTime";
            // 
            // PrmforPayroll
            // 
            this.PrmforPayroll.Description = "forPayroll";
            this.PrmforPayroll.Name = "PrmforPayroll";
            this.PrmforPayroll.Type = typeof(bool);
            this.PrmforPayroll.ValueInfo = "False";
            this.PrmforPayroll.Visible = false;
            // 
            // ReportHeader
            // 
            this.ReportHeader.HeightF = 0F;
            this.ReportHeader.KeepTogether = true;
            this.ReportHeader.Name = "ReportHeader";
            // 
            // CalcJoiningDateMonth
            // 
            this.CalcJoiningDateMonth.DataMember = "PayslipOrgV2Query";
            this.CalcJoiningDateMonth.Expression = PayslipArSql.CalcJoiningDateMonthExpression;
            this.CalcJoiningDateMonth.Name = "CalcJoiningDateMonth";
            // 
            // CalcJoingDateMonthNumber
            // 
            this.CalcJoingDateMonthNumber.DataMember = "PayslipOrgV2Query";
            this.CalcJoingDateMonthNumber.Expression = "GetMonth([Joining Date])";
            this.CalcJoingDateMonthNumber.Name = "CalcJoingDateMonthNumber";
            // 
            // CalcPassportMonthNum
            // 
            this.CalcPassportMonthNum.DataMember = "PayslipOrgV2Query";
            this.CalcPassportMonthNum.Expression = "GetMonth([PassportExpiryDate])";
            this.CalcPassportMonthNum.Name = "CalcPassportMonthNum";
            // 
            // CalcPassportMonthAr
            // 
            this.CalcPassportMonthAr.DataMember = "PayslipOrgV2Query";
            this.CalcPassportMonthAr.Expression = PayslipArSql.CalcPassportMonthArExpression;
            this.CalcPassportMonthAr.Name = "CalcPassportMonthAr";
            // 
            // PrmempStatus
            // 
            this.PrmempStatus.Description = "Employee Status";
            staticListLookUpSettings3.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(0, "Inactive"));
            staticListLookUpSettings3.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(1, "Active"));
            this.PrmempStatus.LookUpSettings = staticListLookUpSettings3;
            this.PrmempStatus.Name = "PrmempStatus";
            this.PrmempStatus.Type = typeof(int);
            this.PrmempStatus.ValueInfo = "1";
            // 
            // CalcHeader
            // 
            this.CalcHeader.DataMember = "PayslipOrgV2Query";
            this.CalcHeader.Expression = PayslipArSql.CalcHeaderExpression;
            this.CalcHeader.Name = "CalcHeader";
            // 
            // PrmSendMail
            // 
            this.PrmSendMail.Description = "Send Mail";
            this.PrmSendMail.Name = "PrmSendMail";
            this.PrmSendMail.Type = typeof(bool);
            this.PrmSendMail.ValueInfo = "False";
            // 
            // PrmEmployeeCode
            // 
            this.PrmEmployeeCode.Description = "Employee Code";
            this.PrmEmployeeCode.Name = "PrmEmployeeCode";
            // 
            // rptPayslipAr
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin,
            this.ReportHeader});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.CalcInPeriodMonth,
            this.CalcInPeriodYear,
            this.CalcForPeriodMonth,
            this.CalcForPeriodYear,
            this.CalcArMonth,
            this.CalcNetPay,
            this.CalcDateTime,
            this.CalcJoiningDateMonth,
            this.CalcJoingDateMonthNumber,
            this.CalcPassportMonthNum,
            this.CalcPassportMonthAr,
            this.CalcHeader});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.PayslipDS});
            this.DataMember = "PayslipOrgV2Query";
            this.DataSource = this.PayslipDS;
            this.Margins = new System.Drawing.Printing.Margins(5, 5, 0, 4);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.PrmEmployeeCode,
            this.PrmPeriodMonth,
            this.PrmPeriodYear,
            this.PrmPayrollArea,
            this.PrmforPayroll,
            this.PrmempStatus,
            this.PrmPayrollRunType,
            this.PrmSendMail});
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.Ar_MainDataStyle,
            this.Ar_DataStyle});
            this.Version = "23.2";
            ((System.ComponentModel.ISupportInitialize)(this.xrTable1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.XRControlStyle Ar_MainDataStyle;
        private DevExpress.XtraReports.UI.XRControlStyle Ar_DataStyle;
        private DevExpress.XtraReports.UI.XRLabel xrLabel10;
        private DevExpress.DataAccess.Sql.SqlDataSource PayslipDS;
        private DevExpress.XtraReports.UI.CalculatedField CalcInPeriodMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcInPeriodYear;
        private DevExpress.XtraReports.UI.CalculatedField CalcForPeriodMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcForPeriodYear;
        private DevExpress.XtraReports.Parameters.Parameter PrmPayrollArea;
        private DevExpress.XtraReports.Parameters.Parameter PrmPeriodMonth;
        private DevExpress.XtraReports.Parameters.Parameter PrmPeriodYear;
        private DevExpress.XtraReports.UI.XRSubreport srptPaySlipEarningAr;
        private DevExpress.XtraReports.UI.XRSubreport srptPaySlipDeductionAr;
        private DevExpress.XtraReports.UI.XRSubreport srptPayslipAttendenceSummaryAr;
        private DevExpress.XtraReports.UI.XRSubreport srptPayslipIndemnityDaysSummaryAr;
        private DevExpress.XtraReports.UI.XRSubreport srptPayslipWorkingDaysSummaryAr;
        private DevExpress.XtraReports.UI.XRLabel xrLabel31;
        private DevExpress.XtraReports.UI.XRLabel xrLabel4;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.CalculatedField CalcArMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcNetPay;
        private DevExpress.XtraReports.UI.XRLabel xrLabel33;
        private DevExpress.XtraReports.UI.CalculatedField CalcDateTime;
        private DevExpress.XtraReports.Parameters.Parameter PrmforPayroll;
        private DevExpress.XtraReports.UI.ReportHeaderBand ReportHeader;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox1;
        private DevExpress.XtraReports.UI.CalculatedField CalcJoiningDateMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcJoingDateMonthNumber;
        private DevExpress.XtraReports.UI.CalculatedField CalcPassportMonthNum;
        private DevExpress.XtraReports.UI.CalculatedField CalcPassportMonthAr;
        private DevExpress.XtraReports.UI.XRTable xrTable1;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow1;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell1;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell2;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell3;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell4;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow2;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell5;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell6;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell7;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell8;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow3;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell9;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell10;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell11;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell12;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow4;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell13;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell14;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell15;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell16;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow5;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell17;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell18;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell19;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell20;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow6;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell23;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell24;
        private DevExpress.XtraReports.Parameters.Parameter PrmempStatus;
        private DevExpress.XtraReports.Parameters.Parameter PrmPayrollRunType;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        private DevExpress.XtraReports.UI.CalculatedField CalcHeader;
        private DevExpress.XtraReports.Parameters.Parameter PrmSendMail;
        private DevExpress.XtraReports.UI.XRLabel xrLabel9;
        private DevExpress.XtraReports.UI.XRLabel xrLabel6;
        private DevExpress.XtraReports.UI.XRPanel PanelSignature;
        private DevExpress.XtraReports.UI.XRLabel xrLabel12;
        private DevExpress.XtraReports.UI.XRLabel LoggedUserNme;
        private DevExpress.XtraReports.Parameters.Parameter PrmEmployeeCode;
    }
}
