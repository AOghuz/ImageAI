using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Identity.BusinessLayer.Business;
using Identity.BusinessLayer.Options;
using Identity.PersistenceLayer.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Identity.ApplicationLayer.Validators;
using Identity.EntityLayer.Entities; // Role entities için
using Microsoft.AspNetCore.Identity; // RoleManager için
using Microsoft.AspNetCore.HttpOverrides; // IP forwarding için

var builder = WebApplication.CreateBuilder(args);

// 1. GÜVENLÝK: Reverse Proxy (Nginx/Docker) için IP baþlýklarýný okuma ayarý
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Localde test ederken bilinen proxy limitini kaldýrýyoruz
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Persistence + Business
builder.Services.AddIdentityPersistence(builder.Configuration);
builder.Services.AddIdentityBusiness(builder.Configuration);

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// JWT Mapping Fix
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// JWT Ayarlarý
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
          ?? throw new InvalidOperationException("Jwt ayarlarý eksik.");

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
            NameClaimType = "sub"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Identity API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT girin: Bearer {token}"
    });
    c.AddSecurityRequirement(new()
    {
        {
            new() { Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 2. PERFORMANS: Uygulama ayaða kalkarken Rolleri bir kere kontrol et/oluþtur (Seeding)
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
    var roles = new[] { "user", "admin" }; // Sistemdeki tüm roller
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new AppRole { Name = role });
        }
    }
}

// Middleware Sýralamasý Önemlidir
app.UseForwardedHeaders(); // IP düzeltmesi en baþta olmalý

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();