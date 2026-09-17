using Microsoft.EntityFrameworkCore;
using DemoMVC.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Thêm các dịch vụ Controller và View
builder.Services.AddControllersWithViews();

// 2. Cấu hình DbContext kết nối tới SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// 3. Cấu hình HTTP Request Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();