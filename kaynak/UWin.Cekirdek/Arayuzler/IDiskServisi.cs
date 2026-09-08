using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

/// <summary>Diskleri listeler ve siniflandirir. Hicbir yazma islemi yapmaz.</summary>
public interface IDiskServisi
{
    /// <summary>
    /// Secilebilir diskleri listeler. Varsayilan modda yalnizca USB bellekler;
    /// gelismis modda harici ve dahili diskler de gorunur (sistem diski dahil,
    /// ama o hicbir zaman yazilabilir degildir).
    /// </summary>
    IReadOnlyList<DiskBilgisi> DiskleriListele(bool gelismisMod = false);

    DiskBilgisi? DiskBul(int diskNumarasi);

    /// <summary>
    /// Secilen diskin hala ayni fiziksel aygit oldugunu dogrular.
    /// Kullanici secimden sonra USB'yi degistirdiyse false doner.
    /// </summary>
    bool HedefHalaGecerliMi(DiskBilgisi secilen);
}
