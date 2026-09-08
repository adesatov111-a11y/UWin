namespace UWin.Cekirdek.Modeller;

/// <summary>Kurulumda silinecek tek bir kullanici klasoru.</summary>
public sealed record KullaniciKlasoru(string Ad, string Yol, long BoyutBayt, int DosyaSayisi);

/// <summary>
/// Bu bilgisayarda kurulumla birlikte gidecek verilerin ozeti.
///
/// Uyarinin zamanlamasi icerigi kadar onemlidir. Kullanici bunu
/// kurulumun ortasindaki disk secme ekraninda ogrenirse is isten
/// gecmistir: bilgisayar kapali, USB takili, yedek alacak bir yol yok.
/// Bu yuzden rapor USB hazirlanmadan once, hala calisan bir Windows'ta
/// cikarilir.
/// </summary>
public sealed record VeriRaporu(
    IReadOnlyList<KullaniciKlasoru> Klasorler,
    bool Okunabildi = true)
{
    public long ToplamBayt => Klasorler.Sum(k => k.BoyutBayt);

    public int ToplamDosya => Klasorler.Sum(k => k.DosyaSayisi);

    /// <summary>Icinde dosya bulunan klasorler - bos olanlar kullaniciyi mesgul etmez.</summary>
    public IReadOnlyList<KullaniciKlasoru> DoluKlasorler
        => Klasorler.Where(k => k.DosyaSayisi > 0).ToList();

    public bool VeriVar => ToplamDosya > 0;
}
