namespace HCMv3.Reports.Others
{
    partial class rptExperienceCertificate
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
            this.sqlDataSource1 = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLabel43 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLine15 = new DevExpress.XtraReports.UI.XRLine();
            this.xrLabel44 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel29 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel28 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel27 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel26 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel11 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel10 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel32 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel33 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel8 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel17 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel18 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel30 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel31 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel22 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel24 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel25 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel9 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel16 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLine3 = new DevExpress.XtraReports.UI.XRLine();
            this.xrLabel19 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel20 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel21 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel23 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLine1 = new DevExpress.XtraReports.UI.XRLine();
            this.xrLabel15 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel14 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel13 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel12 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblDateAr = new DevExpress.XtraReports.UI.XRLabel();
            this.PrmDate = new DevExpress.XtraReports.Parameters.Parameter();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.Title = new DevExpress.XtraReports.UI.XRControlStyle();
            this.FieldCaption = new DevExpress.XtraReports.UI.XRControlStyle();
            this.PageInfo = new DevExpress.XtraReports.UI.XRControlStyle();
            this.DataField = new DevExpress.XtraReports.UI.XRControlStyle();
            this.PrmEmpCode = new DevExpress.XtraReports.Parameters.Parameter();
            this.XrStyleBlack = new DevExpress.XtraReports.UI.XRControlStyle();
            this.XrStyleWhite = new DevExpress.XtraReports.UI.XRControlStyle();
            this.calculatedField1 = new DevExpress.XtraReports.UI.CalculatedField();
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
            customSqlQuery1.Parameters.Add(queryParameter1);
            customSqlQuery1.Sql = OthersCertificatesSql.ExperienceCertificateQuery;
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1});
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel43,
            this.xrLine15,
            this.xrLabel44,
            this.xrLabel29,
            this.xrLabel28,
            this.xrLabel27,
            this.xrLabel26,
            this.xrLabel11,
            this.xrLabel10,
            this.xrLabel32,
            this.xrLabel33,
            this.xrLabel1,
            this.xrLabel2,
            this.xrLabel3,
            this.xrLabel4,
            this.xrLabel7,
            this.xrLabel8,
            this.xrLabel6,
            this.xrLabel17,
            this.xrLabel18,
            this.xrLabel30,
            this.xrLabel31,
            this.xrLabel22,
            this.xrLabel24,
            this.xrLabel25,
            this.xrLabel9,
            this.xrLabel5,
            this.xrLabel16,
            this.xrLine3,
            this.xrLabel19,
            this.xrLabel20,
            this.xrLabel21,
            this.xrLabel23,
            this.xrLine1,
            this.xrLabel15,
            this.xrLabel14,
            this.xrLabel13,
            this.xrLabel12,
            this.lblDateAr});
            this.Detail.HeightF = 1077.083F;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.StyleName = "DataField";
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabel43
            // 
            this.xrLabel43.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel43.ForeColor = System.Drawing.Color.Black;
            this.xrLabel43.LocationFloat = new DevExpress.Utils.PointFloat(133.5942F, 756.75F);
            this.xrLabel43.Name = "xrLabel43";
            this.xrLabel43.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel43.SizeF = new System.Drawing.SizeF(144.1757F, 19.59033F);
            this.xrLabel43.StylePriority.UseFont = false;
            this.xrLabel43.StylePriority.UseForeColor = false;
            this.xrLabel43.StylePriority.UseTextAlignment = false;
            this.xrLabel43.Text = "Authorized Signatory";
            this.xrLabel43.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel43.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel43_BeforePrint);
            // 
            // xrLine15
            // 
            this.xrLine15.LocationFloat = new DevExpress.Utils.PointFloat(278.3608F, 774.3403F);
            this.xrLine15.Name = "xrLine15";
            this.xrLine15.SizeF = new System.Drawing.SizeF(258.9836F, 3.000244F);
            this.xrLine15.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLine15_BeforePrint);
            // 
            // xrLabel44
            // 
            this.xrLabel44.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel44.ForeColor = System.Drawing.Color.Black;
            this.xrLabel44.LocationFloat = new DevExpress.Utils.PointFloat(538.2872F, 756.75F);
            this.xrLabel44.Name = "xrLabel44";
            this.xrLabel44.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel44.SizeF = new System.Drawing.SizeF(89.74023F, 19.59033F);
            this.xrLabel44.StylePriority.UseFont = false;
            this.xrLabel44.StylePriority.UseForeColor = false;
            this.xrLabel44.StylePriority.UseTextAlignment = false;
            this.xrLabel44.Text = "المفوض بالتوقيع";
            this.xrLabel44.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel44.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel44_BeforePrint);
            // 
            // xrLabel29
            // 
            this.xrLabel29.Font = new System.Drawing.Font("Arabic Transparent", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel29.LocationFloat = new DevExpress.Utils.PointFloat(417.2335F, 601.663F);
            this.xrLabel29.Name = "xrLabel29";
            this.xrLabel29.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel29.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel29.SizeF = new System.Drawing.SizeF(357.5571F, 63.34033F);
            this.xrLabel29.StylePriority.UseFont = false;
            this.xrLabel29.StylePriority.UseTextAlignment = false;
            this.xrLabel29.Text = "وقد أعطيت له هذه الشهادة بناءاً على طلبه دون تحمل الشركة أدنى مسئولية تجاه حقوق ا" +
    "لغير.";
            this.xrLabel29.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabel29.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel29_BeforePrint);
            // 
            // xrLabel28
            // 
            this.xrLabel28.BorderColor = System.Drawing.Color.Silver;
            this.xrLabel28.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel28.Font = new System.Drawing.Font("Arabic Transparent", 12F, System.Drawing.FontStyle.Bold);
            this.xrLabel28.ForeColor = System.Drawing.Color.Black;
            this.xrLabel28.LocationFloat = new DevExpress.Utils.PointFloat(42.29496F, 601.663F);
            this.xrLabel28.Name = "xrLabel28";
            this.xrLabel28.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel28.SizeF = new System.Drawing.SizeF(361.4616F, 63.34033F);
            this.xrLabel28.StylePriority.UseBorderColor = false;
            this.xrLabel28.StylePriority.UseBorders = false;
            this.xrLabel28.StylePriority.UseFont = false;
            this.xrLabel28.StylePriority.UseForeColor = false;
            this.xrLabel28.StylePriority.UseTextAlignment = false;
            this.xrLabel28.Text = "This certificate is issued upon his request without any responsiblity towards the" +
    " company towards other rights.";
            this.xrLabel28.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabel28.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel28_BeforePrint);
            // 
            // xrLabel27
            // 
            this.xrLabel27.Font = new System.Drawing.Font("Arabic Transparent", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel27.LocationFloat = new DevExpress.Utils.PointFloat(521.2338F, 552.2047F);
            this.xrLabel27.Name = "xrLabel27";
            this.xrLabel27.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel27.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel27.SizeF = new System.Drawing.SizeF(58.64984F, 19.59027F);
            this.xrLabel27.StylePriority.UseFont = false;
            this.xrLabel27.StylePriority.UseTextAlignment = false;
            this.xrLabel27.Text = "إلى :";
            this.xrLabel27.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel27.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel27_BeforePrint);
            // 
            // xrLabel26
            // 
            this.xrLabel26.Font = new System.Drawing.Font("Arabic Transparent", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel26.LocationFloat = new DevExpress.Utils.PointFloat(693.4252F, 552.2048F);
            this.xrLabel26.Name = "xrLabel26";
            this.xrLabel26.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel26.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel26.SizeF = new System.Drawing.SizeF(81.36542F, 19.59027F);
            this.xrLabel26.StylePriority.UseFont = false;
            this.xrLabel26.StylePriority.UseTextAlignment = false;
            this.xrLabel26.Text = "من :";
            this.xrLabel26.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel26.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel26_BeforePrint);
            // 
            // xrLabel11
            // 
            this.xrLabel11.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel11.ForeColor = System.Drawing.Color.Black;
            this.xrLabel11.LocationFloat = new DevExpress.Utils.PointFloat(252.7896F, 552.2047F);
            this.xrLabel11.Name = "xrLabel11";
            this.xrLabel11.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel11.SizeF = new System.Drawing.SizeF(43.29715F, 19.59033F);
            this.xrLabel11.StylePriority.UseFont = false;
            this.xrLabel11.StylePriority.UseForeColor = false;
            this.xrLabel11.StylePriority.UseTextAlignment = false;
            this.xrLabel11.Text = "To :";
            this.xrLabel11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel11.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel11_BeforePrint);
            // 
            // xrLabel10
            // 
            this.xrLabel10.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel10.ForeColor = System.Drawing.Color.Black;
            this.xrLabel10.LocationFloat = new DevExpress.Utils.PointFloat(38.62505F, 552.2047F);
            this.xrLabel10.Name = "xrLabel10";
            this.xrLabel10.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel10.SizeF = new System.Drawing.SizeF(93.33115F, 19.59033F);
            this.xrLabel10.StylePriority.UseFont = false;
            this.xrLabel10.StylePriority.UseForeColor = false;
            this.xrLabel10.StylePriority.UseTextAlignment = false;
            this.xrLabel10.Text = "From :";
            this.xrLabel10.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel10.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel10_BeforePrint);
            // 
            // xrLabel32
            // 
            this.xrLabel32.Font = new System.Drawing.Font("Arabic Transparent", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel32.LocationFloat = new DevExpress.Utils.PointFloat(693.4253F, 514.0834F);
            this.xrLabel32.Name = "xrLabel32";
            this.xrLabel32.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel32.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel32.SizeF = new System.Drawing.SizeF(81.36542F, 19.59027F);
            this.xrLabel32.StylePriority.UseFont = false;
            this.xrLabel32.StylePriority.UseTextAlignment = false;
            this.xrLabel32.Text = "الوظيفة :";
            this.xrLabel32.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel32.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel32_BeforePrint);
            // 
            // xrLabel33
            // 
            this.xrLabel33.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel33.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[JobTitleAr]")});
            this.xrLabel33.Font = new System.Drawing.Font("Arabic Transparent", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel33.LocationFloat = new DevExpress.Utils.PointFloat(417.2335F, 514.0833F);
            this.xrLabel33.Name = "xrLabel33";
            this.xrLabel33.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel33.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel33.SizeF = new System.Drawing.SizeF(276.1917F, 19.59024F);
            this.xrLabel33.StylePriority.UseBorders = false;
            this.xrLabel33.StylePriority.UseFont = false;
            this.xrLabel33.StylePriority.UseTextAlignment = false;
            this.xrLabel33.Text = "الاسم :";
            this.xrLabel33.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel33.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel33_BeforePrint);
            // 
            // xrLabel1
            // 
            this.xrLabel1.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CivilID]")});
            this.xrLabel1.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(417.2337F, 476.6707F);
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel1.SizeF = new System.Drawing.SizeF(276.1917F, 19.59036F);
            this.xrLabel1.StylePriority.UseBorders = false;
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.Text = "الاسم :";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel1.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel1_BeforePrint);
            // 
            // xrLabel2
            // 
            this.xrLabel2.Font = new System.Drawing.Font("Arabic Transparent", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(693.4254F, 476.6707F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel2.SizeF = new System.Drawing.SizeF(81.36542F, 19.59036F);
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = "الرقم المدنى : ";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel2.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel2_BeforePrint);
            // 
            // xrLabel3
            // 
            this.xrLabel3.Font = new System.Drawing.Font("Arabic Transparent", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(693.4254F, 439.8432F);
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel3.SizeF = new System.Drawing.SizeF(81.36548F, 19.5903F);
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.StylePriority.UseTextAlignment = false;
            this.xrLabel3.Text = "الجنسية : ";
            this.xrLabel3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel3.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel3_BeforePrint);
            // 
            // xrLabel4
            // 
            this.xrLabel4.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NationalityAr]")});
            this.xrLabel4.Font = new System.Drawing.Font("Arabic Transparent", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(417.2337F, 439.8432F);
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel4.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel4.SizeF = new System.Drawing.SizeF(276.1917F, 19.5903F);
            this.xrLabel4.StylePriority.UseBorders = false;
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UseTextAlignment = false;
            this.xrLabel4.Text = "الاسم :";
            this.xrLabel4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel4.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel4_BeforePrint);
            // 
            // xrLabel7
            // 
            this.xrLabel7.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel7.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FullNameAr]")});
            this.xrLabel7.Font = new System.Drawing.Font("Arabic Transparent", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(417.2338F, 384.9093F);
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel7.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel7.SizeF = new System.Drawing.SizeF(276.1916F, 43.18396F);
            this.xrLabel7.StylePriority.UseBorders = false;
            this.xrLabel7.StylePriority.UseFont = false;
            this.xrLabel7.StylePriority.UseTextAlignment = false;
            this.xrLabel7.Text = "الاسم :";
            this.xrLabel7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabel7.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel7_BeforePrint);
            // 
            // xrLabel8
            // 
            this.xrLabel8.Font = new System.Drawing.Font("Arabic Transparent", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel8.LocationFloat = new DevExpress.Utils.PointFloat(693.4254F, 384.9093F);
            this.xrLabel8.Name = "xrLabel8";
            this.xrLabel8.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel8.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel8.SizeF = new System.Drawing.SizeF(81.36548F, 19.5903F);
            this.xrLabel8.StylePriority.UseFont = false;
            this.xrLabel8.StylePriority.UseTextAlignment = false;
            this.xrLabel8.Text = "الاسم :";
            this.xrLabel8.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel8.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel8_BeforePrint);
            // 
            // xrLabel6
            // 
            this.xrLabel6.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel6.ForeColor = System.Drawing.Color.Black;
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(38.62505F, 384.9093F);
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(94.96907F, 19.5903F);
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UseForeColor = false;
            this.xrLabel6.StylePriority.UseTextAlignment = false;
            this.xrLabel6.Text = "Name :";
            this.xrLabel6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel6.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel6_BeforePrint);
            // 
            // xrLabel17
            // 
            this.xrLabel17.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel17.ForeColor = System.Drawing.Color.Black;
            this.xrLabel17.LocationFloat = new DevExpress.Utils.PointFloat(38.62505F, 439.8432F);
            this.xrLabel17.Name = "xrLabel17";
            this.xrLabel17.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel17.SizeF = new System.Drawing.SizeF(94.96909F, 19.59027F);
            this.xrLabel17.StylePriority.UseFont = false;
            this.xrLabel17.StylePriority.UseForeColor = false;
            this.xrLabel17.StylePriority.UseTextAlignment = false;
            this.xrLabel17.Text = "Nationality :";
            this.xrLabel17.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel17.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel17_BeforePrint);
            // 
            // xrLabel18
            // 
            this.xrLabel18.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel18.ForeColor = System.Drawing.Color.Black;
            this.xrLabel18.LocationFloat = new DevExpress.Utils.PointFloat(38.62505F, 476.6707F);
            this.xrLabel18.Name = "xrLabel18";
            this.xrLabel18.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel18.SizeF = new System.Drawing.SizeF(94.9691F, 19.59033F);
            this.xrLabel18.StylePriority.UseFont = false;
            this.xrLabel18.StylePriority.UseForeColor = false;
            this.xrLabel18.StylePriority.UseTextAlignment = false;
            this.xrLabel18.Text = "Civil ID :";
            this.xrLabel18.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel18.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel18_BeforePrint);
            // 
            // xrLabel30
            // 
            this.xrLabel30.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel30.ForeColor = System.Drawing.Color.Black;
            this.xrLabel30.LocationFloat = new DevExpress.Utils.PointFloat(38.62505F, 514.0834F);
            this.xrLabel30.Name = "xrLabel30";
            this.xrLabel30.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel30.SizeF = new System.Drawing.SizeF(94.96904F, 19.59033F);
            this.xrLabel30.StylePriority.UseFont = false;
            this.xrLabel30.StylePriority.UseForeColor = false;
            this.xrLabel30.StylePriority.UseTextAlignment = false;
            this.xrLabel30.Text = "Job Title :";
            this.xrLabel30.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel30.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel30_BeforePrint);
            // 
            // xrLabel31
            // 
            this.xrLabel31.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel31.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Upper(Remove([JobTitleEn], 1)) + Substring([JobTitleEn], 1)")});
            this.xrLabel31.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel31.ForeColor = System.Drawing.Color.Black;
            this.xrLabel31.LocationFloat = new DevExpress.Utils.PointFloat(133.5942F, 514.0834F);
            this.xrLabel31.Name = "xrLabel31";
            this.xrLabel31.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel31.SizeF = new System.Drawing.SizeF(266.4925F, 19.59033F);
            this.xrLabel31.StylePriority.UseBorders = false;
            this.xrLabel31.StylePriority.UseFont = false;
            this.xrLabel31.StylePriority.UseForeColor = false;
            this.xrLabel31.StylePriority.UseTextAlignment = false;
            this.xrLabel31.Text = "Name :";
            this.xrLabel31.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel31.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel31_BeforePrint);
            // 
            // xrLabel22
            // 
            this.xrLabel22.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel22.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CivilID]")});
            this.xrLabel22.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel22.ForeColor = System.Drawing.Color.Black;
            this.xrLabel22.LocationFloat = new DevExpress.Utils.PointFloat(133.5942F, 476.6707F);
            this.xrLabel22.Name = "xrLabel22";
            this.xrLabel22.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel22.SizeF = new System.Drawing.SizeF(266.4925F, 19.59033F);
            this.xrLabel22.StylePriority.UseBorders = false;
            this.xrLabel22.StylePriority.UseFont = false;
            this.xrLabel22.StylePriority.UseForeColor = false;
            this.xrLabel22.StylePriority.UseTextAlignment = false;
            this.xrLabel22.Text = "Name :";
            this.xrLabel22.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel22.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel22_BeforePrint);
            // 
            // xrLabel24
            // 
            this.xrLabel24.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel24.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[NationalityEn]")});
            this.xrLabel24.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel24.ForeColor = System.Drawing.Color.Black;
            this.xrLabel24.LocationFloat = new DevExpress.Utils.PointFloat(133.5942F, 439.8432F);
            this.xrLabel24.Name = "xrLabel24";
            this.xrLabel24.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel24.SizeF = new System.Drawing.SizeF(266.4925F, 19.59027F);
            this.xrLabel24.StylePriority.UseBorders = false;
            this.xrLabel24.StylePriority.UseFont = false;
            this.xrLabel24.StylePriority.UseForeColor = false;
            this.xrLabel24.StylePriority.UseTextAlignment = false;
            this.xrLabel24.Text = "Name :";
            this.xrLabel24.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel24.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel24_BeforePrint);
            // 
            // xrLabel25
            // 
            this.xrLabel25.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel25.CanGrow = false;
            this.xrLabel25.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FullNameEn]")});
            this.xrLabel25.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel25.ForeColor = System.Drawing.Color.Black;
            this.xrLabel25.LocationFloat = new DevExpress.Utils.PointFloat(133.5942F, 384.9093F);
            this.xrLabel25.Name = "xrLabel25";
            this.xrLabel25.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel25.SizeF = new System.Drawing.SizeF(266.4925F, 43.18396F);
            this.xrLabel25.StylePriority.UseBorders = false;
            this.xrLabel25.StylePriority.UseFont = false;
            this.xrLabel25.StylePriority.UseForeColor = false;
            this.xrLabel25.StylePriority.UseTextAlignment = false;
            this.xrLabel25.Text = "Name :";
            this.xrLabel25.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabel25.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel25_BeforePrint);
            // 
            // xrLabel9
            // 
            this.xrLabel9.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "\'تشهد \' + [SponsorAr] + \' بأن البيانات أدناه من واقع سجلاتها الرسمية \'")});
            this.xrLabel9.Font = new System.Drawing.Font("Arabic Transparent", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel9.ForeColor = System.Drawing.Color.Black;
            this.xrLabel9.LocationFloat = new DevExpress.Utils.PointFloat(417.2338F, 312.9374F);
            this.xrLabel9.Name = "xrLabel9";
            this.xrLabel9.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel9.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel9.SizeF = new System.Drawing.SizeF(357.5572F, 59.0833F);
            this.xrLabel9.StylePriority.UseFont = false;
            this.xrLabel9.StylePriority.UseForeColor = false;
            this.xrLabel9.StylePriority.UseTextAlignment = false;
            this.xrLabel9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabel9.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel9_BeforePrint);
            // 
            // xrLabel5
            // 
            this.xrLabel5.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[SponsorEn] + \' certify the following information from its official registers\'")});
            this.xrLabel5.Font = new System.Drawing.Font("Arabic Transparent", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel5.ForeColor = System.Drawing.Color.Black;
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(38.62505F, 312.9374F);
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.SizeF = new System.Drawing.SizeF(361.4616F, 59.0833F);
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.StylePriority.UseForeColor = false;
            this.xrLabel5.StylePriority.UseTextAlignment = false;
            this.xrLabel5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabel5.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel5_BeforePrint);
            // 
            // xrLabel16
            // 
            this.xrLabel16.Font = new System.Drawing.Font("Arabic Transparent", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel16.LocationFloat = new DevExpress.Utils.PointFloat(38.62505F, 181.5417F);
            this.xrLabel16.Name = "xrLabel16";
            this.xrLabel16.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel16.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel16.SizeF = new System.Drawing.SizeF(736.1658F, 30.95827F);
            this.xrLabel16.StylePriority.UseFont = false;
            this.xrLabel16.StylePriority.UseTextAlignment = false;
            this.xrLabel16.Text = "شهادة الخبرة";
            this.xrLabel16.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel16.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel16_BeforePrint);
            // 
            // xrLine3
            // 
            this.xrLine3.BorderWidth = 0.3F;
            this.xrLine3.LineDirection = DevExpress.XtraReports.UI.LineDirection.Vertical;
            this.xrLine3.LocationFloat = new DevExpress.Utils.PointFloat(404.7566F, 273.9167F);
            this.xrLine3.Name = "xrLine3";
            this.xrLine3.SizeF = new System.Drawing.SizeF(5.902649F, 396.2708F);
            this.xrLine3.StylePriority.UseBorderWidth = false;
            this.xrLine3.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLine3_BeforePrint);
            // 
            // xrLabel19
            // 
            this.xrLabel19.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters].[PrmDate]")});
            this.xrLabel19.Font = new System.Drawing.Font("Arabic Transparent", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel19.ForeColor = System.Drawing.Color.Black;
            this.xrLabel19.LocationFloat = new DevExpress.Utils.PointFloat(103.7072F, 273.9167F);
            this.xrLabel19.Name = "xrLabel19";
            this.xrLabel19.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel19.SizeF = new System.Drawing.SizeF(296.3795F, 24.70831F);
            this.xrLabel19.StylePriority.UseFont = false;
            this.xrLabel19.StylePriority.UseForeColor = false;
            this.xrLabel19.StylePriority.UseTextAlignment = false;
            this.xrLabel19.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel19.TextFormatString = "{0:yyyy/MM/dd}";
            this.xrLabel19.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel19_BeforePrint);
            // 
            // xrLabel20
            // 
            this.xrLabel20.Font = new System.Drawing.Font("Arabic Transparent", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel20.ForeColor = System.Drawing.Color.Black;
            this.xrLabel20.LocationFloat = new DevExpress.Utils.PointFloat(38.62505F, 273.9167F);
            this.xrLabel20.Name = "xrLabel20";
            this.xrLabel20.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel20.SizeF = new System.Drawing.SizeF(65.08215F, 24.7083F);
            this.xrLabel20.StylePriority.UseFont = false;
            this.xrLabel20.StylePriority.UseForeColor = false;
            this.xrLabel20.StylePriority.UseTextAlignment = false;
            this.xrLabel20.Text = "Date :";
            this.xrLabel20.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel20.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel20_BeforePrint);
            // 
            // xrLabel21
            // 
            this.xrLabel21.Font = new System.Drawing.Font("Arabic Transparent", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel21.LocationFloat = new DevExpress.Utils.PointFloat(38.62506F, 217.5F);
            this.xrLabel21.Name = "xrLabel21";
            this.xrLabel21.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel21.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel21.SizeF = new System.Drawing.SizeF(736.1657F, 24.62497F);
            this.xrLabel21.StylePriority.UseFont = false;
            this.xrLabel21.StylePriority.UseTextAlignment = false;
            this.xrLabel21.Text = "Experience Certificate";
            this.xrLabel21.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel21.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel21_BeforePrint);
            // 
            // xrLabel23
            // 
            this.xrLabel23.Font = new System.Drawing.Font("Arabic Transparent", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(178)));
            this.xrLabel23.ForeColor = System.Drawing.Color.Black;
            this.xrLabel23.LocationFloat = new DevExpress.Utils.PointFloat(709.7088F, 273.9167F);
            this.xrLabel23.Name = "xrLabel23";
            this.xrLabel23.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel23.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel23.SizeF = new System.Drawing.SizeF(65.08215F, 24.7083F);
            this.xrLabel23.StylePriority.UseFont = false;
            this.xrLabel23.StylePriority.UseForeColor = false;
            this.xrLabel23.StylePriority.UseTextAlignment = false;
            this.xrLabel23.Text = "التاريخ :";
            this.xrLabel23.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel23.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel23_BeforePrint);
            // 
            // xrLine1
            // 
            this.xrLine1.LocationFloat = new DevExpress.Utils.PointFloat(257.8162F, 212.5F);
            this.xrLine1.Name = "xrLine1";
            this.xrLine1.SizeF = new System.Drawing.SizeF(294.7415F, 3F);
            this.xrLine1.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLine1_BeforePrint);
            // 
            // xrLabel15
            // 
            this.xrLabel15.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel15.CanGrow = false;
            this.xrLabel15.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ToDateAr]")});
            this.xrLabel15.Font = new System.Drawing.Font("Arabic Transparent", 12F, System.Drawing.FontStyle.Bold);
            this.xrLabel15.LocationFloat = new DevExpress.Utils.PointFloat(417.2338F, 552.205F);
            this.xrLabel15.Name = "xrLabel15";
            this.xrLabel15.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel15.SizeF = new System.Drawing.SizeF(104F, 19.59018F);
            this.xrLabel15.StylePriority.UseBorders = false;
            this.xrLabel15.StylePriority.UseFont = false;
            this.xrLabel15.StylePriority.UseTextAlignment = false;
            this.xrLabel15.Text = "xrLabel13";
            this.xrLabel15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel15.TextFormatString = "{0:dd/MM/yyyy}";
            this.xrLabel15.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel15_BeforePrint);
            // 
            // xrLabel14
            // 
            this.xrLabel14.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel14.CanGrow = false;
            this.xrLabel14.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FromDate]")});
            this.xrLabel14.Font = new System.Drawing.Font("Arabic Transparent", 12F, System.Drawing.FontStyle.Bold);
            this.xrLabel14.LocationFloat = new DevExpress.Utils.PointFloat(589.3002F, 552.2048F);
            this.xrLabel14.Name = "xrLabel14";
            this.xrLabel14.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel14.SizeF = new System.Drawing.SizeF(104F, 19.59018F);
            this.xrLabel14.StylePriority.UseBorders = false;
            this.xrLabel14.StylePriority.UseFont = false;
            this.xrLabel14.StylePriority.UseTextAlignment = false;
            this.xrLabel14.Text = "xrLabel12";
            this.xrLabel14.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel14.TextFormatString = "{0:dd/MM/yyyy}";
            this.xrLabel14.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel14_BeforePrint);
            // 
            // xrLabel13
            // 
            this.xrLabel13.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel13.CanGrow = false;
            this.xrLabel13.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ToDateEn]")});
            this.xrLabel13.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel13.LocationFloat = new DevExpress.Utils.PointFloat(296.0867F, 552.2047F);
            this.xrLabel13.Name = "xrLabel13";
            this.xrLabel13.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel13.SizeF = new System.Drawing.SizeF(104F, 19.59036F);
            this.xrLabel13.StylePriority.UseBorders = false;
            this.xrLabel13.StylePriority.UseFont = false;
            this.xrLabel13.StylePriority.UseTextAlignment = false;
            this.xrLabel13.Text = "xrLabel13";
            this.xrLabel13.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel13.TextFormatString = "{0:dd/MM/yyyy}";
            this.xrLabel13.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel13_BeforePrint);
            // 
            // xrLabel12
            // 
            this.xrLabel12.Borders = DevExpress.XtraPrinting.BorderSide.Bottom;
            this.xrLabel12.CanGrow = false;
            this.xrLabel12.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FromDate]")});
            this.xrLabel12.Font = new System.Drawing.Font("Arabic Transparent", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel12.LocationFloat = new DevExpress.Utils.PointFloat(131.9562F, 552.2047F);
            this.xrLabel12.Name = "xrLabel12";
            this.xrLabel12.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel12.SizeF = new System.Drawing.SizeF(104F, 19.5903F);
            this.xrLabel12.StylePriority.UseBorders = false;
            this.xrLabel12.StylePriority.UseFont = false;
            this.xrLabel12.StylePriority.UseTextAlignment = false;
            this.xrLabel12.Text = "xrLabel12";
            this.xrLabel12.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel12.TextFormatString = "{0:dd/MM/yyyy}";
            this.xrLabel12.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.xrLabel12_BeforePrint);
            // 
            // lblDateAr
            // 
            this.lblDateAr.CanGrow = false;
            this.lblDateAr.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters].[PrmDate]")});
            this.lblDateAr.Font = new System.Drawing.Font("Arabic Transparent", 12F, System.Drawing.FontStyle.Bold);
            this.lblDateAr.LocationFloat = new DevExpress.Utils.PointFloat(417.2338F, 273.9167F);
            this.lblDateAr.Name = "lblDateAr";
            this.lblDateAr.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblDateAr.SizeF = new System.Drawing.SizeF(292.4751F, 24.70831F);
            this.lblDateAr.StylePriority.UseFont = false;
            this.lblDateAr.StylePriority.UseTextAlignment = false;
            this.lblDateAr.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lblDateAr.TextFormatString = "{0:dd/MM/yyyy}";
            this.lblDateAr.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.lblDateAr_BeforePrint);
            // 
            // PrmDate
            // 
            this.PrmDate.Description = "Date";
            this.PrmDate.Name = "PrmDate";
            this.PrmDate.Type = typeof(System.DateTime);
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 1.999998F;
            this.TopMargin.Name = "TopMargin";
            this.TopMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.TopMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // BottomMargin
            // 
            this.BottomMargin.HeightF = 3.125381F;
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
            // calculatedField1
            // 
            this.calculatedField1.DataMember = "Query";
            this.calculatedField1.DisplayName = "EmpFullName";
            this.calculatedField1.Expression = "[Code] + \' \' + [FullNameEn]";
            this.calculatedField1.Name = "calculatedField1";
            // 
            // rptExperienceCertificate
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.calculatedField1});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.sqlDataSource1});
            this.DataMember = "Query";
            this.DataSource = this.sqlDataSource1;
            this.Margins = new System.Drawing.Printing.Margins(2, 2, 2, 3);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.PrmEmpCode,
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
        private DevExpress.XtraReports.Parameters.Parameter PrmEmpCode;
        public DevExpress.DataAccess.Sql.SqlDataSource sqlDataSource1;
        private DevExpress.XtraReports.Parameters.Parameter PrmDate;
        private DevExpress.XtraReports.UI.XRControlStyle XrStyleBlack;
        private DevExpress.XtraReports.UI.XRControlStyle XrStyleWhite;
        private DevExpress.XtraReports.UI.XRLabel lblDateAr;
        private DevExpress.XtraReports.UI.CalculatedField calculatedField1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel13;
        private DevExpress.XtraReports.UI.XRLabel xrLabel12;
        private DevExpress.XtraReports.UI.XRLabel xrLabel15;
        private DevExpress.XtraReports.UI.XRLabel xrLabel14;
        private DevExpress.XtraReports.UI.XRLabel xrLabel16;
        private DevExpress.XtraReports.UI.XRLine xrLine3;
        private DevExpress.XtraReports.UI.XRLabel xrLabel19;
        private DevExpress.XtraReports.UI.XRLabel xrLabel20;
        private DevExpress.XtraReports.UI.XRLabel xrLabel21;
        private DevExpress.XtraReports.UI.XRLabel xrLabel23;
        private DevExpress.XtraReports.UI.XRLine xrLine1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel9;
        private DevExpress.XtraReports.UI.XRLabel xrLabel5;
        private DevExpress.XtraReports.UI.XRLabel xrLabel6;
        private DevExpress.XtraReports.UI.XRLabel xrLabel17;
        private DevExpress.XtraReports.UI.XRLabel xrLabel18;
        private DevExpress.XtraReports.UI.XRLabel xrLabel30;
        private DevExpress.XtraReports.UI.XRLabel xrLabel31;
        private DevExpress.XtraReports.UI.XRLabel xrLabel22;
        private DevExpress.XtraReports.UI.XRLabel xrLabel24;
        private DevExpress.XtraReports.UI.XRLabel xrLabel25;
        private DevExpress.XtraReports.UI.XRLabel xrLabel32;
        private DevExpress.XtraReports.UI.XRLabel xrLabel33;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.XRLabel xrLabel3;
        private DevExpress.XtraReports.UI.XRLabel xrLabel4;
        private DevExpress.XtraReports.UI.XRLabel xrLabel7;
        private DevExpress.XtraReports.UI.XRLabel xrLabel8;
        private DevExpress.XtraReports.UI.XRLabel xrLabel10;
        private DevExpress.XtraReports.UI.XRLabel xrLabel11;
        private DevExpress.XtraReports.UI.XRLabel xrLabel27;
        private DevExpress.XtraReports.UI.XRLabel xrLabel26;
        private DevExpress.XtraReports.UI.XRLabel xrLabel29;
        private DevExpress.XtraReports.UI.XRLabel xrLabel28;
        private DevExpress.XtraReports.UI.XRLabel xrLabel43;
        private DevExpress.XtraReports.UI.XRLine xrLine15;
        private DevExpress.XtraReports.UI.XRLabel xrLabel44;
    }
}
