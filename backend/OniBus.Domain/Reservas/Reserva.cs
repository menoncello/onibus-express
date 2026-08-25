namespace OniBus.Domain.Reservas;

/// <summary>
/// Reserva de um Assento numa Viagem. Esta story modela apenas o suficiente para os dois
/// índices únicos fundacionais (AD-6, AD-7) existirem desde a primeira migração; os dados do
/// Passageiro (AD-5) e o gerador de Código (AD-8) chegam com a story que introduz
/// <c>POST /reservas</c>.
/// </summary>
public class Reserva
{
    public Guid Id { get; set; }

    public Guid ViagemId { get; set; }

    /// <summary>Posição de 1 a 44, ver <see cref="OniBus.Domain.Viagens.LayoutOnibus"/>.</summary>
    public required int NumeroAssento { get; set; }

    /// <summary>Forma canônica <c>AAA-00000</c>. Único por <c>ux_reservas_codigo</c>.</summary>
    public required string Codigo { get; set; }

    public required StatusReserva Status { get; set; }
}
