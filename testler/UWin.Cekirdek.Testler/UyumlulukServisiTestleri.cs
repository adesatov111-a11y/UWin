using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Uyumluluk raporunun asil isi "kurulamaz" demek degil, kullaniciya
/// ne yapabilecegini soylemektir. Testler bu ayrimi korur.
/// </summary>
public class UyumlulukServisiTestleri
{
    private static DonanimRaporu Rapor(
        string? tpm = "2.0",
        bool uefi = true,
        bool secureBoot = true,
        long ram = 16L * 1024 * 1024 * 1024,
        long disk = 512L * 1024 * 1024 * 1024,
        string islemci = "AMD Ryzen 5 5600 6-Core Processor")
        => new(
            AnakartUretici: "Gigabyte",
            AnakartModel: "A520M K V2",
            Islemci: islemci,
            RamBayt: ram,
            TpmSurumu: tpm,
            UefiMi: uefi,
            SecureBootDestekli: secureBoot,
            SistemDiskiBoyutBayt: disk);

    private static UyumlulukMaddesi Madde(UyumlulukRaporu rapor, string ad)
        => rapor.Maddeler.Single(m => m.Ad == ad);

    [Fact]
    public void TumGereksinimlerKarsilaninceUyumluDoner()
    {
        var rapor = new UyumlulukServisi().Degerlendir(Rapor());

        Assert.True(rapor.Uyumlu);
        Assert.All(rapor.Maddeler, m => Assert.Equal(UyumlulukDurumu.Gecti, m.Durum));
    }

    [Fact]
    public void HerGereksinimIcinBirMaddeUretir()
    {
        var rapor = new UyumlulukServisi().Degerlendir(Rapor());

        // Bes gereksinim: TPM, Secure Boot, UEFI, RAM, disk.
        Assert.Equal(5, rapor.Maddeler.Count);
    }

    /// <summary>
    /// En degerli ayrim: UEFI acikken TPM gorunmuyorsa, TPM buyuk
    /// olasilikla anakartta var ve BIOS'ta kapali. Bu makineye "Windows 11
    /// kurulamaz" demek yanlis olur.
    /// </summary>
    [Fact]
    public void UefiVarkenTpmYoksaBiostanAcilabilirSayilir()
    {
        var rapor = new UyumlulukServisi().Degerlendir(Rapor(tpm: null, uefi: true));

        var tpm = Madde(rapor, "TPM 2.0");

        Assert.Equal(UyumlulukDurumu.AcilabilirDurumda, tpm.Durum);
        Assert.NotNull(tpm.NeYapmali);
        Assert.NotNull(tpm.BiosAyarAdi);
    }

    /// <summary>
    /// Eski BIOS'ta (UEFI yok) TPM'in BIOS'tan acilabilecegini soylemek
    /// bos umut olur; o makinelerde gercekten yoktur.
    /// </summary>
    [Fact]
    public void EskiBiostaTpmYoksaKaldiSayilir()
    {
        var rapor = new UyumlulukServisi().Degerlendir(Rapor(tpm: null, uefi: false));

        Assert.Equal(UyumlulukDurumu.Kaldi, Madde(rapor, "TPM 2.0").Durum);
    }

    [Fact]
    public void Tpm12BulunursaKaldiSayilir()
    {
        // TPM 1.2 gercek bir yongadir ama Windows 11 kabul etmez ve
        // bir ayarla 2.0 olmaz.
        var rapor = new UyumlulukServisi().Degerlendir(Rapor(tpm: "1.2"));

        Assert.Equal(UyumlulukDurumu.Kaldi, Madde(rapor, "TPM 2.0").Durum);
    }

    [Fact]
    public void UefiVarkenSecureBootKapaliysaBiostanAcilabilir()
    {
        var rapor = new UyumlulukServisi().Degerlendir(Rapor(secureBoot: false, uefi: true));

        var sb = Madde(rapor, "Secure Boot");

        Assert.Equal(UyumlulukDurumu.AcilabilirDurumda, sb.Durum);
        Assert.NotNull(sb.BiosAyarAdi);
    }

    [Fact]
    public void RamYetersizseKaldiSayilir()
    {
        // RAM bir BIOS ayariyla artmaz; tek cozum takmaktir.
        var rapor = new UyumlulukServisi().Degerlendir(Rapor(ram: 2L * 1024 * 1024 * 1024));

        var ram = Madde(rapor, "RAM");

        Assert.Equal(UyumlulukDurumu.Kaldi, ram.Durum);
        Assert.NotNull(ram.NeYapmali);
    }

    [Fact]
    public void DiskYetersizseKaldiSayilir()
    {
        var rapor = new UyumlulukServisi().Degerlendir(Rapor(disk: 32L * 1024 * 1024 * 1024));

        Assert.Equal(UyumlulukDurumu.Kaldi, Madde(rapor, "Disk").Durum);
    }

    [Fact]
    public void EskiBiosUefiMaddesiniKaldiYapar()
    {
        var rapor = new UyumlulukServisi().Degerlendir(Rapor(uefi: false));

        Assert.Equal(UyumlulukDurumu.Kaldi, Madde(rapor, "UEFI").Durum);
    }

    /// <summary>
    /// Kullanicinin gormesi gereken en onemli sonuc: eksiklerin hepsi
    /// BIOS'tan aciliyorsa bu makineye Windows 11 kurulabilir.
    /// </summary>
    [Fact]
    public void HepsiBiostanAcilabiliyorsaBiosAyariylaCozulurDoner()
    {
        var rapor = new UyumlulukServisi().Degerlendir(
            Rapor(tpm: null, secureBoot: false, uefi: true));

        Assert.False(rapor.Uyumlu);
        Assert.True(rapor.BiosAyariylaCozulur);
        Assert.Equal(2, rapor.BiostanAcilabilirler.Count);
        Assert.Empty(rapor.Kalanlar);
    }

    [Fact]
    public void DonanimEksigiVarsaBiosAyariylaCozulmez()
    {
        var rapor = new UyumlulukServisi().Degerlendir(
            Rapor(tpm: null, ram: 2L * 1024 * 1024 * 1024, uefi: true));

        Assert.False(rapor.BiosAyariylaCozulur);
        Assert.NotEmpty(rapor.Kalanlar);
    }

    /// <summary>
    /// BIOS ayar adi markaya gore degisir; Gigabyte'ta TPM'in adi
    /// "AMD fTPM Switch"tir. Genel bir ad soylemek kullaniciyi BIOS'ta
    /// olmayan bir menuyu ararken birakiyor.
    /// </summary>
    [Fact]
    public void BiosAyarAdiMarkayaGoreDegisir()
    {
        var servis = new UyumlulukServisi();

        var gigabyte = servis.Degerlendir(Rapor(tpm: null)).Maddeler
            .Single(m => m.Ad == "TPM 2.0").BiosAyarAdi;

        var intelli = servis.Degerlendir(
                Rapor(tpm: null, islemci: "Intel(R) Core(TM) i5-10400 CPU @ 2.90GHz"))
            .Maddeler.Single(m => m.Ad == "TPM 2.0").BiosAyarAdi;

        Assert.NotEqual(gigabyte, intelli);
    }

    [Fact]
    public void MaddelerBosDonanimdaCokmez()
    {
        var bos = new DonanimRaporu("", "", "", 0, null, false, false, 0);

        var rapor = new UyumlulukServisi().Degerlendir(bos);

        Assert.False(rapor.Uyumlu);
        Assert.Equal(5, rapor.Maddeler.Count);
    }
}
