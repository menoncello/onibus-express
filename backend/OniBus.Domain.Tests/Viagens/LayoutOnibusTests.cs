using OniBus.Domain.Viagens;

namespace OniBus.Domain.Tests.Viagens;

public class LayoutOnibusTests
{
    [Fact]
    public void Layout_do_onibus_tem_44_assentos()
    {
        Assert.Equal(44, LayoutOnibus.TotalAssentos);
    }
}
