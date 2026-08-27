

namespace HCMv3.Reports.Payroll
{
    partial class rptSocialSecurity
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
            DevExpress.DataAccess.Sql.QueryParameter queryParameter5 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter6 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery2 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery3 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery4 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.MasterDetailInfo masterDetailInfo1 = new DevExpress.DataAccess.Sql.MasterDetailInfo();
            DevExpress.DataAccess.Sql.RelationColumnInfo relationColumnInfo1 = new DevExpress.DataAccess.Sql.RelationColumnInfo();
            DevExpress.DataAccess.Sql.MasterDetailInfo masterDetailInfo2 = new DevExpress.DataAccess.Sql.MasterDetailInfo();
            DevExpress.DataAccess.Sql.RelationColumnInfo relationColumnInfo2 = new DevExpress.DataAccess.Sql.RelationColumnInfo();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings1 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            DevExpress.XtraReports.UI.XRSummary xrSummary1 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.UI.XRSummary xrSummary2 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.UI.XRSummary xrSummary3 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.UI.XRSummary xrSummary4 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.UI.XRSummary xrSummary5 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.UI.XRSummary xrSummary6 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.UI.XRSummary xrSummary7 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.UI.XRSummary xrSummary8 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.UI.XRSummary xrSummary9 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings2 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings3 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings2 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            this.PayrollSheetDS = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLabel10 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel8 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel104 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel103 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel41 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel40 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel39 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel36 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel28 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel27 = new DevExpress.XtraReports.UI.XRLabel();
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
            this.CalcNetPay = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcForGetMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcInGetMonth = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcForGetYear = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcInGetYear = new DevExpress.XtraReports.UI.CalculatedField();
            this.PageHeader = new DevExpress.XtraReports.UI.PageHeaderBand();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel102 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel101 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel24 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel23 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel22 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel20 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel18 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblHeader = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel9 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel16 = new DevExpress.XtraReports.UI.XRLabel();
            this.ReportFooter = new DevExpress.XtraReports.UI.ReportFooterBand();
            this.xrLabel33 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel32 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel31 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel171 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel123 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel122 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel48 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel52 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel54 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel55 = new DevExpress.XtraReports.UI.XRLabel();
            this.PageFooter = new DevExpress.XtraReports.UI.PageFooterBand();
            this.xrPageInfo1 = new DevExpress.XtraReports.UI.XRPageInfo();
            this.PrmSheetDate = new DevExpress.XtraReports.Parameters.Parameter();
            this.xrControlStyle1 = new DevExpress.XtraReports.UI.XRControlStyle();
            this.ReportHeader = new DevExpress.XtraReports.UI.ReportHeaderBand();
            this.PrmCompany = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmDepartment = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmPayrollArea = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmYear = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmMonth = new DevExpress.XtraReports.Parameters.Parameter();
            this.xrLabel12 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblUserlogin = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel11 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel13 = new DevExpress.XtraReports.UI.XRLabel();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // PayrollSheetDS
            // 
            this.PayrollSheetDS.ConnectionName = HCMv3.Reports.HcmReportBase.DesignTimeConnectionName;
            this.PayrollSheetDS.ConnectionOptions.DbCommandTimeout = 0;
            this.PayrollSheetDS.Name = "PayrollSheetDS";
            customSqlQuery1.MetaSerializable = "<Meta X=\"20\" Y=\"20\" Width=\"100\" Height=\"645\" />";
            customSqlQuery1.Name = "PayrollSheetQuery";
            queryParameter1.Name = "PrmMonth";
            queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmMonth]", typeof(int));
            queryParameter2.Name = "PrmPayrollRunType";
            queryParameter2.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter2.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmPayrollRunType]", typeof(int));
            queryParameter3.Name = "PrmCompany";
            queryParameter3.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter3.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmCompany]", typeof(string));
            queryParameter4.Name = "PrmDepartment";
            queryParameter4.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter4.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmDepartment]", typeof(string));
            queryParameter5.Name = "PrmYear";
            queryParameter5.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter5.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmYear]", typeof(int));
            queryParameter6.Name = "PrmPayrollArea";
            queryParameter6.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter6.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmPayrollArea]", typeof(string));
            customSqlQuery1.Parameters.Add(queryParameter1);
            customSqlQuery1.Parameters.Add(queryParameter2);
            customSqlQuery1.Parameters.Add(queryParameter3);
            customSqlQuery1.Parameters.Add(queryParameter4);
            customSqlQuery1.Parameters.Add(queryParameter5);
            customSqlQuery1.Parameters.Add(queryParameter6);
            customSqlQuery1.Sql = PayslipVariantSql.SocialSecurityQuery;
            customSqlQuery2.MetaSerializable = "<Meta X=\"140\" Y=\"20\" Width=\"100\" Height=\"70\" />";
            customSqlQuery2.Name = "dsCompany";
            customSqlQuery2.Sql = PayslipVariantSql.SocialSecurityCompanyList;
            customSqlQuery3.MetaSerializable = "<Meta X=\"200\" Y=\"250\" Width=\"100\" Height=\"70\" />";
            customSqlQuery3.Name = "dsDepartment";
            customSqlQuery3.Sql = PayslipVariantSql.SocialSecurityDepartmentList;
            customSqlQuery4.Name = "dsPayrollArea";
            customSqlQuery4.Sql = PayslipVariantSql.PayrollAreaList;
            this.PayrollSheetDS.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1,
            customSqlQuery2,
            customSqlQuery3,
            customSqlQuery4});
            // PORT NOTE -- two master/detail relations DROPPED, deliberately.
            // The legacy layout related PayrollSheetQuery to dsCompany on a column named
            // "DivisionEn" and to dsDepartment on "DepartmentEn". Neither query has ever had a
            // DivisionEn column (dsCompany selects CompanyEn), so the first relation could never
            // resolve; the second nests a flat combo list under every master row for no reason.
            // Both are copy-paste debris from the payroll-sheet report this one was cloned from.
            // Nothing in the layout consumes either relation -- the PrmCompany / PrmDepartment
            // combos bind straight to the data source via DynamicListLookUpSettings.DataMember --
            // and in v23.2 an unresolvable relation column is a hard error at document creation
            // rather than the silent no-op it was in v17.2.
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel10,
            this.xrLabel8,
            this.xrLabel7,
            this.xrLabel104,
            this.xrLabel103,
            this.xrLabel41,
            this.xrLabel40,
            this.xrLabel39,
            this.xrLabel36,
            this.xrLabel28,
            this.xrLabel27});
            this.Detail.HeightF = 24.89584F;
            this.Detail.KeepTogether = true;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabel10
            // 
            this.xrLabel10.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel10.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel10.CanGrow = false;
            this.xrLabel10.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DiffEmployerContribution]")});
            this.xrLabel10.Font = new System.Drawing.Font("Tahoma", 8F);
            this.xrLabel10.LocationFloat = new DevExpress.Utils.PointFloat(538.9441F, 0F);
            this.xrLabel10.Name = "xrLabel10";
            this.xrLabel10.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel10.SizeF = new System.Drawing.SizeF(67.68732F, 24.89584F);
            this.xrLabel10.StylePriority.UseBorderColor = false;
            this.xrLabel10.StylePriority.UseBorders = false;
            this.xrLabel10.StylePriority.UseFont = false;
            this.xrLabel10.StylePriority.UsePadding = false;
            this.xrLabel10.StylePriority.UseTextAlignment = false;
            this.xrLabel10.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel10.TextFormatString = "{0:#0.000}";
            this.xrLabel10.WordWrap = false;
            // 
            // xrLabel8
            // 
            this.xrLabel8.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel8.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel8.CanGrow = false;
            this.xrLabel8.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[EmployerPIFSSDed]")});
            this.xrLabel8.Font = new System.Drawing.Font("Tahoma", 8F);
            this.xrLabel8.LocationFloat = new DevExpress.Utils.PointFloat(401.99F, 0F);
            this.xrLabel8.Name = "xrLabel8";
            this.xrLabel8.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel8.SizeF = new System.Drawing.SizeF(70F, 24.89584F);
            this.xrLabel8.StylePriority.UseBorderColor = false;
            this.xrLabel8.StylePriority.UseBorders = false;
            this.xrLabel8.StylePriority.UseFont = false;
            this.xrLabel8.StylePriority.UsePadding = false;
            this.xrLabel8.StylePriority.UseTextAlignment = false;
            this.xrLabel8.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel8.TextFormatString = "{0:#0.000}";
            this.xrLabel8.WordWrap = false;
            // 
            // xrLabel7
            // 
            this.xrLabel7.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel7.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel7.CanGrow = false;
            this.xrLabel7.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DiffEmployeeContribution]")});
            this.xrLabel7.Font = new System.Drawing.Font("Tahoma", 8F);
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(332.9441F, 0F);
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel7.SizeF = new System.Drawing.SizeF(69.04587F, 24.89584F);
            this.xrLabel7.StylePriority.UseBorderColor = false;
            this.xrLabel7.StylePriority.UseBorders = false;
            this.xrLabel7.StylePriority.UseFont = false;
            this.xrLabel7.StylePriority.UsePadding = false;
            this.xrLabel7.StylePriority.UseTextAlignment = false;
            this.xrLabel7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel7.TextFormatString = "{0:#0.000}";
            this.xrLabel7.WordWrap = false;
            // 
            // xrLabel104
            // 
            this.xrLabel104.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel104.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel104.CanGrow = false;
            this.xrLabel104.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TotalClaimAmount]")});
            this.xrLabel104.Font = new System.Drawing.Font("Tahoma", 8F);
            this.xrLabel104.LocationFloat = new DevExpress.Utils.PointFloat(676.6314F, 0F);
            this.xrLabel104.Name = "xrLabel104";
            this.xrLabel104.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel104.SizeF = new System.Drawing.SizeF(72.86908F, 24.89584F);
            this.xrLabel104.StylePriority.UseBorderColor = false;
            this.xrLabel104.StylePriority.UseBorders = false;
            this.xrLabel104.StylePriority.UseFont = false;
            this.xrLabel104.StylePriority.UsePadding = false;
            this.xrLabel104.StylePriority.UseTextAlignment = false;
            this.xrLabel104.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel104.TextFormatString = "{0:#0.000}";
            this.xrLabel104.WordWrap = false;
            // 
            // xrLabel103
            // 
            this.xrLabel103.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel103.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel103.CanGrow = false;
            this.xrLabel103.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[DiffInTotalDed]")});
            this.xrLabel103.Font = new System.Drawing.Font("Tahoma", 8F);
            this.xrLabel103.LocationFloat = new DevExpress.Utils.PointFloat(749.5005F, 0F);
            this.xrLabel103.Name = "xrLabel103";
            this.xrLabel103.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel103.SizeF = new System.Drawing.SizeF(67.49951F, 24.89584F);
            this.xrLabel103.StylePriority.UseBorderColor = false;
            this.xrLabel103.StylePriority.UseBorders = false;
            this.xrLabel103.StylePriority.UseFont = false;
            this.xrLabel103.StylePriority.UsePadding = false;
            this.xrLabel103.StylePriority.UseTextAlignment = false;
            this.xrLabel103.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel103.TextFormatString = "{0:#0.000}";
            this.xrLabel103.WordWrap = false;
            // 
            // xrLabel41
            // 
            this.xrLabel41.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel41.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel41.CanGrow = false;
            this.xrLabel41.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[TotalPIFSSDed]")});
            this.xrLabel41.Font = new System.Drawing.Font("Tahoma", 8F);
            this.xrLabel41.LocationFloat = new DevExpress.Utils.PointFloat(606.6314F, 0F);
            this.xrLabel41.Name = "xrLabel41";
            this.xrLabel41.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel41.SizeF = new System.Drawing.SizeF(70F, 24.89584F);
            this.xrLabel41.StylePriority.UseBorderColor = false;
            this.xrLabel41.StylePriority.UseBorders = false;
            this.xrLabel41.StylePriority.UseFont = false;
            this.xrLabel41.StylePriority.UsePadding = false;
            this.xrLabel41.StylePriority.UseTextAlignment = false;
            this.xrLabel41.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel41.TextFormatString = "{0:#0.000}";
            this.xrLabel41.WordWrap = false;
            // 
            // xrLabel40
            // 
            this.xrLabel40.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel40.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel40.CanGrow = false;
            this.xrLabel40.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[EmployerClaimAmount]")});
            this.xrLabel40.Font = new System.Drawing.Font("Tahoma", 8F);
            this.xrLabel40.LocationFloat = new DevExpress.Utils.PointFloat(471.99F, 0F);
            this.xrLabel40.Name = "xrLabel40";
            this.xrLabel40.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel40.SizeF = new System.Drawing.SizeF(66.95413F, 24.89584F);
            this.xrLabel40.StylePriority.UseBorderColor = false;
            this.xrLabel40.StylePriority.UseBorders = false;
            this.xrLabel40.StylePriority.UseFont = false;
            this.xrLabel40.StylePriority.UsePadding = false;
            this.xrLabel40.StylePriority.UseTextAlignment = false;
            this.xrLabel40.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel40.TextFormatString = "{0:#0.000}";
            this.xrLabel40.WordWrap = false;
            // 
            // xrLabel39
            // 
            this.xrLabel39.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel39.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel39.CanGrow = false;
            this.xrLabel39.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[EmployeeClaimAmount]")});
            this.xrLabel39.Font = new System.Drawing.Font("Tahoma", 8F);
            this.xrLabel39.LocationFloat = new DevExpress.Utils.PointFloat(264.9441F, 0F);
            this.xrLabel39.Name = "xrLabel39";
            this.xrLabel39.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel39.SizeF = new System.Drawing.SizeF(68F, 24.89584F);
            this.xrLabel39.StylePriority.UseBorderColor = false;
            this.xrLabel39.StylePriority.UseBorders = false;
            this.xrLabel39.StylePriority.UseFont = false;
            this.xrLabel39.StylePriority.UsePadding = false;
            this.xrLabel39.StylePriority.UseTextAlignment = false;
            this.xrLabel39.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel39.TextFormatString = "{0:#0.000}";
            this.xrLabel39.WordWrap = false;
            // 
            // xrLabel36
            // 
            this.xrLabel36.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel36.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel36.CanGrow = false;
            this.xrLabel36.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[EmployeePIFSSDed]")});
            this.xrLabel36.Font = new System.Drawing.Font("Tahoma", 8F);
            this.xrLabel36.LocationFloat = new DevExpress.Utils.PointFloat(191.9324F, 0F);
            this.xrLabel36.Name = "xrLabel36";
            this.xrLabel36.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel36.SizeF = new System.Drawing.SizeF(73.01172F, 24.89584F);
            this.xrLabel36.StylePriority.UseBorderColor = false;
            this.xrLabel36.StylePriority.UseBorders = false;
            this.xrLabel36.StylePriority.UseFont = false;
            this.xrLabel36.StylePriority.UsePadding = false;
            this.xrLabel36.StylePriority.UseTextAlignment = false;
            this.xrLabel36.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel36.TextFormatString = "{0:#0.000}";
            this.xrLabel36.WordWrap = false;
            // 
            // xrLabel28
            // 
            this.xrLabel28.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel28.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel28.CanGrow = false;
            this.xrLabel28.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[FullNameEn]")});
            this.xrLabel28.Font = new System.Drawing.Font("Tahoma", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel28.LocationFloat = new DevExpress.Utils.PointFloat(60.80791F, 0F);
            this.xrLabel28.Name = "xrLabel28";
            this.xrLabel28.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel28.SizeF = new System.Drawing.SizeF(131.1245F, 24.89584F);
            this.xrLabel28.StylePriority.UseBorderColor = false;
            this.xrLabel28.StylePriority.UseBorders = false;
            this.xrLabel28.StylePriority.UseFont = false;
            this.xrLabel28.StylePriority.UsePadding = false;
            this.xrLabel28.StylePriority.UseTextAlignment = false;
            this.xrLabel28.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel28.WordWrap = false;
            // 
            // xrLabel27
            // 
            this.xrLabel27.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel27.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel27.CanGrow = false;
            this.xrLabel27.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Code]")});
            this.xrLabel27.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel27.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrLabel27.Name = "xrLabel27";
            this.xrLabel27.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel27.SizeF = new System.Drawing.SizeF(60.80791F, 24.89584F);
            this.xrLabel27.StylePriority.UseBorderColor = false;
            this.xrLabel27.StylePriority.UseBorders = false;
            this.xrLabel27.StylePriority.UseFont = false;
            this.xrLabel27.StylePriority.UsePadding = false;
            this.xrLabel27.StylePriority.UseTextAlignment = false;
            this.xrLabel27.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel27.WordWrap = false;
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
            this.TopMargin.HeightF = 20F;
            this.TopMargin.Name = "TopMargin";
            this.TopMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
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
            this.xrLabel3,
            this.xrLabel102,
            this.xrLabel101,
            this.xrLabel1,
            this.xrLabel24,
            this.xrLabel23,
            this.xrLabel22,
            this.xrLabel20,
            this.xrLabel18,
            this.xrLabel2,
            this.xrLabel6});
            this.PageHeader.HeightF = 50.00002F;
            this.PageHeader.Name = "PageHeader";
            // 
            // xrLabel3
            // 
            this.xrLabel3.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel3.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel3.CanGrow = false;
            this.xrLabel3.Font = new System.Drawing.Font("Tahoma", 7F, System.Drawing.FontStyle.Bold);
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(538.9441F, 0F);
            this.xrLabel3.Multiline = true;
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.SizeF = new System.Drawing.SizeF(67.68726F, 50F);
            this.xrLabel3.StylePriority.UseBorderColor = false;
            this.xrLabel3.StylePriority.UseBorders = false;
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.StylePriority.UsePadding = false;
            this.xrLabel3.StylePriority.UseTextAlignment = false;
            this.xrLabel3.Text = "Difference \r\nEmployer Contribution";
            this.xrLabel3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel102
            // 
            this.xrLabel102.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel102.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel102.CanGrow = false;
            this.xrLabel102.Font = new System.Drawing.Font("Tahoma", 7F, System.Drawing.FontStyle.Bold);
            this.xrLabel102.LocationFloat = new DevExpress.Utils.PointFloat(676.6314F, 0F);
            this.xrLabel102.Multiline = true;
            this.xrLabel102.Name = "xrLabel102";
            this.xrLabel102.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel102.SizeF = new System.Drawing.SizeF(72.86908F, 50F);
            this.xrLabel102.StylePriority.UseBorderColor = false;
            this.xrLabel102.StylePriority.UseBorders = false;
            this.xrLabel102.StylePriority.UseFont = false;
            this.xrLabel102.StylePriority.UsePadding = false;
            this.xrLabel102.StylePriority.UseTextAlignment = false;
            this.xrLabel102.Text = "Total Deduction\r\nAs per Claim PIFSS";
            this.xrLabel102.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel101
            // 
            this.xrLabel101.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel101.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel101.CanGrow = false;
            this.xrLabel101.Font = new System.Drawing.Font("Tahoma", 7F, System.Drawing.FontStyle.Bold);
            this.xrLabel101.LocationFloat = new DevExpress.Utils.PointFloat(749.5005F, 0F);
            this.xrLabel101.Multiline = true;
            this.xrLabel101.Name = "xrLabel101";
            this.xrLabel101.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel101.SizeF = new System.Drawing.SizeF(67.49951F, 50F);
            this.xrLabel101.StylePriority.UseBorderColor = false;
            this.xrLabel101.StylePriority.UseBorders = false;
            this.xrLabel101.StylePriority.UseFont = false;
            this.xrLabel101.StylePriority.UsePadding = false;
            this.xrLabel101.StylePriority.UseTextAlignment = false;
            this.xrLabel101.Text = "Difference in Total Deduction";
            this.xrLabel101.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel1
            // 
            this.xrLabel1.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel1.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel1.CanGrow = false;
            this.xrLabel1.Font = new System.Drawing.Font("Tahoma", 7F, System.Drawing.FontStyle.Bold);
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(400.9441F, 0F);
            this.xrLabel1.Multiline = true;
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(70F, 50F);
            this.xrLabel1.StylePriority.UseBorderColor = false;
            this.xrLabel1.StylePriority.UseBorders = false;
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UsePadding = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.Text = "Employer Contribution HRMS";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel24
            // 
            this.xrLabel24.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel24.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel24.CanGrow = false;
            this.xrLabel24.Font = new System.Drawing.Font("Tahoma", 7F, System.Drawing.FontStyle.Bold);
            this.xrLabel24.LocationFloat = new DevExpress.Utils.PointFloat(606.6313F, 0F);
            this.xrLabel24.Multiline = true;
            this.xrLabel24.Name = "xrLabel24";
            this.xrLabel24.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel24.SizeF = new System.Drawing.SizeF(70.00006F, 50.00002F);
            this.xrLabel24.StylePriority.UseBorderColor = false;
            this.xrLabel24.StylePriority.UseBorders = false;
            this.xrLabel24.StylePriority.UseFont = false;
            this.xrLabel24.StylePriority.UsePadding = false;
            this.xrLabel24.StylePriority.UseTextAlignment = false;
            this.xrLabel24.Text = "Total Deduction HRMS";
            this.xrLabel24.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel23
            // 
            this.xrLabel23.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel23.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel23.CanGrow = false;
            this.xrLabel23.Font = new System.Drawing.Font("Tahoma", 7F, System.Drawing.FontStyle.Bold);
            this.xrLabel23.LocationFloat = new DevExpress.Utils.PointFloat(470.9441F, 0F);
            this.xrLabel23.Multiline = true;
            this.xrLabel23.Name = "xrLabel23";
            this.xrLabel23.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel23.SizeF = new System.Drawing.SizeF(68F, 50F);
            this.xrLabel23.StylePriority.UseBorderColor = false;
            this.xrLabel23.StylePriority.UseBorders = false;
            this.xrLabel23.StylePriority.UseFont = false;
            this.xrLabel23.StylePriority.UsePadding = false;
            this.xrLabel23.StylePriority.UseTextAlignment = false;
            this.xrLabel23.Text = "Employer Contribution\r\nAs per PIFSS claim";
            this.xrLabel23.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel22
            // 
            this.xrLabel22.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel22.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel22.CanGrow = false;
            this.xrLabel22.Font = new System.Drawing.Font("Tahoma", 7F, System.Drawing.FontStyle.Bold);
            this.xrLabel22.LocationFloat = new DevExpress.Utils.PointFloat(264.9441F, 0F);
            this.xrLabel22.Multiline = true;
            this.xrLabel22.Name = "xrLabel22";
            this.xrLabel22.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel22.SizeF = new System.Drawing.SizeF(68F, 50F);
            this.xrLabel22.StylePriority.UseBorderColor = false;
            this.xrLabel22.StylePriority.UseBorders = false;
            this.xrLabel22.StylePriority.UseFont = false;
            this.xrLabel22.StylePriority.UsePadding = false;
            this.xrLabel22.StylePriority.UseTextAlignment = false;
            this.xrLabel22.Text = "Employee Contribution\r\nAs per PIFSS claim";
            this.xrLabel22.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel20
            // 
            this.xrLabel20.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel20.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel20.CanGrow = false;
            this.xrLabel20.Font = new System.Drawing.Font("Tahoma", 7F, System.Drawing.FontStyle.Bold);
            this.xrLabel20.LocationFloat = new DevExpress.Utils.PointFloat(332.9441F, 0F);
            this.xrLabel20.Name = "xrLabel20";
            this.xrLabel20.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel20.SizeF = new System.Drawing.SizeF(68F, 50F);
            this.xrLabel20.StylePriority.UseBorderColor = false;
            this.xrLabel20.StylePriority.UseBorders = false;
            this.xrLabel20.StylePriority.UseFont = false;
            this.xrLabel20.StylePriority.UsePadding = false;
            this.xrLabel20.StylePriority.UseTextAlignment = false;
            this.xrLabel20.Text = "Difference in Employee Contribution";
            this.xrLabel20.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel18
            // 
            this.xrLabel18.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel18.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel18.CanGrow = false;
            this.xrLabel18.Font = new System.Drawing.Font("Tahoma", 7F, System.Drawing.FontStyle.Bold);
            this.xrLabel18.LocationFloat = new DevExpress.Utils.PointFloat(191.9324F, 0F);
            this.xrLabel18.Name = "xrLabel18";
            this.xrLabel18.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel18.SizeF = new System.Drawing.SizeF(73.0117F, 50.00002F);
            this.xrLabel18.StylePriority.UseBorderColor = false;
            this.xrLabel18.StylePriority.UseBorders = false;
            this.xrLabel18.StylePriority.UseFont = false;
            this.xrLabel18.StylePriority.UsePadding = false;
            this.xrLabel18.StylePriority.UseTextAlignment = false;
            this.xrLabel18.Text = "Employee Contribution";
            this.xrLabel18.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel2
            // 
            this.xrLabel2.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel2.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel2.CanGrow = false;
            this.xrLabel2.Font = new System.Drawing.Font("Tahoma", 7F, System.Drawing.FontStyle.Bold);
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(60.80791F, 50F);
            this.xrLabel2.StylePriority.UseBorderColor = false;
            this.xrLabel2.StylePriority.UseBorders = false;
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UsePadding = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = "Employee Code";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel6
            // 
            this.xrLabel6.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel6.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel6.CanGrow = false;
            this.xrLabel6.Font = new System.Drawing.Font("Tahoma", 7F, System.Drawing.FontStyle.Bold);
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(60.80791F, 0F);
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(131.1245F, 50F);
            this.xrLabel6.StylePriority.UseBorderColor = false;
            this.xrLabel6.StylePriority.UseBorders = false;
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UsePadding = false;
            this.xrLabel6.StylePriority.UseTextAlignment = false;
            this.xrLabel6.Text = "Employee Name";
            this.xrLabel6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // lblHeader
            // 
            this.lblHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(100)))), ((int)(((byte)(160)))));
            this.lblHeader.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.lblHeader.ForeColor = System.Drawing.Color.White;
            this.lblHeader.LocationFloat = new DevExpress.Utils.PointFloat(0.2119621F, 10.00001F);
            this.lblHeader.Name = "lblHeader";
            this.lblHeader.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblHeader.SizeF = new System.Drawing.SizeF(816.788F, 23.00001F);
            this.lblHeader.StylePriority.UseBackColor = false;
            this.lblHeader.StylePriority.UseFont = false;
            this.lblHeader.StylePriority.UseForeColor = false;
            this.lblHeader.StylePriority.UseTextAlignment = false;
            this.lblHeader.Text = "lblHeader";
            this.lblHeader.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.lblHeader.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.lblHeader_BeforePrint);
            // 
            // xrLabel9
            // 
            this.xrLabel9.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel9.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel9.LocationFloat = new DevExpress.Utils.PointFloat(0.0006993611F, 40.62497F);
            this.xrLabel9.Name = "xrLabel9";
            this.xrLabel9.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel9.SizeF = new System.Drawing.SizeF(113.1404F, 25F);
            this.xrLabel9.StylePriority.UseBorders = false;
            this.xrLabel9.StylePriority.UseFont = false;
            this.xrLabel9.StylePriority.UsePadding = false;
            this.xrLabel9.StylePriority.UseTextAlignment = false;
            this.xrLabel9.Text = "Payroll Area :";
            this.xrLabel9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel9.Visible = false;
            // 
            // xrLabel4
            // 
            this.xrLabel4.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(646.9995F, 40.62497F);
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(58.95728F, 25F);
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UseTextAlignment = false;
            this.xrLabel4.Text = "Date :";
            this.xrLabel4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel4.Visible = false;
            // 
            // xrLabel5
            // 
            this.xrLabel5.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters].[PrmSheetDate]")});
            this.xrLabel5.Font = new System.Drawing.Font("Tahoma", 10.2F, System.Drawing.FontStyle.Bold);
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(705.9568F, 40.62497F);
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.SizeF = new System.Drawing.SizeF(111.0432F, 25F);
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.StylePriority.UseTextAlignment = false;
            this.xrLabel5.Text = "xrLabel3";
            this.xrLabel5.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel5.TextFormatString = "{0:dd/MM/yyyy}";
            this.xrLabel5.Visible = false;
            // 
            // xrLabel16
            // 
            this.xrLabel16.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.xrLabel16.CanGrow = false;
            this.xrLabel16.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[PayrollArea]")});
            this.xrLabel16.Font = new System.Drawing.Font("Tahoma", 11F, System.Drawing.FontStyle.Bold);
            this.xrLabel16.LocationFloat = new DevExpress.Utils.PointFloat(113.1411F, 40.62497F);
            this.xrLabel16.Name = "xrLabel16";
            this.xrLabel16.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel16.SizeF = new System.Drawing.SizeF(520.4379F, 25F);
            this.xrLabel16.StylePriority.UseBorders = false;
            this.xrLabel16.StylePriority.UseFont = false;
            this.xrLabel16.StylePriority.UsePadding = false;
            this.xrLabel16.StylePriority.UseTextAlignment = false;
            this.xrLabel16.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel16.Visible = false;
            // 
            // ReportFooter
            // 
            this.ReportFooter.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel33,
            this.xrLabel32,
            this.xrLabel31,
            this.xrLabel171,
            this.xrLabel123,
            this.xrLabel122,
            this.xrLabel48,
            this.xrLabel52,
            this.xrLabel54,
            this.xrLabel55});
            this.ReportFooter.HeightF = 24.89584F;
            this.ReportFooter.Name = "ReportFooter";
            // 
            // xrLabel33
            // 
            this.xrLabel33.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel33.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel33.CanGrow = false;
            this.xrLabel33.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumSum([EmployerPIFSSDed])")});
            this.xrLabel33.Font = new System.Drawing.Font("Tahoma", 6.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel33.LocationFloat = new DevExpress.Utils.PointFloat(401.99F, 0F);
            this.xrLabel33.Name = "xrLabel33";
            this.xrLabel33.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel33.SizeF = new System.Drawing.SizeF(70F, 24.89584F);
            this.xrLabel33.StylePriority.UseBorderColor = false;
            this.xrLabel33.StylePriority.UseBorders = false;
            this.xrLabel33.StylePriority.UseFont = false;
            this.xrLabel33.StylePriority.UsePadding = false;
            this.xrLabel33.StylePriority.UseTextAlignment = false;
            xrSummary1.Running = DevExpress.XtraReports.UI.SummaryRunning.Report;
            this.xrLabel33.Summary = xrSummary1;
            this.xrLabel33.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel33.TextFormatString = "{0:#0.000}";
            // 
            // xrLabel32
            // 
            this.xrLabel32.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel32.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel32.CanGrow = false;
            this.xrLabel32.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumSum([DiffEmployeeContribution])")});
            this.xrLabel32.Font = new System.Drawing.Font("Tahoma", 6.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel32.LocationFloat = new DevExpress.Utils.PointFloat(332.946F, 0F);
            this.xrLabel32.Name = "xrLabel32";
            this.xrLabel32.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel32.SizeF = new System.Drawing.SizeF(69.04395F, 24.89584F);
            this.xrLabel32.StylePriority.UseBorderColor = false;
            this.xrLabel32.StylePriority.UseBorders = false;
            this.xrLabel32.StylePriority.UseFont = false;
            this.xrLabel32.StylePriority.UsePadding = false;
            this.xrLabel32.StylePriority.UseTextAlignment = false;
            xrSummary2.Running = DevExpress.XtraReports.UI.SummaryRunning.Report;
            this.xrLabel32.Summary = xrSummary2;
            this.xrLabel32.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel32.TextFormatString = "{0:#0.000}";
            // 
            // xrLabel31
            // 
            this.xrLabel31.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel31.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel31.CanGrow = false;
            this.xrLabel31.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumSum([DiffEmployerContribution])")});
            this.xrLabel31.Font = new System.Drawing.Font("Tahoma", 6.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel31.LocationFloat = new DevExpress.Utils.PointFloat(538.9442F, 0F);
            this.xrLabel31.Name = "xrLabel31";
            this.xrLabel31.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel31.SizeF = new System.Drawing.SizeF(67.68726F, 24.89584F);
            this.xrLabel31.StylePriority.UseBorderColor = false;
            this.xrLabel31.StylePriority.UseBorders = false;
            this.xrLabel31.StylePriority.UseFont = false;
            this.xrLabel31.StylePriority.UsePadding = false;
            this.xrLabel31.StylePriority.UseTextAlignment = false;
            xrSummary3.Running = DevExpress.XtraReports.UI.SummaryRunning.Report;
            this.xrLabel31.Summary = xrSummary3;
            this.xrLabel31.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel31.TextFormatString = "{0:#0.000}";
            // 
            // xrLabel171
            // 
            this.xrLabel171.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel171.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel171.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel171.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrLabel171.Multiline = true;
            this.xrLabel171.Name = "xrLabel171";
            this.xrLabel171.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel171.SizeF = new System.Drawing.SizeF(191.9324F, 24.89584F);
            this.xrLabel171.StylePriority.UseBorderColor = false;
            this.xrLabel171.StylePriority.UseBorders = false;
            this.xrLabel171.StylePriority.UseFont = false;
            this.xrLabel171.StylePriority.UseTextAlignment = false;
            this.xrLabel171.Text = "TOTAL :";
            this.xrLabel171.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel123
            // 
            this.xrLabel123.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel123.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel123.CanGrow = false;
            this.xrLabel123.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumSum([DiffInTotalDed])")});
            this.xrLabel123.Font = new System.Drawing.Font("Tahoma", 6.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel123.LocationFloat = new DevExpress.Utils.PointFloat(749.5005F, 0F);
            this.xrLabel123.Name = "xrLabel123";
            this.xrLabel123.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel123.SizeF = new System.Drawing.SizeF(67.49951F, 24.89584F);
            this.xrLabel123.StylePriority.UseBorderColor = false;
            this.xrLabel123.StylePriority.UseBorders = false;
            this.xrLabel123.StylePriority.UseFont = false;
            this.xrLabel123.StylePriority.UsePadding = false;
            this.xrLabel123.StylePriority.UseTextAlignment = false;
            xrSummary4.Running = DevExpress.XtraReports.UI.SummaryRunning.Report;
            this.xrLabel123.Summary = xrSummary4;
            this.xrLabel123.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel123.TextFormatString = "{0:#0.000}";
            // 
            // xrLabel122
            // 
            this.xrLabel122.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel122.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel122.CanGrow = false;
            this.xrLabel122.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumSum([TotalClaimAmount])")});
            this.xrLabel122.Font = new System.Drawing.Font("Tahoma", 6.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel122.LocationFloat = new DevExpress.Utils.PointFloat(676.6314F, 0F);
            this.xrLabel122.Name = "xrLabel122";
            this.xrLabel122.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel122.SizeF = new System.Drawing.SizeF(72.86908F, 24.89584F);
            this.xrLabel122.StylePriority.UseBorderColor = false;
            this.xrLabel122.StylePriority.UseBorders = false;
            this.xrLabel122.StylePriority.UseFont = false;
            this.xrLabel122.StylePriority.UsePadding = false;
            this.xrLabel122.StylePriority.UseTextAlignment = false;
            xrSummary5.Running = DevExpress.XtraReports.UI.SummaryRunning.Report;
            this.xrLabel122.Summary = xrSummary5;
            this.xrLabel122.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel122.TextFormatString = "{0:#0.000}";
            // 
            // xrLabel48
            // 
            this.xrLabel48.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel48.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel48.CanGrow = false;
            this.xrLabel48.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumSum([TotalPIFSSDed])")});
            this.xrLabel48.Font = new System.Drawing.Font("Tahoma", 6.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel48.LocationFloat = new DevExpress.Utils.PointFloat(606.6314F, 0F);
            this.xrLabel48.Name = "xrLabel48";
            this.xrLabel48.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel48.SizeF = new System.Drawing.SizeF(70F, 24.89584F);
            this.xrLabel48.StylePriority.UseBorderColor = false;
            this.xrLabel48.StylePriority.UseBorders = false;
            this.xrLabel48.StylePriority.UseFont = false;
            this.xrLabel48.StylePriority.UsePadding = false;
            this.xrLabel48.StylePriority.UseTextAlignment = false;
            xrSummary6.Running = DevExpress.XtraReports.UI.SummaryRunning.Report;
            this.xrLabel48.Summary = xrSummary6;
            this.xrLabel48.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel48.TextFormatString = "{0:#0.000}";
            // 
            // xrLabel52
            // 
            this.xrLabel52.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel52.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel52.CanGrow = false;
            this.xrLabel52.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumSum([EmployeePIFSSDed])")});
            this.xrLabel52.Font = new System.Drawing.Font("Tahoma", 6.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel52.LocationFloat = new DevExpress.Utils.PointFloat(191.9343F, 0F);
            this.xrLabel52.Name = "xrLabel52";
            this.xrLabel52.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel52.SizeF = new System.Drawing.SizeF(73.00983F, 24.89584F);
            this.xrLabel52.StylePriority.UseBorderColor = false;
            this.xrLabel52.StylePriority.UseBorders = false;
            this.xrLabel52.StylePriority.UseFont = false;
            this.xrLabel52.StylePriority.UsePadding = false;
            this.xrLabel52.StylePriority.UseTextAlignment = false;
            xrSummary7.Running = DevExpress.XtraReports.UI.SummaryRunning.Report;
            this.xrLabel52.Summary = xrSummary7;
            this.xrLabel52.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel52.TextFormatString = "{0:#0.000}";
            // 
            // xrLabel54
            // 
            this.xrLabel54.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel54.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel54.CanGrow = false;
            this.xrLabel54.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumSum([EmployeeClaimAmount])")});
            this.xrLabel54.Font = new System.Drawing.Font("Tahoma", 6.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel54.LocationFloat = new DevExpress.Utils.PointFloat(264.9441F, 0F);
            this.xrLabel54.Name = "xrLabel54";
            this.xrLabel54.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel54.SizeF = new System.Drawing.SizeF(68F, 24.89584F);
            this.xrLabel54.StylePriority.UseBorderColor = false;
            this.xrLabel54.StylePriority.UseBorders = false;
            this.xrLabel54.StylePriority.UseFont = false;
            this.xrLabel54.StylePriority.UsePadding = false;
            this.xrLabel54.StylePriority.UseTextAlignment = false;
            xrSummary8.Running = DevExpress.XtraReports.UI.SummaryRunning.Report;
            this.xrLabel54.Summary = xrSummary8;
            this.xrLabel54.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel54.TextFormatString = "{0:#0.000}";
            // 
            // xrLabel55
            // 
            this.xrLabel55.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel55.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel55.CanGrow = false;
            this.xrLabel55.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "sumSum([EmployerClaimAmount])")});
            this.xrLabel55.Font = new System.Drawing.Font("Tahoma", 6.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel55.LocationFloat = new DevExpress.Utils.PointFloat(471.99F, 0F);
            this.xrLabel55.Name = "xrLabel55";
            this.xrLabel55.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel55.SizeF = new System.Drawing.SizeF(66.9541F, 24.89584F);
            this.xrLabel55.StylePriority.UseBorderColor = false;
            this.xrLabel55.StylePriority.UseBorders = false;
            this.xrLabel55.StylePriority.UseFont = false;
            this.xrLabel55.StylePriority.UsePadding = false;
            this.xrLabel55.StylePriority.UseTextAlignment = false;
            xrSummary9.Running = DevExpress.XtraReports.UI.SummaryRunning.Report;
            this.xrLabel55.Summary = xrSummary9;
            this.xrLabel55.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel55.TextFormatString = "{0:#0.000}";
            // 
            // PageFooter
            // 
            this.PageFooter.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel11,
            this.xrLabel13,
            this.xrLabel12,
            this.lblUserlogin,
            this.xrPageInfo1});
            this.PageFooter.HeightF = 52.06242F;
            this.PageFooter.Name = "PageFooter";
            // 
            // xrPageInfo1
            // 
            this.xrPageInfo1.Font = new System.Drawing.Font("Tahoma", 10.2F);
            this.xrPageInfo1.LocationFloat = new DevExpress.Utils.PointFloat(0.21197F, 0F);
            this.xrPageInfo1.Name = "xrPageInfo1";
            this.xrPageInfo1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrPageInfo1.SizeF = new System.Drawing.SizeF(816.788F, 23F);
            this.xrPageInfo1.StylePriority.UseFont = false;
            this.xrPageInfo1.StylePriority.UseTextAlignment = false;
            this.xrPageInfo1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
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
            this.ReportHeader.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel4,
            this.xrLabel5,
            this.xrLabel9,
            this.xrLabel16,
            this.lblHeader});
            this.ReportHeader.HeightF = 71.875F;
            this.ReportHeader.Name = "ReportHeader";
            // 
            // PrmCompany
            // 
            this.PrmCompany.Description = "Company";
            dynamicListLookUpSettings1.DataAdapter = null;
            dynamicListLookUpSettings1.DataMember = "dsCompany";
            dynamicListLookUpSettings1.DataSource = this.PayrollSheetDS;
            dynamicListLookUpSettings1.DisplayMember = "CompanyEn";
            dynamicListLookUpSettings1.ValueMember = "CompanyEn";
            this.PrmCompany.LookUpSettings = dynamicListLookUpSettings1;
            this.PrmCompany.Name = "PrmCompany";
            // 
            // PrmDepartment
            // 
            this.PrmDepartment.Description = "Department";
            dynamicListLookUpSettings2.DataAdapter = null;
            dynamicListLookUpSettings2.DataMember = "dsDepartment";
            dynamicListLookUpSettings2.DataSource = this.PayrollSheetDS;
            dynamicListLookUpSettings2.DisplayMember = "DepartmentEn";
            dynamicListLookUpSettings2.ValueMember = "DepartmentEn";
            this.PrmDepartment.LookUpSettings = dynamicListLookUpSettings2;
            this.PrmDepartment.Name = "PrmDepartment";
            // 
            // PrmPayrollArea
            // 
            this.PrmPayrollArea.Description = "Payroll Area";
            dynamicListLookUpSettings3.DataAdapter = null;
            dynamicListLookUpSettings3.DataMember = "dsPayrollArea";
            dynamicListLookUpSettings3.DataSource = this.PayrollSheetDS;
            dynamicListLookUpSettings3.DisplayMember = "Name";
            dynamicListLookUpSettings3.ValueMember = "Name";
            this.PrmPayrollArea.LookUpSettings = dynamicListLookUpSettings3;
            this.PrmPayrollArea.Name = "PrmPayrollArea";
            // 
            // PrmYear
            // 
            this.PrmYear.Description = "Year";
            this.PrmYear.Name = "PrmYear";
            this.PrmYear.Type = typeof(int);
            this.PrmYear.ValueInfo = "0";
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
            // xrLabel12
            // 
            this.xrLabel12.Font = new System.Drawing.Font("Tahoma", 11F);
            this.xrLabel12.LocationFloat = new DevExpress.Utils.PointFloat(0.2119621F, 29.06248F);
            this.xrLabel12.Name = "xrLabel12";
            this.xrLabel12.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel12.SizeF = new System.Drawing.SizeF(231.3332F, 21.95834F);
            this.xrLabel12.StylePriority.UseFont = false;
            this.xrLabel12.StylePriority.UseTextAlignment = false;
            this.xrLabel12.Text = "Exported from Pinnacle HRMS by";
            this.xrLabel12.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // lblUserlogin
            // 
            this.lblUserlogin.CanGrow = false;
            this.lblUserlogin.Font = new System.Drawing.Font("Tahoma", 11F);
            this.lblUserlogin.LocationFloat = new DevExpress.Utils.PointFloat(231.5451F, 29.06245F);
            this.lblUserlogin.Name = "lblUserlogin";
            this.lblUserlogin.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblUserlogin.SizeF = new System.Drawing.SizeF(198.846F, 21.95836F);
            this.lblUserlogin.StylePriority.UseFont = false;
            this.lblUserlogin.StylePriority.UseTextAlignment = false;
            this.lblUserlogin.Text = "xrLabel2";
            this.lblUserlogin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel11
            // 
            this.xrLabel11.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel11.LocationFloat = new DevExpress.Utils.PointFloat(612.0815F, 29.06245F);
            this.xrLabel11.Name = "xrLabel11";
            this.xrLabel11.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel11.SizeF = new System.Drawing.SizeF(41.25049F, 21.95833F);
            this.xrLabel11.StylePriority.UseBackColor = false;
            this.xrLabel11.StylePriority.UseFont = false;
            this.xrLabel11.StylePriority.UseTextAlignment = false;
            this.xrLabel11.Text = "On :";
            this.xrLabel11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel13
            // 
            this.xrLabel13.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "LocalDateTimeNow()")});
            this.xrLabel13.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel13.LocationFloat = new DevExpress.Utils.PointFloat(653.332F, 29.06245F);
            this.xrLabel13.Name = "xrLabel13";
            this.xrLabel13.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel13.SizeF = new System.Drawing.SizeF(163.6679F, 22.99997F);
            this.xrLabel13.StylePriority.UseBackColor = false;
            this.xrLabel13.StylePriority.UseFont = false;
            this.xrLabel13.StylePriority.UseTextAlignment = false;
            this.xrLabel13.Text = "xrLabel2";
            this.xrLabel13.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel13.TextFormatString = "{0:dd/MM/yyyy hh:mm tt}";
            // 
            // rptSocialSecurity
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin,
            this.PageHeader,
            this.ReportFooter,
            this.PageFooter,
            this.ReportHeader});
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
            this.PayrollSheetDS});
            this.DataMember = "PayrollSheetQuery";
            this.DataSource = this.PayrollSheetDS;
            this.Margins = new System.Drawing.Printing.Margins(5, 5, 20, 1);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.PrmPayrollArea,
            this.PrmMonth,
            this.PrmYear,
            this.PrmPayrollRunType,
            this.PrmSheetDate,
            this.PrmDepartment,
            this.PrmCompany});
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.EngMainDataStyle,
            this.ENGDataStyle,
            this.xrControlStyle1});
            this.Version = "23.2";
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
        private DevExpress.XtraReports.UI.XRControlStyle xrControlStyle1;
        private DevExpress.XtraReports.UI.ReportHeaderBand ReportHeader;
        private DevExpress.XtraReports.UI.XRLabel xrLabel16;
        private DevExpress.XtraReports.UI.XRLabel xrLabel9;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.XRLabel xrLabel6;
        private DevExpress.XtraReports.UI.XRLabel xrLabel18;
        private DevExpress.XtraReports.UI.XRLabel xrLabel20;
        private DevExpress.XtraReports.UI.XRLabel xrLabel24;
        private DevExpress.XtraReports.UI.XRLabel xrLabel23;
        private DevExpress.XtraReports.UI.XRLabel xrLabel22;
        private DevExpress.XtraReports.Parameters.Parameter PrmCompany;
        private DevExpress.XtraReports.Parameters.Parameter PrmDepartment;
        private DevExpress.XtraReports.Parameters.Parameter PrmPayrollArea;
        private DevExpress.XtraReports.Parameters.Parameter PrmYear;
        private DevExpress.XtraReports.Parameters.Parameter PrmMonth;
        private DevExpress.XtraReports.UI.XRLabel xrLabel41;
        private DevExpress.XtraReports.UI.XRLabel xrLabel40;
        private DevExpress.XtraReports.UI.XRLabel xrLabel39;
        private DevExpress.XtraReports.UI.XRLabel xrLabel36;
        private DevExpress.XtraReports.UI.XRLabel xrLabel28;
        private DevExpress.XtraReports.UI.XRLabel xrLabel27;
        private DevExpress.XtraReports.UI.XRLabel xrLabel48;
        private DevExpress.XtraReports.UI.XRLabel xrLabel52;
        private DevExpress.XtraReports.UI.XRLabel xrLabel54;
        private DevExpress.XtraReports.UI.XRLabel xrLabel55;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        private DevExpress.XtraReports.UI.XRLabel xrLabel102;
        private DevExpress.XtraReports.UI.XRLabel xrLabel101;
        private DevExpress.XtraReports.UI.XRLabel xrLabel104;
        private DevExpress.XtraReports.UI.XRLabel xrLabel103;
        private DevExpress.XtraReports.UI.XRLabel xrLabel123;
        private DevExpress.XtraReports.UI.XRLabel xrLabel122;
        private DevExpress.XtraReports.UI.XRLabel xrLabel171;
        private DevExpress.XtraReports.UI.XRLabel xrLabel7;
        private DevExpress.XtraReports.UI.XRLabel xrLabel8;
        private DevExpress.XtraReports.UI.XRLabel xrLabel3;
        private DevExpress.XtraReports.UI.XRLabel xrLabel10;
        private DevExpress.XtraReports.UI.XRLabel xrLabel33;
        private DevExpress.XtraReports.UI.XRLabel xrLabel32;
        private DevExpress.XtraReports.UI.XRLabel xrLabel31;
        private DevExpress.XtraReports.UI.XRLabel xrLabel12;
        private DevExpress.XtraReports.UI.XRLabel lblUserlogin;
        private DevExpress.XtraReports.UI.XRLabel xrLabel11;
        private DevExpress.XtraReports.UI.XRLabel xrLabel13;
    }
}
