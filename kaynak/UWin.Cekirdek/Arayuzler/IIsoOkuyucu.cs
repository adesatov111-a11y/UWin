using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

/// <summary>ISO dosyasini okur ve icerigini cikarir.</summary>
public interface IIsoOkuyucu
{
    IsoIcerigi Oku(string isoYolu);

    Task DosyaCikarAsync(string isoYolu, string goreliYol, string hedefYol, CancellationToken iptal = default);

    /// <summary>Buyuk WIM'i FAT32'ye sigacak SWM parcalarina boler; parca yollarini doner.</summary>
    Task<IReadOnlyList<string>> WimBolAsync(
        string wimYolu,
        string hedefKlasor,
        long parcaBoyutuBayt,
        IProgress<double>? ilerleme = null,
        CancellationToken iptal = default);
}
