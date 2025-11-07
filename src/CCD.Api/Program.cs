using System.Text;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using CCD.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- SECCIÓN DE CONFIGURACIÓN DE SERVICIOS ---

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// 1. Configuración de CORS (Cross-Origin Resource Sharing)
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          // PERMITE QUE TU FRONTEND SE COMUNIQUE CON LA API
                          // Si tu frontend corre en otro puerto local, añádelo aquí
                          policy.WithOrigins("http://localhost:5173",
                                           "http://localhost:8080"
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
// 4. Configuración de los Controladores de la API
builder.Services.AddControllers();

// 5. Configuración de Swagger para la documentación de la API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    // Configurar Swagger para usar JWT
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

// 6. Configuración de la Autenticación JWT (JSON Web Token)
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

// --- Construcción de la Aplicación ---
var app = builder.Build();

// --- SECCIÓN DE CONFIGURACIÓN DEL PIPELINE HTTP ---
// El orden aquí es muy importante.

// Habilita Swagger y SwaggerUI en TODOS los entornos (Desarrollo y Producción)
// Esto soluciona el error 404 que estabas viendo.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // Esto hace que Swagger esté disponible en la raíz (ej: /) en lugar de /swagger
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CCD API v1");
    c.RoutePrefix = string.Empty; 
});


// Redirige HTTP a HTTPS (Certbot ya configura esto, pero es bueno tenerlo)
app.UseHttpsRedirection();

// Aplica la política de CORS
app.UseCors(MyAllowSpecificOrigins);

// 1. Autenticación: Verifica quién es el usuario (lee el token JWT)
app.UseAuthentication();

// 2. Autorización: Verifica si el usuario tiene permiso para acceder
app.UseAuthorization();

// 3. Mapeo a los controladores: Dirige la petición al endpoint correcto
app.MapControllers();

// Inicia la aplicación
app.Run();