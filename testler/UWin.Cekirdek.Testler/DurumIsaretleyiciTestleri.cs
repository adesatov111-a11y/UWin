using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

public class DurumIsaretleyiciTestleri : IDisposable
{
    private readonly string _klasor =
        Path.Combine(Path.GetTempPath(), "uwin-isaret-" + Guid.NewGuid().ToString("N"));

    public DurumIsaretleyiciTestleri() => Directory.CreateDirectory(_klasor);

    public void Dispose()
    {
        if (Directory.Exists(_klasor))
            Directory.Delete(_klasor, recursive: true);
    }

    private static YazmaDurumIsareti Isaret() =>
        new("SN-1", DateTimeOffset.UtcNow, "Windows 11 24H2");

    [Fact]
    public async Task BirakilanIsaretGeriOkunur()
    {
        var servis = new DurumIsaretleyici();

        await servis.IsaretBirakAsync(_klasor, Isaret());
        var okunan = await servis.IsaretOkuAsync(_klasor);

        Assert.NotNull(okunan);
        Assert.Equal("SN-1", okunan!.DiskSeriNumarasi);
        Assert.Equal("Windows 11 24H2", okunan.SurumAdi);
    }

    [Fact]
    public async Task IsaretsizSurucudeNullDoner()
    {
        Assert.Null(await new DurumIsaretleyici().IsaretOkuAsync(_klasor));
    }

    [Fact]
    public async Task SilinenIsaretArtikOkunmaz()
    {
        var servis = new DurumIsaretleyici();
        await servis.IsaretBirakAsync(_klasor, Isaret());

        await servis.IsaretSilAsync(_klasor);

        Assert.Null(await servis.IsaretOkuAsync(_klasor));
    }

    [Fact]
    public async Task OlmayanIsaretiSilmekHataVermez()
    {
        await new DurumIsaretleyici().IsaretSilAsync(_klasor);
    }

    [Fact]
    public async Task BozukIsaretDosyasiNullDoner()
    {
        await File.WriteAllTextAsync(Path.Combine(_klasor, ".uwin-yarim"), "bu gecerli json degil {{{");

        Assert.Null(await new DurumIsaretleyici().IsaretOkuAsync(_klasor));
    }

    [Fact]
    public async Task OlmayanSurucudeOkumaCokmez()
    {
        var yok = Path.Combine(_klasor, "olmayan-klasor");

        Assert.Null(await new DurumIsaretleyici().IsaretOkuAsync(yok));
    }

    [Fact]
    public async Task IsaretDosyasiGizlidir()
    {
        await new DurumIsaretleyici().IsaretBirakAsync(_klasor, Isaret());

        var nitelikler = File.GetAttributes(Path.Combine(_klasor, ".uwin-yarim"));

        Assert.True(nitelikler.HasFlag(FileAttributes.Hidden));
    }
}
