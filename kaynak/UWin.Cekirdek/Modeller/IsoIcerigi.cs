namespace UWin.Cekirdek.Modeller;

public sealed record IsoDosyasi(string GoreliYol, long BoyutBayt);

/// <summary>ISO icindeki dosya listesi ve FAT32 uyumluluk karari.</summary>
public sealed record IsoIcerigi(IReadOnlyList<IsoDosyasi> Dosyalar)
{
    /// <summary>FAT32'nin tek dosya siniri: 4 GB eksi 1 bayt.</summary>
    public const long Fat32AzamiDosyaBoyutu = 4L * 1024 * 1024 * 1024 - 1;

    public IsoDosyasi? InstallWim => Dosyalar.FirstOrDefault(
        d => d.GoreliYol.EndsWith("install.wim", StringComparison.OrdinalIgnoreCase));

    /// <summary>install.wim FAT32'ye sigmiyorsa parcalara bolunmelidir.</summary>
    public bool WimBolunmeliMi => InstallWim is { BoyutBayt: > Fat32AzamiDosyaBoyutu };
}
