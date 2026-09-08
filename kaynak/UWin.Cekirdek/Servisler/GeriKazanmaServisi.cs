using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// Kurulum USB'sini gunluk kullanima dondurur.
///
/// Kurulum bittikten sonra elde 8 GB'lik, FAT32 bicimli, uzerinde
/// Windows kurulum dosyalari olan bir bellek kalir. Kullanici bunu
/// silmeye kalkistiginda Windows'un kendi bicimlendirme araci cogu
/// zaman ise yaramaz: bolum yapisi kurulum medyasindan kalmistir ve
/// sag tik > Bicimlendir bunu duzeltmez. Insanlar bu yuzden USB'yi
/// bozuldu sanip atiyor.
///
/// Yikici bir islem oldugu icin YazmaServisi ile ayni kapilari gecer:
/// sistem diski asla, hedef yazmadan hemen once yeniden dogrulanir,
/// kilit her kosulda birakilir.
/// </summary>
public sealed class GeriKazanmaServisi : IGeriKazanmaServisi
{
    private readonly IYerelDiskErisimi _erisim;
    private readonly IDiskServisi _diskServisi;
    private readonly IMetinSaglayici _metinler;

    /// <param name="metinler">
    /// Kullaniciya gosterilen metinler buradan gelir; verilmezse
    /// varsayilan saglayici kullanilir.
    /// </param>
    public GeriKazanmaServisi(
        IYerelDiskErisimi erisim,
        IDiskServisi diskServisi,
        IMetinSaglayici? metinler = null)
    {
        _erisim = erisim;
        _diskServisi = diskServisi;
        _metinler = metinler ?? new MetinSaglayici();
    }

    public async Task<GeriKazanmaSonucu> GeriKazanAsync(
        DiskBilgisi hedef,
        string dosyaSistemi = "exFAT",
        string etiket = "USB",
        IProgress<GeriKazanmaIlerlemesi>? ilerleme = null,
        CancellationToken iptal = default)
    {
        var hata = Dogrula(hedef);
        if (hata is not null)
            return new GeriKazanmaSonucu(false, null, hata);

        Bildir(ilerleme, GeriKazanmaAdimi.Dogrulama, 10, _metinler.Al("kazanma.ilerleme.dogrulandi"));

        IDisposable? kilit = null;

        try
        {
            iptal.ThrowIfCancellationRequested();

            kilit = await _erisim.DiskiKilitleAsync(hedef.DiskNumarasi, iptal);
            Bildir(ilerleme, GeriKazanmaAdimi.Kilitleme, 25, _metinler.Al("kazanma.ilerleme.kilitlendi"));

            await _erisim.BirimleriSokAsync(hedef.DiskNumarasi, iptal);

            // MBR bilerek secilir: gunluk kullanimda GPT'den daha genis
            // uyumludur. Eski makineler, televizyonlar ve araba teypleri
            // GPT bicimli bellegi hic gormez.
            await _erisim.BolumTablosuYazAsync(hedef.DiskNumarasi, BolumTablosuTipi.Mbr, iptal);
            Bildir(ilerleme, GeriKazanmaAdimi.BolumTablosu, 60, _metinler.Al("kazanma.ilerleme.bolum"));

            await _erisim.BicimlendirAsync(hedef.DiskNumarasi, dosyaSistemi, etiket, iptal);
            Bildir(ilerleme, GeriKazanmaAdimi.Bicimlendirme, 95, _metinler.Al("kazanma.ilerleme.bicimlendi"));

            var surucu = _erisim.SurucuYoluBul(hedef.DiskNumarasi);

            Bildir(ilerleme, GeriKazanmaAdimi.Tamamlandi, 100, _metinler.Al("kazanma.ilerleme.hazir"));
            return new GeriKazanmaSonucu(true, surucu, null);
        }
        catch (OperationCanceledException)
        {
            return new GeriKazanmaSonucu(false, null, new UWinHatasi(
                NeOldu: _metinler.Al("hata.iptal"),
                Neden: _metinler.Al("kazanma.hata.iptal.neden"),
                NeYapmali: _metinler.Al("kazanma.hata.iptal.yapmali")));
        }
        catch (Exception e)
        {
            return new GeriKazanmaSonucu(false, null, HataYorumla(e));
        }
        finally
        {
            kilit?.Dispose();
        }
    }

    /// <summary>
    /// Yazma oncesi son savunma. Sistem diski kontrolu burada da
    /// tekrarlanir - tek bir bayrak degerine guvenilmez.
    /// </summary>
    private UWinHatasi? Dogrula(DiskBilgisi hedef)
    {
        if (hedef.Sinif == DiskSinifi.SistemDiski || !hedef.YazilabilirMi)
        {
            return new UWinHatasi(
                NeOldu: _metinler.Al("kazanma.hata.sistem"),
                Neden: _metinler.Al("hata.sistem.diski.neden"),
                NeYapmali: _metinler.Al("hata.sistem.diski.yapmali"));
        }

        if (!_diskServisi.HedefHalaGecerliMi(hedef))
        {
            return new UWinHatasi(
                NeOldu: _metinler.Al("hata.disk.yok"),
                Neden: _metinler.Al("hata.disk.yok.neden"),
                NeYapmali: _metinler.Al("kazanma.hata.disk.yok.yapmali"));
        }

        return null;
    }

    private UWinHatasi HataYorumla(Exception e) => e switch
    {
        UnauthorizedAccessException => new UWinHatasi(
            NeOldu: _metinler.Al("hata.yetki"),
            Neden: _metinler.Al("kazanma.hata.yetki.neden"),
            NeYapmali: _metinler.Al("hata.yetki.yapmali"),
            TeknikAyrinti: e.Message),

        _ => new UWinHatasi(
            NeOldu: _metinler.Al("kazanma.hata.genel"),
            Neden: _metinler.Al("hata.yazilamadi.neden"),
            NeYapmali: _metinler.Al("kazanma.hata.genel.yapmali"),
            TeknikAyrinti: e.Message)
    };

    private static void Bildir(
        IProgress<GeriKazanmaIlerlemesi>? ilerleme,
        GeriKazanmaAdimi adim,
        double yuzde,
        string aciklama)
        => ilerleme?.Report(new GeriKazanmaIlerlemesi(adim, yuzde, aciklama));
}
