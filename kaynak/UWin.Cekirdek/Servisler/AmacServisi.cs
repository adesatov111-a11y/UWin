using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// Kurulum amacini tavsiyeye cevirir.
///
/// Bu servisin varlik sebebi su celiski: "kurulumda Sil'e basma,
/// dosyalarin gider" ile "kurulumda mutlaka Sil'e bas, yoksa virus
/// kalir" ayni anda dogru olamaz. Hangisinin dogru oldugu kullanicinin
/// neden kurduguna baglidir ve program bunu sormadan bilemez.
///
/// Amac sorulmadiginda en temkinli yol secilir: veri silmeyi onermeyiz.
/// Yanlis yerde "sil" demek geri alinamaz; yanlis yerde "silme" demek
/// yalnizca ikinci bir kurulum gerektirir.
/// </summary>
public sealed class AmacServisi : IAmacServisi
{
    private readonly IMetinSaglayici _metinler;

    /// <param name="metinler">
    /// Tavsiye metinleri buradan gelir; verilmezse varsayilan
    /// saglayici kullanilir.
    /// </param>
    public AmacServisi(IMetinSaglayici? metinler = null)
        => _metinler = metinler ?? new MetinSaglayici();

    public AmacTavsiyesi TavsiyeGetir(KurulumAmaci amac) => amac switch
    {
        KurulumAmaci.Virus => Tavsiye(amac, "virus", BolumSilinmeli: true, adimSayisi: 4),
        KurulumAmaci.Yavaslik => Tavsiye(amac, "yavaslik", BolumSilinmeli: true, adimSayisi: 3),
        KurulumAmaci.YeniDisk => Tavsiye(amac, "yenidisk", BolumSilinmeli: true, adimSayisi: 2),
        KurulumAmaci.SurumYukseltme => Tavsiye(amac, "yukseltme", BolumSilinmeli: false, adimSayisi: 3),
        _ => Tavsiye(KurulumAmaci.Belirtilmemis, "genel", BolumSilinmeli: false, adimSayisi: 1)
    };

    /// <summary>
    /// Tek bir amacin tavsiyesini metin kaynagindan kurar. Ek adimlar
    /// "tavsiye.{amac}.1", ".2" ... seklinde numarali durur; yeni bir
    /// madde eklemek kod degil metin isidir.
    /// </summary>
    private AmacTavsiyesi Tavsiye(
        KurulumAmaci amac, string anahtar, bool BolumSilinmeli, int adimSayisi)
    {
        List<string> adimlar = [];

        for (var i = 1; i <= adimSayisi; i++)
            adimlar.Add(_metinler.Al($"tavsiye.{anahtar}.{i}"));

        return new AmacTavsiyesi(
            Amac: amac,
            Baslik: _metinler.Al($"tavsiye.{anahtar}.baslik"),
            DiskEkraniTavsiyesi: _metinler.Al($"tavsiye.{anahtar}.disk"),
            BolumSilinmeli: BolumSilinmeli,
            EkAdimlar: adimlar);
    }
}
