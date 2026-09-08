using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;
using UWin.Cekirdek.Testler.Sahteler;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// ISO taninmasi ozet yerine icerige bakar. Bu testler asil olarak sunu
/// korur: gecerli bir Windows ISO'su asla "tanimadik" diye isaretlenmemeli.
/// </summary>
public class IsoTanimaServisiTestleri
{
    private static IsoTanimaServisi Servis(SahteIsoOkuyucu okuyucu) => new(okuyucu);

    [Fact]
    public void GercekWindowsIsosuTaninir()
    {
        var kimlik = Servis(SahteIsoOkuyucu.KucukWimli()).Tani("herhangi.iso");

        Assert.True(kimlik.WindowsMu);
        Assert.NotEqual(IsoTanimaDurumu.WindowsDegil, kimlik.Durum);
    }

    [Fact]
    public void KurulumDosyalariYoksaWindowsDegilDoner()
    {
        var okuyucu = new SahteIsoOkuyucu()
            .DosyaEkle("belgeler/okuma.txt", 1_000)
            .DosyaEkle("muzik/parca.mp3", 4_000_000);

        var kimlik = Servis(okuyucu).Tani("baska.iso");

        Assert.Equal(IsoTanimaDurumu.WindowsDegil, kimlik.Durum);
        Assert.False(kimlik.WindowsMu);
    }

    [Fact]
    public void SurumBilinmeseBileWindowsSayilir()
    {
        // setup.exe + sources/install.wim var ama surum dosyasi yok.
        var okuyucu = new SahteIsoOkuyucu()
            .DosyaEkle("setup.exe", 100_000)
            .DosyaEkle("sources/install.wim", 3_000_000_000);

        var kimlik = Servis(okuyucu).Tani("eski.iso");

        Assert.True(kimlik.WindowsMu);
    }

    [Fact]
    public void BuyukWimIcinGerekenBoyutHesaplanir()
    {
        var kimlik = Servis(SahteIsoOkuyucu.BuyukWimli()).Tani("buyuk.iso");

        // 5,2 GB WIM + boot dosyalari 4 GB'lik bir USB'ye asla sigmaz.
        Assert.True(kimlik.GerekenUsbBoyutuBayt > 4L * 1000 * 1000 * 1000);
    }

    [Fact]
    public void GerekenBoyutIcerigeGoreDegisir()
    {
        var kucuk = Servis(SahteIsoOkuyucu.KucukWimli()).Tani("k.iso");
        var buyuk = Servis(SahteIsoOkuyucu.BuyukWimli()).Tani("b.iso");

        Assert.True(buyuk.GerekenUsbBoyutuBayt > kucuk.GerekenUsbBoyutuBayt);
    }

    [Fact]
    public void GerekenBoyutIcerikToplamindanKucukOlamaz()
    {
        var okuyucu = SahteIsoOkuyucu.KucukWimli();
        var toplam = okuyucu.Oku("x.iso").Dosyalar.Sum(d => d.BoyutBayt);

        var kimlik = Servis(okuyucu).Tani("x.iso");

        Assert.True(kimlik.GerekenUsbBoyutuBayt >= toplam);
    }

    [Fact]
    public void AcilamayanDosyaCokmeYerineOkunamadiDoner()
    {
        var kimlik = new IsoTanimaServisi(new PatlayanIsoOkuyucu()).Tani("bozuk.iso");

        Assert.Equal(IsoTanimaDurumu.Okunamadi, kimlik.Durum);
        Assert.False(kimlik.WindowsMu);
    }

    /// <summary>ISO acilamadiginda servis cokmemeli, durum dondurmeli.</summary>
    private sealed class PatlayanIsoOkuyucu : Arayuzler.IIsoOkuyucu
    {
        public IsoIcerigi Oku(string isoYolu) => throw new IOException("acilamadi");

        public Task DosyaCikarAsync(string i, string g, string h, CancellationToken t = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<string>> WimBolAsync(
            string w, string h, long p, IProgress<double>? il = null, CancellationToken t = default)
            => Task.FromResult<IReadOnlyList<string>>([]);
    }
}
