using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Wallet.Business.Business;     // Wallet Business Layer
using Wallet.Persistence.Persistence; // Wallet Persistence Layer

var builder = WebApplication.CreateBuilder(args);

// 1. Ayarlar (Appsettings'den)
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];
var jwtSecret = jwtSection["Secret"];

// 2. Servis Kayýtlarý (Wallet Katmanlarý)
// BURASI ÇOK ÖNEMLÝ: Identity deðil Wallet servislerini ekliyoruz.
builder.Services.AddWalletPersistence(builder.Configuration);
builder.Services.AddWalletBusiness(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 3. Swagger (Bearer Token Destekli)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Wallet API", Version = "v1" });

    // Bearer Token Kilidi
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Token giriniz: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// 4. Authentication (JWT)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret ?? "varsayilan_cok_gizli_anahtar_123456")),
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

// 5. Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // Kimlik Doðrulama
app.UseAuthorization();  // Yetkilendirme

app.MapControllers();

app.Run();