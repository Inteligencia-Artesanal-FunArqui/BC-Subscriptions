using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using MySql.EntityFrameworkCore.Extensions;
using OsitoPolar.Subscriptions.Service.Domain.Repositories;
using OsitoPolar.Subscriptions.Service.Domain.Services;
using OsitoPolar.Subscriptions.Service.Application.Internal.CommandServices;
using OsitoPolar.Subscriptions.Service.Application.Internal.QueryServices;
using OsitoPolar.Subscriptions.Service.Infrastructure.Persistence.EFC.Configuration;
using OsitoPolar.Subscriptions.Service.Infrastructure.Persistence.EFC.Repositories;
using OsitoPolar.Subscriptions.Service.Infrastructure.External.Stripe;
using OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;
using OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Interfaces.ASP.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllPolicy",
        policy => policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<SubscriptionsDbContext>(options =>
{
    if (connectionString != null)
    {
        options.UseMySQL(connectionString)
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
    }
});

// ⚠️ CRÍTICO: Register DbContext as base class for UnitOfWork and BaseRepository
// Sin esto, obtendrás error: "Unable to resolve service for type 'Microsoft.EntityFrameworkCore.DbContext'"
builder.Services.AddScoped<DbContext>(provider => provider.GetRequiredService<SubscriptionsDbContext>());

// Dependency Injection - Repositories
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
builder.Services.AddScoped<IServicePaymentRepository, ServicePaymentRepository>();

// Dependency Injection - UnitOfWork
builder.Services.AddScoped<OsitoPolar.Subscriptions.Service.Shared.Domain.Repositories.IUnitOfWork, OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Persistence.EFC.Repositories.UnitOfWork>();

// Dependency Injection - Services
// ⚠️ COMENTADO TEMPORALMENTE: PaymentCommandService necesita IStripeService que no está implementado
// builder.Services.AddScoped<IPaymentCommandService, PaymentCommandService>();
builder.Services.AddScoped<ISubscriptionCommandService, SubscriptionCommandService>();
builder.Services.AddScoped<ISubscriptionQueryService, SubscriptionQueryService>();

// Stripe Configuration
builder.Services.Configure<StripeConfiguration>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddScoped<IPaymentProvider, StripePaymentProvider>();

// Controllers
builder.Services.AddControllers(options =>
{
    options.Conventions.Add(new KebabCaseRouteNamingConvention());
});

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OsitoPolar Subscriptions & Payment Service API",
        Version = "v1",
        Description = "Subscriptions & Payment Microservice - Plans, Payments & Service Payments"
    });
    options.EnableAnnotations();
});

var app = builder.Build();

// Verify database connection on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<SubscriptionsDbContext>();
    try
    {
        context.Database.CanConnect();
        Console.WriteLine("✅ Database connection successful");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database connection failed: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAllPolicy");

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("🚀 Subscriptions & Payment Service running on port 5006");

app.Run();
