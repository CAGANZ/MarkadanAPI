using Markadan.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(Markadan.Application.Mapping.CatalogProfile).Assembly));



// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration); // konfigürasyonlarýmý kayýt ettim





var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Markadan.Infrastructure.Data.MarkadanDbContext>();
    db.Database.Migrate();
}

app.Run();
