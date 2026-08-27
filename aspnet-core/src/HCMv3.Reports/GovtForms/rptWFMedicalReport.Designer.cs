namespace HCMv3.Reports.GovtForms
{
    partial class rptWFMedicalReport
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
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.lblResidencyNumber = new DevExpress.XtraReports.UI.XRLabel();
            this.lblDateTimeDay = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel8 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblAge = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.PrmDate = new DevExpress.XtraReports.Parameters.Parameter();
            this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.PrmEmpCode = new DevExpress.XtraReports.Parameters.Parameter();
            this.CalcDateMonthNum = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcDateMonthAr = new DevExpress.XtraReports.UI.CalculatedField();
            this.XrStyleBlack = new DevExpress.XtraReports.UI.XRControlStyle();
            this.XrStyleWhite = new DevExpress.XtraReports.UI.XRControlStyle();
            this.PrmCivilId = new DevExpress.XtraReports.Parameters.Parameter();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // Detail
            // 
            this.Detail.HeightF = 0F;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
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
            this.BottomMargin.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.lblResidencyNumber,
            this.lblDateTimeDay,
            this.xrLabel8,
            this.lblAge,
            this.xrLabel6,
            this.xrLabel4,
            this.xrLabel3,
            this.xrLabel2,
            this.xrLabel1,
            this.xrLabel5,
            this.xrPictureBox1});
            // v17.2 -> v23.2 BREAK. This form is drawn ENTIRELY inside the bottom-margin band
            // (Detail.HeightF is 0) and the legacy band height was 1172.417 on an A4 page whose
            // height is 1169 -- i.e. the margins were 3.4px taller than the sheet. v17.2 tolerated
            // that and clipped; v23.2 throws "Page margins are greater than page size" and renders
            // nothing at all. Reduced to 1168 so that TopMargin(0) + BottomMargin fits inside 1169.
            // The picture box below is clipped to match, NOT rescaled (its Sizing is the default
            // Normal), so every field on the scan stays exactly where the labels expect it and the
            // only loss is the bottom 4.4px of the image -- 0.4% of the page, already partly off
            // the sheet in the legacy report.
            this.BottomMargin.HeightF = 1168F;
            this.BottomMargin.Name = "BottomMargin";
            this.BottomMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.BottomMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // lblResidencyNumber
            // 
            this.lblResidencyNumber.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ResidencyNum]"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([ResidencyNum]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.lblResidencyNumber.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblResidencyNumber.LocationFloat = new DevExpress.Utils.PointFloat(460.2561F, 208.2639F);
            this.lblResidencyNumber.Name = "lblResidencyNumber";
            this.lblResidencyNumber.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblResidencyNumber.SizeF = new System.Drawing.SizeF(219.4906F, 36.0294F);
            this.lblResidencyNumber.StylePriority.UseFont = false;
            this.lblResidencyNumber.StylePriority.UseTextAlignment = false;
            this.lblResidencyNumber.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblDateTimeDay
            // 
            this.lblDateTimeDay.BorderColor = System.Drawing.Color.DarkGray;
            this.lblDateTimeDay.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblDateTimeDay.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters.PrmDate]")});
            this.lblDateTimeDay.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDateTimeDay.ForeColor = System.Drawing.Color.Black;
            this.lblDateTimeDay.LocationFloat = new DevExpress.Utils.PointFloat(480.959F, 334.6167F);
            this.lblDateTimeDay.Name = "lblDateTimeDay";
            this.lblDateTimeDay.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblDateTimeDay.SizeF = new System.Drawing.SizeF(188.9837F, 19.38705F);
            this.lblDateTimeDay.StylePriority.UseBorderColor = false;
            this.lblDateTimeDay.StylePriority.UseBorders = false;
            this.lblDateTimeDay.StylePriority.UseFont = false;
            this.lblDateTimeDay.StylePriority.UseForeColor = false;
            this.lblDateTimeDay.StylePriority.UsePadding = false;
            this.lblDateTimeDay.StylePriority.UseTextAlignment = false;
            this.lblDateTimeDay.Text = " ";
            this.lblDateTimeDay.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lblDateTimeDay.TextFormatString = "{0:yyyy/MM/dd}";
            // 
            // xrLabel8
            // 
            this.xrLabel8.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Fullname]"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Fullname]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel8.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel8.LocationFloat = new DevExpress.Utils.PointFloat(39.48151F, 342F);
            this.xrLabel8.Name = "xrLabel8";
            this.xrLabel8.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel8.SizeF = new System.Drawing.SizeF(189.307F, 25.43433F);
            this.xrLabel8.StylePriority.UseFont = false;
            this.xrLabel8.StylePriority.UseTextAlignment = false;
            this.xrLabel8.Text = "xrLabel5";
            this.xrLabel8.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblAge
            // 
            this.lblAge.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Age]"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Age]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.lblAge.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblAge.LocationFloat = new DevExpress.Utils.PointFloat(37.66665F, 284.8468F);
            this.lblAge.Name = "lblAge";
            this.lblAge.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblAge.SizeF = new System.Drawing.SizeF(138.9915F, 36.72977F);
            this.lblAge.StylePriority.UseFont = false;
            this.lblAge.StylePriority.UseTextAlignment = false;
            this.lblAge.Text = "xrLabel5";
            this.lblAge.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel6
            // 
            this.xrLabel6.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Jobtitle]"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Jobtitle]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel6.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(274.2144F, 284.8468F);
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(183.0391F, 36.72974F);
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UseTextAlignment = false;
            this.xrLabel6.Text = "xrLabel5";
            this.xrLabel6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel4
            // 
            this.xrLabel4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Last Name]"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Last Name]),\'IsNull\',[Last Name])")});
            this.xrLabel4.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(165.7561F, 245.9044F);
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(128.0417F, 37.16663F);
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UseTextAlignment = false;
            this.xrLabel4.Text = "xrLabel1";
            this.xrLabel4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel4.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel4_BeforePrint);
            // 
            // xrLabel3
            // 
            this.xrLabel3.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Third Name]"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Third Name]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel3.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(295.2769F, 245.9044F);
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.SizeF = new System.Drawing.SizeF(128.0417F, 37.16663F);
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.StylePriority.UseTextAlignment = false;
            this.xrLabel3.Text = "xrLabel1";
            this.xrLabel3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel2
            // 
            this.xrLabel2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Second Name]"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Second Name]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel2.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(424.6311F, 245.9044F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(128.3654F, 37.16663F);
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = "xrLabel1";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel1
            // 
            this.xrLabel1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[First Name]"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([First Name]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel1.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(554.059F, 245.9044F);
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(125.6943F, 37.16664F);
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.Text = "xrLabel1";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel5
            // 
            this.xrLabel5.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Nationality]"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Nationality]),\'XrStyleWhite\', \'XrStyleBlack\')")});
            this.xrLabel5.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(554.6937F, 284.8468F);
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.SizeF = new System.Drawing.SizeF(125.053F, 36.72974F);
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.StylePriority.UseTextAlignment = false;
            this.xrLabel5.Text = "xrLabel5";
            this.xrLabel5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(822F, 1168F);  // clipped with the band; see BottomMargin.HeightF
            // 
            // PrmDate
            // 
            this.PrmDate.Description = "Date";
            this.PrmDate.Name = "PrmDate";
            this.PrmDate.Type = typeof(System.DateTime);
            this.PrmDate.ValueInfo = "2017-07-11";
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
            customSqlQuery1.Sql = MedicalReportSql.Query;
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1});
            // 
            // PrmEmpCode
            // 
            this.PrmEmpCode.Description = "Emp Code";
            this.PrmEmpCode.Name = "PrmEmpCode";
            // 
            // CalcDateMonthNum
            // 
            this.CalcDateMonthNum.DataMember = "Query";
            this.CalcDateMonthNum.Expression = "GetMonth([Parameters].[PrmDate])";
            this.CalcDateMonthNum.Name = "CalcDateMonthNum";
            // 
            // CalcDateMonthAr
            // 
            this.CalcDateMonthAr.DataMember = "Query";
            this.CalcDateMonthAr.Expression = @"Iif([CalcDateMonthNum] == 1 ,'يناير',Iif([CalcDateMonthNum] == 2, 'فبراير' , Iif([CalcDateMonthNum] == 3, 'مارس' ,Iif([CalcDateMonthNum] = 4, 'أبريل' ,Iif([CalcDateMonthNum] == 5, 'مايو' ,Iif([CalcDateMonthNum] == 6, 'يونيو' ,Iif([CalcDateMonthNum] == 7, 'يوليو'  , Iif([CalcDateMonthNum] == 8, 'أغسطس' ,Iif([CalcDateMonthNum] == 9, 'سبتمبر' , Iif([CalcDateMonthNum] == 10, 'أكتوبر' , Iif([CalcDateMonthNum] == 11, 'نوفمبر' , Iif([CalcDateMonthNum] == 12, 'ديسمبر' ,'N/A' ))))))))))))";
            this.CalcDateMonthAr.Name = "CalcDateMonthAr";
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
            // rptWFMedicalReport
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.CalcDateMonthNum,
            this.CalcDateMonthAr});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
            this.DataMember = "Query";
            this.DataSource = this.sqlDataSource1;
            this.Margins = new System.Drawing.Printing.Margins(2, 3, 0, 1168);  // was 1172; see BottomMargin.HeightF
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.PrmEmpCode,
            this.PrmCivilId,
            this.PrmDate});
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.XrStyleBlack,
            this.XrStyleWhite});
            this.Version = "23.2";
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel4;
        private DevExpress.XtraReports.UI.XRLabel xrLabel3;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        private DevExpress.DataAccess.Sql.SqlDataSource sqlDataSource1;
        private DevExpress.XtraReports.Parameters.Parameter PrmEmpCode;
        private DevExpress.XtraReports.Parameters.Parameter PrmDate;
        private DevExpress.XtraReports.UI.XRLabel xrLabel8;
        private DevExpress.XtraReports.UI.XRLabel lblAge;
        private DevExpress.XtraReports.UI.XRLabel xrLabel6;
        private DevExpress.XtraReports.UI.XRLabel xrLabel5;
        private DevExpress.XtraReports.UI.XRLabel lblDateTimeDay;
        private DevExpress.XtraReports.UI.CalculatedField CalcDateMonthNum;
        private DevExpress.XtraReports.UI.CalculatedField CalcDateMonthAr;
        private DevExpress.XtraReports.UI.XRLabel lblResidencyNumber;
        private DevExpress.XtraReports.UI.XRControlStyle XrStyleBlack;
        private DevExpress.XtraReports.UI.XRControlStyle XrStyleWhite;
        private DevExpress.XtraReports.Parameters.Parameter PrmCivilId;
    }
}
