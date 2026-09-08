using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UWin.Cekirdek.Modeller;
using UWin.Uygulama.Bicimlendirme;
using UWin.Uygulama.GorunumModelleri;
using UWin.Uygulama.Servisler;

namespace UWin.Uygulama.Adimlar;

/// <summary>
/// Kurulum amaci ve bu bilgisayarda silinecek dosyalar.
///
/// Iki soru ayni ekranda durur cunku ikisi de tek bir seye hizmet eder:
/// kullanicinin kurulumun ortasinda degil, hala geri donebilecegi bu
/// anda ne kaybedecegini bilmesi.
///
/// Amac sorusu kozmetik degildir. "Disk secme ekraninda Sil'e basma"
/// ile "mutlaka Sil'e bas" ayni anda dogru olamaz; hangisinin dogru
/// oldugu kullanicinin neden kurdugua baglidir. Virusten kurtulmak
/// icin kuran birine dosyalarini koruma tavsiyesi vermek, virusu de
/// korumak demektir.
/// </summary>
public sealed partial class Adim2Amac : Page
{
    private SihirbazDurumu? _durum;
    private CancellationTokenSource? _iptalKaynagi;

    public Adim2Amac() => InitializeComponent();

    /// <summary>Listede gosterilen amac secenegi.</summary>
    public sealed record AmacSatiri(KurulumAmaci Amac, string Baslik, string Altyazi);

    /// <summary>Silinecek klasorun tek satirlik ozeti.</summary>
    public sealed record KlasorSatiri(string Ad, string Ayrinti);

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        _durum = (SihirbazDurumu)e.Parameter;

        var metinler = ServisSaglayici.Metinler;

        AciklamaMetni.Text = metinler.Al("amac.aciklama");
        TaramaMetni.Text = metinler.Al("veri.taraniyor");
        OnayKutusu.Content = metinler.Al("veri.anladim");
        BaskaBilgisayarKutusu.Content = metinler.Al("veri.baska.bilgisayar");

        AmacListesi.ItemsSource = new List<AmacSatiri>
        {
            new(KurulumAmaci.Virus, metinler.Al("amac.virus"), metinler.Al("amac.virus.alt")),
            new(KurulumAmaci.Yavaslik, metinler.Al("amac.yavaslik"), metinler.Al("amac.yavaslik.alt")),
            new(KurulumAmaci.YeniDisk, metinler.Al("amac.yenidisk"), metinler.Al("amac.yenidisk.alt")),
            new(KurulumAmaci.SurumYukseltme, metinler.Al("amac.yukseltme"), metinler.Al("amac.yukseltme.alt"))
        };

        // Geri donuldugunde onceki secim yerinde durur.
        if (_durum.Amac != KurulumAmaci.Belirtilmemis)
        {
            AmacListesi.SelectedItem = ((List<AmacSatiri>)AmacListesi.ItemsSource)
                .FirstOrDefault(s => s.Amac == _durum.Amac);
        }

        BaskaBilgisayarKutusu.IsChecked = !_durum.BuBilgisayaraKurulacak;
        OnayKutusu.IsChecked = _durum.VeriUyarisiOnaylandi;

        await VeriyiYukleAsync();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        // Sayfadan cikilirsa suren tarama birakilmaz, iptal edilir.
        _iptalKaynagi?.Cancel();
        _iptalKaynagi?.Dispose();
        _iptalKaynagi = null;
    }

    /// <summary>
    /// Kullanici klasorlerini sayar. Yalnizca bir kez yapilir: geri
    /// donuldugunde rapor zaten elde durur ve dosyalari tekrar taramak
    /// hem gereksiz hem yavastir.
    /// </summary>
    private async Task VeriyiYukleAsync()
    {
        if (_durum is null)
            return;

        if (_durum.Veri is not null)
        {
            VeriyiGoster();
            return;
        }

        TaramaPaneli.Visibility = Visibility.Visible;

        _iptalKaynagi?.Dispose();
        _iptalKaynagi = new CancellationTokenSource();

        try
        {
            _durum.Veri = await ServisSaglayici.VeriServisi.RaporAlAsync(
                iptal: _iptalKaynagi.Token);
        }
        catch (OperationCanceledException)
        {
            // Sayfadan cikildi; gosterilecek bir sey yok.
            return;
        }
        finally
        {
            TaramaPaneli.Visibility = Visibility.Collapsed;
        }

        VeriyiGoster();
    }

    private void VeriyiGoster()
    {
        if (_durum?.Veri is not { } veri)
            return;

        var metinler = ServisSaglayici.Metinler;

        // Baska bilgisayara kurulacaksa buradaki dosyalar tehlikede degil.
        if (!_durum.BuBilgisayaraKurulacak || !veri.VeriVar)
        {
            VeriPaneli.Visibility = Visibility.Collapsed;
            return;
        }

        VeriKutusu.Title = metinler.Al("veri.baslik");
        VeriKutusu.Message = metinler.Al("veri.aciklama")
                             + "  "
                             + metinler.Al(
                                 "veri.ozet",
                                 veri.DoluKlasorler.Count,
                                 veri.ToplamDosya.ToString("N0"),
                                 BaytBicimlendirici.Bicimle(veri.ToplamBayt));

        // Klasor adlari gosterim aninda yeniden cozulur. Tarama sonucu
        // onbellekte durur (dosya saymak yavastir) ama icindeki adlar
        // tarama anindaki dile aitti; dil degisince "Masaüstü" yazili
        // kalirdi. Yol degismedigi icin ad yoldan yeniden bulunabilir.
        var adlar = UWin.Cekirdek.Servisler.VeriServisi
            .VarsayilanKlasorler(metinler)
            .ToDictionary(k => k.Yol, k => k.Ad, StringComparer.OrdinalIgnoreCase);

        KlasorListesi.ItemsSource = veri.DoluKlasorler
            .OrderByDescending(k => k.BoyutBayt)
            .Select(k => new KlasorSatiri(
                adlar.GetValueOrDefault(k.Yol, k.Ad),
                metinler.Al(
                    "veri.klasor",
                    k.DosyaSayisi.ToString("N0"),
                    BaytBicimlendirici.Bicimle(k.BoyutBayt))))
            .ToList();

        VeriPaneli.Visibility = Visibility.Visible;
    }

    private void AmacSecildi(object gonderen, SelectionChangedEventArgs e)
    {
        if (_durum is null || AmacListesi.SelectedItem is not AmacSatiri satir)
            return;

        _durum.Amac = satir.Amac;
        TavsiyeyiGoster(satir.Amac);
    }

    /// <summary>
    /// Secilen amaca gore kurulumda ne yapmasi gerektigini soyler.
    /// Kullanici bunu simdi okur, USB hazir olduktan sonra da "Sirada
    /// ne var" ekranindan tekrar gorur.
    /// </summary>
    private void TavsiyeyiGoster(KurulumAmaci amac)
    {
        var tavsiye = ServisSaglayici.AmacServisi.TavsiyeGetir(amac);

        TavsiyeBasligi.Text = ServisSaglayici.Metinler.Al("amac.disk.ekrani");
        TavsiyeMetni.Text = tavsiye.DiskEkraniTavsiyesi;
        TavsiyeKutusu.Visibility = Visibility.Visible;
    }

    private void OnayDegisti(object gonderen, RoutedEventArgs e)
    {
        if (_durum is not null)
            _durum.VeriUyarisiOnaylandi = OnayKutusu.IsChecked ?? false;
    }

    private void BaskaBilgisayarDegisti(object gonderen, RoutedEventArgs e)
    {
        if (_durum is null)
            return;

        _durum.BuBilgisayaraKurulacak = !(BaskaBilgisayarKutusu.IsChecked ?? false);
        VeriyiGoster();
    }
}
