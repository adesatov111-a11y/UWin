namespace UWin.Cekirdek.Modeller;

/// <summary>Tek bir gereksinimin sonucu.</summary>
public enum UyumlulukDurumu
{
    /// <summary>Gereksinim karsilaniyor.</summary>
    Gecti,

    /// <summary>Su an karsilanmiyor ama kullanici BIOS'tan acabilir.</summary>
    AcilabilirDurumda,

    /// <summary>Donanim bunu desteklemiyor; ayar degisikligiyle cozulmez.</summary>
    Kaldi
}

/// <summary>
/// Tek bir Windows 11 gereksinimi ve kullanicinin ne yapabilecegi.
///
/// <see cref="NeYapmali"/> yalnizca kullanicinin yapabilecegi bir sey
/// varsa doludur. "TPM 2.0 bulunamadi" demek yeterli degil: bu makinelerin
/// cogunda TPM anakartta vardir ve yalnizca BIOS'tan kapalidir. Neyi
/// acacagini soylemeyen bir uyari kullaniciyi yeni bilgisayar almaya
/// ikna ediyor - oysa tek yapmasi gereken bir ayari acmak.
/// </summary>
public sealed record UyumlulukMaddesi(
    string Ad,
    UyumlulukDurumu Durum,
    string Aciklama,
    string? NeYapmali = null,
    string? BiosAyarAdi = null);

/// <summary>
/// Makinenin Windows 11 karnesi. Tek bir "uyumlu/uyumsuz" damgasi yerine
/// madde madde durur, cunku kullanicinin ihtiyaci olan sey karar degil
/// yapilacaklar listesidir.
/// </summary>
public sealed record UyumlulukRaporu(IReadOnlyList<UyumlulukMaddesi> Maddeler)
{
    public bool Uyumlu => Maddeler.All(m => m.Durum == UyumlulukDurumu.Gecti);

    /// <summary>BIOS'tan acilarak cozulebilecek maddeler.</summary>
    public IReadOnlyList<UyumlulukMaddesi> BiostanAcilabilirler
        => Maddeler.Where(m => m.Durum == UyumlulukDurumu.AcilabilirDurumda).ToList();

    /// <summary>Donanimdan kaynaklanan, cozulemeyecek maddeler.</summary>
    public IReadOnlyList<UyumlulukMaddesi> Kalanlar
        => Maddeler.Where(m => m.Durum == UyumlulukDurumu.Kaldi).ToList();

    /// <summary>
    /// Uyumsuzlugun tamami BIOS ayariyla cozulebiliyorsa true.
    /// Bu durumda kullaniciya "Windows 11 kuramazsin" demek yanlistir -
    /// dogrusu "su iki ayari ac, kurabilirsin".
    /// </summary>
    public bool BiosAyariylaCozulur
        => !Uyumlu && Kalanlar.Count == 0 && BiostanAcilabilirler.Count > 0;
}
