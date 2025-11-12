using System;
using EVWarrantyManagement.BLL.Interfaces;
using EVWarrantyManagement.BLL.Services;
using EVWarrantyManagement.Configuration;
using EVWarrantyManagement.DAL;
using EVWarrantyManagement.DAL.Interfaces;
using EVWarrantyManagement.DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EVWarrantyManagement.Services;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add SignalR
builder.Services.AddSignalR();

// Add Session support for AI suggestions
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// DbContext
builder.Services.AddDbContext<EVWarrantyManagementContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnectionString")));

// DAL repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVehicleRepository, VehicleRepository>();
builder.Services.AddScoped<IPartRepository, PartRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IWarrantyClaimRepository, WarrantyClaimRepository>();

// BLL services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IPartService, PartService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IWarrantyClaimService, WarrantyClaimService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IInvoicePdfBuilder, QuestPdfInvoiceBuilder>();

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.Configure<N8nSettings>(builder.Configuration.GetSection("N8n"));
builder.Services.AddHttpClient("n8n", (serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<IOptions<N8nSettings>>().Value;
    if (!string.IsNullOrWhiteSpace(settings.BaseUrl) &&
        Uri.TryCreate(settings.BaseUrl, UriKind.Absolute, out var baseUri))
    {
        client.BaseAddress = baseUri;
    }

    if (!string.IsNullOrWhiteSpace(settings.ApiKey))
    {
        if (!client.DefaultRequestHeaders.Contains("X-N8N-API-KEY"))
        {
            client.DefaultRequestHeaders.Add("X-N8N-API-KEY", settings.ApiKey);
        }
    }
});

// Cookie authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    // Backward-compatible: accept legacy role names ('SC', 'EVM') besides new labels
    options.AddPolicy("RequireSCStaff", policy => policy.RequireRole("SC Staff", "SC", "Admin"));
    options.AddPolicy("RequireTechnician", policy => policy.RequireRole("SC Technician", "SC", "Admin"));
    options.AddPolicy("RequireEVM", policy => policy.RequireRole("EVM Staff", "EVM", "Admin"));
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Redirect root based on auth/role
app.MapGet("/", async context =>
{
    var user = context.User;
    if (user.Identity?.IsAuthenticated == true)
    {
        if (user.IsInRole("Admin") || user.IsInRole("EVM Staff") || user.IsInRole("EVM"))
        {
            context.Response.Redirect("/Dashboard");
        }
        else if (user.IsInRole("SC Staff") || user.IsInRole("SC"))
        {
            context.Response.Redirect("/Claims");
        }
        else if (user.IsInRole("SC Technician") || user.IsInRole("SC"))
        {
            context.Response.Redirect("/Claims/Index");
        }
        else
        {
            context.Response.Redirect("/Claims");
        }
    }
    else
    {
        context.Response.Redirect("/Account/Login");
    }
});

app.MapStaticAssets();

// Map SignalR hubs
app.MapHub<EVWarrantyManagement.Hubs.NotificationHub>("/hubs/notification");
app.MapHub<EVWarrantyManagement.Hubs.ChatHub>("/hubs/chat");

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
