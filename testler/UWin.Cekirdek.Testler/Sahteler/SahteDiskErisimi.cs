using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Testler.Sahteler;

/// <summary>
/// Bellek ici disk. Tum cagrilari sirasiyla kaydeder ve hicbir gercek
/// diske dokunmaz. YazmaServisi testlerinin temelidir.
/// </summary>
public sealed class SahteDiskErisimi : IYerelDiskErisimi
{
    private readonly List<HamDiskGirisi> _diskler = [];
    private readonly List<string> _cagrilar = [];
    private readonly Dictionary<string, string> _yazilanDosyalar = [];

    /// <summary>Cagri gecmisi: "BolumTablosuYaz(1, Gpt)" bicimindedir.</summary>
    public IReadOnlyList<string> Cagrilar => _cagrilar;

    /// <summary>Diske yazilmis dosyalar: goreli yol -> kaynak yol.</summary>
    public IReadOnlyDictionary<string, string> YazilanDosyalar => _yazilanDosyalar;

    public bool KilitAcikMi { get; private set; }

    /// <summary>Verilirse bu ad tasiyan cagri InvalidOperationException firlatir.</summary>
    public string? BasarisizOlacakCagri { get; set; }

    public string? SahteSurucuYolu { get; set; }

    public SahteDiskErisimi DiskEkle(HamDiskGirisi disk)
    {
        _diskler.Add(disk);
        return this;
    }

    /// <summary>Listeden bir diski cikarir - yazma sirasinda USB cekilmesini taklit eder.</summary>
    public void DiskiCikar(int diskNumarasi) => _diskler.RemoveAll(d => d.DiskNumarasi == diskNumarasi);

    public IReadOnlyList<HamDiskGirisi> DiskleriListele() => _diskler;

    public string? SurucuYoluBul(int diskNumarasi)
    {
        Kaydet($"SurucuYoluBul({diskNumarasi})");
        return SahteSurucuYolu;
    }

    public Task<IDisposable> DiskiKilitleAsync(int diskNumarasi, CancellationToken iptal = default)
    {
        Kaydet($"DiskiKilitle({diskNumarasi})");
        KilitAcikMi = true;
        return Task.FromResult<IDisposable>(new Kilit(() => KilitAcikMi = false));
    }

    public Task BirimleriSokAsync(int diskNumarasi, CancellationToken iptal = default)
    {
        Kaydet($"BirimleriSok({diskNumarasi})");
        return Task.CompletedTask;
    }

    public Task BolumTablosuYazAsync(int diskNumarasi, BolumTablosuTipi tip, CancellationToken iptal = default)
    {
        Kaydet($"BolumTablosuYaz({diskNumarasi}, {tip})");
        return Task.CompletedTask;
    }

    public Task BicimlendirAsync(int diskNumarasi, string dosyaSistemi, string etiket, CancellationToken iptal = default)
    {
        Kaydet($"Bicimlendir({diskNumarasi}, {dosyaSistemi}, {etiket})");
        return Task.CompletedTask;
    }

    /// <summary>
    /// true ise kopyalanan dosyalar <see cref="SahteSurucuYolu"/> altina
    /// gercekten yazilir. Geri okuma dogrulamasi dosya sistemine baktigi
    /// icin, o yolu sinamak isteyen testlerde bu acilir.
    /// </summary>
    public bool DiskeGercektenYaz { get; set; }

    /// <summary>
    /// Verilirse bu goreli yol icin diske yazilacak bayt sayisi ISO'daki
    /// boyuttan farkli olur - yarim kalmis kopyalamayi taklit eder.
    /// </summary>
    public Dictionary<string, long> BozukYazilacakDosyalar { get; } = [];

    /// <summary>ISO'daki dosya boyutlari: gercek yazmada bu kadar bayt uretilir.</summary>
    public Dictionary<string, long> DosyaBoyutlari { get; } = [];

    public Task DosyaKopyalaAsync(
        int diskNumarasi,
        string kaynakYol,
        string hedefGoreliYol,
        IProgress<double>? ilerleme = null,
        CancellationToken iptal = default)
    {
        Kaydet($"DosyaKopyala({diskNumarasi}, {hedefGoreliYol})");
        _yazilanDosyalar[hedefGoreliYol] = kaynakYol;

        if (DiskeGercektenYaz && SahteSurucuYolu is { } surucu)
            GercekDosyaYaz(surucu, hedefGoreliYol);

        ilerleme?.Report(100);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Dosyayi olusturur ve uzunlugunu ayarlar - icerigi yazmadan.
    /// Gercek ISO'lardaki install.wim gigabaytlarcadir; testte o kadar
    /// bayti gercekten uretmek ne bellege ne sureye sigar. SetLength
    /// seyrek dosya birakir: uzunluk dogru gorunur, disk yer kaplamaz.
    /// </summary>
    private void GercekDosyaYaz(string surucu, string goreliYol)
    {
        var tam = Path.Combine(surucu, goreliYol.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(tam)!);

        var boyut = BozukYazilacakDosyalar.TryGetValue(goreliYol, out var bozuk)
            ? bozuk
            : DosyaBoyutlari.GetValueOrDefault(goreliYol);

        using var akis = new FileStream(tam, FileMode.Create, FileAccess.Write);
        akis.SetLength(boyut);
    }

    public Task BootYazAsync(int diskNumarasi, BolumTablosuTipi tip, CancellationToken iptal = default)
    {
        Kaydet($"BootYaz({diskNumarasi}, {tip})");
        return Task.CompletedTask;
    }

    private void Kaydet(string cagri)
    {
        _cagrilar.Add(cagri);

        if (BasarisizOlacakCagri is not null && cagri.StartsWith(BasarisizOlacakCagri, StringComparison.Ordinal))
            throw new InvalidOperationException($"Sahte hata: {cagri}");
    }

    private sealed class Kilit(Action birak) : IDisposable
    {
        public void Dispose() => birak();
    }
}
