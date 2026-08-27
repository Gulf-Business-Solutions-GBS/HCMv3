namespace HCMv3.Reports.Others
{
    partial class rptSalaryContinuousCertificate
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
            this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLabel28 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblMonthlySalary = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel26 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel24 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel23 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel22 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel21 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel20 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel13 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel8 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrPictureBox2 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.xrLabel19 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel18 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel16 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel25 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel12 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel14 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel11 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel10 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel9 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrRichText1 = new DevExpress.XtraReports.UI.XRRichText();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.PageHeader = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel17 = new DevExpress.XtraReports.UI.XRLabel();
            this.PrmEmpCode = new DevExpress.XtraReports.Parameters.Parameter();
            this.ReportHeader = new DevExpress.XtraReports.UI.ReportHeaderBand();
            this.xrLabel15 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.PrmDate = new DevExpress.XtraReports.Parameters.Parameter();
            ((System.ComponentModel.ISupportInitialize)(this.xrRichText1)).BeginInit();
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
            queryParameter2.Name = "PrmDate";
            queryParameter2.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter2.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmDate]", typeof(System.DateTime));
            customSqlQuery1.Parameters.Add(queryParameter1);
            customSqlQuery1.Parameters.Add(queryParameter2);
            customSqlQuery1.Sql = OthersCertificatesSql.SalaryContinuousCertificateEnQuery;
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1});
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel28,
            this.lblMonthlySalary,
            this.xrLabel26,
            this.xrLabel24,
            this.xrLabel23,
            this.xrLabel22,
            this.xrLabel21,
            this.xrLabel20,
            this.xrLabel13,
            this.xrLabel8,
            this.xrLabel1,
            this.xrPictureBox2,
            this.xrLabel19,
            this.xrLabel18,
            this.xrLabel16,
            this.xrLabel25,
            this.xrLabel12,
            this.xrLabel14,
            this.xrLabel3,
            this.xrLabel11,
            this.xrLabel10,
            this.xrLabel9,
            this.xrRichText1,
            this.xrLabel7,
            this.xrLabel6,
            this.xrLabel5,
            this.xrLabel4});
            this.Detail.HeightF = 866.0726F;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(15, 5, 5, 5, 100F);
            this.Detail.StylePriority.UsePadding = false;
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabel28
            // 
            this.xrLabel28.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNullOrEmpty([AccountNum]) ,\nIif(IsNullOrEmpty([IBAN]),\'\',[IBAN]  ) ,\n[Accou" +
                    "ntNum] )")});
            this.xrLabel28.Font = new System.Drawing.Font("Tahoma", 11F);
            this.xrLabel28.LocationFloat = new DevExpress.Utils.PointFloat(198.9365F, 288.2917F);
            this.xrLabel28.Name = "xrLabel28";
            this.xrLabel28.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel28.SizeF = new System.Drawing.SizeF(508.0634F, 23F);
            this.xrLabel28.StylePriority.UseFont = false;
            this.xrLabel28.Text = "xrLabel28";
            // 
            // lblMonthlySalary
            // 
            this.lblMonthlySalary.Font = new System.Drawing.Font("Tahoma", 11F);
            this.lblMonthlySalary.LocationFloat = new DevExpress.Utils.PointFloat(198.9365F, 213.9584F);
            this.lblMonthlySalary.Name = "lblMonthlySalary";
            this.lblMonthlySalary.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblMonthlySalary.SizeF = new System.Drawing.SizeF(508.0635F, 23.00002F);
            this.lblMonthlySalary.StylePriority.UseFont = false;
            this.lblMonthlySalary.Text = "lblMonthlySalary";
            // 
            // xrLabel26
            // 
            this.xrLabel26.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[EmpBank]")});
            this.xrLabel26.Font = new System.Drawing.Font("Tahoma", 11F);
            this.xrLabel26.LocationFloat = new DevExpress.Utils.PointFloat(198.9364F, 251.0417F);
            this.xrLabel26.Name = "xrLabel26";
            this.xrLabel26.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel26.SizeF = new System.Drawing.SizeF(508.0634F, 22.99995F);
            this.xrLabel26.StylePriority.UseFont = false;
            this.xrLabel26.Text = "xrLabel26";
            // 
            // xrLabel24
            // 
            this.xrLabel24.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CivilID]")});
            this.xrLabel24.Font = new System.Drawing.Font("Tahoma", 11F);
            this.xrLabel24.LocationFloat = new DevExpress.Utils.PointFloat(198.9364F, 145.4583F);
            this.xrLabel24.Name = "xrLabel24";
            this.xrLabel24.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel24.SizeF = new System.Drawing.SizeF(508.0636F, 22.99998F);
            this.xrLabel24.StylePriority.UseFont = false;
            this.xrLabel24.Text = "xrLabel24";
            // 
            // xrLabel23
            // 
            this.xrLabel23.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel23.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel23.LocationFloat = new DevExpress.Utils.PointFloat(181.5833F, 288.2917F);
            this.xrLabel23.Name = "xrLabel23";
            this.xrLabel23.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel23.SizeF = new System.Drawing.SizeF(7.312714F, 22.91669F);
            this.xrLabel23.StylePriority.UseBorders = false;
            this.xrLabel23.StylePriority.UseFont = false;
            this.xrLabel23.Text = ":";
            // 
            // xrLabel22
            // 
            this.xrLabel22.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel22.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel22.LocationFloat = new DevExpress.Utils.PointFloat(181.9583F, 251.0417F);
            this.xrLabel22.Name = "xrLabel22";
            this.xrLabel22.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel22.SizeF = new System.Drawing.SizeF(7.312714F, 22.91667F);
            this.xrLabel22.StylePriority.UseBorders = false;
            this.xrLabel22.StylePriority.UseFont = false;
            this.xrLabel22.Text = ":";
            // 
            // xrLabel21
            // 
            this.xrLabel21.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel21.LocationFloat = new DevExpress.Utils.PointFloat(0F, 288.29F);
            this.xrLabel21.Name = "xrLabel21";
            this.xrLabel21.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel21.SizeF = new System.Drawing.SizeF(181.5833F, 22.91663F);
            this.xrLabel21.StylePriority.UseFont = false;
            this.xrLabel21.Text = "Account number / IBAN";
            // 
            // xrLabel20
            // 
            this.xrLabel20.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel20.LocationFloat = new DevExpress.Utils.PointFloat(0F, 251.0417F);
            this.xrLabel20.Name = "xrLabel20";
            this.xrLabel20.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel20.SizeF = new System.Drawing.SizeF(181.4722F, 22.91666F);
            this.xrLabel20.StylePriority.UseFont = false;
            this.xrLabel20.Text = "Bank Name";
            // 
            // xrLabel13
            // 
            this.xrLabel13.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel13.LocationFloat = new DevExpress.Utils.PointFloat(0F, 146.3735F);
            this.xrLabel13.Name = "xrLabel13";
            this.xrLabel13.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel13.SizeF = new System.Drawing.SizeF(181.361F, 22.91667F);
            this.xrLabel13.StylePriority.UseFont = false;
            this.xrLabel13.Text = "Civil ID #";
            // 
            // xrLabel8
            // 
            this.xrLabel8.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel8.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel8.LocationFloat = new DevExpress.Utils.PointFloat(181.5417F, 74.04022F);
            this.xrLabel8.Name = "xrLabel8";
            this.xrLabel8.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel8.SizeF = new System.Drawing.SizeF(7.312683F, 22.91666F);
            this.xrLabel8.StylePriority.UseBorders = false;
            this.xrLabel8.StylePriority.UseFont = false;
            this.xrLabel8.Text = ":";
            // 
            // xrLabel1
            // 
            this.xrLabel1.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel1.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(181.5417F, 109.1236F);
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(7.312714F, 22.91666F);
            this.xrLabel1.StylePriority.UseBorders = false;
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.Text = ":";
            // 
            // xrPictureBox2
            // 
            this.xrPictureBox2.LocationFloat = new DevExpress.Utils.PointFloat(626.9583F, 798.9058F);
            this.xrPictureBox2.Name = "xrPictureBox2";
            this.xrPictureBox2.SizeF = new System.Drawing.SizeF(76.04169F, 57.16675F);
            this.xrPictureBox2.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            // 
            // xrLabel19
            // 
            this.xrLabel19.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "\'Copy : File \' + [Parameters].[PrmEmpCode]")});
            this.xrLabel19.Font = new System.Drawing.Font("Tahoma", 11F);
            this.xrLabel19.LocationFloat = new DevExpress.Utils.PointFloat(23.47813F, 833.7808F);
            this.xrLabel19.Name = "xrLabel19";
            this.xrLabel19.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel19.SizeF = new System.Drawing.SizeF(231.5186F, 22.29169F);
            this.xrLabel19.StylePriority.UseFont = false;
            this.xrLabel19.Text = "Copy: ID file ";
            this.xrLabel19.TextFormatString = "{0}";
            // 
            // xrLabel18
            // 
            this.xrLabel18.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel18.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel18.LocationFloat = new DevExpress.Utils.PointFloat(181.5417F, 214.0417F);
            this.xrLabel18.Name = "xrLabel18";
            this.xrLabel18.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel18.SizeF = new System.Drawing.SizeF(7.312714F, 22.91666F);
            this.xrLabel18.StylePriority.UseBorders = false;
            this.xrLabel18.StylePriority.UseFont = false;
            this.xrLabel18.Text = ":";
            // 
            // xrLabel16
            // 
            this.xrLabel16.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel16.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel16.LocationFloat = new DevExpress.Utils.PointFloat(181.5417F, 179.6667F);
            this.xrLabel16.Name = "xrLabel16";
            this.xrLabel16.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel16.SizeF = new System.Drawing.SizeF(7.312714F, 22.91666F);
            this.xrLabel16.StylePriority.UseBorders = false;
            this.xrLabel16.StylePriority.UseFont = false;
            this.xrLabel16.Text = ":";
            // 
            // xrLabel25
            // 
            this.xrLabel25.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel25.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel25.LocationFloat = new DevExpress.Utils.PointFloat(181.5417F, 145.5417F);
            this.xrLabel25.Name = "xrLabel25";
            this.xrLabel25.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel25.SizeF = new System.Drawing.SizeF(7.312714F, 22.91666F);
            this.xrLabel25.StylePriority.UseBorders = false;
            this.xrLabel25.StylePriority.UseFont = false;
            this.xrLabel25.Text = ":";
            // 
            // xrLabel12
            // 
            this.xrLabel12.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[JoiningDate]")});
            this.xrLabel12.Font = new System.Drawing.Font("Tahoma", 11F);
            this.xrLabel12.LocationFloat = new DevExpress.Utils.PointFloat(198.9365F, 181.3735F);
            this.xrLabel12.Name = "xrLabel12";
            this.xrLabel12.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel12.SizeF = new System.Drawing.SizeF(136.6019F, 23.00002F);
            this.xrLabel12.StylePriority.UseFont = false;
            this.xrLabel12.Text = "xrLabel12";
            this.xrLabel12.TextFormatString = "{0:dd/MM/yyyy}";
            // 
            // xrLabel14
            // 
            this.xrLabel14.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Jobtitle]")});
            this.xrLabel14.Font = new System.Drawing.Font("Tahoma", 11F);
            this.xrLabel14.LocationFloat = new DevExpress.Utils.PointFloat(198.9365F, 110.2068F);
            this.xrLabel14.Name = "xrLabel14";
            this.xrLabel14.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel14.SizeF = new System.Drawing.SizeF(508.0634F, 22.99999F);
            this.xrLabel14.StylePriority.UseFont = false;
            this.xrLabel14.Text = "xrLabel14";
            // 
            // xrLabel3
            // 
            this.xrLabel3.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(0F, 74.04021F);
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.SizeF = new System.Drawing.SizeF(180.9165F, 22.91666F);
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.Text = "Name ";
            // 
            // xrLabel11
            // 
            this.xrLabel11.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel11.LocationFloat = new DevExpress.Utils.PointFloat(0F, 214.29F);
            this.xrLabel11.Name = "xrLabel11";
            this.xrLabel11.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel11.SizeF = new System.Drawing.SizeF(181.2499F, 22.91664F);
            this.xrLabel11.StylePriority.UseFont = false;
            this.xrLabel11.Text = "Monthly Salary";
            // 
            // xrLabel10
            // 
            this.xrLabel10.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel10.LocationFloat = new DevExpress.Utils.PointFloat(0F, 180.2276F);
            this.xrLabel10.Name = "xrLabel10";
            this.xrLabel10.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel10.SizeF = new System.Drawing.SizeF(181.1388F, 22.91666F);
            this.xrLabel10.StylePriority.UseFont = false;
            this.xrLabel10.Text = "Date of Joining";
            // 
            // xrLabel9
            // 
            this.xrLabel9.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel9.LocationFloat = new DevExpress.Utils.PointFloat(0F, 110.3735F);
            this.xrLabel9.Name = "xrLabel9";
            this.xrLabel9.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel9.SizeF = new System.Drawing.SizeF(181.0276F, 22.91667F);
            this.xrLabel9.StylePriority.UseFont = false;
            this.xrLabel9.Text = "Title";
            // 
            // xrRichText1
            // 
            this.xrRichText1.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrRichText1.LocationFloat = new DevExpress.Utils.PointFloat(21.74204F, 631.179F);
            this.xrRichText1.Name = "xrRichText1";
            this.xrRichText1.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.xrRichText1.SerializableRtfString = OthersCertificatesResources.SignatureRtf;
            this.xrRichText1.SizeF = new System.Drawing.SizeF(434.1435F, 70.61108F);
            this.xrRichText1.StylePriority.UseFont = false;
            this.xrRichText1.StylePriority.UsePadding = false;
            // 
            // xrLabel7
            // 
            this.xrLabel7.Font = new System.Drawing.Font("Tahoma", 11F);
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(0F, 422.5403F);
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel7.SizeF = new System.Drawing.SizeF(706.9998F, 28.47226F);
            this.xrLabel7.StylePriority.UseFont = false;
            this.xrLabel7.StylePriority.UseTextAlignment = false;
            this.xrLabel7.Text = "Note: This certificate is valid for three months from the issue date. ";
            this.xrLabel7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopJustify;
            // 
            // xrLabel6
            // 
            this.xrLabel6.Font = new System.Drawing.Font("Tahoma", 11F);
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(0F, 361.2067F);
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(707F, 52.33353F);
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UseTextAlignment = false;
            this.xrLabel6.Text = "This certificate is issued on behalf of the employee’s request and bears no finan" +
    "cial responsibility on or behalf of any of the employer and authorized signatori" +
    "es.";
            this.xrLabel6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopJustify;
            // 
            // xrLabel5
            // 
            this.xrLabel5.Font = new System.Drawing.Font("Tahoma", 11F);
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(0F, 14.67902F);
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.SizeF = new System.Drawing.SizeF(706.9999F, 42.36115F);
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.StylePriority.UseTextAlignment = false;
            this.xrLabel5.Text = "Kuwait Specialized Eye Center certifies that below details of employee and eligib" +
    "ility of total continuous monthly salary as per our records;";
            this.xrLabel5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopJustify;
            // 
            // xrLabel4
            // 
            this.xrLabel4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FullNameEn]")});
            this.xrLabel4.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(198.9364F, 74.04022F);
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(508.0634F, 22.99998F);
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.Text = "xrLabel4";
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 0F;
            this.TopMargin.Name = "TopMargin";
            this.TopMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 5, 5, 100F);
            this.TopMargin.StylePriority.UsePadding = false;
            this.TopMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // BottomMargin
            // 
            this.BottomMargin.HeightF = 0F;
            this.BottomMargin.Name = "BottomMargin";
            this.BottomMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 5, 5, 100F);
            this.BottomMargin.StylePriority.UsePadding = false;
            this.BottomMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // PageHeader
            // 
            this.PageHeader.HeightF = 0F;
            this.PageHeader.Name = "PageHeader";
            this.PageHeader.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 5, 5, 100F);
            this.PageHeader.StylePriority.UsePadding = false;
            // 
            // xrLabel2
            // 
            this.xrLabel2.Font = new System.Drawing.Font("Tahoma", 11F);
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(547.6805F, 200.9848F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(52.12506F, 22.99998F);
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = "Date : ";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            // 
            // xrLabel17
            // 
            this.xrLabel17.Font = new System.Drawing.Font("Tahoma", 16F);
            this.xrLabel17.ForeColor = System.Drawing.Color.Black;
            this.xrLabel17.LocationFloat = new DevExpress.Utils.PointFloat(0F, 223.9848F);
            this.xrLabel17.Name = "xrLabel17";
            this.xrLabel17.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel17.SizeF = new System.Drawing.SizeF(706.9999F, 31.10783F);
            this.xrLabel17.StylePriority.UseFont = false;
            this.xrLabel17.StylePriority.UseForeColor = false;
            this.xrLabel17.StylePriority.UseTextAlignment = false;
            this.xrLabel17.Text = "SALARY CONTINUOUS CERTIFICATE";
            this.xrLabel17.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // PrmEmpCode
            // 
            this.PrmEmpCode.Description = "Employee Code";
            this.PrmEmpCode.Name = "PrmEmpCode";
            // 
            // ReportHeader
            // 
            this.ReportHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel15,
            this.xrLabel2,
            this.xrLabel17,
            this.xrPictureBox1});
            this.ReportHeader.HeightF = 255.0926F;
            this.ReportHeader.Name = "ReportHeader";
            this.ReportHeader.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 5, 5, 100F);
            this.ReportHeader.StylePriority.UsePadding = false;
            // 
            // xrLabel15
            // 
            this.xrLabel15.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters].[PrmDate]")});
            this.xrLabel15.Font = new System.Drawing.Font("Tahoma", 11F);
            this.xrLabel15.LocationFloat = new DevExpress.Utils.PointFloat(599.8057F, 200.9848F);
            this.xrLabel15.Name = "xrLabel15";
            this.xrLabel15.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel15.SizeF = new System.Drawing.SizeF(107.1942F, 22.99995F);
            this.xrLabel15.StylePriority.UseFont = false;
            this.xrLabel15.StylePriority.UseTextAlignment = false;
            this.xrLabel15.Text = "xrLabel15";
            this.xrLabel15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabel15.TextFormatString = "{0:dd/MM/yyyy}";
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Image", "[Companylogo]")});
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(24.14481F, 10.00001F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(324.0035F, 90.63765F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.AutoSize;
            this.xrPictureBox1.Visible = false;
            // 
            // PrmDate
            // 
            this.PrmDate.Description = "Date";
            this.PrmDate.Name = "PrmDate";
            this.PrmDate.Type = typeof(System.DateTime);
            // 
            // rptSalaryContinuousCertificate
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin,
            this.PageHeader,
            this.ReportHeader});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
            this.DataMember = "Query";
            this.DataSource = this.sqlDataSource1;
            this.Margins = new System.Drawing.Printing.Margins(52, 68, 0, 0);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.PrmEmpCode,
            this.PrmDate});
            this.Version = "23.2";
            // Re-wired from report-level BeforePrint to the Detail band: report-level BeforePrint fires
            // once, before any data row is current, so GetCurrentColumnValue there returns nothing.
            this.Detail.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.Detail_BeforePrint);
            ((System.ComponentModel.ISupportInitialize)(this.xrRichText1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.DataAccess.Sql.SqlDataSource sqlDataSource1;
        private DevExpress.XtraReports.UI.PageHeaderBand PageHeader;
        private DevExpress.XtraReports.UI.XRLabel xrLabel17;
        private DevExpress.XtraReports.UI.XRLabel xrLabel11;
        private DevExpress.XtraReports.UI.XRLabel xrLabel10;
        private DevExpress.XtraReports.UI.XRLabel xrLabel9;
        private DevExpress.XtraReports.UI.XRRichText xrRichText1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel7;
        private DevExpress.XtraReports.UI.XRLabel xrLabel6;
        private DevExpress.XtraReports.UI.XRLabel xrLabel5;
        private DevExpress.XtraReports.UI.XRLabel xrLabel3;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.Parameters.Parameter PrmEmpCode;
        private DevExpress.XtraReports.UI.XRLabel xrLabel14;
        private DevExpress.XtraReports.UI.XRLabel xrLabel4;
        private DevExpress.XtraReports.UI.ReportHeaderBand ReportHeader;
        private DevExpress.XtraReports.UI.XRLabel xrLabel18;
        private DevExpress.XtraReports.UI.XRLabel xrLabel16;
        private DevExpress.XtraReports.UI.XRLabel xrLabel25;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel15;
        private DevExpress.XtraReports.Parameters.Parameter PrmDate;
        private DevExpress.XtraReports.UI.XRLabel xrLabel19;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox2;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel21;
        private DevExpress.XtraReports.UI.XRLabel xrLabel20;
        private DevExpress.XtraReports.UI.XRLabel xrLabel13;
        private DevExpress.XtraReports.UI.XRLabel xrLabel8;
        private DevExpress.XtraReports.UI.XRLabel xrLabel12;
        private DevExpress.XtraReports.UI.XRLabel xrLabel23;
        private DevExpress.XtraReports.UI.XRLabel xrLabel22;
        private DevExpress.XtraReports.UI.XRLabel xrLabel28;
        private DevExpress.XtraReports.UI.XRLabel lblMonthlySalary;
        private DevExpress.XtraReports.UI.XRLabel xrLabel26;
        private DevExpress.XtraReports.UI.XRLabel xrLabel24;
    }
}
