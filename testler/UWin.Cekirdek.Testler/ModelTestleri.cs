using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Testler;

public class ModelTestleri
{
    private static DiskBilgisi Disk(DiskSinifi sinif) =>
        new(DiskNumarasi: 1, Ad: "Test", Model: "TestModel", BoyutBayt: 32_000_000_000,
            Sinif: sinif, SeriNumarasi: "SN1", KullanilanBayt: 0);

    [Theory]
    [InlineData(DiskSinifi.UsbBellek, true)]
    [InlineData(DiskSinifi.HariciDisk, true)]
    [InlineData(DiskSinifi.DahiliDisk, true)]
    [InlineData(DiskSinifi.SistemDiski, false)]
    public void SistemDiskiHicbirZamanYazilabilirDegil(DiskSinifi sinif, bool beklenen)
    {
        Assert.Equal(beklenen, Disk(sinif).YazilabilirMi);
    }

    [Fact]
    public void HataUcluBilgiTasir()
    {
        var hata = new UWinHatasi(
            NeOldu: "USB'ye yazilamadi.",
            Neden: "Aygit yazma korumali olabilir.",
            NeYapmali: "Bellegin yanindaki kucuk kilit anahtarini kontrol et.");

        Assert.NotEmpty(hata.NeOldu);
        Assert.NotEmpty(hata.Neden);
        Assert.NotEmpty(hata.NeYapmali);
        Assert.Null(hata.TeknikAyrinti);
    }

    [Fact]
    public void KalanSureHizVeKalanBayttanHesaplanir()
    {
        // 100 MB kaldi, saniyede 10 MB -> 10 saniye.
        var ilerleme = new YazmaIlerlemesi(
            YazmaAdimi.DosyaKopyalama, 50, 70, "...",
            YazilanBayt: 100_000_000,
            ToplamBayt: 200_000_000,
            BaytBolumSaniye: 10_000_000);

        Assert.Equal(10, ilerleme.KalanSure!.Value.TotalSeconds, precision: 1);
    }

    /// <summary>
    /// Hiz olculemediyse tahmin gosterilmez. Yanlis bir tahmin, hic
    /// tahmin olmamasindan kotudur: bir kere "2 dakika" deyip 20 dakika
    /// bekleten program bir daha inandirici olmuyor.
    /// </summary>
    [Fact]
    public void HizBilinmiyorsaKalanSureNullDoner()
    {
        var ilerleme = new YazmaIlerlemesi(
            YazmaAdimi.DosyaKopyalama, 50, 70, "...",
            YazilanBayt: 100, ToplamBayt: 200, BaytBolumSaniye: 0);

        Assert.Null(ilerleme.KalanSure);
    }

    [Fact]
    public void ToplamBilinmiyorsaKalanSureNullDoner()
    {
        var ilerleme = new YazmaIlerlemesi(
            YazmaAdimi.DosyaKopyalama, 50, 70, "...",
            YazilanBayt: 100, ToplamBayt: 0, BaytBolumSaniye: 5);

        Assert.Null(ilerleme.KalanSure);
    }

    [Fact]
    public void KopyalamaBittigindeKalanSureNullDoner()
    {
        var ilerleme = new YazmaIlerlemesi(
            YazmaAdimi.DosyaKopyalama, 100, 95, "...",
            YazilanBayt: 200, ToplamBayt: 200, BaytBolumSaniye: 5);

        Assert.Null(ilerleme.KalanSure);
    }

    [Fact]
    public void IlerlemeBaytSayaclariVarsayilanSifirdir()
    {
        // Eski cagrilar (bayt vermeyenler) bozulmamali.
        var ilerleme = new YazmaIlerlemesi(YazmaAdimi.Kilitleme, 100, 10, "...");

        Assert.Equal(0, ilerleme.ToplamBayt);
        Assert.Null(ilerleme.KalanSure);
    }
}
