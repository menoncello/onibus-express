namespace OniBus.Domain.Viagens;

/// <summary>
/// Par ordenado origem→destino com duração estimada. Uma Rota tem muitas Viagens.
/// </summary>
public class Rota
{
    public Guid Id { get; set; }

    public required string Origem { get; set; }

    public required string Destino { get; set; }

    /// <summary>
    /// Duração estimada do trecho, independente de qualquer Viagem específica.
    /// </summary>
    public required TimeSpan DuracaoEstimada { get; set; }
}
