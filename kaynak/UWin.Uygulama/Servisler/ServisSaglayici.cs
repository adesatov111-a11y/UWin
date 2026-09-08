using Microsoft.Win32;
using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Servisler;
using UWin.Cekirdek.Yerel;

namespace UWin.Uygulama.Servisler;

/// <summary>
/// Tum bagimliliklari tek yerden kurar.
/// Arayuz katmani yerel katmani yalnizca buradan tanir.
/// </summary>
public static class ServisSaglayici
{
    private const string SecureBootAnahtari = @"SYSTEM\CurrentControlSet\Control\SecureBoot\State";

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(30) };

    /// <summary>
    /// Metin saglayici en basta kurulur ve butun servislere acikca
    /// verilir.
    ///
    /// Sirasi onemlidir: statik alan baslatmalari bildirim sirasinda
    /// calisir. Asagida bildirilip yukarida kullanilan bir alan null
    /// gelir ve servisler sessizce kendi saglayicilarini kurar - o da
    /// dil degistiginde eski dilde kalmalari demektir.
    /// </summary>
    public static IMetinSaglayici Metinler { get; } = new MetinSaglayici();

    public static IYerelDiskErisimi DiskErisimi { get; } = new Win32DiskErisimi();

    public static IIsoOkuyucu IsoOkuyucu { get; } = new DiscUtilsIsoOkuyucu();

    public static IDiskServisi DiskServisi { get; } = new DiskServisi(DiskErisimi);

    public static IIsoTanimaServisi IsoTanimaServisi { get; } = new IsoTanimaServisi(IsoOkuyucu);

    public static IDurumIsaretleyici DurumIsaretleyici { get; } = new DurumIsaretleyici();

    public static IDogrulamaServisi DogrulamaServisi { get; } = new DogrulamaServisi();

    public static IYazmaServisi YazmaServisi { get; } =
        new YazmaServisi(
            DiskErisimi, DiskServisi, IsoOkuyucu, DurumIsaretleyici, DogrulamaServisi, Metinler);

    /// <summary>Kurulum USB'sini gunluk kullanima donduren yikici servis.</summary>
    public static IGeriKazanmaServisi GeriKazanmaServisi { get; } =
        new GeriKazanmaServisi(DiskErisimi, DiskServisi, Metinler);

    public static IKurtarmaServisi KurtarmaServisi { get; } = new KurtarmaServisi(Metinler);

    /// <summary>Bellegin gercek kapasitesini sinar; bolum tablosuna dokunmaz.</summary>
    public static ISaglikServisi SaglikServisi { get; } = new SaglikServisi(Metinler);

    public static IUyumlulukServisi UyumlulukServisi { get; } = new UyumlulukServisi(Metinler);

    public static IAmacServisi AmacServisi { get; } = new AmacServisi(Metinler);

    public static IVeriServisi VeriServisi { get; } = new VeriServisi(Metinler);

    public static ISurumServisi SurumServisi { get; } = new SurumServisi(Http);

    public static IIndirmeServisi IndirmeServisi { get; } = new IndirmeServisi(Http, Metinler);

    public static IRehberServisi RehberServisi { get; } = new RehberServisi(Metinler);

    /// <summary>Kalici kullanici tercihleri (su an yalnizca dil).</summary>
    public static IAyarServisi AyarServisi { get; } = new AyarServisi();

    public static IYolculukServisi YolculukServisi { get; } =
        new YolculukServisi(RehberServisi, Metinler);

    /// <summary>TPM bilgisi ayri bir WMI ad alanindadir ve yonetici yetkisi ister.</summary>
    private static readonly IWmiOkuyucu TpmOkuyucu =
        new WmiOkuyucu(@"root\CIMV2\Security\MicrosoftTpm");

    public static IDonanimServisi DonanimServisi { get; } = new DonanimServisi(
        new WmiOkuyucu(),
        uefiMi: UefiMiTespit(),
        secureBootDestekli: SecureBootDestekliMiTespit(),
        tpmWmi: TpmOkuyucu);

    /// <summary>
    /// UEFI modu, SecureBoot kayit anahtarinin varligindan anlasilir.
    /// Bu anahtar yalnizca UEFI ile onyuklenen sistemlerde bulunur.
    /// </summary>
    private static bool UefiMiTespit() => AnahtarOku(anahtar => anahtar is not null);

    private static bool SecureBootDestekliMiTespit()
        => AnahtarOku(anahtar => anahtar?.GetValue("UEFISecureBootEnabled") is not null);

    private static bool AnahtarOku(Func<RegistryKey?, bool> okuyucu)
    {
        try
        {
            using var anahtar = Registry.LocalMachine.OpenSubKey(SecureBootAnahtari);
            return okuyucu(anahtar);
        }
        catch (Exception e) when (e is UnauthorizedAccessException or System.Security.SecurityException)
        {
            // Erisim yoksa "desteklenmiyor" varsayilir. Bu, kullaniciya
            // yanlislikla "Windows 11 kurulabilir" demekten guvenlidir.
            return false;
        }
    }
}
