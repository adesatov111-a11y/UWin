namespace UWin.Cekirdek.Modeller;

/// <summary>
/// Kullaniciya gosterilebilir hata. Ham hata kodu asla NeOldu/Neden/NeYapmali
/// alanlarina girmez; kod yalnizca TeknikAyrinti icinde, katlanabilir alanda durur.
/// </summary>
public sealed record UWinHatasi(
    string NeOldu,
    string Neden,
    string NeYapmali,
    string? TeknikAyrinti = null);
