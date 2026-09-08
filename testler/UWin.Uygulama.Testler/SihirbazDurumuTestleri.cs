using UWin.Cekirdek.Modeller;
using UWin.Uygulama.GorunumModelleri;

namespace UWin.Uygulama.Testler;

public class SihirbazDurumuTestleri
{
    private static DiskBilgisi Disk(
        DiskSinifi sinif = DiskSinifi.UsbBellek,
        long boyut = 32_000_000_000,
        string ad = "SanDisk Ultra")
        => new(DiskNumarasi: 1, Ad: ad, Model: ad, BoyutBayt: boyut,
            Sinif: sinif, SeriNumarasi: "SN-1", KullanilanBayt: 0);

    private static WindowsSurumu Surum() =>
        new("win11-25h2", "Windows 11", "25H2", "tr-TR", "x64", null, null, 6_979_321_856);

    private static DonanimRaporu Donanim() =>
        new("MSI", "B550-A PRO", "Ryzen 5 5600", 17_179_869_184, "2.0", true, true, 512_110_190_592);

    /// <summary>Onay adimina kadar ilerlemis, gecerli secimleri olan bir durum.</summary>
    private static SihirbazDurumu HazirDurum()
    {
        var d = KaynagaKadar();
        d.SecilenSurum = Surum();
        d.IsoYolu = "C:/test.iso";
        d.Ilerle();                       // Kaynak -> Disk
        d.SecilenDisk = Disk();
        d.Ilerle();                       // Disk -> Onay
        return d;
    }

    /// <summary>Kaynak adimina kadar ilerlemis bos durum.</summary>
    private static SihirbazDurumu KaynagaKadar()
    {
        var d = new SihirbazDurumu { Donanim = Donanim() };
        d.Ilerle();                       // Karsilama -> Amac
        d.Ilerle();                       // Amac -> Kaynak
        return d;
    }

    [Fact]
    public void BaslangictaKarsilamaAdimindadir()
    {
        Assert.Equal(SihirbazAdimi.Karsilama, new SihirbazDurumu().AktifAdim);
    }

    [Fact]
    public void DonanimOkunmadanKarsilamadanIlerlenemez()
    {
        Assert.False(new SihirbazDurumu().IlerleyebilirMi());
    }

    [Fact]
    public void IsoSecilmedenKaynaktanIlerlenemez()
    {
        var durum = KaynagaKadar();

        Assert.Equal(SihirbazAdimi.Kaynak, durum.AktifAdim);
        Assert.False(durum.IlerleyebilirMi());
    }

    [Fact]
    public void KaynakAdimiSurumdenOnceGelir()
    {
        var durum = KaynagaKadar();

        // Elindeki ISO Windows 7 bile olabilir; once dosyayi sormak gerekir.
        Assert.Equal(SihirbazAdimi.Kaynak, durum.AktifAdim);
    }

    [Fact]
    public void KendiIsosuSecilinceSurumSorulmaz()
    {
        var durum = KaynagaKadar();
        durum.KendiIsosu = true;
        durum.IsoYolu = "C:/kendi.iso";

        // Surum secilmemis olmasi ilerlemeyi engellememeli: dosya zaten
        // hangi Windows oldugunu soyluyor.
        Assert.Null(durum.SecilenSurum);
        Assert.True(durum.IlerleyebilirMi());

        durum.Ilerle();
        Assert.Equal(SihirbazAdimi.Disk, durum.AktifAdim);
    }

    [Fact]
    public void KaynaktanGeriDonulunceAmacaGelinir()
    {
        var durum = KaynagaKadar();
        durum.Geri();

        Assert.Equal(SihirbazAdimi.Amac, durum.AktifAdim);
    }

    [Fact]
    public void IsoKimligiGerekenBoyutuBelirler()
    {
        var durum = new SihirbazDurumu
        {
            Donanim = Donanim(),
            SecilenSurum = Surum(),
            IsoKimligi = new IsoKimligi(
                IsoTanimaDurumu.Tanindi, "Windows 11", null, "x64", 9_300_000_000)
        };

        // ISO gercekte 9,3 GB istiyorsa surumun sabit 8 GB'i degil o gecerlidir.
        Assert.Equal(9_300_000_000, durum.GerekenUsbBoyutuBayt);
    }

    [Fact]
    public void IsoyaGoreKucukKalanDiskSecilemez()
    {
        var durum = new SihirbazDurumu
        {
            Donanim = Donanim(),
            IsoKimligi = new IsoKimligi(
                IsoTanimaDurumu.Tanindi, "Windows 11", null, "x64", 9_300_000_000),
            SecilenDisk = Disk(boyut: 8_000_000_000)
        };

        Assert.False(durum.DiskYeterinceBuyukMu);
    }

    [Fact]
    public void UsbBellekOnayindaAdYazmakGerekmez()
    {
        var durum = HazirDurum();

        Assert.Equal(SihirbazAdimi.Onay, durum.AktifAdim);
        Assert.True(durum.IlerleyebilirMi());
    }

    [Fact]
    public void UsbDisiDiskteAdYazilmadanIlerlenemez()
    {
        var durum = HazirDurum();
        durum.SecilenDisk = Disk(DiskSinifi.HariciDisk);

        Assert.True(durum.AdOnayiGerekiyorMu);
        Assert.False(durum.IlerleyebilirMi());
    }

    [Fact]
    public void DogruAdYazilincaUsbDisiDiskOnaylanir()
    {
        var durum = HazirDurum();
        durum.SecilenDisk = Disk(DiskSinifi.HariciDisk);

        durum.AdOnayi = "SanDisk Ultra";

        Assert.True(durum.IlerleyebilirMi());
    }

    [Fact]
    public void YanlisAdYazilirsaOnaylanmaz()
    {
        var durum = HazirDurum();
        durum.SecilenDisk = Disk(DiskSinifi.HariciDisk);

        durum.AdOnayi = "Baska Disk";

        Assert.False(durum.IlerleyebilirMi());
    }

    [Fact]
    public void AdOnayiBuyukKucukHarfVeBoslukToleransliDir()
    {
        var durum = HazirDurum();
        durum.SecilenDisk = Disk(DiskSinifi.HariciDisk);

        durum.AdOnayi = "  sandisk ultra  ";

        Assert.True(durum.IlerleyebilirMi());
    }

    [Fact]
    public void SistemDiskiSecilirseAsalIlerlenemez()
    {
        var durum = HazirDurum();
        durum.SecilenDisk = Disk(DiskSinifi.SistemDiski);
        durum.AdOnayi = "SanDisk Ultra";

        Assert.False(durum.IlerleyebilirMi());
    }

    [Fact]
    public void CokKucukUsbSecilirseIlerlenemez()
    {
        var durum = HazirDurum();

        // 4 GB'lik bir bellek Windows kurulumuna yetmez.
        durum.SecilenDisk = Disk(boyut: 4_000_000_000);

        Assert.False(durum.DiskYeterinceBuyukMu);
        Assert.False(durum.IlerleyebilirMi());
    }

    [Fact]
    public void YeterliBoyuttakiUsbKabulEdilir()
    {
        var durum = HazirDurum();

        durum.SecilenDisk = Disk(boyut: 8_000_000_000);

        Assert.True(durum.DiskYeterinceBuyukMu);
        Assert.True(durum.IlerleyebilirMi());
    }

    [Fact]
    public void GeriGitmeSecimleriKorur()
    {
        var durum = HazirDurum();

        durum.Geri();

        Assert.Equal(SihirbazAdimi.Disk, durum.AktifAdim);
        Assert.NotNull(durum.SecilenSurum);
        Assert.NotNull(durum.SecilenDisk);
    }

    [Fact]
    public void IlkAdimdaGeriGidilmez()
    {
        var durum = new SihirbazDurumu();

        durum.Geri();

        Assert.Equal(SihirbazAdimi.Karsilama, durum.AktifAdim);
    }

    [Fact]
    public void DiskDegisirseAdOnayiSifirlanir()
    {
        var durum = HazirDurum();
        durum.SecilenDisk = Disk(DiskSinifi.HariciDisk);
        durum.AdOnayi = "SanDisk Ultra";

        durum.SecilenDisk = Disk(DiskSinifi.HariciDisk, ad: "WD Elements");

        Assert.Null(durum.AdOnayi);
        Assert.False(durum.IlerleyebilirMi());
    }

    [Fact]
    public void SayacHerIkiYoldaAyniIlerler()
    {
        var durum = new SihirbazDurumu { Donanim = Donanim(), KendiIsosu = true };

        Assert.Equal(1, durum.AdimSirasi);
        Assert.Equal(6, durum.ToplamAdimSayisi);

        durum.Ilerle();                    // Karsilama -> Amac
        Assert.Equal(2, durum.AdimSirasi);

        durum.Ilerle();                    // Amac -> Kaynak
        Assert.Equal(3, durum.AdimSirasi);

        durum.IsoYolu = "C:/kendi.iso";
        durum.Ilerle();                    // Kaynak -> Disk
        Assert.Equal(4, durum.AdimSirasi);
    }

    [Fact]
    public void SonAdimToplamSayiyaEsittir()
    {
        var durum = HazirDurum();          // Onay adiminda
        durum.SecilenDisk = Disk();
        durum.Ilerle();                    // Onay -> Yazma

        Assert.Equal(SihirbazAdimi.Yazma, durum.AktifAdim);
        Assert.Equal(durum.ToplamAdimSayisi, durum.AdimSirasi);
    }

    // --- Kurulum amaci ve veri kaybi uyarisi ---

    private static VeriRaporu DoluVeri() => new(
    [
        new KullaniciKlasoru("Masaüstü", @"C:\Users\x\Desktop", 12_000_000_000, 3_400),
        new KullaniciKlasoru("Belgeler", @"C:\Users\x\Documents", 4_000_000_000, 900)
    ]);

    private static VeriRaporu BosVeri() => new(
    [
        new KullaniciKlasoru("Masaüstü", @"C:\Users\x\Desktop", 0, 0)
    ]);

    [Fact]
    public void AmacAdimiKarsilamadanHemenSonraGelir()
    {
        var durum = new SihirbazDurumu { Donanim = Donanim() };

        durum.Ilerle();

        Assert.Equal(SihirbazAdimi.Amac, durum.AktifAdim);
    }

    /// <summary>
    /// Amac secmek zorunlu degildir. Emin olmayan kullaniciyi bir secim
    /// yapmaya zorlamak, yanlis tavsiye almasina yol acar - gecebilmeli.
    /// </summary>
    [Fact]
    public void AmacSecilmedenDeIlerlenebilir()
    {
        var durum = new SihirbazDurumu { Donanim = Donanim() };
        durum.Ilerle();

        Assert.Equal(KurulumAmaci.Belirtilmemis, durum.Amac);
        Assert.True(durum.IlerleyebilirMi());
    }

    /// <summary>
    /// Silinecek dosya varsa kullanici uyariyi gordugunu onaylamadan
    /// ilerleyemez. Bu uyarinin tek degeri zamanlamasidir: kurulumun
    /// ortasinda ogrenirse yedek alacak durumu kalmaz.
    /// </summary>
    [Fact]
    public void VeriVarsaOnaylanmadanIlerlenemez()
    {
        var durum = new SihirbazDurumu { Donanim = Donanim(), Veri = DoluVeri() };
        durum.Ilerle();

        Assert.True(durum.VeriUyarisiGerekiyorMu);
        Assert.False(durum.IlerleyebilirMi());
    }

    [Fact]
    public void VeriOnaylanincaIlerlenir()
    {
        var durum = new SihirbazDurumu { Donanim = Donanim(), Veri = DoluVeri() };
        durum.Ilerle();

        durum.VeriUyarisiOnaylandi = true;

        Assert.True(durum.IlerleyebilirMi());
    }

    /// <summary>
    /// USB baska bir bilgisayar icin hazirlaniyorsa buradaki dosyalar
    /// tehlikede degildir; uyari gosterilmez.
    /// </summary>
    [Fact]
    public void BaskaBilgisayaraKurulacaksaVeriUyarisiGerekmez()
    {
        var durum = new SihirbazDurumu
        {
            Donanim = Donanim(),
            Veri = DoluVeri(),
            BuBilgisayaraKurulacak = false
        };
        durum.Ilerle();

        Assert.False(durum.VeriUyarisiGerekiyorMu);
        Assert.True(durum.IlerleyebilirMi());
    }

    [Fact]
    public void BosKlasorlerdeVeriUyarisiGerekmez()
    {
        var durum = new SihirbazDurumu { Donanim = Donanim(), Veri = BosVeri() };
        durum.Ilerle();

        Assert.False(durum.VeriUyarisiGerekiyorMu);
        Assert.True(durum.IlerleyebilirMi());
    }

    [Fact]
    public void VeriHenuzOkunmamissaIlerlemeEngellenmez()
    {
        // Tarama surerken kullaniciyi bekletmek yerine gecmesine izin
        // verilir; rapor geldiginde uyari zaten gorunur.
        var durum = new SihirbazDurumu { Donanim = Donanim() };
        durum.Ilerle();

        Assert.Null(durum.Veri);
        Assert.True(durum.IlerleyebilirMi());
    }

    [Fact]
    public void AmacSecimiKorunur()
    {
        var durum = new SihirbazDurumu { Donanim = Donanim() };
        durum.Ilerle();
        durum.Amac = KurulumAmaci.Virus;
        durum.Ilerle();
        durum.Geri();

        Assert.Equal(KurulumAmaci.Virus, durum.Amac);
    }

    /// <summary>
    /// Kurtarma araclari varsayilan olarak eklenir: birkac yuz kilobayt
    /// yer kaplar ve acilmayan bir Windows karsisinda tek dayanak olabilir.
    /// </summary>
    [Fact]
    public void KurtarmaAraclariVarsayilanOlarakAcik()
    {
        Assert.True(new SihirbazDurumu().KurtarmaAraclariEklensin);
    }

    [Fact]
    public void ToplamAdimSayisiAltidir()
    {
        Assert.Equal(6, new SihirbazDurumu().ToplamAdimSayisi);
    }
}
