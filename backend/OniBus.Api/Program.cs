using Microsoft.EntityFrameworkCore;
using OniBus.Api.Features.Rotas;
using OniBus.Api.Persistence;
using OniBus.Api.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddDbContext<OniBusDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

// AD-15: migração e seed no boot, mesmo caminho de código com ou sem Docker — a Api só aceita
// requisições depois que o schema existe e há ao menos uma Rota para listar.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OniBusDbContext>();
    await db.Database.MigrateAsync();
    await RotaSeed.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGetRotas();

app.Run();

public partial class Program;
