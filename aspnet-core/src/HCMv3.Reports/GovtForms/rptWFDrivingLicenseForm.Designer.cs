namespace HCMv3.Reports.GovtForms
{
    partial class rptWFDrivingLicenseForm
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
            DevExpress.DataAccess.Sql.SelectQuery selectQuery1 = new DevExpress.DataAccess.Sql.SelectQuery();
            DevExpress.DataAccess.Sql.Column column1 = new DevExpress.DataAccess.Sql.Column();
            DevExpress.DataAccess.Sql.ColumnExpression columnExpression1 = new DevExpress.DataAccess.Sql.ColumnExpression();
            DevExpress.DataAccess.Sql.Table table1 = new DevExpress.DataAccess.Sql.Table();
            DevExpress.DataAccess.Sql.Column column2 = new DevExpress.DataAccess.Sql.Column();
            DevExpress.DataAccess.Sql.ColumnExpression columnExpression2 = new DevExpress.DataAccess.Sql.ColumnExpression();
            DevExpress.XtraReports.Parameters.DynamicListLookUpSettings dynamicListLookUpSettings1 = new DevExpress.XtraReports.Parameters.DynamicListLookUpSettings();
            this.dsGovtForm = new DevExpress.DataAccess.Sql.SqlDataSource(this.components);
            this.Detail = new DevExpress.XtraReports.UI.DetailBand();
            this.xrLabel3 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel2 = new DevExpress.XtraReports.UI.XRLabel();
            this.xrLabel1 = new DevExpress.XtraReports.UI.XRLabel();
            this.lblResidencyNumber = new DevExpress.XtraReports.UI.XRLabel();
            this.lblIssueDateDay = new DevExpress.XtraReports.UI.XRLabel();
            this.lblExpiryDateDay = new DevExpress.XtraReports.UI.XRLabel();
            this.lblBirthDateDay = new DevExpress.XtraReports.UI.XRLabel();
            this.lblParamDateYear = new DevExpress.XtraReports.UI.XRLabel();
            this.lblLicenceType = new DevExpress.XtraReports.UI.XRLabel();
            this.lblLicenceNumber = new DevExpress.XtraReports.UI.XRLabel();
            this.lblWorkAddress = new DevExpress.XtraReports.UI.XRLabel();
            this.lblHomeAddress = new DevExpress.XtraReports.UI.XRLabel();
            this.lblJobtitle = new DevExpress.XtraReports.UI.XRLabel();
            this.lblBloodGroup = new DevExpress.XtraReports.UI.XRLabel();
            this.lblGender = new DevExpress.XtraReports.UI.XRLabel();
            this.lblNationality = new DevExpress.XtraReports.UI.XRLabel();
            this.lblNameEn = new DevExpress.XtraReports.UI.XRLabel();
            this.lblNameAr = new DevExpress.XtraReports.UI.XRLabel();
            this.lblFees = new DevExpress.XtraReports.UI.XRLabel();
            this.xrPictureBox2 = new DevExpress.XtraReports.UI.XRPictureBox();
            this.Date = new DevExpress.XtraReports.Parameters.Parameter();
            this.Fees = new DevExpress.XtraReports.Parameters.Parameter();
            this.TopMargin = new DevExpress.XtraReports.UI.TopMarginBand();
            this.BottomMargin = new DevExpress.XtraReports.UI.BottomMarginBand();
            this.empcode = new DevExpress.XtraReports.Parameters.Parameter();
            this.CalcGender = new DevExpress.XtraReports.UI.CalculatedField();
            this.Document_Type = new DevExpress.XtraReports.Parameters.Parameter();
            this.CalcParamDateAr = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcParamMonthNum = new DevExpress.XtraReports.UI.CalculatedField();
            this.XrStyleWhite = new DevExpress.XtraReports.UI.XRControlStyle();
            this.CalcBirthMonthNum = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcBirthDateAr = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcExpiryMonthNumber = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcExpiryMonthAr = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcIssueDateMonthNum = new DevExpress.XtraReports.UI.CalculatedField();
            this.CalcIssueMonthAr = new DevExpress.XtraReports.UI.CalculatedField();
            this.XrStyleBlack = new DevExpress.XtraReports.UI.XRControlStyle();
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            // 
            // dsGovtForm
            // 
            this.dsGovtForm.ConnectionName = HCMv3.Reports.HcmReportBase.DesignTimeConnectionName;
            this.dsGovtForm.Name = "dsGovtForm";
            customSqlQuery1.Name = "Query";
            queryParameter1.Name = "DocType";
            queryParameter1.Type = typeof(DevExpress.DataAccess.Expression);
            queryParameter1.Value = new DevExpress.DataAccess.Expression("[Parameters.Document_Type]", typeof(int));
            customSqlQuery1.Parameters.Add(queryParameter1);
            customSqlQuery1.Sql = DrivingLicenseFormSql.Query;
            columnExpression1.ColumnName = "OID";
            table1.Name = "DocumentType";
            columnExpression1.Table = table1;
            column1.Expression = columnExpression1;
            columnExpression2.ColumnName = "NameAr";
            columnExpression2.Table = table1;
            column2.Expression = columnExpression2;
            selectQuery1.Columns.Add(column1);
            selectQuery1.Columns.Add(column2);
            selectQuery1.Name = "DocumentType";
            selectQuery1.Tables.Add(table1);
            this.dsGovtForm.Queries.AddRange(new DevExpress.DataAccess.Sql.SqlQuery[] {
            customSqlQuery1,
            selectQuery1});
            // 
            // Detail
            // 
            this.Detail.Controls.AddRange(new DevExpress.XtraReports.UI.XRControl[] {
            this.xrLabel3,
            this.xrLabel2,
            this.xrLabel1,
            this.lblResidencyNumber,
            this.lblIssueDateDay,
            this.lblExpiryDateDay,
            this.lblBirthDateDay,
            this.lblParamDateYear,
            this.lblLicenceType,
            this.lblLicenceNumber,
            this.lblWorkAddress,
            this.lblHomeAddress,
            this.lblJobtitle,
            this.lblBloodGroup,
            this.lblGender,
            this.lblNationality,
            this.lblNameEn,
            this.lblNameAr,
            this.lblFees,
            this.xrPictureBox2});
            this.Detail.HeightF = 1164F;
            this.Detail.Name = "Detail";
            this.Detail.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.Detail.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // xrLabel3
            // 
            this.xrLabel3.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[MOIFileNumber]")});
            this.xrLabel3.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.xrLabel3.LocationFloat = new DevExpress.Utils.PointFloat(32.13489F, 696.2157F);
            this.xrLabel3.Name = "xrLabel3";
            this.xrLabel3.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel3.SizeF = new System.Drawing.SizeF(122.694F, 35.99768F);
            this.xrLabel3.StylePriority.UseFont = false;
            this.xrLabel3.StylePriority.UseTextAlignment = false;
            this.xrLabel3.Text = "xrLabel3";
            this.xrLabel3.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel2
            // 
            this.xrLabel2.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Licence Type]),\'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Licence Type]")});
            this.xrLabel2.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel2.LocationFloat = new DevExpress.Utils.PointFloat(247.6782F, 135.6667F);
            this.xrLabel2.Name = "xrLabel2";
            this.xrLabel2.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel2.SizeF = new System.Drawing.SizeF(315.496F, 33.7565F);
            this.xrLabel2.StylePriority.UseFont = false;
            this.xrLabel2.StylePriority.UseTextAlignment = false;
            this.xrLabel2.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrLabel1
            // 
            this.xrLabel1.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Nationality]),\'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Nationality]")});
            this.xrLabel1.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.xrLabel1.LocationFloat = new DevExpress.Utils.PointFloat(32.13489F, 662.4592F);
            this.xrLabel1.Name = "xrLabel1";
            this.xrLabel1.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.xrLabel1.SizeF = new System.Drawing.SizeF(122.694F, 33.75647F);
            this.xrLabel1.StylePriority.UseFont = false;
            this.xrLabel1.StylePriority.UseTextAlignment = false;
            this.xrLabel1.Text = "lblNationality";
            this.xrLabel1.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblResidencyNumber
            // 
            this.lblResidencyNumber.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ResidencyNum]")});
            this.lblResidencyNumber.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblResidencyNumber.LocationFloat = new DevExpress.Utils.PointFloat(483.905F, 290.1667F);
            this.lblResidencyNumber.Name = "lblResidencyNumber";
            this.lblResidencyNumber.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblResidencyNumber.SizeF = new System.Drawing.SizeF(202.9975F, 39.04117F);
            this.lblResidencyNumber.StylePriority.UseFont = false;
            this.lblResidencyNumber.StylePriority.UseTextAlignment = false;
            this.lblResidencyNumber.Text = "    ";
            this.lblResidencyNumber.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblIssueDateDay
            // 
            this.lblIssueDateDay.BorderColor = System.Drawing.Color.DarkGray;
            this.lblIssueDateDay.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblIssueDateDay.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([IssueDate]),\'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[IssueDate]")});
            this.lblIssueDateDay.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblIssueDateDay.ForeColor = System.Drawing.Color.Black;
            this.lblIssueDateDay.LocationFloat = new DevExpress.Utils.PointFloat(494.1647F, 698.0411F);
            this.lblIssueDateDay.Name = "lblIssueDateDay";
            this.lblIssueDateDay.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblIssueDateDay.SizeF = new System.Drawing.SizeF(194.3039F, 34.17236F);
            this.lblIssueDateDay.StyleName = "XrStyleWhite";
            this.lblIssueDateDay.StylePriority.UseBorderColor = false;
            this.lblIssueDateDay.StylePriority.UseBorders = false;
            this.lblIssueDateDay.StylePriority.UseFont = false;
            this.lblIssueDateDay.StylePriority.UseForeColor = false;
            this.lblIssueDateDay.StylePriority.UsePadding = false;
            this.lblIssueDateDay.StylePriority.UseTextAlignment = false;
            this.lblIssueDateDay.Text = " ";
            this.lblIssueDateDay.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lblIssueDateDay.TextFormatString = "{0:yyyy/MM/dd}";
            // 
            // lblExpiryDateDay
            // 
            this.lblExpiryDateDay.BorderColor = System.Drawing.Color.DarkGray;
            this.lblExpiryDateDay.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblExpiryDateDay.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([ExpiryDate]),\'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[ExpiryDate]")});
            this.lblExpiryDateDay.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblExpiryDateDay.ForeColor = System.Drawing.Color.Black;
            this.lblExpiryDateDay.LocationFloat = new DevExpress.Utils.PointFloat(233.5231F, 698.9023F);
            this.lblExpiryDateDay.Name = "lblExpiryDateDay";
            this.lblExpiryDateDay.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblExpiryDateDay.SizeF = new System.Drawing.SizeF(165.126F, 33.31122F);
            this.lblExpiryDateDay.StyleName = "XrStyleWhite";
            this.lblExpiryDateDay.StylePriority.UseBorderColor = false;
            this.lblExpiryDateDay.StylePriority.UseBorders = false;
            this.lblExpiryDateDay.StylePriority.UseFont = false;
            this.lblExpiryDateDay.StylePriority.UseForeColor = false;
            this.lblExpiryDateDay.StylePriority.UsePadding = false;
            this.lblExpiryDateDay.StylePriority.UseTextAlignment = false;
            this.lblExpiryDateDay.Text = " ";
            this.lblExpiryDateDay.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lblExpiryDateDay.TextFormatString = "{0:yyyy/MM/dd}";
            // 
            // lblBirthDateDay
            // 
            this.lblBirthDateDay.BorderColor = System.Drawing.Color.DarkGray;
            this.lblBirthDateDay.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblBirthDateDay.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([BirthDate]), \'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[BirthDate]")});
            this.lblBirthDateDay.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBirthDateDay.ForeColor = System.Drawing.Color.Black;
            this.lblBirthDateDay.LocationFloat = new DevExpress.Utils.PointFloat(32.13489F, 433F);
            this.lblBirthDateDay.Name = "lblBirthDateDay";
            this.lblBirthDateDay.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblBirthDateDay.SizeF = new System.Drawing.SizeF(287.8977F, 33.70096F);
            this.lblBirthDateDay.StyleName = "XrStyleWhite";
            this.lblBirthDateDay.StylePriority.UseBorderColor = false;
            this.lblBirthDateDay.StylePriority.UseBorders = false;
            this.lblBirthDateDay.StylePriority.UseFont = false;
            this.lblBirthDateDay.StylePriority.UseForeColor = false;
            this.lblBirthDateDay.StylePriority.UsePadding = false;
            this.lblBirthDateDay.StylePriority.UseTextAlignment = false;
            this.lblBirthDateDay.Text = " ";
            this.lblBirthDateDay.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lblBirthDateDay.TextFormatString = "{0:yyyy/MM/dd}";
            // 
            // lblParamDateYear
            // 
            this.lblParamDateYear.BorderColor = System.Drawing.Color.DarkGray;
            this.lblParamDateYear.Borders = DevExpress.XtraPrinting.BorderSide.None;
            this.lblParamDateYear.CanGrow = false;
            this.lblParamDateYear.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters.Date]")});
            this.lblParamDateYear.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblParamDateYear.ForeColor = System.Drawing.Color.Black;
            this.lblParamDateYear.LocationFloat = new DevExpress.Utils.PointFloat(247.6782F, 172.625F);
            this.lblParamDateYear.Name = "lblParamDateYear";
            this.lblParamDateYear.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblParamDateYear.SizeF = new System.Drawing.SizeF(315.496F, 36.93286F);
            this.lblParamDateYear.StylePriority.UseBorderColor = false;
            this.lblParamDateYear.StylePriority.UseBorders = false;
            this.lblParamDateYear.StylePriority.UseFont = false;
            this.lblParamDateYear.StylePriority.UseForeColor = false;
            this.lblParamDateYear.StylePriority.UsePadding = false;
            this.lblParamDateYear.StylePriority.UseTextAlignment = false;
            this.lblParamDateYear.Text = " ";
            this.lblParamDateYear.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            this.lblParamDateYear.TextFormatString = "{0:yyyy/MM/dd}";
            // 
            // lblLicenceType
            // 
            this.lblLicenceType.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Licence Type]),\'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Licence Type]")});
            this.lblLicenceType.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLicenceType.LocationFloat = new DevExpress.Utils.PointFloat(233.5231F, 662.4592F);
            this.lblLicenceType.Name = "lblLicenceType";
            this.lblLicenceType.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblLicenceType.SizeF = new System.Drawing.SizeF(143.6549F, 33.75647F);
            this.lblLicenceType.StylePriority.UseFont = false;
            this.lblLicenceType.StylePriority.UseTextAlignment = false;
            this.lblLicenceType.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblLicenceNumber
            // 
            this.lblLicenceNumber.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CivilID]")});
            this.lblLicenceNumber.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLicenceNumber.LocationFloat = new DevExpress.Utils.PointFloat(471.8937F, 662.4592F);
            this.lblLicenceNumber.Name = "lblLicenceNumber";
            this.lblLicenceNumber.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblLicenceNumber.SizeF = new System.Drawing.SizeF(216.5749F, 33.75647F);
            this.lblLicenceNumber.StylePriority.UseFont = false;
            this.lblLicenceNumber.StylePriority.UseTextAlignment = false;
            this.lblLicenceNumber.Text = "    ";
            this.lblLicenceNumber.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblWorkAddress
            // 
            this.lblWorkAddress.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Org Address]),\'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Org Address]")});
            this.lblWorkAddress.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWorkAddress.LocationFloat = new DevExpress.Utils.PointFloat(191.2751F, 535.6639F);
            this.lblWorkAddress.Name = "lblWorkAddress";
            this.lblWorkAddress.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblWorkAddress.SizeF = new System.Drawing.SizeF(494.2573F, 67.00372F);
            this.lblWorkAddress.StylePriority.UseFont = false;
            this.lblWorkAddress.StylePriority.UseTextAlignment = false;
            this.lblWorkAddress.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            // 
            // lblHomeAddress
            // 
            this.lblHomeAddress.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([CurrentAddress]),\'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CurrentAddress]")});
            this.lblHomeAddress.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblHomeAddress.LocationFloat = new DevExpress.Utils.PointFloat(32.13489F, 469.8988F);
            this.lblHomeAddress.Name = "lblHomeAddress";
            this.lblHomeAddress.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblHomeAddress.SizeF = new System.Drawing.SizeF(653.3975F, 62.79117F);
            this.lblHomeAddress.StylePriority.UseFont = false;
            this.lblHomeAddress.StylePriority.UseTextAlignment = false;
            this.lblHomeAddress.Text = "lblHomeAddress";
            this.lblHomeAddress.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopRight;
            // 
            // lblJobtitle
            // 
            this.lblJobtitle.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Jobtitle]), \'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Jobtitle]")});
            this.lblJobtitle.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblJobtitle.LocationFloat = new DevExpress.Utils.PointFloat(434.4633F, 433F);
            this.lblJobtitle.Name = "lblJobtitle";
            this.lblJobtitle.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblJobtitle.SizeF = new System.Drawing.SizeF(251.0692F, 33.70096F);
            this.lblJobtitle.StylePriority.UseFont = false;
            this.lblJobtitle.StylePriority.UseTextAlignment = false;
            this.lblJobtitle.Text = "lblJobtitle";
            this.lblJobtitle.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblBloodGroup
            // 
            this.lblBloodGroup.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([BloodGroup]),\'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[BloodGroup]")});
            this.lblBloodGroup.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBloodGroup.LocationFloat = new DevExpress.Utils.PointFloat(32.13489F, 399F);
            this.lblBloodGroup.Name = "lblBloodGroup";
            this.lblBloodGroup.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblBloodGroup.SizeF = new System.Drawing.SizeF(86.03064F, 31.61758F);
            this.lblBloodGroup.StylePriority.UseFont = false;
            this.lblBloodGroup.StylePriority.UseTextAlignment = false;
            this.lblBloodGroup.Text = "lblBloodGroup";
            this.lblBloodGroup.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblGender
            // 
            this.lblGender.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([CalcGender]), \'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[CalcGender]")});
            this.lblGender.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblGender.ForeColor = System.Drawing.Color.Black;
            this.lblGender.LocationFloat = new DevExpress.Utils.PointFloat(201.3777F, 399.4314F);
            this.lblGender.Name = "lblGender";
            this.lblGender.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblGender.SizeF = new System.Drawing.SizeF(118.6549F, 32.10782F);
            this.lblGender.StylePriority.UseFont = false;
            this.lblGender.StylePriority.UseForeColor = false;
            this.lblGender.StylePriority.UseTextAlignment = false;
            this.lblGender.Text = "lblGender";
            this.lblGender.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblNationality
            // 
            this.lblNationality.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Nationality]),\'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Nationality]")});
            this.lblNationality.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNationality.LocationFloat = new DevExpress.Utils.PointFloat(434.4633F, 399F);
            this.lblNationality.Name = "lblNationality";
            this.lblNationality.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblNationality.SizeF = new System.Drawing.SizeF(251.0692F, 31.61755F);
            this.lblNationality.StylePriority.UseFont = false;
            this.lblNationality.StylePriority.UseTextAlignment = false;
            this.lblNationality.Text = "lblNationality";
            this.lblNationality.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblNameEn
            // 
            this.lblNameEn.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Fullname En]), \'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Fullname En]")});
            this.lblNameEn.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNameEn.LocationFloat = new DevExpress.Utils.PointFloat(106.36F, 367F);
            this.lblNameEn.Name = "lblNameEn";
            this.lblNameEn.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblNameEn.SizeF = new System.Drawing.SizeF(676.8585F, 24.99997F);
            this.lblNameEn.StylePriority.UseFont = false;
            this.lblNameEn.StylePriority.UseTextAlignment = false;
            this.lblNameEn.Text = "lblNameEn";
            this.lblNameEn.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleLeft;
            // 
            // lblNameAr
            // 
            this.lblNameAr.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Fullname Ar]),\'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Fullname Ar]")});
            this.lblNameAr.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblNameAr.LocationFloat = new DevExpress.Utils.PointFloat(44.88F, 335F);
            this.lblNameAr.Name = "lblNameAr";
            this.lblNameAr.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblNameAr.SizeF = new System.Drawing.SizeF(642.0224F, 24.99997F);
            this.lblNameAr.StylePriority.UseFont = false;
            this.lblNameAr.StylePriority.UseTextAlignment = false;
            this.lblNameAr.Text = "lblNameAr";
            this.lblNameAr.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // lblFees
            // 
            this.lblFees.CanGrow = false;
            this.lblFees.ExpressionBindings.AddRange(new DevExpress.XtraReports.UI.ExpressionBinding[] {
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "StyleName", "Iif(IsNull([Parameters.Fees]),\'XrStyleWhite\', \'XrStyleBlack\')"),
            new DevExpress.XtraReports.UI.ExpressionBinding("BeforePrint", "Text", "[Parameters.Fees]")});
            this.lblFees.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblFees.LocationFloat = new DevExpress.Utils.PointFloat(321.4359F, 210.6667F);
            this.lblFees.Name = "lblFees";
            this.lblFees.Padding = new DevExpress.XtraPrinting.PaddingInfo(2, 2, 0, 0, 100F);
            this.lblFees.SizeF = new System.Drawing.SizeF(241.7383F, 36.97917F);
            this.lblFees.StylePriority.UseFont = false;
            this.lblFees.StylePriority.UseTextAlignment = false;
            this.lblFees.Text = "lblFees";
            this.lblFees.TextAlignment = DevExpress.XtraPrinting.TextAlignment.MiddleRight;
            // 
            // xrPictureBox2
            // 
            this.xrPictureBox2.LocationFloat = new DevExpress.Utils.PointFloat(0F, 0F);
            this.xrPictureBox2.Name = "xrPictureBox2";
            this.xrPictureBox2.SizeF = new System.Drawing.SizeF(821F, 1164F);
            this.xrPictureBox2.Sizing = DevExpress.XtraPrinting.ImageSizeMode.StretchImage;
            // 
            // Date
            // 
            this.Date.Description = "Date";
            this.Date.Name = "Date";
            this.Date.Type = typeof(System.DateTime);
            // 
            // Fees
            // 
            this.Fees.Description = "Fees";
            this.Fees.Name = "Fees";
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
            this.BottomMargin.HeightF = 0.6667455F;
            this.BottomMargin.Name = "BottomMargin";
            this.BottomMargin.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            this.BottomMargin.TextAlignment = DevExpress.XtraPrinting.TextAlignment.TopLeft;
            // 
            // empcode
            // 
            this.empcode.Description = "Emp Code";
            this.empcode.Name = "empcode";
            // 
            // CalcGender
            // 
            this.CalcGender.DataMember = "Query";
            this.CalcGender.Expression = "Iif([Gender]  == 0, \'الذكر\'  , Iif([Gender]  == 1, \'إناثا\' ,\'\'))";
            this.CalcGender.Name = "CalcGender";
            // 
            // Document_Type
            // 
            this.Document_Type.Description = "Document Type";
            dynamicListLookUpSettings1.DataAdapter = null;
            dynamicListLookUpSettings1.DataMember = "DocumentType";
            dynamicListLookUpSettings1.DataSource = this.dsGovtForm;
            dynamicListLookUpSettings1.DisplayMember = "NameAr";
            dynamicListLookUpSettings1.ValueMember = "OID";
            this.Document_Type.LookUpSettings = dynamicListLookUpSettings1;
            this.Document_Type.Name = "Document_Type";
            this.Document_Type.Type = typeof(int);
            this.Document_Type.ValueInfo = "0";
            // 
            // CalcParamDateAr
            // 
            this.CalcParamDateAr.DataMember = "Query";
            this.CalcParamDateAr.DisplayName = "CalcParamDateAr";
            this.CalcParamDateAr.Expression = @"Iif([calcParamMonthNum] == 1 ,'يناير',Iif([calcParamMonthNum] == 2, 'فبراير' , Iif([calcParamMonthNum] == 3, 'مارس' ,Iif([calcParamMonthNum] = 4, 'أبريل' ,Iif([calcParamMonthNum] == 5, 'مايو' ,Iif([calcParamMonthNum] == 6, 'يونيو' ,Iif([calcParamMonthNum] == 7, 'يوليو'  , Iif([calcParamMonthNum] == 8, 'أغسطس' ,Iif([calcParamMonthNum] == 9, 'سبتمبر' , Iif([calcParamMonthNum] == 10, 'أكتوبر' , Iif([calcParamMonthNum] == 11, 'نوفمبر' , Iif([calcParamMonthNum] == 12, 'ديسمبر' ,' ' ))))))))))))";
            this.CalcParamDateAr.Name = "CalcParamDateAr";
            // 
            // CalcParamMonthNum
            // 
            this.CalcParamMonthNum.DataMember = "Query";
            this.CalcParamMonthNum.DisplayName = "CalcParamMonthNum";
            this.CalcParamMonthNum.Expression = "GetMonth([Parameters].[Date])";
            this.CalcParamMonthNum.Name = "CalcParamMonthNum";
            // 
            // XrStyleWhite
            // 
            this.XrStyleWhite.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.XrStyleWhite.Name = "XrStyleWhite";
            this.XrStyleWhite.Padding = new DevExpress.XtraPrinting.PaddingInfo(0, 0, 0, 0, 100F);
            // 
            // CalcBirthMonthNum
            // 
            this.CalcBirthMonthNum.DataMember = "Query";
            this.CalcBirthMonthNum.Expression = "GetMonth([BirthDate])";
            this.CalcBirthMonthNum.Name = "CalcBirthMonthNum";
            // 
            // CalcBirthDateAr
            // 
            this.CalcBirthDateAr.DataMember = "Query";
            this.CalcBirthDateAr.Expression = @"Iif([CalcBirthMonthNum] == 1 ,'يناير',Iif([CalcBirthMonthNum] == 2, 'فبراير' , Iif([CalcBirthMonthNum] == 3, 'مارس' ,Iif([CalcBirthMonthNum] = 4, 'أبريل' ,Iif([CalcBirthMonthNum] == 5, 'مايو' ,Iif([CalcBirthMonthNum] == 6, 'يونيو' ,Iif([CalcBirthMonthNum] == 7, 'يوليو'  , Iif([CalcBirthMonthNum] == 8, 'أغسطس' ,Iif([CalcBirthMonthNum] == 9, 'سبتمبر' , Iif([CalcBirthMonthNum] == 10, 'أكتوبر' , Iif([CalcBirthMonthNum] == 11, 'نوفمبر' , Iif([CalcBirthMonthNum] == 12, 'ديسمبر' ,' ' ))))))))))))";
            this.CalcBirthDateAr.Name = "CalcBirthDateAr";
            // 
            // CalcExpiryMonthNumber
            // 
            this.CalcExpiryMonthNumber.DataMember = "Query";
            this.CalcExpiryMonthNumber.Expression = "GetMonth([ExpiryDate])";
            this.CalcExpiryMonthNumber.Name = "CalcExpiryMonthNumber";
            // 
            // CalcExpiryMonthAr
            // 
            this.CalcExpiryMonthAr.DataMember = "Query";
            this.CalcExpiryMonthAr.Expression = @"Iif([calcExpiryMonthNumber] == 1 ,'يناير',Iif([calcExpiryMonthNumber] == 2, 'فبراير' , Iif([calcExpiryMonthNumber] == 3, 'مارس' ,Iif([calcExpiryMonthNumber] = 4, 'أبريل' ,Iif([calcExpiryMonthNumber] == 5, 'مايو' ,Iif([calcExpiryMonthNumber] == 6, 'يونيو' ,Iif([calcExpiryMonthNumber] == 7, 'يوليو'  , Iif([calcExpiryMonthNumber] == 8, 'أغسطس' ,Iif([calcExpiryMonthNumber] == 9, 'سبتمبر' , Iif([calcExpiryMonthNumber] == 10, 'أكتوبر' , Iif([calcExpiryMonthNumber] == 11, 'نوفمبر' , Iif([calcExpiryMonthNumber] == 12, 'ديسمبر' ,' ' ))))))))))))";
            this.CalcExpiryMonthAr.Name = "CalcExpiryMonthAr";
            // 
            // CalcIssueDateMonthNum
            // 
            this.CalcIssueDateMonthNum.DataMember = "Query";
            this.CalcIssueDateMonthNum.Expression = "GetMonth([IssueDate])";
            this.CalcIssueDateMonthNum.Name = "CalcIssueDateMonthNum";
            // 
            // CalcIssueMonthAr
            // 
            this.CalcIssueMonthAr.DataMember = "Query";
            this.CalcIssueMonthAr.Expression = @"Iif([CalcIssueDateMonthNum] == 1 ,'يناير',Iif([CalcIssueDateMonthNum] == 2, 'فبراير' , Iif([CalcIssueDateMonthNum] == 3, 'مارس' ,Iif([CalcIssueDateMonthNum] = 4, 'أبريل' ,Iif([CalcIssueDateMonthNum] == 5, 'مايو' ,Iif([CalcIssueDateMonthNum] == 6, 'يونيو' ,Iif([CalcIssueDateMonthNum] == 7, 'يوليو'  , Iif([CalcIssueDateMonthNum] == 8, 'أغسطس' ,Iif([CalcIssueDateMonthNum] == 9, 'سبتمبر' , Iif([CalcIssueDateMonthNum] == 10, 'أكتوبر' , Iif([CalcIssueDateMonthNum] == 11, 'نوفمبر' , Iif([CalcIssueDateMonthNum] == 12, 'ديسمبر' ,' ' ))))))))))))";
            this.CalcIssueMonthAr.Name = "CalcIssueMonthAr";
            // 
            // XrStyleBlack
            // 
            this.XrStyleBlack.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.XrStyleBlack.Name = "XrStyleBlack";
            // 
            // rptWFDrivingLicenseForm
            // 
            this.Bands.AddRange(new DevExpress.XtraReports.UI.Band[] {
            this.Detail,
            this.TopMargin,
            this.BottomMargin});
            this.CalculatedFields.AddRange(new DevExpress.XtraReports.UI.CalculatedField[] {
            this.CalcGender,
            this.CalcParamDateAr,
            this.CalcParamMonthNum,
            this.CalcBirthMonthNum,
            this.CalcBirthDateAr,
            this.CalcExpiryMonthNumber,
            this.CalcExpiryMonthAr,
            this.CalcIssueDateMonthNum,
            this.CalcIssueMonthAr});
            this.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            this.dsGovtForm});
            this.DataMember = "Query";
            this.DataSource = this.dsGovtForm;
            this.FilterString = "[Emp code] In (?empcode) And [DoctypeOID] = ?Document_Type And [Status] = 1";
            this.Margins = new System.Drawing.Printing.Margins(2, 4, 0, 1);
            this.PageHeight = 1169;
            this.PageWidth = 827;
            this.PaperKind = DevExpress.Drawing.Printing.DXPaperKind.A4;
            this.Parameters.AddRange(new DevExpress.XtraReports.Parameters.Parameter[] {
            this.empcode,
            this.Date,
            this.Fees,
            this.Document_Type});
            this.StyleSheet.AddRange(new DevExpress.XtraReports.UI.XRControlStyle[] {
            this.XrStyleWhite,
            this.XrStyleBlack});
            this.Version = "23.2";
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();

        }

        #endregion

        private DevExpress.XtraReports.UI.DetailBand Detail;
        private DevExpress.XtraReports.UI.TopMarginBand TopMargin;
        private DevExpress.XtraReports.UI.BottomMarginBand BottomMargin;
        private DevExpress.XtraReports.UI.XRLabel lblNameEn;
        private DevExpress.XtraReports.UI.XRLabel lblNameAr;
        private DevExpress.XtraReports.UI.XRLabel lblFees;
        private DevExpress.XtraReports.UI.XRLabel lblWorkAddress;
        private DevExpress.XtraReports.UI.XRLabel lblHomeAddress;
        private DevExpress.XtraReports.UI.XRLabel lblJobtitle;
        private DevExpress.XtraReports.UI.XRLabel lblBloodGroup;
        private DevExpress.XtraReports.UI.XRLabel lblGender;
        private DevExpress.XtraReports.UI.XRLabel lblNationality;
        private DevExpress.XtraReports.Parameters.Parameter Fees;
        private DevExpress.XtraReports.Parameters.Parameter Date;
        private DevExpress.DataAccess.Sql.SqlDataSource dsGovtForm;
        private DevExpress.XtraReports.Parameters.Parameter empcode;
        private DevExpress.XtraReports.UI.CalculatedField CalcGender;
        private DevExpress.XtraReports.UI.XRLabel lblLicenceType;
        private DevExpress.XtraReports.UI.XRLabel lblLicenceNumber;
        private DevExpress.XtraReports.Parameters.Parameter Document_Type;
        private DevExpress.XtraReports.UI.CalculatedField CalcParamDateAr;
        private DevExpress.XtraReports.UI.CalculatedField CalcParamMonthNum;
        private DevExpress.XtraReports.UI.XRLabel lblParamDateYear;
        private DevExpress.XtraReports.UI.XRControlStyle XrStyleWhite;
        private DevExpress.XtraReports.UI.XRLabel lblBirthDateDay;
        private DevExpress.XtraReports.UI.CalculatedField CalcBirthMonthNum;
        private DevExpress.XtraReports.UI.CalculatedField CalcBirthDateAr;
        private DevExpress.XtraReports.UI.XRLabel lblExpiryDateDay;
        private DevExpress.XtraReports.UI.CalculatedField CalcExpiryMonthNumber;
        private DevExpress.XtraReports.UI.CalculatedField CalcExpiryMonthAr;
        private DevExpress.XtraReports.UI.XRLabel lblIssueDateDay;
        private DevExpress.XtraReports.UI.CalculatedField CalcIssueDateMonthNum;
        private DevExpress.XtraReports.UI.CalculatedField CalcIssueMonthAr;
        private DevExpress.XtraReports.UI.XRLabel lblResidencyNumber;
        private DevExpress.XtraReports.UI.XRLabel xrLabel1;
        private DevExpress.XtraReports.UI.XRControlStyle XrStyleBlack;
        private DevExpress.XtraReports.UI.XRLabel xrLabel2;
        private DevExpress.XtraReports.UI.XRLabel xrLabel3;
        private DevExpress.XtraReports.UI.XRPictureBox xrPictureBox2;
    }
}
