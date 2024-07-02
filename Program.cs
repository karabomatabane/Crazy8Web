using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Crazy8Web.Data;
using Crazy8Web.Hubs;
using Crazy8Web.Services;
using MatBlazor;
using Microsoft.AspNetCore.ResponseCompression;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddHubOptions(options => { options.MaximumReceiveMessageSize = 500 * 1024; });
builder.Services.AddSingleton<GameService>();
builder.Services.AddSignalR();
builder.Services.AddMatBlazor();
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
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" });
});

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapHub<GameHub>("/gameHub");
app.MapFallbackToPage("/_Host");

app.Run();