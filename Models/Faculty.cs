using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models
{
    public class Faculty
    {
        [Key]
        [Display(Name = "Mã Khoa")]
        [Required(ErrorMessage = "Mã khoa không được để trống")]
        public string FacultyId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tên khoa không được để trống")]
        [Display(Name = "Tên Khoa")]
        public string FacultyName { get; set; } = string.Empty;

        // THÊM THUỘC TÍNH NÀY: Mã số đại diện cho Khoa/Ngành (2 chữ số)
        [Display(Name = "Mã số Ngành/Khoa")]
        public string MajorCode { get; set; } = "01";

        public ICollection<Student>? Students { get; set; }
    }
}