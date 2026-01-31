using System.Text;
using Dotmim.Sync;
using Dotmim.Sync.Enumerations;
using Dotmim.Sync.SqlServer;
using Dotmim.Sync.Web.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PDV.Data.Cloud.Context;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<CloudDbContext>(options =>
    options.UseSqlServer(connectionString));

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Dotmim.Sync
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// Configure Sync tables with direction
var syncSetup = new SyncSetup("Products", "Sales", "SaleItems", "Payments", "Operators");

// Products and Operators: Download only (Server → Client)
// Master data comes from server
syncSetup.Tables["Products"].SyncDirection = SyncDirection.DownloadOnly;
syncSetup.Tables["Operators"].SyncDirection = SyncDirection.DownloadOnly;

// Sales, SaleItems, Payments: Bidirectional (Client ↔ Server)
// Sales created locally, synced to server
syncSetup.Tables["Sales"].SyncDirection = SyncDirection.Bidirectional;
syncSetup.Tables["SaleItems"].SyncDirection = SyncDirection.Bidirectional;
syncSetup.Tables["Payments"].SyncDirection = SyncDirection.Bidirectional;

var syncOptions = new SyncOptions
{
    BatchSize = 1000,
    DisableConstraintsOnApplyChanges = true
};

builder.Services.AddSyncServer<SqlSyncProvider>(connectionString, syncSetup, syncOptions);

// CORS for Desktop client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDesktop", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCors("AllowDesktop");
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.MapControllers();

// Health check endpoint
app.MapGet("/", () => Results.Ok(new { Status = "Healthy", Timestamp = DateTime.UtcNow }));

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
    await context.Database.MigrateAsync();
    Log.Information("Database migration completed");

    // Seed default operator if none exists
    if (!context.Operators.Any())
    {
        var defaultOperator = new PDV.Core.Entities.Operator("Admin", "ADMIN", "1234", isAdmin: true);
        context.Operators.Add(defaultOperator);
        await context.SaveChangesAsync();
        Log.Information("Default operator created: ADMIN / 1234");
    }

    // Seed sample products if none exists
    if (!context.Products.Any())
    {
        var products = new[]
        {
            new PDV.Core.Entities.Product("7891234567890", "Coca-Cola 350ml", 5.50m),
            new PDV.Core.Entities.Product("7891234567891", "Pepsi 350ml", 5.00m),
            new PDV.Core.Entities.Product("7891234567892", "Agua Mineral 500ml", 3.00m),
            new PDV.Core.Entities.Product("7891234567893", "Salgadinho Doritos", 8.90m),
            new PDV.Core.Entities.Product("7891234567894", "Chocolate Bis", 4.50m),
        };
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
        Log.Information("Sample products created: {Count} products", products.Length);
    }
}

app.Run();
