using ImageProcessingService.Services;
using ImageProcessingService.Models.GcsBackground;
using ImageProcessingService.Services.Wallet;
using ImageProcessingService.Integrations.Fal;
using ImageProcessingService.Services.Fal.Abstract;
using ImageProcessingService.Services.Fal.Adapters.Core;
using ImageProcessingService.Services.Fal.Generic;
using ImageProcessingService.Services.Fal.Generic.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Features;
using System.IdentityModel.Tokens.Jwt;

// --- YENİ ADAPTER USINGLERİ (Eskileri sildik, bunları ekledik) ---
// Not: Tek tek using eklemene gerek yok, Reflection ile otomatik bulacağız.
// Ama manuel eklemek istersen namespace'leri buraya yazabilirsin.

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURATION (AYARLAR)
// User Secrets, Env Variables ve JSON dosyalarını otomatik okur.
// "Fal:ApiKey" environment variable'ı varsa, FalOptions.Key property'sine otomatik maplenir.
builder.Services.Configure<FalOptions>(builder.Configuration.GetSection("Fal"));
builder.Services.Configure<FalModelsOptions>(builder.Configuration.GetSection("FalModels"));

builder.Services.AddControllers();

// 2. JWT AUTHENTICATION
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap["sub"] = ClaimTypes.NameIdentifier;

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret missing");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "imageaiproject.identity";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "imageaiproject.clients";
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = key,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });

builder.Services.AddAuthorization();

// 3. HTTP CLIENTS & SERVICES

// A) GCS Background Services
builder.Services.AddHttpClient<Backgroundv1>(c => c.Timeout = TimeSpan.FromMinutes(3));
builder.Services.AddHttpClient<Backgroundv2>(c => c.Timeout = TimeSpan.FromMinutes(3));
builder.Services.AddHttpClient<Backgroundv3>(c => c.Timeout = TimeSpan.FromMinutes(3));
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService.Services.ImageProcessingService>();

// B) Wallet API Client
builder.Services.AddHttpClient<IWalletApiClient, WalletApiClient>(client =>
{
    // appsettings.json'da "Services:WalletApiUrl" olmalı.
    // Eğer eski kodunda "Services:WalletApi" ise onu kullan.
    var walletUrl = builder.Configuration["Services:WalletApi"] ?? builder.Configuration["Services:WalletApiUrl"]
        ?? throw new InvalidOperationException("Services:WalletApi configuration missing");

    client.BaseAddress = new Uri(walletUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// C) FAL & CDN Client
builder.Services.AddTransient<FalAuthHandler>(); // Auth Handler'ı kaydet

builder.Services.AddHttpClient<IFalQueueClient, FalQueueClient>((sp, client) =>
{
    var opt = sp.GetRequiredService<IOptions<FalOptions>>().Value;
    var baseUrl = string.IsNullOrWhiteSpace(opt.BaseUrl)
        ? "https://queue.fal.run"
        : opt.BaseUrl.TrimEnd('/');

    client.BaseAddress = new Uri(baseUrl + "/");
    client.Timeout = TimeSpan.FromMinutes(5);
})
.AddHttpMessageHandler<FalAuthHandler>(); // Handler'ı bağla

builder.Services.AddHttpClient("cdn", c => c.Timeout = TimeSpan.FromMinutes(3));

// 4. CORE SERVICES (YENİ MİMARİ)
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IGeneratedFileStore, LocalGeneratedFileStore>();
builder.Services.AddScoped<IFalModelRegistry, FalModelRegistry>();
builder.Services.AddScoped<IFalJobsService, FalJobsService>();

// 5. ADAPTERS (OTOMATİK KAYIT - REFLECTION)
// Tek tek AddScoped<IFalModelAdapter, FluxAdapter>() yazmak yerine hepsini bulur.
var adapterAssembly = typeof(FalModelAdapterBase).Assembly;
var adapterTypes = adapterAssembly.GetTypes()
    .Where(t => t.GetInterfaces().Contains(typeof(IFalModelAdapter))
                && !t.IsAbstract
                && !t.IsInterface);

foreach (var type in adapterTypes)
{
    builder.Services.AddScoped(type); // Kendisi olarak
    builder.Services.AddScoped(typeof(IFalModelAdapter), type); // Interface olarak
}

// 6. UPLOAD LIMITS & CORS
builder.Services.Configure<FormOptions>(o => { o.MultipartBodyLengthLimit = 50_000_000; }); // 50MB

builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowFrontend", p =>
        p.WithOrigins("https://localhost:7233") // Frontend URL'ini buraya yaz
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});

// 7. SWAGGER
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Image Processing API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT için: Bearer {token}"
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

var app = builder.Build();

// 8. STARTUP VALIDATION (API KEY KONTROLÜ)
// Bu kısım uygulama ayağa kalkarken çalışır ve key yoksa hata fırlatır.
// .NET Core environment variable'ları otomatik olarak IConfiguration içine yükler.
// Yani Environment'ta "Fal__ApiKey" veya "Fal:ApiKey" varsa, burası onu görür.
var falApiKey = app.Configuration["Fal:ApiKey"];
if (string.IsNullOrWhiteSpace(falApiKey))
{
    // Loglayıp devam edelim ki uygulama çökmesin, ama uyarı verelim.
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("UYARI: Fal:ApiKey bulunamadı! Fal.AI işlemleri çalışmayacaktır.");
}

// 9. PIPELINE
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles(); // Resim sunumu için

app.MapControllers();

app.Run();