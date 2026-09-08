using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UWin.Cekirdek.Arayuzler;
using UWin.Uygulama.Bicimlendirme;
using UWin.Uygulama.GorunumModelleri;
using UWin.Uygulama.Servisler;

namespace UWin.Uygulama.Adimlar;

/// <summary>
/// Yazma oncesi son kontrol. USB disi bir hedef secilmisse kullanici
/// diskin adini yazmadan devam edemez - bu, yanlis diske yazmayi
/// pratikte imkansiz kilar.
/// </summary>
public sealed partial class Adim5Onay : Page
{
    private SihirbazDurumu? _durum;

    public Adim5Onay() => InitializeComponent();

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _durum = (SihirbazDurumu)e.Parameter;

        if (_durum.SecilenDisk is not { } disk)
            return;

        var metinler = ServisSaglayici.Metinler;

        VeriUyarisi.Title = metinler.Al("onay.geri.alinamaz");
        AdOnayKutusu.PlaceholderText = metinler.Al("disk.ad.yer.tutucu");

        KurtarmaKutusu.Content = metinler.Al("kurtarma.ekle");
        KurtarmaAciklamasi.Text = metinler.Al("kurtarma.ekle.aciklama");
        KurtarmaKutusu.IsChecked = _durum.KurtarmaAraclariEklensin;

        OzetMetni.Text = metinler.Al(
            "disk.silinecek",
            $"{disk.Ad} ({BaytBicimlendirici.Bicimle(disk.BoyutBayt)})");

        // Kendi ISO'sunu secen kullanicida SecilenSurum bostur; o zaman
        // dosyadan okunan kimlik gosterilir. Yazmadan hemen onceki bu ekran
        // "ne yaziliyor" sorusuna her iki yolda da cevap vermeli.
        KaynakMetni.Text = KaynakOzeti(metinler);

        VeriUyarisi.Message = disk.KullanilanBayt > 0
            ? metinler.Al("disk.veri.uyari", BaytBicimlendirici.Bicimle(disk.KullanilanBayt))
            : metinler.Al("disk.bos.uyari");

        if (_durum.AdOnayiGerekiyorMu)
        {
            AdOnayPaneli.Visibility = Visibility.Visible;
            AdOnayAciklamasi.Text = metinler.Al("disk.ad.onay", disk.Ad);
            AdOnayKutusu.Text = _durum.AdOnayi ?? string.Empty;
        }
    }

    /// <summary>Yazilacak kaynagin tek satirlik ozeti.</summary>
    private string KaynakOzeti(IMetinSaglayici metinler)
    {
        if (_durum?.SecilenSurum is { } surum)
        {
            return _durum.IsoDogrulandi
                ? $"{surum.TamAd}  ·  {metinler.Al("onay.orijinal")}"
                : surum.TamAd;
        }

        if (_durum?.IsoKimligi is { WindowsMu: true } kimlik)
        {
            var ad = kimlik.Ad ?? metinler.Al("kaynak.windows.surumsuz");
            var dosya = Path.GetFileName(_durum.IsoYolu ?? string.Empty);

            return string.IsNullOrEmpty(dosya) ? ad : $"{ad}  ·  {dosya}";
        }

        return Path.GetFileName(_durum?.IsoYolu ?? string.Empty);
    }

    private void KurtarmaDegisti(object gonderen, RoutedEventArgs e)
    {
        if (_durum is not null)
            _durum.KurtarmaAraclariEklensin = KurtarmaKutusu.IsChecked ?? false;
    }

    private void AdOnayiDegisti(object gonderen, TextChangedEventArgs e)
    {
        // Ana penceredeki Devam dugmesi bu degere bakarak kendini acar.
        if (_durum is not null)
            _durum.AdOnayi = AdOnayKutusu.Text;
    }
}
