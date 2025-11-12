using System.Text;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using CCD.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MercadoPago.Config;

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