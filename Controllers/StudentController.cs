using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;
using DemoMVC.Models;

namespace DemoMVC.Controllers
{
    public class StudentController : Controller
    {
        private readonly ApplicationDbContext _context;
        public StudentController(ApplicationDbContext context)
            {
                _context = context;
            }
            public async Task<IActionResult> Index()
            {
                var students = await _context.Students.ToListAsync();
                return View(students);
            }
            [HttpGet]
            public IActionResult Create()
            {
                return View();
            }
            
            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Create(Student std)
            {
                if (ModelState.IsValid)
                {
                    _context.Add(std);
                    await _context.SaveChangesAsync();

                    TempData["Data"] = $"Xin chào {std.FullName} - Địa chỉ: {std.Address} - Trường: {std.University}";

                    return RedirectToAction(nameof(Index));
                }

                return View(std);
            }

            [HttpGet]
            public async Task<IActionResult> Edit(int? id)
            {
                if (id == null) return NotFound();

                var student = await _context.Students.FindAsync(id);
                if (student == null) return NotFound();

                return View(student);
            }

            [HttpPost]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> Edit(int id, Student std)
            {
                if (id != std.Id) return NotFound();

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(std);
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!_context.Students.Any(e => e.Id == std.Id)) return NotFound();
                        else throw;
                    }
                    return RedirectToAction(nameof(Index));
                }
                return View(std);
            }
            
            [HttpGet]
            public async Task<IActionResult> Delete(int? id)
            {
                if (id == null) return NotFound();

                var student = await _context.Students.FirstOrDefaultAsync(m => m.Id == id);
                if (student == null) return NotFound();

                return View(student);
            }

            [HttpPost, ActionName("Delete")]
            [ValidateAntiForgeryToken]
            public async Task<IActionResult> DeleteConfirmed( int id)
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