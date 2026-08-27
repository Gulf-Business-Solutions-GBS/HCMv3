

namespace HCMv3.Reports.Payroll
{
    partial class rptPayrollSheetBySponsor
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
            DevExpress.DataAccess.Sql.QueryParameter queryParameter2 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter3 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter4 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery2 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery3 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings1 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings2 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            this.PayrollSheetDS = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrPivotGrid1 = new DevExpress.XtraReports.UI.XRPivotGrid();
            this.fieldSponsor1 = new DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField();
            this.fieldFileNumber = new DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField();
            this.fieldPaymentType1 = new DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField();
            this.fieldPayInterval1 = new DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField();
            this.fieldForPeriod1 = new DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField();
            this.fieldDepartmentEn1 = new DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField();
            this.fieldDepartmentAr1 = new DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField();
            this.fieldNumber1 = new DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField();
            this.fieldAmount1 = new DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField();
            this.PrmPayrollRunType = new DevExpress.XtraReports.Parameters.Parameter();
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
            this.CalcHeaders = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcNetPay = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcForGetMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcInGetMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcForGetYear = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcInGetYear = new DevExpress.XtraReports.UI.CalculatedField();
            this.PageHeader = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.xrLabel16 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblHeader = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel9 = new DevExpress.XtraReports.UI.XRLabel();
            this.ReportFooter = new DevExpress.XtraReports.UI.ReportFooterBand();
            this.xrLine1 = new DevExpress.XtraReports.UI.XRLine();
            this.PageFooter = new DevExpress.XtraReports.UI.PageFooterBand();
            this.xrPageInfo1 = new DevExpress.XtraReports.UI.XRPageInfo();
            this.PrmSheetDate = new DevExpress.XtraReports.Parameters.Parameter();
            this.xrControlStyle1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.ReportHeader = new DevExpress.XtraReports.UI.ReportHeaderBand();
            this.PrmPayrollArea = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmMonth = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmYear = new DevExpress.XtraReports.Parameters.Parameter();
            this.GroupHeader1 = new DevExpress.XtraReports.UI.GroupHeaderBand();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrPivotGridField1 = new DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField();
            this.xrPivotGridField2 = new DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // PayrollSheetDS
            // 
            this.PayrollSheetDS.ConnectionName = HCMv3.Reports.HcmReportBase.DesignTimeConnectionName;
            this.PayrollSheetDS.Name = "PayrollSheetDS";
            customSqlQuery1.Name = "PayrollSheetQuery";
            queryParameter1.Name = "PrmMonth";
            queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmMonth]", typeof(int));
            queryParameter2.Name = "PrmPayrollRunType";
            queryParameter2.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter2.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmPayrollRunType]", typeof(int));
            queryParameter3.Name = "PrmPayrollArea";
            queryParameter3.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter3.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmPayrollArea]", typeof(string));
            queryParameter4.Name = "PrmYear";
            queryParameter4.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter4.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmYear]", typeof(int));
            customSqlQuery1.Parameters.Add(queryParameter1);
            customSqlQuery1.Parameters.Add(queryParameter2);
            customSqlQuery1.Parameters.Add(queryParameter3);
            customSqlQuery1.Parameters.Add(queryParameter4);
            customSqlQuery1.Sql = PayrollSheetSql.PayrollSheetBySponsorQuery;
            customSqlQuery2.Name = "dsCompany";
            customSqlQuery2.Sql = PayrollSheetSql.CompanyLookup;
            customSqlQuery3.Name = "dsPayrollArea";
            customSqlQuery3.Sql = PayrollSheetSql.PayrollAreaLookup;
            this.PayrollSheetDS.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1,
            customSqlQuery2,
            customSqlQuery3});
            // 
            // Detail
            // 
            this.Detail.Expanded = false;
            this.Detail.HeightF = 68.75F;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrPivotGrid1
            // 
            this.xrPivotGrid1.Appearance.Cell.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.xrPivotGrid1.Appearance.CustomTotalCell.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.xrPivotGrid1.Appearance.FieldHeader.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.xrPivotGrid1.Appearance.FieldValue.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.xrPivotGrid1.Appearance.FieldValueGrandTotal.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.xrPivotGrid1.Appearance.FieldValueTotal.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.xrPivotGrid1.Appearance.GrandTotalCell.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.xrPivotGrid1.Appearance.Lines.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.xrPivotGrid1.Appearance.TotalCell.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.xrPivotGrid1.DataMember = "PayrollSheetQuery";
            this.xrPivotGrid1.DataSource = this.PayrollSheetDS;
            this.xrPivotGrid1.Fields.AddRange(new DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField[] {
            this.fieldSponsor1,
            this.fieldFileNumber,
            this.fieldPaymentType1,
            this.fieldPayInterval1,
            this.fieldForPeriod1,
            this.fieldDepartmentEn1,
            this.fieldDepartmentAr1,
            this.fieldNumber1,
            this.fieldAmount1});
            this.xrPivotGrid1.LocationFloat = new DevExpress.Utils.PointFloat(4.768372E-05F, 158.4376F);
            this.xrPivotGrid1.Name = "xrPivotGrid1";
            this.xrPivotGrid1.OLAPConnectionString = "";
            this.xrPivotGrid1.OptionsPrint.FilterSeparatorBarPadding = 3;
            this.xrPivotGrid1.OptionsPrint.PrintHeadersOnEveryPage = true;
            this.xrPivotGrid1.SizeF = new System.Drawing.SizeF(1159F, 83.75002F);
            // 
            // fieldSponsor1
            // 
            this.fieldSponsor1.AreaIndex = 0;
            this.fieldSponsor1.FieldName = "Sponsor";
            this.fieldSponsor1.Name = "fieldSponsor1";
            // 
            // fieldFileNumber
            // 
            this.fieldFileNumber.Area = DevExpress.XtraPivotGrid.PivotArea.RowArea;
            this.fieldFileNumber.AreaIndex = 1;
            this.fieldFileNumber.FieldName = "FileNumber";
            this.fieldFileNumber.Name = "fieldFileNumber";
            this.fieldFileNumber.Width = 250;
            // 
            // fieldPaymentType1
            // 
            this.fieldPaymentType1.Area = DevExpress.XtraPivotGrid.PivotArea.RowArea;
            this.fieldPaymentType1.AreaIndex = 0;
            this.fieldPaymentType1.FieldName = "PaymentType";
            this.fieldPaymentType1.Name = "fieldPaymentType1";
            this.fieldPaymentType1.Width = 80;
            // 
            // fieldPayInterval1
            // 
            this.fieldPayInterval1.Area = DevExpress.XtraPivotGrid.PivotArea.RowArea;
            this.fieldPayInterval1.AreaIndex = 2;
            this.fieldPayInterval1.FieldName = "PayInterval";
            this.fieldPayInterval1.Name = "fieldPayInterval1";
            // 
            // fieldForPeriod1
            // 
            this.fieldForPeriod1.AreaIndex = 1;
            this.fieldForPeriod1.FieldName = "ForPeriod";
            this.fieldForPeriod1.Name = "fieldForPeriod1";
            // 
            // fieldDepartmentEn1
            // 
            this.fieldDepartmentEn1.Area = DevExpress.XtraPivotGrid.PivotArea.ColumnArea;
            this.fieldDepartmentEn1.AreaIndex = 0;
            this.fieldDepartmentEn1.FieldName = "DepartmentEn";
            this.fieldDepartmentEn1.Name = "fieldDepartmentEn1";
            // 
            // fieldDepartmentAr1
            // 
            this.fieldDepartmentAr1.AreaIndex = 2;
            this.fieldDepartmentAr1.FieldName = "DepartmentAr";
            this.fieldDepartmentAr1.Name = "fieldDepartmentAr1";
            // 
            // fieldNumber1
            // 
            this.fieldNumber1.Area = DevExpress.XtraPivotGrid.PivotArea.DataArea;
            this.fieldNumber1.AreaIndex = 0;
            this.fieldNumber1.FieldName = "Number";
            this.fieldNumber1.Name = "fieldNumber1";
            this.fieldNumber1.Width = 60;
            // 
            // fieldAmount1
            // 
            this.fieldAmount1.Area = DevExpress.XtraPivotGrid.PivotArea.DataArea;
            this.fieldAmount1.AreaIndex = 1;
            this.fieldAmount1.FieldName = "Amount";
            this.fieldAmount1.Name = "fieldAmount1";
            this.fieldAmount1.Width = 70;
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
            this.BottomMargin.HeightF = 3.833262F;
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
            // CalcHeaders
            // 
            this.CalcHeaders.DataMember = "PayslipQuery";
            this.CalcHeaders.DisplayName = "CalcHeaders";
            this.CalcHeaders.Expression = PayrollSheetSql.CalcHeadersExpression;
            this.CalcHeaders.Name = "CalcHeaders";
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
            this.xrPivotGrid1,
            this.xrPictureBox1,
            this.xrLabel16,
            this.xrLabel4,
            this.xrLabel5,
            this.lblHeader,
            this.xrLabel9});
            this.PageHeader.HeightF = 242.1876F;
            this.PageHeader.Name = "PageHeader";
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Image", "[Companylogo]")});
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(960F, 13.12501F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(191.8077F, 94.37504F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            // 
            // xrLabel16
            // 
            this.xrLabel16.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel16.CanGrow = false;
            this.xrLabel16.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters].[PrmPayrollArea]")});
            this.xrLabel16.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel16.LocationFloat = new DevExpress.Utils.PointFloat(149.2098F, 13.12501F);
            this.xrLabel16.Name = "xrLabel16";
            this.xrLabel16.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel16.SizeF = new System.Drawing.SizeF(455.3566F, 25F);
            this.xrLabel16.StylePriority.UseBorders = false;
            this.xrLabel16.StylePriority.UseFont = false;
            this.xrLabel16.StylePriority.UsePadding = false;
            this.xrLabel16.StylePriority.UseTextAlignment = false;
            this.xrLabel16.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel4
            // 
            this.xrLabel4.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(3.973643E-05F, 50.62501F);
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(120.0432F, 23F);
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UseTextAlignment = false;
            this.xrLabel4.Text = "Date :";
            this.xrLabel4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel5
            // 
            this.xrLabel5.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters].[PrmSheetDate]")});
            this.xrLabel5.Font = new System.Drawing.Font("Tahoma", 10.2F, System.Drawing.FontStyle.Bold);
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(149.2098F, 50.62501F);
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.SizeF = new System.Drawing.SizeF(191.4109F, 23.00002F);
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.StylePriority.UseTextAlignment = false;
            this.xrLabel5.Text = "xrLabel3";
            this.xrLabel5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel5.TextFormatString = "{0:dd/MMM/yyyy}";
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(100)))), ((int)(((byte)(160)))));
            this.lblHeader.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.LocationFloat = new DevExpress.Utils.PointFloat(255.0293F, 84.50006F);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblHeader.SizeF = new System.Drawing.SizeF(648.9414F, 22.99999F);
            this.lblHeader.StylePriority.UseBackColor = false;
            this.lblHeader.StylePriority.UseFont = false;
            this.lblHeader.StylePriority.UseForeColor = false;
            this.lblHeader.StylePriority.UseTextAlignment = false;
            this.lblHeader.Text = "lblHeader";
            this.lblHeader.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.lblHeader.TextFormatString = "{0:MMMM, yyyy}";
            // 
            // xrLabel9
            // 
            this.xrLabel9.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel9.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel9.LocationFloat = new DevExpress.Utils.PointFloat(0F, 13.12501F);
            this.xrLabel9.Name = "xrLabel9";
            this.xrLabel9.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel9.SizeF = new System.Drawing.SizeF(120.0432F, 25F);
            this.xrLabel9.StylePriority.UseBorders = false;
            this.xrLabel9.StylePriority.UseFont = false;
            this.xrLabel9.StylePriority.UsePadding = false;
            this.xrLabel9.StylePriority.UseTextAlignment = false;
            this.xrLabel9.Text = "Payroll Area :";
            this.xrLabel9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // ReportFooter
            // 
            this.ReportFooter.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLine1});
            this.ReportFooter.Expanded = false;
            this.ReportFooter.HeightF = 27.50004F;
            this.ReportFooter.Name = "ReportFooter";
            // 
            // xrLine1
            // 
            this.xrLine1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 25F);
            this.xrLine1.Name = "xrLine1";
            this.xrLine1.SizeF = new System.Drawing.SizeF(1159F, 2.500032F);
            // 
            // PageFooter
            // 
            this.PageFooter.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrPageInfo1});
            this.PageFooter.HeightF = 23.33336F;
            this.PageFooter.Name = "PageFooter";
            // 
            // xrPageInfo1
            // 
            this.xrPageInfo1.Font = new System.Drawing.Font("Tahoma", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrPageInfo1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0.333341F);
            this.xrPageInfo1.Name = "xrPageInfo1";
            this.xrPageInfo1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrPageInfo1.SizeF = new System.Drawing.SizeF(1159F, 23F);
            this.xrPageInfo1.StylePriority.UseFont = false;
            this.xrPageInfo1.StylePriority.UseTextAlignment = false;
            this.xrPageInfo1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrPageInfo1.TextFormatString = "Page {0} of {1}";
            // 
            // PrmSheetDate
            // 
            this.PrmSheetDate.Description = "Print Date";
            this.PrmSheetDate.Name = "PrmSheetDate";
            this.PrmSheetDate.Type = typeof(System.DateTime);
            this.PrmSheetDate.ValueInfo = "2018-12-10";
            // 
            // xrControlStyle1
            // 
            this.xrControlStyle1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrControlStyle1.Name = "xrControlStyle1";
            // 
            // ReportHeader
            // 
            this.ReportHeader.HeightF = 0F;
            this.ReportHeader.Name = "ReportHeader";
            // 
            // PrmPayrollArea
            // 
            this.PrmPayrollArea.Description = "Payroll Area";
            dynamicListLookUpSettings1.DataAdapter = null;
            dynamicListLookUpSettings1.DataMember = "dsPayrollArea";
            dynamicListLookUpSettings1.DataSource = this.PayrollSheetDS;
            dynamicListLookUpSettings1.DisplayMember = "PayrollArea";
            dynamicListLookUpSettings1.ValueMember = "PayrollArea";
            this.PrmPayrollArea.LookUpSettings = dynamicListLookUpSettings1;
            this.PrmPayrollArea.Name = "PrmPayrollArea";
            // 
            // PrmMonth
            // 
            this.PrmMonth.Description = "Month";
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(1, "January"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(2, "February"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(3, "March"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(4, "April"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(5, "May"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(6, "June"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(7, "July"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(8, "August"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(9, "September"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(10, "October"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(11, "November"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(12, "December"));
            this.PrmMonth.LookUpSettings = staticListLookUpSettings2;
            this.PrmMonth.Name = "PrmMonth";
            this.PrmMonth.Type = typeof(int);
            this.PrmMonth.ValueInfo = "0";
            // 
            // PrmYear
            // 
            this.PrmYear.Description = "Year";
            this.PrmYear.Name = "PrmYear";
            this.PrmYear.Type = typeof(int);
            this.PrmYear.ValueInfo = "0";
            // 
            // GroupHeader1
            // 
            this.GroupHeader1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel2,
            this.xrLabel1});
            this.GroupHeader1.Expanded = false;
            this.GroupHeader1.GroupFields.AddRange(new DevExpress.XtraReports.UI.GroupField[] {
            new DevExpress.XtraReports.UI.GroupField("PaymentType", DevExpress.XtraReports.UI.XRColumnSortOrder.Ascending)});
            this.GroupHeader1.HeightF = 25.52083F;
            this.GroupHeader1.Name = "GroupHeader1";
            // 
            // xrLabel2
            // 
            this.xrLabel2.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel2.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Top | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel2.CanGrow = false;
            this.xrLabel2.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(4.768372E-05F, 0F);
            this.xrLabel2.Multiline = true;
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(137.915F, 25.52083F);
            this.xrLabel2.StylePriority.UseBorderColor = false;
            this.xrLabel2.StylePriority.UseBorders = false;
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UsePadding = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = "Payment Type :";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel2.Visible = false;
            // 
            // xrLabel1
            // 
            this.xrLabel1.BorderColor = System.Drawing.Color.DarkGray;
            this.xrLabel1.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Top | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel1.CanGrow = false;
            this.xrLabel1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[PaymentType]")});
            this.xrLabel1.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(137.915F, 0F);
            this.xrLabel1.Multiline = true;
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(1021.085F, 25.52083F);
            this.xrLabel1.StylePriority.UseBorderColor = false;
            this.xrLabel1.StylePriority.UseBorders = false;
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UsePadding = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel1.Visible = false;
            // 
            // xrPivotGridField1
            // 
            this.xrPivotGridField1.Area = DevExpress.XtraPivotGrid.PivotArea.ColumnArea;
            this.xrPivotGridField1.AreaIndex = 1;
            this.xrPivotGridField1.Name = "xrPivotGridField1";
            // 
            // xrPivotGridField2
            // 
            this.xrPivotGridField2.Area = DevExpress.XtraPivotGrid.PivotArea.RowArea;
            this.xrPivotGridField2.AreaIndex = 2;
            this.xrPivotGridField2.Name = "xrPivotGridField2";
            // 
            // rptPayrollSheetBySponsor
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin,
            this.PageHeader,
            this.ReportFooter,
            this.PageFooter,
            this.ReportHeader,
            this.GroupHeader1});
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
            this.CalcHeaders,
            this.CalcNetPay,
            this.CalcForGetMonth,
            this.CalcInGetMonth,
            this.CalcForGetYear,
            this.CalcInGetYear});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.PayrollSheetDS});
            this.DataMember = "PayrollSheetQuery";
            this.DataSource = this.PayrollSheetDS;
            this.Landscape = true;
            this.Margins = new System.Drawing.Printing.Margins(5, 5, 0, 4);
            this.PageHeight = 827;
            this.PageWidth = 1169;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.PrmPayrollArea,
            this.PrmMonth,
            this.PrmYear,
            this.PrmPayrollRunType,
            this.PrmSheetDate});
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.EngMainDataStyle,
            this.ENGDataStyle,
            this.xrControlStyle1});
            this.Version = "23.2";
            this.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.rptPayrollSheetSponsorWise_BeforePrint);
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
        private DevExpress.DataAccess.Sql.SqlDataSource PayrollSheetDS;
        private DevExpress.XtraReports.UI.CalculatedField CalcInPeriodMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcInPeriodYear;
        private DevExpress.XtraReports.UI.CalculatedField CalcDateTime;
        private DevExpress.XtraReports.UI.CalculatedField CalcHeaders;
        private DevExpress.XtraReports.UI.CalculatedField CalcNetPay;
        private DevExpress.XtraReports.Parameters.Parameter PrmPayrollRunType;
        private DevExpress.XtraReports.UI.CalculatedField CalcForGetMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcInGetMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcForGetYear;
        private DevExpress.XtraReports.UI.CalculatedField CalcInGetYear;
        private DevExpress.XtraReports.UI.PageHeaderBand PageHeader;
        private DevExpress.XtraReports.UI.XRLabel lblHeader;
        private DevExpress.XtraReports.UI.ReportFooterBand ReportFooter;
        private DevExpress.XtraReports.UI.XRLabel xrLabel4;
        private DevExpress.XtraReports.UI.XRLabel xrLabel5;
        private DevExpress.XtraReports.UI.PageFooterBand PageFooter;
        private DevExpress.XtraReports.UI.XRPageInfo xrPageInfo1;
        private DevExpress.XtraReports.Parameters.Parameter PrmSheetDate;
        private DevExpress.XtraReports.UI.XRLine xrLine1;
        private DevExpress.XtraReports.UI.XRControlStyle xrControlStyle1;
        private DevExpress.XtraReports.UI.ReportHeaderBand ReportHeader;
        private DevExpress.XtraReports.UI.XRLabel xrLabel16;
        private DevExpress.XtraReports.UI.XRLabel xrLabel9;
        private DevExpress.XtraReports.Parameters.Parameter PrmPayrollArea;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox1;
        private DevExpress.XtraReports.Parameters.Parameter PrmMonth;
        private DevExpress.XtraReports.Parameters.Parameter PrmYear;
        private DevExpress.XtraReports.UI.XRPivotGrid xrPivotGrid1;
        private DevExpress.XtraReports.UI.GroupHeaderBand GroupHeader1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        private DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField fieldPayInterval1;
        private DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField fieldDepartmentEn1;
        private DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField xrPivotGridField1;
        private DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField fieldNumber1;
        private DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField fieldAmount1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField xrPivotGridField2;
        private DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField fieldSponsor1;
        private DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField fieldPaymentType1;
        private DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField fieldForPeriod1;
        private DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField fieldDepartmentAr1;
        private DevExpress.XtraReports.UI.PivotGrid.XRPivotGridField fieldFileNumber;
    }
}
