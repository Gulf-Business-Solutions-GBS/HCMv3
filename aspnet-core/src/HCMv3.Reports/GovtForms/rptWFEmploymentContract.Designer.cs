namespace HCMv3.Reports.GovtForms
{
    partial class rptWFEmploymentContract
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
            DevExpress.DataAccess.ConnectionParameters.CustomStringConnectionParameters customStringConnectionParameters1 = new DevExpress.DataAccess.ConnectionParameters.CustomStringConnectionParameters();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery1 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter1 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter2 = new DevExpress.DataAccess.Sql.QueryParameter();
            this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.lblDateTimeMonthAr = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel15 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblDateTimeYear = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel16 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblDateTimeDay = new DevExpress.XtraReports.UI.XRLabel();
            this.lblResidentPeriod = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel13 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblPassportNum = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel9 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblContractSalary = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel11 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel12 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.PrmDate = new DevExpress.XtraReports.Parameters.Parameter();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.Title = new DevExpress.XtraReports.UI.XRControlStyle();
            this.FieldCaption = new DevExpress.XtraReports.UI.XRControlStyle();
            this.PageInfo = new DevExpress.XtraReports.UI.XRControlStyle();
            this.DataField = new DevExpress.XtraReports.UI.XRControlStyle();
            this.PrmEmpCode = new DevExpress.XtraReports.Parameters.Parameter();
            this.CaclDateTime = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcDateTimeMonthNum = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcDateTimeMonthAr = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcResidencyPeriod = new DevExpress.XtraReports.UI.CalculatedField();
            this.XrStyleBlack = new DevExpress.XtraReports.UI.XRControlStyle();
            this.XrStyleWhite = new DevExpress.XtraReports.UI.XRControlStyle();
            this.PrmCivilId = new DevExpress.XtraReports.Parameters.Parameter();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // sqlDataSource1
            // 
            this.sqlDataSource1.ConnectionName = HCMv3.Reports.HcmReportBase.DesignTimeConnectionName;
            this.sqlDataSource1.Name = "sqlDataSource1";
            customSqlQuery1.Name = "Query";
            queryParameter1.Name = "PrmEmpCode";
            queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmEmpCode]", typeof(string));
            queryParameter2.Name = "PrmCivilId";
            queryParameter2.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter2.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmCivilId]", typeof(string));
            customSqlQuery1.Parameters.Add(queryParameter1);
            customSqlQuery1.Parameters.Add(queryParameter2);
            customSqlQuery1.Sql = EmploymentContractSql.Query;
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1});
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.lblDateTimeMonthAr,
            this.xrLabel15,
            this.lblDateTimeYear,
            this.xrLabel16,
            this.lblDateTimeDay,
            this.lblResidentPeriod,
            this.xrLabel13,
            this.xrLabel1,
            this.xrLabel2,
            this.xrLabel3,
            this.xrLabel4,
            this.xrLabel5,
            this.lblPassportNum,
            this.xrLabel7,
            this.xrLabel9,
            this.lblContractSalary,
            this.xrLabel11,
            this.xrLabel12,
            this.xrPictureBox1});
            this.Detail.HeightF = 1129.819F;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.StyleName = "DataField";
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // lblDateTimeMonthAr
            // 
            this.lblDateTimeMonthAr.CanGrow = false;
            this.lblDateTimeMonthAr.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([CalcDateTimeMonthAr]),\'IsNull\' ,[CalcDateTimeMonthAr] )"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([CalcDateTimeMonthAr]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.lblDateTimeMonthAr.Font = new System.Drawing.Font("Tahoma", 13.8F, System.Drawing.FontStyle.Bold);
            this.lblDateTimeMonthAr.LocationFloat = new DevExpress.Utils.PointFloat(602.8682F, 138.0005F);
            this.lblDateTimeMonthAr.Name = "lblDateTimeMonthAr";
            this.lblDateTimeMonthAr.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblDateTimeMonthAr.SizeF = new System.Drawing.SizeF(63.22437F, 27.16998F);
            this.lblDateTimeMonthAr.StylePriority.UseFont = false;
            this.lblDateTimeMonthAr.StylePriority.UseTextAlignment = false;
            this.lblDateTimeMonthAr.Text = "lblDateTimeMonthAr";
            this.lblDateTimeMonthAr.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.lblDateTimeMonthAr.TextFormatString = "{0:dd/MM/yyyy}";
            // 
            // xrLabel15
            // 
            this.xrLabel15.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel15.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel15.CanGrow = false;
            this.xrLabel15.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel15.ForeColor = System.Drawing.Color.Black;
            this.xrLabel15.LocationFloat = new DevExpress.Utils.PointFloat(592.8682F, 138.0005F);
            this.xrLabel15.Name = "xrLabel15";
            this.xrLabel15.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel15.SizeF = new System.Drawing.SizeF(9.999969F, 26.74001F);
            this.xrLabel15.StylePriority.UseBorderColor = false;
            this.xrLabel15.StylePriority.UseBorders = false;
            this.xrLabel15.StylePriority.UseFont = false;
            this.xrLabel15.StylePriority.UseForeColor = false;
            this.xrLabel15.StylePriority.UsePadding = false;
            this.xrLabel15.StylePriority.UseTextAlignment = false;
            this.xrLabel15.Text = "/";
            this.xrLabel15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel15.TextFormatString = "{0:yyyy}";
            // 
            // lblDateTimeYear
            // 
            this.lblDateTimeYear.BorderColor = System.Drawing.Color.DarkGray;
            this.lblDateTimeYear.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblDateTimeYear.CanGrow = false;
            this.lblDateTimeYear.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([Parameters.PrmDate]),\'9999\',[Parameters.PrmDate] )")});
            this.lblDateTimeYear.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.lblDateTimeYear.ForeColor = System.Drawing.Color.Black;
            this.lblDateTimeYear.LocationFloat = new DevExpress.Utils.PointFloat(526.6199F, 138.0045F);
            this.lblDateTimeYear.Name = "lblDateTimeYear";
            this.lblDateTimeYear.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblDateTimeYear.SizeF = new System.Drawing.SizeF(66.24835F, 26.73611F);
            this.lblDateTimeYear.StylePriority.UseBorderColor = false;
            this.lblDateTimeYear.StylePriority.UseBorders = false;
            this.lblDateTimeYear.StylePriority.UseFont = false;
            this.lblDateTimeYear.StylePriority.UseForeColor = false;
            this.lblDateTimeYear.StylePriority.UsePadding = false;
            this.lblDateTimeYear.StylePriority.UseTextAlignment = false;
            this.lblDateTimeYear.Text = " ";
            this.lblDateTimeYear.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lblDateTimeYear.TextFormatString = "{0:yyyy}";
            // 
            // xrLabel16
            // 
            this.xrLabel16.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel16.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel16.CanGrow = false;
            this.xrLabel16.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel16.ForeColor = System.Drawing.Color.Black;
            this.xrLabel16.LocationFloat = new DevExpress.Utils.PointFloat(666.0927F, 138.4305F);
            this.xrLabel16.Name = "xrLabel16";
            this.xrLabel16.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel16.SizeF = new System.Drawing.SizeF(9.999939F, 26.74001F);
            this.xrLabel16.StylePriority.UseBorderColor = false;
            this.xrLabel16.StylePriority.UseBorders = false;
            this.xrLabel16.StylePriority.UseFont = false;
            this.xrLabel16.StylePriority.UseForeColor = false;
            this.xrLabel16.StylePriority.UsePadding = false;
            this.xrLabel16.StylePriority.UseTextAlignment = false;
            this.xrLabel16.Text = "/";
            this.xrLabel16.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel16.TextFormatString = "{0:yyyy}";
            // 
            // lblDateTimeDay
            // 
            this.lblDateTimeDay.BorderColor = System.Drawing.Color.DarkGray;
            this.lblDateTimeDay.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblDateTimeDay.CanGrow = false;
            this.lblDateTimeDay.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([Parameters.PrmDate]), \'31\', [Parameters.PrmDate])"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Parameters.PrmDate]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.lblDateTimeDay.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.lblDateTimeDay.ForeColor = System.Drawing.Color.Black;
            this.lblDateTimeDay.LocationFloat = new DevExpress.Utils.PointFloat(676.0927F, 138.0005F);
            this.lblDateTimeDay.Name = "lblDateTimeDay";
            this.lblDateTimeDay.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblDateTimeDay.SizeF = new System.Drawing.SizeF(22.00647F, 26.74001F);
            this.lblDateTimeDay.StylePriority.UseBorderColor = false;
            this.lblDateTimeDay.StylePriority.UseBorders = false;
            this.lblDateTimeDay.StylePriority.UseFont = false;
            this.lblDateTimeDay.StylePriority.UseForeColor = false;
            this.lblDateTimeDay.StylePriority.UsePadding = false;
            this.lblDateTimeDay.StylePriority.UseTextAlignment = false;
            this.lblDateTimeDay.Text = " ";
            this.lblDateTimeDay.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lblDateTimeDay.TextFormatString = "{0:dd}";
            // 
            // lblResidentPeriod
            // 
            this.lblResidentPeriod.CanGrow = false;
            this.lblResidentPeriod.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([CalcResidencyPeriod]), \'0\',[CalcResidencyPeriod])"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([CalcResidencyPeriod]), \'XrStyleWhite\', \'XrStyleBlack\')")});
            this.lblResidentPeriod.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold);
            this.lblResidentPeriod.LocationFloat = new DevExpress.Utils.PointFloat(604.9558F, 694F);
            this.lblResidentPeriod.Name = "lblResidentPeriod";
            this.lblResidentPeriod.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblResidentPeriod.SizeF = new System.Drawing.SizeF(120.3125F, 23F);
            this.lblResidentPeriod.StylePriority.UseFont = false;
            this.lblResidentPeriod.StylePriority.UseTextAlignment = false;
            this.lblResidentPeriod.Text = "xrLabel8";
            this.lblResidentPeriod.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel13
            // 
            this.xrLabel13.CanGrow = false;
            this.xrLabel13.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([Sponsor_Governate]),\'IsNull\', [Sponsor_Governate])"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Sponsor_Governate]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel13.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrLabel13.LocationFloat = new DevExpress.Utils.PointFloat(352.3335F, 89.81948F);
            this.xrLabel13.Name = "xrLabel13";
            this.xrLabel13.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel13.SizeF = new System.Drawing.SizeF(266.6665F, 21.88889F);
            this.xrLabel13.StylePriority.UseFont = false;
            this.xrLabel13.StylePriority.UseTextAlignment = false;
            this.xrLabel13.Text = "xrLabel13";
            this.xrLabel13.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel1
            // 
            this.xrLabel1.CanGrow = false;
            this.xrLabel1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([Organization]),\'IsNull\',[Organization] )"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Organization]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel1.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold);
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(47.55889F, 263F);
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(600.2778F, 23F);
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.Text = "xrLabel1";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel2
            // 
            this.xrLabel2.CanGrow = false;
            this.xrLabel2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([Sponsor_Name]),\'IsNull\', [Sponsor_Name])"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Sponsor_Name]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel2.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold);
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(50.78111F, 307F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(598.0555F, 23F);
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = "xrLabel2";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel3
            // 
            this.xrLabel3.CanGrow = false;
            this.xrLabel3.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([Sponsor_Org_Address]),\'IsNull\',[Sponsor_Org_Address])"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Sponsor_Org_Address]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel3.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold);
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(50.78111F, 355.4445F);
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.SizeF = new System.Drawing.SizeF(597.0555F, 23F);
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.StylePriority.UseTextAlignment = false;
            this.xrLabel3.Text = "xrLabel3";
            this.xrLabel3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel4
            // 
            this.xrLabel4.CanGrow = false;
            this.xrLabel4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([Employee_Name]),\'IsNull\',[Employee_Name] )"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Employee_Name]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel4.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold);
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(342.4722F, 433F);
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(382.7777F, 23.00003F);
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UseTextAlignment = false;
            this.xrLabel4.Text = "xrLabel4";
            this.xrLabel4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel5
            // 
            this.xrLabel5.CanGrow = false;
            this.xrLabel5.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([Employee_Nationality]),\'IsNull\', [Employee_Nationality])"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Employee_Nationality]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel5.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold);
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(46.55597F, 434F);
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.SizeF = new System.Drawing.SizeF(226.6929F, 23.00006F);
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.StylePriority.UseTextAlignment = false;
            this.xrLabel5.Text = "xrLabel5";
            this.xrLabel5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblPassportNum
            // 
            this.lblPassportNum.CanGrow = false;
            this.lblPassportNum.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([Passport Number]),\'123456\',[Passport Number])"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Passport Number]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.lblPassportNum.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.lblPassportNum.LocationFloat = new DevExpress.Utils.PointFloat(45.29675F, 480F);
            this.lblPassportNum.Name = "lblPassportNum";
            this.lblPassportNum.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblPassportNum.SizeF = new System.Drawing.SizeF(618.7499F, 23F);
            this.lblPassportNum.StylePriority.UseFont = false;
            this.lblPassportNum.StylePriority.UseTextAlignment = false;
            this.lblPassportNum.Text = "lblPassportNum";
            this.lblPassportNum.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel7
            // 
            this.xrLabel7.CanGrow = false;
            this.xrLabel7.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([GovtJobtitle]),\'IsNull\',[GovtJobtitle])"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([GovtJobtitle]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel7.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold);
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(47.89918F, 568.1666F);
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel7.SizeF = new System.Drawing.SizeF(252.0833F, 23F);
            this.xrLabel7.StylePriority.UseFont = false;
            this.xrLabel7.StylePriority.UseTextAlignment = false;
            this.xrLabel7.Text = "xrLabel7";
            this.xrLabel7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel9
            // 
            this.xrLabel9.CanGrow = false;
            this.xrLabel9.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([Organization]),\'IsNull\',[Organization] )"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Organization]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel9.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold);
            this.xrLabel9.LocationFloat = new DevExpress.Utils.PointFloat(45.28834F, 655F);
            this.xrLabel9.Multiline = true;
            this.xrLabel9.Name = "xrLabel9";
            this.xrLabel9.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel9.SizeF = new System.Drawing.SizeF(235.4167F, 65F);
            this.xrLabel9.StylePriority.UseFont = false;
            this.xrLabel9.StylePriority.UseTextAlignment = false;
            this.xrLabel9.Text = "xrLabel9";
            this.xrLabel9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            // 
            // lblContractSalary
            // 
            this.lblContractSalary.CanGrow = false;
            this.lblContractSalary.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([Contract_Salary]), \'IsNull\', [Contract_Salary])"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Contract_Salary]), \'XrStyleWhite\', \'XrStyleBlack\')")});
            this.lblContractSalary.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.lblContractSalary.LocationFloat = new DevExpress.Utils.PointFloat(407.2675F, 604.3333F);
            this.lblContractSalary.Name = "lblContractSalary";
            this.lblContractSalary.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblContractSalary.SizeF = new System.Drawing.SizeF(123.9583F, 23F);
            this.lblContractSalary.StylePriority.UseFont = false;
            this.lblContractSalary.StylePriority.UseTextAlignment = false;
            this.lblContractSalary.Text = "lblContractSalary";
            this.lblContractSalary.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel11
            // 
            this.xrLabel11.CanGrow = false;
            this.xrLabel11.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([Employee_Name]),\'IsNull\',[Employee_Name])"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Employee_Name]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel11.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold);
            this.xrLabel11.LocationFloat = new DevExpress.Utils.PointFloat(497.9167F, 1057.188F);
            this.xrLabel11.Name = "xrLabel11";
            this.xrLabel11.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel11.SizeF = new System.Drawing.SizeF(285.4167F, 32.83325F);
            this.xrLabel11.StylePriority.UseFont = false;
            this.xrLabel11.StylePriority.UseTextAlignment = false;
            this.xrLabel11.Text = "xrLabel11";
            this.xrLabel11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel12
            // 
            this.xrLabel12.CanGrow = false;
            this.xrLabel12.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNull([Sponsor_Name]),\'IsNull\', [Sponsor_Name])"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Sponsor_Name]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel12.Font = new System.Drawing.Font("Tahoma", 14F, System.Drawing.FontStyle.Bold);
            this.xrLabel12.LocationFloat = new DevExpress.Utils.PointFloat(16.75F, 1057.188F);
            this.xrLabel12.Name = "xrLabel12";
            this.xrLabel12.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel12.SizeF = new System.Drawing.SizeF(330.2083F, 32.83325F);
            this.xrLabel12.StylePriority.UseFont = false;
            this.xrLabel12.StylePriority.UseTextAlignment = false;
            this.xrLabel12.Text = "xrLabel12";
            this.xrLabel12.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(820.3334F, 1127.042F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            // 
            // PrmDate
            // 
            this.PrmDate.Description = "Date";
            this.PrmDate.Name = "PrmDate";
            this.PrmDate.Type = typeof(System.DateTime);
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 2F;
            this.TopMargin.Name = "TopMargin";
            this.TopMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.TopMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // BottomMargin
            // 
            this.BottomMargin.HeightF = 2F;
            this.BottomMargin.Name = "BottomMargin";
            this.BottomMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.BottomMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // Title
            // 
            this.Title.BackColor = System.Drawing.Color.Transparent;
            this.Title.BorderColor = System.Drawing.Color.Black;
            this.Title.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.Title.BorderWidth = 1F;
            this.Title.Font = new System.Drawing.Font("Tahoma", 20F, System.Drawing.FontStyle.Bold);
            this.Title.ForeColor = System.Drawing.Color.Maroon;
            this.Title.Name = "Title";
            // 
            // FieldCaption
            // 
            this.FieldCaption.BackColor = System.Drawing.Color.Transparent;
            this.FieldCaption.BorderColor = System.Drawing.Color.Black;
            this.FieldCaption.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.FieldCaption.BorderWidth = 1F;
            this.FieldCaption.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.FieldCaption.ForeColor = System.Drawing.Color.Maroon;
            this.FieldCaption.Name = "FieldCaption";
            // 
            // PageInfo
            // 
            this.PageInfo.BackColor = System.Drawing.Color.Transparent;
            this.PageInfo.BorderColor = System.Drawing.Color.Black;
            this.PageInfo.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.PageInfo.BorderWidth = 1F;
            this.PageInfo.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.PageInfo.ForeColor = System.Drawing.Color.Black;
            this.PageInfo.Name = "PageInfo";
            // 
            // DataField
            // 
            this.DataField.BackColor = System.Drawing.Color.Transparent;
            this.DataField.BorderColor = System.Drawing.Color.Black;
            this.DataField.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.DataField.BorderWidth = 1F;
            this.DataField.Font = new System.Drawing.Font("Tahoma", 10F);
            this.DataField.ForeColor = System.Drawing.Color.Black;
            this.DataField.Name = "DataField";
            this.DataField.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            // 
            // PrmEmpCode
            // 
            this.PrmEmpCode.Description = "Employee Code";
            this.PrmEmpCode.Name = "PrmEmpCode";
            // 
            // CaclDateTime
            // 
            this.CaclDateTime.Expression = "LocalDateTimeToday()";
            this.CaclDateTime.FieldType = DevExpress.XtraReports.UI.FieldType.DateTime;
            this.CaclDateTime.Name = "CaclDateTime";
            // 
            // CalcDateTimeMonthNum
            // 
            this.CalcDateTimeMonthNum.DataMember = "Query";
            this.CalcDateTimeMonthNum.Expression = "GetMonth([Parameters].[PrmDate])";
            this.CalcDateTimeMonthNum.Name = "CalcDateTimeMonthNum";
            // 
            // CalcDateTimeMonthAr
            // 
            this.CalcDateTimeMonthAr.DataMember = "Query";
            this.CalcDateTimeMonthAr.Expression = @"Iif([CalcDateTimeMonthNum] == 1 ,'يناير',Iif([CalcDateTimeMonthNum] == 2, 'فبراير' , Iif([CalcDateTimeMonthNum] == 3, 'مارس' ,Iif([CalcDateTimeMonthNum] = 4, 'أبريل' ,Iif([CalcDateTimeMonthNum] == 5, 'مايو' ,Iif([CalcDateTimeMonthNum] == 6, 'يونيو' ,Iif([CalcDateTimeMonthNum] == 7, 'يوليو'  , Iif([CalcDateTimeMonthNum] == 8, 'أغسطس' ,Iif([CalcDateTimeMonthNum] == 9, 'سبتمبر' , Iif([CalcDateTimeMonthNum] == 10, 'أكتوبر' , Iif([CalcDateTimeMonthNum] == 11, 'نوفمبر' , Iif([CalcDateTimeMonthNum] == 12, 'ديسمبر' ,' ' ))))))))))))";
            this.CalcDateTimeMonthAr.Name = "CalcDateTimeMonthAr";
            // 
            // CalcResidencyPeriod
            // 
            this.CalcResidencyPeriod.DataMember = "Query";
            this.CalcResidencyPeriod.Expression = @"Iif([Residency_Period] == 1, 'سنة واحدة' ,
Iif([Residency_Period] == 2, 'سنتان' ,
Iif([Residency_Period] == 3, 'ثلاث سنوات' ,
Iif([Residency_Period] == 4, 'أربع سنوات' , 
Iif([Residency_Period] == 5, 'خمس سنوات' ,'')
))))";
            this.CalcResidencyPeriod.Name = "CalcResidencyPeriod";
            // 
            // XrStyleBlack
            // 
            this.XrStyleBlack.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.XrStyleBlack.Name = "XrStyleBlack";
            // 
            // XrStyleWhite
            // 
            this.XrStyleWhite.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.XrStyleWhite.Name = "XrStyleWhite";
            // 
            // PrmCivilId
            // 
            this.PrmCivilId.Description = "Civil Id";
            this.PrmCivilId.Name = "PrmCivilId";
            // 
            // rptWFEmploymentContract
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.CaclDateTime,
            this.CalcDateTimeMonthNum,
            this.CalcDateTimeMonthAr,
            this.CalcResidencyPeriod});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
            this.DataMember = "Query";
            this.DataSource = this.sqlDataSource1;
            this.Margins = new System.Drawing.Printing.Margins(2, 2, 2, 2);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.PrmEmpCode,
            this.PrmCivilId,
            this.PrmDate});
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.Title,
            this.FieldCaption,
            this.PageInfo,
            this.DataField,
            this.XrStyleBlack,
            this.XrStyleWhite});
            this.Version = "23.2";
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.XRControlStyle Title;
        private DevExpress.XtraReports.UI.XRControlStyle FieldCaption;
        private DevExpress.XtraReports.UI.XRControlStyle PageInfo;
        private DevExpress.XtraReports.UI.XRControlStyle DataField;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel13;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.XRLabel xrLabel3;
        private DevExpress.XtraReports.UI.XRLabel xrLabel4;
        private DevExpress.XtraReports.UI.XRLabel xrLabel5;
        private DevExpress.XtraReports.UI.XRLabel lblPassportNum;
        private DevExpress.XtraReports.UI.XRLabel xrLabel7;
        private DevExpress.XtraReports.UI.XRLabel xrLabel9;
        private DevExpress.XtraReports.UI.XRLabel lblContractSalary;
        private DevExpress.XtraReports.UI.XRLabel xrLabel11;
        private DevExpress.XtraReports.UI.XRLabel xrLabel12;
        private DevExpress.XtraReports.UI.XRLabel lblResidentPeriod;
        private DevExpress.XtraReports.Parameters.Parameter PrmEmpCode;
        public DevExpress.DataAccess.Sql.SqlDataSource sqlDataSource1;
        private DevExpress.XtraReports.UI.CalculatedField CaclDateTime;
        private DevExpress.XtraReports.UI.XRLabel lblDateTimeMonthAr;
        private DevExpress.XtraReports.UI.XRLabel xrLabel15;
        private DevExpress.XtraReports.UI.XRLabel lblDateTimeYear;
        private DevExpress.XtraReports.UI.XRLabel xrLabel16;
        private DevExpress.XtraReports.UI.XRLabel lblDateTimeDay;
        private DevExpress.XtraReports.UI.CalculatedField CalcDateTimeMonthNum;
        private DevExpress.XtraReports.UI.CalculatedField CalcDateTimeMonthAr;
        private DevExpress.XtraReports.Parameters.Parameter PrmDate;
        private DevExpress.XtraReports.UI.CalculatedField CalcResidencyPeriod;
        private DevExpress.XtraReports.UI.XRControlStyle XrStyleBlack;
        private DevExpress.XtraReports.UI.XRControlStyle XrStyleWhite;
        private DevExpress.XtraReports.Parameters.Parameter PrmCivilId;
    }
}
