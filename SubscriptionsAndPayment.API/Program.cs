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
using OsitoPolar.Subscriptions.Service.Infrastructure.External.Http;
using OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;
using OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Interfaces.ASP.Configuration;
using OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Tokens.JWT.Configuration;
using OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Tokens.JWT.Services;
using OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Pipeline.Middleware.Extensions;
using MassTransit;

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

// Dependency Injection - ACL Facades
// ✅ REACTIVADO: Facade para que otros servicios obtengan datos de suscripciones
builder.Services.AddScoped<OsitoPolar.Subscriptions.Service.Interfaces.ACL.ISubscriptionContextFacade,
    OsitoPolar.Subscriptions.Service.Application.ACL.SubscriptionContextFacade>();

// Dependency Injection - UnitOfWork
builder.Services.AddScoped<OsitoPolar.Subscriptions.Service.Shared.Domain.Repositories.IUnitOfWork, OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Persistence.EFC.Repositories.UnitOfWork>();

// Dependency Injection - Services
// ⚠️ COMENTADO TEMPORALMENTE: PaymentCommandService necesita IStripeService que no está implementado
// builder.Services.AddScoped<IPaymentCommandService, PaymentCommandService>();
builder.Services.AddScoped<ISubscriptionCommandService, SubscriptionCommandService>();
builder.Services.AddScoped<ISubscriptionQueryService, SubscriptionQueryService>();

// HTTP Clients for Cross-Service Communication
var profilesServiceUrl = builder.Configuration["ServiceUrls:Profiles"] ?? "http://profiles-service:8080";
builder.Services.AddHttpClient<IProfilesHttpFacade, ProfilesHttpFacade>(client =>
{
    client.BaseAddress = new Uri(profilesServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var workOrdersServiceUrl = builder.Configuration["ServiceUrls:WorkOrders"] ?? "http://workorders-service:8080";
builder.Services.AddHttpClient<IWorkOrdersHttpFacade, WorkOrdersHttpFacade>(client =>
{
    client.BaseAddress = new Uri(workOrdersServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var serviceRequestsServiceUrl = builder.Configuration["ServiceUrls:ServiceRequests"] ?? "http://servicerequests-service:8080";
builder.Services.AddHttpClient<IServiceRequestsHttpFacade, ServiceRequestsHttpFacade>(client =>
{
    client.BaseAddress = new Uri(serviceRequestsServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var notificationsServiceUrl = builder.Configuration["ServiceUrls:Notifications"] ?? "http://notifications-service:8080";
builder.Services.AddHttpClient<INotificationsHttpFacade, NotificationsHttpFacade>(client =>
{
    client.BaseAddress = new Uri(notificationsServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Stripe Configuration
builder.Services.Configure<StripeConfiguration>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddScoped<IPaymentProvider, StripePaymentProvider>();

// JWT Token Configuration - Must use same secret as IAM Service
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));
builder.Services.AddScoped<ITokenService, TokenService>();

// ===========================
// MassTransit + RabbitMQ Configuration
// ===========================
builder.Services.AddMassTransit(x =>
{
    // Configure RabbitMQ
    x.UsingRabbitMq((context, cfg) =>
    {
        var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitMqPort = builder.Configuration["RabbitMQ:Port"] ?? "5672";
        var rabbitMqUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
        var rabbitMqPass = builder.Configuration["RabbitMQ:Password"] ?? "guest";

        cfg.Host($"rabbitmq://{rabbitMqHost}:{rabbitMqPort}", h =>
        {
            h.Username(rabbitMqUser);
            h.Password(rabbitMqPass);
        });

        // Configure message retry policy
        cfg.UseMessageRetry(r => r.Incremental(3, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)));

        // Auto-configure all consumers
        cfg.ConfigureEndpoints(context);
    });
});

Console.WriteLine("✅ MassTransit + RabbitMQ configured for Subscriptions Service");

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

// Verify database connection, ensure schema, and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<SubscriptionsDbContext>();
    try
    {
        context.Database.EnsureCreated();
        Console.WriteLine("✅ Database connection successful and schema ensured");

        // Seed initial data (subscription plans)
        await OsitoPolar.Subscriptions.Service.Shared.Infrastructure.Persistence.EFC.Configuration.DatabaseSeeder.SeedDatabase(context);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Database initialization failed: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAllPolicy");

// JWT Authorization Middleware - validates tokens and sets HttpContext.Items["User"]
app.UseRequestAuthorization();

app.UseAuthorization();

app.MapControllers();

Console.WriteLine("🚀 Subscriptions & Payment Service running on port 5006");

app.Run();
