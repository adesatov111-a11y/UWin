namespace UWin.Cekirdek.Modeller;

/// <summary>
/// Isletim sisteminden okunan ham disk kaydi. Risk siniflandirmasi
/// yapilmamis haldir; DiskServisi bunu DiskBilgisi'ne cevirir.
/// </summary>
public sealed record HamDiskGirisi(
    int DiskNumarasi,
    string Model,
    string SeriNumarasi,
    long BoyutBayt,
    bool CikarilabilirMi,
    string VeriYolu,
    bool SistemDiskiMi,
    long KullanilanBayt);
