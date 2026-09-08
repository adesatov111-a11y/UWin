namespace UWin.Cekirdek.Modeller;

/// <summary>
/// Yazma sonucu. <see cref="Dogrulama"/>, yazilan USB'nin geri okunarak
/// karsilastirilmasidir; dogrulama atlandiysa null olur.
/// </summary>
public sealed record YazmaSonucu(
    bool Basarili,
    UWinHatasi? Hata,
    DogrulamaSonucu? Dogrulama = null);
