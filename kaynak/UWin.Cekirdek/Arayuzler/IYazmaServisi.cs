using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

/// <summary>Sistemdeki TEK yikici bilesen. Baska hicbir servis diske yazmaz.</summary>
public interface IYazmaServisi
{
    /// <param name="dogrula">
    /// Yazma bitince USB geri okunup ISO ile karsilastirilir. Kapatmak
    /// islemi hizlandirir ama ucuz belleklerdeki sessiz yazma hatasini
    /// gozden kacirir.
    /// </param>
    Task<YazmaSonucu> YazAsync(
        DiskBilgisi hedef,
        string isoYolu,
        BolumTablosuTipi tip,
        IProgress<YazmaIlerlemesi>? ilerleme = null,
        CancellationToken iptal = default,
        bool dogrula = true);
}
