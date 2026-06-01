using Microsoft.AspNetCore.Authentication.Cookies;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Backend WCF wrapper. Singleton — one channel for the whole app.
builder.Services.AddSingleton<CheckersBackend>();

// Cookie-based auth: Login page sets the cookie after a successful
// admin login, and the [Authorize] attribute on /Admin pages enforces
// it. The cookie holds the admin's WCF session token in a claim so the
// pages can call admin-only operations.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath  = "/Login";
        options.LogoutPath = "/Logout";
        options.AccessDeniedPath = "/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p =>
        p.RequireAuthenticatedUser().RequireRole("Admin"));
});

builder.Services.AddRazorPages(options =>
{
    // Every page under /Admin requires the admin policy.
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnly");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
