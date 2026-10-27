namespace HCMv3.Reports.GovtForms
{
    partial class rptEndorsmentForVisitVisa
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
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings1 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            this.EmpDiscReportDS = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.lblNationalityAr2 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblCivilIdAr = new DevExpress.XtraReports.UI.XRLabel();
            this.lblFullNameAr2 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblNationalityEn = new DevExpress.XtraReports.UI.XRLabel();
            this.lblNationalityEn2 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblPassportAr = new DevExpress.XtraReports.UI.XRLabel();
            this.lblCivilIdEn = new DevExpress.XtraReports.UI.XRLabel();
            this.lblFullNameEn2 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblFullNameAr = new DevExpress.XtraReports.UI.XRLabel();
            this.lblNationalityAr = new DevExpress.XtraReports.UI.XRLabel();
            this.lblFullNameEn = new DevExpress.XtraReports.UI.XRLabel();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.PrmEmpCode = new DevExpress.XtraReports.Parameters.Parameter();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.xrControlStyle1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.PrmDate = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmLanguage = new DevExpress.XtraReports.Parameters.Parameter();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // EmpDiscReportDS
            // 
            this.EmpDiscReportDS.ConnectionName = HCMv3.Reports.HcmReportBase.DesignTimeConnectionName;
            this.EmpDiscReportDS.Name = "EmpDiscReportDS";
            customSqlQuery1.Name = "SalaryCertificate_LNDQuery";
            queryParameter1.Name = "PrmDate";
            queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmDate]", typeof(System.DateTime));
            queryParameter2.Name = "PrmEmpCode";
            queryParameter2.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter2.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmEmpCode]", typeof(string));
            customSqlQuery1.Parameters.Add(queryParameter1);
            customSqlQuery1.Parameters.Add(queryParameter2);
            customSqlQuery1.Sql = GovtFormsSqlFirstHalf.EndorsementForVisitVisaQuery;
            this.EmpDiscReportDS.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1});
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.lblNationalityAr2,
            this.lblCivilIdAr,
            this.lblFullNameAr2,
            this.lblNationalityEn,
            this.lblNationalityEn2,
            this.lblPassportAr,
            this.lblCivilIdEn,
            this.lblFullNameEn2,
            this.lblFullNameAr,
            this.lblNationalityAr,
            this.lblFullNameEn,
            this.xrPictureBox1});
            this.Detail.HeightF = 1078.375F;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // lblNationalityAr2
            // 
            this.lblNationalityAr2.CanGrow = false;
            this.lblNationalityAr2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NationalityAr]")});
            this.lblNationalityAr2.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNationalityAr2.LocationFloat = new DevExpress.Utils.PointFloat(204.1765F, 861.9167F);
            this.lblNationalityAr2.Name = "lblNationalityAr2";
            this.lblNationalityAr2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblNationalityAr2.SizeF = new System.Drawing.SizeF(388.5366F, 26.125F);
            this.lblNationalityAr2.StylePriority.UseFont = false;
            this.lblNationalityAr2.StylePriority.UseTextAlignment = false;
            this.lblNationalityAr2.Text = "lblNationalityAr";
            this.lblNationalityAr2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblCivilIdAr
            // 
            this.lblCivilIdAr.CanGrow = false;
            this.lblCivilIdAr.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CivilID]")});
            this.lblCivilIdAr.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCivilIdAr.LocationFloat = new DevExpress.Utils.PointFloat(204.1765F, 835.7917F);
            this.lblCivilIdAr.Name = "lblCivilIdAr";
            this.lblCivilIdAr.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblCivilIdAr.SizeF = new System.Drawing.SizeF(388.5366F, 26.12503F);
            this.lblCivilIdAr.StylePriority.UseFont = false;
            this.lblCivilIdAr.StylePriority.UseTextAlignment = false;
            this.lblCivilIdAr.Text = "lblCivilIdEn";
            this.lblCivilIdAr.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblFullNameAr2
            // 
            this.lblFullNameAr2.CanGrow = false;
            this.lblFullNameAr2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FullNameAr]")});
            this.lblFullNameAr2.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFullNameAr2.LocationFloat = new DevExpress.Utils.PointFloat(204.1765F, 809.6666F);
            this.lblFullNameAr2.Name = "lblFullNameAr2";
            this.lblFullNameAr2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblFullNameAr2.SizeF = new System.Drawing.SizeF(388.5367F, 26.125F);
            this.lblFullNameAr2.StylePriority.UseFont = false;
            this.lblFullNameAr2.StylePriority.UseTextAlignment = false;
            this.lblFullNameAr2.Text = "xrLabel2";
            this.lblFullNameAr2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblNationalityEn
            // 
            this.lblNationalityEn.CanGrow = false;
            this.lblNationalityEn.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NationalityEn]")});
            this.lblNationalityEn.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNationalityEn.LocationFloat = new DevExpress.Utils.PointFloat(111.4217F, 234F);
            this.lblNationalityEn.Name = "lblNationalityEn";
            this.lblNationalityEn.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblNationalityEn.SizeF = new System.Drawing.SizeF(101.0367F, 26.12502F);
            this.lblNationalityEn.StylePriority.UseFont = false;
            this.lblNationalityEn.StylePriority.UseTextAlignment = false;
            this.lblNationalityEn.Text = "xrLabel3";
            this.lblNationalityEn.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // lblNationalityEn2
            // 
            this.lblNationalityEn2.CanGrow = false;
            this.lblNationalityEn2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NationalityEn]")});
            this.lblNationalityEn2.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNationalityEn2.LocationFloat = new DevExpress.Utils.PointFloat(162.5098F, 861.9167F);
            this.lblNationalityEn2.Name = "lblNationalityEn2";
            this.lblNationalityEn2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblNationalityEn2.SizeF = new System.Drawing.SizeF(388.5367F, 26.125F);
            this.lblNationalityEn2.StylePriority.UseFont = false;
            this.lblNationalityEn2.StylePriority.UseTextAlignment = false;
            this.lblNationalityEn2.Text = "xrLabel3";
            this.lblNationalityEn2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // lblPassportAr
            // 
            this.lblPassportAr.CanGrow = false;
            this.lblPassportAr.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[PassportNum]")});
            this.lblPassportAr.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPassportAr.LocationFloat = new DevExpress.Utils.PointFloat(510.8958F, 130.2916F);
            this.lblPassportAr.Name = "lblPassportAr";
            this.lblPassportAr.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblPassportAr.SizeF = new System.Drawing.SizeF(117.7085F, 26.12502F);
            this.lblPassportAr.StylePriority.UseFont = false;
            this.lblPassportAr.StylePriority.UseTextAlignment = false;
            this.lblPassportAr.Text = "xrLabel3";
            this.lblPassportAr.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // lblCivilIdEn
            // 
            this.lblCivilIdEn.CanGrow = false;
            this.lblCivilIdEn.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CivilID]")});
            this.lblCivilIdEn.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCivilIdEn.LocationFloat = new DevExpress.Utils.PointFloat(162.5098F, 835.7917F);
            this.lblCivilIdEn.Name = "lblCivilIdEn";
            this.lblCivilIdEn.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblCivilIdEn.SizeF = new System.Drawing.SizeF(388.5366F, 26.12503F);
            this.lblCivilIdEn.StylePriority.UseFont = false;
            this.lblCivilIdEn.StylePriority.UseTextAlignment = false;
            this.lblCivilIdEn.Text = "lblCivilIdEn";
            this.lblCivilIdEn.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // lblFullNameEn2
            // 
            this.lblFullNameEn2.CanGrow = false;
            this.lblFullNameEn2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FullNameEn]")});
            this.lblFullNameEn2.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFullNameEn2.LocationFloat = new DevExpress.Utils.PointFloat(162.5098F, 809.6667F);
            this.lblFullNameEn2.Name = "lblFullNameEn2";
            this.lblFullNameEn2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblFullNameEn2.SizeF = new System.Drawing.SizeF(388.5367F, 26.125F);
            this.lblFullNameEn2.StylePriority.UseFont = false;
            this.lblFullNameEn2.StylePriority.UseTextAlignment = false;
            this.lblFullNameEn2.Text = "xrLabel2";
            this.lblFullNameEn2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // lblFullNameAr
            // 
            this.lblFullNameAr.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FullNameAr]")});
            this.lblFullNameAr.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFullNameAr.LocationFloat = new DevExpress.Utils.PointFloat(204.1765F, 105.5417F);
            this.lblFullNameAr.Name = "lblFullNameAr";
            this.lblFullNameAr.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblFullNameAr.SizeF = new System.Drawing.SizeF(247.9116F, 26.125F);
            this.lblFullNameAr.StylePriority.UseFont = false;
            this.lblFullNameAr.StylePriority.UseTextAlignment = false;
            this.lblFullNameAr.Text = "lblFullNameAr";
            this.lblFullNameAr.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblNationalityAr
            // 
            this.lblNationalityAr.CanGrow = false;
            this.lblNationalityAr.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NationalityAr]")});
            this.lblNationalityAr.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNationalityAr.LocationFloat = new DevExpress.Utils.PointFloat(22.9263F, 105.5417F);
            this.lblNationalityAr.Name = "lblNationalityAr";
            this.lblNationalityAr.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblNationalityAr.SizeF = new System.Drawing.SizeF(110.4168F, 26.12503F);
            this.lblNationalityAr.StylePriority.UseFont = false;
            this.lblNationalityAr.StylePriority.UseTextAlignment = false;
            this.lblNationalityAr.Text = "lblNationalityAr";
            this.lblNationalityAr.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblFullNameEn
            // 
            this.lblFullNameEn.CanGrow = false;
            this.lblFullNameEn.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FullNameEn]")});
            this.lblFullNameEn.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFullNameEn.LocationFloat = new DevExpress.Utils.PointFloat(450.005F, 210F);
            this.lblFullNameEn.Name = "lblFullNameEn";
            this.lblFullNameEn.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblFullNameEn.SizeF = new System.Drawing.SizeF(315.6248F, 26.12495F);
            this.lblFullNameEn.StylePriority.UseFont = false;
            this.lblFullNameEn.StylePriority.UseTextAlignment = false;
            this.lblFullNameEn.Text = "lblFullNameEn";
            this.lblFullNameEn.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(823F, 1077.08F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            // 
            // PrmEmpCode
            // 
            this.PrmEmpCode.Description = "EmpCode";
            this.PrmEmpCode.Name = "PrmEmpCode";
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
            this.BottomMargin.HeightF = 0.9583791F;
            this.BottomMargin.Name = "BottomMargin";
            this.BottomMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.BottomMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrControlStyle1
            // 
            this.xrControlStyle1.Name = "xrControlStyle1";
            this.xrControlStyle1.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            // 
            // PrmDate
            // 
            this.PrmDate.Description = "Date";
            this.PrmDate.Name = "PrmDate";
            this.PrmDate.Type = typeof(System.DateTime);
            // 
            // PrmLanguage
            // 
            this.PrmLanguage.Description = "Language";
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue("English", "English"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue("Arabic", "Arabic"));
            this.PrmLanguage.LookUpSettings = staticListLookUpSettings1;
            this.PrmLanguage.Name = "PrmLanguage";
            // 
            // rptEndorsmentForVisitVisa
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.EmpDiscReportDS});
            this.DataMember = "SalaryCertificate_LNDQuery";
            this.DataSource = this.EmpDiscReportDS;
            this.Margins = new System.Drawing.Printing.Margins(0, 1, 0, 1);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.PrmEmpCode,
            this.PrmDate,
            this.PrmLanguage});
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.xrControlStyle1});
            this.Version = "23.2";
            this.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.rptEndorsmentForVisitVisa_BeforePrint);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.XRControlStyle xrControlStyle1;
        private DevExpress.XtraReports.Parameters.Parameter PrmEmpCode;
        private DevExpress.DataAccess.Sql.SqlDataSource SalaryCertificate_LNDReportDS;
        private DevExpress.XtraReports.UI.XRLabel lblFullNameAr;
        private DevExpress.XtraReports.UI.XRLabel lblNationalityAr;
        private DevExpress.XtraReports.UI.XRLabel lblFullNameEn;
        private DevExpress.DataAccess.Sql.SqlDataSource EmpDiscReportDS;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox1;
        private DevExpress.XtraReports.UI.XRLabel lblFullNameEn2;
        private DevExpress.XtraReports.UI.XRLabel lblCivilIdEn;
        private DevExpress.XtraReports.Parameters.Parameter PrmDate;
        private DevExpress.XtraReports.UI.XRLabel lblNationalityEn;
        private DevExpress.XtraReports.UI.XRLabel lblNationalityEn2;
        private DevExpress.XtraReports.UI.XRLabel lblPassportAr;
        private DevExpress.XtraReports.UI.XRLabel lblFullNameAr2;
        private DevExpress.XtraReports.UI.XRLabel lblNationalityAr2;
        private DevExpress.XtraReports.UI.XRLabel lblCivilIdAr;
        private DevExpress.XtraReports.Parameters.Parameter PrmLanguage;
    }
}
