using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UWin.Cekirdek.Modeller;
using UWin.Uygulama.Servisler;

namespace UWin.Uygulama;

/// <summary>
/// USB hazir olduktan sonra yapilacaklarin tamami.
///
/// Sihirbazin herhangi bir adiminda acilabilir: kullanici daha USB
/// hazirlamadan "sonrasinda beni ne bekliyor" sorusunun cevabini gorebilsin.
/// Donanim taninmissa adimlar kullanicinin anakartina gore gelir.
/// </summary>
public sealed partial class SiradaNeVarPenceresi : ContentDialog
{
    public SiradaNeVarPenceresi(
        DonanimRaporu? donanim,
        bool usbHazir,
        KurulumAmaci amac = KurulumAmaci.Belirtilmemis,
        bool kurtarmaEklendi = false)
    {
        InitializeComponent();

        var metinler = ServisSaglayici.Metinler;
        var yolculuk = ServisSaglayici.YolculukServisi.YolculukGetir(donanim);

        Title = metinler.Al("sirada.baslik");
        CloseButtonText = metinler.Al("dugme.kapat");

        AciklamaMetni.Text = metinler.Al("sirada.aciklama");

        // USB henuz yazilmadiysa bunu acikca soyle - kullanici ekrandaki
        // adimlari simdi uygulamaya kalkismasin. Baslik once, IsOpen sonra:
        // ters sirada kutu sifir yukseklikle acilip gorunmez oluyor.
        if (usbHazir)
        {
            OnizlemeKutusu.Visibility = Visibility.Collapsed;
        }
        else
        {
            OnizlemeKutusu.Title = metinler.Al("sirada.onizleme");
            OnizlemeKutusu.IsOpen = true;
        }

        MakineMetni.Text = yolculuk.Makine is { } makine
            ? metinler.Al("sirada.makine", makine)
            : metinler.Al("sirada.makine.bilinmiyor");

        GirisTusuMetni.Text = metinler.Al("rehber.bios.giris", yolculuk.GirisTusu);

        if (yolculuk.BootMenuTusu is { } bootTusu)
            BootMenuMetni.Text = metinler.Al("rehber.boot.menu", bootTusu);
        else
            BootMenuMetni.Visibility = Visibility.Collapsed;

        AmaciGoster(amac);

        BolumListesi.ItemsSource = yolculuk.Bolumler;

        if (yolculuk.SecureBootNotu is { } not)
        {
            SecureBootKutusu.Title = metinler.Al("rehber.secureboot");
            SecureBootKutusu.Message = not;
        }
        else
        {
            SecureBootKutusu.Visibility = Visibility.Collapsed;
        }

        if (kurtarmaEklendi)
            KurtarmaAraclariniGoster();
    }

    /// <summary>Kurtarma listesinde gosterilen tek arac.</summary>
    public sealed record AracSatiri(KurtarmaAraci Arac)
    {
        public string Ad => Arac.Ad;

        public string NeZaman => Arac.NeZamanKullanilir;

        public string Dosya => ServisSaglayici.Metinler.Al("kurtarma.dosya", $"{Arac.Kimlik}.bat");

        public string Uyari => Arac.DiskeYazar
            ? ServisSaglayici.Metinler.Al("kurtarma.diske.yazar")
            : string.Empty;

        public Visibility UyariGorunurlugu =>
            Arac.DiskeYazar ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Kurulum amacina gore disk secme ekrani tavsiyesi.
    ///
    /// Genel adimlardan once durur cunku kullanicinin kurulumda
    /// verecegi en kritik ve geri alinamaz karar budur - ve amaca
    /// gore tam tersine doner.
    /// </summary>
    private void AmaciGoster(KurulumAmaci amac)
    {
        if (amac == KurulumAmaci.Belirtilmemis)
            return;

        var tavsiye = ServisSaglayici.AmacServisi.TavsiyeGetir(amac);

        AmacBasligi.Text = ServisSaglayici.Metinler.Al("amac.disk.ekrani");
        AmacMetni.Text = tavsiye.DiskEkraniTavsiyesi;
        AmacAdimListesi.ItemsSource = tavsiye.EkAdimlar;

        AmacKutusu.Visibility = Visibility.Visible;
    }

    private void KurtarmaAraclariniGoster()
    {
        var metinler = ServisSaglayici.Metinler;

        KurtarmaBasligi.Text = metinler.Al("kurtarma.araclar");
        KurtarmaNasil.Text = metinler.Al("kurtarma.nasil.aciklama");

        KurtarmaListesi.ItemsSource = ServisSaglayici.KurtarmaServisi.AraclariGetir()
            .Select(a => new AracSatiri(a))
            .ToList();

        KurtarmaPaneli.Visibility = Visibility.Visible;
    }
}
