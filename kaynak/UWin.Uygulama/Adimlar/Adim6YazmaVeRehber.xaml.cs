using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;
using UWin.Uygulama.Bicimlendirme;
using UWin.Uygulama.GorunumModelleri;
using UWin.Uygulama.Servisler;

namespace UWin.Uygulama.Adimlar;

/// <summary>
/// USB'yi yazar, sonra ayni ekranda BIOS rehberine donusur.
/// Bu sayfa programin bitisi degil, kullanicinin yolculugunun ortasidir.
/// </summary>
public sealed partial class Adim6YazmaVeRehber : Page
{
    private const string RehberDosyaAdi = "UWin-Rehber.html";

    private SihirbazDurumu? _durum;

    public Adim6YazmaVeRehber() => InitializeComponent();

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        _durum = (SihirbazDurumu)e.Parameter;

        YazmaUyarisi.Text = ServisSaglayici.Metinler.Al("yazma.uyari");
        TeknikBolum.Header = ServisSaglayici.Metinler.Al("hata.teknik");

        if (_durum.SecilenDisk is not { } hedef || _durum.IsoYolu is not { } iso)
            return;

        // Hedef makine UEFI ise GPT, degilse MBR. Karar kullaniciya sorulmaz.
        var tip = (_durum.Donanim?.UefiMi ?? true)
            ? BolumTablosuTipi.Gpt
            : BolumTablosuTipi.Mbr;

        var ilerleme = new Progress<YazmaIlerlemesi>(IlerlemeyiGoster);

        var sonuc = await ServisSaglayici.YazmaServisi.YazAsync(hedef, iso, tip, ilerleme);

        YazmaPaneli.Visibility = Visibility.Collapsed;

        if (sonuc.Basarili)
            await BasariyiGosterAsync(hedef, sonuc);
        else if (sonuc.Dogrulama is { Basarili: false, Tamamlanabildi: true })
            DogrulamaHatasiniGoster(sonuc);
        else if (sonuc.Hata is { } hata)
            HatayiGoster(hata);
    }

    /// <summary>
    /// Ilerlemeyi yuzde, bayt ve kalan sureyle birlikte gosterir.
    /// Kalan sure yalnizca olculebildiginde yazilir: yanlis bir tahmin,
    /// hic tahmin olmamasindan kotudur.
    /// </summary>
    private void IlerlemeyiGoster(YazmaIlerlemesi r)
    {
        var metinler = ServisSaglayici.Metinler;

        AdimAciklamasi.Text = r.Aciklama;
        YazmaCubugu.Value = r.ToplamYuzde;

        YuzdeMetni.Text = r.ToplamBayt > 0
            ? $"%{r.ToplamYuzde:0}  ·  {BaytBicimlendirici.Bicimle(r.YazilanBayt)} / "
              + BaytBicimlendirici.Bicimle(r.ToplamBayt)
            : $"%{r.ToplamYuzde:0}";

        KalanSureMetni.Text = r.KalanSure is { } sure && sure.TotalSeconds > 2
            ? metinler.Al("yazma.kalan", SureMetni(sure))
            : string.Empty;
    }

    private static string SureMetni(TimeSpan sure) => sure.TotalMinutes < 1
        ? ServisSaglayici.Metinler.Al("kaynak.sure.saniye", $"{sure.TotalSeconds:0}")
        : ServisSaglayici.Metinler.Al("kaynak.sure.dakika", $"{sure.TotalMinutes:0}");

    private void HatayiGoster(UWinHatasi hata)
    {
        HataKutusu.Title = hata.NeOldu;
        HataKutusu.Message = $"{hata.Neden}\n\n{hata.NeYapmali}";

        // Ham hata kodu yalnizca katlanabilir bolumde durur.
        if (hata.TeknikAyrinti is { } ayrinti)
        {
            TeknikMetin.Text = ayrinti;
        }
        else
        {
            TeknikBolum.Visibility = Visibility.Collapsed;
        }

        HataPaneli.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Geri okuma basarisizsa bunu ayri bir mesajla soyler. Genel yazma
    /// hatasi metni "tekrar dene" diyor; oysa burada dogru tavsiye
    /// "bu bellegi kullanma, baskasini dene" - ayni USB'yle tekrar
    /// denemek ayni sonucu verir.
    /// </summary>
    private void DogrulamaHatasiniGoster(YazmaSonucu sonuc)
    {
        var metinler = ServisSaglayici.Metinler;

        HataKutusu.Title = metinler.Al("yazma.dogrulama.hatasi");
        HataKutusu.Message = metinler.Al("yazma.dogrulama.aciklama");

        if (sonuc.Hata?.TeknikAyrinti is { } ayrinti)
            TeknikMetin.Text = ayrinti;
        else
            TeknikBolum.Visibility = Visibility.Collapsed;

        HataPaneli.Visibility = Visibility.Visible;
    }

    private async Task BasariyiGosterAsync(DiskBilgisi hedef, YazmaSonucu sonuc)
    {
        var metinler = ServisSaglayici.Metinler;
        var donanim = _durum?.Donanim;
        var rehber = ServisSaglayici.RehberServisi.RehberGetir(donanim?.AnakartUretici ?? "Genel");

        BasariKutusu.Title = metinler.Al("yazma.tamamlandi");
        BasariKutusu.Message = metinler.Al("yazma.sonuc", hedef.Ad);

        // Geri okuma yapildiysa sonucunu soyle: kullanici USB'nin yalnizca
        // yazildigini degil, kontrol de edildigini bilmeli.
        if (sonuc.Dogrulama is { Basarili: true })
        {
            DogrulamaKutusu.Title = metinler.Al("yazma.dogrulandi");
            DogrulamaKutusu.Visibility = Visibility.Visible;
        }

        // Kurtarma araclari: USB yazildiktan sonra eklenir, cunku surucu
        // harfi ancak bicimlendirme bitince olusur.
        if (_durum?.KurtarmaAraclariEklensin ?? false)
            await KurtarmaAraclariniYazAsync(hedef);

        GirisTusuMetni.Text = metinler.Al("rehber.bios.giris", rehber.GirisTusu);

        if (rehber.BootMenuTusu is { } bootTusu)
            BootMenuMetni.Text = metinler.Al("rehber.boot.menu", bootTusu);
        else
            BootMenuMetni.Visibility = Visibility.Collapsed;

        AmacTavsiyesiniGoster();

        AdimListesi.ItemsSource = rehber.Adimlar.OrderBy(a => a.Sira).ToList();

        if (rehber.SecureBootNotu is { } not)
        {
            SecureBootKutusu.Title = metinler.Al("rehber.secureboot");
            SecureBootKutusu.Message = not;
        }
        else
        {
            SecureBootKutusu.Visibility = Visibility.Collapsed;
        }

        RehberPaneli.Visibility = Visibility.Visible;

        // Rehber USB'ye de yazilir: kullanici bilgisayari kapattiginda
        // ekranda okuyacak bir sey kalmiyor, telefondan acabilsin.
        if (donanim is not null)
            RehberKaydiMetni.Text = await RehberiUsbyeYazAsync(hedef, rehber, donanim);
    }

    /// <summary>
    /// Kurulum amacina gore disk secme ekrani tavsiyesi. Kullanici bunu
    /// amac adiminda gormustu; USB hazir olduktan sonra tekrar gosterilir
    /// cunku uygulayacagi an simdi geldi.
    /// </summary>
    private void AmacTavsiyesiniGoster()
    {
        if (_durum is null || _durum.Amac == KurulumAmaci.Belirtilmemis)
            return;

        var tavsiye = ServisSaglayici.AmacServisi.TavsiyeGetir(_durum.Amac);

        AmacBasligi.Text = ServisSaglayici.Metinler.Al("amac.disk.ekrani");
        AmacMetni.Text = tavsiye.DiskEkraniTavsiyesi;
        AmacKutusu.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Onarim araclarini USB'ye yazar. Basarisiz olursa kurulum USB'si
    /// yine calisir; bu yuzden hata kullaniciyi durdurmaz, yalnizca
    /// bilgilendirir.
    /// </summary>
    private async Task KurtarmaAraclariniYazAsync(DiskBilgisi hedef)
    {
        var surucu = ServisSaglayici.DiskErisimi.SurucuYoluBul(hedef.DiskNumarasi);
        if (surucu is null)
            return;

        var metinler = ServisSaglayici.Metinler;
        var sonuc = await ServisSaglayici.KurtarmaServisi.UsbyeYazAsync(surucu);

        if (sonuc.Basarili)
        {
            KurtarmaKutusu.Title = metinler.Al("kurtarma.baslik");
            KurtarmaKutusu.Message = metinler.Al("kurtarma.yazildi", KurtarmaServisi.KlasorAdi)
                                     + "  " + metinler.Al("kurtarma.nasil.aciklama");
        }
        else
        {
            KurtarmaKutusu.Severity = InfoBarSeverity.Warning;
            KurtarmaKutusu.Title = metinler.Al("kurtarma.yazilamadi");
        }

        KurtarmaKutusu.Visibility = Visibility.Visible;
    }

    private static async Task<string> RehberiUsbyeYazAsync(
        DiskBilgisi hedef, BiosRehberi rehber, DonanimRaporu donanim)
    {
        var surucu = ServisSaglayici.DiskErisimi.SurucuYoluBul(hedef.DiskNumarasi);
        if (surucu is null)
            return string.Empty;

        try
        {
            var html = ServisSaglayici.RehberServisi.HtmlUret(rehber, donanim);
            await File.WriteAllTextAsync(Path.Combine(surucu, RehberDosyaAdi), html);

            return ServisSaglayici.Metinler.Al("rehber.kaydedildi");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Rehber yazilamazsa USB yine calisir; ekrandaki rehber yeterlidir.
            return string.Empty;
        }
    }
}
