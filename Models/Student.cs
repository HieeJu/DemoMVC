using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [Required(ErrorMessage = " Địa chỉ không được để trống")]
        [Display(Name = " Địa chỉ ")]
        public string? Address { get; set; }

        [Required(ErrorMessage = " Tên đại học không được để trống ")]
        [Display(Name = " Trường Đại Học ")]
        public string? University { get; set; }

        [EmailAddress(ErrorMessage = " Email không đúng định dạng ")]
        [Display(Name = " Địa chỉ Email ")]
        public string? Email { get; set; }
    }
}