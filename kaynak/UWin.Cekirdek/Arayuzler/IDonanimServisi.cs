using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

public interface IDonanimServisi
{
    Task<DonanimRaporu> RaporAlAsync(CancellationToken iptal = default);
}
