using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// Windows 11 gereksinimlerini madde madde degerlendirir.
///
/// Buradaki asil deger "kurulamaz" demek degil, cozulebilir olani
/// cozulemez olandan ayirmaktir. Sahadaki en yaygin durum su: makinede
/// TPM 2.0 var, Secure Boot destekli, ama ikisi de BIOS'ta kapali.
/// Microsoft'un kendi araci bu makineye "uyumlu degil" der ve kullanici
/// yeni bilgisayar bakmaya gider. Oysa yapmasi gereken tek sey iki
/// ayari acmaktir - ve o ayarlarin adi markaya gore degisir.
/// </summary>
public sealed class UyumlulukServisi : IUyumlulukServisi
{
    private const long AsgariRam = 4L * 1024 * 1024 * 1024;
    private const long AsgariDisk = 64L * 1024 * 1024 * 1024;

    private readonly IMetinSaglayici _metinler;

    /// <param name="metinler">
    /// Madde adlari ve aciklamalari buradan gelir; verilmezse
    /// varsayilan saglayici kullanilir.
    /// </param>
    public UyumlulukServisi(IMetinSaglayici? metinler = null)
        => _metinler = metinler ?? new MetinSaglayici();

    public UyumlulukRaporu Degerlendir(DonanimRaporu donanim) => new(
    [
        TpmMaddesi(donanim),
        SecureBootMaddesi(donanim),
        UefiMaddesi(donanim),
        RamMaddesi(donanim),
        DiskMaddesi(donanim)
    ]);

    /// <summary>
    /// TPM 2.0 varsa gecer. Yoksa karar UEFI'ye bakar: UEFI ile acilan
    /// bir makine 2013 sonrasi uretilmistir ve neredeyse kesinlikle
    /// islemcinin icinde bir TPM tasir (Intel PTT / AMD fTPM). Gorunmuyor
    /// olmasi yok oldugu anlamina gelmez, kapali oldugu anlamina gelir.
    /// </summary>
    private UyumlulukMaddesi TpmMaddesi(DonanimRaporu d)
    {
        var ad = _metinler.Al("madde.tpm");

        if (d.TpmSurumu is not null && d.TpmSurumu.StartsWith("2.", StringComparison.Ordinal))
            return new UyumlulukMaddesi(ad, UyumlulukDurumu.Gecti, _metinler.Al("madde.tpm.gecti"));

        // TPM 1.2 gercek bir yongadir ama bir ayarla 2.0 olmaz.
        if (d.TpmSurumu is not null)
        {
            return new UyumlulukMaddesi(
                ad,
                UyumlulukDurumu.Kaldi,
                _metinler.Al("madde.tpm.eski", d.TpmSurumu),
                _metinler.Al("madde.win10.oneri"));
        }

        if (!d.UefiMi)
        {
            return new UyumlulukMaddesi(
                ad,
                UyumlulukDurumu.Kaldi,
                _metinler.Al("madde.tpm.yok"),
                _metinler.Al("madde.win10.oneri"));
        }

        var ayar = TpmAyarAdi(d.Islemci);

        return new UyumlulukMaddesi(
            ad,
            UyumlulukDurumu.AcilabilirDurumda,
            _metinler.Al("madde.tpm.kapali"),
            _metinler.Al("madde.tpm.kapali.yapmali", ayar),
            ayar);
    }

    /// <summary>
    /// Islemci markasina gore TPM ayarinin BIOS'taki adi. Bu adlar
    /// gercekten farklidir: AMD kartinda "Intel PTT" diye bir ayar yoktur
    /// ve kullanici olmayan bir menuyu arar durur.
    /// </summary>
    private string TpmAyarAdi(string islemci)
    {
        if (islemci.Contains("AMD", StringComparison.OrdinalIgnoreCase)
            || islemci.Contains("Ryzen", StringComparison.OrdinalIgnoreCase))
        {
            return _metinler.Al("bios.ayar.amd");
        }

        if (islemci.Contains("Intel", StringComparison.OrdinalIgnoreCase))
            return _metinler.Al("bios.ayar.intel");

        return _metinler.Al("bios.ayar.genel");
    }

    private UyumlulukMaddesi SecureBootMaddesi(DonanimRaporu d)
    {
        var ad = _metinler.Al("madde.secureboot");

        if (d.SecureBootDestekli)
        {
            return new UyumlulukMaddesi(
                ad, UyumlulukDurumu.Gecti, _metinler.Al("madde.secureboot.gecti"));
        }

        if (!d.UefiMi)
        {
            return new UyumlulukMaddesi(
                ad,
                UyumlulukDurumu.Kaldi,
                _metinler.Al("madde.secureboot.eskibios"),
                _metinler.Al("madde.win10.kisa"));
        }

        return new UyumlulukMaddesi(
            ad,
            UyumlulukDurumu.AcilabilirDurumda,
            _metinler.Al("madde.secureboot.kapali"),
            _metinler.Al("madde.secureboot.kapali.yapmali"),
            _metinler.Al("bios.ayar.secureboot"));
    }

    private UyumlulukMaddesi UefiMaddesi(DonanimRaporu d)
    {
        var ad = _metinler.Al("madde.uefi");

        return d.UefiMi
            ? new UyumlulukMaddesi(ad, UyumlulukDurumu.Gecti, _metinler.Al("madde.uefi.gecti"))
            : new UyumlulukMaddesi(
                ad,
                UyumlulukDurumu.Kaldi,
                _metinler.Al("madde.uefi.yok"),
                _metinler.Al("madde.uefi.yok.yapmali"));
    }

    private UyumlulukMaddesi RamMaddesi(DonanimRaporu d)
    {
        var ad = _metinler.Al("madde.ram");
        var gb = $"{d.RamBayt / (1024.0 * 1024 * 1024):0.#}";

        return d.RamBayt >= AsgariRam
            ? new UyumlulukMaddesi(ad, UyumlulukDurumu.Gecti, _metinler.Al("madde.ram.gecti", gb))
            : new UyumlulukMaddesi(
                ad,
                UyumlulukDurumu.Kaldi,
                _metinler.Al("madde.ram.az", gb),
                _metinler.Al("madde.ram.az.yapmali"));
    }

    private UyumlulukMaddesi DiskMaddesi(DonanimRaporu d)
    {
        var ad = _metinler.Al("madde.disk");
        var gb = $"{d.SistemDiskiBoyutBayt / (1000.0 * 1000 * 1000):0}";

        return d.SistemDiskiBoyutBayt >= AsgariDisk
            ? new UyumlulukMaddesi(ad, UyumlulukDurumu.Gecti, _metinler.Al("madde.disk.gecti", gb))
            : new UyumlulukMaddesi(
                ad,
                UyumlulukDurumu.Kaldi,
                _metinler.Al("madde.disk.az", gb),
                _metinler.Al("madde.disk.az.yapmali"));
    }
}
