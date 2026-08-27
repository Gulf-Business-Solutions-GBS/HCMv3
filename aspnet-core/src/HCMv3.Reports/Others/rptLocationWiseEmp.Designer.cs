

namespace HCMv3.Reports.Others
{
    partial class rptLocationWiseEmp
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
            DevExpress.DataAccess.Sql.QueryParameter queryParameter1 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery2 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings1 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings2 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            this.EmpWorklocationDS = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrTable2 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow3 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell25 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell4 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell6 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable1 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow1 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell2 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell7 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell10 = new DevExpress.XtraReports.UI.XRTableCell();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.EngMainDataStyle = new DevExpress.XtraReports.UI.XRControlStyle();
            this.OFFCurrentMonthCal = new DevExpress.XtraReports.UI.CalculatedField();
            this.SLCal = new DevExpress.XtraReports.UI.CalculatedField();
            this.ENGDataStyle = new DevExpress.XtraReports.UI.XRControlStyle();
            this.CalNetPay = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalEarning = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalDeduct = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcForPeriodMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcForPeriodYear = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcInPeriodMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcInPeriodYear = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcDateTime = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcNetPay = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcForGetMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcInGetMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcForGetYear = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcInGetYear = new DevExpress.XtraReports.UI.CalculatedField();
            this.PageHeader = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.xrLabel10 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel9 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrControlStyle1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.PrmWorkLocation = new DevExpress.XtraReports.Parameters.Parameter();
            this.PageFooter = new DevExpress.XtraReports.UI.PageFooterBand();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblMudirTitle = new DevExpress.XtraReports.UI.XRLabel();
            this.PrmSignature = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmCompany = new DevExpress.XtraReports.Parameters.Parameter();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // EmpWorklocationDS
            // 
            this.EmpWorklocationDS.ConnectionName = HCMv3.Reports.HcmReportBase.DesignTimeConnectionName;
            this.EmpWorklocationDS.Name = "EmpWorklocationDS";
            customSqlQuery1.Name = "EmpWorklocationQuery";
            queryParameter1.Name = "PrmWorkLocation";
            queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmWorkLocation]", typeof(int));
            customSqlQuery1.Parameters.Add(queryParameter1);
            customSqlQuery1.Sql = OthersAdminSql.LocationWiseEmpQuery;
            customSqlQuery2.Name = "WorkLocation";
            customSqlQuery2.Sql = OthersAdminSql.WorkLocationLookupQuery;
            this.EmpWorklocationDS.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1,
            customSqlQuery2});
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable2});
            this.Detail.HeightF = 25F;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrTable2
            // 
            this.xrTable2.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right)));
            this.xrTable2.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTable2.LocationFloat = new DevExpress.Utils.PointFloat(9.999954F, 0F);
            this.xrTable2.Name = "xrTable2";
            this.xrTable2.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow3});
            this.xrTable2.SizeF = new System.Drawing.SizeF(797.0001F, 25F);
            this.xrTable2.StylePriority.UseBorders = false;
            this.xrTable2.StylePriority.UseFont = false;
            this.xrTable2.StylePriority.UseTextAlignment = false;
            this.xrTable2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrTableRow3
            // 
            this.xrTableRow3.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell25,
            this.xrTableCell4,
            this.xrTableCell6});
            this.xrTableRow3.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableRow3.Name = "xrTableRow3";
            this.xrTableRow3.StylePriority.UseFont = false;
            this.xrTableRow3.Weight = 1D;
            // 
            // xrTableCell25
            // 
            this.xrTableCell25.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell25.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CivilID]")});
            this.xrTableCell25.Font = new System.Drawing.Font("Tahoma", 10.2F);
            this.xrTableCell25.Name = "xrTableCell25";
            this.xrTableCell25.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 100F);
            this.xrTableCell25.StylePriority.UseBorders = false;
            this.xrTableCell25.StylePriority.UseFont = false;
            this.xrTableCell25.StylePriority.UsePadding = false;
            this.xrTableCell25.StylePriority.UseTextAlignment = false;
            this.xrTableCell25.Text = "xrTableCell25";
            this.xrTableCell25.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell25.Weight = 2.4291363444523784D;
            // 
            // xrTableCell4
            // 
            this.xrTableCell4.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NationalityAr]")});
            this.xrTableCell4.Font = new System.Drawing.Font("Tahoma", 10.2F);
            this.xrTableCell4.Name = "xrTableCell4";
            this.xrTableCell4.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 100F);
            this.xrTableCell4.StylePriority.UseBorders = false;
            this.xrTableCell4.StylePriority.UseFont = false;
            this.xrTableCell4.StylePriority.UsePadding = false;
            this.xrTableCell4.StylePriority.UseTextAlignment = false;
            this.xrTableCell4.Text = "xrTableCell4";
            this.xrTableCell4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell4.Weight = 1.4783581427167924D;
            // 
            // xrTableCell6
            // 
            this.xrTableCell6.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell6.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FullNameAr]")});
            this.xrTableCell6.Font = new System.Drawing.Font("Tahoma", 10.2F);
            this.xrTableCell6.Name = "xrTableCell6";
            this.xrTableCell6.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 100F);
            this.xrTableCell6.StylePriority.UseBorders = false;
            this.xrTableCell6.StylePriority.UseFont = false;
            this.xrTableCell6.StylePriority.UsePadding = false;
            this.xrTableCell6.StylePriority.UseTextAlignment = false;
            this.xrTableCell6.Text = "xrTableCell6";
            this.xrTableCell6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell6.Weight = 3.9214075362309875D;
            // 
            // xrTable1
            // 
            this.xrTable1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.xrTable1.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrTable1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTable1.LocationFloat = new DevExpress.Utils.PointFloat(9.999959F, 352.4583F);
            this.xrTable1.Name = "xrTable1";
            this.xrTable1.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 5, 100F);
            this.xrTable1.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow1});
            this.xrTable1.SizeF = new System.Drawing.SizeF(797.0001F, 25F);
            this.xrTable1.StylePriority.UseBackColor = false;
            this.xrTable1.StylePriority.UseBorders = false;
            this.xrTable1.StylePriority.UseFont = false;
            this.xrTable1.StylePriority.UsePadding = false;
            this.xrTable1.StylePriority.UseTextAlignment = false;
            this.xrTable1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrTableRow1
            // 
            this.xrTableRow1.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableRow1.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell2,
            this.xrTableCell7,
            this.xrTableCell10});
            this.xrTableRow1.Name = "xrTableRow1";
            this.xrTableRow1.StylePriority.UseBorders = false;
            this.xrTableRow1.Weight = 1D;
            // 
            // xrTableCell2
            // 
            this.xrTableCell2.Font = new System.Drawing.Font("Tahoma", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell2.Name = "xrTableCell2";
            this.xrTableCell2.StylePriority.UseFont = false;
            this.xrTableCell2.StylePriority.UseTextAlignment = false;
            this.xrTableCell2.Text = "الرقم المدني";
            this.xrTableCell2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            this.xrTableCell2.Weight = 2.4291362992355645D;
            // 
            // xrTableCell7
            // 
            this.xrTableCell7.Font = new System.Drawing.Font("Tahoma", 10.2F, System.Drawing.FontStyle.Bold);
            this.xrTableCell7.Name = "xrTableCell7";
            this.xrTableCell7.StylePriority.UseFont = false;
            this.xrTableCell7.StylePriority.UseTextAlignment = false;
            this.xrTableCell7.Text = "الجنسية";
            this.xrTableCell7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            this.xrTableCell7.Weight = 1.4783580208374836D;
            // 
            // xrTableCell10
            // 
            this.xrTableCell10.Font = new System.Drawing.Font("Tahoma", 10.2F, System.Drawing.FontStyle.Bold);
            this.xrTableCell10.Name = "xrTableCell10";
            this.xrTableCell10.StylePriority.UseFont = false;
            this.xrTableCell10.StylePriority.UseTextAlignment = false;
            this.xrTableCell10.Text = "الاســــــــم";
            this.xrTableCell10.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            this.xrTableCell10.Weight = 3.9214086656877347D;
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 0F;
            this.TopMargin.Name = "TopMargin";
            this.TopMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 10, 10, 100F);
            this.TopMargin.StylePriority.UsePadding = false;
            this.TopMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.TopMargin.Visible = false;
            // 
            // BottomMargin
            // 
            this.BottomMargin.HeightF = 1F;
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
            // OFFCurrentMonthCal
            // 
            this.OFFCurrentMonthCal.DataMember = "Employee.Absence";
            this.OFFCurrentMonthCal.Expression = "SUM(DateDiffDay([StartDate], [EndDate]))";
            this.OFFCurrentMonthCal.Name = "OFFCurrentMonthCal";
            // 
            // SLCal
            // 
            this.SLCal.DataMember = "Employee.Absence";
            this.SLCal.Expression = "iif([Employee.Absence.AbsenceType.NameEn] == \'Sick Leave\', \r\nSUM(DateDiffDay([Emp" +
    "loyee.Absence.StartDate],[Employee.Absence.EndDate] )),0)";
            this.SLCal.Name = "SLCal";
            // 
            // ENGDataStyle
            // 
            this.ENGDataStyle.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ENGDataStyle.Name = "ENGDataStyle";
            this.ENGDataStyle.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.ENGDataStyle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // CalNetPay
            // 
            this.CalNetPay.DataMember = "SalaryRegisterDetail";
            this.CalNetPay.Expression = "SUM(IIF([WageType.NameEn]==\'Net Pay\',[Amount],0))";
            this.CalNetPay.FieldType = DevExpress.XtraReports.UI.FieldType.Double;
            this.CalNetPay.Name = "CalNetPay";
            // 
            // CalEarning
            // 
            this.CalEarning.DataMember = "SalaryRegisterDetail";
            this.CalEarning.DisplayName = "CalEarning";
            this.CalEarning.Expression = "(IIF([WageType.BillingType]==0,SUM([Amount]),0))";
            this.CalEarning.FieldType = DevExpress.XtraReports.UI.FieldType.Double;
            this.CalEarning.Name = "CalEarning";
            // 
            // CalDeduct
            // 
            this.CalDeduct.DataMember = "SalaryRegisterDetail";
            this.CalDeduct.Expression = "(IIF([WageType.BillingType]==1,SUM([Amount]),0))";
            this.CalDeduct.FieldType = DevExpress.XtraReports.UI.FieldType.Double;
            this.CalDeduct.Name = "CalDeduct";
            // 
            // CalcForPeriodMonth
            // 
            this.CalcForPeriodMonth.DataMember = "PayslipQuery";
            this.CalcForPeriodMonth.Expression = "GetMonth([ForPeriod])";
            this.CalcForPeriodMonth.FieldType = DevExpress.XtraReports.UI.FieldType.Int32;
            this.CalcForPeriodMonth.Name = "CalcForPeriodMonth";
            // 
            // CalcForPeriodYear
            // 
            this.CalcForPeriodYear.DataMember = "PayslipQuery";
            this.CalcForPeriodYear.Expression = "GetYear([ForPeriod])";
            this.CalcForPeriodYear.FieldType = DevExpress.XtraReports.UI.FieldType.Int32;
            this.CalcForPeriodYear.Name = "CalcForPeriodYear";
            // 
            // CalcInPeriodMonth
            // 
            this.CalcInPeriodMonth.DataMember = "PayslipQuery";
            this.CalcInPeriodMonth.Expression = "GetMonth([InPeriod])";
            this.CalcInPeriodMonth.Name = "CalcInPeriodMonth";
            // 
            // CalcInPeriodYear
            // 
            this.CalcInPeriodYear.DataMember = "PayslipQuery";
            this.CalcInPeriodYear.Expression = "GetYear([InPeriod])";
            this.CalcInPeriodYear.Name = "CalcInPeriodYear";
            // 
            // CalcDateTime
            // 
            this.CalcDateTime.DataMember = "PayslipQuery";
            this.CalcDateTime.Expression = "LocalDateTimeNow()";
            this.CalcDateTime.Name = "CalcDateTime";
            // 
            // CalcNetPay
            // 
            this.CalcNetPay.DataMember = "PayslipQuery";
            this.CalcNetPay.Expression = "Round([Net Pay],3)";
            this.CalcNetPay.Name = "CalcNetPay";
            // 
            // CalcForGetMonth
            // 
            this.CalcForGetMonth.DataMember = "Query";
            this.CalcForGetMonth.Expression = "ToStr(GetMonth([ForPeriod]))";
            this.CalcForGetMonth.Name = "CalcForGetMonth";
            // 
            // CalcInGetMonth
            // 
            this.CalcInGetMonth.DataMember = "Query";
            this.CalcInGetMonth.Expression = "ToStr(GetMonth([InPeriod]))";
            this.CalcInGetMonth.Name = "CalcInGetMonth";
            // 
            // CalcForGetYear
            // 
            this.CalcForGetYear.DataMember = "Query";
            this.CalcForGetYear.Expression = "ToStr(GetYear([ForPeriod]))";
            this.CalcForGetYear.Name = "CalcForGetYear";
            // 
            // CalcInGetYear
            // 
            this.CalcInGetYear.DataMember = "Query";
            this.CalcInGetYear.Expression = "ToStr(GetYear([InPeriod]))";
            this.CalcInGetYear.Name = "CalcInGetYear";
            // 
            // PageHeader
            // 
            this.PageHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel10,
            this.xrLabel9,
            this.xrLabel5,
            this.xrLabel4,
            this.xrLabel2,
            this.xrLabel1,
            this.xrLabel3,
            this.xrTable1});
            this.PageHeader.HeightF = 377.4583F;
            this.PageHeader.Name = "PageHeader";
            // 
            // xrLabel10
            // 
            this.xrLabel10.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters].[PrmCompany]")});
            this.xrLabel10.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel10.LocationFloat = new DevExpress.Utils.PointFloat(257.2916F, 272.5417F);
            this.xrLabel10.Name = "xrLabel10";
            this.xrLabel10.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel10.SizeF = new System.Drawing.SizeF(417.2918F, 22.99999F);
            this.xrLabel10.StylePriority.UseFont = false;
            this.xrLabel10.StylePriority.UseTextAlignment = false;
            this.xrLabel10.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel9
            // 
            this.xrLabel9.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel9.LocationFloat = new DevExpress.Utils.PointFloat(257.2916F, 237.125F);
            this.xrLabel9.Name = "xrLabel9";
            this.xrLabel9.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel9.SizeF = new System.Drawing.SizeF(392.2918F, 23F);
            this.xrLabel9.StylePriority.UseFont = false;
            this.xrLabel9.StylePriority.UseTextAlignment = false;
            this.xrLabel9.Text = "عامر يحيى زكريا الأنصاري";
            this.xrLabel9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel5
            // 
            this.xrLabel5.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TradeLicenseNum]")});
            this.xrLabel5.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(240.2083F, 306.9167F);
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.SizeF = new System.Drawing.SizeF(290.6251F, 23F);
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.StylePriority.UseTextAlignment = false;
            this.xrLabel5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel4
            // 
            this.xrLabel4.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(530.8333F, 306.9167F);
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(276.1667F, 22.99999F);
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UseTextAlignment = false;
            this.xrLabel4.Text = "رقم الترخيص حسب جهة الأصدار ";
            this.xrLabel4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel2
            // 
            this.xrLabel2.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(674.5834F, 272.5417F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(132.4166F, 22.99999F);
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = "/ الاسم التجاري";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel1
            // 
            this.xrLabel1.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(649.5833F, 237.125F);
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(157.4167F, 23F);
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.Text = "/اسم صاحب العمل";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel3
            // 
            this.xrLabel3.Font = new System.Drawing.Font("Tahoma", 14.25F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))));
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(9.9998F, 187.75F);
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.SizeF = new System.Drawing.SizeF(797.0001F, 32.37498F);
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.StylePriority.UseTextAlignment = false;
            this.xrLabel3.Text = "كشف بأسماء العاملين ";
            this.xrLabel3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrControlStyle1
            // 
            this.xrControlStyle1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrControlStyle1.Name = "xrControlStyle1";
            // 
            // PrmWorkLocation
            // 
            this.PrmWorkLocation.Description = "Work Location";
            dynamicListLookUpSettings1.DataAdapter = null;
            dynamicListLookUpSettings1.DataMember = "WorkLocation";
            dynamicListLookUpSettings1.DataSource = this.EmpWorklocationDS;
            dynamicListLookUpSettings1.DisplayMember = "NameEn";
            dynamicListLookUpSettings1.ValueMember = "OID";
            this.PrmWorkLocation.LookUpSettings = dynamicListLookUpSettings1;
            this.PrmWorkLocation.Name = "PrmWorkLocation";
            this.PrmWorkLocation.Type = typeof(int);
            this.PrmWorkLocation.ValueInfo = "0";
            // 
            // PageFooter
            // 
            this.PageFooter.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel6,
            this.xrLabel7,
            this.lblMudirTitle});
            this.PageFooter.HeightF = 263.5417F;
            this.PageFooter.Name = "PageFooter";
            this.PageFooter.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.PageFooter_BeforePrint);
            // 
            // xrLabel6
            // 
            this.xrLabel6.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters].[PrmCompany]")});
            this.xrLabel6.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(9.999998F, 26.00002F);
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(397.7918F, 23F);
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UseTextAlignment = false;
            this.xrLabel6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel7
            // 
            this.xrLabel7.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters].[PrmSignature]")});
            this.xrLabel7.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(9.999959F, 110.375F);
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel7.SizeF = new System.Drawing.SizeF(397.7918F, 23F);
            this.xrLabel7.StylePriority.UseFont = false;
            this.xrLabel7.StylePriority.UseTextAlignment = false;
            this.xrLabel7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // lblMudirTitle
            // 
            this.lblMudirTitle.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMudirTitle.LocationFloat = new DevExpress.Utils.PointFloat(9.999959F, 144.75F);
            this.lblMudirTitle.Name = "lblMudirTitle";
            this.lblMudirTitle.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblMudirTitle.SizeF = new System.Drawing.SizeF(397.7918F, 23F);
            this.lblMudirTitle.StylePriority.UseFont = false;
            this.lblMudirTitle.StylePriority.UseTextAlignment = false;
            this.lblMudirTitle.Text = "مدير الموارد البشرية و ادارة التدريب";
            this.lblMudirTitle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // PrmSignature
            // 
            this.PrmSignature.Description = "Signature";
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue("محمد بشير", "محمد بشير"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue("وائل محمد كمال الدين شربجي", "وائل محمد كمال الدين شربجي"));
            this.PrmSignature.LookUpSettings = staticListLookUpSettings1;
            this.PrmSignature.Name = "PrmSignature";
            // 
            // PrmCompany
            // 
            this.PrmCompany.Description = "Company";
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue("شركة الطرف الاغر للتجارة العامة", "شركة الطرف الاغر للتجارة العامة"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue("شركة كويت لاكسجري للهدايا والكماليات ", "شركة كويت لاكسجري للهدايا والكماليات "));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue("شركة اتحاد الفخامة بالتجزئة للهدايا و الكماليات\n ", "شركة اتحاد الفخامة بالتجزئة للهدايا و الكماليات\n "));
            this.PrmCompany.LookUpSettings = staticListLookUpSettings2;
            this.PrmCompany.Name = "PrmCompany";
            // 
            // rptLocationWiseEmp
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin,
            this.PageHeader,
            this.PageFooter});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.OFFCurrentMonthCal,
            this.SLCal,
            this.CalNetPay,
            this.CalEarning,
            this.CalDeduct,
            this.CalcForPeriodMonth,
            this.CalcForPeriodYear,
            this.CalcInPeriodMonth,
            this.CalcInPeriodYear,
            this.CalcDateTime,
            this.CalcNetPay,
            this.CalcForGetMonth,
            this.CalcInGetMonth,
            this.CalcForGetYear,
            this.CalcInGetYear});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.EmpWorklocationDS});
            this.DataMember = "EmpWorklocationQuery";
            this.DataSource = this.EmpWorklocationDS;
            this.Margins = new System.Drawing.Printing.Margins(5, 5, 0, 1);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.PrmWorkLocation,
            this.PrmSignature,
            this.PrmCompany});
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.EngMainDataStyle,
            this.ENGDataStyle,
            this.xrControlStyle1});
            this.Version = "23.2";
            ((System.ComponentModel.ISupportInitialize)(this.xrTable2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.XRControlStyle EngMainDataStyle;
        private DevExpress.XtraReports.UI.CalculatedField OFFCurrentMonthCal;
        private DevExpress.XtraReports.UI.CalculatedField SLCal;
        private DevExpress.XtraReports.UI.XRControlStyle ENGDataStyle;
        private DevExpress.XtraReports.UI.CalculatedField CalNetPay;
        private DevExpress.XtraReports.UI.CalculatedField CalEarning;
        private DevExpress.XtraReports.UI.CalculatedField CalDeduct;
        private DevExpress.XtraReports.UI.CalculatedField CalcForPeriodMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcForPeriodYear;
        private DevExpress.DataAccess.Sql.SqlDataSource EmpWorklocationDS;
        private DevExpress.XtraReports.UI.CalculatedField CalcInPeriodMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcInPeriodYear;
        private DevExpress.XtraReports.UI.CalculatedField CalcDateTime;
        private DevExpress.XtraReports.UI.CalculatedField CalcNetPay;
        private DevExpress.XtraReports.UI.XRTable xrTable1;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow1;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell2;
        private DevExpress.XtraReports.UI.CalculatedField CalcForGetMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcInGetMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcForGetYear;
        private DevExpress.XtraReports.UI.CalculatedField CalcInGetYear;
        private DevExpress.XtraReports.UI.PageHeaderBand PageHeader;
        private DevExpress.XtraReports.UI.XRControlStyle xrControlStyle1;
        private DevExpress.XtraReports.Parameters.Parameter PrmWorkLocation;
        private DevExpress.XtraReports.UI.XRLabel xrLabel3;
        private DevExpress.XtraReports.UI.XRTable xrTable2;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow3;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell25;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell4;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell6;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell7;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell10;
        private DevExpress.XtraReports.UI.PageFooterBand PageFooter;
        private DevExpress.XtraReports.UI.XRLabel xrLabel4;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel5;
        private DevExpress.XtraReports.UI.XRLabel xrLabel6;
        private DevExpress.XtraReports.UI.XRLabel xrLabel7;
        private DevExpress.XtraReports.UI.XRLabel lblMudirTitle;
        private DevExpress.XtraReports.UI.XRLabel xrLabel9;
        private DevExpress.XtraReports.UI.XRLabel xrLabel10;
        private DevExpress.XtraReports.Parameters.Parameter PrmSignature;
        private DevExpress.XtraReports.Parameters.Parameter PrmCompany;
    }
}
