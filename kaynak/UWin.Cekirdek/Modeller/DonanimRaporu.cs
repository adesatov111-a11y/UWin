namespace UWin.Cekirdek.Modeller;

/// <summary>Makinenin salt okuma donanim raporu ve Windows 11 uyumluluk karari.</summary>
public sealed record DonanimRaporu(
    string AnakartUretici,
    string AnakartModel,
    string Islemci,
    long RamBayt,
    string? TpmSurumu,
    bool UefiMi,
    bool SecureBootDestekli,
    long SistemDiskiBoyutBayt)
{
    private const long AsgariRam = 4L * 1024 * 1024 * 1024;
    private const long AsgariDisk = 64L * 1024 * 1024 * 1024;

    /// <summary>
    /// Karsilanmayan gereksinimlerin kod adlari.
    ///
    /// Bunlar kullaniciya gosterilmez - gosterilecek metinleri
    /// UyumlulukServisi uretir ve o metinler dile gore degisir.
    /// Buradaki degerler yalnizca "kac tanesi eksik" sorusuna cevap
    /// verir ve testlerde okunur; bu yuzden cevrilmezler.
    /// </summary>
    public IReadOnlyList<string> UyumsuzlukNedenleri
    {
        get
        {
            var nedenler = new List<string>();

            if (TpmSurumu is null || !TpmSurumu.StartsWith("2.", StringComparison.Ordinal))
                nedenler.Add("TPM");
            if (!UefiMi)
                nedenler.Add("UEFI");
            if (!SecureBootDestekli)
                nedenler.Add("SecureBoot");
            if (RamBayt < AsgariRam)
                nedenler.Add("RAM");
            if (SistemDiskiBoyutBayt < AsgariDisk)
                nedenler.Add("Disk");

            return nedenler;
        }
    }

    public bool Windows11Uyumlu => UyumsuzlukNedenleri.Count == 0;
}
