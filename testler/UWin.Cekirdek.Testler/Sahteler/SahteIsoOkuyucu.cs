using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Testler.Sahteler;

/// <summary>Gercek ISO acmadan icerik tanimlamaya yarayan test ikizi.</summary>
public sealed class SahteIsoOkuyucu : IIsoOkuyucu
{
    private readonly List<IsoDosyasi> _dosyalar = [];

    public List<string> BolunenWimler { get; } = [];

    public SahteIsoOkuyucu DosyaEkle(string goreliYol, long boyutBayt)
    {
        _dosyalar.Add(new IsoDosyasi(goreliYol, boyutBayt));
        return this;
    }

    /// <summary>Tipik bir Windows ISO icerigi: kucuk WIM, FAT32'ye sigar.</summary>
    public static SahteIsoOkuyucu KucukWimli() => new SahteIsoOkuyucu()
        .DosyaEkle("bootmgr", 400_000)
        .DosyaEkle("boot/bcd", 260_000)
        .DosyaEkle("efi/boot/bootx64.efi", 1_500_000)
        .DosyaEkle("sources/install.wim", 3_500_000_000)
        .DosyaEkle("sources/boot.wim", 400_000_000);

    /// <summary>4 GB ustu WIM iceren ISO: FAT32'ye sigmaz, bolunmesi gerekir.</summary>
    public static SahteIsoOkuyucu BuyukWimli() => new SahteIsoOkuyucu()
        .DosyaEkle("bootmgr", 400_000)
        .DosyaEkle("boot/bcd", 260_000)
        .DosyaEkle("efi/boot/bootx64.efi", 1_500_000)
        .DosyaEkle("sources/install.wim", 5_200_000_000)
        .DosyaEkle("sources/boot.wim", 400_000_000);

    public IsoIcerigi Oku(string isoYolu) => new(_dosyalar);

    public Task DosyaCikarAsync(string isoYolu, string goreliYol, string hedefYol, CancellationToken iptal = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<string>> WimBolAsync(
        string wimYolu,
        string hedefKlasor,
        long parcaBoyutuBayt,
        IProgress<double>? ilerleme = null,
        CancellationToken iptal = default)
    {
        BolunenWimler.Add(wimYolu);
        ilerleme?.Report(100);

        IReadOnlyList<string> parcalar =
        [
            Path.Combine(hedefKlasor, "install.swm"),
            Path.Combine(hedefKlasor, "install2.swm")
        ];

        return Task.FromResult(parcalar);
    }
}
