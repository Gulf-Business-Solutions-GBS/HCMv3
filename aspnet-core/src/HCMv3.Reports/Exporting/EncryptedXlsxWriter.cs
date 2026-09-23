using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using DevExpress.Export.Xl;
using DevExpress.Office.Crypto;

namespace HCMv3.Reports.Exporting
{
    /// <summary>
    /// Writes a grid report's table as an .xlsx that asks for a password before it opens.
    ///
    /// Legacy <c>GridReportHelper.Export</c> did this when <c>ManageSecuritySettings.EnableReportExportPwd</c>
    /// was on: <c>XlsxExportOptions.EncryptionOptions.Password = pwd; Type = XlEncryptionType.Strong</c>. The
    /// normal HCMv3 export goes through MiniExcel, which cannot encrypt, so protected exports come here and
    /// use DevExpress's own Excel export library (already shipped with the reporting package) with the same
    /// "strong" encryption.
    /// </summary>
    public static class EncryptedXlsxWriter
    {
        /// <param name="table">Column captions and cell values, exactly as the plain export would write them.</param>
        /// <param name="numericColumnIndexes">Columns that get <paramref name="numberFormat"/>.</param>
        /// <param name="numberFormat">Excel number format such as <c>#,##0.000</c>.</param>
        /// <param name="password">The password Excel asks for. Must not be blank.</param>
        public static byte[] Write(
            DataTable table,
            ICollection<int> numericColumnIndexes,
            string numberFormat,
            string password)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("A password is required for a protected export.", nameof(password));
            }

            var numeric = new HashSet<int>(numericColumnIndexes ?? Array.Empty<int>());
            var encryption = new EncryptionOptions { Password = password, Type = ModelEncryptionType.Strong };
            var exporter = XlExport.CreateExporter(XlDocumentFormat.Xlsx);

            using (var stream = new MemoryStream())
            {
                using (var document = exporter.CreateDocument(stream, encryption))
                using (var sheet = document.CreateSheet())
                {
                    for (var c = 0; c < table.Columns.Count; c++)
                    {
                        using (var column = sheet.CreateColumn())
                        {
                            column.WidthInPixels = 120;
                        }
                    }

                    var headerFormatting = new XlCellFormatting { Font = new XlFont { Bold = true } };
                    using (var header = sheet.CreateRow())
                    {
                        foreach (DataColumn dataColumn in table.Columns)
                        {
                            using (var cell = header.CreateCell())
                            {
                                cell.Value = dataColumn.ColumnName;
                                cell.ApplyFormatting(headerFormatting);
                            }
                        }
                    }

                    var numberFormatting = new XlCellFormatting { NumberFormat = numberFormat };
                    foreach (DataRow dataRow in table.Rows)
                    {
                        using (var row = sheet.CreateRow())
                        {
                            for (var c = 0; c < table.Columns.Count; c++)
                            {
                                using (var cell = row.CreateCell())
                                {
                                    var value = dataRow[c];
                                    if (value == null || value == DBNull.Value)
                                    {
                                        continue;
                                    }

                                    cell.Value = XlVariantValue.FromObject(value);
                                    if (numeric.Contains(c))
                                    {
                                        cell.ApplyFormatting(numberFormatting);
                                    }
                                }
                            }
                        }
                    }
                }

                return stream.ToArray();
            }
        }
    }
}
