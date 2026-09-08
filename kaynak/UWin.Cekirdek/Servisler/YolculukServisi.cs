using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// "Sirada ne var?" ekraninin icerigini kurar: markaya ozel BIOS adimlari,
/// Windows kurulum ekranlari ve kurulum sonrasi yapilacaklar.
///
/// BIOS adimlari RehberServisi'nden gelir; kurulum ve sonrasi adimlari
/// metin dosyasindan okunur. Boylece dil degisiminde tek kaynak yeterlidir.
/// </summary>
public sealed class YolculukServisi : IYolculukServisi
{
    /// <summary>Metin anahtarlarindaki adim sayilari; yeni adim eklemek metin isi.</summary>
    private const int KurulumAdimSayisi = 5;
    private const int SonrasiAdimSayisi = 4;

    /// <summary>Anakart ureticisinin adiyla anilacagi adimin sirasi.</summary>
    private const int SurucuAdimiSirasi = 3;

    private readonly IRehberServisi _rehber;
    private readonly IMetinSaglayici _metinler;

    public YolculukServisi(IRehberServisi rehber, IMetinSaglayici metinler)
    {
        _rehber = rehber;
        _metinler = metinler;
    }

    public Yolculuk YolculukGetir(DonanimRaporu? donanim)
    {
        var bios = _rehber.RehberGetir(donanim?.AnakartUretici ?? "Genel");

        return new Yolculuk(
            Makine: MakineAdi(donanim),
            GirisTusu: bios.GirisTusu,
            BootMenuTusu: bios.BootMenuTusu,
            SecureBootNotu: bios.SecureBootNotu,
            Bolumler:
            [
                new YolculukBolumu(
                    _metinler.Al("sirada.bolum.bios"),
                    [.. bios.Adimlar.OrderBy(a => a.Sira)]),

                new YolculukBolumu(
                    _metinler.Al("sirada.bolum.kurulum"),
                    Adimlar("sirada.kurulum", KurulumAdimSayisi, donanim)),

                new YolculukBolumu(
                    _metinler.Al("sirada.bolum.sonrasi"),
                    Adimlar("sirada.sonrasi", SonrasiAdimSayisi, donanim))
            ]);
    }

    private static string? MakineAdi(DonanimRaporu? donanim)
    {
        if (donanim is null)
            return null;

        var ad = $"{donanim.AnakartUretici} {donanim.AnakartModel}".Trim();

        return string.IsNullOrWhiteSpace(ad) ? null : ad;
    }

    private RehberAdimi[] Adimlar(string onEk, int sayi, DonanimRaporu? donanim)
        => [.. Enumerable.Range(1, sayi).Select(sira => new RehberAdimi(
            sira,
            _metinler.Al($"{onEk}.{sira}.baslik"),
            Aciklama(onEk, sira, donanim)))];

    /// <summary>
    /// Surucu adimi anakart ureticisinin adini icerir. Uretici bilinmiyorsa
    /// yer tutuculu metin kullaniciya sizmasin diye genel surumune duser.
    /// </summary>
    private string Aciklama(string onEk, int sira, DonanimRaporu? donanim)
    {
        var anahtar = $"{onEk}.{sira}.aciklama";

        if (onEk != "sirada.sonrasi" || sira != SurucuAdimiSirasi)
            return _metinler.Al(anahtar);

        var uretici = donanim?.AnakartUretici;

        return string.IsNullOrWhiteSpace(uretici)
            ? _metinler.Al($"{anahtar}.genel")
            : _metinler.Al(anahtar, uretici);
    }
}
