

namespace HCMv3.Reports.Others
{
    partial class rptEmpWorklocation
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
            DevExpress.XtraReports.UI.XRSummary xrSummary1 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            this.EmpWorklocationDS = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrTable2 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow3 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell3 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell24 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell25 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell4 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell5 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell6 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell9 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTable1 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow1 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell1 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell21 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell2 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell7 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell8 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell10 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell11 = new DevExpress.XtraReports.UI.XRTableCell();
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
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrControlStyle1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.PrmWorkLocation = new DevExpress.XtraReports.Parameters.Parameter();
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
            customSqlQuery1.Sql = OthersAdminSql.EmpWorklocationQuery;
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
            this.xrTable2.LocationFloat = new DevExpress.Utils.PointFloat(10F, 0F);
            this.xrTable2.Name = "xrTable2";
            this.xrTable2.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow3});
            this.xrTable2.SizeF = new System.Drawing.SizeF(780F, 25F);
            this.xrTable2.StylePriority.UseBorders = false;
            this.xrTable2.StylePriority.UseFont = false;
            this.xrTable2.StylePriority.UseTextAlignment = false;
            this.xrTable2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrTableRow3
            // 
            this.xrTableRow3.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell3,
            this.xrTableCell24,
            this.xrTableCell25,
            this.xrTableCell4,
            this.xrTableCell5,
            this.xrTableCell6,
            this.xrTableCell9});
            this.xrTableRow3.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableRow3.Name = "xrTableRow3";
            this.xrTableRow3.StylePriority.UseFont = false;
            this.xrTableRow3.Weight = 1D;
            // 
            // xrTableCell3
            // 
            this.xrTableCell3.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell3.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumRecordNumber([Code])")});
            this.xrTableCell3.Font = new System.Drawing.Font("Tahoma", 10.2F);
            this.xrTableCell3.Name = "xrTableCell3";
            this.xrTableCell3.StylePriority.UseBorders = false;
            this.xrTableCell3.StylePriority.UseFont = false;
            xrSummary1.Running = DevExpress.XtraReports.UI.SummaryRunning.Report;
            this.xrTableCell3.Summary = xrSummary1;
            this.xrTableCell3.Weight = 0.35198975500625751D;
            // 
            // xrTableCell24
            // 
            this.xrTableCell24.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell24.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Code]")});
            this.xrTableCell24.Font = new System.Drawing.Font("Tahoma", 10.2F);
            this.xrTableCell24.Name = "xrTableCell24";
            this.xrTableCell24.StylePriority.UseBorders = false;
            this.xrTableCell24.StylePriority.UseFont = false;
            this.xrTableCell24.Text = "xrTableCell24";
            this.xrTableCell24.Weight = 0.54026280583848663D;
            // 
            // xrTableCell25
            // 
            this.xrTableCell25.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell25.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FullNameEn]")});
            this.xrTableCell25.Font = new System.Drawing.Font("Tahoma", 10.2F);
            this.xrTableCell25.Name = "xrTableCell25";
            this.xrTableCell25.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 100F);
            this.xrTableCell25.StylePriority.UseBorders = false;
            this.xrTableCell25.StylePriority.UseFont = false;
            this.xrTableCell25.StylePriority.UsePadding = false;
            this.xrTableCell25.StylePriority.UseTextAlignment = false;
            this.xrTableCell25.Text = "xrTableCell25";
            this.xrTableCell25.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell25.Weight = 2.2633747324939479D;
            // 
            // xrTableCell4
            // 
            this.xrTableCell4.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CivilID]")});
            this.xrTableCell4.Font = new System.Drawing.Font("Tahoma", 10.2F);
            this.xrTableCell4.Name = "xrTableCell4";
            this.xrTableCell4.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 100F);
            this.xrTableCell4.StylePriority.UseBorders = false;
            this.xrTableCell4.StylePriority.UseFont = false;
            this.xrTableCell4.StylePriority.UsePadding = false;
            this.xrTableCell4.StylePriority.UseTextAlignment = false;
            this.xrTableCell4.Text = "xrTableCell4";
            this.xrTableCell4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrTableCell4.Weight = 1.3310111474201736D;
            // 
            // xrTableCell5
            // 
            this.xrTableCell5.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell5.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[JobTitleEn]")});
            this.xrTableCell5.Font = new System.Drawing.Font("Tahoma", 10.2F);
            this.xrTableCell5.Name = "xrTableCell5";
            this.xrTableCell5.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 100F);
            this.xrTableCell5.StylePriority.UseBorders = false;
            this.xrTableCell5.StylePriority.UseFont = false;
            this.xrTableCell5.StylePriority.UsePadding = false;
            this.xrTableCell5.StylePriority.UseTextAlignment = false;
            this.xrTableCell5.Text = "xrTableCell5";
            this.xrTableCell5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell5.Weight = 1.2687986069505928D;
            // 
            // xrTableCell6
            // 
            this.xrTableCell6.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell6.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NationalityEn]")});
            this.xrTableCell6.Font = new System.Drawing.Font("Tahoma", 10.2F);
            this.xrTableCell6.Name = "xrTableCell6";
            this.xrTableCell6.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 100F);
            this.xrTableCell6.StylePriority.UseBorders = false;
            this.xrTableCell6.StylePriority.UseFont = false;
            this.xrTableCell6.StylePriority.UsePadding = false;
            this.xrTableCell6.StylePriority.UseTextAlignment = false;
            this.xrTableCell6.Text = "xrTableCell6";
            this.xrTableCell6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrTableCell6.Weight = 1.118589900165551D;
            // 
            // xrTableCell9
            // 
            this.xrTableCell9.Borders = ((DevExpress.XtraPrinting.BorderSide)(((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTableCell9.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[LastDayInService]")});
            this.xrTableCell9.Font = new System.Drawing.Font("Tahoma", 10.2F);
            this.xrTableCell9.Name = "xrTableCell9";
            this.xrTableCell9.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 0, 0, 100F);
            this.xrTableCell9.StylePriority.UseBorders = false;
            this.xrTableCell9.StylePriority.UseFont = false;
            this.xrTableCell9.StylePriority.UsePadding = false;
            this.xrTableCell9.StylePriority.UseTextAlignment = false;
            this.xrTableCell9.Text = "xrTableCell9";
            this.xrTableCell9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell9.TextFormatString = "{0:dd/MM/yyyy}";
            this.xrTableCell9.Weight = 0.78788389182460639D;
            // 
            // xrTable1
            // 
            this.xrTable1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.xrTable1.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrTable1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTable1.LocationFloat = new DevExpress.Utils.PointFloat(10F, 45.16668F);
            this.xrTable1.Name = "xrTable1";
            this.xrTable1.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 5, 100F);
            this.xrTable1.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow1});
            this.xrTable1.SizeF = new System.Drawing.SizeF(780F, 24.99999F);
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
            this.xrTableCell1,
            this.xrTableCell21,
            this.xrTableCell2,
            this.xrTableCell7,
            this.xrTableCell8,
            this.xrTableCell10,
            this.xrTableCell11});
            this.xrTableRow1.Name = "xrTableRow1";
            this.xrTableRow1.StylePriority.UseBorders = false;
            this.xrTableRow1.Weight = 1D;
            // 
            // xrTableCell1
            // 
            this.xrTableCell1.Font = new System.Drawing.Font("Tahoma", 10.2F, System.Drawing.FontStyle.Bold);
            this.xrTableCell1.Name = "xrTableCell1";
            this.xrTableCell1.StylePriority.UseFont = false;
            this.xrTableCell1.StylePriority.UseTextAlignment = false;
            this.xrTableCell1.Text = "S.No";
            this.xrTableCell1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            this.xrTableCell1.Weight = 0.38295511606693133D;
            // 
            // xrTableCell21
            // 
            this.xrTableCell21.Font = new System.Drawing.Font("Tahoma", 10.2F, System.Drawing.FontStyle.Bold);
            this.xrTableCell21.Name = "xrTableCell21";
            this.xrTableCell21.StylePriority.UseFont = false;
            this.xrTableCell21.StylePriority.UseTextAlignment = false;
            this.xrTableCell21.Text = "Emp#";
            this.xrTableCell21.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            this.xrTableCell21.Weight = 0.58779108114800893D;
            // 
            // xrTableCell2
            // 
            this.xrTableCell2.Font = new System.Drawing.Font("Tahoma", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell2.Name = "xrTableCell2";
            this.xrTableCell2.StylePriority.UseFont = false;
            this.xrTableCell2.StylePriority.UseTextAlignment = false;
            this.xrTableCell2.Text = "Employee Name";
            this.xrTableCell2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            this.xrTableCell2.Weight = 2.4624892989914646D;
            // 
            // xrTableCell7
            // 
            this.xrTableCell7.Font = new System.Drawing.Font("Tahoma", 10.2F, System.Drawing.FontStyle.Bold);
            this.xrTableCell7.Name = "xrTableCell7";
            this.xrTableCell7.StylePriority.UseFont = false;
            this.xrTableCell7.StylePriority.UseTextAlignment = false;
            this.xrTableCell7.Text = "Civil Id";
            this.xrTableCell7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            this.xrTableCell7.Weight = 1.44810336567448D;
            // 
            // xrTableCell8
            // 
            this.xrTableCell8.Font = new System.Drawing.Font("Tahoma", 10.2F, System.Drawing.FontStyle.Bold);
            this.xrTableCell8.Name = "xrTableCell8";
            this.xrTableCell8.StylePriority.UseFont = false;
            this.xrTableCell8.StylePriority.UseTextAlignment = false;
            this.xrTableCell8.Text = "Job Title";
            this.xrTableCell8.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            this.xrTableCell8.Weight = 1.380417916682342D;
            // 
            // xrTableCell10
            // 
            this.xrTableCell10.Font = new System.Drawing.Font("Tahoma", 10.2F, System.Drawing.FontStyle.Bold);
            this.xrTableCell10.Name = "xrTableCell10";
            this.xrTableCell10.StylePriority.UseFont = false;
            this.xrTableCell10.StylePriority.UseTextAlignment = false;
            this.xrTableCell10.Text = "Nationality";
            this.xrTableCell10.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            this.xrTableCell10.Weight = 1.2169948780760449D;
            // 
            // xrTableCell11
            // 
            this.xrTableCell11.Font = new System.Drawing.Font("Tahoma", 10.2F, System.Drawing.FontStyle.Bold);
            this.xrTableCell11.Name = "xrTableCell11";
            this.xrTableCell11.StylePriority.UseFont = false;
            this.xrTableCell11.StylePriority.UseTextAlignment = false;
            this.xrTableCell11.Text = "Remarks";
            this.xrTableCell11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            this.xrTableCell11.Weight = 0.85719571215063151D;
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 2F;
            this.TopMargin.Name = "TopMargin";
            this.TopMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.TopMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.TopMargin.Visible = false;
            // 
            // BottomMargin
            // 
            this.BottomMargin.HeightF = 2F;
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
            this.xrLabel3,
            this.xrTable1});
            this.PageHeader.HeightF = 70.16669F;
            this.PageHeader.Name = "PageHeader";
            // 
            // xrLabel3
            // 
            this.xrLabel3.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[WL_en]")});
            this.xrLabel3.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(10F, 8.583348F);
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.SizeF = new System.Drawing.SizeF(780F, 23F);
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.StylePriority.UseTextAlignment = false;
            this.xrLabel3.Text = "Department";
            this.xrLabel3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
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
            // rptEmpWorklocation
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin,
            this.PageHeader});
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
            this.Margins = new System.Drawing.Printing.Margins(15, 12, 2, 2);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.PrmWorkLocation});
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
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell21;
        private DevExpress.XtraReports.UI.CalculatedField CalcForGetMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcInGetMonth;
        private DevExpress.XtraReports.UI.CalculatedField CalcForGetYear;
        private DevExpress.XtraReports.UI.CalculatedField CalcInGetYear;
        private DevExpress.XtraReports.UI.PageHeaderBand PageHeader;
        private DevExpress.XtraReports.UI.XRControlStyle xrControlStyle1;
        private DevExpress.XtraReports.Parameters.Parameter PrmWorkLocation;
        private DevExpress.XtraReports.UI.XRLabel xrLabel3;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell1;
        private DevExpress.XtraReports.UI.XRTable xrTable2;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow3;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell3;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell24;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell25;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell4;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell5;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell6;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell7;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell8;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell10;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell9;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell11;
    }
}
