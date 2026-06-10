using Markadan.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Fail-fast: instance'a özgü sırlar config'ten gelmeli, koda gömülü olmamalı
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
    throw new InvalidOperationException(
        "Jwt:Key eksik veya 32 byte'tan kısa. Ortam değişkeni (Jwt__Key) veya user-secrets ile sağlayın.");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection eksik. Ortam değişkeni (ConnectionStrings__DefaultConnection) ile sağlayın.");

builder.Services.AddControllers(o =>
{
    o.Filters.Add<Markadan.API.Filters.ApiExceptionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Markadan API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = " test i�in gelen tokeni value k�sm�na yaz�n authorize butonuna bas�n...",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, Array.Empty<string>() }
    });
});

builder.Services.AddDataProtection();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<Markadan.Application.Options.JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length == 0)
    Console.WriteLine("UYARI: Cors:AllowedOrigins boş — tüm cross-origin istekleri reddedilecek.");

builder.Services.AddCors(o => o.AddPolicy("Frontend",
    p => p.WithOrigins(allowedOrigins)
          .AllowAnyHeader().AllowAnyMethod()));



builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, o =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        o.TokenValidationParameters = new()
        {
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // Token s�resi dolduktan sonra hemen ge�ersiz k�lmay� sa�l�yor deneme...
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});

// Public katalog uçları için IP bazlı rate limiting — bot/scraper engeli
var permitLimit   = builder.Configuration.GetValue<int>("RateLimit:PermitLimit",   60);
var windowSeconds = builder.Configuration.GetValue<int>("RateLimit:WindowSeconds", 60);

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("public-catalog", o =>
    {
        o.PermitLimit          = permitLimit;
        o.Window               = TimeSpan.FromSeconds(windowSeconds);
        o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        o.QueueLimit           = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();  // de�erli bilgi****buradan sonraki s�ralama �nemli AddAuthentication, addAuthorizationve addJwtBearer middleware'leri UseAuthorization'dan �nce ve UseAuthentication'dan sonra olmal�****
app.UseCors("Frontend");

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();


await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Markadan.Infrastructure.Data.MarkadanDbContext>();
    db.Database.Migrate();
    await Markadan.Infrastructure.Seeding.AdminSeeder.SeedAsync(scope.ServiceProvider, builder.Configuration);
}

app.Run();
