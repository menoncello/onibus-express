using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using OniBus.Api.Features.Rotas;
using OniBus.Api.Persistence;
using OniBus.Domain.Viagens;
using Testcontainers.PostgreSql;

namespace OniBus.Api.Tests.Features.Rotas;

/// <summary>
/// AD-10: teste de contrato HTTP contra PostgreSQL real via Testcontainers, imagem pinada
/// idêntica à do compose (<c>postgres:18-alpine</c>). Nunca SQLite in-memory.
/// </summary>
public sealed class GetRotasTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("onibus")
        .WithUsername("onibus")
        .WithPassword("onibus_test")
        .Build();

    private WebApplicationFactory<Program>? _factory;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString());
        });

        // Força o boot completo (migração + seed) antes do primeiro request do teste.
        using var scope = _factory.Services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<OniBusDbContext>();
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task GetRotas_com_seed_minimo_retorna_200_com_origem_destino_e_duracao()
    {
        var client = _factory!.CreateClient();

        var response = await client.GetAsync("/rotas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rotas = await response.Content.ReadFromJsonAsync<List<RotaResponse>>();

        Assert.NotNull(rotas);
        Assert.NotEmpty(rotas);
        Assert.All(rotas!, rota =>
        {
            Assert.False(string.IsNullOrWhiteSpace(rota.Origem));
            Assert.False(string.IsNullOrWhiteSpace(rota.Destino));
            Assert.True(rota.DuracaoEstimadaMinutos > 0);
        });
    }

    [Fact]
    public async Task GetRotas_com_multiplas_rotas_ordena_por_origem_e_depois_por_destino()
    {
        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OniBusDbContext>();

            // A rota semeada por RotaSeed é ("São Paulo", "Rio de Janeiro"); esta rota extra
            // ordena antes dela, exercitando o OrderBy(Origem).ThenBy(Destino) com mais de uma
            // linha — o seed mínimo só cobre uma.
            db.Rotas.Add(new Rota
            {
                Id = Guid.NewGuid(),
                Origem = "Belo Horizonte",
                Destino = "Salvador",
                DuracaoEstimada = TimeSpan.FromHours(20),
            });

            await db.SaveChangesAsync();
        }

        var client = _factory!.CreateClient();

        var response = await client.GetAsync("/rotas");
        var rotas = await response.Content.ReadFromJsonAsync<List<RotaResponse>>();

        Assert.NotNull(rotas);
        Assert.True(rotas!.Count >= 2);

        var origens = rotas.Select(r => (r.Origem, r.Destino)).ToList();
        var origensOrdenadas = origens
            .OrderBy(par => par.Origem, StringComparer.Ordinal)
            .ThenBy(par => par.Destino, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(origensOrdenadas, origens);
        Assert.Equal("Belo Horizonte", rotas[0].Origem);
    }
}
