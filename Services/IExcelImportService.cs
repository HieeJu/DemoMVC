using DemoMVC.Models;
using Microsoft.AspNetCore.Http;

namespace DemoMVC.Services;

public interface IExcelImportService
{
    Task<ExcelImportViewModel> PreviewAsync(IFormFile file, string entityName, CancellationToken cancellationToken);
    Task<ExcelImportResultViewModel> ExecuteAsync(Guid previewToken, string mode, CancellationToken cancellationToken);
}
