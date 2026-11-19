using DemoMVC.Models;
using DemoMVC.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers;

public sealed class ExcelImportController(IExcelImportService importService) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        var model = new ExcelImportViewModel();
        if (TempData.TryGetValue("ImportResult", out var resultJson) && resultJson is string json)
        {
            model.Result = System.Text.Json.JsonSerializer.Deserialize<ExcelImportResultViewModel>(json);
        }
        if (TempData.TryGetValue("ImportError", out var error) && error is string message)
        {
            model.Errors.Add(message);
        }
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<IActionResult> Preview(IFormFile? file, string? entityName, CancellationToken cancellationToken)
    {
        var model = new ExcelImportViewModel { EntityName = entityName ?? string.Empty };
        if (file is null || string.IsNullOrWhiteSpace(entityName))
        {
            model.Errors.Add("Vui lòng chọn thực thể và tệp Excel.");
            return View("Index", model);
        }

        try
        {
            model = await importService.PreviewAsync(file, entityName, cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            model.Errors.Add(exception.Message);
        }
        return View("Index", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(Guid previewToken, string? mode, CancellationToken cancellationToken)
    {
        try
        {
            var result = await importService.ExecuteAsync(previewToken, mode ?? string.Empty, cancellationToken);
            TempData["ImportResult"] = System.Text.Json.JsonSerializer.Serialize(result);
        }
        catch (InvalidDataException exception)
        {
            TempData["ImportError"] = exception.Message;
        }
        catch (DbUpdateException exception)
        {
            TempData["ImportError"] = "Cơ sở dữ liệu từ chối dữ liệu nhập. Không có thay đổi nào được xác nhận.";
            HttpContext.RequestServices.GetRequiredService<ILogger<ExcelImportController>>()
                .LogError(exception, "Excel import was rejected by the database.");
        }
        return RedirectToAction(nameof(Index));
    }
}
