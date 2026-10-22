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
                .Include(s => s.Faculty) // JOIN bảng Faculty để lấy tên Khoa[cite: 3]
                .ToListAsync();

            return View(students);
        }

        // ==========================================
        // 2. CREATE: Giao diện Thêm mới
        // ==========================================
        [HttpGet]
        public IActionResult Create()
        {
            // Nạp danh sách Khoa của HUMG vào SelectList cho thẻ <select>[cite: 3]
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName");
            return View();
        }

        // ==========================================
        // 2. CREATE: Xử lý Thêm mới (Sinh mã tự động theo Khoa)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Address,Email,FacultyId")] Student std, string yearPrefix = "28", string systemPrefix = "21")
        {
            // Tìm thông tin Khoa mà người dùng đã chọn ở Form[cite: 3]
            var faculty = await _context.Faculties.FindAsync(std.FacultyId);

            // Lấy mã số Ngành/Khoa (MajorCode) tương ứng (Mặc định "01" nếu không tìm thấy)[cite: 3]
            string majorPrefix = faculty?.MajorCode ?? "01";

            // Tạo Tiền tố: [Năm 2 chữ số] + [Hệ 2 chữ số] + [Mã Ngành 2 chữ số] (VD: 28 + 21 + 05 -> "282105")[cite: 3]
            string prefix = $"{yearPrefix}{systemPrefix}{majorPrefix}";

            // Lấy Mã sinh viên lớn nhất hiện tại thuộc tiền tố này[cite: 3]
            var maxStudentId = await _context.Students
                .Where(s => s.StudentId.StartsWith(prefix))
                .OrderByDescending(s => s.StudentId)
                .Select(s => s.StudentId)
                .FirstOrDefaultAsync();

            // Sinh mã sinh viên tự động kế tiếp (VD: 2821050001)
            std.StudentId = StringProcess.GenerateStudentId(prefix, maxStudentId);

            // Bỏ qua kiểm tra Validate cho StudentId vì được sinh tự động
            ModelState.Remove("StudentId");

            if (ModelState.IsValid)
            {
                _context.Add(std);
                await _context.SaveChangesAsync();
                TempData["Message"] = $"Thêm mới thành công sinh viên {std.FullName} - Mã SV: {std.StudentId}";
                return RedirectToAction(nameof(Index));
            }

            // Nếu dữ liệu không hợp lệ, nạp lại danh sách Khoa cho Dropdown
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", std.FacultyId);
            return View(std);
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

            // Chọn sẵn Khoa hiện tại của sinh viên trong Dropdown[cite: 3]
            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", student.FacultyId);
            return View(student);
        }

        // ==========================================
        // 3. UPDATE: Xử lý Lưu chỉnh sửa
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("StudentId,FullName,Address,Email,FacultyId")] Student std)
        {
            if (id != std.StudentId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(std);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Students.Any(e => e.StudentId == std.StudentId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewData["FacultyId"] = new SelectList(_context.Faculties, "FacultyId", "FacultyName", std.FacultyId);
            return View(std);
        }

        // ==========================================
        // 4. DELETE: Giao diện Xác nhận xóa
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // Load dữ liệu sinh viên kèm tên Khoa để hiển thị trang xác nhận xóa
            var student = await _context.Students
                .Include(s => s.Faculty)
                .FirstOrDefaultAsync(m => m.StudentId == id);

            if (student == null) return NotFound();

            return View(student);
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
            return RedirectToAction(nameof(Index));
        }
    }
}