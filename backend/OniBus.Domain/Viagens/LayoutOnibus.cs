namespace OniBus.Domain.Viagens;

/// <summary>
/// Disposição fixa dos Assentos de uma Viagem: 11 fileiras de 4 (2+2, corredor central),
/// idêntica para toda Viagem no MVP (AD-4). Não existe tabela de Assentos — esta constante,
/// combinada com as Reservas Confirmadas de uma Viagem, é a única fonte da disponibilidade.
/// Consumida por stories futuras (mapa de Assentos, validação de numero_assento); esta story
/// não deriva nenhum estado de Assento ainda.
/// </summary>
public static class LayoutOnibus
{
    public const int TotalAssentos = 44;
}
