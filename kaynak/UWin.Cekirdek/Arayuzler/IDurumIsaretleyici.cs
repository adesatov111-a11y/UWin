using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

public interface IDurumIsaretleyici
{
    Task IsaretBirakAsync(string surucuYolu, YazmaDurumIsareti isaret, CancellationToken iptal = default);

    /// <summary>Isaret yoksa, okunamiyorsa veya bozuksa null doner - hicbir kosulda firlatmaz.</summary>
    Task<YazmaDurumIsareti?> IsaretOkuAsync(string surucuYolu, CancellationToken iptal = default);

    Task IsaretSilAsync(string surucuYolu, CancellationToken iptal = default);
}
