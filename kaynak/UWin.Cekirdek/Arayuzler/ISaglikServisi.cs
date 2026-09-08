using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

/// <summary>
/// Bellegin gercek kapasitesini ve veri butunlugunu sinar. Bellege
/// gecici dosya yazar, sonra hepsini siler; bolum tablosuna veya
/// bicimlendirmeye dokunmaz.
/// </summary>
public interface ISaglikServisi
{
    Task<SaglikSonucu> TestEtAsync(
        string surucuYolu,
        long bildirilenBoyutBayt,
        IProgress<double>? ilerleme = null,
        CancellationToken iptal = default);
}
