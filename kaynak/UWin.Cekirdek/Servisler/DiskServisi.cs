using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// Diskleri okur ve risk sinifina ayirir. Salt okuma: bu servis
/// hicbir kosulda diske yazmaz.
/// </summary>
public sealed class DiskServisi : IDiskServisi
{
    private static readonly string[] HariciVeriYollari = ["USB", "1394", "Thunderbolt"];

    /// <summary>
    /// Bu boyutun ustundeki "cikarilabilir" aygitlar pratikte harici disktir.
    /// WMI, USB harici HDD'leri de Removable Media diye bildirir; boyut bakmadan
    /// onlar varsayilan listeye girer ve kullanici ad yazma kilidi olmadan
    /// 1 TB'lik arsivini secebilirdi.
    /// </summary>
    private const long HariciDiskEsigi = 128L * 1000 * 1000 * 1000;

    private readonly IYerelDiskErisimi _erisim;

    public DiskServisi(IYerelDiskErisimi erisim) => _erisim = erisim;

    public IReadOnlyList<DiskBilgisi> DiskleriListele(bool gelismisMod = false)
    {
        var hepsi = _erisim.DiskleriListele().Select(Cevir).ToList();

        return gelismisMod
            ? hepsi
            : hepsi.Where(d => d.Sinif == DiskSinifi.UsbBellek).ToList();
    }

    public DiskBilgisi? DiskBul(int diskNumarasi)
        => _erisim.DiskleriListele()
            .Where(h => h.DiskNumarasi == diskNumarasi)
            .Select(Cevir)
            .FirstOrDefault();

    public bool HedefHalaGecerliMi(DiskBilgisi secilen)
    {
        var guncel = DiskBul(secilen.DiskNumarasi);

        return guncel is not null
            && guncel.SeriNumarasi == secilen.SeriNumarasi
            && guncel.BoyutBayt == secilen.BoyutBayt;
    }

    private static DiskBilgisi Cevir(HamDiskGirisi ham) => new(
        DiskNumarasi: ham.DiskNumarasi,
        Ad: ham.Model,
        Model: ham.Model,
        BoyutBayt: ham.BoyutBayt,
        Sinif: Siniflandir(ham),
        SeriNumarasi: ham.SeriNumarasi,
        KullanilanBayt: ham.KullanilanBayt);

    /// <summary>
    /// Sira onemlidir: sistem diski kontrolu her zaman once gelir.
    /// Cikarilabilir bir sistem diski bile SistemDiski sayilir.
    /// </summary>
    private static DiskSinifi Siniflandir(HamDiskGirisi ham)
    {
        if (ham.SistemDiskiMi)
            return DiskSinifi.SistemDiski;

        if (ham.CikarilabilirMi)
            return ham.BoyutBayt > HariciDiskEsigi
                ? DiskSinifi.HariciDisk
                : DiskSinifi.UsbBellek;

        if (HariciVeriYollari.Any(y => ham.VeriYolu.Contains(y, StringComparison.OrdinalIgnoreCase)))
            return DiskSinifi.HariciDisk;

        return DiskSinifi.DahiliDisk;
    }
}
