namespace HCMv3.DataImporting.Excel;

public class ImportFromExcelDto
{
    public int? TenantId { get; set; }

    /// <summary>
    /// Can be set when reading data from excel or when importing user
    /// </summary>
    public string Exception { get; set; }
}