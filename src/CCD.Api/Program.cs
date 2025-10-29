// --- Imports necesarios para toda la funcionalidad ---
using System.Text;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using CCD.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- SECCIÓN DE CONFIGURACIÓN DE SERVICIOS ---

// 1. Configuración de la Base de Datos (Entity Framework Core)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Registro del Repositorio de Autenticación (Inyección de Dependencias)
builder.Services.AddScoped<IAuthRepository, AuthRepository>();

// 3. Registro del Servicio de Aprovisionamiento de Bases de Datos
builder.Services.AddScoped<IDatabaseProvisioner, DatabaseProvisioner>();

// 4. Configuración de los Controladores de la API
builder.Services.AddControllers();

// 4. Configuración de Swagger para la documentación de la API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "CCD.Api", 
        Version = "v1" 
    });
    
    // Configuración de seguridad JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ingresa solo el token (sin 'Bearer').",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// 5. Configuración de la Autenticación JWT (JSON Web Token)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Leemos la clave secreta desde appsettings.json
        var tokenKeyString = builder.Configuration.GetSection("AppSettings:Token").Value;
        
        // Verificamos que la clave exista para evitar errores en tiempo de ejecución
        if (string.IsNullOrEmpty(tokenKeyString))
            throw new Exception("La clave del token 'AppSettings:Token' no está configurada.");

        // Parámetros para validar los tokens que recibe la API
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 1. Autenticación: Verifica quién es el usuario (lee el token JWT)
app.UseAuthentication();

// 2. Autorización: Verifica si el usuario tiene permiso para acceder
app.UseAuthorization();

// 3. Mapeo a los controladores: Dirige la petición al endpoint correcto
app.MapControllers();

app.Run();