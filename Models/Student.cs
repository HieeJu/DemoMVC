using System.ComponentModel.DataAnnotations;

namespace DemoMVC.Models
{
    public class Student
    {
        [Key]
        public int Id { get; set; }

        public string? FullName { get; set; }

        public string? Address { get; set; }

        public string? University { get; set; }

        public string? Email { get; set; }
    }
}