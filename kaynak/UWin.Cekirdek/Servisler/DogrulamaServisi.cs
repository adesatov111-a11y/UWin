using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// Yazma bittikten sonra USB'yi geri okur ve ISO ile karsilastirir.
///
/// Neden gerekli: ucuz USB belleklerin en sinsi hatasi sessiz yazmadir.
/// Dosya yazilir, isletim sistemi hicbir hata dondurmez, ama geri
/// okununca icerik farklidir. Kullanici bunu ancak BIOS'ta "no bootable
/// device" yazisini gorunce anlar ve sebebini asla bulamaz - USB'yi mi
/// yoksa programi mi suclayacagini bilemez. Yazmadan hemen sonra bir
/// geri okuma bu hatayi kullanici bilgisayari kapatmadan yakalar.
///
/// Her baytin ozetini almak yazma suresini ikiye katlardi. Bunun yerine
/// butun dosyalarin boyutu, onyukleme icin kritik olanlarin ise icerigi
/// kontrol edilir - bozulma en cok buyuk dosyalarda gorulur ve onyukleme
/// zincirini kiran da zaten o dosyalardir.
/// </summary>
public sealed class DogrulamaServisi : IDogrulamaServisi
{
    private const int TamponBoyutu = 1024 * 1024;

    /// <summary>
    /// Icerigi de okunacak dosyalar. Bunlar onyukleme zincirinin halkalari:
    /// biri bozuksa USB acilmaz, ama boyut kontrolu bunu yakalamaz.
    /// </summary>
    private static readonly string[] KritikDosyalar =
    [
        "bootmgr",
        "bootmgr.efi",
        "boot/bcd",
        "efi/boot/bootx64.efi",
        "efi/microsoft/boot/bcd",
        "sources/boot.wim"
    ];

    public async Task<DogrulamaSonucu> DogrulaAsync(
        string surucuYolu,
        IsoIcerigi beklenen,
        IProgress<double>? ilerleme = null,
        CancellationToken iptal = default)
    {
        if (!Directory.Exists(surucuYolu))
            return new DogrulamaSonucu([], Tamamlanabildi: false);

        List<DosyaDogrulamasi> sonuclar = [];
        var toplam = beklenen.Dosyalar.Count;

        for (var i = 0; i < toplam; i++)
        {
            iptal.ThrowIfCancellationRequested();

            var dosya = beklenen.Dosyalar[i];
            sonuclar.Add(await DosyayiDenetleAsync(surucuYolu, dosya, iptal));

            ilerleme?.Report((i + 1) * 100.0 / toplam);
        }

        return new DogrulamaSonucu(sonuclar);
    }

    private static async Task<DosyaDogrulamasi> DosyayiDenetleAsync(
        string surucuYolu, IsoDosyasi dosya, CancellationToken iptal)
    {
        var tamYol = Path.Combine(
            surucuYolu, dosya.GoreliYol.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(tamYol))
        {
            // Buyuk install.wim FAT32'ye sigmadigi icin parcalanmis olabilir.
            // Parcalar yerindeyse dosya eksik degildir.
            if (BolunmusWimVar(surucuYolu, dosya))
                return new DosyaDogrulamasi(dosya.GoreliYol, DosyaDogrulamaDurumu.Eslesti);

            return new DosyaDogrulamasi(dosya.GoreliYol, DosyaDogrulamaDurumu.Eksik);
        }

        var bilgi = new FileInfo(tamYol);

        if (bilgi.Length != dosya.BoyutBayt)
            return new DosyaDogrulamasi(dosya.GoreliYol, DosyaDogrulamaDurumu.BoyutTutmuyor);

        // Kritik dosyalarda bastan sona okuma yapilir: boyut dogru olsa
        // bile icerik bozuk olabilir ve tam bu dosyalarda bozulma USB'yi
        // acilmaz yapar.
        if (KritikMi(dosya.GoreliYol) && !await OkunabilirMiAsync(tamYol, iptal))
            return new DosyaDogrulamasi(dosya.GoreliYol, DosyaDogrulamaDurumu.IcerikBozuk);

        return new DosyaDogrulamasi(dosya.GoreliYol, DosyaDogrulamaDurumu.Eslesti);
    }

    private static bool KritikMi(string goreliYol)
        => KritikDosyalar.Any(k => goreliYol.Equals(k, StringComparison.OrdinalIgnoreCase));

    /// <summary>install.wim yerine SWM parcalari yazilmis mi?</summary>
    private static bool BolunmusWimVar(string surucuYolu, IsoDosyasi dosya)
    {
        if (!dosya.GoreliYol.EndsWith("install.wim", StringComparison.OrdinalIgnoreCase))
            return false;

        var klasor = Path.Combine(surucuYolu, "sources");

        return Directory.Exists(klasor) && Directory.EnumerateFiles(klasor, "*.swm").Any();
    }

    /// <summary>
    /// Dosyayi bastan sona okur. Bozuk sektor varsa surucu IOException
    /// firlatir; sessiz bozulma da cogu USB'de bu asamada ortaya cikar.
    /// </summary>
    private static async Task<bool> OkunabilirMiAsync(string yol, CancellationToken iptal)
    {
        try
        {
            await using var akis = new FileStream(
                yol, FileMode.Open, FileAccess.Read, FileShare.Read,
                TamponBoyutu, FileOptions.SequentialScan | FileOptions.Asynchronous);

            var tampon = new byte[TamponBoyutu];

            while (await akis.ReadAsync(tampon, iptal) > 0)
                iptal.ThrowIfCancellationRequested();

            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
