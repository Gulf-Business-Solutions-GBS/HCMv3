

namespace HCMv3.Reports.Payroll
{
    partial class rptLeaveSettlementAr
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
            // PORT NOTE: the legacy PayrollArea data member was a designer SelectQuery over the table
            // "PayrollArea" (columns Code, Name). A SelectQuery cannot express the mandatory
            // "TenantId = @TenantId AND IsDeleted = 0" predicate, so it becomes a CustomSqlQuery.
            // Its data-member name is unchanged, so the PrmPayrollArea look-up binding still resolves.
            DevExpress.DataAccess.Sql.CustomSqlQuery payrollAreaQuery = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.CustomSqlQuery customSqlQuery1 = new DevExpress.DataAccess.Sql.CustomSqlQuery();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter1 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter2 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter3 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter4 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter5 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.DataAccess.Sql.QueryParameter queryParameter6 = new DevExpress.DataAccess.Sql.QueryParameter();
            DevExpress.XtraReports.UI.XRSummary xrSummary1 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.UI.XRSummary xrSummary2 = new DevExpress.XtraReports.UI.XRSummary();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings1 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            DevExpress.XtraReports.Parameters.StaticListLookUpSettings staticListLookUpSettings2 = new DevExpress.XtraReports.Parameters.StaticListLookUpSettings();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            this.PayslipDS = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLabel51 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel47 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblUnpaidLeave = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel46 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblLeaveStartDate = new DevExpress.XtraReports.UI.XRLabel();
            this.lblPaidLeave = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel52 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel50 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblExpReturnDate = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel48 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel49 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel31 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel4 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel45 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblPaidLeave2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel41 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel42 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel43 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel44 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel39 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel40 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel37 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel35 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblLeaveEndDate = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel32 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel30 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel29 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel28 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel27 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel26 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel25 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel24 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel23 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel19 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel22 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel6 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel21 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel20 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel18 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel17 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel16 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel15 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel14 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel13 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel12 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel11 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel9 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel8 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.srptLeaveSettlementWorkingDaysSummaryEn = new DevExpress.XtraReports.UI.XRSubreport();
            this.PrmMonth = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmYear = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmPayrollRunType = new DevExpress.XtraReports.Parameters.Parameter();
            this.srptLeaveSettlementEarningAr = new DevExpress.XtraReports.UI.XRSubreport();
            this.srptLeaveSettlementDeductionAr = new DevExpress.XtraReports.UI.XRSubreport();
            this.xrPictureBox1 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.PrmEmpCode = new DevExpress.XtraReports.Parameters.Parameter();
            this.xrLabel5 = new DevExpress.XtraReports.UI.XRLabel();
            this.LoggedUserNme = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.EngMainDataStyle = new DevExpress.XtraReports.UI.XRControlStyle();
            this.ENGDataStyle = new DevExpress.XtraReports.UI.XRControlStyle();
            this.ReportHeader = new DevExpress.XtraReports.UI.ReportHeaderBand();
            this.CalcDateTime = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcNetPay = new DevExpress.XtraReports.UI.CalculatedField();
            this.PageFooter = new DevExpress.XtraReports.UI.PageFooterBand();
            this.PrmSendMail = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmEncashment = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmPayrollArea = new DevExpress.XtraReports.Parameters.Parameter();
            this.PrmPrintDate = new DevExpress.XtraReports.Parameters.Parameter();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel7 = new DevExpress.XtraReports.UI.XRLabel();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // PayslipDS
            // 
            this.PayslipDS.ConnectionName = HCMv3.Reports.HcmReportBase.DesignTimeConnectionName;
            this.PayslipDS.Name = "PayslipDS";
            payrollAreaQuery.Name = "PayrollArea";
            payrollAreaQuery.Sql = LeaveSettlementSql.PayrollArea;
            customSqlQuery1.Name = "PayslipOrgV2Query";
            queryParameter1.Name = "PrmEmpCode";
            queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmEmpCode]", typeof(string));
            queryParameter2.Name = "PrmMonth";
            queryParameter2.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter2.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmMonth]", typeof(int));
            queryParameter3.Name = "PrmYear";
            queryParameter3.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter3.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmYear]", typeof(int));
            queryParameter4.Name = "PrmPayrollRunType";
            queryParameter4.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter4.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmPayrollRunType]", typeof(int));
            queryParameter5.Name = "PrmPayrollArea";
            queryParameter5.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter5.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmPayrollArea]", typeof(string));
            queryParameter6.Name = "PrmEncashment";
            queryParameter6.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter6.Value = new DevExpress.DataAccess.Expression("[Parameters.PrmEncashment]", typeof(bool));
            customSqlQuery1.Parameters.Add(queryParameter1);
            customSqlQuery1.Parameters.Add(queryParameter2);
            customSqlQuery1.Parameters.Add(queryParameter3);
            customSqlQuery1.Parameters.Add(queryParameter4);
            customSqlQuery1.Parameters.Add(queryParameter5);
            customSqlQuery1.Parameters.Add(queryParameter6);
            customSqlQuery1.Sql = LeaveSettlementSql.LeaveSettlementMasterAr;
            this.PayslipDS.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            payrollAreaQuery,
            customSqlQuery1});
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel7,
            this.xrLabel2,
            this.xrLabel51,
            this.xrLabel47,
            this.lblUnpaidLeave,
            this.xrLabel46,
            this.lblLeaveStartDate,
            this.lblPaidLeave,
            this.xrLabel52,
            this.xrLabel50,
            this.lblExpReturnDate,
            this.xrLabel48,
            this.xrLabel49,
            this.xrLabel31,
            this.xrLabel4,
            this.xrLabel45,
            this.lblPaidLeave2,
            this.xrLabel41,
            this.xrLabel42,
            this.xrLabel43,
            this.xrLabel44,
            this.xrLabel39,
            this.xrLabel40,
            this.xrLabel37,
            this.xrLabel35,
            this.lblLeaveEndDate,
            this.xrLabel32,
            this.xrLabel30,
            this.xrLabel29,
            this.xrLabel28,
            this.xrLabel27,
            this.xrLabel26,
            this.xrLabel25,
            this.xrLabel24,
            this.xrLabel23,
            this.xrLabel19,
            this.xrLabel22,
            this.xrLabel6,
            this.xrLabel21,
            this.xrLabel20,
            this.xrLabel18,
            this.xrLabel17,
            this.xrLabel16,
            this.xrLabel15,
            this.xrLabel14,
            this.xrLabel13,
            this.xrLabel12,
            this.xrLabel11,
            this.xrLabel9,
            this.xrLabel8,
            this.xrLabel1,
            this.srptLeaveSettlementWorkingDaysSummaryEn,
            this.srptLeaveSettlementEarningAr,
            this.srptLeaveSettlementDeductionAr,
            this.xrPictureBox1});
            this.Detail.HeightF = 943.4166F;
            this.Detail.KeepTogether = true;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.PageBreak = DevExpress.XtraReports.UI.PageBreak.AfterBandExceptLastEntry;
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabel51
            // 
            this.xrLabel51.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel51.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel51.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel51.LocationFloat = new DevExpress.Utils.PointFloat(9.999939F, 299.2735F);
            this.xrLabel51.Name = "xrLabel51";
            this.xrLabel51.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel51.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel51.SizeF = new System.Drawing.SizeF(406.1962F, 23F);
            this.xrLabel51.StylePriority.UseBorderColor = false;
            this.xrLabel51.StylePriority.UseBorders = false;
            this.xrLabel51.StylePriority.UseFont = false;
            this.xrLabel51.StylePriority.UseTextAlignment = false;
            this.xrLabel51.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel47
            // 
            this.xrLabel47.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel47.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel47.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel47.LocationFloat = new DevExpress.Utils.PointFloat(278.0302F, 161.2735F);
            this.xrLabel47.Multiline = true;
            this.xrLabel47.Name = "xrLabel47";
            this.xrLabel47.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel47.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel47.SizeF = new System.Drawing.SizeF(138.595F, 23F);
            this.xrLabel47.StylePriority.UseBorderColor = false;
            this.xrLabel47.StylePriority.UseBorders = false;
            this.xrLabel47.StylePriority.UseFont = false;
            this.xrLabel47.StylePriority.UseTextAlignment = false;
            this.xrLabel47.Text = "تاريخ الطباعة\r\n";
            this.xrLabel47.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // lblUnpaidLeave
            // 
            this.lblUnpaidLeave.BorderColor = System.Drawing.Color.LightGray;
            this.lblUnpaidLeave.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.lblUnpaidLeave.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblUnpaidLeave.LocationFloat = new DevExpress.Utils.PointFloat(310.5588F, 345.2735F);
            this.lblUnpaidLeave.Name = "lblUnpaidLeave";
            this.lblUnpaidLeave.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblUnpaidLeave.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.lblUnpaidLeave.SizeF = new System.Drawing.SizeF(109.7423F, 23F);
            this.lblUnpaidLeave.StylePriority.UseBorderColor = false;
            this.lblUnpaidLeave.StylePriority.UseBorders = false;
            this.lblUnpaidLeave.StylePriority.UseFont = false;
            this.lblUnpaidLeave.StylePriority.UseTextAlignment = false;
            this.lblUnpaidLeave.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.lblUnpaidLeave.TextFormatString = "{0:#0.00}";
            // 
            // xrLabel46
            // 
            this.xrLabel46.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel46.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel46.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel46.BorderWidth = 2F;
            this.xrLabel46.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Currency]")});
            this.xrLabel46.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel46.LocationFloat = new DevExpress.Utils.PointFloat(606.7646F, 627.5389F);
            this.xrLabel46.Name = "xrLabel46";
            this.xrLabel46.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 5, 100F);
            this.xrLabel46.SizeF = new System.Drawing.SizeF(42.94159F, 22.99994F);
            this.xrLabel46.StyleName = "ENGDataStyle";
            this.xrLabel46.StylePriority.UseBorderColor = false;
            this.xrLabel46.StylePriority.UseBorderDashStyle = false;
            this.xrLabel46.StylePriority.UseBorders = false;
            this.xrLabel46.StylePriority.UseBorderWidth = false;
            this.xrLabel46.StylePriority.UseFont = false;
            this.xrLabel46.StylePriority.UsePadding = false;
            this.xrLabel46.StylePriority.UseTextAlignment = false;
            xrSummary1.IgnoreNullValues = true;
            this.xrLabel46.Summary = xrSummary1;
            this.xrLabel46.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel46.TextFormatString = " {0}";
            // 
            // lblLeaveStartDate
            // 
            this.lblLeaveStartDate.BorderColor = System.Drawing.Color.LightGray;
            this.lblLeaveStartDate.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.lblLeaveStartDate.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblLeaveStartDate.LocationFloat = new DevExpress.Utils.PointFloat(416.1961F, 299.2735F);
            this.lblLeaveStartDate.Name = "lblLeaveStartDate";
            this.lblLeaveStartDate.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblLeaveStartDate.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.lblLeaveStartDate.SizeF = new System.Drawing.SizeF(280F, 23F);
            this.lblLeaveStartDate.StylePriority.UseBorderColor = false;
            this.lblLeaveStartDate.StylePriority.UseBorders = false;
            this.lblLeaveStartDate.StylePriority.UseFont = false;
            this.lblLeaveStartDate.StylePriority.UseTextAlignment = false;
            this.lblLeaveStartDate.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // lblPaidLeave
            // 
            this.lblPaidLeave.BorderColor = System.Drawing.Color.LightGray;
            this.lblPaidLeave.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.lblPaidLeave.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblPaidLeave.LocationFloat = new DevExpress.Utils.PointFloat(568.5686F, 345.2735F);
            this.lblPaidLeave.Name = "lblPaidLeave";
            this.lblPaidLeave.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblPaidLeave.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.lblPaidLeave.SizeF = new System.Drawing.SizeF(111.7784F, 23F);
            this.lblPaidLeave.StylePriority.UseBorderColor = false;
            this.lblPaidLeave.StylePriority.UseBorders = false;
            this.lblPaidLeave.StylePriority.UseFont = false;
            this.lblPaidLeave.StylePriority.UseTextAlignment = false;
            this.lblPaidLeave.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.lblPaidLeave.TextFormatString = "{0:#0.00}";
            // 
            // xrLabel52
            // 
            this.xrLabel52.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(100)))), ((int)(((byte)(160)))));
            this.xrLabel52.CanGrow = false;
            this.xrLabel52.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel52.ForeColor = System.Drawing.Color.White;
            this.xrLabel52.LocationFloat = new DevExpress.Utils.PointFloat(9.999975F, 125.7292F);
            this.xrLabel52.Name = "xrLabel52";
            this.xrLabel52.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel52.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel52.SizeF = new System.Drawing.SizeF(795.5712F, 23.00005F);
            this.xrLabel52.StylePriority.UseBackColor = false;
            this.xrLabel52.StylePriority.UseFont = false;
            this.xrLabel52.StylePriority.UseForeColor = false;
            this.xrLabel52.StylePriority.UseTextAlignment = false;
            this.xrLabel52.Text = "اعداد تسوية الاجازة";
            this.xrLabel52.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            this.xrLabel52.WordWrap = false;
            // 
            // xrLabel50
            // 
            this.xrLabel50.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel50.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel50.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel50.LocationFloat = new DevExpress.Utils.PointFloat(277.6013F, 322.2735F);
            this.xrLabel50.Multiline = true;
            this.xrLabel50.Name = "xrLabel50";
            this.xrLabel50.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel50.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel50.SizeF = new System.Drawing.SizeF(138.595F, 23F);
            this.xrLabel50.StylePriority.UseBorderColor = false;
            this.xrLabel50.StylePriority.UseBorders = false;
            this.xrLabel50.StylePriority.UseFont = false;
            this.xrLabel50.StylePriority.UseTextAlignment = false;
            this.xrLabel50.Text = "تاريخ العودة\r\n";
            this.xrLabel50.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // lblExpReturnDate
            // 
            this.lblExpReturnDate.BorderColor = System.Drawing.Color.LightGray;
            this.lblExpReturnDate.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.lblExpReturnDate.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblExpReturnDate.LocationFloat = new DevExpress.Utils.PointFloat(9.820892F, 322.2735F);
            this.lblExpReturnDate.Name = "lblExpReturnDate";
            this.lblExpReturnDate.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblExpReturnDate.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.lblExpReturnDate.SizeF = new System.Drawing.SizeF(267.7804F, 23F);
            this.lblExpReturnDate.StylePriority.UseBorderColor = false;
            this.lblExpReturnDate.StylePriority.UseBorders = false;
            this.lblExpReturnDate.StylePriority.UseFont = false;
            this.lblExpReturnDate.StylePriority.UseTextAlignment = false;
            this.lblExpReturnDate.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel48
            // 
            this.xrLabel48.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel48.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel48.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel48.LocationFloat = new DevExpress.Utils.PointFloat(277.6013F, 276.2735F);
            this.xrLabel48.Multiline = true;
            this.xrLabel48.Name = "xrLabel48";
            this.xrLabel48.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel48.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel48.SizeF = new System.Drawing.SizeF(138.595F, 23F);
            this.xrLabel48.StylePriority.UseBorderColor = false;
            this.xrLabel48.StylePriority.UseBorders = false;
            this.xrLabel48.StylePriority.UseFont = false;
            this.xrLabel48.StylePriority.UseTextAlignment = false;
            this.xrLabel48.Text = "الراتب الأساسي\r\n";
            this.xrLabel48.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel49
            // 
            this.xrLabel49.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel49.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel49.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[BasicPay]")});
            this.xrLabel49.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel49.LocationFloat = new DevExpress.Utils.PointFloat(9.820862F, 276.2735F);
            this.xrLabel49.Name = "xrLabel49";
            this.xrLabel49.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel49.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel49.SizeF = new System.Drawing.SizeF(267.7804F, 23F);
            this.xrLabel49.StylePriority.UseBorderColor = false;
            this.xrLabel49.StylePriority.UseBorders = false;
            this.xrLabel49.StylePriority.UseFont = false;
            this.xrLabel49.StylePriority.UseTextAlignment = false;
            this.xrLabel49.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel49.TextFormatString = "{0:#0.000}";
            // 
            // xrLabel31
            // 
            this.xrLabel31.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel31.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel31.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel31.BorderWidth = 2F;
            this.xrLabel31.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel31.LocationFloat = new DevExpress.Utils.PointFloat(649.7063F, 627.5389F);
            this.xrLabel31.Multiline = true;
            this.xrLabel31.Name = "xrLabel31";
            this.xrLabel31.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 5, 100F);
            this.xrLabel31.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel31.SizeF = new System.Drawing.SizeF(156.2939F, 22.99994F);
            this.xrLabel31.StyleName = "EngMainDataStyle";
            this.xrLabel31.StylePriority.UseBorderColor = false;
            this.xrLabel31.StylePriority.UseBorderDashStyle = false;
            this.xrLabel31.StylePriority.UseBorders = false;
            this.xrLabel31.StylePriority.UseBorderWidth = false;
            this.xrLabel31.StylePriority.UseFont = false;
            this.xrLabel31.StylePriority.UsePadding = false;
            this.xrLabel31.StylePriority.UseTextAlignment = false;
            this.xrLabel31.Text = "صافي  المبلغ المدفوع\r\n";
            this.xrLabel31.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleCenter;
            // 
            // xrLabel4
            // 
            this.xrLabel4.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel4.BorderDashStyle = DevExpress.XtraPrinting.BorderDashStyle.Solid;
            this.xrLabel4.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel4.BorderWidth = 2F;
            this.xrLabel4.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CalcNetPay]")});
            this.xrLabel4.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel4.LocationFloat = new DevExpress.Utils.PointFloat(524.0532F, 627.5389F);
            this.xrLabel4.Name = "xrLabel4";
            this.xrLabel4.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 5, 100F);
            this.xrLabel4.SizeF = new System.Drawing.SizeF(82.71143F, 22.99994F);
            this.xrLabel4.StyleName = "ENGDataStyle";
            this.xrLabel4.StylePriority.UseBorderColor = false;
            this.xrLabel4.StylePriority.UseBorderDashStyle = false;
            this.xrLabel4.StylePriority.UseBorders = false;
            this.xrLabel4.StylePriority.UseBorderWidth = false;
            this.xrLabel4.StylePriority.UseFont = false;
            this.xrLabel4.StylePriority.UsePadding = false;
            this.xrLabel4.StylePriority.UseTextAlignment = false;
            xrSummary2.IgnoreNullValues = true;
            this.xrLabel4.Summary = xrSummary2;
            this.xrLabel4.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel4.TextFormatString = "{0:#0.000}";
            // 
            // xrLabel45
            // 
            this.xrLabel45.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel45.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel45.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel45.LocationFloat = new DevExpress.Utils.PointFloat(524.0532F, 677.539F);
            this.xrLabel45.Multiline = true;
            this.xrLabel45.Name = "xrLabel45";
            this.xrLabel45.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel45.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel45.SizeF = new System.Drawing.SizeF(130.6177F, 23F);
            this.xrLabel45.StylePriority.UseBorderColor = false;
            this.xrLabel45.StylePriority.UseBorders = false;
            this.xrLabel45.StylePriority.UseFont = false;
            this.xrLabel45.StylePriority.UseTextAlignment = false;
            this.xrLabel45.Text = "إجازة مدفوعة الأجر\r\n";
            this.xrLabel45.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // lblPaidLeave2
            // 
            this.lblPaidLeave2.BorderColor = System.Drawing.Color.LightGray;
            this.lblPaidLeave2.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.lblPaidLeave2.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblPaidLeave2.LocationFloat = new DevExpress.Utils.PointFloat(463.939F, 677.5389F);
            this.lblPaidLeave2.Name = "lblPaidLeave2";
            this.lblPaidLeave2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblPaidLeave2.SizeF = new System.Drawing.SizeF(60.11423F, 23F);
            this.lblPaidLeave2.StylePriority.UseBorderColor = false;
            this.lblPaidLeave2.StylePriority.UseBorders = false;
            this.lblPaidLeave2.StylePriority.UseFont = false;
            this.lblPaidLeave2.StylePriority.UseTextAlignment = false;
            this.lblPaidLeave2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.lblPaidLeave2.TextFormatString = "{0:#0.00}";
            // 
            // xrLabel41
            // 
            this.xrLabel41.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel41.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel41.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel41.LocationFloat = new DevExpress.Utils.PointFloat(715.0002F, 677.539F);
            this.xrLabel41.Multiline = true;
            this.xrLabel41.Name = "xrLabel41";
            this.xrLabel41.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel41.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel41.SizeF = new System.Drawing.SizeF(91F, 23F);
            this.xrLabel41.StylePriority.UseBorderColor = false;
            this.xrLabel41.StylePriority.UseBorders = false;
            this.xrLabel41.StylePriority.UseFont = false;
            this.xrLabel41.StylePriority.UseTextAlignment = false;
            this.xrLabel41.Text = "عدد أيام العمل\r\n";
            this.xrLabel41.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel42
            // 
            this.xrLabel42.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel42.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel42.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[WorkingDays]")});
            this.xrLabel42.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel42.LocationFloat = new DevExpress.Utils.PointFloat(654.671F, 677.5389F);
            this.xrLabel42.Name = "xrLabel42";
            this.xrLabel42.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel42.SizeF = new System.Drawing.SizeF(60F, 23F);
            this.xrLabel42.StylePriority.UseBorderColor = false;
            this.xrLabel42.StylePriority.UseBorders = false;
            this.xrLabel42.StylePriority.UseFont = false;
            this.xrLabel42.StylePriority.UseTextAlignment = false;
            this.xrLabel42.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel42.TextFormatString = "{0:#0.00}";
            // 
            // xrLabel43
            // 
            this.xrLabel43.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel43.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel43.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel43.LocationFloat = new DevExpress.Utils.PointFloat(390.9258F, 677.539F);
            this.xrLabel43.Multiline = true;
            this.xrLabel43.Name = "xrLabel43";
            this.xrLabel43.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel43.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel43.SizeF = new System.Drawing.SizeF(73.01318F, 23F);
            this.xrLabel43.StylePriority.UseBorderColor = false;
            this.xrLabel43.StylePriority.UseBorders = false;
            this.xrLabel43.StylePriority.UseFont = false;
            this.xrLabel43.StylePriority.UseTextAlignment = false;
            this.xrLabel43.Text = "إجازة عامة\r\n";
            this.xrLabel43.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel44
            // 
            this.xrLabel44.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel44.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel44.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[PHday]")});
            this.xrLabel44.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel44.LocationFloat = new DevExpress.Utils.PointFloat(341.4391F, 677.5389F);
            this.xrLabel44.Name = "xrLabel44";
            this.xrLabel44.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel44.SizeF = new System.Drawing.SizeF(49.48676F, 23F);
            this.xrLabel44.StylePriority.UseBorderColor = false;
            this.xrLabel44.StylePriority.UseBorders = false;
            this.xrLabel44.StylePriority.UseFont = false;
            this.xrLabel44.StylePriority.UseTextAlignment = false;
            this.xrLabel44.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel44.TextFormatString = "{0:#0.00}";
            // 
            // xrLabel39
            // 
            this.xrLabel39.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel39.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel39.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[PHday]")});
            this.xrLabel39.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel39.LocationFloat = new DevExpress.Utils.PointFloat(9.820849F, 345.2734F);
            this.xrLabel39.Name = "xrLabel39";
            this.xrLabel39.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel39.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel39.SizeF = new System.Drawing.SizeF(185.0103F, 23F);
            this.xrLabel39.StylePriority.UseBorderColor = false;
            this.xrLabel39.StylePriority.UseBorders = false;
            this.xrLabel39.StylePriority.UseFont = false;
            this.xrLabel39.StylePriority.UseTextAlignment = false;
            this.xrLabel39.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel39.TextFormatString = "{0:#0.00}";
            // 
            // xrLabel40
            // 
            this.xrLabel40.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel40.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel40.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel40.LocationFloat = new DevExpress.Utils.PointFloat(194.8311F, 345.2734F);
            this.xrLabel40.Multiline = true;
            this.xrLabel40.Name = "xrLabel40";
            this.xrLabel40.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel40.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel40.SizeF = new System.Drawing.SizeF(115.7277F, 23F);
            this.xrLabel40.StylePriority.UseBorderColor = false;
            this.xrLabel40.StylePriority.UseBorders = false;
            this.xrLabel40.StylePriority.UseFont = false;
            this.xrLabel40.StylePriority.UseTextAlignment = false;
            this.xrLabel40.Text = "الاجازة العامة\r\n";
            this.xrLabel40.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel37
            // 
            this.xrLabel37.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel37.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel37.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel37.LocationFloat = new DevExpress.Utils.PointFloat(420.3011F, 345.2735F);
            this.xrLabel37.Multiline = true;
            this.xrLabel37.Name = "xrLabel37";
            this.xrLabel37.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel37.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel37.SizeF = new System.Drawing.SizeF(148.2675F, 23F);
            this.xrLabel37.StylePriority.UseBorderColor = false;
            this.xrLabel37.StylePriority.UseBorders = false;
            this.xrLabel37.StylePriority.UseFont = false;
            this.xrLabel37.StylePriority.UseTextAlignment = false;
            this.xrLabel37.Text = "إجازة غير مدفوعة الاجر\r\n";
            this.xrLabel37.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel35
            // 
            this.xrLabel35.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel35.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel35.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel35.LocationFloat = new DevExpress.Utils.PointFloat(680.347F, 345.2735F);
            this.xrLabel35.Multiline = true;
            this.xrLabel35.Name = "xrLabel35";
            this.xrLabel35.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel35.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel35.SizeF = new System.Drawing.SizeF(125.6532F, 23F);
            this.xrLabel35.StylePriority.UseBorderColor = false;
            this.xrLabel35.StylePriority.UseBorders = false;
            this.xrLabel35.StylePriority.UseFont = false;
            this.xrLabel35.StylePriority.UseTextAlignment = false;
            this.xrLabel35.Text = "إجازة مدفوعة الاجر \r\n";
            this.xrLabel35.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // lblLeaveEndDate
            // 
            this.lblLeaveEndDate.BorderColor = System.Drawing.Color.LightGray;
            this.lblLeaveEndDate.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.lblLeaveEndDate.Font = new System.Drawing.Font("Tahoma", 9F);
            this.lblLeaveEndDate.LocationFloat = new DevExpress.Utils.PointFloat(416.1961F, 322.2735F);
            this.lblLeaveEndDate.Name = "lblLeaveEndDate";
            this.lblLeaveEndDate.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblLeaveEndDate.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.lblLeaveEndDate.SizeF = new System.Drawing.SizeF(280F, 23F);
            this.lblLeaveEndDate.StylePriority.UseBorderColor = false;
            this.lblLeaveEndDate.StylePriority.UseBorders = false;
            this.lblLeaveEndDate.StylePriority.UseFont = false;
            this.lblLeaveEndDate.StylePriority.UseTextAlignment = false;
            this.lblLeaveEndDate.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel32
            // 
            this.xrLabel32.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel32.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel32.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CivilID]")});
            this.xrLabel32.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel32.LocationFloat = new DevExpress.Utils.PointFloat(416.1963F, 276.2735F);
            this.xrLabel32.Name = "xrLabel32";
            this.xrLabel32.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel32.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel32.SizeF = new System.Drawing.SizeF(280F, 23F);
            this.xrLabel32.StylePriority.UseBorderColor = false;
            this.xrLabel32.StylePriority.UseBorders = false;
            this.xrLabel32.StylePriority.UseFont = false;
            this.xrLabel32.StylePriority.UseTextAlignment = false;
            this.xrLabel32.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel30
            // 
            this.xrLabel30.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel30.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel30.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[JoiningDate]")});
            this.xrLabel30.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel30.LocationFloat = new DevExpress.Utils.PointFloat(416.1961F, 253.2735F);
            this.xrLabel30.Name = "xrLabel30";
            this.xrLabel30.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel30.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel30.SizeF = new System.Drawing.SizeF(280F, 23F);
            this.xrLabel30.StylePriority.UseBorderColor = false;
            this.xrLabel30.StylePriority.UseBorders = false;
            this.xrLabel30.StylePriority.UseFont = false;
            this.xrLabel30.StylePriority.UseTextAlignment = false;
            this.xrLabel30.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel30.TextFormatString = "{0:dd/MM/yyyy}";
            // 
            // xrLabel29
            // 
            this.xrLabel29.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel29.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel29.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Department]")});
            this.xrLabel29.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel29.LocationFloat = new DevExpress.Utils.PointFloat(416.6252F, 230.2735F);
            this.xrLabel29.Name = "xrLabel29";
            this.xrLabel29.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel29.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel29.SizeF = new System.Drawing.SizeF(280F, 23F);
            this.xrLabel29.StylePriority.UseBorderColor = false;
            this.xrLabel29.StylePriority.UseBorders = false;
            this.xrLabel29.StylePriority.UseFont = false;
            this.xrLabel29.StylePriority.UseTextAlignment = false;
            this.xrLabel29.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel28
            // 
            this.xrLabel28.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel28.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel28.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Jobtitle]")});
            this.xrLabel28.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel28.LocationFloat = new DevExpress.Utils.PointFloat(416.1962F, 207.2735F);
            this.xrLabel28.Name = "xrLabel28";
            this.xrLabel28.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel28.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel28.SizeF = new System.Drawing.SizeF(280F, 23F);
            this.xrLabel28.StylePriority.UseBorderColor = false;
            this.xrLabel28.StylePriority.UseBorders = false;
            this.xrLabel28.StylePriority.UseFont = false;
            this.xrLabel28.StylePriority.UseTextAlignment = false;
            this.xrLabel28.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel27
            // 
            this.xrLabel27.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel27.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel27.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[IBAN]")});
            this.xrLabel27.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel27.LocationFloat = new DevExpress.Utils.PointFloat(10F, 253.2734F);
            this.xrLabel27.Name = "xrLabel27";
            this.xrLabel27.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel27.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel27.SizeF = new System.Drawing.SizeF(267.6013F, 23F);
            this.xrLabel27.StylePriority.UseBorderColor = false;
            this.xrLabel27.StylePriority.UseBorders = false;
            this.xrLabel27.StylePriority.UseFont = false;
            this.xrLabel27.StylePriority.UseTextAlignment = false;
            this.xrLabel27.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel26
            // 
            this.xrLabel26.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel26.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel26.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[BankBranch]")});
            this.xrLabel26.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel26.LocationFloat = new DevExpress.Utils.PointFloat(9.820862F, 230.2734F);
            this.xrLabel26.Name = "xrLabel26";
            this.xrLabel26.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel26.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel26.SizeF = new System.Drawing.SizeF(268.2093F, 23F);
            this.xrLabel26.StylePriority.UseBorderColor = false;
            this.xrLabel26.StylePriority.UseBorders = false;
            this.xrLabel26.StylePriority.UseFont = false;
            this.xrLabel26.StylePriority.UseTextAlignment = false;
            this.xrLabel26.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel25
            // 
            this.xrLabel25.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel25.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel25.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[AccountNum]")});
            this.xrLabel25.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel25.LocationFloat = new DevExpress.Utils.PointFloat(9.999962F, 207.2735F);
            this.xrLabel25.Name = "xrLabel25";
            this.xrLabel25.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel25.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel25.SizeF = new System.Drawing.SizeF(267.6013F, 23F);
            this.xrLabel25.StylePriority.UseBorderColor = false;
            this.xrLabel25.StylePriority.UseBorders = false;
            this.xrLabel25.StylePriority.UseFont = false;
            this.xrLabel25.StylePriority.UseTextAlignment = false;
            this.xrLabel25.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel24
            // 
            this.xrLabel24.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel24.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel24.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[BankAr]")});
            this.xrLabel24.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel24.LocationFloat = new DevExpress.Utils.PointFloat(9.820862F, 184.2735F);
            this.xrLabel24.Name = "xrLabel24";
            this.xrLabel24.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel24.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel24.SizeF = new System.Drawing.SizeF(267.7804F, 23F);
            this.xrLabel24.StylePriority.UseBorderColor = false;
            this.xrLabel24.StylePriority.UseBorders = false;
            this.xrLabel24.StylePriority.UseFont = false;
            this.xrLabel24.StylePriority.UseTextAlignment = false;
            this.xrLabel24.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel23
            // 
            this.xrLabel23.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel23.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel23.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Code]+ \' \' + [FullNameAr]")});
            this.xrLabel23.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel23.LocationFloat = new DevExpress.Utils.PointFloat(416.1963F, 184.2735F);
            this.xrLabel23.Name = "xrLabel23";
            this.xrLabel23.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel23.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel23.SizeF = new System.Drawing.SizeF(280F, 23F);
            this.xrLabel23.StylePriority.UseBorderColor = false;
            this.xrLabel23.StylePriority.UseBorders = false;
            this.xrLabel23.StylePriority.UseFont = false;
            this.xrLabel23.StylePriority.UseTextAlignment = false;
            this.xrLabel23.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel19
            // 
            this.xrLabel19.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel19.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel19.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[InPeriod]")});
            this.xrLabel19.Font = new System.Drawing.Font("Tahoma", 9F);
            this.xrLabel19.LocationFloat = new DevExpress.Utils.PointFloat(416.6251F, 161.2735F);
            this.xrLabel19.Name = "xrLabel19";
            this.xrLabel19.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel19.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel19.SizeF = new System.Drawing.SizeF(280F, 23F);
            this.xrLabel19.StylePriority.UseBorderColor = false;
            this.xrLabel19.StylePriority.UseBorders = false;
            this.xrLabel19.StylePriority.UseFont = false;
            this.xrLabel19.StylePriority.UseTextAlignment = false;
            this.xrLabel19.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel19.TextFormatString = "{0:MM/yyyy}";
            // 
            // xrLabel22
            // 
            this.xrLabel22.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel22.LocationFloat = new DevExpress.Utils.PointFloat(23.08345F, 908.25F);
            this.xrLabel22.Multiline = true;
            this.xrLabel22.Name = "xrLabel22";
            this.xrLabel22.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel22.SizeF = new System.Drawing.SizeF(192.5681F, 23F);
            this.xrLabel22.StylePriority.UseFont = false;
            this.xrLabel22.StylePriority.UseTextAlignment = false;
            this.xrLabel22.Text = "تمت الموافقة عليه بواسطة";
            this.xrLabel22.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabel6
            // 
            this.xrLabel6.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel6.LocationFloat = new DevExpress.Utils.PointFloat(254.0925F, 908.25F);
            this.xrLabel6.Multiline = true;
            this.xrLabel6.Name = "xrLabel6";
            this.xrLabel6.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel6.SizeF = new System.Drawing.SizeF(145.8333F, 23F);
            this.xrLabel6.StylePriority.UseFont = false;
            this.xrLabel6.StylePriority.UseTextAlignment = false;
            this.xrLabel6.Text = "كبير المحاسبين";
            this.xrLabel6.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabel21
            // 
            this.xrLabel21.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel21.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel21.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel21.LocationFloat = new DevExpress.Utils.PointFloat(696.6252F, 322.2735F);
            this.xrLabel21.Multiline = true;
            this.xrLabel21.Name = "xrLabel21";
            this.xrLabel21.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel21.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel21.SizeF = new System.Drawing.SizeF(109.375F, 23F);
            this.xrLabel21.StylePriority.UseBorderColor = false;
            this.xrLabel21.StylePriority.UseBorders = false;
            this.xrLabel21.StylePriority.UseFont = false;
            this.xrLabel21.StylePriority.UseTextAlignment = false;
            this.xrLabel21.Text = "نهاية الاجازة\r\n";
            this.xrLabel21.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel20
            // 
            this.xrLabel20.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel20.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel20.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel20.LocationFloat = new DevExpress.Utils.PointFloat(696.6252F, 299.2735F);
            this.xrLabel20.Multiline = true;
            this.xrLabel20.Name = "xrLabel20";
            this.xrLabel20.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel20.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel20.SizeF = new System.Drawing.SizeF(109.375F, 23F);
            this.xrLabel20.StylePriority.UseBorderColor = false;
            this.xrLabel20.StylePriority.UseBorders = false;
            this.xrLabel20.StylePriority.UseFont = false;
            this.xrLabel20.StylePriority.UseTextAlignment = false;
            this.xrLabel20.Text = "بداية الاجازة\r\n";
            this.xrLabel20.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel18
            // 
            this.xrLabel18.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel18.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel18.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel18.LocationFloat = new DevExpress.Utils.PointFloat(277.6013F, 253.2735F);
            this.xrLabel18.Multiline = true;
            this.xrLabel18.Name = "xrLabel18";
            this.xrLabel18.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel18.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel18.SizeF = new System.Drawing.SizeF(138.595F, 23F);
            this.xrLabel18.StylePriority.UseBorderColor = false;
            this.xrLabel18.StylePriority.UseBorders = false;
            this.xrLabel18.StylePriority.UseFont = false;
            this.xrLabel18.StylePriority.UseTextAlignment = false;
            this.xrLabel18.Text = "الايبان\r\n";
            this.xrLabel18.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel17
            // 
            this.xrLabel17.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel17.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel17.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel17.LocationFloat = new DevExpress.Utils.PointFloat(278.0302F, 230.2734F);
            this.xrLabel17.Multiline = true;
            this.xrLabel17.Name = "xrLabel17";
            this.xrLabel17.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel17.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel17.SizeF = new System.Drawing.SizeF(138.595F, 22.99998F);
            this.xrLabel17.StylePriority.UseBorderColor = false;
            this.xrLabel17.StylePriority.UseBorders = false;
            this.xrLabel17.StylePriority.UseFont = false;
            this.xrLabel17.StylePriority.UseTextAlignment = false;
            this.xrLabel17.Text = "الفرع\r\n";
            this.xrLabel17.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel16
            // 
            this.xrLabel16.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel16.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel16.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel16.LocationFloat = new DevExpress.Utils.PointFloat(277.6013F, 207.2735F);
            this.xrLabel16.Multiline = true;
            this.xrLabel16.Name = "xrLabel16";
            this.xrLabel16.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel16.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel16.SizeF = new System.Drawing.SizeF(138.595F, 23F);
            this.xrLabel16.StylePriority.UseBorderColor = false;
            this.xrLabel16.StylePriority.UseBorders = false;
            this.xrLabel16.StylePriority.UseFont = false;
            this.xrLabel16.StylePriority.UseTextAlignment = false;
            this.xrLabel16.Text = "رقم حساب البنك\r\n";
            this.xrLabel16.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel15
            // 
            this.xrLabel15.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel15.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel15.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel15.LocationFloat = new DevExpress.Utils.PointFloat(277.6013F, 184.2735F);
            this.xrLabel15.Multiline = true;
            this.xrLabel15.Name = "xrLabel15";
            this.xrLabel15.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel15.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel15.SizeF = new System.Drawing.SizeF(138.595F, 23F);
            this.xrLabel15.StylePriority.UseBorderColor = false;
            this.xrLabel15.StylePriority.UseBorders = false;
            this.xrLabel15.StylePriority.UseFont = false;
            this.xrLabel15.StylePriority.UseTextAlignment = false;
            this.xrLabel15.Text = "البنك\r\n";
            this.xrLabel15.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel14
            // 
            this.xrLabel14.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel14.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel14.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel14.LocationFloat = new DevExpress.Utils.PointFloat(696.1962F, 276.2735F);
            this.xrLabel14.Multiline = true;
            this.xrLabel14.Name = "xrLabel14";
            this.xrLabel14.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel14.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel14.SizeF = new System.Drawing.SizeF(109.804F, 23F);
            this.xrLabel14.StylePriority.UseBorderColor = false;
            this.xrLabel14.StylePriority.UseBorders = false;
            this.xrLabel14.StylePriority.UseFont = false;
            this.xrLabel14.StylePriority.UseTextAlignment = false;
            this.xrLabel14.Text = "الرقم المدني \r\n";
            this.xrLabel14.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel13
            // 
            this.xrLabel13.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel13.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel13.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel13.LocationFloat = new DevExpress.Utils.PointFloat(696.3754F, 253.2735F);
            this.xrLabel13.Multiline = true;
            this.xrLabel13.Name = "xrLabel13";
            this.xrLabel13.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel13.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel13.SizeF = new System.Drawing.SizeF(109.6248F, 22.99998F);
            this.xrLabel13.StylePriority.UseBorderColor = false;
            this.xrLabel13.StylePriority.UseBorders = false;
            this.xrLabel13.StylePriority.UseFont = false;
            this.xrLabel13.StylePriority.UseTextAlignment = false;
            this.xrLabel13.Text = "تاريخ التعيين\r\n";
            this.xrLabel13.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel12
            // 
            this.xrLabel12.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel12.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel12.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel12.LocationFloat = new DevExpress.Utils.PointFloat(696.625F, 230.2735F);
            this.xrLabel12.Multiline = true;
            this.xrLabel12.Name = "xrLabel12";
            this.xrLabel12.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel12.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel12.SizeF = new System.Drawing.SizeF(109.375F, 23F);
            this.xrLabel12.StylePriority.UseBorderColor = false;
            this.xrLabel12.StylePriority.UseBorders = false;
            this.xrLabel12.StylePriority.UseFont = false;
            this.xrLabel12.StylePriority.UseTextAlignment = false;
            this.xrLabel12.Text = "القسم\r\n";
            this.xrLabel12.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel11
            // 
            this.xrLabel11.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel11.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel11.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel11.LocationFloat = new DevExpress.Utils.PointFloat(696.1962F, 207.2735F);
            this.xrLabel11.Multiline = true;
            this.xrLabel11.Name = "xrLabel11";
            this.xrLabel11.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel11.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel11.SizeF = new System.Drawing.SizeF(109.804F, 23F);
            this.xrLabel11.StylePriority.UseBorderColor = false;
            this.xrLabel11.StylePriority.UseBorders = false;
            this.xrLabel11.StylePriority.UseFont = false;
            this.xrLabel11.StylePriority.UseTextAlignment = false;
            this.xrLabel11.Text = "الوظيفة\r\n";
            this.xrLabel11.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel9
            // 
            this.xrLabel9.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel9.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel9.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters].[PrmPrintDate]")});
            this.xrLabel9.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel9.LocationFloat = new DevExpress.Utils.PointFloat(9.999962F, 161.2735F);
            this.xrLabel9.Name = "xrLabel9";
            this.xrLabel9.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel9.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel9.SizeF = new System.Drawing.SizeF(267.6013F, 23F);
            this.xrLabel9.StylePriority.UseBorderColor = false;
            this.xrLabel9.StylePriority.UseBorders = false;
            this.xrLabel9.StylePriority.UseFont = false;
            this.xrLabel9.StylePriority.UseTextAlignment = false;
            this.xrLabel9.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            this.xrLabel9.TextFormatString = "{0:dd/MM/yyyy}";
            // 
            // xrLabel8
            // 
            this.xrLabel8.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel8.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel8.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel8.LocationFloat = new DevExpress.Utils.PointFloat(696.1962F, 184.2735F);
            this.xrLabel8.Multiline = true;
            this.xrLabel8.Name = "xrLabel8";
            this.xrLabel8.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel8.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel8.SizeF = new System.Drawing.SizeF(109.804F, 23F);
            this.xrLabel8.StylePriority.UseBorderColor = false;
            this.xrLabel8.StylePriority.UseBorders = false;
            this.xrLabel8.StylePriority.UseFont = false;
            this.xrLabel8.StylePriority.UseTextAlignment = false;
            this.xrLabel8.Text = "اسم الموظف\r\n";
            this.xrLabel8.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // xrLabel1
            // 
            this.xrLabel1.BorderColor = System.Drawing.Color.LightGray;
            this.xrLabel1.Borders = ((DevExpress.XtraPrinting.BorderSide)((((DevExpress.XtraPrinting.BorderSide.Left | DevExpress.XtraPrinting.BorderSide.Top) 
            | DevExpress.XtraPrinting.BorderSide.Right) 
            | DevExpress.XtraPrinting.BorderSide.Bottom)));
            this.xrLabel1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(696.6251F, 161.2735F);
            this.xrLabel1.Multiline = true;
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.RightToLeft = DevExpress.XtraReports.UI.RightToLeft.Yes;
            this.xrLabel1.SizeF = new System.Drawing.SizeF(109.375F, 23F);
            this.xrLabel1.StylePriority.UseBorderColor = false;
            this.xrLabel1.StylePriority.UseBorders = false;
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.Text = "الفترة\r\n";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // srptLeaveSettlementWorkingDaysSummaryEn
            // 
            this.srptLeaveSettlementWorkingDaysSummaryEn.LocationFloat = new DevExpress.Utils.PointFloat(9.820849F, 676.7058F);
            this.srptLeaveSettlementWorkingDaysSummaryEn.Name = "srptLeaveSettlementWorkingDaysSummaryEn";
            this.srptLeaveSettlementWorkingDaysSummaryEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmEmpCode", null, "PayslipOrgV2Query.Code"));
            this.srptLeaveSettlementWorkingDaysSummaryEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmMonth", this.PrmMonth));
            this.srptLeaveSettlementWorkingDaysSummaryEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmYear", this.PrmYear));
            this.srptLeaveSettlementWorkingDaysSummaryEn.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmPayrollRunType", this.PrmPayrollRunType));
            this.srptLeaveSettlementWorkingDaysSummaryEn.ReportSource = new HCMv3.Reports.Payroll.srptLeaveSettlementWorkingDaysSummaryEn();
            this.srptLeaveSettlementWorkingDaysSummaryEn.SizeF = new System.Drawing.SizeF(316.5713F, 23.83319F);
            this.srptLeaveSettlementWorkingDaysSummaryEn.Visible = false;
            // 
            // PrmMonth
            // 
            this.PrmMonth.Description = "Month";
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(1, "January"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(2, "February"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(3, "March"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(4, "April"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(5, "May"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(6, "June"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(7, "July"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(8, "August"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(9, "September"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(10, "October"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(11, "November"));
            staticListLookUpSettings1.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(12, "December"));
            this.PrmMonth.LookUpSettings = staticListLookUpSettings1;
            this.PrmMonth.Name = "PrmMonth";
            this.PrmMonth.Type = typeof(int);
            this.PrmMonth.ValueInfo = "1";
            // 
            // PrmYear
            // 
            this.PrmYear.Description = "Year";
            this.PrmYear.Name = "PrmYear";
            this.PrmYear.Type = typeof(int);
            this.PrmYear.ValueInfo = "2017";
            // 
            // PrmPayrollRunType
            // 
            this.PrmPayrollRunType.Description = "Run Type";
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(0, "Regular"));
            staticListLookUpSettings2.LookUpValues.Add(new DevExpress.XtraReports.Parameters.LookUpValue(1, "Off-Cycle"));
            this.PrmPayrollRunType.LookUpSettings = staticListLookUpSettings2;
            this.PrmPayrollRunType.Name = "PrmPayrollRunType";
            this.PrmPayrollRunType.Type = typeof(int);
            this.PrmPayrollRunType.ValueInfo = "0";
            // 
            // srptLeaveSettlementEarningAr
            // 
            this.srptLeaveSettlementEarningAr.LocationFloat = new DevExpress.Utils.PointFloat(9.999974F, 404.6224F);
            this.srptLeaveSettlementEarningAr.Name = "srptLeaveSettlementEarningAr";
            this.srptLeaveSettlementEarningAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmEmpCode", null, "PayslipOrgV2Query.Code"));
            this.srptLeaveSettlementEarningAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmMonth", this.PrmMonth));
            this.srptLeaveSettlementEarningAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmYear", this.PrmYear));
            this.srptLeaveSettlementEarningAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmPayrollRunType", this.PrmPayrollRunType));
            this.srptLeaveSettlementEarningAr.ReportSource = new HCMv3.Reports.Payroll.srptLeaveSettlementEarningAr();
            this.srptLeaveSettlementEarningAr.SizeF = new System.Drawing.SizeF(385.9451F, 209.4622F);
            // 
            // srptLeaveSettlementDeductionAr
            // 
            this.srptLeaveSettlementDeductionAr.LocationFloat = new DevExpress.Utils.PointFloat(407.055F, 404.6224F);
            this.srptLeaveSettlementDeductionAr.Name = "srptLeaveSettlementDeductionAr";
            this.srptLeaveSettlementDeductionAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmEmpCode", null, "PayslipOrgV2Query.Code"));
            this.srptLeaveSettlementDeductionAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmMonth", this.PrmMonth));
            this.srptLeaveSettlementDeductionAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmYear", this.PrmYear));
            this.srptLeaveSettlementDeductionAr.ParameterBindings.Add(new DevExpress.XtraReports.UI.ParameterBinding("PrmPayrollRunType", this.PrmPayrollRunType));
            this.srptLeaveSettlementDeductionAr.ReportSource = new HCMv3.Reports.Payroll.srptLeaveSettlementDeductionAr();
            this.srptLeaveSettlementDeductionAr.SizeF = new System.Drawing.SizeF(398.945F, 209.4623F);
            // 
            // xrPictureBox1
            // 
            this.xrPictureBox1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Image", "[CompanyLogo]")});
            this.xrPictureBox1.LocationFloat = new DevExpress.Utils.PointFloat(278.04F, 12F);
            this.xrPictureBox1.Name = "xrPictureBox1";
            this.xrPictureBox1.SizeF = new System.Drawing.SizeF(259.93F, 94.87F);
            this.xrPictureBox1.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            // 
            // PrmEmpCode
            // 
            this.PrmEmpCode.Description = "Employee Code";
            this.PrmEmpCode.Name = "PrmEmpCode";
            // 
            // xrLabel5
            // 
            this.xrLabel5.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel5.LocationFloat = new DevExpress.Utils.PointFloat(12.08345F, 10F);
            this.xrLabel5.Name = "xrLabel5";
            this.xrLabel5.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel5.SizeF = new System.Drawing.SizeF(46.79168F, 23F);
            this.xrLabel5.StylePriority.UseFont = false;
            this.xrLabel5.Text = "User :";
            // 
            // LoggedUserNme
            // 
            this.LoggedUserNme.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LoggedUserNme.LocationFloat = new DevExpress.Utils.PointFloat(77.62513F, 10F);
            this.LoggedUserNme.Name = "LoggedUserNme";
            this.LoggedUserNme.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.LoggedUserNme.SizeF = new System.Drawing.SizeF(138.7661F, 23.00001F);
            this.LoggedUserNme.StylePriority.UseFont = false;
            // 
            // xrLabel3
            // 
            this.xrLabel3.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "LocalDateTimeToday()")});
            this.xrLabel3.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(472.726F, 10F);
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.SizeF = new System.Drawing.SizeF(322.0595F, 23F);
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.StylePriority.UseTextAlignment = false;
            this.xrLabel3.Text = "xrLabel3";
            this.xrLabel3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.xrLabel3.TextFormatString = "{0:dd/MM/yyyy hh:mm tt}";
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
            this.BottomMargin.HeightF = 0F;
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
            // ENGDataStyle
            // 
            this.ENGDataStyle.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ENGDataStyle.Name = "ENGDataStyle";
            this.ENGDataStyle.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.ENGDataStyle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // ReportHeader
            // 
            this.ReportHeader.HeightF = 0F;
            this.ReportHeader.KeepTogether = true;
            this.ReportHeader.Name = "ReportHeader";
            // 
            // CalcDateTime
            // 
            this.CalcDateTime.DataMember = "PayslipOrgV2Query";
            this.CalcDateTime.Expression = "LocalDateTimeNow()";
            this.CalcDateTime.Name = "CalcDateTime";
            // 
            // CalcNetPay
            // 
            this.CalcNetPay.DataMember = "PayslipOrgV2Query";
            this.CalcNetPay.Expression = "Round([NetPay],3)";
            this.CalcNetPay.Name = "CalcNetPay";
            // 
            // PageFooter
            // 
            this.PageFooter.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel5,
            this.LoggedUserNme,
            this.xrLabel3});
            this.PageFooter.HeightF = 44.79167F;
            this.PageFooter.Name = "PageFooter";
            // 
            // PrmSendMail
            // 
            this.PrmSendMail.Description = "Send Email";
            this.PrmSendMail.Name = "PrmSendMail";
            this.PrmSendMail.Type = typeof(bool);
            this.PrmSendMail.ValueInfo = "False";
            this.PrmSendMail.Visible = false;
            // 
            // PrmEncashment
            // 
            this.PrmEncashment.Description = "Is Encashment Print";
            this.PrmEncashment.Name = "PrmEncashment";
            this.PrmEncashment.Type = typeof(bool);
            this.PrmEncashment.ValueInfo = "False";
            this.PrmEncashment.Visible = false;
            // 
            // PrmPayrollArea
            // 
            this.PrmPayrollArea.Description = "Payroll Area";
            dynamicListLookUpSettings1.DataAdapter = null;
            dynamicListLookUpSettings1.DataMember = "PayrollArea";
            dynamicListLookUpSettings1.DataSource = this.PayslipDS;
            dynamicListLookUpSettings1.DisplayMember = "Name";
            dynamicListLookUpSettings1.ValueMember = "Name";
            this.PrmPayrollArea.LookUpSettings = dynamicListLookUpSettings1;
            this.PrmPayrollArea.Name = "PrmPayrollArea";
            // 
            // PrmPrintDate
            // 
            this.PrmPrintDate.Description = "Print Date";
            this.PrmPrintDate.Name = "PrmPrintDate";
            this.PrmPrintDate.Type = typeof(System.DateTime);
            // 
            // xrLabel2
            // 
            this.xrLabel2.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(443.9451F, 908.25F);
            this.xrLabel2.Multiline = true;
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(145.8333F, 23F);
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.Text = "مدير شؤون الموظفين";
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // xrLabel7
            // 
            this.xrLabel7.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel7.LocationFloat = new DevExpress.Utils.PointFloat(629.7501F, 908.25F);
            this.xrLabel7.Multiline = true;
            this.xrLabel7.Name = "xrLabel7";
            this.xrLabel7.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel7.SizeF = new System.Drawing.SizeF(145.8333F, 23F);
            this.xrLabel7.StylePriority.UseFont = false;
            this.xrLabel7.StylePriority.UseTextAlignment = false;
            this.xrLabel7.Text = "قسم شؤون الموظفين";
            this.xrLabel7.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopCenter;
            // 
            // rptLeaveSettlementAr
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin,
            this.ReportHeader,
            this.PageFooter});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.CalcDateTime,
            this.CalcNetPay});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.PayslipDS});
            this.DataMember = "PayslipOrgV2Query";
            this.DataSource = this.PayslipDS;
            this.Margins = new System.Drawing.Printing.Margins(5, 6, 0, 0);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.PrmPayrollArea,
            this.PrmEmpCode,
            this.PrmMonth,
            this.PrmYear,
            this.PrmPayrollRunType,
            this.PrmSendMail,
            this.PrmEncashment,
            this.PrmPrintDate});
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.EngMainDataStyle,
            this.ENGDataStyle});
            this.Version = "23.2";
            this.BeforePrint += new DevExpress.XtraReports.UI.BeforePrintEventHandler(this.rptLeaveSettlementAr_BeforePrint);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.XRControlStyle EngMainDataStyle;
        private DevExpress.XtraReports.UI.XRControlStyle ENGDataStyle;
        private DevExpress.XtraReports.Parameters.Parameter PrmMonth;
        private DevExpress.XtraReports.Parameters.Parameter PrmYear;
        private DevExpress.DataAccess.Sql.SqlDataSource PayslipDS;
        private DevExpress.XtraReports.UI.XRLabel xrLabel3;
        private DevExpress.XtraReports.UI.ReportHeaderBand ReportHeader;
        private DevExpress.XtraReports.UI.CalculatedField CalcDateTime;
        private DevExpress.XtraReports.UI.XRLabel LoggedUserNme;
        private DevExpress.XtraReports.UI.XRLabel xrLabel5;
        private DevExpress.XtraReports.UI.CalculatedField CalcNetPay;
        private DevExpress.XtraReports.UI.PageFooterBand PageFooter;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox1;
        private DevExpress.XtraReports.Parameters.Parameter PrmPayrollRunType;
        private DevExpress.XtraReports.Parameters.Parameter PrmSendMail;
        private DevExpress.XtraReports.Parameters.Parameter PrmEmpCode;
        private DevExpress.XtraReports.Parameters.Parameter PrmEncashment;
        private DevExpress.XtraReports.Parameters.Parameter PrmPayrollArea;
        private DevExpress.XtraReports.Parameters.Parameter PrmPrintDate;
        private DevExpress.XtraReports.UI.XRLabel xrLabel52;
        private DevExpress.XtraReports.UI.XRLabel xrLabel51;
        private DevExpress.XtraReports.UI.XRLabel xrLabel47;
        private DevExpress.XtraReports.UI.XRLabel lblUnpaidLeave;
        private DevExpress.XtraReports.UI.XRLabel xrLabel46;
        private DevExpress.XtraReports.UI.XRLabel lblLeaveStartDate;
        private DevExpress.XtraReports.UI.XRLabel lblPaidLeave;
        private DevExpress.XtraReports.UI.XRLabel xrLabel50;
        private DevExpress.XtraReports.UI.XRLabel lblExpReturnDate;
        private DevExpress.XtraReports.UI.XRLabel xrLabel48;
        private DevExpress.XtraReports.UI.XRLabel xrLabel49;
        private DevExpress.XtraReports.UI.XRLabel xrLabel31;
        private DevExpress.XtraReports.UI.XRLabel xrLabel4;
        private DevExpress.XtraReports.UI.XRLabel xrLabel45;
        private DevExpress.XtraReports.UI.XRLabel lblPaidLeave2;
        private DevExpress.XtraReports.UI.XRLabel xrLabel41;
        private DevExpress.XtraReports.UI.XRLabel xrLabel42;
        private DevExpress.XtraReports.UI.XRLabel xrLabel43;
        private DevExpress.XtraReports.UI.XRLabel xrLabel44;
        private DevExpress.XtraReports.UI.XRLabel xrLabel39;
        private DevExpress.XtraReports.UI.XRLabel xrLabel40;
        private DevExpress.XtraReports.UI.XRLabel xrLabel37;
        private DevExpress.XtraReports.UI.XRLabel xrLabel35;
        private DevExpress.XtraReports.UI.XRLabel lblLeaveEndDate;
        private DevExpress.XtraReports.UI.XRLabel xrLabel32;
        private DevExpress.XtraReports.UI.XRLabel xrLabel30;
        private DevExpress.XtraReports.UI.XRLabel xrLabel29;
        private DevExpress.XtraReports.UI.XRLabel xrLabel28;
        private DevExpress.XtraReports.UI.XRLabel xrLabel27;
        private DevExpress.XtraReports.UI.XRLabel xrLabel26;
        private DevExpress.XtraReports.UI.XRLabel xrLabel25;
        private DevExpress.XtraReports.UI.XRLabel xrLabel24;
        private DevExpress.XtraReports.UI.XRLabel xrLabel23;
        private DevExpress.XtraReports.UI.XRLabel xrLabel19;
        private DevExpress.XtraReports.UI.XRLabel xrLabel22;
        private DevExpress.XtraReports.UI.XRLabel xrLabel6;
        private DevExpress.XtraReports.UI.XRLabel xrLabel21;
        private DevExpress.XtraReports.UI.XRLabel xrLabel20;
        private DevExpress.XtraReports.UI.XRLabel xrLabel18;
        private DevExpress.XtraReports.UI.XRLabel xrLabel17;
        private DevExpress.XtraReports.UI.XRLabel xrLabel16;
        private DevExpress.XtraReports.UI.XRLabel xrLabel15;
        private DevExpress.XtraReports.UI.XRLabel xrLabel14;
        private DevExpress.XtraReports.UI.XRLabel xrLabel13;
        private DevExpress.XtraReports.UI.XRLabel xrLabel12;
        private DevExpress.XtraReports.UI.XRLabel xrLabel11;
        private DevExpress.XtraReports.UI.XRLabel xrLabel9;
        private DevExpress.XtraReports.UI.XRLabel xrLabel8;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        private DevExpress.XtraReports.UI.XRSubreport srptLeaveSettlementWorkingDaysSummaryEn;
        private DevExpress.XtraReports.UI.XRSubreport srptLeaveSettlementEarningAr;
        private DevExpress.XtraReports.UI.XRSubreport srptLeaveSettlementDeductionAr;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.XRLabel xrLabel7;
    }
}
