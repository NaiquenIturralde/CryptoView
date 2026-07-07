using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using CryptoView.Data;
using CryptoView.Services;

var builder = WebApplication.CreateBuilder(args);

// =========================================
// CONFIGURACIÓN DE SERVICIOS
// =========================================

// 1. Servicios de Blazor Server
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// 2. Configuración de la base de datos
// Usar SQL Server LocalDB (predeterminado en desarrollo)
builder.Services.AddDbContext<CryptoDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server=(localdb)\\mssqllocaldb;Database=CryptoViewDb;Trusted_Connection=True;MultipleActiveResultSets=true",
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// 3. Configuración de controladores API
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    });

// 4. Configuración de HttpClient para CryptoApiService (Coinlore API - 100% GRATIS)
builder.Services.AddHttpClient<CryptoApiService>(client =>
{
    client.BaseAddress = new Uri("https://api.coinlore.net/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    return new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
});

// 5. Registrar servicios personalizados
// Nota: CryptoApiService ya está registrado por AddHttpClient<CryptoApiService> como Transient
// con su HttpClient configurado. No se registra AddScoped adicional para evitar conflictos.

// Servicio de notificaciones — Scoped = una instancia por circuito Blazor (por usuario)
builder.Services.AddScoped<NotificationService>();

// Servicio de autenticación
builder.Services.AddScoped<CryptoView.Services.AuthService>();

// Servicio de estado de autenticación (validación centralizada)
builder.Services.AddScoped<CryptoView.Services.AuthStateService>();

// Servicio de perfil de usuario
builder.Services.AddScoped<CryptoView.Services.ProfileService>();

// 6. Configuración de Swagger/OpenAPI para documentación de API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CryptoView API",
        Version = "v1",
        Description = "API para gestionar criptomonedas y notas de usuario - Trabajo Final Integrador",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Tecnicatura en Desarrollo de Software",
            Email = "contacto@cryptoview.edu"
        }
    });
});

// 7. Configuración CORS (si es necesario para consumir API desde otros orígenes)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 8. Logging mejorado
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

// =========================================
// INICIALIZACIÓN DE BASE DE DATOS
// =========================================

// Crear la base de datos y aplicar migraciones automáticamente
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<CryptoDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        // Create DB + tables if they don’t exist yet
        context.Database.EnsureCreated();

        // Create AppUsers table if the DB already existed before this table was added
        context.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (
                SELECT 1 FROM sys.objects
                WHERE object_id = OBJECT_ID(N'[dbo].[AppUsers]') AND type = 'U'
            )
            BEGIN
                CREATE TABLE [dbo].[AppUsers] (
                    [Id]           INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
                    [Username]     NVARCHAR(100)  NOT NULL,
                    [PasswordHash] NVARCHAR(512)  NOT NULL,
                    [Email]        NVARCHAR(256)  NULL,
                    [Role]         NVARCHAR(50)   NOT NULL DEFAULT 'Usuario',
                    [CreatedAt]    DATETIME2      NOT NULL DEFAULT GETUTCDATE(),
                    CONSTRAINT UQ_AppUsers_Username UNIQUE ([Username])
                )
            END");

        // ── Admin seed / recovery ────────────────────────────────────────────────
        var adminCfg = app.Configuration.GetSection("AdminSeed");
        var adminUser = adminCfg["Username"] ?? "admin";
        var adminPassEnv = Environment.GetEnvironmentVariable("CRYPTOVIEW_ADMIN_PASSWORD");

        var authService = services.GetRequiredService<CryptoView.Services.AuthService>();

        // Check if admin already exists in DB
        var adminExists = await context.AppUsers.AnyAsync(u => u.Role == "Admin");

        if (!string.IsNullOrEmpty(adminPassEnv))
        {
            // Case 1: Variable is present -> Force update or create
            logger.LogInformation("CRYPTOVIEW_ADMIN_PASSWORD detected. Updating Admin credentials...");
            await authService.EnsureAdminAsync(adminUser, adminPassEnv, forceReset: true);
        }
        else if (adminExists)
        {
            // Case 2: Variable missing, but Admin exists in DB -> Do nothing (safe)
            logger.LogInformation("Admin user found in DB. No password update required.");
        }
        else
        {
            // Case 3: Variable missing AND no Admin exists -> Critical Error
            throw new InvalidOperationException(
                "FATAL: No Admin user found and CRYPTOVIEW_ADMIN_PASSWORD is not set. " +
                "Please set the environment variable to create the initial Admin account.");
        }

        logger.LogInformation("Base de datos inicializada correctamente");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar la base de datos");
    }
}

// =========================================
// CONFIGURACIÓN DEL PIPELINE HTTP
// =========================================

// Configuración del pipeline para Development
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CryptoView API V1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    // Configuración para Production
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Middleware de seguridad y enrutamiento
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Activar CORS (si fue configurado)
app.UseCors("AllowAll");

// Autenticación y autorización (para futuras extensiones)
// app.UseAuthentication();
// app.UseAuthorization();

// =========================================
// CONFIGURACIÓN DE ENDPOINTS
// =========================================

app.MapControllers(); // Rutas de API (CryptoController, NotesController)
app.MapBlazorHub();   // Hub de SignalR para Blazor Server
app.MapFallbackToPage("/_Host"); // Página host para Blazor

// =========================================
// INICIAR APLICACIÓN
// =========================================

app.Logger.LogInformation("==============================================");
app.Logger.LogInformation("  CryptoView Dashboard - Iniciando...");
app.Logger.LogInformation("  Blazor Server + .NET + Entity Framework Core");
app.Logger.LogInformation("  Trabajo Final Integrador - 2024");
app.Logger.LogInformation("==============================================");

app.Run();
