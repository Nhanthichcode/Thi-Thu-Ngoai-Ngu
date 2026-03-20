using ExamSystem.Core.Entities;
using Microsoft.AspNetCore.Identity;

public class CheckLockoutMiddleware
{
    private readonly RequestDelegate _next;

    public CheckLockoutMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        var path = context.Request.Path.Value.ToLower();

        // CHẶN NGAY: Nếu đang ở trang Lockout hoặc Login thì cho đi qua luôn, không kiểm tra nữa
        if (path.Contains("/account/lockout") || path.Contains("/account/login"))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity.IsAuthenticated)
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user != null && await userManager.IsLockedOutAsync(user))
            {
                await signInManager.SignOutAsync();
                // Sử dụng đường dẫn tuyệt đối hoặc kiểm tra HTTPS
                context.Response.Redirect("/Account/Lockout");
                return;
            }
        }
        await _next(context);
    }
}