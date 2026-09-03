using Microsoft.AspNetCore.Mvc;

namespace DemoMVC.Controllers
{
    public class Lesson4Controller : Controller
    {
       
        public IActionResult Index()
        {
            ViewData["Title"] = "Nội dung buổi học";
            ViewBag.Date = "03/09/2026";
            
            return View();
        }
        public IActionResult DemoViewBag()
        {
            ViewData["Message"] = "Dữ liệu từ ViewData";
            ViewBag.Notification = "Dữ liệu từ ViewBag";
            return View();
        }
        // In Controllers/Lesson1Controller.cs
        [HttpPost]
        public IActionResult Search(string keyword)
        {
            ViewBag.Result = $"Từ khóa đã tìm: {keyword}";
            return View();
        }
    }
}