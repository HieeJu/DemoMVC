using DemoMVC.Data;
using DemoMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoMVC.Controllers;

public sealed class FacultyController(ApplicationDbContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index() => View(await context.Faculties.OrderBy(item => item.FacultyName).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Rows() =>
        PartialView("_Rows", await context.Faculties.OrderBy(item => item.FacultyName).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        var faculty = await context.Faculties.FindAsync(id);
        return faculty is null ? NotFound() : PartialView("_Details", faculty);
    }

    [HttpGet]
    public IActionResult Create() => PartialView("_Form", new Faculty());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Faculty faculty)
    {
        if (await context.Faculties.AnyAsync(item => item.FacultyId == faculty.FacultyId))
        {
            ModelState.AddModelError(nameof(faculty.FacultyId), "Mã khoa đã tồn tại.");
        }

        if (!ModelState.IsValid) return InvalidPartial("_Form", faculty);

        context.Faculties.Add(faculty);
        await context.SaveChangesAsync();
        return Json(new { success = true, message = "Đã thêm khoa." });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var faculty = await context.Faculties.FindAsync(id);
        return faculty is null ? NotFound() : PartialView("_Form", faculty);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Faculty input)
    {
        if (id != input.FacultyId) return BadRequest(new { message = "Mã khoa không hợp lệ." });
        if (!ModelState.IsValid) return InvalidPartial("_Form", input);

        var faculty = await context.Faculties.FindAsync(id);
        if (faculty is null) return NotFound();
        faculty.FacultyName = input.FacultyName;
        faculty.MajorCode = input.MajorCode;
        await context.SaveChangesAsync();
        return Json(new { success = true, message = "Đã cập nhật khoa." });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var faculty = await context.Faculties.FindAsync(id);
        return faculty is null ? NotFound() : PartialView("_Delete", faculty);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var faculty = await context.Faculties.FindAsync(id);
        if (faculty is null) return NotFound();
        if (await context.Students.AnyAsync(student => student.FacultyId == id))
        {
            return Conflict(new { message = "Không thể xóa khoa đang có sinh viên." });
        }

        context.Faculties.Remove(faculty);
        await context.SaveChangesAsync();
        return Json(new { success = true, message = "Đã xóa khoa." });
    }

    private IActionResult InvalidPartial(string viewName, Faculty model)
    {
        Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        return PartialView(viewName, model);
    }
}
