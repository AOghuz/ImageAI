using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens; // Bu using'i ekleyin
using Wallet.Persistence.Persistence;
using Wallet.Business.Business;
using System.Text;
using Wallet.Application.DTOs;
using System.IdentityModel.Tokens.Jwt;
using FluentValidation.AspNetCore;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateReservationRequestValidator>();
// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Wallet API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT için: Bearer {token}"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// Add custom services (Persistence + Business layers)
builder.Services.AddWalletPersistence(builder.Configuration);
builder.Services.AddWalletBusiness(builder.Configuration);

// JWT claims mapping fix - BU SATIRI EKLEYÝN
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// Enable JWT Authentication
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>();
if (string.IsNullOrEmpty(jwt?.Secret))
{
    throw new InvalidOperationException("JWT Secret is missing in configuration.");
}

// DÜZELTME: Base64Url encoding'i kaldýrdýk
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub" // Bu da yardýmcý olabilir
        };
    });
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});
builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger Setup
app.UseSwagger();
app.UseSwaggerUI();
app.UseExceptionHandler("/error");

// Request logging
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Health check endpoint ekleyin
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();