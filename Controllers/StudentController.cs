using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;
using DemoMVC.Models;
using DemoMVC.Process;

namespace DemoMVC.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StudentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. READ: Danh sách sinh viên (Có JOIN bảng Faculty)
        // ==========================================
        public async Task<IActionResult> Index()
        {
            var students = await _context.Students
                .Include(s => s.Faculty)
                .ToListAsync();

            return View(students);
        }

        [HttpGet]
        public async Task<IActionResult> Rows()
        {
            var students = await _context.Students.Include(student => student.Faculty).ToListAsync();
            return PartialView("_Rows", students);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var student = await _context.Students.Include(item => item.Faculty)
                .FirstOrDefaultAsync(item => item.StudentId == id);
            if (student is null) return NotFound();
            return IsAjaxRequest() ? PartialView("_Details", student) : View(student);
        }

        // ==========================================
        // 2. CREATE: Giao diện Thêm mới
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName");
            return IsAjaxRequest() ? PartialView("_Form", new Student()) : View();
        }

        // ==========================================
        // 2. CREATE: Xử lý Thêm mới (Sinh mã tự động theo Khoa)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Address,Email,FacultyId")] Student std, string yearPrefix = "28", string systemPrefix = "21")
        {
            var faculty = await _context.Faculties.FindAsync(std.FacultyId);
            if (faculty is null)
            {
                ModelState.AddModelError(nameof(std.FacultyId), "Khoa được chọn không tồn tại.");
                return await CreateInvalidAsync(std);
            }

            string majorPrefix = faculty.MajorCode;
            string prefix = $"{yearPrefix}{systemPrefix}{majorPrefix}";

            var maxStudentId = await _context.Students
                .Where(s => s.StudentId.StartsWith(prefix))
                .OrderByDescending(s => s.StudentId)
                .Select(s => s.StudentId)
                .FirstOrDefaultAsync();

            std.StudentId = StringProcess.GenerateStudentId(prefix, maxStudentId ?? string.Empty);
            ModelState.Remove("StudentId");

            if (ModelState.IsValid)
            {
                _context.Add(std);
                await _context.SaveChangesAsync();
                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = $"Đã thêm sinh viên {std.FullName} ({std.StudentId})." });
                }
                return RedirectToAction(nameof(Index));
            }

            return await CreateInvalidAsync(std);
        }

        // ==========================================
        // 3. UPDATE: Giao diện Chỉnh sửa
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", student.FacultyId);
            return IsAjaxRequest() ? PartialView("_Form", student) : View(student);
        }

        // ==========================================
        // 3. UPDATE: Xử lý Lưu chỉnh sửa
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("StudentId,FullName,Address,Email,FacultyId")] Student std)
        {
            if (id != std.StudentId) return NotFound();

            var facultyExists = await _context.Faculties.AnyAsync(faculty => faculty.FacultyId == std.FacultyId);
            if (!facultyExists)
            {
                ModelState.AddModelError(nameof(std.FacultyId), "Khoa được chọn không tồn tại.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var student = await _context.Students.FindAsync(id);
                    if (student is null) return NotFound();
                    student.FullName = std.FullName;
                    student.Address = std.Address;
                    student.Email = std.Email;
                    student.FacultyId = std.FacultyId;
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Students.Any(e => e.StudentId == std.StudentId)) return NotFound();
                    else throw;
                }
                if (IsAjaxRequest())
                {
                    return Json(new { success = true, message = $"Đã cập nhật sinh viên {std.FullName}." });
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", std.FacultyId);
            return IsAjaxRequest() ? InvalidPartial("_Form", std) : View(std);
        }

        // ==========================================
        // 4. DELETE: Giao diện Xác nhận xóa
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var student = await _context.Students
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(m => m.StudentId == id);

            if (student == null) return NotFound();

            return IsAjaxRequest() ? PartialView("_Delete", student) : View(student);
        }

        // ==========================================
        // 4. DELETE: Thực thi Xóa
        // ==========================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }
            if (IsAjaxRequest())
            {
                return Json(new { success = true, message = "Đã xóa sinh viên." });
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<IActionResult> CreateInvalidAsync(Student student)
        {
            ViewData["FacultyId"] = new SelectList(
                await _context.Faculties.ToListAsync(), "FacultyId", "FacultyName", student.FacultyId);
            return IsAjaxRequest() ? InvalidPartial("_Form", student) : View(student);
        }

        private IActionResult InvalidPartial(string viewName, Student student)
        {
            Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            return PartialView(viewName, student);
        }

        private bool IsAjaxRequest() =>
            string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
    }
}