using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

/// <summary>
/// Kurulum USB'sini normal kullanima dondurur. YIKICI - YazmaServisi
/// ile ayni korumalari tasir.
/// </summary>
public interface IGeriKazanmaServisi
{
    Task<GeriKazanmaSonucu> GeriKazanAsync(
        DiskBilgisi hedef,
        string dosyaSistemi = "exFAT",
        string etiket = "USB",
        IProgress<GeriKazanmaIlerlemesi>? ilerleme = null,
        CancellationToken iptal = default);
}
