namespace HCMv3.Reports.Payroll
{
    partial class srptPayslipAttendenceSummaryAr
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
            DevExpress.DataAccess.Sql.QueryParameter queryParameterEmpCode = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameterForPeriod = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameterInPeriod = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameterRunType = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings1 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrTable2 = new DevExpress.XtraReports.UI.XRTable();
            this.xrTableRow6 = new DevExpress.XtraReports.UI.XRTableRow();
            this.xrTableCell21 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrTableCell23 = new DevExpress.XtraReports.UI.XRTableCell();
            this.xrLabel28 = new DevExpress.XtraReports.UI.XRLabel();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.GroupHeader1 = new DevExpress.XtraReports.UI.GroupHeaderBand();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.empcode = new DevExpress.XtraReports.Parameters.Parameter();
            this.InPeriod = new DevExpress.XtraReports.Parameters.Parameter();
            this.ForPeriod = new DevExpress.XtraReports.Parameters.Parameter();
            this.PayrollRuntype = new DevExpress.XtraReports.Parameters.Parameter();
            ((System.ComponentModel.ISupportInitialize)(this.xrTable2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrTable2});
            this.Detail.HeightF = 24.4555F;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrTable2
            // 
            this.xrTable2.BorderColor = System.Drawing.Color.DarkGray;
            this.xrTable2.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrTable2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrTable2.Name = "xrTable2";
            this.xrTable2.Rows.AddRange(new DevExpress.XtraReports.UI.XRTableRow[] {
            this.xrTableRow6});
            this.xrTable2.SizeF = new System.Drawing.SizeF(246.875F, 24.4555F);
            this.xrTable2.StylePriority.UseBorderColor = false;
            this.xrTable2.StylePriority.UseBorders = false;
            // 
            // xrTableRow6
            // 
            this.xrTableRow6.Cells.AddRange(new DevExpress.XtraReports.UI.XRTableCell[] {
            this.xrTableCell21,
            this.xrTableCell23});
            this.xrTableRow6.Name = "xrTableRow6";
            this.xrTableRow6.Weight = 1D;
            // 
            // xrTableCell21
            // 
            this.xrTableCell21.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Number]")});
            this.xrTableCell21.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrTableCell21.Name = "xrTableCell21";
            this.xrTableCell21.Padding = new DevExpress.XtraPrinting.PaddingInfo(10, 0, 0, 0, 100F);
            this.xrTableCell21.StylePriority.UseFont = false;
            this.xrTableCell21.StylePriority.UsePadding = false;
            this.xrTableCell21.StylePriority.UseTextAlignment = false;
            this.xrTableCell21.Text = "OFF";
            this.xrTableCell21.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrTableCell21.TextFormatString = "{0:0.000}";
            this.xrTableCell21.Weight = 0.74250239278505048D;
            // 
            // xrTableCell23
            // 
            this.xrTableCell23.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel28});
            this.xrTableCell23.Name = "xrTableCell23";
            this.xrTableCell23.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 5, 0, 0, 100F);
            this.xrTableCell23.StylePriority.UsePadding = false;
            this.xrTableCell23.Weight = 1.2018215376209622D;
            // 
            // xrLabel28
            // 
            this.xrLabel28.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel28.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Wage Type]")});
            this.xrLabel28.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel28.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrLabel28.Name = "xrLabel28";
            this.xrLabel28.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 5, 0, 0, 100F);
            this.xrLabel28.SizeF = new System.Drawing.SizeF(152.5979F, 24.4555F);
            this.xrLabel28.StylePriority.UseBorders = false;
            this.xrLabel28.StylePriority.UseFont = false;
            this.xrLabel28.StylePriority.UsePadding = false;
            this.xrLabel28.StylePriority.UseTextAlignment = false;
            this.xrLabel28.Text = "xrLabel28";
            this.xrLabel28.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
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
            this.BottomMargin.HeightF = 1F;
            this.BottomMargin.Name = "BottomMargin";
            this.BottomMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.BottomMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // sqlDataSource1
            // 
            this.sqlDataSource1.ConnectionName = HCMv3.Reports.HcmReportBase.DesignTimeConnectionName;
            this.sqlDataSource1.Name = "sqlDataSource1";
            customSqlQuery1.Name = "Query";
            customSqlQuery1.Sql = PayslipArSql.PayslipAttendanceQuery;
            queryParameterEmpCode.Name = "empcode";
            queryParameterEmpCode.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameterEmpCode.Value = new DevExpress.DataAccess.Expression("[Parameters.empcode]", typeof(string));
            queryParameterForPeriod.Name = "ForPeriod";
            queryParameterForPeriod.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameterForPeriod.Value = new DevExpress.DataAccess.Expression("[Parameters.ForPeriod]", typeof(System.DateTime));
            queryParameterInPeriod.Name = "InPeriod";
            queryParameterInPeriod.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameterInPeriod.Value = new DevExpress.DataAccess.Expression("[Parameters.InPeriod]", typeof(System.DateTime));
            queryParameterRunType.Name = "RunType";
            queryParameterRunType.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameterRunType.Value = new DevExpress.DataAccess.Expression("[Parameters.PayrollRuntype]", typeof(int));
            customSqlQuery1.Parameters.Add(queryParameterEmpCode);
            customSqlQuery1.Parameters.Add(queryParameterForPeriod);
            customSqlQuery1.Parameters.Add(queryParameterInPeriod);
            customSqlQuery1.Parameters.Add(queryParameterRunType);
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1});
            // 
            // GroupHeader1
            // 
            this.GroupHeader1.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel3});
            this.GroupHeader1.HeightF = 23.2639F;
            this.GroupHeader1.Name = "GroupHeader1";
            // 
            // xrLabel3
            // 
            this.xrLabel3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(100)))), ((int)(((byte)(160)))));
            this.xrLabel3.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel3.ForeColor = System.Drawing.Color.White;
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.SizeF = new System.Drawing.SizeF(246.875F, 22.99999F);
            this.xrLabel3.StylePriority.UseBackColor = false;
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.StylePriority.UseForeColor = false;
            this.xrLabel3.StylePriority.UseTextAlignment = false;
            this.xrLabel3.Text = "مغادرة الملخص";
            this.xrLabel3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.BottomCenter;
            // 
            // empcode
            // 
            this.empcode.Description = "empcode";
            this.empcode.Name = "empcode";
            // 
            // InPeriod
            // 
            this.InPeriod.Description = "InPeriod";
            this.InPeriod.Name = "InPeriod";
            this.InPeriod.Type = typeof(System.DateTime);
            // 
            // ForPeriod
            // 
            this.ForPeriod.Description = "ForPeriod";
            this.ForPeriod.Name = "ForPeriod";
            this.ForPeriod.Type = typeof(System.DateTime);
            // 
            // PayrollRuntype
            // 
            this.PayrollRuntype.Description = "Run Type";
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(0, "regular"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(1, "Off-Cycle"));
            this.PayrollRuntype.LookUpSettings = staticListLookUpSettings1;
            this.PayrollRuntype.Name = "PayrollRuntype";
            this.PayrollRuntype.Type = typeof(int);
            this.PayrollRuntype.ValueInfo = "0";
            // 
            // srptPayslipAttendenceSummaryAr
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin,
            this.GroupHeader1});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
            this.DataMember = "Query";
            this.DataSource = this.sqlDataSource1;
            this.Margins = new System.Drawing.Printing.Margins(0, 601, 0, 1);
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.empcode,
            this.InPeriod,
            this.ForPeriod,
            this.PayrollRuntype});
            this.Version = "23.2";
            ((System.ComponentModel.ISupportInitialize)(this.xrTable2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.DataAccess.Sql.SqlDataSource sqlDataSource1;
        private DevExpress.XtraReports.UI.GroupHeaderBand GroupHeader1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel3;
        private DevExpress.XtraReports.UI.XRTable xrTable2;
        private DevExpress.XtraReports.UI.XRTableRow xrTableRow6;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell21;
        private DevExpress.XtraReports.UI.XRTableCell xrTableCell23;
        private DevExpress.XtraReports.UI.XRLabel xrLabel28;
        private DevExpress.XtraReports.Parameters.Parameter empcode;
        private DevExpress.XtraReports.Parameters.Parameter InPeriod;
        private DevExpress.XtraReports.Parameters.Parameter ForPeriod;
        private DevExpress.XtraReports.Parameters.Parameter PayrollRuntype;
    }
}
