namespace OniBus.Domain.Reservas;

/// <summary>
/// Estado de uma Reserva. Persistido como string (<c>HasConversion&lt;string&gt;()</c>) para que
/// o índice único parcial <c>ux_reservas_viagem_assento_confirmada</c> cite o literal
/// <c>'Confirmada'</c> e não dependa da ordem numérica do enum (AD-6).
/// </summary>
public enum StatusReserva
{
    Confirmada,
    Cancelada,
}
