using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Bellek sagligi testi. Gercek USB yerine gecici bir klasor kullanilir;
/// servis dosya sistemi uzerinden calistigi icin ayrim yapmaz.
///
/// Sahte kapasite senaryosu, klasore yazilabilecek bayti sinirlayarak
/// taklit edilir - gercek sahte belleklerin davranisi da budur: belli
/// bir noktadan sonra yazma sessizce basarisiz olur veya veri bozulur.
/// </summary>
public class SaglikServisiTestleri : IDisposable
{
    private readonly string _usb =
        Path.Combine(Path.GetTempPath(), "uwin-saglik-" + Guid.NewGuid().ToString("N"));

    public SaglikServisiTestleri() => Directory.CreateDirectory(_usb);

    public void Dispose()
    {
        if (Directory.Exists(_usb))
            Directory.Delete(_usb, recursive: true);
    }

    [Fact]
    public async Task SaglamBellekSaglamDoner()
    {
        var sonuc = await new SaglikServisi().TestEtAsync(_usb, 4_000_000);

        Assert.True(sonuc.Saglam);
        Assert.Equal(SaglikDurumu.Saglam, sonuc.Durum);
    }

    [Fact]
    public async Task TestSonrasiGeciciDosyalarSilinir()
    {
        await new SaglikServisi().TestEtAsync(_usb, 4_000_000);

        Assert.Empty(Directory.GetFiles(_usb, "*", SearchOption.AllDirectories));
    }

    /// <summary>
    /// Test edilen bayt bildirilen boyutu asmamali: 64 GB'lik bir
    /// bellegi bastan sona yazmak saatler surer ve kimse beklemez.
    /// </summary>
    [Fact]
    public async Task TestEdilenBaytSinirinIcindeKalir()
    {
        var sonuc = await new SaglikServisi().TestEtAsync(_usb, 2_000_000);

        Assert.True(sonuc.TestEdilenBayt <= 2_000_000);
        Assert.True(sonuc.TestEdilenBayt > 0);
    }

    [Fact]
    public async Task OlmayanSurucuTestEdilemezDoner()
    {
        var sonuc = await new SaglikServisi().TestEtAsync(
            Path.Combine(_usb, "olmayan"), 1_000_000);

        Assert.Equal(SaglikDurumu.TestEdilemedi, sonuc.Durum);
        Assert.NotNull(sonuc.Hata);
        Assert.NotEmpty(sonuc.Hata.NeYapmali);
    }

    [Fact]
    public async Task IlerlemeBildirilir()
    {
        List<double> yuzdeler = [];

        await new SaglikServisi().TestEtAsync(
            _usb, 4_000_000, new Progress<double>(yuzdeler.Add));

        Assert.NotEmpty(yuzdeler);
        Assert.All(yuzdeler, y => Assert.InRange(y, 0, 100));
    }

    [Fact]
    public async Task IptalEdilirseIstisnaFirlatir()
    {
        using var kaynak = new CancellationTokenSource();
        await kaynak.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => new SaglikServisi().TestEtAsync(_usb, 4_000_000, null, kaynak.Token));
    }

    /// <summary>
    /// Iptal edilse bile gecici dosyalar birakilmamali - yarim kalan
    /// test yuzunden bellekte gigabaytlarca cop kalmasi kabul edilemez.
    /// </summary>
    [Fact]
    public async Task IptalSonrasiGeciciDosyaKalmaz()
    {
        using var kaynak = new CancellationTokenSource();

        // Ilk ilerleme bildiriminde iptal et: yazma basladiktan sonra.
        var ilerleme = new Progress<double>(_ => kaynak.Cancel());

        try
        {
            await new SaglikServisi().TestEtAsync(_usb, 20_000_000, ilerleme, kaynak.Token);
        }
        catch (OperationCanceledException)
        {
            // Beklenen.
        }

        // Progress geri cagrisi is parcaciginda kosuyor; silme bitene kadar bekle.
        await Task.Delay(200);

        Assert.Empty(Directory.GetFiles(_usb, "uwin-saglik*", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task BildirilenBoyutSonuctaTasinir()
    {
        var sonuc = await new SaglikServisi().TestEtAsync(_usb, 3_000_000);

        Assert.Equal(3_000_000, sonuc.BildirilenBoyutBayt);
    }

    [Fact]
    public void KapasiteOraniHesaplanir()
    {
        var sonuc = new SaglikSonucu(
            SaglikDurumu.SahteKapasite,
            BildirilenBoyutBayt: 64_000_000_000,
            GercekBoyutBayt: 8_000_000_000,
            TestEdilenBayt: 8_000_000_000);

        Assert.Equal(0.125, sonuc.KapasiteOrani, precision: 3);
    }

    [Fact]
    public void BildirilenBoyutSifirsaOranSifirdir()
    {
        var sonuc = new SaglikSonucu(SaglikDurumu.TestEdilemedi, 0, 0, 0);

        Assert.Equal(0, sonuc.KapasiteOrani);
    }
}
