using System.ComponentModel.DataAnnotations;
using System.Text;
using DemoMVC.Data;
using DemoMVC.Models;
using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Services;

public sealed class ExcelImportService(
    ApplicationDbContext context,
    ExcelImportPreviewStore previewStore) : IExcelImportService
{
    private const long MaximumFileBytes = 10 * 1024 * 1024;
    private const int MaximumRows = 10_000;

    public async Task<ExcelImportViewModel> PreviewAsync(
        IFormFile file,
        string entityName,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0 || file.Length > MaximumFileBytes)
        {
            throw new InvalidDataException("Tệp rỗng hoặc vượt quá giới hạn 10 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!string.Equals(extension, ".xlsx", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".xls", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Chỉ chấp nhận tệp .xlsx hoặc .xls.");
        }

        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        await using var stream = file.OpenReadStream();
        using var reader = ExcelReaderFactory.CreateReader(stream);
        if (!reader.Read())
        {
            throw new InvalidDataException("Tệp Excel không có dòng tiêu đề.");
        }

        var headers = ReadHeaders(reader);
        var plan = entityName switch
        {
            "Students" => await ReadStudentsAsync(reader, headers, cancellationToken),
            "Faculties" => await ReadFacultiesAsync(reader, headers, cancellationToken),
            _ => throw new InvalidDataException("Thực thể nhập không được hỗ trợ.")
        };
        if (plan.Rows.Count == 0)
        {
            throw new InvalidDataException("Tệp không có dòng dữ liệu để nhập.");
        }

        if (plan.EntityName == "Students")
        {
            var facultyIds = plan.Students.Select(student => student.FacultyId ?? string.Empty)
                .Where(id => id.Length > 0).Distinct().ToList();
            var knownFacultyIds = await context.Faculties
                .Where(faculty => facultyIds.Contains(faculty.FacultyId))
                .Select(faculty => faculty.FacultyId)
                .ToListAsync(cancellationToken);
            for (var index = 0; index < plan.Students.Count; index++)
            {
                var facultyId = plan.Students[index].FacultyId;
                if (!string.IsNullOrWhiteSpace(facultyId) && !knownFacultyIds.Contains(facultyId))
                {
                    plan.Rows[index].Errors.Add("Mã khoa không tồn tại.");
                }
            }
        }

        var token = previewStore.Store(plan);
        return new ExcelImportViewModel
        {
            EntityName = plan.EntityName,
            PreviewToken = token,
            TotalRows = plan.Rows.Count,
            ValidRows = plan.Rows.Count(row => row.Errors.Count == 0),
            InvalidRows = plan.Rows.Count(row => row.Errors.Count > 0),
            Rows = plan.Rows.Take(50).ToList()
        };
    }

    public async Task<ExcelImportResultViewModel> ExecuteAsync(
        Guid previewToken,
        string mode,
        CancellationToken cancellationToken)
    {
        if (mode is not ("Overwrite" or "Append"))
        {
            throw new InvalidDataException("Phải xác nhận chế độ Overwrite hoặc Append.");
        }

        if (!previewStore.TryTake(previewToken, out var plan) || plan is null)
        {
            throw new InvalidDataException("Bản xem trước đã hết hạn hoặc đã được sử dụng. Hãy tải tệp lên lại.");
        }

        if (mode == "Overwrite" && plan.Rows.Any(row => row.Errors.Count > 0))
        {
            return Failure(plan.Rows, "Overwrite bị hủy vì tệp có dòng dữ liệu không hợp lệ.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var result = new ExcelImportResultViewModel { TotalRows = plan.Rows.Count };
        if (plan.EntityName == "Faculties" && mode == "Overwrite")
        {
            if (await context.Students.AnyAsync(cancellationToken))
            {
                return Failure(plan.Rows, "Không thể ghi đè danh sách khoa khi vẫn còn sinh viên tham chiếu tới khoa.");
            }
            context.Faculties.RemoveRange(await context.Faculties.ToListAsync(cancellationToken));
            await context.SaveChangesAsync(cancellationToken);
            context.Faculties.AddRange(plan.Faculties);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            result.SuccessRows = plan.Rows.Count;
            result.Message = "Đã thay thế toàn bộ danh sách khoa.";
            return result;
        }

        if (plan.EntityName == "Students" && mode == "Overwrite")
        {
            var validRows = plan.Rows.Where(row => row.Errors.Count == 0).ToList();
            if (validRows.Count != plan.Rows.Count)
            {
                return Failure(plan.Rows, "Overwrite bị hủy vì tệp có dòng dữ liệu không hợp lệ.");
            }

            var facultyIds = plan.Students.Select(student => student.FacultyId!).Distinct().ToList();
            var knownFacultyIds = await context.Faculties
                .Where(faculty => facultyIds.Contains(faculty.FacultyId))
                .Select(faculty => faculty.FacultyId)
                .ToListAsync(cancellationToken);
            if (facultyIds.Except(knownFacultyIds).Any())
            {
                return Failure(plan.Rows, "Overwrite bị hủy vì có Student tham chiếu tới Faculty không tồn tại.");
            }

            context.Students.RemoveRange(await context.Students.ToListAsync(cancellationToken));
            await context.SaveChangesAsync(cancellationToken);
            context.Students.AddRange(plan.Students);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            result.SuccessRows = plan.Rows.Count;
            result.Message = "Đã thay thế toàn bộ danh sách sinh viên.";
            return result;
        }

        if (plan.EntityName == "Faculties")
        {
            var rowsByKey = plan.Faculties.Select((faculty, index) => (faculty, row: plan.Rows[index]))
                .Where(item => item.row.Errors.Count == 0).ToList();
            var keys = rowsByKey.Select(item => item.faculty.FacultyId).ToList();
            var existing = await context.Faculties.Where(faculty => keys.Contains(faculty.FacultyId))
                .Select(faculty => faculty.FacultyId).ToListAsync(cancellationToken);
            foreach (var item in rowsByKey)
            {
                if (existing.Contains(item.faculty.FacultyId))
                {
                    item.row.Errors.Add("Mã khoa đã tồn tại; Append không cập nhật bản ghi cũ.");
                }
                else
                {
                    context.Faculties.Add(item.faculty);
                }
            }
        }
        else
        {
            var rowsByKey = plan.Students.Select((student, index) => (student, row: plan.Rows[index]))
                .Where(item => item.row.Errors.Count == 0).ToList();
            var keys = rowsByKey.Select(item => item.student.StudentId).ToList();
            var existing = await context.Students.Where(student => keys.Contains(student.StudentId))
                .Select(student => student.StudentId).ToListAsync(cancellationToken);
            var facultyIds = rowsByKey.Select(item => item.student.FacultyId!).Distinct().ToList();
            var knownFacultyIds = await context.Faculties.Where(faculty => facultyIds.Contains(faculty.FacultyId))
                .Select(faculty => faculty.FacultyId).ToListAsync(cancellationToken);

            foreach (var item in rowsByKey)
            {
                if (existing.Contains(item.student.StudentId))
                {
                    item.row.Errors.Add("Mã sinh viên đã tồn tại; Append không cập nhật bản ghi cũ.");
                }
                else if (!knownFacultyIds.Contains(item.student.FacultyId!))
                {
                    item.row.Errors.Add("Mã khoa không tồn tại.");
                }
                else
                {
                    context.Students.Add(item.student);
                }
            }
        }

        result.SuccessRows = plan.Rows.Count(row => row.Errors.Count == 0);
        result.ErrorRows = plan.Rows.Count - result.SuccessRows;
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        result.Errors = FormatErrors(plan.Rows);
        result.Message = $"Nhập hoàn tất: {result.SuccessRows} dòng thành công, {result.ErrorRows} dòng lỗi.";
        return result;
    }

    private async Task<ExcelImportPlan> ReadStudentsAsync(
        IExcelDataReader reader,
        IReadOnlyDictionary<string, int> headers,
        CancellationToken cancellationToken)
    {
        RequireHeaders(headers, "StudentId", "FullName", "Address", "Email", "FacultyId");
        var plan = new ExcelImportPlan { EntityName = "Students" };
        var physicalRowNumber = 1;
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            physicalRowNumber++;
            if (IsEmptyRow(reader)) continue;
            if (plan.Rows.Count >= MaximumRows) throw new InvalidDataException("Tệp vượt quá giới hạn 10.000 dòng.");

            var student = new Student
            {
                StudentId = ReadCell(reader, headers, "StudentId"),
                FullName = ReadCell(reader, headers, "FullName"),
                Address = ReadCell(reader, headers, "Address"),
                Email = ReadCell(reader, headers, "Email"),
                FacultyId = ReadCell(reader, headers, "FacultyId")
            };
            var row = CreateRow(physicalRowNumber, student, student.StudentId);
            plan.Students.Add(student);
            plan.Rows.Add(row);
        }
        MarkDuplicateKeys(plan.Rows, plan.Students.Select(student => student.StudentId), "Mã sinh viên bị trùng trong tệp.");
        await Task.CompletedTask;
        return plan;
    }

    private async Task<ExcelImportPlan> ReadFacultiesAsync(
        IExcelDataReader reader,
        IReadOnlyDictionary<string, int> headers,
        CancellationToken cancellationToken)
    {
        RequireHeaders(headers, "FacultyId", "FacultyName", "MajorCode");
        var plan = new ExcelImportPlan { EntityName = "Faculties" };
        var physicalRowNumber = 1;
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            physicalRowNumber++;
            if (IsEmptyRow(reader)) continue;
            if (plan.Rows.Count >= MaximumRows) throw new InvalidDataException("Tệp vượt quá giới hạn 10.000 dòng.");

            var faculty = new Faculty
            {
                FacultyId = ReadCell(reader, headers, "FacultyId"),
                FacultyName = ReadCell(reader, headers, "FacultyName"),
                MajorCode = ReadCell(reader, headers, "MajorCode") is { Length: > 0 } majorCode ? majorCode : "01"
            };
            var row = CreateRow(physicalRowNumber, faculty, faculty.FacultyId);
            plan.Faculties.Add(faculty);
            plan.Rows.Add(row);
        }
        MarkDuplicateKeys(plan.Rows, plan.Faculties.Select(faculty => faculty.FacultyId), "Mã khoa bị trùng trong tệp.");
        await Task.CompletedTask;
        return plan;
    }

    private static ExcelImportRowViewModel CreateRow(int rowNumber, object model, string key)
    {
        var row = new ExcelImportRowViewModel { RowNumber = rowNumber, Summary = key };
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(model, new ValidationContext(model), validationResults, validateAllProperties: true))
        {
            row.Errors.AddRange(validationResults.Select(result => result.ErrorMessage ?? "Dữ liệu không hợp lệ."));
        }
        return row;
    }

    private static IReadOnlyDictionary<string, int> ReadHeaders(IExcelDataReader reader)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < reader.FieldCount; index++)
        {
            var header = reader.GetValue(index)?.ToString()?.Trim();
            if (!string.IsNullOrEmpty(header) && !result.TryAdd(header, index))
            {
                throw new InvalidDataException($"Cột '{header}' bị lặp.");
            }
        }
        return result;
    }

    private static void RequireHeaders(IReadOnlyDictionary<string, int> headers, params string[] required)
    {
        var missing = required.Where(header => !headers.ContainsKey(header)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidDataException($"Thiếu cột bắt buộc: {string.Join(", ", missing)}.");
        }
    }

    private static string ReadCell(IExcelDataReader reader, IReadOnlyDictionary<string, int> headers, string name) =>
        reader.GetValue(headers[name])?.ToString()?.Trim() ?? string.Empty;

    private static bool IsEmptyRow(IExcelDataReader reader) =>
        Enumerable.Range(0, reader.FieldCount).All(index => string.IsNullOrWhiteSpace(reader.GetValue(index)?.ToString()));

    private static void MarkDuplicateKeys(
        IReadOnlyList<ExcelImportRowViewModel> rows,
        IEnumerable<string> keys,
        string message)
    {
        var keyList = keys.ToList();
        var duplicates = keyList.GroupBy(key => key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < rows.Count; index++)
        {
            if (duplicates.Contains(keyList[index]))
            {
                rows[index].Errors.Add(message);
            }
        }
    }

    private static ExcelImportResultViewModel Failure(List<ExcelImportRowViewModel> rows, string message) =>
        new()
        {
            TotalRows = rows.Count,
            ErrorRows = rows.Count,
            Errors = [message, .. FormatErrors(rows)],
            Message = message
        };

    private static List<string> FormatErrors(IEnumerable<ExcelImportRowViewModel> rows) =>
        rows.Where(row => row.Errors.Count > 0)
            .SelectMany(row => row.Errors.Select(error => $"Dòng {row.RowNumber}: {error}"))
            .Take(100)
            .ToList();
}
