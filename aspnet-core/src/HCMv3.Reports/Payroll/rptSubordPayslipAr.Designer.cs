namespace HCMv3.Reports.Payroll
{
    partial class rptSubordPayslipAr
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
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery2 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            DevExpress.XtraReports.UI.XRSummary xrSummary1 = new DevExpress.XtraReports.UI.XRSummary();
            this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.xrLabel32 = new DevExpress.XtraReports.UI.XRLabel();
            this.LoggedUserNme = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblYearHeader = new DevExpress.XtraReports.UI.XRLabel();
            this.srptPayslipAttendenceSummaryAr = new DevExpress.XtraReports.UI.XRSubreport();
            this.srptPayslipIndemnityDaysSummaryAr = new DevExpress.XtraReports.UI.XRSubreport();
            this.Subordinates = new DevExpress.XtraReports.Parameters.Parameter();
            this.srptPayslipWorkingDaysSummaryAr = new DevExpress.XtraReports.UI.XRSubreport();
            this.xrLabel31 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.srptPaySlipEarningAr = new DevExpress.XtraReports.UI.XRSubreport();
            this.srptPaySlipDeductionAr = new DevExpress.XtraReports.UI.XRSubreport();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblMontheader = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel10 = new DevExpress.XtraReports.UI.XRLabel();
            this.EmployeeCode = new DevExpress.XtraReports.Parameters.Parameter();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.Ar_MainDataStyle = new DevExpress.XtraReports.UI.XRControlStyle();
            this.Ar_DataStyle = new DevExpress.XtraReports.UI.XRControlStyle();
            this.CalcInPeriodMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.calcInPeriodYear = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalForPeriodMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalForPeriodYear = new DevExpress.XtraReports.UI.CalculatedField();
            this.PeriodMonth = new DevExpress.XtraReports.Parameters.Parameter();
            this.PeriodYear = new DevExpress.XtraReports.Parameters.Parameter();
            this.calcArMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcNetPay = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcDateTime = new DevExpress.XtraReports.UI.CalculatedField();
            this.xrTable1 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow1 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell1 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTableCell2 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell3 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel8 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTableCell4 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow2 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell5 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel11 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTableCell6 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell7 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel13 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTableCell8 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow3 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell9 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel15 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTableCell10 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell11 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel17 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTableCell12 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow4 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell13 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblJoingDateDay2 = new DevExpress.XtraReports.UI.XRLabel();
            this.lbljoiningDateYear2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel21 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblJoiningDateMonthAr2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTableCell14 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell15 = new DevExpress.XtraReports.UI.XRTableCell();
            this.lblJoingDateDay = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel38 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblJoiningDateMonthAr = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel9 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblJoingDateYear = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTableCell16 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow5 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell17 = new DevExpress.XtraReports.UI.XRTableCell();
            this.lblPassportExpiryDateDay = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel23 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblPassportExpiryMonthAr = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel25 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblPassportExpiryDateYear = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTableCell18 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell19 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel29 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTableCell20 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableRow6 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell23 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel27 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrTableCell24 = new DevExpress.XtraReports.UI.XRTableCell();
            this.CalcJoingDateMonthNumber = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcJoiningMonthAr = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcPassportMonthNum = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcPassportMonthAr = new DevExpress.XtraReports.UI.CalculatedField();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // sqlDataSource1
            // 
            this.sqlDataSource1.ConnectionName = HCMv3.Reports.HcmReportBase.DesignTimeConnectionName;
            this.sqlDataSource1.Name = "sqlDataSource1";
            customSqlQuery1.Name = "Query";
            customSqlQuery1.Sql = PayslipVariantSql.SubordPayslipArQuery;
            customSqlQuery2.Name = "QueryLogo";
            // Was the raw legacy string "select logo from OrganizationUnit where OrgUnitType = 1",
            // which throws Invalid object name 'OrganizationUnit' against the HCMv3 schema. Uses the
            // ported query, which keeps the column shape and the @TenantId predicate.
            customSqlQuery2.Sql = HCMv3.Reports.Payroll.PayslipVariantSql.LogoQuery;
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1,
            customSqlQuery2});
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable1,
            this.xrPictureBox1,
            this.xrLabel32,
            this.LoggedUserNme,
            this.xrLabel2,
            this.lblYearHeader,
            this.srptPayslipAttendenceSummaryAr,
            this.srptPayslipIndemnityDaysSummaryAr,
            this.srptPayslipWorkingDaysSummaryAr,
            this.xrLabel31,
            this.xrLabel4,
            this.srptPaySlipEarningAr,
            this.srptPaySlipDeductionAr,
            this.xrLabel1,
            this.lblMontheader,
            this.xrLabel10});
            this.Detail.HeightF = 1017.255F;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.Detail.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.rptSubordPayslipAr_BeforePrint);
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Image", null, "QueryLogo.logo")});
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(352.9307F, 0F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(106.9444F, 75.41666F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            // 
            // xrLabel32
            // 
            this.xrLabel32.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel32.LocationFloat = new DevExpress.Utils.PointFloat(181.6742F, 943.2825F);
            this.xrLabel32.Name = "xrLabel32";
            this.xrLabel32.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel32.SizeF = new System.Drawing.SizeF(91.53255F, 23F);
            this.xrLabel32.StylePriority.UseFont = false;
            this.xrLabel32.Text = ": المستخدم";
            // 
            // LoggedUserNme
            // 
            this.LoggedUserNme.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LoggedUserNme.LocationFloat = new DevExpress.Utils.PointFloat(13F, 943.2825F);
            this.LoggedUserNme.Name = "LoggedUserNme";
            this.LoggedUserNme.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.LoggedUserNme.SizeF = new System.Drawing.SizeF(168.6742F, 23F);
            this.LoggedUserNme.StylePriority.UseFont = false;
            this.LoggedUserNme.StylePriority.UseTextAlignment = false;
            this.LoggedUserNme.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel2
            // 
            this.xrLabel2.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(715.1991F, 954.7407F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(100F, 23F);
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = ": ملاحظات";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblYearHeader
            // 
            this.lblYearHeader.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.CalForPeriodYear")});
            this.lblYearHeader.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblYearHeader.ForeColor = System.Drawing.Color.White;
            this.lblYearHeader.LocationFloat = new DevExpress.Utils.PointFloat(317.2214F, 84.07411F);
            this.lblYearHeader.Name = "lblYearHeader";
            this.lblYearHeader.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblYearHeader.SizeF = new System.Drawing.SizeF(63.487F, 23F);
            this.lblYearHeader.StylePriority.UseFont = false;
            this.lblYearHeader.StylePriority.UseForeColor = false;
            this.lblYearHeader.StylePriority.UseTextAlignment = false;
            this.lblYearHeader.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // srptPayslipAttendenceSummaryAr
            // 
            this.srptPayslipAttendenceSummaryAr.LocationFloat = new DevExpress.Utils.PointFloat(569.3431F, 638.9074F);
            this.srptPayslipAttendenceSummaryAr.Name = "srptPayslipAttendenceSummaryAr";
            this.srptPayslipAttendenceSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("empcode", null, "Query.SubordinateCode"));
            this.srptPayslipAttendenceSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "Query.InPeriod"));
            this.srptPayslipAttendenceSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "Query.ForPeriod"));
            this.srptPayslipAttendenceSummaryAr.ReportSource = new HCMv3.Reports.Payroll.srptPayslipAttendenceSummaryAr();
            this.srptPayslipAttendenceSummaryAr.SizeF = new System.Drawing.SizeF(260.6569F, 140.4999F);
            // 
            // srptPayslipIndemnityDaysSummaryAr
            // 
            this.srptPayslipIndemnityDaysSummaryAr.LocationFloat = new DevExpress.Utils.PointFloat(296.1744F, 638.9074F);
            this.srptPayslipIndemnityDaysSummaryAr.Name = "srptPayslipIndemnityDaysSummaryAr";
            this.srptPayslipIndemnityDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("EmpCode", this.Subordinates));
            this.srptPayslipIndemnityDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "Query.InPeriod"));
            this.srptPayslipIndemnityDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "Query.ForPeriod"));
            this.srptPayslipIndemnityDaysSummaryAr.ReportSource = new HCMv3.Reports.Payroll.srptPayslipIndemnityDaysSummaryAr();
            this.srptPayslipIndemnityDaysSummaryAr.SizeF = new System.Drawing.SizeF(256.905F, 140.4999F);
            // 
            // Subordinates
            // 
            this.Subordinates.Description = "Subordinates";
            dynamicListLookUpSettings1.DataAdapter = null;
            dynamicListLookUpSettings1.DataMember = "Query";
            dynamicListLookUpSettings1.DataSource = this.sqlDataSource1;
            dynamicListLookUpSettings1.DisplayMember = "subordinate";
            dynamicListLookUpSettings1.FilterString = "[manager Code] = ?EmployeeCode Or [manager Code] < 0";
            dynamicListLookUpSettings1.ValueMember = "SubordinateOid";
            this.Subordinates.LookUpSettings = dynamicListLookUpSettings1;
            this.Subordinates.Name = "Subordinates";
            this.Subordinates.Type = typeof(int);
            this.Subordinates.ValueInfo = "0";
            // 
            // srptPayslipWorkingDaysSummaryAr
            // 
            this.srptPayslipWorkingDaysSummaryAr.LocationFloat = new DevExpress.Utils.PointFloat(25.19435F, 638.9074F);
            this.srptPayslipWorkingDaysSummaryAr.Name = "srptPayslipWorkingDaysSummaryAr";
            this.srptPayslipWorkingDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("empcode", null, "Query.SubordinateCode"));
            this.srptPayslipWorkingDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "Query.InPeriod"));
            this.srptPayslipWorkingDaysSummaryAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "Query.ForPeriod"));
            this.srptPayslipWorkingDaysSummaryAr.ReportSource = new HCMv3.Reports.Payroll.srptPayslipWorkingDaysSummaryAr();
            this.srptPayslipWorkingDaysSummaryAr.SizeF = new System.Drawing.SizeF(248.0124F, 140.4999F);
            // 
            // xrLabel31
            // 
            this.xrLabel31.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel31.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel31.BorderWidth = 2F;
            this.xrLabel31.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel31.LocationFloat = new DevExpress.Utils.PointFloat(111.6393F, 592.8241F);
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
            this.xrLabel4.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.NetPay", "{0:0.000}")});
            this.xrLabel4.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(25.19435F, 592.8241F);
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
            // 
            // srptPaySlipEarningAr
            // 
            this.srptPaySlipEarningAr.LocationFloat = new DevExpress.Utils.PointFloat(428.4334F, 283.4491F);
            this.srptPaySlipEarningAr.Name = "srptPaySlipEarningAr";
            this.srptPaySlipEarningAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("EmployeeCode", null, "Query.SubordinateCode"));
            this.srptPaySlipEarningAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "Query.InPeriod"));
            this.srptPaySlipEarningAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "Query.ForPeriod"));
            this.srptPaySlipEarningAr.ReportSource = new HCMv3.Reports.Payroll.srptPaySlipEarningAr();
            this.srptPaySlipEarningAr.SizeF = new System.Drawing.SizeF(386.7657F, 292.7916F);
            // 
            // srptPaySlipDeductionAr
            // 
            this.srptPaySlipDeductionAr.LocationFloat = new DevExpress.Utils.PointFloat(25.19435F, 283.4492F);
            this.srptPaySlipDeductionAr.Name = "srptPaySlipDeductionAr";
            this.srptPaySlipDeductionAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("EmployeeCode", null, "Query.SubordinateCode"));
            this.srptPaySlipDeductionAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("InPeriod", null, "Query.InPeriod"));
            this.srptPaySlipDeductionAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("ForPeriod", null, "Query.ForPeriod"));
            this.srptPaySlipDeductionAr.ReportSource = new HCMv3.Reports.Payroll.srptPaySlipDeductionAr();
            this.srptPaySlipDeductionAr.SizeF = new System.Drawing.SizeF(372.7854F, 292.7915F);
            // 
            // xrLabel1
            // 
            this.xrLabel1.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel1.ForeColor = System.Drawing.Color.White;
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(441.0703F, 84.07411F);
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(220.9168F, 23F);
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UseForeColor = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.Text = ":قسيمة الدفع للشهر";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // lblMontheader
            // 
            this.lblMontheader.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.calcArMonth")});
            this.lblMontheader.Font = new System.Drawing.Font("Tahoma", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMontheader.ForeColor = System.Drawing.Color.White;
            this.lblMontheader.LocationFloat = new DevExpress.Utils.PointFloat(380.7084F, 84.07411F);
            this.lblMontheader.Name = "lblMontheader";
            this.lblMontheader.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblMontheader.SizeF = new System.Drawing.SizeF(60.36194F, 23F);
            this.lblMontheader.StylePriority.UseFont = false;
            this.lblMontheader.StylePriority.UseForeColor = false;
            this.lblMontheader.StylePriority.UseTextAlignment = false;
            this.lblMontheader.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel10
            // 
            this.xrLabel10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(100)))), ((int)(((byte)(160)))));
            this.xrLabel10.ForeColor = System.Drawing.Color.White;
            this.xrLabel10.LocationFloat = new DevExpress.Utils.PointFloat(10.53567F, 82.07411F);
            this.xrLabel10.Name = "xrLabel10";
            this.xrLabel10.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel10.SizeF = new System.Drawing.SizeF(819.4644F, 29.66463F);
            this.xrLabel10.StylePriority.UseBackColor = false;
            this.xrLabel10.StylePriority.UseForeColor = false;
            this.xrLabel10.StylePriority.UseTextAlignment = false;
            this.xrLabel10.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // EmployeeCode
            // 
            this.EmployeeCode.Description = "Employee Code";
            this.EmployeeCode.Name = "EmployeeCode";
            this.EmployeeCode.Type = typeof(int);
            this.EmployeeCode.ValueInfo = "0";
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
            this.BottomMargin.HeightF = 14.25025F;
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
            this.CalcInPeriodMonth.DataMember = "Query";
            this.CalcInPeriodMonth.Expression = "GetMonth([InPeriod])";
            this.CalcInPeriodMonth.Name = "CalcInPeriodMonth";
            // 
            // calcInPeriodYear
            // 
            this.calcInPeriodYear.DataMember = "Query";
            this.calcInPeriodYear.Expression = "GetYear([InPeriod])";
            this.calcInPeriodYear.Name = "calcInPeriodYear";
            // 
            // CalForPeriodMonth
            // 
            this.CalForPeriodMonth.DataMember = "Query";
            this.CalForPeriodMonth.Expression = "GetMonth([ForPeriod])";
            this.CalForPeriodMonth.Name = "CalForPeriodMonth";
            // 
            // CalForPeriodYear
            // 
            this.CalForPeriodYear.DataMember = "Query";
            this.CalForPeriodYear.Expression = "GetYear([ForPeriod])";
            this.CalForPeriodYear.Name = "CalForPeriodYear";
            // 
            // PeriodMonth
            // 
            this.PeriodMonth.Description = "Period Month";
            this.PeriodMonth.Name = "PeriodMonth";
            this.PeriodMonth.Type = typeof(int);
            this.PeriodMonth.ValueInfo = "1";
            // 
            // PeriodYear
            // 
            this.PeriodYear.Description = "Period Year";
            this.PeriodYear.Name = "PeriodYear";
            this.PeriodYear.Type = typeof(int);
            this.PeriodYear.ValueInfo = "2017";
            // 
            // calcArMonth
            // 
            this.calcArMonth.DataMember = "Query";
            this.calcArMonth.Expression = PayslipArSql.CalcArMonthExpression;
            this.calcArMonth.Name = "calcArMonth";
            // 
            // CalcNetPay
            // 
            this.CalcNetPay.DataMember = "Query";
            this.CalcNetPay.DisplayName = "CalcNetPay";
            this.CalcNetPay.Expression = "Round([Net Pay])";
            this.CalcNetPay.Name = "CalcNetPay";
            // 
            // CalcDateTime
            // 
            this.CalcDateTime.DataMember = "Query";
            this.CalcDateTime.Expression = "LocalDateTimeNow()";
            this.CalcDateTime.Name = "CalcDateTime";
            // 
            // xrTable1
            // 
            this.xrTable1.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable1.LocationFloat = new DevExpress.Utils.PointFloat(13.00003F, 117.0944F);
            this.xrTable1.Name = "xrTable1";
            this.xrTable1.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow1,
            this.xrTableRow2,
            this.xrTableRow3,
            this.xrTableRow4,
            this.xrTableRow5,
            this.xrTableRow6});
            this.xrTable1.SizeF = new System.Drawing.SizeF(817F, 151.7714F);
            this.xrTable1.StylePriority.UseBorders = false;
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
            this.xrTableCell1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel5});
            this.xrTableCell1.Name = "xrTableCell1";
            this.xrTableCell1.Text = "xrTableCell1";
            this.xrTableCell1.Weight = 1.3602901320046572D;
            // 
            // xrLabel5
            // 
            this.xrLabel5.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel5.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel5.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.OrgUnit")});
            this.xrLabel5.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel5.ForeColor = System.Drawing.Color.Black;
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(1.959443F, 0.9896688F);
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.SizeF = new System.Drawing.SizeF(268.386F, 26.7361F);
            this.xrLabel5.StylePriority.UseBorderColor = false;
            this.xrLabel5.StylePriority.UseBorders = false;
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.StylePriority.UseForeColor = false;
            this.xrLabel5.StylePriority.UsePadding = false;
            this.xrLabel5.StylePriority.UseTextAlignment = false;
            this.xrLabel5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrTableCell2
            // 
            this.xrTableCell2.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell2.Name = "xrTableCell2";
            this.xrTableCell2.StylePriority.UseFont = false;
            this.xrTableCell2.StylePriority.UseTextAlignment = false;
            this.xrTableCell2.Text = "وحدة التنظيم    ";
            this.xrTableCell2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell2.Weight = 0.63970986799534291D;
            // 
            // xrTableCell3
            // 
            this.xrTableCell3.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel8});
            this.xrTableCell3.Name = "xrTableCell3";
            this.xrTableCell3.Text = "xrTableCell3";
            this.xrTableCell3.Weight = 1.332755354214902D;
            // 
            // xrLabel8
            // 
            this.xrLabel8.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel8.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel8.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.SubordinateCode")});
            this.xrLabel8.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel8.ForeColor = System.Drawing.Color.Black;
            this.xrLabel8.LocationFloat = new DevExpress.Utils.PointFloat(10.7164F, 0F);
            this.xrLabel8.Name = "xrLabel8";
            this.xrLabel8.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 5, 0, 0, 100F);
            this.xrLabel8.SizeF = new System.Drawing.SizeF(254.1568F, 26.73612F);
            this.xrLabel8.StylePriority.UseBorderColor = false;
            this.xrLabel8.StylePriority.UseBorders = false;
            this.xrLabel8.StylePriority.UseFont = false;
            this.xrLabel8.StylePriority.UseForeColor = false;
            this.xrLabel8.StylePriority.UsePadding = false;
            this.xrLabel8.StylePriority.UseTextAlignment = false;
            this.xrLabel8.Text = "xrTableCell23";
            this.xrLabel8.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrTableCell4
            // 
            this.xrTableCell4.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell4.Name = "xrTableCell4";
            this.xrTableCell4.StylePriority.UseFont = false;
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
            this.xrTableCell5.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel11});
            this.xrTableCell5.Name = "xrTableCell5";
            this.xrTableCell5.Text = "xrTableCell5";
            this.xrTableCell5.Weight = 1.360290132004657D;
            // 
            // xrLabel11
            // 
            this.xrLabel11.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel11.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel11.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.Grade")});
            this.xrLabel11.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel11.ForeColor = System.Drawing.Color.Black;
            this.xrLabel11.LocationFloat = new DevExpress.Utils.PointFloat(8.830337E-06F, 0.9896688F);
            this.xrLabel11.Name = "xrLabel11";
            this.xrLabel11.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel11.SizeF = new System.Drawing.SizeF(268.262F, 26.73611F);
            this.xrLabel11.StylePriority.UseBorderColor = false;
            this.xrLabel11.StylePriority.UseBorders = false;
            this.xrLabel11.StylePriority.UseFont = false;
            this.xrLabel11.StylePriority.UseForeColor = false;
            this.xrLabel11.StylePriority.UsePadding = false;
            this.xrLabel11.StylePriority.UseTextAlignment = false;
            this.xrLabel11.Text = " ";
            this.xrLabel11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrTableCell6
            // 
            this.xrTableCell6.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell6.Name = "xrTableCell6";
            this.xrTableCell6.StylePriority.UseFont = false;
            this.xrTableCell6.StylePriority.UseTextAlignment = false;
            this.xrTableCell6.Text = "درجة";
            this.xrTableCell6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell6.Weight = 0.639709867995343D;
            // 
            // xrTableCell7
            // 
            this.xrTableCell7.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel13});
            this.xrTableCell7.Name = "xrTableCell7";
            this.xrTableCell7.Text = "xrTableCell7";
            this.xrTableCell7.Weight = 1.3327550471059124D;
            // 
            // xrLabel13
            // 
            this.xrLabel13.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel13.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel13.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.subordinate")});
            this.xrLabel13.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel13.ForeColor = System.Drawing.Color.Black;
            this.xrLabel13.LocationFloat = new DevExpress.Utils.PointFloat(10.00002F, 0.9896895F);
            this.xrLabel13.Name = "xrLabel13";
            this.xrLabel13.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 5, 0, 0, 100F);
            this.xrLabel13.SizeF = new System.Drawing.SizeF(254.0072F, 26.73611F);
            this.xrLabel13.StylePriority.UseBorderColor = false;
            this.xrLabel13.StylePriority.UseBorders = false;
            this.xrLabel13.StylePriority.UseFont = false;
            this.xrLabel13.StylePriority.UseForeColor = false;
            this.xrLabel13.StylePriority.UsePadding = false;
            this.xrLabel13.StylePriority.UseTextAlignment = false;
            this.xrLabel13.Text = " ";
            this.xrLabel13.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrTableCell8
            // 
            this.xrTableCell8.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell8.Multiline = true;
            this.xrTableCell8.Name = "xrTableCell8";
            this.xrTableCell8.StylePriority.UseFont = false;
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
            this.xrTableCell9.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel15});
            this.xrTableCell9.Name = "xrTableCell9";
            this.xrTableCell9.Text = "xrTableCell9";
            this.xrTableCell9.Weight = 1.3498071201990802D;
            // 
            // xrLabel15
            // 
            this.xrLabel15.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel15.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel15.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.Jobtitle")});
            this.xrLabel15.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel15.ForeColor = System.Drawing.Color.Black;
            this.xrLabel15.LocationFloat = new DevExpress.Utils.PointFloat(4.959435F, 0F);
            this.xrLabel15.Name = "xrLabel15";
            this.xrLabel15.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel15.SizeF = new System.Drawing.SizeF(263.3026F, 26.73611F);
            this.xrLabel15.StylePriority.UseBorderColor = false;
            this.xrLabel15.StylePriority.UseBorders = false;
            this.xrLabel15.StylePriority.UseFont = false;
            this.xrLabel15.StylePriority.UseForeColor = false;
            this.xrLabel15.StylePriority.UsePadding = false;
            this.xrLabel15.StylePriority.UseTextAlignment = false;
            this.xrLabel15.Text = " ";
            this.xrLabel15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrTableCell10
            // 
            this.xrTableCell10.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell10.Name = "xrTableCell10";
            this.xrTableCell10.StylePriority.UseFont = false;
            this.xrTableCell10.StylePriority.UseTextAlignment = false;
            this.xrTableCell10.Text = "التعيين";
            this.xrTableCell10.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell10.Weight = 0.65019287980091967D;
            // 
            // xrTableCell11
            // 
            this.xrTableCell11.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel17});
            this.xrTableCell11.Name = "xrTableCell11";
            this.xrTableCell11.Text = "xrTableCell11";
            this.xrTableCell11.Weight = 1.3327550471059122D;
            // 
            // xrLabel17
            // 
            this.xrLabel17.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel17.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel17.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.SalaryProfile")});
            this.xrLabel17.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel17.ForeColor = System.Drawing.Color.Black;
            this.xrLabel17.LocationFloat = new DevExpress.Utils.PointFloat(10.7164F, 0.9896895F);
            this.xrLabel17.Name = "xrLabel17";
            this.xrLabel17.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 5, 0, 0, 100F);
            this.xrLabel17.SizeF = new System.Drawing.SizeF(254.1568F, 26.73611F);
            this.xrLabel17.StylePriority.UseBorderColor = false;
            this.xrLabel17.StylePriority.UseBorders = false;
            this.xrLabel17.StylePriority.UseFont = false;
            this.xrLabel17.StylePriority.UseForeColor = false;
            this.xrLabel17.StylePriority.UsePadding = false;
            this.xrLabel17.StylePriority.UseTextAlignment = false;
            this.xrLabel17.Text = " ";
            this.xrLabel17.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrTableCell12
            // 
            this.xrTableCell12.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell12.Multiline = true;
            this.xrTableCell12.Name = "xrTableCell12";
            this.xrTableCell12.StylePriority.UseFont = false;
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
            this.xrTableCell13.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel6,
            this.lblJoingDateDay2,
            this.lbljoiningDateYear2,
            this.xrLabel21,
            this.lblJoiningDateMonthAr2});
            this.xrTableCell13.Name = "xrTableCell13";
            this.xrTableCell13.Text = "xrTableCell13";
            this.xrTableCell13.Weight = 1.3498071201990802D;
            // 
            // xrLabel6
            // 
            this.xrLabel6.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel6.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel6.ForeColor = System.Drawing.Color.Black;
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(161.4675F, 0F);
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(9.999954F, 26.73615F);
            this.xrLabel6.StylePriority.UseBorderColor = false;
            this.xrLabel6.StylePriority.UseBorders = false;
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UseForeColor = false;
            this.xrLabel6.StylePriority.UsePadding = false;
            this.xrLabel6.StylePriority.UseTextAlignment = false;
            this.xrLabel6.Text = "/";
            this.xrLabel6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel6.TextFormatString = "{0:yyyy}";
            // 
            // lblJoingDateDay2
            // 
            this.lblJoingDateDay2.BorderColor = System.Drawing.Color.DarkGray;
            this.lblJoingDateDay2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblJoingDateDay2.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.JoiningDate")});
            this.lblJoingDateDay2.ForeColor = System.Drawing.Color.Black;
            this.lblJoingDateDay2.LocationFloat = new DevExpress.Utils.PointFloat(244.692F, 0F);
            this.lblJoingDateDay2.Name = "lblJoingDateDay2";
            this.lblJoingDateDay2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblJoingDateDay2.SizeF = new System.Drawing.SizeF(23.57F, 26.7F);
            this.lblJoingDateDay2.StylePriority.UseBorderColor = false;
            this.lblJoingDateDay2.StylePriority.UseBorders = false;
            this.lblJoingDateDay2.StylePriority.UseFont = false;
            this.lblJoingDateDay2.StylePriority.UseForeColor = false;
            this.lblJoingDateDay2.StylePriority.UsePadding = false;
            this.lblJoingDateDay2.StylePriority.UseTextAlignment = false;
            this.lblJoingDateDay2.Text = " ";
            this.lblJoingDateDay2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lblJoingDateDay2.TextFormatString = "{0:dd}";
            // 
            // lbljoiningDateYear2
            // 
            this.lbljoiningDateYear2.BorderColor = System.Drawing.Color.DarkGray;
            this.lbljoiningDateYear2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lbljoiningDateYear2.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.JoiningDate")});
            this.lbljoiningDateYear2.ForeColor = System.Drawing.Color.Black;
            this.lbljoiningDateYear2.LocationFloat = new DevExpress.Utils.PointFloat(4.961144F, 0F);
            this.lbljoiningDateYear2.Name = "lbljoiningDateYear2";
            this.lbljoiningDateYear2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lbljoiningDateYear2.SizeF = new System.Drawing.SizeF(156.5064F, 26.30609F);
            this.lbljoiningDateYear2.StylePriority.UseBorderColor = false;
            this.lbljoiningDateYear2.StylePriority.UseBorders = false;
            this.lbljoiningDateYear2.StylePriority.UseFont = false;
            this.lbljoiningDateYear2.StylePriority.UseForeColor = false;
            this.lbljoiningDateYear2.StylePriority.UsePadding = false;
            this.lbljoiningDateYear2.StylePriority.UseTextAlignment = false;
            this.lbljoiningDateYear2.Text = " ";
            this.lbljoiningDateYear2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lbljoiningDateYear2.TextFormatString = "{0:yyyy}";
            // 
            // xrLabel21
            // 
            this.xrLabel21.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel21.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel21.ForeColor = System.Drawing.Color.Black;
            this.xrLabel21.LocationFloat = new DevExpress.Utils.PointFloat(234.692F, 0F);
            this.xrLabel21.Name = "xrLabel21";
            this.xrLabel21.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel21.SizeF = new System.Drawing.SizeF(9.999969F, 26.73613F);
            this.xrLabel21.StylePriority.UseBorderColor = false;
            this.xrLabel21.StylePriority.UseBorders = false;
            this.xrLabel21.StylePriority.UseFont = false;
            this.xrLabel21.StylePriority.UseForeColor = false;
            this.xrLabel21.StylePriority.UsePadding = false;
            this.xrLabel21.StylePriority.UseTextAlignment = false;
            this.xrLabel21.Text = "/";
            this.xrLabel21.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel21.TextFormatString = "{0:yyyy}";
            // 
            // lblJoiningDateMonthAr2
            // 
            this.lblJoiningDateMonthAr2.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblJoiningDateMonthAr2.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.CalcJoiningMonthAr", "{0:dd/MM/yyyy}")});
            this.lblJoiningDateMonthAr2.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblJoiningDateMonthAr2.LocationFloat = new DevExpress.Utils.PointFloat(171.4677F, 0F);
            this.lblJoiningDateMonthAr2.Name = "lblJoiningDateMonthAr2";
            this.lblJoiningDateMonthAr2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblJoiningDateMonthAr2.SizeF = new System.Drawing.SizeF(63.22437F, 26.73608F);
            this.lblJoiningDateMonthAr2.StylePriority.UseBorders = false;
            this.lblJoiningDateMonthAr2.StylePriority.UseFont = false;
            this.lblJoiningDateMonthAr2.StylePriority.UseTextAlignment = false;
            this.lblJoiningDateMonthAr2.Text = "lblDate";
            this.lblJoiningDateMonthAr2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            // 
            // xrTableCell14
            // 
            this.xrTableCell14.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell14.Multiline = true;
            this.xrTableCell14.Name = "xrTableCell14";
            this.xrTableCell14.StylePriority.UseFont = false;
            this.xrTableCell14.StylePriority.UseTextAlignment = false;
            this.xrTableCell14.Text = "التعويض التسجيل\r\n";
            this.xrTableCell14.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell14.Weight = 0.65019287980091967D;
            // 
            // xrTableCell15
            // 
            this.xrTableCell15.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.lblJoingDateDay,
            this.xrLabel38,
            this.lblJoiningDateMonthAr,
            this.xrLabel9,
            this.lblJoingDateYear});
            this.xrTableCell15.Name = "xrTableCell15";
            this.xrTableCell15.Text = "xrTableCell15";
            this.xrTableCell15.Weight = 1.3327550471059124D;
            // 
            // lblJoingDateDay
            // 
            this.lblJoingDateDay.BorderColor = System.Drawing.Color.DarkGray;
            this.lblJoingDateDay.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblJoingDateDay.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.JoiningDate")});
            this.lblJoingDateDay.ForeColor = System.Drawing.Color.Black;
            this.lblJoingDateDay.LocationFloat = new DevExpress.Utils.PointFloat(239.7309F, 1.41971F);
            this.lblJoingDateDay.Name = "lblJoingDateDay";
            this.lblJoingDateDay.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblJoingDateDay.SizeF = new System.Drawing.SizeF(24.27637F, 26.30611F);
            this.lblJoingDateDay.StylePriority.UseBorderColor = false;
            this.lblJoingDateDay.StylePriority.UseBorders = false;
            this.lblJoingDateDay.StylePriority.UseFont = false;
            this.lblJoingDateDay.StylePriority.UseForeColor = false;
            this.lblJoingDateDay.StylePriority.UsePadding = false;
            this.lblJoingDateDay.StylePriority.UseTextAlignment = false;
            this.lblJoingDateDay.Text = " ";
            this.lblJoingDateDay.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lblJoingDateDay.TextFormatString = "{0:dd}";
            // 
            // xrLabel38
            // 
            this.xrLabel38.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel38.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel38.ForeColor = System.Drawing.Color.Black;
            this.xrLabel38.LocationFloat = new DevExpress.Utils.PointFloat(229.731F, 1.41971F);
            this.xrLabel38.Name = "xrLabel38";
            this.xrLabel38.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel38.SizeF = new System.Drawing.SizeF(10F, 26.73613F);
            this.xrLabel38.StylePriority.UseBorderColor = false;
            this.xrLabel38.StylePriority.UseBorders = false;
            this.xrLabel38.StylePriority.UseFont = false;
            this.xrLabel38.StylePriority.UseForeColor = false;
            this.xrLabel38.StylePriority.UsePadding = false;
            this.xrLabel38.StylePriority.UseTextAlignment = false;
            this.xrLabel38.Text = "/";
            this.xrLabel38.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel38.TextFormatString = "{0:yyyy}";
            // 
            // lblJoiningDateMonthAr
            // 
            this.lblJoiningDateMonthAr.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblJoiningDateMonthAr.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.CalcJoiningMonthAr", "{0:dd/MM/yyyy}")});
            this.lblJoiningDateMonthAr.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblJoiningDateMonthAr.LocationFloat = new DevExpress.Utils.PointFloat(166.5066F, 1.41971F);
            this.lblJoiningDateMonthAr.Name = "lblJoiningDateMonthAr";
            this.lblJoiningDateMonthAr.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblJoiningDateMonthAr.SizeF = new System.Drawing.SizeF(63.22443F, 26.73608F);
            this.lblJoiningDateMonthAr.StylePriority.UseBorders = false;
            this.lblJoiningDateMonthAr.StylePriority.UseFont = false;
            this.lblJoiningDateMonthAr.StylePriority.UseTextAlignment = false;
            this.lblJoiningDateMonthAr.Text = "lblDate";
            this.lblJoiningDateMonthAr.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            // 
            // xrLabel9
            // 
            this.xrLabel9.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel9.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel9.ForeColor = System.Drawing.Color.Black;
            this.xrLabel9.LocationFloat = new DevExpress.Utils.PointFloat(156.5067F, 1.41971F);
            this.xrLabel9.Name = "xrLabel9";
            this.xrLabel9.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel9.SizeF = new System.Drawing.SizeF(9.999939F, 26.73616F);
            this.xrLabel9.StylePriority.UseBorderColor = false;
            this.xrLabel9.StylePriority.UseBorders = false;
            this.xrLabel9.StylePriority.UseFont = false;
            this.xrLabel9.StylePriority.UseForeColor = false;
            this.xrLabel9.StylePriority.UsePadding = false;
            this.xrLabel9.StylePriority.UseTextAlignment = false;
            this.xrLabel9.Text = "/";
            this.xrLabel9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel9.TextFormatString = "{0:yyyy}";
            // 
            // lblJoingDateYear
            // 
            this.lblJoingDateYear.BorderColor = System.Drawing.Color.DarkGray;
            this.lblJoingDateYear.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblJoingDateYear.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.JoiningDate")});
            this.lblJoingDateYear.ForeColor = System.Drawing.Color.Black;
            this.lblJoingDateYear.LocationFloat = new DevExpress.Utils.PointFloat(10.00002F, 1.419705F);
            this.lblJoingDateYear.Name = "lblJoingDateYear";
            this.lblJoingDateYear.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblJoingDateYear.SizeF = new System.Drawing.SizeF(146.5065F, 26.3061F);
            this.lblJoingDateYear.StylePriority.UseBorderColor = false;
            this.lblJoingDateYear.StylePriority.UseBorders = false;
            this.lblJoingDateYear.StylePriority.UseFont = false;
            this.lblJoingDateYear.StylePriority.UseForeColor = false;
            this.lblJoingDateYear.StylePriority.UsePadding = false;
            this.lblJoingDateYear.StylePriority.UseTextAlignment = false;
            this.lblJoingDateYear.Text = " ";
            this.lblJoingDateYear.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lblJoingDateYear.TextFormatString = "{0:yyyy}";
            // 
            // xrTableCell16
            // 
            this.xrTableCell16.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell16.Multiline = true;
            this.xrTableCell16.Name = "xrTableCell16";
            this.xrTableCell16.StylePriority.UseFont = false;
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
            this.xrTableCell17.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.lblPassportExpiryDateDay,
            this.xrLabel23,
            this.lblPassportExpiryMonthAr,
            this.xrLabel25,
            this.lblPassportExpiryDateYear});
            this.xrTableCell17.Name = "xrTableCell17";
            this.xrTableCell17.Text = "xrTableCell17";
            this.xrTableCell17.Weight = 1.3498071201990802D;
            // 
            // lblPassportExpiryDateDay
            // 
            this.lblPassportExpiryDateDay.BorderColor = System.Drawing.Color.DarkGray;
            this.lblPassportExpiryDateDay.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblPassportExpiryDateDay.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.PassportExpiryDate")});
            this.lblPassportExpiryDateDay.ForeColor = System.Drawing.Color.Black;
            this.lblPassportExpiryDateDay.LocationFloat = new DevExpress.Utils.PointFloat(244.692F, 0.989731F);
            this.lblPassportExpiryDateDay.Name = "lblPassportExpiryDateDay";
            this.lblPassportExpiryDateDay.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblPassportExpiryDateDay.SizeF = new System.Drawing.SizeF(22.01F, 23F);
            this.lblPassportExpiryDateDay.StylePriority.UseBorderColor = false;
            this.lblPassportExpiryDateDay.StylePriority.UseBorders = false;
            this.lblPassportExpiryDateDay.StylePriority.UseFont = false;
            this.lblPassportExpiryDateDay.StylePriority.UseForeColor = false;
            this.lblPassportExpiryDateDay.StylePriority.UsePadding = false;
            this.lblPassportExpiryDateDay.StylePriority.UseTextAlignment = false;
            this.lblPassportExpiryDateDay.Text = " ";
            this.lblPassportExpiryDateDay.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lblPassportExpiryDateDay.TextFormatString = "{0:dd}";
            // 
            // xrLabel23
            // 
            this.xrLabel23.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel23.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel23.ForeColor = System.Drawing.Color.Black;
            this.xrLabel23.LocationFloat = new DevExpress.Utils.PointFloat(160.2391F, 0F);
            this.xrLabel23.Name = "xrLabel23";
            this.xrLabel23.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel23.SizeF = new System.Drawing.SizeF(9.999939F, 26.73613F);
            this.xrLabel23.StylePriority.UseBorderColor = false;
            this.xrLabel23.StylePriority.UseBorders = false;
            this.xrLabel23.StylePriority.UseFont = false;
            this.xrLabel23.StylePriority.UseForeColor = false;
            this.xrLabel23.StylePriority.UsePadding = false;
            this.xrLabel23.StylePriority.UseTextAlignment = false;
            this.xrLabel23.Text = "/";
            this.xrLabel23.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel23.TextFormatString = "{0:yyyy}";
            // 
            // lblPassportExpiryMonthAr
            // 
            this.lblPassportExpiryMonthAr.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblPassportExpiryMonthAr.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.CalcPassportMonthAr", "{0:dd/MM/yyyy}")});
            this.lblPassportExpiryMonthAr.Font = new System.Drawing.Font("Tahoma", 12F);
            this.lblPassportExpiryMonthAr.LocationFloat = new DevExpress.Utils.PointFloat(170.2392F, 0F);
            this.lblPassportExpiryMonthAr.Name = "lblPassportExpiryMonthAr";
            this.lblPassportExpiryMonthAr.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblPassportExpiryMonthAr.SizeF = new System.Drawing.SizeF(63.2244F, 26.73607F);
            this.lblPassportExpiryMonthAr.StylePriority.UseBorders = false;
            this.lblPassportExpiryMonthAr.StylePriority.UseFont = false;
            this.lblPassportExpiryMonthAr.StylePriority.UseTextAlignment = false;
            this.lblPassportExpiryMonthAr.Text = "lblDate";
            this.lblPassportExpiryMonthAr.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            // 
            // xrLabel25
            // 
            this.xrLabel25.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel25.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel25.ForeColor = System.Drawing.Color.Black;
            this.xrLabel25.LocationFloat = new DevExpress.Utils.PointFloat(233.4636F, 0F);
            this.xrLabel25.Name = "xrLabel25";
            this.xrLabel25.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel25.SizeF = new System.Drawing.SizeF(9.999969F, 26.73615F);
            this.xrLabel25.StylePriority.UseBorderColor = false;
            this.xrLabel25.StylePriority.UseBorders = false;
            this.xrLabel25.StylePriority.UseFont = false;
            this.xrLabel25.StylePriority.UseForeColor = false;
            this.xrLabel25.StylePriority.UsePadding = false;
            this.xrLabel25.StylePriority.UseTextAlignment = false;
            this.xrLabel25.Text = "/";
            this.xrLabel25.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel25.TextFormatString = "{0:yyyy}";
            // 
            // lblPassportExpiryDateYear
            // 
            this.lblPassportExpiryDateYear.BorderColor = System.Drawing.Color.DarkGray;
            this.lblPassportExpiryDateYear.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblPassportExpiryDateYear.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.PassportExpiryDate")});
            this.lblPassportExpiryDateYear.ForeColor = System.Drawing.Color.Black;
            this.lblPassportExpiryDateYear.LocationFloat = new DevExpress.Utils.PointFloat(3.732641F, 0F);
            this.lblPassportExpiryDateYear.Name = "lblPassportExpiryDateYear";
            this.lblPassportExpiryDateYear.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblPassportExpiryDateYear.SizeF = new System.Drawing.SizeF(156.5064F, 26.30609F);
            this.lblPassportExpiryDateYear.StylePriority.UseBorderColor = false;
            this.lblPassportExpiryDateYear.StylePriority.UseBorders = false;
            this.lblPassportExpiryDateYear.StylePriority.UseFont = false;
            this.lblPassportExpiryDateYear.StylePriority.UseForeColor = false;
            this.lblPassportExpiryDateYear.StylePriority.UsePadding = false;
            this.lblPassportExpiryDateYear.StylePriority.UseTextAlignment = false;
            this.lblPassportExpiryDateYear.Text = " ";
            this.lblPassportExpiryDateYear.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lblPassportExpiryDateYear.TextFormatString = "{0:yyyy}";
            // 
            // xrTableCell18
            // 
            this.xrTableCell18.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell18.Multiline = true;
            this.xrTableCell18.Name = "xrTableCell18";
            this.xrTableCell18.StylePriority.UseFont = false;
            this.xrTableCell18.StylePriority.UseTextAlignment = false;
            this.xrTableCell18.Text = "انتهاء جواز السفر\r\n";
            this.xrTableCell18.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell18.Weight = 0.65019287980091967D;
            // 
            // xrTableCell19
            // 
            this.xrTableCell19.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel29});
            this.xrTableCell19.Name = "xrTableCell19";
            this.xrTableCell19.Text = "xrTableCell19";
            this.xrTableCell19.Weight = 1.3327550471059124D;
            // 
            // xrLabel29
            // 
            this.xrLabel29.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel29.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel29.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.BankName")});
            this.xrLabel29.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel29.ForeColor = System.Drawing.Color.Black;
            this.xrLabel29.LocationFloat = new DevExpress.Utils.PointFloat(10.7164F, 0.9897305F);
            this.xrLabel29.Name = "xrLabel29";
            this.xrLabel29.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 5, 0, 0, 100F);
            this.xrLabel29.SizeF = new System.Drawing.SizeF(253.2908F, 26.73612F);
            this.xrLabel29.StylePriority.UseBorderColor = false;
            this.xrLabel29.StylePriority.UseBorders = false;
            this.xrLabel29.StylePriority.UseFont = false;
            this.xrLabel29.StylePriority.UseForeColor = false;
            this.xrLabel29.StylePriority.UsePadding = false;
            this.xrLabel29.StylePriority.UseTextAlignment = false;
            this.xrLabel29.Text = " ";
            this.xrLabel29.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrTableCell20
            // 
            this.xrTableCell20.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell20.Multiline = true;
            this.xrTableCell20.Name = "xrTableCell20";
            this.xrTableCell20.StylePriority.UseFont = false;
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
            this.xrTableCell23.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel27});
            this.xrTableCell23.Name = "xrTableCell23";
            this.xrTableCell23.Text = "xrTableCell23";
            this.xrTableCell23.Weight = 3.3327550471059126D;
            // 
            // xrLabel27
            // 
            this.xrLabel27.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel27.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel27.DataBindings.AddRange(new DevExpress.XtraReports.UI.XRBinding[] {
            new DevExpress.XtraReports.UI.XRBinding("Text", null, "Query.BankIBAN")});
            this.xrLabel27.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel27.ForeColor = System.Drawing.Color.Black;
            this.xrLabel27.LocationFloat = new DevExpress.Utils.PointFloat(33.07893F, 0F);
            this.xrLabel27.Name = "xrLabel27";
            this.xrLabel27.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel27.SizeF = new System.Drawing.SizeF(639.4283F, 27.9603F);
            this.xrLabel27.StylePriority.UseBorderColor = false;
            this.xrLabel27.StylePriority.UseBorders = false;
            this.xrLabel27.StylePriority.UseFont = false;
            this.xrLabel27.StylePriority.UseForeColor = false;
            this.xrLabel27.StylePriority.UsePadding = false;
            this.xrLabel27.StylePriority.UseTextAlignment = false;
            this.xrLabel27.Text = " ";
            this.xrLabel27.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrTableCell24
            // 
            this.xrTableCell24.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrTableCell24.Name = "xrTableCell24";
            this.xrTableCell24.StylePriority.UseFont = false;
            this.xrTableCell24.StylePriority.UseTextAlignment = false;
            this.xrTableCell24.Text = "حساب البنك";
            this.xrTableCell24.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell24.Weight = 0.66724495289408758D;
            // 
            // CalcJoingDateMonthNumber
            // 
            this.CalcJoingDateMonthNumber.DataMember = "Query";
            this.CalcJoingDateMonthNumber.Expression = "GetMonth([JoiningDate])";
            this.CalcJoingDateMonthNumber.Name = "CalcJoingDateMonthNumber";
            // 
            // CalcJoiningMonthAr
            // 
            this.CalcJoiningMonthAr.DataMember = "Query";
            this.CalcJoiningMonthAr.Expression = PayslipArSql.CalcJoiningDateMonthExpression;
            this.CalcJoiningMonthAr.Name = "CalcJoiningMonthAr";
            // 
            // CalcPassportMonthNum
            // 
            this.CalcPassportMonthNum.DataMember = "Query";
            this.CalcPassportMonthNum.Expression = "GetMonth([PassportExpiryDate])";
            this.CalcPassportMonthNum.Name = "CalcPassportMonthNum";
            // 
            // CalcPassportMonthAr
            // 
            this.CalcPassportMonthAr.DataMember = "Query";
            this.CalcPassportMonthAr.Expression = PayslipArSql.CalcPassportMonthArExpression;
            this.CalcPassportMonthAr.Name = "CalcPassportMonthAr";
            // 
            // rptSubordPayslipAr
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.CalcInPeriodMonth,
            this.calcInPeriodYear,
            this.CalForPeriodMonth,
            this.CalForPeriodYear,
            this.calcArMonth,
            this.CalcNetPay,
            this.CalcDateTime,
            this.CalcJoingDateMonthNumber,
            this.CalcJoiningMonthAr,
            this.CalcPassportMonthNum,
            this.CalcPassportMonthAr});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
            this.DataMember = "Query";
            this.DataSource = this.sqlDataSource1;
            this.FilterString = PayslipVariantSql.SubordPayslipArFilter;
            this.Margins = new System.Drawing.Printing.Margins(5, 5, 0, 14);
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.EmployeeCode,
            this.Subordinates,
            this.PeriodMonth,
            this.PeriodYear});
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
        private DevExpress.XtraReports.Parameters.Parameter EmployeeCode;
        private DevExpress.XtraReports.UI.XRLabel xrLabel10;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        private DevExpress.XtraReports.UI.XRLabel lblMontheader;
        private DevExpress.DataAccess.Sql.SqlDataSource sqlDataSource1;
        private DevExpress.XtraReports.UI.CalculatedField CalcInPeriodMonth;
        private DevExpress.XtraReports.UI.CalculatedField calcInPeriodYear;
        private DevExpress.XtraReports.UI.CalculatedField CalForPeriodMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalForPeriodYear;
        private DevExpress.XtraReports.Parameters.Parameter PeriodMonth;
        private DevExpress.XtraReports.Parameters.Parameter PeriodYear;
        private DevExpress.XtraReports.UI.XRSubreport srptPaySlipEarningAr;
        private DevExpress.XtraReports.UI.XRSubreport srptPaySlipDeductionAr;
        private DevExpress.XtraReports.UI.XRSubreport srptPayslipAttendenceSummaryAr;
        private DevExpress.XtraReports.UI.XRSubreport srptPayslipIndemnityDaysSummaryAr;
        private DevExpress.XtraReports.UI.XRSubreport srptPayslipWorkingDaysSummaryAr;
        private DevExpress.XtraReports.UI.XRLabel xrLabel31;
        private DevExpress.XtraReports.UI.XRLabel xrLabel4;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.XRLabel lblYearHeader;
        private DevExpress.XtraReports.UI.CalculatedField calcArMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcNetPay;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel32;
        private DevExpress.XtraReports.UI.XRLabel LoggedUserNme;
        private DevExpress.XtraReports.UI.CalculatedField CalcDateTime;
        private DevExpress.XtraReports.Parameters.Parameter Subordinates;
        private DevExpress.XtraReports.UI.XRTable xrTable1;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow1;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel5;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell2;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell3;
        private DevExpress.XtraReports.UI.XRLabel xrLabel8;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell4;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow2;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell5;
        private DevExpress.XtraReports.UI.XRLabel xrLabel11;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell6;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell7;
        private DevExpress.XtraReports.UI.XRLabel xrLabel13;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell8;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow3;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell9;
        private DevExpress.XtraReports.UI.XRLabel xrLabel15;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell10;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell11;
        private DevExpress.XtraReports.UI.XRLabel xrLabel17;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell12;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow4;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell13;
        private DevExpress.XtraReports.UI.XRLabel xrLabel6;
        private DevExpress.XtraReports.UI.XRLabel lblJoingDateDay2;
        private DevExpress.XtraReports.UI.XRLabel lbljoiningDateYear2;
        private DevExpress.XtraReports.UI.XRLabel xrLabel21;
        private DevExpress.XtraReports.UI.XRLabel lblJoiningDateMonthAr2;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell14;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell15;
        private DevExpress.XtraReports.UI.XRLabel lblJoingDateDay;
        private DevExpress.XtraReports.UI.XRLabel xrLabel38;
        private DevExpress.XtraReports.UI.XRLabel lblJoiningDateMonthAr;
        private DevExpress.XtraReports.UI.XRLabel xrLabel9;
        private DevExpress.XtraReports.UI.XRLabel lblJoingDateYear;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell16;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow5;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell17;
        private DevExpress.XtraReports.UI.XRLabel lblPassportExpiryDateDay;
        private DevExpress.XtraReports.UI.XRLabel xrLabel23;
        private DevExpress.XtraReports.UI.XRLabel lblPassportExpiryMonthAr;
        private DevExpress.XtraReports.UI.XRLabel xrLabel25;
        private DevExpress.XtraReports.UI.XRLabel lblPassportExpiryDateYear;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell18;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell19;
        private DevExpress.XtraReports.UI.XRLabel xrLabel29;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell20;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow6;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell23;
        private DevExpress.XtraReports.UI.XRLabel xrLabel27;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell24;
        private DevExpress.XtraReports.UI.CalculatedField CalcJoingDateMonthNumber;
        private DevExpress.XtraReports.UI.CalculatedField CalcJoiningMonthAr;
        private DevExpress.XtraReports.UI.CalculatedField CalcPassportMonthNum;
        private DevExpress.XtraReports.UI.CalculatedField CalcPassportMonthAr;
    }
}
