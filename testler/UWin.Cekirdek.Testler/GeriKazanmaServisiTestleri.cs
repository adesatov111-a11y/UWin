using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;
using UWin.Cekirdek.Testler.Sahteler;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Geri kazanma da yikici bir islemdir ve YazmaServisi ile ayni
/// korumalari tasimak zorundadir. Bu testlerin yarisi ozellikle
/// "yanlis diski silmesin" diye vardir.
/// </summary>
public class GeriKazanmaServisiTestleri
{
    private static HamDiskGirisi HamUsb(int no = 1) => new(
        DiskNumarasi: no, Model: "SanDisk Ultra", SeriNumarasi: "SN-1",
        BoyutBayt: 32_000_000_000, CikarilabilirMi: true, VeriYolu: "USB",
        SistemDiskiMi: false, KullanilanBayt: 8_000_000_000);

    private static HamDiskGirisi HamSistem() => new(
        DiskNumarasi: 0, Model: "Samsung 980", SeriNumarasi: "SN-0",
        BoyutBayt: 512_000_000_000, CikarilabilirMi: false, VeriYolu: "NVMe",
        SistemDiskiMi: true, KullanilanBayt: 200_000_000_000);

    private sealed record Ortam(SahteDiskErisimi Disk, DiskServisi Servisi, GeriKazanmaServisi Kazanma);

    private static Ortam Kur(params HamDiskGirisi[] diskler)
    {
        var sahte = new SahteDiskErisimi { SahteSurucuYolu = @"E:\" };

        foreach (var d in diskler.Length > 0 ? diskler : [HamUsb()])
            sahte.DiskEkle(d);

        var diskServisi = new DiskServisi(sahte);

        return new Ortam(sahte, diskServisi, new GeriKazanmaServisi(sahte, diskServisi));
    }

    private static DiskBilgisi Disk(Ortam o, int no) => o.Servisi.DiskBul(no)!;

    [Fact]
    public async Task BasariliGeriKazanmaAdimlariDogruSiradaCagirir()
    {
        var o = Kur();

        var sonuc = await o.Kazanma.GeriKazanAsync(Disk(o, 1));

        Assert.True(sonuc.Basarili);

        var sira = o.Disk.Cagrilar.ToList();
        var kilit = sira.FindIndex(c => c.StartsWith("DiskiKilitle", StringComparison.Ordinal));
        var bolum = sira.FindIndex(c => c.StartsWith("BolumTablosuYaz", StringComparison.Ordinal));
        var bicim = sira.FindIndex(c => c.StartsWith("Bicimlendir", StringComparison.Ordinal));

        Assert.True(kilit >= 0 && kilit < bolum, "Kilit bolum tablosundan once alinmali");
        Assert.True(bolum < bicim, "Bolum tablosu bicimlendirmeden once yazilmali");
    }

    /// <summary>
    /// En onemli test: sistem diski hicbir kosulda geri kazanilamaz.
    /// Bu cagri calisan Windows'u silerdi.
    /// </summary>
    [Fact]
    public async Task SistemDiskiGeriKazanilamaz()
    {
        var o = Kur(HamSistem(), HamUsb());

        var sonuc = await o.Kazanma.GeriKazanAsync(Disk(o, 0));

        Assert.False(sonuc.Basarili);
        Assert.NotNull(sonuc.Hata);

        // Tek bir yikici cagri bile yapilmamis olmali.
        Assert.DoesNotContain(o.Disk.Cagrilar, c => c.StartsWith("BolumTablosuYaz", StringComparison.Ordinal));
        Assert.DoesNotContain(o.Disk.Cagrilar, c => c.StartsWith("Bicimlendir", StringComparison.Ordinal));
    }

    /// <summary>
    /// Kullanici diski sectikten sonra cikarip baskasini takmis olabilir.
    /// Seri numarasi tutmuyorsa islem baslamaz.
    /// </summary>
    [Fact]
    public async Task DiskDegismisseGeriKazanmaBaslamaz()
    {
        var o = Kur();
        var hedef = Disk(o, 1);

        o.Disk.DiskiCikar(1);

        var sonuc = await o.Kazanma.GeriKazanAsync(hedef);

        Assert.False(sonuc.Basarili);
        Assert.DoesNotContain(o.Disk.Cagrilar, c => c.StartsWith("Bicimlendir", StringComparison.Ordinal));
    }

    /// <summary>
    /// Windows kurulum USB'si FAT32'dir ve 4 GB ustu dosya alamaz.
    /// Geri kazanilan USB gunluk kullanim icindir; buyuk dosya
    /// kopyalanabilmeli, yani NTFS veya exFAT olmali.
    /// </summary>
    [Fact]
    public async Task VarsayilanBicimFat32Degildir()
    {
        var o = Kur();

        await o.Kazanma.GeriKazanAsync(Disk(o, 1));

        var bicim = o.Disk.Cagrilar.Single(c => c.StartsWith("Bicimlendir", StringComparison.Ordinal));

        Assert.DoesNotContain("FAT32", bicim, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task IstenenDosyaSistemiKullanilir()
    {
        var o = Kur();

        await o.Kazanma.GeriKazanAsync(Disk(o, 1), "NTFS");

        Assert.Contains(o.Disk.Cagrilar, c => c.Contains("NTFS", StringComparison.Ordinal));
    }

    [Fact]
    public async Task VerilenEtiketKullanilir()
    {
        var o = Kur();

        await o.Kazanma.GeriKazanAsync(Disk(o, 1), etiket: "YEDEK");

        Assert.Contains(o.Disk.Cagrilar, c => c.Contains("YEDEK", StringComparison.Ordinal));
    }

    /// <summary>
    /// Kurulum USB'si GPT olabilir; gunluk kullanimda MBR daha genis
    /// uyumludur (eski makineler, TV'ler, araba teypleri GPT okumaz).
    /// </summary>
    [Fact]
    public async Task GunlukKullanimIcinMbrYazilir()
    {
        var o = Kur();

        await o.Kazanma.GeriKazanAsync(Disk(o, 1));

        Assert.Contains(o.Disk.Cagrilar, c => c.Contains("Mbr", StringComparison.Ordinal));
    }

    [Fact]
    public async Task KilitHerKosuldaBirakilir()
    {
        var o = Kur();
        o.Disk.BasarisizOlacakCagri = "Bicimlendir";

        var sonuc = await o.Kazanma.GeriKazanAsync(Disk(o, 1));

        Assert.False(sonuc.Basarili);
        Assert.False(o.Disk.KilitAcikMi);
    }

    [Fact]
    public async Task HataKullaniciDilindeDoner()
    {
        var o = Kur();
        o.Disk.BasarisizOlacakCagri = "BolumTablosuYaz";

        var sonuc = await o.Kazanma.GeriKazanAsync(Disk(o, 1));

        Assert.NotNull(sonuc.Hata);
        Assert.NotEmpty(sonuc.Hata.NeOldu);
        Assert.NotEmpty(sonuc.Hata.NeYapmali);
    }

    [Fact]
    public async Task IlerlemeBildirilir()
    {
        var o = Kur();
        List<GeriKazanmaIlerlemesi> bildirimler = [];

        await o.Kazanma.GeriKazanAsync(
            Disk(o, 1), ilerleme: new Progress<GeriKazanmaIlerlemesi>(bildirimler.Add));

        Assert.NotEmpty(bildirimler);
    }

    [Fact]
    public async Task BasariliSonucSurucuHarfiDoner()
    {
        var o = Kur();

        var sonuc = await o.Kazanma.GeriKazanAsync(Disk(o, 1));

        Assert.Equal(@"E:\", sonuc.SurucuHarfi);
    }

    [Fact]
    public async Task IptalEdilirseYikiciCagriYapilmaz()
    {
        var o = Kur();

        using var kaynak = new CancellationTokenSource();
        await kaynak.CancelAsync();

        var sonuc = await o.Kazanma.GeriKazanAsync(Disk(o, 1), iptal: kaynak.Token);

        Assert.False(sonuc.Basarili);
        Assert.DoesNotContain(o.Disk.Cagrilar, c => c.StartsWith("Bicimlendir", StringComparison.Ordinal));
    }
}
