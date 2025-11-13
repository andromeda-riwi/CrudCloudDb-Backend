using System.Text;
using CCD.Core.Interfaces;
using CCD.Infrastructure.Data;
using CCD.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer; //to add authentication of the API with JWTBearer
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;  //to get the tokens to authentication in the login 
using MercadoPago.Config; //config of mercado pago.. webhooks and etc

// Cargar variables de entorno desde el archivo .env
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
    Console.WriteLine($"✅ Archivo .env cargado desde: {envPath}");
}
else
{
    Console.WriteLine($"⚠️ No se encontró el archivo .env en: {envPath}");
}

var builder = WebApplication.CreateBuilder(args);

// Sobrescribir la configuración con las variables de entorno cargadas
builder.Configuration.AddEnvironmentVariables();


var mercadoPagoAccessToken = builder.Configuration["MercadoPago:AccessToken"]; //create a configuration of the mercado pago token
if (string.IsNullOrEmpty(mercadoPagoAccessToken))
{
    throw new Exception("El Access Token de Mercado Pago no está configurado. Asegúrate de definir la variable de entorno 'MercadoPago__AccessToken'.");
}
MercadoPagoConfig.AccessToken = mercadoPagoAccessToken; //storage in the mercado pago var the access token


var MyAllowSpecificOrigins = "_myAllowSpecificOrigins"; //cors configuration


builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:5173",
                                           "http://localhost:8080",
                                           "https://andromeda.andrescortes.dev") //cors configuration and polities
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
builder.Services.AddScoped<IPaymentService, PaymentService>(); //Mercado pago Ipayment services 
// 4. Configuración de los Controladores de la API
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme //configuration of the jwtbearer and authorization
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
        
        if (string.IsNullOrEmpty(tokenKeyString)) //expected error
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


app.UseSwagger(); //to use swagger and make endpoints with interface in the web
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "CCD API v1");
    c.RoutePrefix = string.Empty; 
});

app.UseHttpsRedirection(); //to use https

app.UseCors(MyAllowSpecificOrigins); //to use cors

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run(); //run the application