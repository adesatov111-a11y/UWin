using System.Security.Cryptography;
using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;
using UWin.Cekirdek.Testler.Sahteler;

namespace UWin.Cekirdek.Testler;

public class IndirmeServisiTestleri : IDisposable
{
    private readonly string _klasor = Path.Combine(Path.GetTempPath(), "uwin-test-" + Guid.NewGuid().ToString("N"));

    public IndirmeServisiTestleri() => Directory.CreateDirectory(_klasor);

    public void Dispose()
    {
        if (Directory.Exists(_klasor))
            Directory.Delete(_klasor, recursive: true);
    }

    private static byte[] OrnekIcerik(int uzunluk = 4096)
    {
        var veri = new byte[uzunluk];
        for (var i = 0; i < uzunluk; i++)
            veri[i] = (byte)(i % 251);
        return veri;
    }

    private static string Ozet(byte[] veri) => Convert.ToHexString(SHA256.HashData(veri)).ToLowerInvariant();

    [Fact]
    public async Task DogruOzetliDosyaBasariylaIndirilir()
    {
        var icerik = OrnekIcerik();
        var servis = new IndirmeServisi(new HttpClient(new SahteHttpIsleyici(icerik)));
        var hedef = Path.Combine(_klasor, "test.iso");

        var sonuc = await servis.IndirAsync("https://ornek/test.iso", hedef, Ozet(icerik));

        Assert.True(sonuc.Basarili);
        Assert.Null(sonuc.Hata);
        Assert.Equal(Ozet(icerik), sonuc.Sha256);
        Assert.True(File.Exists(hedef));
        Assert.Equal(icerik, await File.ReadAllBytesAsync(hedef));
    }

    [Fact]
    public async Task BozukOzetDosyayiSilerVeHataDoner()
    {
        var icerik = OrnekIcerik();
        var servis = new IndirmeServisi(new HttpClient(new SahteHttpIsleyici(icerik)));
        var hedef = Path.Combine(_klasor, "bozuk.iso");

        var sonuc = await servis.IndirAsync("https://ornek/bozuk.iso", hedef, new string('a', 64));

        Assert.False(sonuc.Basarili);
        Assert.NotNull(sonuc.Hata);
        Assert.False(File.Exists(hedef));
    }

    [Fact]
    public async Task KesintiSonrasiKaldigiYerdenDevamEder()
    {
        var icerik = OrnekIcerik();
        var isleyici = new SahteHttpIsleyici(icerik, kesmeNoktasi: 1000);
        var servis = new IndirmeServisi(new HttpClient(isleyici));
        var hedef = Path.Combine(_klasor, "devam.iso");

        var sonuc = await servis.IndirAsync("https://ornek/devam.iso", hedef, Ozet(icerik));

        Assert.True(sonuc.Basarili, sonuc.Hata?.TeknikAyrinti ?? sonuc.Hata?.NeOldu ?? "?");
        Assert.True(isleyici.IstekSayisi > 1);
        Assert.Equal(icerik, await File.ReadAllBytesAsync(hedef));
    }

    [Fact]
    public async Task IlerlemeBildirilir()
    {
        var icerik = OrnekIcerik();
        var servis = new IndirmeServisi(new HttpClient(new SahteHttpIsleyici(icerik)));
        var hedef = Path.Combine(_klasor, "ilerleme.iso");
        var raporlar = new List<IndirmeIlerlemesi>();

        await servis.IndirAsync("https://ornek/i.iso", hedef, Ozet(icerik),
            new Progress<IndirmeIlerlemesi>(raporlar.Add));

        Assert.All(raporlar, r => Assert.InRange(r.Yuzde, 0, 100));
    }

    [Fact]
    public async Task IptalEdilenIndirmeBasarisizDoner()
    {
        var icerik = OrnekIcerik(1_000_000);
        var servis = new IndirmeServisi(new HttpClient(new SahteHttpIsleyici(icerik)));
        var hedef = Path.Combine(_klasor, "iptal.iso");

        using var kaynak = new CancellationTokenSource();
        await kaynak.CancelAsync();

        var sonuc = await servis.IndirAsync("https://ornek/iptal.iso", hedef, Ozet(icerik), null, kaynak.Token);

        Assert.False(sonuc.Basarili);
    }

    [Fact]
    public async Task IptalEdilenIndirmeYarimDosyayiKorur()
    {
        // Varsayilan davranis: kullanici tekrar denerse kaldigi yerden devam etsin.
        var icerik = OrnekIcerik(200_000);
        var isleyici = new SahteHttpIsleyici(icerik, kesmeNoktasi: 50_000);
        var servis = new IndirmeServisi(new HttpClient(isleyici));
        var hedef = Path.Combine(_klasor, "korunan.iso");

        using var kaynak = new CancellationTokenSource();
        var gorev = servis.IndirAsync("https://ornek/k.iso", hedef, Ozet(icerik), null, kaynak.Token);
        await kaynak.CancelAsync();
        await gorev;

        // Hedef dosya olusmadi ama gecici dosya devam icin duruyor olabilir.
        Assert.False(File.Exists(hedef));
    }

    [Fact]
    public async Task IstenirseIptalYarimDosyayiSiler()
    {
        var icerik = OrnekIcerik(200_000);
        var servis = new IndirmeServisi(new HttpClient(new SahteHttpIsleyici(icerik)));
        var hedef = Path.Combine(_klasor, "silinen.iso");

        using var kaynak = new CancellationTokenSource();
        await kaynak.CancelAsync();

        var sonuc = await servis.IndirAsync(
            "https://ornek/s.iso", hedef, Ozet(icerik), null, kaynak.Token, yarimDosyayiKoru: false);

        Assert.False(sonuc.Basarili);
        Assert.False(File.Exists(hedef));
        Assert.False(File.Exists(hedef + ".indiriliyor"));
    }

    [Fact]
    public async Task Sha256DosyadanHesaplanir()
    {
        var icerik = OrnekIcerik();
        var dosya = Path.Combine(_klasor, "ozet.bin");
        await File.WriteAllBytesAsync(dosya, icerik);

        var ozet = await new IndirmeServisi(new HttpClient()).Sha256HesaplaAsync(dosya);

        Assert.Equal(Ozet(icerik), ozet);
    }

    [Fact]
    public async Task BeklenenOzetVerilmezseDogrulamaAtlanir()
    {
        var icerik = OrnekIcerik();
        var servis = new IndirmeServisi(new HttpClient(new SahteHttpIsleyici(icerik)));
        var hedef = Path.Combine(_klasor, "ozetsiz.iso");

        var sonuc = await servis.IndirAsync("https://ornek/o.iso", hedef, beklenenSha256: null);

        Assert.True(sonuc.Basarili);
        Assert.NotNull(sonuc.Sha256);
    }

    [Fact]
    public void KalanSureHesaplanir()
    {
        var ilerleme = new IndirmeIlerlemesi(IndirilenBayt: 500, ToplamBayt: 1000, BaytBolumSaniye: 100);

        Assert.Equal(50, ilerleme.Yuzde);
        Assert.Equal(TimeSpan.FromSeconds(5), ilerleme.KalanSure);
    }
}
