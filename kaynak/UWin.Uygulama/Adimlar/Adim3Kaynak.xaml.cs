using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UWin.Cekirdek.Modeller;
using UWin.Uygulama.Bicimlendirme;
using UWin.Uygulama.GorunumModelleri;
using UWin.Uygulama.Servisler;
using WinRT.Interop;

namespace UWin.Uygulama.Adimlar;

/// <summary>
/// ISO kaynagi: Microsoft'tan indirme veya kullanicinin kendi dosyasi.
///
/// Kullanicinin dosyasi ozet (SHA-256) ile degil, icine bakilarak taninir.
/// Ozet yontemi hem yanlisti - Microsoft ISO'lari her istekte yeniden
/// paketledigi icin sabit bir ozet yok, dolayisiyla gecerli dosyalar bile
/// "tanimadik" diye isaretleniyordu - hem de 8 GB'lik dosyayi bastan sona
/// okudugu icin dakikalar suruyordu.
///
/// Dosya yalnizca gozat penceresinden gelir. Surukle-birak denendi ve
/// kaldirildi: program diske yazabilmek icin yonetici yetkisiyle
/// calisir, Windows ise normal yetkili Explorer'dan yonetici
/// penceresine surukleme yapilmasini engeller (UIPI). Imlec yasak
/// isareti gosteriyor ve olay uygulamaya hic ulasmiyordu; calismayan
/// bir birakma alani birakmaktansa tek bir dugme dogru.
/// </summary>
public sealed partial class Adim3Kaynak : Page
{
    private SihirbazDurumu? _durum;
    private CancellationTokenSource? _iptalKaynagi;

    public Adim3Kaynak() => InitializeComponent();

    /// <summary>Indirme listesinde gosterilen satir.</summary>
    public sealed record SurumSatiri(WindowsSurumu Surum)
    {
        public string TamAd => Surum.TamAd;

        public string BoyutMetni => ServisSaglayici.Metinler.Al(
            "surum.boyut", BaytBicimlendirici.Bicimle(Surum.TahminiBoyutBayt));
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        _durum = (SihirbazDurumu)e.Parameter;

        var metinler = ServisSaglayici.Metinler;

        KaynakSecimi.ItemsSource = new[]
        {
            metinler.Al("kaynak.indir"),
            metinler.Al("kaynak.kendi")
        };

        IndirDugmesi.Content = metinler.Al("dugme.indir");
        IptalDugmesi.Content = metinler.Al("dugme.iptal");
        GozatDugmesi.Content = metinler.Al("dugme.gozat");

        KaynakSecimi.SelectedIndex = 0;

        await SurumleriYukleAsync();
    }

    /// <summary>Indirilebilir surumleri listeler ve makineye uygun olani secer.</summary>
    private async Task SurumleriYukleAsync()
    {
        if (_durum is null)
            return;

        var surumler = await ServisSaglayici.SurumServisi.SurumleriGetirAsync();

        // Makinenin dili ve mimarisi one alinir; gerisi asagida kalir.
        var siralanmis = surumler
            .OrderByDescending(s => s.Dil == "tr-TR")
            .ThenByDescending(s => s.Mimari == "x64")
            .ThenByDescending(s => s.GorunenAd)
            .Select(s => new SurumSatiri(s))
            .ToList();

        SurumListesi.ItemsSource = siralanmis;

        HangiWindowsMetni.Text = ServisSaglayici.Metinler.Al("kaynak.hangi.windows");

        var win11Uyumlu = _durum.Donanim?.Windows11Uyumlu ?? true;

        OneriMetni.Text = ServisSaglayici.Metinler.Al(
            win11Uyumlu ? "surum.oneri.uyumlu" : "surum.oneri.uyumsuz");

        // Uyumluysa Windows 11, degilse Windows 10 onceden secili gelir.
        var varsayilan = siralanmis.FirstOrDefault(
            s => s.Surum.GorunenAd.Contains(win11Uyumlu ? "11" : "10", StringComparison.Ordinal));

        SurumListesi.SelectedItem = varsayilan ?? siralanmis.FirstOrDefault();
    }

    private void SurumSecildi(object gonderen, SelectionChangedEventArgs e)
    {
        if (_durum is not null && SurumListesi.SelectedItem is SurumSatiri satir)
            _durum.SecilenSurum = satir.Surum;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        // Sayfadan cikilirsa suren indirme birakilmaz, iptal edilir.
        _iptalKaynagi?.Cancel();
        _iptalKaynagi?.Dispose();
        _iptalKaynagi = null;
    }

    private void KaynakDegisti(object gonderen, SelectionChangedEventArgs e)
    {
        var indirmeSecili = KaynakSecimi.SelectedIndex == 0;

        IndirmePaneli.Visibility = indirmeSecili ? Visibility.Visible : Visibility.Collapsed;
        DosyaPaneli.Visibility = indirmeSecili ? Visibility.Collapsed : Visibility.Visible;
        DurumKutusu.IsOpen = false;

        // Kendi dosyasini secen kullaniciya surum sorulmaz; bu bayrak
        // sihirbazin surum adimini atlamasini saglar.
        if (_durum is not null)
            _durum.KendiIsosu = !indirmeSecili;
    }

    private async void GozatTiklandi(object gonderen, RoutedEventArgs e)
    {
        var metinler = ServisSaglayici.Metinler;

        try
        {
            var pencereTanitici = App.AnaPencereOrnegi is { } pencere
                ? WindowNative.GetWindowHandle(pencere)
                : nint.Zero;

            var yol = DosyaSecici.Sec(
                pencereTanitici, metinler.Al("dugme.gozat"), "Windows ISO", ".iso");

            if (yol is null)
                return;

            await IsoyuKabulEtAsync(yol);
        }
        catch (Exception hata) when (hata is IOException or UnauthorizedAccessException)
        {
            // async void icinde yakalanmayan istisna sureci oldurur;
            // dosya okunamadiginda kullaniciya anlasilir bir mesaj gosterilir.
            Bildir(InfoBarSeverity.Error,
                metinler.Al("kaynak.dosya.okunamadi"),
                metinler.Al("kaynak.dosya.okunamadi.aciklama"));
        }
    }

    /// <summary>
    /// Gozat ve surukle-birak yollarinin ortak sonu. Dosya taninir,
    /// kabul edilirse durum guncellenir; Windows degilse secim
    /// alinmaz ve Devam dugmesi kapali kalir.
    /// </summary>
    private async Task IsoyuKabulEtAsync(string yol)
    {
        if (_durum is null)
            return;

        var metinler = ServisSaglayici.Metinler;

        SecilenDosyaMetni.Text = yol;
        SecilenDosyaMetni.Visibility = Visibility.Visible;

        GozatDugmesi.IsEnabled = false;
        Bildir(InfoBarSeverity.Informational, metinler.Al("kaynak.kontrol"), null);

        try
        {
            // Dosyanin icine bakmak buyuk bir ISO'da bile saniyenin altinda
            // surer; yine de arayuz kilitlenmesin diye ayri is parcaciginda.
            var kimlik = await Task.Run(() => ServisSaglayici.IsoTanimaServisi.Tani(yol));

            _durum.IsoKimligi = kimlik;
            _durum.IsoDogrulandi = kimlik.WindowsMu;

            // Windows olmayan bir dosya ile devam etmenin anlami yok:
            // yazma islemi kesin basarisiz olur. Bu yuzden secim kabul
            // edilmez ve ileri dugmesi kapali kalir.
            if (!kimlik.WindowsMu)
            {
                _durum.IsoYolu = null;
                IsoHatasiniGoster(kimlik.Durum);
                return;
            }

            _durum.IsoYolu = yol;
            TaninanIsoyuGoster(kimlik);
        }
        finally
        {
            GozatDugmesi.IsEnabled = true;
        }
    }

    private async void IndirTiklandi(object gonderen, RoutedEventArgs e)
    {
        if (_durum?.SecilenSurum is not { } surum)
            return;

        IndirmeyiBaslat();

        var metinler = ServisSaglayici.Metinler;
        var iptal = _iptalKaynagi!.Token;

        try
        {
            IndirmeDurumu.Text = metinler.Al("kaynak.baglaniyor");

            var cozum = await ServisSaglayici.SurumServisi.BaglantiCozAsync(
                surum.Kimlik, surum.Dil, surum.Mimari, iptal);

            // Uc nokta resmi bir API degil. Basarisizsa kullaniciya NEDEN
            // basarisiz oldugu soylenir; sessizce baska bir sekmeye atlamak
            // kullaniciyi neye ugradigini anlamadan birakir.
            if (!cozum.Basarili || cozum.Surum?.IndirmeBaglantisi is not { } baglanti)
            {
                IndirmeyiBitir();
                CozumHatasiniGoster(cozum.Durum);
                return;
            }

            var cozulmus = cozum.Surum;

            var hedef = Path.Combine(
                Path.GetTempPath(), $"UWin-{surum.Kimlik}-{surum.Dil}-{surum.Mimari}.iso");

            var ilerleme = new Progress<IndirmeIlerlemesi>(r =>
            {
                IndirmeCubugu.IsIndeterminate = false;
                IndirmeCubugu.Value = r.Yuzde;

                var kalan = r.KalanSure is { } sure && sure.TotalSeconds > 1
                    ? "  ·  " + metinler.Al("kaynak.kalan", SureMetni(sure))
                    : string.Empty;

                IndirmeDurumu.Text =
                    $"{BaytBicimlendirici.Bicimle(r.IndirilenBayt)} / "
                    + $"{BaytBicimlendirici.Bicimle(r.ToplamBayt)}{kalan}";
            });

            var sonuc = await ServisSaglayici.IndirmeServisi.IndirAsync(
                baglanti, hedef, cozulmus.Sha256, ilerleme, iptal);

            IndirmeyiBitir();

            if (sonuc is { Basarili: true, DosyaYolu: { } yol })
            {
                _durum.IsoYolu = yol;
                _durum.IsoDogrulandi = cozulmus.Sha256 is not null;

                // Indirilen dosyanin da icine bakilir: gereken USB boyutu
                // ancak icerik bilinince dogru hesaplanabilir.
                _durum.IsoKimligi = ServisSaglayici.IsoTanimaServisi.Tani(yol);

                Bildir(InfoBarSeverity.Success, metinler.Al("kaynak.dogrulandi"), null);
            }
            else if (sonuc.Hata is { } hata)
            {
                Bildir(InfoBarSeverity.Error, hata.NeOldu, $"{hata.Neden} {hata.NeYapmali}");
            }
        }
        catch (OperationCanceledException)
        {
            IndirmeyiBitir();
            Bildir(InfoBarSeverity.Informational,
                metinler.Al("kaynak.iptal.edildi"),
                metinler.Al("kaynak.iptal.aciklama"));
        }
    }

    /// <summary>Cozumleme neden basarisiz oldugunu kullaniciya acikca soyler.</summary>
    private void CozumHatasiniGoster(BaglantiDurumu durum)
    {
        var metinler = ServisSaglayici.Metinler;

        var (baslik, aciklama) = durum switch
        {
            BaglantiDurumu.AdresEngellendi => (
                metinler.Al("kaynak.engellendi"),
                metinler.Al("kaynak.engellendi.aciklama")),

            BaglantiDurumu.Reddedildi => (
                metinler.Al("kaynak.indirilemedi"),
                metinler.Al("kaynak.indirilemedi.aciklama")),

            _ => (
                metinler.Al("kaynak.erisilemedi"),
                metinler.Al("kaynak.erisilemedi.aciklama"))
        };

        Bildir(InfoBarSeverity.Warning, baslik, aciklama);
    }

    private void IptalTiklandi(object gonderen, RoutedEventArgs e)
    {
        IptalDugmesi.IsEnabled = false;
        IndirmeDurumu.Text = ServisSaglayici.Metinler.Al("kaynak.iptal.ediliyor");
        _iptalKaynagi?.Cancel();
    }

    private void IndirmeyiBaslat()
    {
        _iptalKaynagi?.Dispose();
        _iptalKaynagi = new CancellationTokenSource();

        IndirDugmesi.IsEnabled = false;
        IptalDugmesi.IsEnabled = true;
        IptalDugmesi.Visibility = Visibility.Visible;

        IlerlemePaneli.Visibility = Visibility.Visible;
        IndirmeCubugu.IsIndeterminate = true;
        IndirmeCubugu.Value = 0;
        DurumKutusu.IsOpen = false;
    }

    private void IndirmeyiBitir()
    {
        IndirDugmesi.IsEnabled = true;
        IptalDugmesi.Visibility = Visibility.Collapsed;
        IndirmeCubugu.IsIndeterminate = false;

        _iptalKaynagi?.Dispose();
        _iptalKaynagi = null;
    }

    private static string SureMetni(TimeSpan sure) => sure.TotalMinutes < 1
        ? ServisSaglayici.Metinler.Al("kaynak.sure.saniye", $"{sure.TotalSeconds:0}")
        : ServisSaglayici.Metinler.Al("kaynak.sure.dakika", $"{sure.TotalMinutes:0}");

    /// <summary>Taninan ISO icin ne bulundugunu ve gereken USB boyutunu soyler.</summary>
    private void TaninanIsoyuGoster(IsoKimligi kimlik)
    {
        var metinler = ServisSaglayici.Metinler;
        var gereken = BaytBicimlendirici.Bicimle(kimlik.GerekenUsbBoyutuBayt);

        if (kimlik.Durum == IsoTanimaDurumu.WindowsAmaSurumBilinmiyor || kimlik.Ad is null)
        {
            Bildir(InfoBarSeverity.Success,
                metinler.Al("kaynak.windows.surumsuz"),
                metinler.Al("kaynak.windows.surumsuz.aciklama")
                + " " + metinler.Al("kaynak.gereken.usb", gereken));
            return;
        }

        // Windows 7 ve 8.1 hala yazilabilir, ama kullanici ne sectigini bilmeli.
        var eski = kimlik.Ad is "Windows 7" or "Windows 8.1";

        var aciklama = eski
            ? metinler.Al("kaynak.eski.surum", kimlik.Ad)
            : metinler.Al("kaynak.taninan.aciklama");

        Bildir(eski ? InfoBarSeverity.Warning : InfoBarSeverity.Success,
            metinler.Al("kaynak.taninan", kimlik.Ad),
            aciklama + " " + metinler.Al("kaynak.gereken.usb", gereken));
    }

    /// <summary>Windows olmayan ya da acilamayan dosya icin nedenini soyler.</summary>
    private void IsoHatasiniGoster(IsoTanimaDurumu durum)
    {
        var metinler = ServisSaglayici.Metinler;

        var (baslik, aciklama) = durum == IsoTanimaDurumu.Okunamadi
            ? (metinler.Al("kaynak.acilamadi"), metinler.Al("kaynak.acilamadi.aciklama"))
            : (metinler.Al("kaynak.windows.degil"), metinler.Al("kaynak.windows.degil.aciklama"));

        Bildir(InfoBarSeverity.Error, baslik, aciklama);
    }

    private void Bildir(InfoBarSeverity seviye, string baslik, string? mesaj)
    {
        DurumKutusu.Severity = seviye;
        DurumKutusu.Title = baslik;
        DurumKutusu.Message = mesaj ?? string.Empty;
        DurumKutusu.IsOpen = true;
    }
}
