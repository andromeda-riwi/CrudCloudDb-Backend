using System.Text;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using CCD.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // <-- ¡AÑADIDO! Necesario para la configuración de Swagger.

var builder = WebApplication.CreateBuilder(args);

// --- SECCIÓN DE CONFIGURACIÓN DE SERVICIOS ---

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

// 1. Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:8080", 
                                             "https://voyager.andrescortes.dev")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

// 2. Configuración de la Base de Datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. Registro de Servicios y Repositorios
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IDatabaseProvisioner, DatabaseProvisioner>();

// 4. Configuración de los Controladores
builder.Services.AddControllers();

// 5. Configuración de Swagger
builder.Services.AddEndpointsApiExplorer();

// ========================================================================
// --- INICIO DEL NUEVO BLOQUE DE CÓDIGO PARA SWAGGER ---
// ========================================================================
builder.Services.AddSwaggerGen(options =>
{
    // Definimos el esquema de seguridad para JWT
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Por favor, introduce 'Bearer' [espacio] y luego tu token. \n\nEjemplo: Bearer eyJhbGciOiJIUzI1Ni..."
    });

    // Añadimos el requisito de seguridad para habilitar el botón "Authorize"
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });
});
// ========================================================================
// --- FIN DEL NUEVO BLOQUE DE CÓDIGO ---
// ========================================================================

// 6. Configuración de la Autenticación JWT
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