using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Yazma sonrasi geri okuma testleri. Gercek USB yerine gecici bir
/// klasor kullanilir; dogrulama servisi zaten dosya sistemi uzerinden
/// calisir, bu yuzden ayrim yapmaz.
/// </summary>
public class DogrulamaServisiTestleri : IDisposable
{
    private readonly string _usb =
        Path.Combine(Path.GetTempPath(), "uwin-dogrulama-" + Guid.NewGuid().ToString("N"));

    public DogrulamaServisiTestleri() => Directory.CreateDirectory(_usb);

    public void Dispose()
    {
        if (Directory.Exists(_usb))
            Directory.Delete(_usb, recursive: true);
    }

    /// <summary>USB'ye dosya yazar; icerik verilmezse boyut kadar sifir yazilir.</summary>
    private void UsbyeYaz(string goreliYol, long boyut, byte dolgu = 0)
    {
        var tam = Path.Combine(_usb, goreliYol.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(tam)!);

        var veri = new byte[boyut];
        Array.Fill(veri, dolgu);
        File.WriteAllBytes(tam, veri);
    }

    private static IsoIcerigi Icerik(params (string Yol, long Boyut)[] dosyalar)
        => new(dosyalar.Select(d => new IsoDosyasi(d.Yol, d.Boyut)).ToList());

    [Fact]
    public async Task TumDosyalarDogruBoyuttaysaBasariliDoner()
    {
        UsbyeYaz("bootmgr", 400);
        UsbyeYaz("sources/install.wim", 5000);

        var sonuc = await new DogrulamaServisi().DogrulaAsync(
            _usb, Icerik(("bootmgr", 400), ("sources/install.wim", 5000)));

        Assert.True(sonuc.Basarili);
        Assert.Empty(sonuc.Sorunlular);
    }

    [Fact]
    public async Task EksikDosyaYakalanir()
    {
        UsbyeYaz("bootmgr", 400);

        var sonuc = await new DogrulamaServisi().DogrulaAsync(
            _usb, Icerik(("bootmgr", 400), ("sources/install.wim", 5000)));

        Assert.False(sonuc.Basarili);

        var sorun = Assert.Single(sonuc.Sorunlular);
        Assert.Equal("sources/install.wim", sorun.GoreliYol);
        Assert.Equal(DosyaDogrulamaDurumu.Eksik, sorun.Durum);
    }

    /// <summary>
    /// Kopyalama yarim kalirsa dosya var ama kisadir. Bu, USB dolunca
    /// veya yazma sirasinda cekilince olur.
    /// </summary>
    [Fact]
    public async Task KisaKalanDosyaYakalanir()
    {
        UsbyeYaz("sources/install.wim", 3000);

        var sonuc = await new DogrulamaServisi().DogrulaAsync(
            _usb, Icerik(("sources/install.wim", 5000)));

        var sorun = Assert.Single(sonuc.Sorunlular);
        Assert.Equal(DosyaDogrulamaDurumu.BoyutTutmuyor, sorun.Durum);
    }

    /// <summary>
    /// WIM bolunmusse USB'de install.wim yerine install.swm parcalari
    /// olur. Bunu eksik dosya saymak, dogru yazilmis her USB'yi bozuk
    /// gostermek olurdu.
    /// </summary>
    [Fact]
    public async Task BolunmusWimEksikSayilmaz()
    {
        UsbyeYaz("bootmgr", 400);
        UsbyeYaz("sources/install.swm", 3000);
        UsbyeYaz("sources/install2.swm", 2000);

        var sonuc = await new DogrulamaServisi().DogrulaAsync(
            _usb, Icerik(("bootmgr", 400), ("sources/install.wim", 5000)));

        Assert.True(sonuc.Basarili);
    }

    [Fact]
    public async Task BolunmusWimParcasiEksikseYakalanir()
    {
        UsbyeYaz("bootmgr", 400);

        var sonuc = await new DogrulamaServisi().DogrulaAsync(
            _usb, Icerik(("bootmgr", 400), ("sources/install.wim", 5000)));

        Assert.False(sonuc.Basarili);
    }

    [Fact]
    public async Task IlerlemeBildirilir()
    {
        UsbyeYaz("bootmgr", 400);
        UsbyeYaz("boot/bcd", 200);
        UsbyeYaz("sources/install.wim", 5000);

        List<double> yuzdeler = [];

        await new DogrulamaServisi().DogrulaAsync(
            _usb,
            Icerik(("bootmgr", 400), ("boot/bcd", 200), ("sources/install.wim", 5000)),
            new Progress<double>(yuzdeler.Add));

        // Progress<T> is parcaciklarina siralar; en az bir bildirim yeterli.
        Assert.NotEmpty(yuzdeler);
    }

    [Fact]
    public async Task SurucuYokduysaTamamlanamadiDoner()
    {
        var sonuc = await new DogrulamaServisi().DogrulaAsync(
            Path.Combine(_usb, "olmayan-klasor"), Icerik(("bootmgr", 400)));

        Assert.False(sonuc.Basarili);
        Assert.False(sonuc.Tamamlanabildi);
    }

    [Fact]
    public async Task IptalEdilirseIstisnaFirlatir()
    {
        UsbyeYaz("bootmgr", 400);

        using var kaynak = new CancellationTokenSource();
        await kaynak.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new DogrulamaServisi().DogrulaAsync(
                _usb, Icerik(("bootmgr", 400)), null, kaynak.Token));
    }

    /// <summary>
    /// UWin'in USB'ye kendi yazdigi dosyalar (rehber, isaret) ISO'da yok.
    /// Bunlarin varligi hata degildir; dogrulama yalnizca ISO'dan gelen
    /// dosyalara bakar.
    /// </summary>
    [Fact]
    public async Task UsbdekiFazladanDosyaSorunSayilmaz()
    {
        UsbyeYaz("bootmgr", 400);
        UsbyeYaz("UWin-Rehber.html", 9000);

        var sonuc = await new DogrulamaServisi().DogrulaAsync(_usb, Icerik(("bootmgr", 400)));

        Assert.True(sonuc.Basarili);
    }
}
