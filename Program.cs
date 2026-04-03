using Crazy8Web.Data;
using Crazy8Web.Data.Entities;
using Crazy8Web.Hubs;
using Crazy8Web.Services;
using MatBlazor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddHubOptions(options => { options.MaximumReceiveMessageSize = 500 * 1024; });

string? connectionString = builder.Configuration.GetConnectionString("Default") ?? 
    throw new InvalidOperationException("Connection string 'Default' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddSingleton<GameService>();
builder.Services.AddSignalR();
builder.Services.AddBlazorBootstrap();
builder.Services.AddMatBlazor();
builder.Services.AddScoped(sp =>
{
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
});
builder.Services.AddMatToaster(config =>
{
    config.Position = MatToastPosition.BottomRight;
    config.PreventDuplicates = true;
    config.NewestOnTop = true;
    config.ShowCloseButton = true;
    config.MaximumOpacity = 95;
    config.VisibleStateDuration = 3000;
});
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(["application/octet-stream"]);
});

WebApplication app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see
    // https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/auth/register", async (
    [FromForm] string username,
    [FromForm] string email,
    [FromForm] string password,
    [FromForm] string confirmPassword,
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager) =>
{
    Console.WriteLine("Registering....");
    string escapedUsername = Uri.EscapeDataString(username ?? string.Empty);
    string escapedEmail = Uri.EscapeDataString(email ?? string.Empty);

    if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email) || string.IsNullOrWhiteSpace(password) || password.Length < 6 || string.IsNullOrWhiteSpace(confirmPassword) || confirmPassword.Length < 6)
    {
        return Results.Redirect($"/?registerError=2&registerUsername={escapedUsername}&registerEmail={escapedEmail}");
    }

    if (!string.Equals(password, confirmPassword, StringComparison.Ordinal))
    {
        return Results.Redirect($"/?registerError=1&registerUsername={escapedUsername}&registerEmail={escapedEmail}");
    }

    var user = new IdentityUser { UserName = username, Email = email };
    var result = await userManager.CreateAsync(user, password);
    if (!result.Succeeded)
    {
        return Results.Redirect($"/?registerError=3&registerUsername={escapedUsername}&registerEmail={escapedEmail}");
    }

    await signInManager.SignInAsync(user, isPersistent: false);
    return Results.Redirect("/");
})
.DisableAntiforgery();

app.MapPost("/auth/login", async (
    [FromForm] string email,
    [FromForm] string password,
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager) =>
{
    if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email) || string.IsNullOrWhiteSpace(password))
    {
        return Results.Redirect("/?loginError=2");
    }

    IdentityUser? user = await userManager.FindByEmailAsync(email);
    if (user is null)
    {
        return Results.Redirect("/?loginError=1");
    }

    var result = await signInManager.PasswordSignInAsync(user.UserName!, password, false, false);
    if (!result.Succeeded) return Results.Redirect("/?loginError=1");
    return Results.Redirect("/");
})
.DisableAntiforgery();

app.MapPost("/auth/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
}).DisableAntiforgery();

app.MapBlazorHub();
app.MapHub<GameHub>("/gameHub");
app.MapFallbackToPage("/_Host");

app.Run();