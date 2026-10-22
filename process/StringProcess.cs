namespace DemoMVC.Process
{
    public class StringProcess
    {
        // Hàm sinh mã sinh viên kế tiếp dựa trên Prefix (Mã cố định) và Mã lớn nhất hiện tại
        public static string GenerateStudentId(string prefix, string maxStudentId)
        {
            if (string.IsNullOrEmpty(maxStudentId))
            {
                // Nếu chưa có sinh viên nào trong hệ thống/ngành này -> Bắt đầu từ 0001
                return prefix + "0001";
            }

            // Lấy 4 số cuối (STT)
            string lastFourDigits = maxStudentId.Substring(maxStudentId.Length - 4);
            int currentNum = int.Parse(lastFourDigits);
            int nextNum = currentNum + 1;

            // Định dạng lại thành 4 chữ số (VD: 199 -> 0200)
            return prefix + nextNum.ToString("D4");
        }
    }
}