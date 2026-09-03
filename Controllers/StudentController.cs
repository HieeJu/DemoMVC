using Microsoft.AspNetCore.Mvc;
using DemoMVC.Models;

namespace DemoMVC.Controllers
{
    public class StudentController : Controller
    {
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        
        [HttpPost]
        public IActionResult Create(Student std)
        {
            if (!string.IsNullOrEmpty(std.FullName))
            {
                ViewData["Data"] = $"Xin chào {std.FullName} - Địa chỉ: {std.Address} - Trường: {std.University}";
            }

            return View(std);
        }
    }
}