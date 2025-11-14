using System.Text;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using CCD.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MercadoPago.Config;
using DotNetEnv;

// Cargar .env automáticamente (busca hacia arriba en el árbol de directorios)
// Busca desde el directorio actual (src/CCD.Api) hasta la raíz del proyecto
Env.TraversePath().Load();
Console.WriteLine("Variables de entorno cargadas desde .env");

// Convertir variables del formato .env al formato ASP.NET Core (con doble guion bajo)
var envVarMappings = new Dictionary<string, string>
{
    ["MERCADOPAGO_ACCESS_TOKEN"] = "MercadoPago__AccessToken",
    ["MERCADOPAGO_WEBHOOK_SECRET"] = "MercadoPago__WebhookSecret",
    ["JWT_SECRET_TOKEN"] = "AppSettings__Token",
    ["DEFAULT_CONNECTION"] = "ConnectionStrings__DefaultConnection",
    ["ADMIN_POSTGRES_CONNECTION"] = "ConnectionStrings__AdminPostgresConnection",
    ["ADMIN_MYSQL_CONNECTION"] = "ConnectionStrings__AdminMySqlConnection",
    ["ADMIN_SQLSERVER_CONNECTION"] = "ConnectionStrings__AdminSqlServerConnection",
    ["SENDGRID_API_KEY"] = "SendGrid__ApiKey",
    ["SENDGRID_FROM_EMAIL"] = "SendGrid__FromEmail",
    ["SENDGRID_FROM_NAME"] = "SendGrid__FromName",
    ["APP_DASHBOARD_URL"] = "App__DashboardUrl",
    // ASPNETCORE_* ya están en el formato correcto, solo asegurarse que existan
    ["ASPNETCORE_ENVIRONMENT"] = "ASPNETCORE_ENVIRONMENT",
    ["ASPNETCORE_URLS"] = "ASPNETCORE_URLS"
};

foreach (var (original, aspnetCore) in envVarMappings)
{
    var value = Environment.GetEnvironmentVariable(original);
    if (!string.IsNullOrEmpty(value))
    {
        Environment.SetEnvironmentVariable(aspnetCore, value);
        Console.WriteLine($"  ✓ {aspnetCore} configurado");
    }
}

// Valores por defecto si no están definidos
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
}
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    // Usar localhost para acceso local, o + para todas las interfaces
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:5063");
}

var builder = WebApplication.CreateBuilder(args);


var mercadoPagoAccessToken = builder.Configuration["MercadoPago:AccessToken"];
if (string.IsNullOrEmpty(mercadoPagoAccessToken))
{
    throw new Exception("El Access Token de Mercado Pago no está configurado. Asegúrate de definir la variable de entorno 'MercadoPago__AccessToken'.");
}
MercadoPagoConfig.AccessToken = mercadoPagoAccessToken;


var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";


builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:5173",
                                           "http://localhost:8080",
                                           "https://andromeda.andrescortes.dev")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// 2. Configuración de la base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Configuración del servicio de correo electrónico
builder.Services.AddScoped<IEmailService, SendGridEmailService>();

// 3. Registro de Servicios y Repositorios (Inyección de Dependencias)
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IDatabaseProvisioner, DatabaseProvisioner>();
// A medida que crees más servicios (pagos, correos), los registrarás aquí.
builder.Services.AddScoped<IPaymentService, PaymentService>(); 
// 4. Configuración de los Controladores de la API
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingresa el token JWT en este formato: Bearer {tu token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var tokenKeyString = builder.Configuration.GetSection("AppSettings:Token").Value;
        
        if (string.IsNullOrEmpty(tokenKeyString))
            throw new Exception("La clave del token 'AppSettings:Token' no está configurada.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenKeyString)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });


var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CCD API v1");
    c.RoutePrefix = string.Empty; 
});

app.UseHttpsRedirection();

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();