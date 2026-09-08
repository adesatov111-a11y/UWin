using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>Makineyi salt okuma modunda tanir. Hicbir yazma islemi yapmaz.</summary>
public sealed class DonanimServisi : IDonanimServisi
{
    /// <summary>Uzun uretici adlarini kullanicinin tanidigi marka adina indirger.</summary>
    private static readonly (string Anahtar, string Marka)[] MarkaEslemesi =
    [
        ("micro-star", "MSI"),
        ("asustek", "ASUS"),
        ("gigabyte", "Gigabyte"),
        ("asrock", "ASRock"),
        ("hewlett", "HP"),
        ("dell", "Dell"),
        ("lenovo", "Lenovo"),
        ("acer", "Acer"),
        ("casper", "Casper"),
        ("monster", "Monster"),
        ("toshiba", "Toshiba"),
        ("samsung", "Samsung")
    ];

    private readonly IWmiOkuyucu _wmi;
    private readonly IWmiOkuyucu _tpmWmi;
    private readonly bool _uefiMi;
    private readonly bool _secureBootDestekli;

    /// <param name="tpmWmi">
    /// TPM, root\CIMV2 altinda degil root\CIMV2\Security\MicrosoftTpm
    /// ad alaninda durur ve okunmasi yonetici yetkisi ister. Verilmezse
    /// ana okuyucu kullanilir (testlerde tek sahte okuyucu yeterlidir).
    /// </param>
    public DonanimServisi(
        IWmiOkuyucu wmi,
        bool uefiMi,
        bool secureBootDestekli,
        IWmiOkuyucu? tpmWmi = null)
    {
        _wmi = wmi;
        _tpmWmi = tpmWmi ?? wmi;
        _uefiMi = uefiMi;
        _secureBootDestekli = secureBootDestekli;
    }

    public Task<DonanimRaporu> RaporAlAsync(CancellationToken iptal = default)
    {
        var anakart = IlkSatir("Win32_BaseBoard");
        var islemci = IlkSatir("Win32_Processor");
        var sistem = IlkSatir("Win32_ComputerSystem");
        var tpm = _tpmWmi.Sorgula("Win32_Tpm").FirstOrDefault();
        var disk = IlkSatir("Win32_DiskDrive");

        var rapor = new DonanimRaporu(
            AnakartUretici: MarkaNormalize(Deger(anakart, "Manufacturer", "Bilinmiyor")),
            AnakartModel: Deger(anakart, "Product", "Bilinmiyor"),
            Islemci: Deger(islemci, "Name", "Bilinmiyor"),
            RamBayt: Sayi(sistem, "TotalPhysicalMemory"),
            TpmSurumu: tpm is null ? null : Deger(tpm, "SpecVersion", "").Split(',')[0].Trim(),
            UefiMi: _uefiMi,
            SecureBootDestekli: _secureBootDestekli,
            SistemDiskiBoyutBayt: Sayi(disk, "Size"));

        return Task.FromResult(rapor);
    }

    private IReadOnlyDictionary<string, string>? IlkSatir(string sinif)
        => _wmi.Sorgula(sinif).FirstOrDefault();

    private static string Deger(IReadOnlyDictionary<string, string>? satir, string alan, string varsayilan)
        => satir is not null && satir.TryGetValue(alan, out var d) && !string.IsNullOrWhiteSpace(d)
            ? d
            : varsayilan;

    private static long Sayi(IReadOnlyDictionary<string, string>? satir, string alan)
        => long.TryParse(Deger(satir, alan, "0"), out var s) ? s : 0;

    /// <summary>Bilinen markalarda kisa ad doner; taninmayanda ham degeri korur.</summary>
    private static string MarkaNormalize(string ham)
    {
        var kucuk = ham.ToLowerInvariant();

        foreach (var (anahtar, marka) in MarkaEslemesi)
            if (kucuk.Contains(anahtar, StringComparison.Ordinal))
                return marka;

        return ham;
    }
}
