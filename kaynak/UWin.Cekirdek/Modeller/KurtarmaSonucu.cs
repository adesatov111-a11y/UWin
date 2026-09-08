namespace UWin.Cekirdek.Modeller;

/// <summary>Kurtarma araclarinin USB'ye yazilma sonucu.</summary>
public sealed record KurtarmaSonucu(bool Basarili, string? KlasorYolu, UWinHatasi? Hata);
