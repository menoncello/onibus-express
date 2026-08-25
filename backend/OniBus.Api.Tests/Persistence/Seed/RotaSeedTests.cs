using Microsoft.EntityFrameworkCore;
using OniBus.Api.Persistence;
using OniBus.Api.Persistence.Seed;
using Testcontainers.PostgreSql;

namespace OniBus.Api.Tests.Persistence.Seed;

/// <summary>
/// AD-15: idempotência do seed é por chave natural, não por contagem prévia — precisa
/// sobreviver a um segundo boot (um segundo <c>docker-compose up</c>) sem duplicar.
/// </summary>
public sealed class RotaSeedTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("onibus")
        .WithUsername("onibus")
        .WithPassword("onibus_test")
        .Build();

    public async ValueTask InitializeAsync() => await _postgres.StartAsync();

    public async ValueTask DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task SeedAsync_chamado_duas_vezes_nao_duplica_a_rota_da_mesma_chave_natural()
    {
        var options = new DbContextOptionsBuilder<OniBusDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        using var db = new OniBusDbContext(options);
        await db.Database.MigrateAsync();

        await RotaSeed.SeedAsync(db);
        await RotaSeed.SeedAsync(db);

        var linhas = await db.Rotas
            .Where(r => r.Origem == "São Paulo" && r.Destino == "Rio de Janeiro")
            .ToListAsync();

        Assert.Single(linhas);
    }
}
