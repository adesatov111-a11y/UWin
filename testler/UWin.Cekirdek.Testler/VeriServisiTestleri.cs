using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Kurulumda gidecek verilerin raporu. Gercek kullanici klasorleri
/// yerine gecici bir agac kullanilir; servis yol listesini disaridan
/// aldigi icin ayrim yapmaz.
/// </summary>
public class VeriServisiTestleri : IDisposable
{
    private readonly string _kok =
        Path.Combine(Path.GetTempPath(), "uwin-veri-" + Guid.NewGuid().ToString("N"));

    public VeriServisiTestleri() => Directory.CreateDirectory(_kok);

    public void Dispose()
    {
        if (Directory.Exists(_kok))
            Directory.Delete(_kok, recursive: true);
    }

    private string KlasorKur(string ad, params (string Dosya, int Boyut)[] dosyalar)
    {
        var klasor = Path.Combine(_kok, ad);
        Directory.CreateDirectory(klasor);

        foreach (var (dosya, boyut) in dosyalar)
        {
            var yol = Path.Combine(klasor, dosya);
            Directory.CreateDirectory(Path.GetDirectoryName(yol)!);
            File.WriteAllBytes(yol, new byte[boyut]);
        }

        return klasor;
    }

    [Fact]
    public async Task DosyaSayisiVeBoyutDogruHesaplanir()
    {
        var masaustu = KlasorKur("Masaustu", ("a.txt", 100), ("b.txt", 200));

        var rapor = await new VeriServisi().RaporAlAsync([("Masaüstü", masaustu)]);

        var klasor = Assert.Single(rapor.Klasorler);
        Assert.Equal(2, klasor.DosyaSayisi);
        Assert.Equal(300, klasor.BoyutBayt);
    }

    [Fact]
    public async Task AltKlasorlerDeSayilir()
    {
        var belgeler = KlasorKur("Belgeler",
            ("ust.txt", 50),
            (Path.Combine("alt", "ic.txt"), 150));

        var rapor = await new VeriServisi().RaporAlAsync([("Belgeler", belgeler)]);

        Assert.Equal(2, rapor.ToplamDosya);
        Assert.Equal(200, rapor.ToplamBayt);
    }

    [Fact]
    public async Task BosKlasorVeriVarSayilmaz()
    {
        var bos = KlasorKur("Bos");

        var rapor = await new VeriServisi().RaporAlAsync([("Boş", bos)]);

        Assert.False(rapor.VeriVar);
        Assert.Empty(rapor.DoluKlasorler);
    }

    [Fact]
    public async Task BirdenFazlaKlasorToplanir()
    {
        var a = KlasorKur("A", ("1.txt", 100));
        var b = KlasorKur("B", ("2.txt", 400));

        var rapor = await new VeriServisi().RaporAlAsync([("A", a), ("B", b)]);

        Assert.Equal(2, rapor.Klasorler.Count);
        Assert.Equal(500, rapor.ToplamBayt);
        Assert.True(rapor.VeriVar);
    }

    /// <summary>
    /// Olmayan klasor hata degildir: her bilgisayarda OneDrive veya
    /// Videolar klasoru bulunmaz. O satir sifir dosyayla gecilir.
    /// </summary>
    [Fact]
    public async Task OlmayanKlasorCokmez()
    {
        var yok = Path.Combine(_kok, "hicboyle-bir-klasor-yok");

        var rapor = await new VeriServisi().RaporAlAsync([("Yok", yok)]);

        var klasor = Assert.Single(rapor.Klasorler);
        Assert.Equal(0, klasor.DosyaSayisi);
        Assert.False(rapor.VeriVar);
    }

    [Fact]
    public async Task DoluKlasorlerBosOlanlariEler()
    {
        var dolu = KlasorKur("Dolu", ("x.txt", 10));
        var bos = KlasorKur("Bos2");

        var rapor = await new VeriServisi().RaporAlAsync([("Dolu", dolu), ("Boş", bos)]);

        Assert.Equal(2, rapor.Klasorler.Count);
        var tek = Assert.Single(rapor.DoluKlasorler);
        Assert.Equal("Dolu", tek.Ad);
    }

    [Fact]
    public async Task IptalEdilirseIstisnaFirlatir()
    {
        var klasor = KlasorKur("Iptal", ("a.txt", 10));

        using var kaynak = new CancellationTokenSource();
        await kaynak.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new VeriServisi().RaporAlAsync([("Iptal", klasor)], kaynak.Token));
    }

    /// <summary>
    /// Varsayilan klasor listesi bu makinenin gercek kullanici
    /// klasorlerini gosterir; en azindan Masaustu ve Belgeler olmali.
    /// </summary>
    [Fact]
    public void VarsayilanKlasorlerMasaustuVeBelgeleriIcerir()
    {
        var klasorler = VeriServisi.VarsayilanKlasorler();

        Assert.Contains(klasorler, k => k.Yol == Environment.GetFolderPath(
            Environment.SpecialFolder.DesktopDirectory));
        Assert.Contains(klasorler, k => k.Yol == Environment.GetFolderPath(
            Environment.SpecialFolder.MyDocuments));
    }

    [Fact]
    public void VarsayilanKlasorlerinAdlariBostuDegildir()
    {
        Assert.All(VeriServisi.VarsayilanKlasorler(), k => Assert.NotEmpty(k.Ad));
    }
}
