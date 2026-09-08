namespace UWin.Cekirdek.Modeller;

public sealed record RehberAdimi(int Sira, string Baslik, string Aciklama);

/// <summary>Bir markaya ozel BIOS giris ve boot sirasi rehberi.</summary>
public sealed record BiosRehberi(
    string Marka,
    string GirisTusu,
    string? BootMenuTusu,
    IReadOnlyList<RehberAdimi> Adimlar,
    string? SecureBootNotu);
