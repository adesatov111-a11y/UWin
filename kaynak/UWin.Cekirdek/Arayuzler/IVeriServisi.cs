using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

/// <summary>
/// Kurulumda gidecek kullanici verilerini sayar. Salt okuma:
/// hicbir dosyaya dokunmaz.
/// </summary>
public interface IVeriServisi
{
    /// <param name="klasorler">
    /// Verilmezse bu makinenin standart kullanici klasorleri kullanilir.
    /// </param>
    Task<VeriRaporu> RaporAlAsync(
        IReadOnlyList<(string Ad, string Yol)>? klasorler = null,
        CancellationToken iptal = default);
}
