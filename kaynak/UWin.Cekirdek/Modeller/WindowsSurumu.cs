namespace UWin.Cekirdek.Modeller;

/// <summary>Indirilebilir tek bir Windows surumu (surum + dil + mimari kombinasyonu).</summary>
public sealed record WindowsSurumu(
    string Kimlik,
    string GorunenAd,
    string Surum,
    string Dil,
    string Mimari,
    string? IndirmeBaglantisi,
    string? Sha256,
    long TahminiBoyutBayt)
{
    /// <summary>
    /// Bu surumun yazilabilmesi icin gereken en kucuk USB boyutu.
    /// Microsoft 8 GB soyler; sikistirilmis kurulum dosyalari ve
    /// FAT32 ek yuku icin gercek ihtiyac buna yakindir.
    /// </summary>
    public long AsgariUsbBoyutuBayt => 8L * 1000 * 1000 * 1000;

    /// <summary>Kullaniciya gosterilen tam ad: "Windows 11 24H2 - Turkce (64 bit)".</summary>
    public string TamAd => $"{GorunenAd} {Surum} - {DilAdi} ({MimariAdi})";

    private string DilAdi => Dil switch
    {
        "tr-TR" => "Turkce",
        "en-US" => "English",
        _ => Dil
    };

    private string MimariAdi => Mimari switch
    {
        "x64" => "64 bit",
        "x86" => "32 bit",
        "arm64" => "ARM 64 bit",
        _ => Mimari
    };
}
