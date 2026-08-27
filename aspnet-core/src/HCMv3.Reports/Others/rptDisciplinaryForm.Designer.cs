namespace HCMv3.Reports.Others
{
    partial class rptDisciplinaryForm
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
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            this.EmpDiscReportDS = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrSubreport1 = new DevExpress.XtraReports.UI.XRSubreport();
            this.PrmEmpCode = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmDiscType = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmRequestOid = new DevExpress.XtraReports.Parameters.Parameter();
            this.xrThirdCount = new DevExpress.XtraReports.UI.XRShape();
            this.xrFourthCount = new DevExpress.XtraReports.UI.XRShape();
            this.xrSecondCount = new DevExpress.XtraReports.UI.XRShape();
            this.xrFirstCount = new DevExpress.XtraReports.UI.XRShape();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.pbLast2 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.pbDED_16 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.pbDED_Warn = new DevExpress.XtraReports.UI.XRPictureBox();
            this.pblast = new DevExpress.XtraReports.UI.XRPictureBox();
            this.pbDED_24 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.pbDED_04 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.pbDED_Sep = new DevExpress.XtraReports.UI.XRPictureBox();
            this.pbDED_08 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.PrmDate = new DevExpress.XtraReports.Parameters.Parameter();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.xrControlStyle1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.PrmDiscCount = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmLastDiscDate = new DevExpress.XtraReports.Parameters.Parameter();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // EmpDiscReportDS
            // 
            this.EmpDiscReportDS.ConnectionName = HCMv3.Reports.HcmReportBase.DesignTimeConnectionName;
            this.EmpDiscReportDS.Name = "EmpDiscReportDS";
            customSqlQuery1.Name = "EmpDiscReportQuery";
            queryParameter1.Name = "PrmOid";
            queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmRequestOid]", typeof(int));
            customSqlQuery1.Parameters.Add(queryParameter1);
            customSqlQuery1.Sql = OthersHrFormsSql.DisciplinaryFormQuery;
            this.EmpDiscReportDS.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1});
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrSubreport1,
            this.xrThirdCount,
            this.xrFourthCount,
            this.xrSecondCount,
            this.xrFirstCount,
            this.xrLabel4,
            this.pbLast2,
            this.pbDED_16,
            this.pbDED_Warn,
            this.pblast,
            this.pbDED_24,
            this.pbDED_04,
            this.pbDED_Sep,
            this.pbDED_08,
            this.xrLabel1,
            this.xrLabel7,
            this.xrLabel6,
            this.xrLabel5,
            this.xrLabel2,
            this.xrPictureBox1});
            this.Detail.HeightF = 1169.5F;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrSubreport1
            // 
            this.xrSubreport1.LocationFloat = new DevExpress.Utils.PointFloat(26.41668F, 208.5F);
            this.xrSubreport1.Name = "xrSubreport1";
            this.xrSubreport1.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("sprmEmpCode", this.PrmEmpCode));
            this.xrSubreport1.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("sprmDiscType", this.PrmDiscType));
            this.xrSubreport1.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("sprmRequestId", this.PrmRequestOid));
            this.xrSubreport1.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("sprmDiscDate", null, "EmpDiscReportQuery.DisciplinaryDate"));
            // LEGACY: this.xrSubreport1.ReportSource = new srptDisciplinaryFormTotalCount();
            // srptDisciplinaryFormTotalCount is NOT part of this batch and has not been ported, so the
            // subreport is left without a ReportSource: it occupies its legacy position and size and
            // renders empty. See PORTING-NOTES-OthersHrForms.md (BLOCKED).
            this.xrSubreport1.SizeF = new System.Drawing.SizeF(339.5833F, 135.5F);
            // 
            // PrmEmpCode
            // 
            this.PrmEmpCode.Description = "EmpCode";
            this.PrmEmpCode.Name = "PrmEmpCode";
            // 
            // PrmDiscType
            // 
            this.PrmDiscType.Description = "Type";
            dynamicListLookUpSettings1.DataAdapter = null;
            dynamicListLookUpSettings1.DataMember = null;
            dynamicListLookUpSettings1.DataSource = this.EmpDiscReportDS;
            dynamicListLookUpSettings1.DisplayMember = "EmpDiscReportQuery.DiscTypeNameAr";
            dynamicListLookUpSettings1.ValueMember = "EmpDiscReportQuery.OID";
            this.PrmDiscType.LookUpSettings = dynamicListLookUpSettings1;
            this.PrmDiscType.Name = "PrmDiscType";
            this.PrmDiscType.Type = typeof(int);
            this.PrmDiscType.ValueInfo = "0";
            // 
            // PrmRequestOid
            // 
            this.PrmRequestOid.Description = "RequestOid";
            this.PrmRequestOid.Name = "PrmRequestOid";
            this.PrmRequestOid.Type = typeof(int);
            this.PrmRequestOid.ValueInfo = "0";
            // 
            // xrThirdCount
            // 
            this.xrThirdCount.LocationFloat = new DevExpress.Utils.PointFloat(568F, 196F);
            this.xrThirdCount.Name = "xrThirdCount";
            this.xrThirdCount.SizeF = new System.Drawing.SizeF(75F, 34.45833F);
            this.xrThirdCount.Visible = false;
            // 
            // xrFourthCount
            // 
            this.xrFourthCount.LocationFloat = new DevExpress.Utils.PointFloat(480.1666F, 196F);
            this.xrFourthCount.Name = "xrFourthCount";
            this.xrFourthCount.SizeF = new System.Drawing.SizeF(75F, 34.45833F);
            this.xrFourthCount.Visible = false;
            // 
            // xrSecondCount
            // 
            this.xrSecondCount.LocationFloat = new DevExpress.Utils.PointFloat(649.1877F, 196F);
            this.xrSecondCount.Name = "xrSecondCount";
            this.xrSecondCount.SizeF = new System.Drawing.SizeF(75F, 34.45833F);
            this.xrSecondCount.Visible = false;
            // 
            // xrFirstCount
            // 
            this.xrFirstCount.LocationFloat = new DevExpress.Utils.PointFloat(728F, 196F);
            this.xrFirstCount.Name = "xrFirstCount";
            this.xrFirstCount.SizeF = new System.Drawing.SizeF(64F, 34.45833F);
            this.xrFirstCount.Visible = false;
            // 
            // xrLabel4
            // 
            this.xrLabel4.CanGrow = false;
            this.xrLabel4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DiscTypeNameAr]")});
            this.xrLabel4.Font = new System.Drawing.Font("Tahoma", 12F);
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(372.75F, 143.8334F);
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(318.7502F, 23F);
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UseTextAlignment = false;
            this.xrLabel4.Text = "xrLabel3";
            this.xrLabel4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            this.xrLabel4.WordWrap = false;
            // 
            // pbLast2
            // 
            this.pbLast2.LocationFloat = new DevExpress.Utils.PointFloat(774F, 907.625F);
            this.pbLast2.Name = "pbLast2";
            this.pbLast2.SizeF = new System.Drawing.SizeF(16.5834F, 16.75F);
            this.pbLast2.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            this.pbLast2.Visible = false;
            // 
            // pbDED_16
            // 
            this.pbDED_16.LocationFloat = new DevExpress.Utils.PointFloat(774F, 882.625F);
            this.pbDED_16.Name = "pbDED_16";
            this.pbDED_16.SizeF = new System.Drawing.SizeF(16.5834F, 16.75F);
            this.pbDED_16.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            this.pbDED_16.Visible = false;
            // 
            // pbDED_Warn
            // 
            this.pbDED_Warn.LocationFloat = new DevExpress.Utils.PointFloat(774F, 855.2501F);
            this.pbDED_Warn.Name = "pbDED_Warn";
            this.pbDED_Warn.SizeF = new System.Drawing.SizeF(16.5834F, 16.75F);
            this.pbDED_Warn.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            this.pbDED_Warn.Visible = false;
            // 
            // pblast
            // 
            this.pblast.LocationFloat = new DevExpress.Utils.PointFloat(527.0833F, 907.625F);
            this.pblast.Name = "pblast";
            this.pblast.SizeF = new System.Drawing.SizeF(16.5834F, 16.75F);
            this.pblast.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            this.pblast.Visible = false;
            // 
            // pbDED_24
            // 
            this.pbDED_24.LocationFloat = new DevExpress.Utils.PointFloat(527.0833F, 880F);
            this.pbDED_24.Name = "pbDED_24";
            this.pbDED_24.SizeF = new System.Drawing.SizeF(16.5834F, 16.75F);
            this.pbDED_24.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            this.pbDED_24.Visible = false;
            // 
            // pbDED_04
            // 
            this.pbDED_04.LocationFloat = new DevExpress.Utils.PointFloat(527.0833F, 855.2501F);
            this.pbDED_04.Name = "pbDED_04";
            this.pbDED_04.SizeF = new System.Drawing.SizeF(16.5834F, 16.75F);
            this.pbDED_04.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            this.pbDED_04.Visible = false;
            // 
            // pbDED_Sep
            // 
            this.pbDED_Sep.LocationFloat = new DevExpress.Utils.PointFloat(292.7083F, 882.625F);
            this.pbDED_Sep.Name = "pbDED_Sep";
            this.pbDED_Sep.SizeF = new System.Drawing.SizeF(16.5834F, 16.75F);
            this.pbDED_Sep.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            this.pbDED_Sep.Visible = false;
            // 
            // pbDED_08
            // 
            this.pbDED_08.LocationFloat = new DevExpress.Utils.PointFloat(292.7083F, 855.2501F);
            this.pbDED_08.Name = "pbDED_08";
            this.pbDED_08.SizeF = new System.Drawing.SizeF(16.5834F, 16.75F);
            this.pbDED_08.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            this.pbDED_08.Visible = false;
            // 
            // xrLabel1
            // 
            this.xrLabel1.CanGrow = false;
            this.xrLabel1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[EmpName] + \' \' + [Code]")});
            this.xrLabel1.Font = new System.Drawing.Font("Tahoma", 13.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(372.75F, 91.83334F);
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(361.4168F, 23.00001F);
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.Text = "xrLabel1";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel1.WordWrap = false;
            // 
            // xrLabel7
            // 
            this.xrLabel7.CanGrow = false;
            this.xrLabel7.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters].[PrmDate]")});
            this.xrLabel7.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(26.41669F, 143.8334F);
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel7.SizeF = new System.Drawing.SizeF(159.9999F, 23.00002F);
            this.xrLabel7.StylePriority.UseFont = false;
            this.xrLabel7.StylePriority.UseTextAlignment = false;
            this.xrLabel7.Text = "xrLabel3";
            this.xrLabel7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel7.TextFormatString = "{0:dd/MM/yyyy}";
            this.xrLabel7.WordWrap = false;
            // 
            // xrLabel6
            // 
            this.xrLabel6.CanGrow = false;
            this.xrLabel6.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[WorkLocation]")});
            this.xrLabel6.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(26.41669F, 117.8334F);
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(243.3333F, 23.00001F);
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UseTextAlignment = false;
            this.xrLabel6.Text = "xrLabel3";
            this.xrLabel6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel6.WordWrap = false;
            // 
            // xrLabel5
            // 
            this.xrLabel5.CanGrow = false;
            this.xrLabel5.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[JobTitle]")});
            this.xrLabel5.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(26.41668F, 91.83334F);
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.SizeF = new System.Drawing.SizeF(243.3333F, 23.00001F);
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.StylePriority.UseTextAlignment = false;
            this.xrLabel5.Text = "xrLabel3";
            this.xrLabel5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel5.WordWrap = false;
            // 
            // xrLabel2
            // 
            this.xrLabel2.CanGrow = false;
            this.xrLabel2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CivilID]")});
            this.xrLabel2.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(372.75F, 116.8334F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(318.7502F, 22.99998F);
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = "xrLabel2";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel2.WordWrap = false;
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(827F, 1169F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            // 
            // PrmDate
            // 
            this.PrmDate.Description = "date";
            this.PrmDate.Name = "PrmDate";
            this.PrmDate.Type = typeof(System.DateTime);
            this.PrmDate.ValueInfo = "2018-03-31";
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 0F;
            this.TopMargin.Name = "TopMargin";
            this.TopMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.TopMargin.SnapLinePadding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.TopMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // BottomMargin
            // 
            this.BottomMargin.HeightF = 0F;
            this.BottomMargin.Name = "BottomMargin";
            this.BottomMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.BottomMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrControlStyle1
            // 
            this.xrControlStyle1.Name = "xrControlStyle1";
            this.xrControlStyle1.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            // 
            // PrmDiscCount
            // 
            this.PrmDiscCount.Description = "PrmDiscCount";
            this.PrmDiscCount.Name = "PrmDiscCount";
            this.PrmDiscCount.Type = typeof(int);
            this.PrmDiscCount.ValueInfo = "0";
            // 
            // PrmLastDiscDate
            // 
            this.PrmLastDiscDate.Description = "Parameter1";
            this.PrmLastDiscDate.Name = "PrmLastDiscDate";
            // 
            // rptDisciplinaryForm
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.EmpDiscReportDS});
            this.DataMember = "EmpDiscReportQuery";
            this.DataSource = this.EmpDiscReportDS;
            this.Margins = new System.Drawing.Printing.Margins(0, 0, 0, 0);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.PrmEmpCode,
            this.PrmDate,
            this.PrmDiscType,
            this.PrmRequestOid,
            this.PrmDiscCount,
            this.PrmLastDiscDate});
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.xrControlStyle1});
            this.Version = "23.2";
            this.Detail.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.Detail_BeforePrint);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.DataAccess.Sql.SqlDataSource EmpDiscReportDS;
        private DevExpress.XtraReports.UI.XRControlStyle xrControlStyle1;
        private DevExpress.XtraReports.Parameters.Parameter PrmEmpCode;
        private DevExpress.XtraReports.Parameters.Parameter PrmDate;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel7;
        private DevExpress.XtraReports.UI.XRLabel xrLabel6;
        private DevExpress.XtraReports.UI.XRLabel xrLabel5;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        public DevExpress.XtraReports.Parameters.Parameter PrmDiscType;
        private DevExpress.XtraReports.UI.XRPictureBox pbLast2;
        private DevExpress.XtraReports.UI.XRPictureBox pbDED_16;
        private DevExpress.XtraReports.UI.XRPictureBox pbDED_Warn;
        private DevExpress.XtraReports.UI.XRPictureBox pblast;
        private DevExpress.XtraReports.UI.XRPictureBox pbDED_24;
        private DevExpress.XtraReports.UI.XRPictureBox pbDED_04;
        private DevExpress.XtraReports.UI.XRPictureBox pbDED_Sep;
        private DevExpress.XtraReports.UI.XRPictureBox pbDED_08;
        private DevExpress.XtraReports.Parameters.Parameter PrmRequestOid;
        private DevExpress.XtraReports.UI.XRLabel xrLabel4;
        private DevExpress.XtraReports.Parameters.Parameter PrmDiscCount;
        private DevExpress.XtraReports.Parameters.Parameter PrmLastDiscDate;
        private DevExpress.XtraReports.UI.XRShape xrFourthCount;
        private DevExpress.XtraReports.UI.XRShape xrSecondCount;
        private DevExpress.XtraReports.UI.XRShape xrFirstCount;
        private DevExpress.XtraReports.UI.XRShape xrThirdCount;
        private DevExpress.XtraReports.UI.XRSubreport xrSubreport1;
    }
}
