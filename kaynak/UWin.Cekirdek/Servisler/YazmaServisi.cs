using System.Diagnostics;
using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// USB'ye onyuklenebilir Windows kurulum medyasi yazar.
/// Sistemdeki tek yikici bilesen budur; hedef her yazmadan once
/// yeniden dogrulanir ve kilit her kosulda birakilir.
/// </summary>
public sealed class YazmaServisi : IYazmaServisi
{
    /// <summary>SWM parca boyutu: FAT32 sinirinin guvenli altinda kalir.</summary>
    private const long SwmParcaBoyutu = 3_800_000_000;

    private const string BirimEtiketi = "UWIN";

    private readonly IYerelDiskErisimi _erisim;
    private readonly IDiskServisi _diskServisi;
    private readonly IIsoOkuyucu _isoOkuyucu;
    private readonly IDurumIsaretleyici _isaretleyici;
    private readonly IDogrulamaServisi _dogrulama;
    private readonly IMetinSaglayici _metinler;

    /// <param name="dogrulama">
    /// Verilmezse varsayilan geri okuma servisi kullanilir.
    /// </param>
    /// <param name="metinler">
    /// Kullaniciya gosterilen metinler buradan gelir. Verilmezse
    /// varsayilan saglayici kullanilir; ilerleme aciklamalari ve
    /// hata mesajlari kullanicinin sectigi dilde olur.
    /// </param>
    public YazmaServisi(
        IYerelDiskErisimi erisim,
        IDiskServisi diskServisi,
        IIsoOkuyucu isoOkuyucu,
        IDurumIsaretleyici isaretleyici,
        IDogrulamaServisi? dogrulama = null,
        IMetinSaglayici? metinler = null)
    {
        _erisim = erisim;
        _diskServisi = diskServisi;
        _isoOkuyucu = isoOkuyucu;
        _isaretleyici = isaretleyici;
        _dogrulama = dogrulama ?? new DogrulamaServisi();
        _metinler = metinler ?? new MetinSaglayici();
    }

    public async Task<YazmaSonucu> YazAsync(
        DiskBilgisi hedef,
        string isoYolu,
        BolumTablosuTipi tip,
        IProgress<YazmaIlerlemesi>? ilerleme = null,
        CancellationToken iptal = default,
        bool dogrula = true)
    {
        // 1. Dogrulama - hicbir yikici cagri oncesinde.
        var dogrulamaHatasi = Dogrula(hedef);
        if (dogrulamaHatasi is not null)
            return new YazmaSonucu(false, dogrulamaHatasi);

        Bildir(ilerleme, YazmaAdimi.Dogrulama, 100, 5, _metinler.Al("ilerleme.dogrulandi"));

        IDisposable? kilit = null;
        string? surucuYolu = null;

        try
        {
            iptal.ThrowIfCancellationRequested();

            // 2. Kilitle ve birimleri sok.
            kilit = await _erisim.DiskiKilitleAsync(hedef.DiskNumarasi, iptal);
            Bildir(ilerleme, YazmaAdimi.Kilitleme, 100, 10, _metinler.Al("ilerleme.kilitlendi"));

            await _erisim.BirimleriSokAsync(hedef.DiskNumarasi, iptal);
            Bildir(ilerleme, YazmaAdimi.BirimSokme, 100, 15, _metinler.Al("ilerleme.sokuldu"));

            // 3. Bolum tablosu ve bicimlendirme.
            await _erisim.BolumTablosuYazAsync(hedef.DiskNumarasi, tip, iptal);
            Bildir(ilerleme, YazmaAdimi.BolumTablosu, 100, 25, _metinler.Al("ilerleme.bolum"));

            await _erisim.BicimlendirAsync(hedef.DiskNumarasi, "FAT32", BirimEtiketi, iptal);
            Bildir(ilerleme, YazmaAdimi.Bicimlendirme, 100, 30, _metinler.Al("ilerleme.bicimlendi"));

            // 4. Yarim kalma isareti: buradan sonra kesinti olursa USB kullanilamaz kalir.
            surucuYolu = _erisim.SurucuYoluBul(hedef.DiskNumarasi);
            if (surucuYolu is not null)
            {
                await _isaretleyici.IsaretBirakAsync(
                    surucuYolu,
                    new YazmaDurumIsareti(hedef.SeriNumarasi, DateTimeOffset.UtcNow, isoYolu),
                    iptal);
            }

            // 5. ISO icerigini kopyala.
            await IcerikKopyalaAsync(hedef, isoYolu, ilerleme, iptal);

            // 6. Boot kayitlarini yaz.
            await _erisim.BootYazAsync(hedef.DiskNumarasi, tip, iptal);
            Bildir(ilerleme, YazmaAdimi.BootYazma, 100, 92, _metinler.Al("ilerleme.boot"));

            // 7. Geri okuma: yazilanlar gercekten yerinde mi?
            DogrulamaSonucu? dogrulamaSonucu = null;

            if (dogrula)
            {
                dogrulamaSonucu = await DogrulaAsync(surucuYolu, isoYolu, ilerleme, iptal);

                // Bozuk bir USB'ye "hazir" demek, kullaniciyi BIOS'ta
                // cozemeyecegi bir hatayla bas basa birakmaktir. Isaret
                // bilerek silinmez: USB yarim isaretli kalir.
                if (!dogrulamaSonucu.Basarili)
                    return new YazmaSonucu(false, DogrulamaHatasi(dogrulamaSonucu), dogrulamaSonucu);
            }

            // 8. Basarili bitis: isaret kaldirilir.
            if (surucuYolu is not null)
                await _isaretleyici.IsaretSilAsync(surucuYolu, iptal);

            Bildir(ilerleme, YazmaAdimi.Tamamlandi, 100, 100, _metinler.Al("ilerleme.hazir"));
            return new YazmaSonucu(true, null, dogrulamaSonucu);
        }
        catch (OperationCanceledException)
        {
            return new YazmaSonucu(false, new UWinHatasi(
                NeOldu: _metinler.Al("hata.iptal"),
                Neden: _metinler.Al("hata.iptal.neden"),
                NeYapmali: _metinler.Al("hata.iptal.yapmali")));
        }
        catch (Exception e)
        {
            return new YazmaSonucu(false, HataYorumla(e));
        }
        finally
        {
            // Kilit her kosulda birakilir - basarili, hatali veya iptal.
            kilit?.Dispose();
        }
    }

    /// <summary>
    /// Yazma oncesi son savunma hatti. Sistem diski kontrolu servis
    /// seviyesinde tekrarlanir - tek bir bayrak degerine guvenilmez.
    /// </summary>
    private UWinHatasi? Dogrula(DiskBilgisi hedef)
    {
        if (hedef.Sinif == DiskSinifi.SistemDiski || !hedef.YazilabilirMi)
        {
            return new UWinHatasi(
                NeOldu: _metinler.Al("hata.sistem.diski"),
                Neden: _metinler.Al("hata.sistem.diski.neden"),
                NeYapmali: _metinler.Al("hata.sistem.diski.yapmali"));
        }

        if (!_diskServisi.HedefHalaGecerliMi(hedef))
        {
            return new UWinHatasi(
                NeOldu: _metinler.Al("hata.disk.yok"),
                Neden: _metinler.Al("hata.disk.yok.neden"),
                NeYapmali: _metinler.Al("hata.disk.yok.yapmali"));
        }

        return null;
    }

    private async Task IcerikKopyalaAsync(
        DiskBilgisi hedef,
        string isoYolu,
        IProgress<YazmaIlerlemesi>? ilerleme,
        CancellationToken iptal)
    {
        var icerik = _isoOkuyucu.Oku(isoYolu);
        var kopyalanacaklar = icerik.Dosyalar.ToList();

        // Kalan sure baytla olculur, dosya sayisiyla degil: 900 kucuk
        // dosya ile tek bir 7 GB'lik install.wim ayni "yuzde" araligini
        // kaplar ama kullanicinin bekledigi sure taban tabana zittir.
        var toplamBayt = icerik.Dosyalar.Sum(d => d.BoyutBayt);
        long yazilanBayt = 0;

        var kronometre = Stopwatch.StartNew();

        // 4 GB ustu install.wim FAT32'ye sigmaz; SWM parcalarina bolunur.
        if (icerik.WimBolunmeliMi && icerik.InstallWim is { } wim)
        {
            Bildir(ilerleme, YazmaAdimi.WimBolme, 0, 35,
                _metinler.Al("ilerleme.wim.bolunuyor"));

            var geciciKlasor = Path.Combine(Path.GetTempPath(), "uwin-wim-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(geciciKlasor);

            try
            {
                var geciciWim = Path.Combine(geciciKlasor, "install.wim");
                await _isoOkuyucu.DosyaCikarAsync(isoYolu, wim.GoreliYol, geciciWim, iptal);

                var parcalar = await _isoOkuyucu.WimBolAsync(
                    geciciWim, geciciKlasor, SwmParcaBoyutu,
                    new Progress<double>(y => Bildir(
                        ilerleme, YazmaAdimi.WimBolme, y, 35 + y * 0.1,
                        _metinler.Al("ilerleme.wim.suruyor"))),
                    iptal);

                kopyalanacaklar.Remove(wim);

                foreach (var parca in parcalar)
                {
                    iptal.ThrowIfCancellationRequested();

                    await _erisim.DosyaKopyalaAsync(
                        hedef.DiskNumarasi, parca, $"sources/{Path.GetFileName(parca)}", null, iptal);

                    // Parcalarin toplami bolunen WIM'e esittir; her parca
                    // icin ayri ayri olcmek yerine WIM'in payi bir kez
                    // eklenir - boylece toplam yuzde 100'u asmaz.
                    yazilanBayt += wim.BoyutBayt / parcalar.Count;

                    BildirKopyalama(ilerleme, yazilanBayt, toplamBayt, kronometre, _metinler);
                }
            }
            finally
            {
                GeciciKlasoruTemizle(geciciKlasor);
            }
        }

        foreach (var dosya in kopyalanacaklar)
        {
            iptal.ThrowIfCancellationRequested();

            await _erisim.DosyaKopyalaAsync(hedef.DiskNumarasi, dosya.GoreliYol, dosya.GoreliYol, null, iptal);

            yazilanBayt += dosya.BoyutBayt;
            BildirKopyalama(ilerleme, yazilanBayt, toplamBayt, kronometre, _metinler);
        }
    }

    /// <summary>Kopyalama ilerlemesini bayt sayaclari ve olculen hizla bildirir.</summary>
    private static void BildirKopyalama(
        IProgress<YazmaIlerlemesi>? ilerleme,
        long yazilan,
        long toplam,
        Stopwatch kronometre,
        IMetinSaglayici metinler)
    {
        if (ilerleme is null)
            return;

        var adimYuzdesi = toplam > 0 ? Math.Clamp(yazilan * 100.0 / toplam, 0, 100) : 0;
        var gecenSaniye = kronometre.Elapsed.TotalSeconds;
        var hiz = gecenSaniye > 0.5 ? yazilan / gecenSaniye : 0;

        ilerleme.Report(new YazmaIlerlemesi(
            YazmaAdimi.DosyaKopyalama,
            adimYuzdesi,
            Math.Clamp(45 + adimYuzdesi * 0.5, 0, 100),
            metinler.Al("ilerleme.kopyalaniyor"),
            YazilanBayt: yazilan,
            ToplamBayt: toplam,
            BaytBolumSaniye: hiz));
    }

    /// <summary>
    /// Yazilan dosyalari USB'den geri okur. Surucu harfi bulunamazsa
    /// dogrulama yapilamaz - bu bir hata degil, olcusuzluktur; o durumda
    /// "tamamlanamadi" doner ve yazma basarili sayilmaya devam eder.
    /// </summary>
    private async Task<DogrulamaSonucu> DogrulaAsync(
        string? surucuYolu,
        string isoYolu,
        IProgress<YazmaIlerlemesi>? ilerleme,
        CancellationToken iptal)
    {
        Bildir(ilerleme, YazmaAdimi.GeriOkuma, 0, 93, _metinler.Al("ilerleme.kontrol"));

        if (surucuYolu is null)
            return new DogrulamaSonucu([], Tamamlanabildi: false);

        var icerik = _isoOkuyucu.Oku(isoYolu);

        var adimIlerlemesi = new Progress<double>(y => Bildir(
            ilerleme, YazmaAdimi.GeriOkuma, y, 93 + y * 0.07, _metinler.Al("ilerleme.kontrol")));

        return await _dogrulama.DogrulaAsync(surucuYolu, icerik, adimIlerlemesi, iptal);
    }

    private UWinHatasi DogrulamaHatasi(DogrulamaSonucu sonuc)
    {
        if (!sonuc.Tamamlanabildi)
        {
            return new UWinHatasi(
                NeOldu: _metinler.Al("hata.dogrulama.okunamadi"),
                Neden: _metinler.Al("hata.dogrulama.okunamadi.neden"),
                NeYapmali: _metinler.Al("hata.dogrulama.okunamadi.yapmali"));
        }

        var sorun = sonuc.Sorunlular.Count;

        return new UWinHatasi(
            NeOldu: _metinler.Al("hata.dogrulama"),
            Neden: sorun == 1
                ? _metinler.Al("hata.dogrulama.tek")
                : _metinler.Al("hata.dogrulama.coklu", sorun),
            NeYapmali: _metinler.Al("hata.dogrulama.yapmali"),
            TeknikAyrinti: string.Join(
                Environment.NewLine,
                sonuc.Sorunlular.Take(20).Select(d => $"{d.GoreliYol}: {d.Durum}")));
    }

    private static void GeciciKlasoruTemizle(string klasor)
    {
        try
        {
            if (Directory.Exists(klasor))
                Directory.Delete(klasor, recursive: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Gecici dosyalar silinemezse islem yine de basarili sayilir;
            // Windows bunlari kendi temizlik dongusunde kaldirir.
        }
    }

    /// <summary>Ham istisnayi kullanicinin anlayacagi ucluye cevirir.</summary>
    private UWinHatasi HataYorumla(Exception e) => e switch
    {
        UnauthorizedAccessException => new UWinHatasi(
            NeOldu: _metinler.Al("hata.yetki"),
            Neden: _metinler.Al("hata.yetki.neden"),
            NeYapmali: _metinler.Al("hata.yetki.yapmali"),
            TeknikAyrinti: e.Message),

        IOException => new UWinHatasi(
            NeOldu: _metinler.Al("hata.yazilamadi"),
            Neden: _metinler.Al("hata.yazilamadi.neden"),
            NeYapmali: _metinler.Al("hata.yazilamadi.yapmali"),
            TeknikAyrinti: e.Message),

        _ => new UWinHatasi(
            NeOldu: _metinler.Al("hata.bilinmeyen"),
            Neden: _metinler.Al("hata.bilinmeyen.neden"),
            NeYapmali: _metinler.Al("hata.bilinmeyen.yapmali"),
            TeknikAyrinti: e.Message)
    };

    private static void Bildir(
        IProgress<YazmaIlerlemesi>? ilerleme,
        YazmaAdimi adim,
        double adimYuzdesi,
        double toplam,
        string aciklama)
        => ilerleme?.Report(new YazmaIlerlemesi(adim, adimYuzdesi, Math.Clamp(toplam, 0, 100), aciklama));
}
