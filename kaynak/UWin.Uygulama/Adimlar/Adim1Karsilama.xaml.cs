using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using UWin.Cekirdek.Modeller;
using UWin.Uygulama.Bicimlendirme;
using UWin.Uygulama.GorunumModelleri;
using UWin.Uygulama.Servisler;

namespace UWin.Uygulama.Adimlar;

/// <summary>
/// Kullanici hicbir sey yapmadan makinesinin raporunu gorur.
///
/// Rapor tek bir "uyumlu / uyumsuz" damgasi degil, madde madde bir
/// karnedir. Sebebi su: sahadaki en yaygin durum, TPM ve Secure Boot'un
/// makinede var olup BIOS'ta kapali olmasi. Microsoft'un kendi araci bu
/// makineye "uyumlu degil" der ve kullanici yeni bilgisayar bakmaya
/// gider. Oysa yapmasi gereken tek sey iki ayari acmaktir - ve o
/// ayarlarin adi anakart markasina gore degisir.
/// </summary>
public sealed partial class Adim1Karsilama : Page
{
    private SihirbazDurumu? _durum;

    public Adim1Karsilama() => InitializeComponent();

    /// <summary>Listede gosterilen tek bir gereksinim satiri.</summary>
    public sealed record MaddeSatiri(UyumlulukMaddesi Madde)
    {
        public string Ad => Madde.Ad;

        public string Aciklama => Madde.Aciklama;

        public SolidColorBrush Renk => new(Madde.Durum switch
        {
            UyumlulukDurumu.Gecti => Colors.SeaGreen,
            UyumlulukDurumu.AcilabilirDurumda => Colors.Goldenrod,
            _ => Colors.IndianRed
        });

        public string NeYapmali => Madde.NeYapmali ?? string.Empty;

        public Visibility NeYapmaliGorunurlugu =>
            string.IsNullOrEmpty(Madde.NeYapmali) ? Visibility.Collapsed : Visibility.Visible;

        public string BiosAyari => Madde.BiosAyarAdi is { } ayar
            ? ServisSaglayici.Metinler.Al("uyumluluk.bios.ayar", ayar)
            : string.Empty;

        public Visibility BiosAyariGorunurlugu =>
            Madde.BiosAyarAdi is null ? Visibility.Collapsed : Visibility.Visible;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        _durum = (SihirbazDurumu)e.Parameter;

        var metinler = ServisSaglayici.Metinler;

        BeklemeMetni.Text = metinler.Al("karsilama.bekleme");
        MaddeBasligi.Text = metinler.Al("uyumluluk.detay");

        // Donanim bir kez okunur - degismez ve okumasi pahalidir.
        _durum.Donanim ??= await ServisSaglayici.DonanimServisi.RaporAlAsync();

        // Karne her seferinde yeniden uretilir: icindeki metinler dile
        // baglidir ve onbelleklenirse dil degistikten sonra eski dilde
        // kalirdi. Uretmek yalnizca birkac karsilastirma, maliyeti yok.
        _durum.Uyumluluk = ServisSaglayici.UyumlulukServisi.Degerlendir(_durum.Donanim);

        var d = _durum.Donanim;

        MakineMetni.Text = $"{d.AnakartUretici} {d.AnakartModel}";

        OzellikMetni.Text = string.Join("  ·  ",
        [
            d.Islemci,
            $"{BaytBicimlendirici.BellekBicimle(d.RamBayt)} RAM",
            d.UefiMi ? "UEFI" : metinler.Al("karsilama.bios.eski"),
            d.TpmSurumu is not null ? $"TPM {d.TpmSurumu}" : metinler.Al("karsilama.tpm.yok")
        ]);

        SonucuGoster(_durum.Uyumluluk);

        MaddeListesi.ItemsSource = _durum.Uyumluluk.Maddeler
            .Select(m => new MaddeSatiri(m))
            .ToList();

        BeklemePaneli.Visibility = Visibility.Collapsed;
        RaporPaneli.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Uc ayri sonuc: hazir, ayarla cozulur, cozulmez. Ortadaki durum
    /// bu ekranin varlik sebebidir - "kurulamaz" demek yerine ne
    /// yapilacagini soyler.
    /// </summary>
    private void SonucuGoster(UyumlulukRaporu rapor)
    {
        var metinler = ServisSaglayici.Metinler;

        if (rapor.Uyumlu)
        {
            SonucKutusu.Severity = InfoBarSeverity.Success;
            SonucKutusu.Title = metinler.Al("uyumluluk.hazir");
            SonucKutusu.Message = metinler.Al("uyumluluk.hazir.aciklama");
            return;
        }

        if (rapor.BiosAyariylaCozulur)
        {
            SonucKutusu.Severity = InfoBarSeverity.Warning;
            SonucKutusu.Title = metinler.Al("uyumluluk.bios.cozulur");
            SonucKutusu.Message = metinler.Al("uyumluluk.bios.cozulur.aciklama");
            return;
        }

        SonucKutusu.Severity = InfoBarSeverity.Warning;
        SonucKutusu.Title = metinler.Al("uyumluluk.uyumsuz");
        SonucKutusu.Message = metinler.Al("uyumluluk.uyumsuz.aciklama");
    }
}
