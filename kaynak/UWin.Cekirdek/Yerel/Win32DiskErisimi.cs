using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Yerel;

/// <summary>
/// IYerelDiskErisimi'nin gercek uygulamasi. Listeleme WMI ile,
/// yikici islemler diskpart ve Win32 cagrilari ile yapilir.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class Win32DiskErisimi : IYerelDiskErisimi
{
    private static readonly string[] CikarilabilirOrtamlar = ["Removable", "External"];

    public IReadOnlyList<HamDiskGirisi> DiskleriListele()
    {
        var sistemDiskNumarasi = SistemDiskiNumarasiBul();
        List<HamDiskGirisi> diskler = [];

        try
        {
            using var arayici = new ManagementObjectSearcher(
                "SELECT Index, Model, SerialNumber, Size, MediaType, InterfaceType FROM Win32_DiskDrive");

            foreach (var nesne in arayici.Get().Cast<ManagementObject>())
            {
                using (nesne)
                {
                    var numara = Convert.ToInt32(nesne["Index"] ?? -1);
                    if (numara < 0)
                        continue;

                    var ortamTipi = nesne["MediaType"]?.ToString() ?? string.Empty;

                    diskler.Add(new HamDiskGirisi(
                        DiskNumarasi: numara,
                        Model: (nesne["Model"]?.ToString() ?? "Bilinmeyen disk").Trim(),
                        SeriNumarasi: (nesne["SerialNumber"]?.ToString() ?? string.Empty).Trim(),
                        BoyutBayt: Convert.ToInt64(nesne["Size"] ?? 0L),
                        CikarilabilirMi: CikarilabilirOrtamlar.Any(
                            o => ortamTipi.Contains(o, StringComparison.OrdinalIgnoreCase)),
                        VeriYolu: nesne["InterfaceType"]?.ToString() ?? "Bilinmiyor",
                        SistemDiskiMi: numara == sistemDiskNumarasi,
                        KullanilanBayt: KullanilanBaytHesapla(numara)));
                }
            }
        }
        catch (ManagementException)
        {
            // WMI erisilemiyorsa bos liste doner; arayuz "disk bulunamadi" gosterir.
        }

        return diskler;
    }

    /// <summary>Windows'un kurulu oldugu surucunun disk numarasini bulur.</summary>
    private static int SistemDiskiNumarasiBul()
    {
        var sistemSurucusu = Path.GetPathRoot(Environment.SystemDirectory)?.TrimEnd('\\');
        if (sistemSurucusu is null)
            return -1;

        try
        {
            using var bolumSorgu = new ManagementObjectSearcher(
                $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{sistemSurucusu}'}} "
                + "WHERE AssocClass = Win32_LogicalDiskToPartition");

            foreach (var bolum in bolumSorgu.Get().Cast<ManagementObject>())
            {
                using (bolum)
                {
                    using var diskSorgu = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{bolum["DeviceID"]}'}} "
                        + "WHERE AssocClass = Win32_DiskDriveToDiskPartition");

                    foreach (var disk in diskSorgu.Get().Cast<ManagementObject>())
                    {
                        using (disk)
                            return Convert.ToInt32(disk["Index"] ?? -1);
                    }
                }
            }
        }
        catch (ManagementException)
        {
            // Cozulemezse -1 doner: hicbir disk sistem diski isaretlenmez.
            // DiskServisi ayrica cikarilabilirlik kontrolu yaptigi icin
            // dahili diskler yine varsayilan listede gorunmez.
        }

        return -1;
    }

    private static long KullanilanBaytHesapla(int diskNumarasi)
    {
        long kullanilan = 0;

        foreach (var mantiksal in MantiksalSurucular(diskNumarasi))
        {
            using (mantiksal)
            {
                var toplam = Convert.ToInt64(mantiksal["Size"] ?? 0L);
                var bos = Convert.ToInt64(mantiksal["FreeSpace"] ?? 0L);
                kullanilan += Math.Max(0, toplam - bos);
            }
        }

        return kullanilan;
    }

    public string? SurucuYoluBul(int diskNumarasi)
    {
        foreach (var mantiksal in MantiksalSurucular(diskNumarasi))
        {
            using (mantiksal)
            {
                var harf = mantiksal["DeviceID"]?.ToString();
                if (!string.IsNullOrEmpty(harf))
                    return harf + Path.DirectorySeparatorChar;
            }
        }

        return null;
    }

    /// <summary>Bir fiziksel diske bagli mantiksal surucularin WMI kayitlarini gezer.</summary>
    private static IEnumerable<ManagementObject> MantiksalSurucular(int diskNumarasi)
    {
        List<ManagementObject> sonuc = [];

        try
        {
            using var bolumSorgu = new ManagementObjectSearcher(
                $@"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='\\.\PHYSICALDRIVE{diskNumarasi}'}} "
                + "WHERE AssocClass = Win32_DiskDriveToDiskPartition");

            foreach (var bolum in bolumSorgu.Get().Cast<ManagementObject>())
            {
                using (bolum)
                {
                    using var mantiksalSorgu = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{bolum["DeviceID"]}'}} "
                        + "WHERE AssocClass = Win32_LogicalDiskToPartition");

                    sonuc.AddRange(mantiksalSorgu.Get().Cast<ManagementObject>());
                }
            }
        }
        catch (ManagementException)
        {
            // Bolum okunamiyorsa bos liste doner.
        }

        return sonuc;
    }

    public Task<IDisposable> DiskiKilitleAsync(int diskNumarasi, CancellationToken iptal = default)
    {
        var tanitici = Win32Cagrilari.CreateFile(
            $@"\\.\PHYSICALDRIVE{diskNumarasi}",
            Win32Cagrilari.GenericRead | Win32Cagrilari.GenericWrite,
            Win32Cagrilari.FileShareRead | Win32Cagrilari.FileShareWrite,
            nint.Zero,
            Win32Cagrilari.OpenExisting,
            0,
            nint.Zero);

        if (tanitici == Win32Cagrilari.GecersizTanitici)
        {
            throw new UnauthorizedAccessException(
                $"Disk acilamadi (Win32 hata {Marshal.GetLastWin32Error()}). Yonetici yetkisi gerekiyor.");
        }

        return Task.FromResult<IDisposable>(new DiskKilidi(tanitici));
    }

    public Task BirimleriSokAsync(int diskNumarasi, CancellationToken iptal = default)
        => DiskpartCalistirAsync($"select disk {diskNumarasi}\nclean\n", iptal);

    public Task BolumTablosuYazAsync(int diskNumarasi, BolumTablosuTipi tip, CancellationToken iptal = default)
    {
        var tipAdi = tip == BolumTablosuTipi.Gpt ? "gpt" : "mbr";

        return DiskpartCalistirAsync(
            $"select disk {diskNumarasi}\nclean\nconvert {tipAdi}\ncreate partition primary\n"
            + (tip == BolumTablosuTipi.Mbr ? "active\n" : string.Empty),
            iptal);
    }

    public Task BicimlendirAsync(
        int diskNumarasi, string dosyaSistemi, string etiket, CancellationToken iptal = default)
        => DiskpartCalistirAsync(
            $"select disk {diskNumarasi}\nselect partition 1\n"
            + $"format fs={dosyaSistemi} label=\"{etiket}\" quick\nassign\n",
            iptal);

    public async Task DosyaKopyalaAsync(
        int diskNumarasi,
        string kaynakYol,
        string hedefGoreliYol,
        IProgress<double>? ilerleme = null,
        CancellationToken iptal = default)
    {
        var surucu = SurucuYoluBul(diskNumarasi)
            ?? throw new IOException("No drive letter found for the formatted disk.");

        var hedefTamYol = Path.Combine(surucu, hedefGoreliYol.Replace('/', Path.DirectorySeparatorChar));

        var hedefKlasor = Path.GetDirectoryName(hedefTamYol);
        if (hedefKlasor is not null)
            Directory.CreateDirectory(hedefKlasor);

        await using var kaynak = File.OpenRead(kaynakYol);
        await using var hedef = File.Create(hedefTamYol);
        await kaynak.CopyToAsync(hedef, iptal);

        ilerleme?.Report(100);
    }

    public Task BootYazAsync(int diskNumarasi, BolumTablosuTipi tip, CancellationToken iptal = default)
    {
        // GPT/UEFI medyada onyukleyici ISO'dan kopyalanan efi/boot dosyalarindan gelir;
        // MBR/Legacy medyada ise MBR onyukleme kodu ayrica yazilmalidir.
        if (tip != BolumTablosuTipi.Mbr)
            return Task.CompletedTask;

        var surucu = SurucuYoluBul(diskNumarasi)
            ?? throw new IOException("No drive letter found for writing boot records.");

        return SureciCalistirAsync("bootsect.exe", $"/nt60 {surucu.TrimEnd('\\')} /mbr", iptal);
    }

    private static async Task DiskpartCalistirAsync(string betik, CancellationToken iptal)
    {
        var betikYolu = Path.Combine(Path.GetTempPath(), $"uwin-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(betikYolu, betik + "exit\n", iptal);

        try
        {
            await SureciCalistirAsync("diskpart.exe", $"/s \"{betikYolu}\"", iptal);
        }
        finally
        {
            if (File.Exists(betikYolu))
                File.Delete(betikYolu);
        }
    }

    private static async Task SureciCalistirAsync(string dosya, string argumanlar, CancellationToken iptal)
    {
        using var surec = new Process
        {
            StartInfo = new ProcessStartInfo(dosya, argumanlar)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        surec.Start();

        var cikti = await surec.StandardOutput.ReadToEndAsync(iptal);
        var hataCiktisi = await surec.StandardError.ReadToEndAsync(iptal);
        await surec.WaitForExitAsync(iptal);

        if (surec.ExitCode != 0)
            throw new IOException($"{dosya} basarisiz oldu (kod {surec.ExitCode}): {cikti} {hataCiktisi}".Trim());
    }

    private sealed class DiskKilidi(nint tanitici) : IDisposable
    {
        public void Dispose() => Win32Cagrilari.CloseHandle(tanitici);
    }
}
