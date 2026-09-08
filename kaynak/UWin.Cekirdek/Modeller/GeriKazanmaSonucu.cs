namespace UWin.Cekirdek.Modeller;

/// <summary>
/// USB'yi normal kullanima dondurme sonucu.
/// <see cref="SurucuHarfi"/> basarili durumda kullaniciya "artik E:
/// olarak kullanabilirsin" demek icin doner.
/// </summary>
public sealed record GeriKazanmaSonucu(bool Basarili, string? SurucuHarfi, UWinHatasi? Hata);
