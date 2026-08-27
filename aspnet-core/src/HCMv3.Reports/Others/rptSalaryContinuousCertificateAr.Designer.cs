namespace HCMv3.Reports.Others
{
    partial class rptSalaryContinuousCertificateAr
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
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
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
            this.xrLabel18 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel16 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel25 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel12 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel14 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel11 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel10 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel9 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.PageHeader = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.xrLabel17 = new DevExpress.XtraReports.UI.XRLabel();
            this.PrmEmpCode = new DevExpress.XtraReports.Parameters.Parameter();
            this.ReportHeader = new DevExpress.XtraReports.UI.ReportHeaderBand();
            this.xrLabel15 = new DevExpress.XtraReports.UI.XRLabel();
            this.PrmDate = new DevExpress.XtraReports.Parameters.Parameter();
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
            customSqlQuery1.Sql = OthersCertificatesSql.SalaryContinuousCertificateArQuery;
            this.sqlDataSource1.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1});
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel2,
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
            this.xrLabel18,
            this.xrLabel16,
            this.xrLabel25,
            this.xrLabel12,
            this.xrLabel14,
            this.xrLabel3,
            this.xrLabel11,
            this.xrLabel10,
            this.xrLabel9,
            this.xrLabel7,
            this.xrLabel5,
            this.xrLabel4});
            this.Detail.HeightF = 646.5726F;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(15, 5, 5, 5, 100F);
            this.Detail.StylePriority.UsePadding = false;
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabel2
            // 
            this.xrLabel2.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold);
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(314.1042F, 611.6003F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel2.SizeF = new System.Drawing.SizeF(101.2915F, 24.3056F);
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = "المفوض بالتوقيع";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel28
            // 
            this.xrLabel28.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "Iif(IsNullOrEmpty([AccountNum]) ,\nIif(IsNullOrEmpty([IBAN]),\'\',[IBAN]  ) ,\n[Accou" +
                    "ntNum] )")});
            this.xrLabel28.Font = new System.Drawing.Font("Arial", 11.5F);
            this.xrLabel28.LocationFloat = new DevExpress.Utils.PointFloat(3.000092F, 288.2086F);
            this.xrLabel28.Name = "xrLabel28";
            this.xrLabel28.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel28.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel28.SizeF = new System.Drawing.SizeF(508.0634F, 23F);
            this.xrLabel28.StylePriority.UseFont = false;
            this.xrLabel28.Text = "xrLabel28";
            // 
            // lblMonthlySalary
            // 
            this.lblMonthlySalary.Font = new System.Drawing.Font("Arial", 11.5F);
            this.lblMonthlySalary.LocationFloat = new DevExpress.Utils.PointFloat(3.000092F, 213.8752F);
            this.lblMonthlySalary.Name = "lblMonthlySalary";
            this.lblMonthlySalary.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblMonthlySalary.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.lblMonthlySalary.SizeF = new System.Drawing.SizeF(508.0635F, 23.00002F);
            this.lblMonthlySalary.StylePriority.UseFont = false;
            this.lblMonthlySalary.Text = "lblMonthlySalary";
            // 
            // xrLabel26
            // 
            this.xrLabel26.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[EmpBank]")});
            this.xrLabel26.Font = new System.Drawing.Font("Arial", 11.5F);
            this.xrLabel26.LocationFloat = new DevExpress.Utils.PointFloat(3F, 250.9585F);
            this.xrLabel26.Name = "xrLabel26";
            this.xrLabel26.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel26.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel26.SizeF = new System.Drawing.SizeF(508.0634F, 22.99995F);
            this.xrLabel26.StylePriority.UseFont = false;
            this.xrLabel26.Text = "xrLabel26";
            // 
            // xrLabel24
            // 
            this.xrLabel24.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CivilID]")});
            this.xrLabel24.Font = new System.Drawing.Font("Arial", 11.5F);
            this.xrLabel24.LocationFloat = new DevExpress.Utils.PointFloat(3F, 145.375F);
            this.xrLabel24.Name = "xrLabel24";
            this.xrLabel24.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel24.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel24.SizeF = new System.Drawing.SizeF(508.0636F, 22.99998F);
            this.xrLabel24.StylePriority.UseFont = false;
            this.xrLabel24.Text = "xrLabel24";
            // 
            // xrLabel23
            // 
            this.xrLabel23.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel23.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel23.LocationFloat = new DevExpress.Utils.PointFloat(518.1039F, 288.2917F);
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
            this.xrLabel22.LocationFloat = new DevExpress.Utils.PointFloat(518.2151F, 251.0417F);
            this.xrLabel22.Name = "xrLabel22";
            this.xrLabel22.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel22.SizeF = new System.Drawing.SizeF(7.312714F, 22.91667F);
            this.xrLabel22.StylePriority.UseBorders = false;
            this.xrLabel22.StylePriority.UseFont = false;
            this.xrLabel22.Text = ":";
            // 
            // xrLabel21
            // 
            this.xrLabel21.Font = new System.Drawing.Font("Arial", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel21.LocationFloat = new DevExpress.Utils.PointFloat(525.4167F, 288.29F);
            this.xrLabel21.Name = "xrLabel21";
            this.xrLabel21.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel21.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel21.SizeF = new System.Drawing.SizeF(181.5833F, 22.91663F);
            this.xrLabel21.StylePriority.UseFont = false;
            this.xrLabel21.Text = "حساب رقم";
            // 
            // xrLabel20
            // 
            this.xrLabel20.Font = new System.Drawing.Font("Arial", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel20.LocationFloat = new DevExpress.Utils.PointFloat(525.5278F, 251.0417F);
            this.xrLabel20.Name = "xrLabel20";
            this.xrLabel20.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel20.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel20.SizeF = new System.Drawing.SizeF(181.4722F, 22.91666F);
            this.xrLabel20.StylePriority.UseFont = false;
            this.xrLabel20.Text = "البنك";
            // 
            // xrLabel13
            // 
            this.xrLabel13.Font = new System.Drawing.Font("Arial", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel13.LocationFloat = new DevExpress.Utils.PointFloat(525.639F, 146.3735F);
            this.xrLabel13.Name = "xrLabel13";
            this.xrLabel13.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel13.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel13.SizeF = new System.Drawing.SizeF(181.361F, 22.91667F);
            this.xrLabel13.StylePriority.UseFont = false;
            this.xrLabel13.Text = "رقم البطاقة المدنية";
            // 
            // xrLabel8
            // 
            this.xrLabel8.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel8.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel8.LocationFloat = new DevExpress.Utils.PointFloat(518.0624F, 74.04022F);
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
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(518.0624F, 109.1236F);
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(7.312714F, 22.91666F);
            this.xrLabel1.StylePriority.UseBorders = false;
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.Text = ":";
            // 
            // xrLabel18
            // 
            this.xrLabel18.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel18.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel18.LocationFloat = new DevExpress.Utils.PointFloat(518.0624F, 214.0417F);
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
            this.xrLabel16.LocationFloat = new DevExpress.Utils.PointFloat(518.0624F, 179.6667F);
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
            this.xrLabel25.LocationFloat = new DevExpress.Utils.PointFloat(518.0624F, 145.5417F);
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
            this.xrLabel12.Font = new System.Drawing.Font("Arial", 11.5F);
            this.xrLabel12.LocationFloat = new DevExpress.Utils.PointFloat(3.000076F, 181.2903F);
            this.xrLabel12.Name = "xrLabel12";
            this.xrLabel12.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel12.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel12.SizeF = new System.Drawing.SizeF(508.0636F, 23.00002F);
            this.xrLabel12.StylePriority.UseFont = false;
            this.xrLabel12.Text = "xrLabel12";
            this.xrLabel12.TextFormatString = "{0:dd/MM/yyyy}";
            // 
            // xrLabel14
            // 
            this.xrLabel14.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Jobtitle]")});
            this.xrLabel14.Font = new System.Drawing.Font("Arial", 11.5F);
            this.xrLabel14.LocationFloat = new DevExpress.Utils.PointFloat(3.000092F, 110.1235F);
            this.xrLabel14.Name = "xrLabel14";
            this.xrLabel14.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel14.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel14.SizeF = new System.Drawing.SizeF(508.0634F, 22.99999F);
            this.xrLabel14.StylePriority.UseFont = false;
            this.xrLabel14.Text = "xrLabel14";
            // 
            // xrLabel3
            // 
            this.xrLabel3.Font = new System.Drawing.Font("Arial", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(526.0833F, 74.04022F);
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel3.SizeF = new System.Drawing.SizeF(180.9165F, 22.91666F);
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.Text = "اسم الموظف";
            // 
            // xrLabel11
            // 
            this.xrLabel11.Font = new System.Drawing.Font("Arial", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel11.LocationFloat = new DevExpress.Utils.PointFloat(525.7501F, 214.29F);
            this.xrLabel11.Name = "xrLabel11";
            this.xrLabel11.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel11.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel11.SizeF = new System.Drawing.SizeF(181.2499F, 22.91664F);
            this.xrLabel11.StylePriority.UseFont = false;
            this.xrLabel11.Text = "إجمالي الراتب";
            // 
            // xrLabel10
            // 
            this.xrLabel10.Font = new System.Drawing.Font("Arial", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel10.LocationFloat = new DevExpress.Utils.PointFloat(525.8612F, 180.2276F);
            this.xrLabel10.Name = "xrLabel10";
            this.xrLabel10.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel10.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel10.SizeF = new System.Drawing.SizeF(181.1388F, 22.91666F);
            this.xrLabel10.StylePriority.UseFont = false;
            this.xrLabel10.Text = "تاريخ التعيين";
            // 
            // xrLabel9
            // 
            this.xrLabel9.Font = new System.Drawing.Font("Arial", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel9.LocationFloat = new DevExpress.Utils.PointFloat(525.9724F, 110.3735F);
            this.xrLabel9.Name = "xrLabel9";
            this.xrLabel9.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel9.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel9.SizeF = new System.Drawing.SizeF(181.0276F, 22.91667F);
            this.xrLabel9.StylePriority.UseFont = false;
            this.xrLabel9.Text = "المسمى الوظيفي";
            // 
            // xrLabel7
            // 
            this.xrLabel7.Font = new System.Drawing.Font("Arial", 11.5F);
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(0F, 422.5403F);
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel7.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel7.SizeF = new System.Drawing.SizeF(706.9997F, 28.47229F);
            this.xrLabel7.StylePriority.UseFont = false;
            this.xrLabel7.StylePriority.UseTextAlignment = false;
            this.xrLabel7.Text = "هذه الشهادة صالحة لمدة 3 اشهر وقد أصدرت بناءً على طلب الموظف دون أدنى مسؤولية على" +
    " الشركة.";
            this.xrLabel7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel5
            // 
            this.xrLabel5.Font = new System.Drawing.Font("Arial", 11F);
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(0F, 14.67906F);
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel5.SizeF = new System.Drawing.SizeF(706.9998F, 30.69448F);
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.StylePriority.UseTextAlignment = false;
            this.xrLabel5.Text = "تشهد شركة [CompanyAr] بأن الموظف المذكور أدناه يعمل لديها :";
            this.xrLabel5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel4
            // 
            this.xrLabel4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FullNameAr]")});
            this.xrLabel4.Font = new System.Drawing.Font("Arial", 11.5F);
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(3F, 73.95689F);
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel4.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel4.SizeF = new System.Drawing.SizeF(508.0634F, 22.99998F);
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.Text = "xrLabel4";
            // 
            // TopMargin
            // 
            this.TopMargin.HeightF = 10F;
            this.TopMargin.Name = "TopMargin";
            this.TopMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 5, 5, 100F);
            this.TopMargin.StylePriority.UsePadding = false;
            this.TopMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // BottomMargin
            // 
            this.BottomMargin.HeightF = 10F;
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
            // xrLabel17
            // 
            this.xrLabel17.Font = new System.Drawing.Font("Arial", 16.2F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel17.ForeColor = System.Drawing.Color.Black;
            this.xrLabel17.LocationFloat = new DevExpress.Utils.PointFloat(0F, 165.9848F);
            this.xrLabel17.Name = "xrLabel17";
            this.xrLabel17.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel17.SizeF = new System.Drawing.SizeF(706.9999F, 31.10783F);
            this.xrLabel17.StylePriority.UseFont = false;
            this.xrLabel17.StylePriority.UseForeColor = false;
            this.xrLabel17.StylePriority.UseTextAlignment = false;
            this.xrLabel17.Text = "شهادة استمرارية راتب";
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
            this.xrLabel17});
            this.ReportHeader.HeightF = 197.0926F;
            this.ReportHeader.Name = "ReportHeader";
            this.ReportHeader.Padding = new DevExpress.XtraPrinting.PaddingInfo(5, 5, 5, 5, 100F);
            this.ReportHeader.StylePriority.UsePadding = false;
            // 
            // xrLabel15
            // 
            this.xrLabel15.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters].[PrmDate]")});
            this.xrLabel15.Font = new System.Drawing.Font("Arial", 11.5F);
            this.xrLabel15.LocationFloat = new DevExpress.Utils.PointFloat(0F, 104.6515F);
            this.xrLabel15.Name = "xrLabel15";
            this.xrLabel15.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel15.SizeF = new System.Drawing.SizeF(180.9165F, 22.99995F);
            this.xrLabel15.StylePriority.UseFont = false;
            this.xrLabel15.StylePriority.UseTextAlignment = false;
            this.xrLabel15.Text = "xrLabel15";
            this.xrLabel15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            this.xrLabel15.TextFormatString = "{0:dd/MM/yyyy}";
            // 
            // PrmDate
            // 
            this.PrmDate.Description = "Date";
            this.PrmDate.Name = "PrmDate";
            this.PrmDate.Type = typeof(System.DateTime);
            // 
            // rptSalaryContinuousCertificateAr
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
            this.Margins = new System.Drawing.Printing.Margins(52, 68, 10, 10);
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
        private DevExpress.XtraReports.UI.XRLabel xrLabel7;
        private DevExpress.XtraReports.UI.XRLabel xrLabel5;
        private DevExpress.XtraReports.UI.XRLabel xrLabel3;
        private DevExpress.XtraReports.Parameters.Parameter PrmEmpCode;
        private DevExpress.XtraReports.UI.XRLabel xrLabel14;
        private DevExpress.XtraReports.UI.XRLabel xrLabel4;
        private DevExpress.XtraReports.UI.ReportHeaderBand ReportHeader;
        private DevExpress.XtraReports.UI.XRLabel xrLabel18;
        private DevExpress.XtraReports.UI.XRLabel xrLabel16;
        private DevExpress.XtraReports.UI.XRLabel xrLabel25;
        private DevExpress.XtraReports.UI.XRLabel xrLabel15;
        private DevExpress.XtraReports.Parameters.Parameter PrmDate;
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
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
    }
}
