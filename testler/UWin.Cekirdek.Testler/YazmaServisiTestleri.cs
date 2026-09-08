using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;
using UWin.Cekirdek.Testler.Sahteler;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Sistemdeki tek yikici bilesenin testleri. Tumu bellek ici sahte disk
/// uzerinde calisir - hicbiri gercek disk silmez.
/// </summary>
public class YazmaServisiTestleri : IDisposable
{
    private readonly string _sahteUsb =
        Path.Combine(Path.GetTempPath(), "uwin-sahte-usb-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_sahteUsb))
            Directory.Delete(_sahteUsb, recursive: true);
    }

    private static HamDiskGirisi HamUsb(int no = 1) => new(
        DiskNumarasi: no, Model: "SanDisk Ultra", SeriNumarasi: "SN-1",
        BoyutBayt: 32_000_000_000, CikarilabilirMi: true, VeriYolu: "USB",
        SistemDiskiMi: false, KullanilanBayt: 0);

    private static HamDiskGirisi HamSistem() => new(
        DiskNumarasi: 0, Model: "Samsung 980", SeriNumarasi: "SN-0",
        BoyutBayt: 512_000_000_000, CikarilabilirMi: false, VeriYolu: "NVMe",
        SistemDiskiMi: true, KullanilanBayt: 200_000_000_000);

    private sealed record Ortam(
        SahteDiskErisimi Disk,
        SahteIsoOkuyucu Iso,
        DiskServisi DiskServisi,
        DurumIsaretleyici Isaretleyici,
        YazmaServisi Servis);

    /// <param name="diskeYaz">
    /// true ise sahte USB klasorune gercekten dosya yazilir. Geri okuma
    /// dogrulamasini sinayan testler bunu ister; digerleri icin gereksiz
    /// disk trafigi olur.
    /// </param>
    private Ortam Kur(SahteIsoOkuyucu? iso = null, bool diskeYaz = true, params HamDiskGirisi[] diskler)
    {
        var sahteDisk = new SahteDiskErisimi
        {
            SahteSurucuYolu = _sahteUsb,
            DiskeGercektenYaz = diskeYaz
        };

        foreach (var d in diskler.Length > 0 ? diskler : [HamUsb()])
            sahteDisk.DiskEkle(d);

        var sahteIso = iso ?? SahteIsoOkuyucu.KucukWimli();

        // Sahte disk, ISO'daki boyutlarla ayni buyuklukte dosya uretir;
        // boylece geri okuma dogrulamasi gercekci bir sonuc verir.
        foreach (var dosya in sahteIso.Oku("test.iso").Dosyalar)
            sahteDisk.DosyaBoyutlari[dosya.GoreliYol] = dosya.BoyutBayt;
        var diskServisi = new DiskServisi(sahteDisk);
        var isaretleyici = new DurumIsaretleyici();

        return new Ortam(
            sahteDisk, sahteIso, diskServisi, isaretleyici,
            new YazmaServisi(sahteDisk, diskServisi, sahteIso, isaretleyici));
    }

    private static int Sira(IReadOnlyList<string> cagrilar, string onEk)
        => cagrilar.ToList().FindIndex(c => c.StartsWith(onEk, StringComparison.Ordinal));

    [Fact]
    public async Task BasariliYazmaAdimlariDogruSiradaCagirir()
    {
        var o = Kur();
        var hedef = o.DiskServisi.DiskleriListele().Single();

        var sonuc = await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt);

        Assert.True(sonuc.Basarili);
        Assert.Null(sonuc.Hata);

        var c = o.Disk.Cagrilar;
        Assert.True(Sira(c, "DiskiKilitle") < Sira(c, "BirimleriSok"));
        Assert.True(Sira(c, "BirimleriSok") < Sira(c, "BolumTablosuYaz"));
        Assert.True(Sira(c, "BolumTablosuYaz") < Sira(c, "Bicimlendir"));
        Assert.True(Sira(c, "Bicimlendir") < Sira(c, "BootYaz"));
    }

    [Fact]
    public async Task SistemDiskineYazmaReddedilirVeHicbirYikiciCagriYapilmaz()
    {
        var o = Kur(null, true, HamSistem(), HamUsb());
        var sistem = o.DiskServisi.DiskleriListele(gelismisMod: true)
            .Single(d => d.Sinif == DiskSinifi.SistemDiski);

        var sonuc = await o.Servis.YazAsync(sistem, "test.iso", BolumTablosuTipi.Gpt);

        Assert.False(sonuc.Basarili);
        Assert.NotNull(sonuc.Hata);
        Assert.DoesNotContain(o.Disk.Cagrilar, c => c.StartsWith("BolumTablosuYaz", StringComparison.Ordinal));
        Assert.DoesNotContain(o.Disk.Cagrilar, c => c.StartsWith("Bicimlendir", StringComparison.Ordinal));
        Assert.DoesNotContain(o.Disk.Cagrilar, c => c.StartsWith("BirimleriSok", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DiskCikarilmissaYazmaBaslamaz()
    {
        var o = Kur();
        var hedef = o.DiskServisi.DiskleriListele().Single();
        o.Disk.DiskiCikar(1);

        var sonuc = await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt);

        Assert.False(sonuc.Basarili);
        Assert.DoesNotContain(o.Disk.Cagrilar, c => c.StartsWith("BolumTablosuYaz", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BuyukWimOtomatikBolunur()
    {
        var o = Kur(SahteIsoOkuyucu.BuyukWimli());
        var hedef = o.DiskServisi.DiskleriListele().Single();

        var sonuc = await o.Servis.YazAsync(hedef, "buyuk.iso", BolumTablosuTipi.Gpt);

        Assert.True(sonuc.Basarili);
        Assert.NotEmpty(o.Iso.BolunenWimler);
        Assert.DoesNotContain(o.Disk.YazilanDosyalar.Keys, k => k.EndsWith("install.wim", StringComparison.Ordinal));
        Assert.Contains(o.Disk.YazilanDosyalar.Keys, k => k.EndsWith(".swm", StringComparison.Ordinal));
    }

    [Fact]
    public async Task KucukWimBolunmedenKopyalanir()
    {
        var o = Kur(SahteIsoOkuyucu.KucukWimli());
        var hedef = o.DiskServisi.DiskleriListele().Single();

        var sonuc = await o.Servis.YazAsync(hedef, "kucuk.iso", BolumTablosuTipi.Gpt);

        Assert.True(sonuc.Basarili);
        Assert.Empty(o.Iso.BolunenWimler);
        Assert.Contains(o.Disk.YazilanDosyalar.Keys, k => k.EndsWith("install.wim", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(BolumTablosuTipi.Gpt)]
    [InlineData(BolumTablosuTipi.Mbr)]
    public async Task IstenenBolumTablosuTipiYazilir(BolumTablosuTipi tip)
    {
        var o = Kur();
        var hedef = o.DiskServisi.DiskleriListele().Single();

        await o.Servis.YazAsync(hedef, "test.iso", tip);

        Assert.Contains(o.Disk.Cagrilar, c => c == $"BolumTablosuYaz(1, {tip})");
    }

    [Fact]
    public async Task HataDurumundaKilitBirakilir()
    {
        var o = Kur();
        o.Disk.BasarisizOlacakCagri = "Bicimlendir";
        var hedef = o.DiskServisi.DiskleriListele().Single();

        var sonuc = await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt);

        Assert.False(sonuc.Basarili);
        Assert.False(o.Disk.KilitAcikMi);
    }

    [Fact]
    public async Task HataMesajiUcluBilgiTasir()
    {
        var o = Kur();
        o.Disk.BasarisizOlacakCagri = "BolumTablosuYaz";
        var hedef = o.DiskServisi.DiskleriListele().Single();

        var sonuc = await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt);

        Assert.NotNull(sonuc.Hata);
        Assert.NotEmpty(sonuc.Hata!.NeOldu);
        Assert.NotEmpty(sonuc.Hata.Neden);
        Assert.NotEmpty(sonuc.Hata.NeYapmali);
    }

    [Fact]
    public async Task IlerlemeSonundaTamamlandiBildirir()
    {
        var o = Kur();
        var hedef = o.DiskServisi.DiskleriListele().Single();
        var raporlar = new List<YazmaIlerlemesi>();

        await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt,
            new Progress<YazmaIlerlemesi>(raporlar.Add));

        Assert.NotEmpty(raporlar);
        Assert.All(raporlar, r => Assert.InRange(r.ToplamYuzde, 0, 100));
        Assert.Contains(raporlar, r => r.Adim == YazmaAdimi.Tamamlandi);
    }

    /// <summary>
    /// Kalan sure tahmini yuzde uzerinden yapilamaz: dosya sayisi ile
    /// bayt miktari birbirini tutmaz. 900 kucuk dosya ile 1 tane 7 GB'lik
    /// install.wim'in "yuzdesi" ayni hizda ilerlemez, ama kullanicinin
    /// bekledigi sure tamamen bayta baglidir.
    /// </summary>
    [Fact]
    public async Task KopyalamaIlerlemesiBaytSayaciTasir()
    {
        var o = Kur();
        var hedef = o.DiskServisi.DiskleriListele().Single();
        var raporlar = new List<YazmaIlerlemesi>();

        await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt,
            new Progress<YazmaIlerlemesi>(raporlar.Add));

        var kopyalama = raporlar.Where(r => r.Adim == YazmaAdimi.DosyaKopyalama).ToList();

        Assert.NotEmpty(kopyalama);
        Assert.All(kopyalama, r => Assert.True(r.ToplamBayt > 0, "Toplam bayt bildirilmeli"));
        Assert.Contains(kopyalama, r => r.YazilanBayt > 0);
    }

    [Fact]
    public async Task ToplamBaytIsoIcerigiylaEslesir()
    {
        var iso = SahteIsoOkuyucu.KucukWimli();
        var o = Kur(iso);
        var hedef = o.DiskServisi.DiskleriListele().Single();
        var raporlar = new List<YazmaIlerlemesi>();

        await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt,
            new Progress<YazmaIlerlemesi>(raporlar.Add));

        var beklenen = iso.Oku("test.iso").Dosyalar.Sum(d => d.BoyutBayt);
        var bildirilen = raporlar.First(r => r.Adim == YazmaAdimi.DosyaKopyalama).ToplamBayt;

        Assert.Equal(beklenen, bildirilen);
    }

    [Fact]
    public async Task YazilanBaytGerilemez()
    {
        var o = Kur();
        var hedef = o.DiskServisi.DiskleriListele().Single();
        var raporlar = new List<YazmaIlerlemesi>();

        await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt,
            new Progress<YazmaIlerlemesi>(raporlar.Add));

        var kopyalama = raporlar
            .Where(r => r.Adim == YazmaAdimi.DosyaKopyalama)
            .Select(r => r.YazilanBayt)
            .ToList();

        for (var i = 1; i < kopyalama.Count; i++)
            Assert.True(kopyalama[i] >= kopyalama[i - 1], "Yazilan bayt geri gitmemeli");
    }

    /// <summary>
    /// Yazma bitince USB geri okunur. Ucuz belleklerin sessiz yazma
    /// hatasi ancak boyle yakalanir; aksi halde kullanici hatayi BIOS'ta
    /// "no bootable device" olarak gorup sebebini asla bulamiyor.
    /// </summary>
    [Fact]
    public async Task YazmaSonrasiGeriOkumaYapilir()
    {
        var o = Kur();
        var hedef = o.DiskServisi.DiskleriListele().Single();
        var raporlar = new List<YazmaIlerlemesi>();

        var sonuc = await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt,
            new Progress<YazmaIlerlemesi>(raporlar.Add));

        Assert.True(sonuc.Basarili);
        Assert.Contains(raporlar, r => r.Adim == YazmaAdimi.GeriOkuma);
        Assert.NotNull(sonuc.Dogrulama);
    }

    [Fact]
    public async Task GeriOkumaBootYazmadanSonraCalisir()
    {
        var o = Kur();
        var hedef = o.DiskServisi.DiskleriListele().Single();
        var raporlar = new List<YazmaIlerlemesi>();

        await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt,
            new Progress<YazmaIlerlemesi>(raporlar.Add));

        var adimlar = raporlar.Select(r => r.Adim).ToList();

        Assert.True(
            adimlar.IndexOf(YazmaAdimi.BootYazma) < adimlar.IndexOf(YazmaAdimi.GeriOkuma),
            "Geri okuma boot yazmadan sonra gelmeli");
    }

    /// <summary>
    /// Dogrulama basarisiz olursa yazma da basarisiz sayilir. Bozuk bir
    /// USB'ye "hazir" demek, kullaniciyi BIOS'ta cozemeyecegi bir hatayla
    /// bas basa birakmak olur.
    /// </summary>
    [Fact]
    public async Task DogrulamaBasarisizsaYazmaBasarisizDoner()
    {
        // Diske gercekten yazilmaz: USB bos kalir, dogrulama eksik bulur.
        var o = Kur(diskeYaz: false);
        var hedef = o.DiskServisi.DiskleriListele().Single();

        var sonuc = await o.Servis.YazAsync(
            hedef, "test.iso", BolumTablosuTipi.Gpt, null, default, dogrula: true);

        Assert.False(sonuc.Basarili);
        Assert.NotNull(sonuc.Dogrulama);
        Assert.NotEmpty(sonuc.Dogrulama.Sorunlular);
        Assert.NotNull(sonuc.Hata);
    }

    [Fact]
    public async Task DogrulamaKapatilabilir()
    {
        var o = Kur();
        var hedef = o.DiskServisi.DiskleriListele().Single();
        var raporlar = new List<YazmaIlerlemesi>();

        var sonuc = await o.Servis.YazAsync(
            hedef, "test.iso", BolumTablosuTipi.Gpt,
            new Progress<YazmaIlerlemesi>(raporlar.Add), default, dogrula: false);

        Assert.True(sonuc.Basarili);
        Assert.Null(sonuc.Dogrulama);
        Assert.DoesNotContain(raporlar, r => r.Adim == YazmaAdimi.GeriOkuma);
    }

    [Fact]
    public async Task IptalEdilenYazmaHataDonerVeKilitAcilir()
    {
        var o = Kur();
        var hedef = o.DiskServisi.DiskleriListele().Single();

        using var kaynak = new CancellationTokenSource();
        await kaynak.CancelAsync();

        var sonuc = await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt, null, kaynak.Token);

        Assert.False(sonuc.Basarili);
        Assert.False(o.Disk.KilitAcikMi);
    }

    [Fact]
    public async Task TumIsoDosyalariDiskeYazilir()
    {
        var o = Kur(SahteIsoOkuyucu.KucukWimli());
        var hedef = o.DiskServisi.DiskleriListele().Single();

        await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt);

        Assert.Contains("bootmgr", o.Disk.YazilanDosyalar.Keys);
        Assert.Contains("efi/boot/bootx64.efi", o.Disk.YazilanDosyalar.Keys);
        Assert.Contains("sources/boot.wim", o.Disk.YazilanDosyalar.Keys);
    }

    [Fact]
    public async Task BasariliYazmaSonundaYarimIsaretiKalmaz()
    {
        var o = Kur();
        var hedef = o.DiskServisi.DiskleriListele().Single();

        await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt);

        Assert.Null(await o.Isaretleyici.IsaretOkuAsync(_sahteUsb));
    }

    [Fact]
    public async Task YazmaOrtasindaHataOlursaYarimIsaretiKalir()
    {
        var o = Kur();
        o.Disk.BasarisizOlacakCagri = "BootYaz";
        var hedef = o.DiskServisi.DiskleriListele().Single();

        var sonuc = await o.Servis.YazAsync(hedef, "test.iso", BolumTablosuTipi.Gpt);

        Assert.False(sonuc.Basarili);
        Assert.NotNull(await o.Isaretleyici.IsaretOkuAsync(_sahteUsb));
    }
}
