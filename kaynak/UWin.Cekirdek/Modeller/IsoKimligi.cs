namespace UWin.Cekirdek.Modeller;

/// <summary>Bir ISO'nun Windows kurulum diski olarak taninip taninmadigi.</summary>
public enum IsoTanimaDurumu
{
    /// <summary>Windows kurulum dosyalari bulundu ve surumu okundu.</summary>
    Tanindi,

    /// <summary>Kurulum dosyalari var ama surum numarasi okunamadi.</summary>
    WindowsAmaSurumBilinmiyor,

    /// <summary>Icinde Windows kurulum dosyalari yok.</summary>
    WindowsDegil,

    /// <summary>Dosya ISO olarak acilamadi.</summary>
    Okunamadi
}

/// <summary>
/// ISO'nun icine bakilarak cikarilan kimlik.
///
/// Ozet (SHA-256) ile tanima bilerek kullanilmaz: Microsoft bu ISO'lari
/// istek basina yeniden uretir, bu yuzden herkeste ayni cikan sabit bir
/// ozet yoktur. Icerige bakmak hem dogru sonuc verir hem de saniyeler
/// yerine dakikalar surmez.
/// </summary>
public sealed record IsoKimligi(
    IsoTanimaDurumu Durum,
    string? Ad,
    int? YapiNumarasi,
    string? Mimari,
    long GerekenUsbBoyutuBayt)
{
    public bool WindowsMu => Durum is IsoTanimaDurumu.Tanindi
                                    or IsoTanimaDurumu.WindowsAmaSurumBilinmiyor;
}
