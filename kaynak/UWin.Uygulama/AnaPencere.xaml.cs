using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;
using UWin.Cekirdek.Servisler;
using UWin.Uygulama.Adimlar;
using UWin.Uygulama.GorunumModelleri;
using UWin.Uygulama.Servisler;

namespace UWin.Uygulama;

public sealed partial class AnaPencere : Window
{
    private static readonly Dictionary<SihirbazAdimi, (Type Sayfa, string BaslikAnahtari)> Adimlar = new()
    {
        [SihirbazAdimi.Karsilama] = (typeof(Adim1Karsilama), "adim.karsilama.baslik"),
        [SihirbazAdimi.Amac] = (typeof(Adim2Amac), "adim.amac.baslik"),
        [SihirbazAdimi.Kaynak] = (typeof(Adim3Kaynak), "adim.kaynak.baslik"),
        [SihirbazAdimi.Disk] = (typeof(Adim4Disk), "adim.disk.baslik"),
        [SihirbazAdimi.Onay] = (typeof(Adim5Onay), "adim.onay.baslik"),
        [SihirbazAdimi.Yazma] = (typeof(Adim6YazmaVeRehber), "adim.yazma.baslik")
    };

    private readonly SihirbazDurumu _durum = new();

    public AnaPencere()
    {
        InitializeComponent();

        Title = "UWin";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(920, 760));
        BaslikCubugunuKur();


        _durum.PropertyChanged += (_, _) => ArayuzuGuncelle();

        // Dil degisince acik olan sayfa yeniden kurulur; sayfalar
        // metinlerini OnNavigatedTo icinde okudugu icin baska bir
        // yenileme yoluna gerek kalmiyor.
        ServisSaglayici.Metinler.DilDegisti += (_, _) => DiliUygula();

        MetinleriYukle();
        ArayuzuGuncelle();

        // Ilk acilista dil sorulur. Bunun icin XAML agacinin kurulmus
        // olmasi gerekir: ContentDialog bir XamlRoot ister ve o, pencere
        // etkinlestiginde degil, kok eleman yuklendiginde hazir olur.
        KokIzgara.Loaded += IlkAcilis;
    }

    /// <summary>
    /// Ilk acilista bir kez dil sorar. Sonraki acilislarda kayitli
    /// secim kullanilir ve bu ekran hic gorunmez.
    /// </summary>
    private async void IlkAcilis(object gonderen, RoutedEventArgs e)
    {
        KokIzgara.Loaded -= IlkAcilis;

        if (!ServisSaglayici.AyarServisi.Oku().DilSorulmali)
            return;

        // Sistemin dili onceden isaretlenir: Turkce bir Windows'ta
        // kullaniciyi bos bir secimle karsilamanin anlami yok.
        var onerilen = AyarServisi.OnerilenDil(
            System.Globalization.CultureInfo.CurrentUICulture.Name);

        ServisSaglayici.Metinler.DilDegistir(onerilen);

        await DiliSorAsync(onerilen);
    }

    /// <summary>Sabit arayuz metinlerini aktif dilde yeniden yazar.</summary>
    private void MetinleriYukle()
    {
        var metinler = ServisSaglayici.Metinler;

        GeriDugmesi.Content = metinler.Al("dugme.geri");
        IleriDugmesi.Content = metinler.Al("dugme.devam");
        HakkindaEtiketi.Text = metinler.Al("dugme.hakkinda");
        SiradaEtiketi.Text = metinler.Al("dugme.sirada.ne.var");
        AraclarEtiketi.Text = metinler.Al("dugme.araclar");

        // Dugmede aktif dilin adi yazar, degisecek dilin degil:
        // "Turkce" yazan bir dugmeye basinca Turkce'den cikmak
        // beklenmedik olurdu; dugme bir durum gostergesidir.
        DilEtiketi.Text = metinler.Al($"dil.{metinler.AktifDil}");
        ToolTipService.SetToolTip(DilDugmesi, metinler.Al("dil.degistir"));
    }

    /// <summary>
    /// Dil degisiminden sonra arayuzu bastan kurar. Acik olan sayfa
    /// yeniden yuklenir - sayfalar metinlerini yalnizca acilirken
    /// okudugu icin baska turlu eski dilde kalirlardi.
    /// </summary>
    private void DiliUygula()
    {
        MetinleriYukle();

        // Ayni sayfaya yeniden gitmek icin once kaydi temizlemek
        // gerekir; aksi halde Navigate ayni tipe gecisi yok sayar.
        var sayfa = AdimCercevesi.SourcePageType;

        if (sayfa is not null)
        {
            AdimCercevesi.Content = null;
            AdimCercevesi.Navigate(sayfa, _durum, new SuppressNavigationTransitionInfo());
        }

        ArayuzuGuncelle();
    }

    /// <summary>
    /// Dil secim penceresini acar ve sonucu kalici olarak kaydeder.
    ///
    /// Pencere acilamazsa program yine calisir. Dil secimi bir
    /// kolayliktir, programin calismasinin sarti degil - burada
    /// dusen bir istisna butun uygulamayi kapatirdi.
    /// </summary>
    private async Task DiliSorAsync(string aktifDil)
    {
        if (KokIzgara.XamlRoot is null)
            return;

        try
        {
            var pencere = new DilPenceresi(aktifDil) { XamlRoot = KokIzgara.XamlRoot };

            await pencere.ShowAsync();

            ServisSaglayici.AyarServisi.DilKaydet(pencere.SecilenDil);
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException)
        {
            // Ayni anda baska bir ContentDialog acikken ikincisi
            // acilamaz; kullanici dili sag ustten degistirebilir.
        }
    }

    /// <summary>
    /// Sistem baslik cubugunu kaldirip yerine uygulamanin kendi cubugunu koyar.
    /// Boylece pencerenin ust seridi programin zemin rengiyle ayni olur; kapat
    /// ve kucult dugmeleri sistemin kendi dugmeleri olarak kalmaya devam eder.
    /// </summary>
    private void BaslikCubugunuKur()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(BaslikCubugu);

        // Dugmelerin arkasi saydam olmazsa ust kosede kutu gibi durur.
        var cubuk = AppWindow.TitleBar;
        cubuk.ButtonBackgroundColor = Colors.Transparent;
        cubuk.ButtonInactiveBackgroundColor = Colors.Transparent;

        MarkaSimgesi.Source = Simgeler.BaslikSimgesi();

        if (Simgeler.PencereIkonuYolu() is { } ikon)
            AppWindow.SetIcon(ikon);
    }

    private void ArayuzuGuncelle()
    {
        var (sayfa, baslikAnahtari) = Adimlar[_durum.AktifAdim];

        BaslikMetni.Text = ServisSaglayici.Metinler.Al(baslikAnahtari);
        AdimSayaci.Text = ServisSaglayici.Metinler.Al(
            "adim.sayaci", _durum.AdimSirasi, _durum.ToplamAdimSayisi);

        AdimGostergesi.Maximum = _durum.ToplamAdimSayisi - 1;
        AdimGostergesi.Value = _durum.AdimSirasi - 1;

        // Yazma basladiktan sonra geri donmek diski yarim birakacagi icin engellenir.
        GeriDugmesi.IsEnabled = _durum.AktifAdim > SihirbazAdimi.Karsilama
                                && _durum.AktifAdim != SihirbazAdimi.Yazma;

        IleriDugmesi.IsEnabled = _durum.IlerleyebilirMi();
        IleriDugmesi.Visibility = _durum.AktifAdim == SihirbazAdimi.Yazma
            ? Visibility.Collapsed
            : Visibility.Visible;

        IleriDugmesi.Content = ServisSaglayici.Metinler.Al(
            _durum.AktifAdim == SihirbazAdimi.Onay ? "dugme.basla" : "dugme.devam");

        if (AdimCercevesi.SourcePageType != sayfa)
            AdimCercevesi.Navigate(sayfa, _durum, new DrillInNavigationTransitionInfo());
    }

    private async void HakkindaTiklandi(object gonderen, RoutedEventArgs e)
    {
        var pencere = new HakkindaPenceresi { XamlRoot = KokIzgara.XamlRoot };
        await pencere.ShowAsync();
    }

    private async void SiradaTiklandi(object gonderen, RoutedEventArgs e)
    {
        // Yazma tamamlandiysa bu bir onizleme degil, gercek yonergedir.
        var usbHazir = _durum.AktifAdim == SihirbazAdimi.Yazma;

        // Amac ve kurtarma araclari da tasinir: kullanicinin kurulumda
        // yapacagi en kritik secim amaca gore degisiyor ve USB'deki
        // onarim araclarini nerede bulacagini bilmesi gerekiyor.
        var pencere = new SiradaNeVarPenceresi(
            _durum.Donanim,
            usbHazir,
            _durum.Amac,
            kurtarmaEklendi: usbHazir && _durum.KurtarmaAraclariEklensin)
        {
            XamlRoot = KokIzgara.XamlRoot
        };

        await pencere.ShowAsync();
    }

    /// <summary>
    /// Araclar penceresi. Sihirbazin herhangi bir adiminda acilabilir:
    /// kullanici bu isler icin programi bastan baslatmak zorunda kalmasin.
    /// </summary>
    private async void AraclarTiklandi(object gonderen, RoutedEventArgs e)
    {
        var pencere = new AraclarPenceresi { XamlRoot = KokIzgara.XamlRoot };
        await pencere.ShowAsync();
    }

    private async void DilTiklandi(object gonderen, RoutedEventArgs e)
        => await DiliSorAsync(ServisSaglayici.Metinler.AktifDil);

    private void IleriTiklandi(object gonderen, RoutedEventArgs e) => _durum.Ilerle();

    private void GeriTiklandi(object gonderen, RoutedEventArgs e) => _durum.Geri();
}
