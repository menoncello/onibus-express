using Microsoft.EntityFrameworkCore;
using OniBus.Api.Persistence;
using OniBus.Domain.Reservas;
using OniBus.Domain.Viagens;
using Testcontainers.PostgreSql;

namespace OniBus.Api.Tests.Persistence;

/// <summary>
/// AD-6/AD-7/AD-15: os três índices únicos fundacionais (rotas por chave natural, reservas por
/// código, reservas por assento Confirmado) precisam ser garantidos pelo Postgres, não só pela
/// aplicação — mesmo sem nenhum endpoint de reserva ainda para exercitá-los via HTTP. Insere
/// diretamente via <c>DbContext</c>, contornando qualquer checagem de aplicação (como a do
/// <see cref="OniBus.Api.Persistence.Seed.RotaSeed"/>), para provar que a restrição vive no schema.
/// </summary>
public sealed class OniBusDbContextIndexesTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:18-alpine")
        .WithDatabase("onibus")
        .WithUsername("onibus")
        .WithPassword("onibus_test")
        .Build();

    public async ValueTask InitializeAsync() => await _postgres.StartAsync();

    public async ValueTask DisposeAsync() => await _postgres.DisposeAsync();

    private async Task<OniBusDbContext> CriarContextoMigradoAsync()
    {
        var options = new DbContextOptionsBuilder<OniBusDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        var db = new OniBusDbContext(options);
        await db.Database.MigrateAsync();
        return db;
    }

    private static async Task<Viagem> SemearViagemAsync(OniBusDbContext db)
    {
        var rota = new Rota
        {
            Id = Guid.NewGuid(),
            Origem = $"Origem {Guid.NewGuid():N}",
            Destino = $"Destino {Guid.NewGuid():N}",
            DuracaoEstimada = TimeSpan.FromHours(1),
        };
        db.Rotas.Add(rota);

        var viagem = new Viagem
        {
            Id = Guid.NewGuid(),
            RotaId = rota.Id,
            Partida = DateTimeOffset.UtcNow,
            PrecoBase = 100m,
        };
        db.Viagens.Add(viagem);

        await db.SaveChangesAsync();
        return viagem;
    }

    private static Reserva CriarReserva(Guid viagemId, int numeroAssento, string codigo, StatusReserva status) =>
        new()
        {
            Id = Guid.NewGuid(),
            ViagemId = viagemId,
            NumeroAssento = numeroAssento,
            Codigo = codigo,
            Status = status,
        };

    [Fact]
    public async Task Rota_com_mesma_origem_e_destino_de_outra_ja_salva_viola_ux_rotas_origem_destino()
    {
        using var db = await CriarContextoMigradoAsync();

        db.Rotas.Add(new Rota
        {
            Id = Guid.NewGuid(),
            Origem = "Origem Duplicada",
            Destino = "Destino Duplicado",
            DuracaoEstimada = TimeSpan.FromHours(1),
        });
        await db.SaveChangesAsync();

        db.Rotas.Add(new Rota
        {
            Id = Guid.NewGuid(),
            Origem = "Origem Duplicada",
            Destino = "Destino Duplicado",
            DuracaoEstimada = TimeSpan.FromHours(2),
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Reserva_com_mesmo_codigo_de_outra_ja_salva_viola_ux_reservas_codigo()
    {
        using var db = await CriarContextoMigradoAsync();
        var viagem = await SemearViagemAsync(db);

        db.Reservas.Add(CriarReserva(viagem.Id, numeroAssento: 1, codigo: "AAA-00001", StatusReserva.Confirmada));
        await db.SaveChangesAsync();

        // Assento diferente do da primeira Reserva: se este teste falhar por causa do índice
        // errado (assento em vez de código), fica evidente ao investigar.
        db.Reservas.Add(CriarReserva(viagem.Id, numeroAssento: 2, codigo: "AAA-00001", StatusReserva.Confirmada));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Reserva_confirmada_com_mesmo_assento_de_outra_confirmada_viola_ux_reservas_viagem_assento_confirmada()
    {
        using var db = await CriarContextoMigradoAsync();
        var viagem = await SemearViagemAsync(db);

        db.Reservas.Add(CriarReserva(viagem.Id, numeroAssento: 5, codigo: "BBB-00001", StatusReserva.Confirmada));
        await db.SaveChangesAsync();

        // Código diferente do da primeira Reserva: se este teste falhar por causa do índice
        // errado (código em vez de assento), fica evidente ao investigar.
        db.Reservas.Add(CriarReserva(viagem.Id, numeroAssento: 5, codigo: "BBB-00002", StatusReserva.Confirmada));

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Reserva_cancelada_com_mesmo_assento_de_outra_confirmada_nao_viola_indice_parcial()
    {
        using var db = await CriarContextoMigradoAsync();
        var viagem = await SemearViagemAsync(db);

        db.Reservas.Add(CriarReserva(viagem.Id, numeroAssento: 7, codigo: "CCC-00001", StatusReserva.Confirmada));
        await db.SaveChangesAsync();

        db.Reservas.Add(CriarReserva(viagem.Id, numeroAssento: 7, codigo: "CCC-00002", StatusReserva.Cancelada));

        // Não deve lançar: o índice único é parcial (HasFilter "status = 'Confirmada'"), então uma
        // Reserva Cancelada no mesmo assento não conflita com a Confirmada já existente.
        await db.SaveChangesAsync();

        var reservas = await db.Reservas
            .Where(r => r.ViagemId == viagem.Id && r.NumeroAssento == 7)
            .ToListAsync();

        Assert.Equal(2, reservas.Count);
    }
}
