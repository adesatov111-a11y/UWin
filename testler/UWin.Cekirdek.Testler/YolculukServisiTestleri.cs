using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Yolculuk, USB hazir olduktan sonrasini anlatan uc bolumdur.
/// Kullanici bu ekrani USB hazirlamadan da acabildigi icin servis
/// donanim raporu olmadan da calismak zorundadir.
/// </summary>
public class YolculukServisiTestleri
{
    private static DonanimRaporu OrnekDonanim(string uretici = "Gigabyte") => new(
        AnakartUretici: uretici, AnakartModel: "A520M K V2", Islemci: "AMD Ryzen 5 5600",
        RamBayt: 17_179_869_184, TpmSurumu: "2.0", UefiMi: true,
        SecureBootDestekli: true, SistemDiskiBoyutBayt: 512_110_190_592);

    private static YolculukServisi Servis() => new(new RehberServisi(), new MetinSaglayici());

    [Fact]
    public void YolculukUcBolumdenOlusur()
    {
        var yolculuk = Servis().YolculukGetir(OrnekDonanim());

        Assert.Equal(3, yolculuk.Bolumler.Count);
    }

    [Fact]
    public void IlkBolumMarkayaOzelBiosAdimlaridir()
    {
        var yolculuk = Servis().YolculukGetir(OrnekDonanim("Gigabyte"));

        var bios = yolculuk.Bolumler[0];

        Assert.NotEmpty(bios.Adimlar);
        // Gigabyte rehberi DELETE tusunu soyler; genel rehber "DELETE veya F2" der.
        Assert.Equal("DELETE", yolculuk.GirisTusu);
    }

    [Fact]
    public void DonanimYoksaGenelRehbereDuserVeCokmez()
    {
        var yolculuk = Servis().YolculukGetir(null);

        Assert.Equal(3, yolculuk.Bolumler.Count);
        Assert.All(yolculuk.Bolumler, b => Assert.NotEmpty(b.Adimlar));
        Assert.Null(yolculuk.Makine);
    }

    [Fact]
    public void DonanimVarsaMakineAdiGosterilir()
    {
        var yolculuk = Servis().YolculukGetir(OrnekDonanim());

        Assert.NotNull(yolculuk.Makine);
        Assert.Contains("Gigabyte", yolculuk.Makine);
        Assert.Contains("A520M K V2", yolculuk.Makine);
    }

    [Fact]
    public void AnakartUreticisiSurucuAdimindaAdiylaAnilir()
    {
        var yolculuk = Servis().YolculukGetir(OrnekDonanim("Gigabyte"));

        var sonrasi = yolculuk.Bolumler[2];
        var surucuAdimi = sonrasi.Adimlar.Single(a => a.Aciklama.Contains("Gigabyte"));

        Assert.NotEmpty(surucuAdimi.Baslik);
    }

    [Fact]
    public void UreticiBilinmiyorsaSurucuAdimiGenelKalir()
    {
        var yolculuk = Servis().YolculukGetir(null);

        var sonrasi = yolculuk.Bolumler[2];

        // Bicimlendirilmemis yer tutucu kullaniciya sizmamali.
        Assert.All(sonrasi.Adimlar, a => Assert.DoesNotContain("{0}", a.Aciklama));
    }

    [Fact]
    public void HicbirAdimBosMetinIcermez()
    {
        var yolculuk = Servis().YolculukGetir(OrnekDonanim());

        Assert.All(yolculuk.Bolumler, bolum =>
        {
            Assert.False(string.IsNullOrWhiteSpace(bolum.Baslik));

            Assert.All(bolum.Adimlar, adim =>
            {
                Assert.False(string.IsNullOrWhiteSpace(adim.Baslik));
                Assert.False(string.IsNullOrWhiteSpace(adim.Aciklama));
            });
        });
    }

    [Fact]
    public void AdimlarBolumIcindeSiraliNumaralanir()
    {
        var yolculuk = Servis().YolculukGetir(OrnekDonanim());

        Assert.All(yolculuk.Bolumler, bolum =>
            Assert.Equal(
                Enumerable.Range(1, bolum.Adimlar.Count),
                bolum.Adimlar.Select(a => a.Sira)));
    }

    [Fact]
    public void BootMenuTusuVarsaAyricaBildirilir()
    {
        var yolculuk = Servis().YolculukGetir(OrnekDonanim("Gigabyte"));

        Assert.Equal("F12", yolculuk.BootMenuTusu);
    }
}
