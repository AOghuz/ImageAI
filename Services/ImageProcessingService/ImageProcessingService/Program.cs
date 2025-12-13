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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using ImageProcessingService.Services.Fal.Adapters.ImageEditing.FluxKontext;
using ImageProcessingService.Services.Fal.Adapters.ImageEditing.NanoBanana;
using ImageProcessingService.Services.Fal.Adapters.ImageEditing.QwenImageEdit;
using ImageProcessingService.Services.Fal.Adapters.ImageEditing.SeedDream;
using ImageProcessingService.Services.Fal.Adapters.ImageGeneration.FluxKontextLora;
using ImageProcessingService.Services.Fal.Adapters.ImageGeneration.FluxSrpo;
using ImageProcessingService.Services.Fal.Adapters.ImageGeneration.Imagen4;
using ImageProcessingService.Services.Fal.Adapters.ImageGeneration.Imagen4Ultra;
using ImageProcessingService.Services.Fal.Adapters.ImageGeneration.QwenImage;
using ImageProcessingService.Services.Fal.Adapters.ImageGeneration.SeedDream;
using ImageProcessingService.Services.Fal.Adapters.RemoveBackground;
using ImageProcessingService.Services.Fal.Adapters.Upscale.AuraSr;
using ImageProcessingService.Services.Fal.Adapters.Upscale.Topaz;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// ⚠️ ÖNEMLI: User Secrets (Development) ve Environment Variables (Production) okur
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables()  // ✅ Production için
    .AddUserSecrets<Program>(optional: true); // ✅ Development için

builder.Services.AddControllers();

// ---- JWT ----
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap["sub"] = ClaimTypes.NameIdentifier;

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret missing");
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

// ---- Background Services ----
builder.Services.AddHttpClient<Backgroundv1>(c => c.Timeout = TimeSpan.FromMinutes(3));
builder.Services.AddHttpClient<Backgroundv2>(c => c.Timeout = TimeSpan.FromMinutes(3));
builder.Services.AddHttpClient<Backgroundv3>(c => c.Timeout = TimeSpan.FromMinutes(3));
builder.Services.AddScoped<IImageProcessingService, ImageProcessingService.Services.ImageProcessingService>();

// ---- Wallet API ----
builder.Services.AddHttpClient<IWalletApiClient, WalletApiClient>(client =>
{
    var walletUrl = builder.Configuration["Services:WalletApi"]
        ?? throw new InvalidOperationException("Services:WalletApi missing");
    client.BaseAddress = new Uri(walletUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ---- FAL ----
builder.Services.Configure<FalOptions>(builder.Configuration.GetSection("Fal"));
builder.Services.Configure<FalModelsOptions>(builder.Configuration.GetSection("FalModels"));

// Model Adapters
builder.Services.AddScoped<IFalModelAdapter, NanoBananaAdapter>();
builder.Services.AddScoped<IFalModelAdapter, FluxKontextAdapter>();
builder.Services.AddScoped<IFalModelAdapter, QwenImageEditAdapter>();
builder.Services.AddScoped<IFalModelAdapter, SeedDreamAdapter>();
builder.Services.AddScoped<IFalModelAdapter, FluxKontextLoraAdapter>();
builder.Services.AddScoped<IFalModelAdapter, FluxSrpoAdapter>();
builder.Services.AddScoped<IFalModelAdapter, Imagen4Adapter>();
builder.Services.AddScoped<IFalModelAdapter, Imagen4UltraAdapter>();
builder.Services.AddScoped<IFalModelAdapter, QwenImageAdapter>();
builder.Services.AddScoped<IFalModelAdapter, SeedDreamV3Adapter>();
builder.Services.AddScoped<IFalModelAdapter, SeedDreamV4Adapter>();
builder.Services.AddScoped<IFalModelAdapter, BgRemoveAdapter>();
builder.Services.AddScoped<IFalModelAdapter, IdeogramV3ReplaceBackgroundAdapter>();
builder.Services.AddScoped<IFalModelAdapter, AuraSrAdapter>();
builder.Services.AddScoped<IFalModelAdapter, TopazUpscaleImageAdapter>();

builder.Services.AddScoped<IFalModelRegistry, FalModelRegistry>();

// Queue Client
builder.Services.AddTransient<FalAuthHandler>();
builder.Services.AddHttpClient<IFalQueueClient, FalQueueClient>((sp, client) =>
{
    var opt = sp.GetRequiredService<IOptions<FalOptions>>().Value;
    var baseUrl = string.IsNullOrWhiteSpace(opt.BaseUrl)
        ? "https://queue.fal.run"
        : opt.BaseUrl.TrimEnd('/');
    client.BaseAddress = new Uri(baseUrl + "/");
    client.Timeout = TimeSpan.FromMinutes(5);
    client.DefaultRequestVersion = new Version(1, 1);
    client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.None,
    AllowAutoRedirect = false,
    UseProxy = false
})
.AddHttpMessageHandler<FalAuthHandler>();

// Storage & CDN
builder.Services.AddHttpClient("cdn", c => c.Timeout = TimeSpan.FromMinutes(3));
builder.Services.AddHttpClient("fal-storage", c =>
{
    c.Timeout = TimeSpan.FromMinutes(2);
    c.DefaultRequestVersion = new Version(1, 1);
});
builder.Services.AddSingleton<IGeneratedFileStore, LocalGeneratedFileStore>();

builder.Services.AddScoped<IFalJobsService, FalJobsService>();

// ---- File Upload Limits ----
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 50_000_000; // 50MB
});

// ---- CORS ----
builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowFrontend", p =>
        p.WithOrigins("https://localhost:7233")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());

    o.AddPolicy("AllowAllOrigins", p =>
        p.AllowAnyOrigin()
         .AllowAnyMethod()
         .AllowAnyHeader());
});

// ---- Swagger ----
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

var app = builder.Build();

// ⚠️ STARTUP VALIDATION: API key kontrolü
var falKey = app.Configuration["Fal:ApiKey"];
if (string.IsNullOrWhiteSpace(falKey))
{
    throw new InvalidOperationException(
        "Fal:ApiKey bulunamadı. " +
        "Development: 'dotnet user-secrets set \"Fal:ApiKey\" \"YOUR_KEY\"' çalıştırın. " +
        "Production: Environment variable olarak Fal__ApiKey set edin.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();