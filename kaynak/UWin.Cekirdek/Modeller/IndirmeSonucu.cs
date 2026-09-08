namespace UWin.Cekirdek.Modeller;

/// <summary>Indirme isleminin sonucu. Basarisizsa Hata doludur ve dosya diskte birakilmaz.</summary>
public sealed record IndirmeSonucu(bool Basarili, string? DosyaYolu, string? Sha256, UWinHatasi? Hata);
