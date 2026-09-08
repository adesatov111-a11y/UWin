using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

public interface IIndirmeServisi
{
    /// <summary>
    /// Dosyayi indirir ve beklenenSha256 verildiyse dogrular.
    /// Ozet uyusmazsa dosya silinir - bozuk ISO diske hic ulasmaz.
    /// </summary>
    /// <param name="yarimDosyayiKoru">
    /// true: kesinti sonrasi indirilen kisim korunur, sonraki deneme kaldigi
    /// yerden devam eder. false: yarim dosya silinir.
    /// </param>
    Task<IndirmeSonucu> IndirAsync(
        string baglanti,
        string hedefYol,
        string? beklenenSha256,
        IProgress<IndirmeIlerlemesi>? ilerleme = null,
        CancellationToken iptal = default,
        bool yarimDosyayiKoru = true);

    Task<string> Sha256HesaplaAsync(
        string dosyaYolu,
        IProgress<double>? ilerleme = null,
        CancellationToken iptal = default);
}
