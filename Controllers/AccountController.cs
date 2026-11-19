using System.Text;
using DemoMVC.Models;
using DemoMVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace DemoMVC.Controllers;

public sealed class AccountController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IAppEmailSender emailSender,
    ILogger<AccountController> logger) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) =>
        View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            return Url.IsLocalUrl(model.ReturnUrl)
                ? LocalRedirect(model.ReturnUrl!)
                : RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty,
            result.IsLockedOut ? "Tài khoản tạm khóa do đăng nhập thất bại quá nhiều lần." :
            result.IsNotAllowed ? "Vui lòng xác thực email trước khi đăng nhập." :
            "Email hoặc mật khẩu không chính xác.");
        return View(model);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        var roleResult = await userManager.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
        {
            logger.LogError("Could not assign the baseline User role to a newly registered account.");
            AddIdentityErrors(roleResult);
            return View(model);
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = Url.Action(nameof(ConfirmEmail), "Account",
            new { userId = user.Id, token = encodedToken }, Request.Scheme);
        var sent = await emailSender.SendAsync(user.Email!, "Xác thực tài khoản DemoMVC",
            $"Mở liên kết sau để xác thực tài khoản: {link}");

        TempData["AccountMessage"] = sent
            ? "Tài khoản đã được tạo. Hãy kiểm tra email để xác thực trước khi đăng nhập."
            : "Tài khoản đã được tạo nhưng email chưa gửi được. Quản trị viên cần kiểm tra cấu hình SMTP.";
        if (!sent)
        {
            logger.LogWarning("Confirmation email could not be sent for a newly registered account.");
        }
        return RedirectToAction(nameof(Message));
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return View("Message", "Liên kết xác thực không hợp lệ.");
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return View("Message", "Không thể xác thực tài khoản.");
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            var result = await userManager.ConfirmEmailAsync(user, decoded);
            return View("Message", result.Succeeded
                ? "Email đã được xác thực. Bạn có thể đăng nhập."
                : "Liên kết xác thực không hợp lệ hoặc đã hết hạn.");
        }
        catch (FormatException)
        {
            return View("Message", "Liên kết xác thực không hợp lệ.");
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByEmailAsync(model.Email);
        var sent = true;
        if (user is not null && await userManager.IsEmailConfirmedAsync(user))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var link = Url.Action(nameof(ResetPassword), "Account",
                new { userId = user.Id, token = encodedToken }, Request.Scheme);
            sent = await emailSender.SendAsync(user.Email!, "Đặt lại mật khẩu DemoMVC",
                $"Mở liên kết sau để đặt lại mật khẩu: {link}");
        }

        if (!sent)
        {
            logger.LogWarning("Password reset email could not be sent; SMTP configuration or delivery should be checked.");
        }
        TempData["AccountMessage"] = "Nếu tài khoản tồn tại và đã xác thực email, hướng dẫn đặt lại mật khẩu sẽ được gửi.";

        return RedirectToAction(nameof(Message));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ResetPassword(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return View("Message", "Liên kết đặt lại mật khẩu không hợp lệ.");
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            return View(new ResetPasswordViewModel { UserId = userId, Token = decoded });
        }
        catch (FormatException)
        {
            return View("Message", "Liên kết đặt lại mật khẩu không hợp lệ.");
        }
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await userManager.FindByIdAsync(model.UserId);
        if (user is null || !string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Không thể đặt lại mật khẩu cho tài khoản này.");
            return View(model);
        }

        var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        TempData["AccountMessage"] = "Mật khẩu đã được cập nhật.";
        return RedirectToAction(nameof(Message));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Message() => View("Message", TempData["AccountMessage"]?.ToString() ?? "Yêu cầu đã được xử lý.");

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
