using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

/// <summary>
/// Kurtarma araclarini listeler ve USB'ye yazar. Hedef diske yalnizca
/// dosya kopyalar; bolum tablosuna veya bicimlendirmeye dokunmaz.
/// </summary>
public interface IKurtarmaServisi
{
    IReadOnlyList<KurtarmaAraci> AraclariGetir();

    Task<KurtarmaSonucu> UsbyeYazAsync(string surucuYolu, CancellationToken iptal = default);
}
