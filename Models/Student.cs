using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoMVC.Models
{
    public class Student
    {
        [Key]
        [Display(Name = "Mã sinh viên")]
        public string StudentId { get; set; } = string.Empty; // Mã tự động sinh ra

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [Display(Name = "Địa chỉ Email")]
        public string? Email { get; set; }

        // --- KHÓA NGOẠI KHOA ---
        [Required(ErrorMessage = "Vui lòng chọn Khoa")]
        [Display(Name = "Khoa")]
        public string? FacultyId { get; set; }

        [ForeignKey("FacultyId")]
        public Faculty? Faculty { get; set; }
    }
}