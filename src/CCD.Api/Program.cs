// --- Imports necesarios para toda la funcionalidad ---
using System.Text;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using CCD.Infrastructure.Services; // Asumo que aquí tendrás tu AuthRepository
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// --- SECCIÓN DE CONFIGURACIÓN DE SERVICIOS ---

// Define un nombre para la política de CORS para reutilizarla
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// 1. Configuración de CORS (Cross-Origin Resource Sharing)
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          // Permite que tu frontend en desarrollo y producción se comuniquen con la API
                          policy.WithOrigins("http://localhost:8080", // Cambia este puerto si tu Vue usa otro
                                             "https://voyager.andrescortes.dev")
                                .AllowAnyHeader()  // Permite cualquier cabecera (como Authorization para el JWT)
                                .AllowAnyMethod(); // Permite cualquier método HTTP (GET, POST, DELETE, etc.)
                      });
});


// 2. Configuración de la Base de Datos (Entity Framework Core)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Registro del Repositorio de Autenticación (Inyección de Dependencias)
// Aquí registrarás todos tus repositorios y servicios a medida que los crees.
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
// Ejemplo: builder.Services.AddScoped<IDatabaseService, DatabaseService>();


// 4. Configuración de los Controladores de la API
builder.Services.AddControllers();

// 5. Configuración de Swagger para la documentación de la API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Habilita Swagger solo en el entorno de desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirige HTTP a HTTPS
app.UseHttpsRedirection();

// ** APLICA LA POLÍTICA DE CORS AQUÍ **
// Debe ir antes de Authentication y Authorization
app.UseCors(MyAllowSpecificOrigins);

// 1. Autenticación: Verifica quién es el usuario (lee el token JWT)
app.UseAuthentication();

// 2. Autorización: Verifica si el usuario tiene permiso para acceder
app.UseAuthorization();

// 3. Mapeo a los controladores: Dirige la petición al endpoint correcto
app.MapControllers();

// Inicia la aplicación
app.Run();