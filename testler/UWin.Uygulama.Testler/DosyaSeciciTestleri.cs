using UWin.Uygulama.Servisler;

namespace UWin.Uygulama.Testler;

/// <summary>
/// Dosya secme penceresi COM (IFileOpenDialog) uzerinden acilir.
///
/// Bu testler pencereyi BILEREK gostermez. Onceki hali gercek diyalogu
/// aciyordu; "dotnet test" masaustu oturumunda kostugu icin her derlemede
/// dort tane "Test" baslikli dosya gezgini aciliyor ve biri kapatana kadar
/// testler bekliyordu. Yani hicbir sey dogrulanmiyordu, sadece bekleniyordu.
///
/// Onemli olan sudur: COM nesnesi kurulabilmeli, suzgecler ve secenekler
/// hatasiz ayarlanabilmeli ve cagri hicbir kosulda istisna firlatmamali -
/// async void icinde bir istisna tum sureci oldururdu.
/// </summary>
public class DosyaSeciciTestleri
{
    private static string? Kur(nint pencere = 0, string baslik = "Test",
        string suzgec = "Windows ISO", string uzanti = ".iso")
        => DosyaSecici.Sec(pencere, baslik, suzgec, uzanti, pencereyiGoster: false);

    [Fact]
    public void DiyalogKurulumuCokmez()
    {
        Assert.Null(Kur());
    }

    [Fact]
    public void ArkaArkayaCagrilarCokmez()
    {
        // COM nesnesi her cagrida duzgun birakilmali; birakilmazsa
        // arka arkaya cagrilar er ya da gec patlar.
        for (var i = 0; i < 5; i++)
            Assert.Null(Kur());
    }

    [Fact]
    public void GecersizPencereTaniticisiCokmez()
    {
        Assert.Null(Kur(pencere: -1));
    }

    [Theory]
    [InlineData(".iso")]
    [InlineData("iso")]
    [InlineData("")]
    public void DegisikUzantiBicimleriCokmez(string uzanti)
    {
        // "iso" ve ".iso" ayni sekilde kabul edilmeli; bos uzanti da
        // istisna yerine null donmeli.
        Assert.Null(Kur(uzanti: uzanti));
    }

    [Fact]
    public void TurkceKarakterliBaslikCokmez()
    {
        Assert.Null(Kur(baslik: "ISO dosyası seç", suzgec: "Tüm dosyalar"));
    }
}
