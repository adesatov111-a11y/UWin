namespace UWin.Cekirdek.Modeller;

/// <summary>
/// Yazma sirasinda USB'de birakilir, basariyla bitince silinir.
/// Varligi, o USB'de yarim kalmis bir islem oldugunu gosterir.
/// </summary>
public sealed record YazmaDurumIsareti(
    string DiskSeriNumarasi,
    DateTimeOffset Baslangic,
    string SurumAdi);
