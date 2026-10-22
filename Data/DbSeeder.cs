using DemoMVC.Models;

namespace DemoMVC.Data
{
    public static class DbSeeder
    {
        public static void SeedFaculties(ApplicationDbContext context)
        {
            if (!context.Faculties.Any())
            {
                var faculties = new List<Faculty>
                {
                    new Faculty { FacultyId = "CNTT", FacultyName = "Công nghệ Thông tin", MajorCode = "01" },
                    new Faculty { FacultyId = "DDK",  FacultyName = "Dầu khí & Năng lượng", MajorCode = "02" },
                    new Faculty { FacultyId = "M",    FacultyName = "Mỏ", MajorCode = "03" },
                    new Faculty { FacultyId = "TD",   FacultyName = "Trắc địa - Bản đồ & Quản lý đất đai", MajorCode = "04" },
                    new Faculty { FacultyId = "KT",   FacultyName = "Kinh tế & Quản trị kinh doanh", MajorCode = "05" },
                    new Faculty { FacultyId = "MT",   FacultyName = "Môi trường", MajorCode = "06" },
                    new Faculty { FacultyId = "XD",   FacultyName = "Xây dựng", MajorCode = "07" }
                };

                context.Faculties.AddRange(faculties);
                context.SaveChanges();
            }
        }
    }
}