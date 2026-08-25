namespace OniBus.Domain.Viagens;

/// <summary>
/// Ocorrência datada de uma Rota: referencia uma Rota, tem data/hora de partida e preço base.
/// Uma Viagem tem muitos Assentos (derivados, ver <see cref="LayoutOnibus"/>) e muitas Reservas.
/// </summary>
public class Viagem
{
    public Guid Id { get; set; }

    public Guid RotaId { get; set; }

    /// <summary>
    /// Instante de partida em UTC (AD-3). Tradução para o fuso de negócio acontece na borda da busca.
    /// </summary>
    public required DateTimeOffset Partida { get; set; }

    public required decimal PrecoBase { get; set; }
}
