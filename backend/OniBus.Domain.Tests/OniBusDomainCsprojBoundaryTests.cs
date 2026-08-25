using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace OniBus.Domain.Tests;

/// <summary>
/// AD-1: <c>OniBus.Domain.csproj</c> deve permanecer um projeto puro — a fronteira é o próprio
/// arquivo do projeto, não apenas disciplina de code review. Este teste falha assim que alguém
/// adicionar a primeira dependência.
/// </summary>
public class OniBusDomainCsprojBoundaryTests
{
    private static string ResolveDomainCsprojPath([CallerFilePath] string thisFilePath = "")
    {
        var testsProjectDir = Path.GetDirectoryName(thisFilePath)!;
        return Path.GetFullPath(Path.Combine(testsProjectDir, "..", "OniBus.Domain", "OniBus.Domain.csproj"));
    }

    [Fact]
    public void OniBus_Domain_csproj_nao_tem_nenhum_PackageReference_ou_ProjectReference()
    {
        var csprojPath = ResolveDomainCsprojPath();
        Assert.True(File.Exists(csprojPath), $"Esperava encontrar o csproj em {csprojPath}");

        var project = XDocument.Load(csprojPath);

        Assert.Empty(project.Descendants("PackageReference"));
        Assert.Empty(project.Descendants("ProjectReference"));
        // Reference/FrameworkReference são outras duas formas de introduzir dependência que o
        // enunciado de AD-1 ("qualquer adição a esse arquivo é violação de arquitetura") também
        // proíbe, mesmo não sendo PackageReference/ProjectReference.
        Assert.Empty(project.Descendants("Reference"));
        Assert.Empty(project.Descendants("FrameworkReference"));
    }

    [Fact]
    public void OniBus_Domain_csproj_usa_o_Sdk_puro_Microsoft_NET_Sdk()
    {
        var csprojPath = ResolveDomainCsprojPath();
        var project = XDocument.Load(csprojPath);

        // Microsoft.NET.Sdk.Web (ou qualquer outro Sdk especializado) traria o shared framework
        // correspondente embutido, sem que isso apareça como PackageReference/ProjectReference —
        // a mesma violação de AD-1 por uma porta diferente.
        Assert.Equal("Microsoft.NET.Sdk", project.Root!.Attribute("Sdk")?.Value);
    }
}
