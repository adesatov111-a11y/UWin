using System.Management;
using System.Runtime.Versioning;
using UWin.Cekirdek.Arayuzler;

namespace UWin.Cekirdek.Yerel;

/// <summary>IWmiOkuyucu icin System.Management tabanli gercek uygulama.</summary>
[SupportedOSPlatform("windows")]
public sealed class WmiOkuyucu : IWmiOkuyucu
{
    private readonly string _adAlani;

    public WmiOkuyucu(string adAlani = @"root\CIMV2") => _adAlani = adAlani;

    public IEnumerable<IReadOnlyDictionary<string, string>> Sorgula(string sinif, params string[] alanlar)
    {
        var secim = alanlar.Length == 0 ? "*" : string.Join(", ", alanlar);
        List<Dictionary<string, string>> sonuc = [];

        try
        {
            using var arayici = new ManagementObjectSearcher(_adAlani, $"SELECT {secim} FROM {sinif}");

            foreach (var nesne in arayici.Get().Cast<ManagementObject>())
            {
                Dictionary<string, string> satir = [];
                foreach (var ozellik in nesne.Properties)
                    satir[ozellik.Name] = ozellik.Value?.ToString() ?? string.Empty;
                sonuc.Add(satir);
            }
        }
        catch (Exception e) when (e is ManagementException or UnauthorizedAccessException)
        {
            // Sinif bu makinede olmayabilir (TPM'siz sistemde Win32_Tpm) veya
            // okunmasi yonetici yetkisi isteyebilir (TPM ad alani boyledir).
            // Bos liste donmek dogru davranis: servis bunu "yok" olarak yorumlar.
        }

        return sonuc;
    }
}
