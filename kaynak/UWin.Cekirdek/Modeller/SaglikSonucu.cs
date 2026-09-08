namespace UWin.Cekirdek.Modeller;

/// <summary>Bellek sagligi testinin sonucu.</summary>
public enum SaglikDurumu
{
    /// <summary>Yazilan her sey dogru geri okundu.</summary>
    Saglam,

    /// <summary>
    /// Bellek bildirdigi kadar yer tutmuyor - sahte kapasite. Bu
    /// belleklerde kurulum yarida kaliyor ve sebebi anlasilmiyor.
    /// </summary>
    SahteKapasite,

    /// <summary>Veri yazildi ama farkli geri okundu; bellek yipranmis.</summary>
    Bozuk,

    /// <summary>Test tamamlanamadi (bellek cikarildi, yazma korumali).</summary>
    TestEdilemedi
}

/// <summary>
/// Bellek sagligi raporu.
///
/// <see cref="GercekBoyutBayt"/>, testin dogrulayabildigi son noktadir.
/// Sahte belleklerde bildirilen boyuttan cok kucuk cikar - 64 GB diye
/// satilan bir bellegin gercekte 8 GB olmasi bu piyasada yaygin.
/// </summary>
public sealed record SaglikSonucu(
    SaglikDurumu Durum,
    long BildirilenBoyutBayt,
    long GercekBoyutBayt,
    long TestEdilenBayt,
    UWinHatasi? Hata = null)
{
    public bool Saglam => Durum == SaglikDurumu.Saglam;

    /// <summary>Gercek kapasitenin bildirilene orani (0-1).</summary>
    public double KapasiteOrani => BildirilenBoyutBayt <= 0
        ? 0
        : Math.Clamp((double)GercekBoyutBayt / BildirilenBoyutBayt, 0, 1);
}
