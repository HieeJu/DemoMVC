namespace DemoMVC.Models;

public sealed class ExcelImportViewModel
{
    public string EntityName { get; set; } = string.Empty;
    public Guid PreviewToken { get; set; }
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public string? Message { get; set; }
    public List<ExcelImportRowViewModel> Rows { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public ExcelImportResultViewModel? Result { get; set; }
}

public sealed class ExcelImportRowViewModel
{
    public int RowNumber { get; set; }
    public string Summary { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = [];
}

public sealed class ExcelImportResultViewModel
{
    public int TotalRows { get; set; }
    public int SuccessRows { get; set; }
    public int ErrorRows { get; set; }
    public List<string> Errors { get; set; } = [];
    public string Message { get; set; } = string.Empty;
}
