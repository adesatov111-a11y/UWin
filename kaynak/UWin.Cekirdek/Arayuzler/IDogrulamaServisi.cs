using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

/// <summary>Yazilan USB'yi geri okuyup ISO ile karsilastirir. Salt okuma.</summary>
public interface IDogrulamaServisi
{
    Task<DogrulamaSonucu> DogrulaAsync(
        string surucuYolu,
        IsoIcerigi beklenen,
        IProgress<double>? ilerleme = null,
        CancellationToken iptal = default);
}
