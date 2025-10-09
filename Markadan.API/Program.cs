using Markadan.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

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
        Description = " test için gelen tokeni value kýsmýna yazýn authorize butonuna basýn...",
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

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Markadan.Application.Mapping.CatalogProfile).Assembly));


builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<Markadan.Application.Options.JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

builder.Services.AddCors(o => o.AddPolicy("Frontend",
    p => p.WithOrigins("http://localhost:3000", "https://localhost:3000")
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
            ClockSkew = TimeSpan.Zero // Token süresi dolduktan sonra hemen geçersiz kýlmayý saðlýyor deneme...
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
});



var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();  // deðerli bilgi****buradan sonraki sýralama önemli AddAuthentication, addAuthorizationve addJwtBearer middleware'leri UseAuthorization'dan önce ve UseAuthentication'dan sonra olmalý****
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();


app.MapControllers();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Markadan.Infrastructure.Data.MarkadanDbContext>();
    db.Database.Migrate();
}

app.Run();
