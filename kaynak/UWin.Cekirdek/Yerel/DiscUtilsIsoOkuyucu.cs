using System.Diagnostics;
using DiscUtils;
using DiscUtils.Iso9660;
using DiscUtils.Udf;
using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Yerel;

/// <summary>
/// IIsoOkuyucu'nun DiscUtils tabanli gercek uygulamasi.
///
/// Once UDF denenir, sonra ISO9660. Sirasi onemlidir: modern Windows
/// ISO'lari UDF ile yazilir ve icindeki ISO9660 katmani yalnizca eski
/// sistemlere "bu diski okuyamazsin" diyen tek bir README.TXT tasir.
/// ISO9660 ile acilirsa dosya listesi 1 satir doner, install.wim
/// bulunamaz ve USB acilmayan bir disk olarak biter.
/// </summary>
public sealed class DiscUtilsIsoOkuyucu : IIsoOkuyucu
{
    public IsoIcerigi Oku(string isoYolu)
    {
        using var akis = File.OpenRead(isoYolu);
        using var dosyaSistemi = DosyaSistemiAc(akis);

        List<IsoDosyasi> dosyalar = [];
        Gez(dosyaSistemi, string.Empty, dosyalar);

        return new IsoIcerigi(dosyalar);
    }

    /// <summary>UDF varsa onu, yoksa ISO9660'i acar.</summary>
    private static DiscFileSystem DosyaSistemiAc(Stream akis)
    {
        akis.Position = 0;

        if (UdfReader.Detect(akis))
        {
            akis.Position = 0;
            return new UdfReader(akis);
        }

        akis.Position = 0;
        return new CDReader(akis, joliet: true);
    }

    private static void Gez(DiscFileSystem fs, string klasor, List<IsoDosyasi> toplam)
    {
        foreach (var dosya in fs.GetFiles(klasor))
        {
            // DiscUtils yollari ters bolu ile doner; ic modelimiz her yerde
            // duz bolu kullanir.
            var goreli = dosya.TrimStart('\\').Replace('\\', '/');
            toplam.Add(new IsoDosyasi(goreli, fs.GetFileLength(dosya)));
        }

        foreach (var altKlasor in fs.GetDirectories(klasor))
            Gez(fs, altKlasor, toplam);
    }

    public async Task DosyaCikarAsync(
        string isoYolu, string goreliYol, string hedefYol, CancellationToken iptal = default)
    {
        using var akis = File.OpenRead(isoYolu);
        using var dosyaSistemi = DosyaSistemiAc(akis);

        var isoYol = goreliYol.Replace('/', '\\');

        await using var kaynak = dosyaSistemi.OpenFile(isoYol, FileMode.Open, FileAccess.Read);
        await using var hedef = File.Create(hedefYol);

        await kaynak.CopyToAsync(hedef, iptal);
    }

    public async Task<IReadOnlyList<string>> WimBolAsync(
        string wimYolu,
        string hedefKlasor,
        long parcaBoyutuBayt,
        IProgress<double>? ilerleme = null,
        CancellationToken iptal = default)
    {
        var swmYolu = Path.Combine(hedefKlasor, "install.swm");
        var parcaMb = parcaBoyutuBayt / (1024 * 1024);

        using var surec = new Process
        {
            StartInfo = new ProcessStartInfo(
                "dism.exe",
                $"/Split-Image /ImageFile:\"{wimYolu}\" /SWMFile:\"{swmYolu}\" /FileSize:{parcaMb}")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        surec.Start();

        var cikti = await surec.StandardOutput.ReadToEndAsync(iptal);
        await surec.WaitForExitAsync(iptal);

        if (surec.ExitCode != 0)
            throw new IOException($"WIM bolunemedi (kod {surec.ExitCode}): {cikti}");

        ilerleme?.Report(100);

        return Directory.GetFiles(hedefKlasor, "*.swm");
    }
}
