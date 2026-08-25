using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using OniBus.Api.Persistence;

namespace OniBus.Api.Features.Rotas;

/// <summary>
/// <c>GET /rotas</c> — endpoint público (sem autenticação) que lista as Rotas semeadas para
/// popular os campos de origem/destino da busca (FR-1).
/// </summary>
public static class GetRotas
{
    public static void MapGetRotas(this IEndpointRouteBuilder app)
    {
        app.MapGet("/rotas", HandleAsync)
            .WithName("GetRotas");
    }

    private static async Task<Ok<IReadOnlyList<RotaResponse>>> HandleAsync(
        OniBusDbContext db,
        CancellationToken cancellationToken)
    {
        var rotas = await db.Rotas
            .OrderBy(r => r.Origem)
            .ThenBy(r => r.Destino)
            .Select(r => new RotaResponse(r.Id, r.Origem, r.Destino, (int)Math.Round(r.DuracaoEstimada.TotalMinutes)))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<RotaResponse>>(rotas);
    }
}

/// <summary>Origem, destino e duração estimada (em minutos) de uma Rota.</summary>
public record RotaResponse(Guid Id, string Origem, string Destino, int DuracaoEstimadaMinutos);
