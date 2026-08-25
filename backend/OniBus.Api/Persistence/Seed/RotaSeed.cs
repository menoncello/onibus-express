using Microsoft.EntityFrameworkCore;
using OniBus.Domain.Viagens;

namespace OniBus.Api.Persistence.Seed;

/// <summary>
/// Seed mínimo desta story: garante ao menos uma Rota para o endpoint <c>GET /rotas</c> ter o
/// que listar. Idempotente por chave natural (origem, destino) — nunca por contagem prévia — para
/// sobreviver a um segundo <c>docker-compose up</c> sem duplicar.
///
/// Provisório e deliberadamente simples: o seed completo com casos de borda (5 Rotas, 16 Viagens,
/// reservas geradas via IGeradorCodigoReserva/CPF real) é escopo da Story 1.4 e pode substituir
/// esta classe por inteiro.
/// </summary>
public static class RotaSeed
{
    private static readonly (string Origem, string Destino, TimeSpan Duracao)[] RotasMinimas =
    [
        ("São Paulo", "Rio de Janeiro", TimeSpan.FromHours(5.5)),
    ];

    public static async Task SeedAsync(OniBusDbContext db, CancellationToken cancellationToken = default)
    {
        foreach (var (origem, destino, duracao) in RotasMinimas)
        {
            var jaExiste = await db.Rotas
                .AnyAsync(r => r.Origem == origem && r.Destino == destino, cancellationToken);

            if (jaExiste)
            {
                continue;
            }

            db.Rotas.Add(new Rota
            {
                Id = Guid.NewGuid(),
                Origem = origem,
                Destino = destino,
                DuracaoEstimada = duracao,
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
