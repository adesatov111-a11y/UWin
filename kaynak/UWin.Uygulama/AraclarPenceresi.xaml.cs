using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using UWin.Cekirdek.Modeller;
using UWin.Uygulama.Bicimlendirme;
using UWin.Uygulama.Servisler;

namespace UWin.Uygulama;

/// <summary>
/// Sihirbaz disindaki islemler: USB'yi normale dondurme, saglik testi,
/// kurtarma araclarini sonradan ekleme ve donanim raporu.
///
/// Hepsi tek bir pencerede durur; secilen arac menunun yerini alir.
/// Dort ayri pencere acmak yerine bunu tercih etmemin sebebi, uc aracin
/// da ayni disk secme adimiyla basliyor olmasi - o adimi dort kez
/// yazmak hem kod tekrari hem de kullaniciya dort farkli gorunum demek.
/// </summary>
public sealed partial class AraclarPenceresi : ContentDialog
{
    private enum Arac { Menu, Kazanma, Saglik, Kurtarma, Rapor }

    private Arac _aktif = Arac.Menu;
    private DiskBilgisi? _secilen;
    private CancellationTokenSource? _iptalKaynagi;

    public AraclarPenceresi()
    {
        InitializeComponent();

        var metinler = ServisSaglayici.Metinler;

        Title = metinler.Al("araclar.baslik");
        CloseButtonText = metinler.Al("dugme.kapat");

        MenuAciklamasi.Text = metinler.Al("araclar.aciklama");
        GeriDugmesi.Content = metinler.Al("araclar.geri");
        DiskBasligi.Text = metinler.Al("kazanma.sec");
        BicimBasligi.Text = metinler.Al("kazanma.bicim");
        EtiketBasligi.Text = metinler.Al("kazanma.etiket");
        SilmeNotu.Text = metinler.Al("kazanma.silme.uyari");

        BicimSecimi.ItemsSource = new[]
        {
            metinler.Al("kazanma.bicim.exfat"),
            metinler.Al("kazanma.bicim.ntfs"),
            metinler.Al("kazanma.bicim.fat32")
        };

        AracListesi.ItemsSource = new List<AracSatiri>
        {
            new(nameof(Arac.Kazanma), "",
                metinler.Al("araclar.kazanma"), metinler.Al("araclar.kazanma.alt")),

            new(nameof(Arac.Saglik), "",
                metinler.Al("araclar.saglik"), metinler.Al("araclar.saglik.alt")),

            new(nameof(Arac.Kurtarma), "",
                metinler.Al("araclar.kurtarma"), metinler.Al("araclar.kurtarma.alt")),

            new(nameof(Arac.Rapor), "",
                metinler.Al("araclar.rapor"), metinler.Al("araclar.rapor.alt"))
        };
    }

    /// <summary>Menude gosterilen tek arac.</summary>
    public sealed record AracSatiri(string Kimlik, string Simge, string Baslik, string Altyazi);

    /// <summary>Disk listesinde gosterilen satir.</summary>
    public sealed record DiskSatiri(DiskBilgisi Disk)
    {
        public string Ad => Disk.Ad;

        public string Ayrinti
        {
            get
            {
                var boyut = BaytBicimlendirici.Bicimle(Disk.BoyutBayt);

                return Disk.KullanilanBayt > 0
                    ? ServisSaglayici.Metinler.Al(
                        "disk.dolu", boyut, BaytBicimlendirici.Bicimle(Disk.KullanilanBayt))
                    : ServisSaglayici.Metinler.Al("disk.bos", boyut);
            }
        }
    }

    /// <summary>Donanim raporundaki tek gereksinim.</summary>
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
    }

    private async void AracSecildi(object gonderen, RoutedEventArgs e)
    {
        if (gonderen is not Button { Tag: string kimlik } || !Enum.TryParse<Arac>(kimlik, out var arac))
            return;

        _aktif = arac;
        _secilen = null;

        MenuPaneli.Visibility = Visibility.Collapsed;
        AracPaneli.Visibility = Visibility.Visible;
        SonucKutusu.Visibility = Visibility.Collapsed;
        UyariKutusu.Visibility = Visibility.Collapsed;
        CalistirDugmesi.Visibility = Visibility.Collapsed;

        var metinler = ServisSaglayici.Metinler;

        switch (arac)
        {
            case Arac.Kazanma:
                Title = metinler.Al("kazanma.baslik");
                AracAciklamasi.Text = metinler.Al("kazanma.aciklama");
                CalistirDugmesi.Content = metinler.Al("kazanma.dugme");
                DiskBasligi.Text = metinler.Al("kazanma.sec");
                DiskleriGoster(kazanmaAyarlari: true);
                break;

            case Arac.Saglik:
                Title = metinler.Al("saglik.baslik");
                AracAciklamasi.Text = metinler.Al("saglik.aciklama");
                CalistirDugmesi.Content = metinler.Al("saglik.basla");
                DiskBasligi.Text = metinler.Al("saglik.sec");
                DiskleriGoster(kazanmaAyarlari: false);
                break;

            case Arac.Kurtarma:
                Title = metinler.Al("kurtarma.ekle.baslik");
                AracAciklamasi.Text = metinler.Al("kurtarma.ekle.aciklama");
                CalistirDugmesi.Content = metinler.Al("kurtarma.ekle.dugme");
                DiskBasligi.Text = metinler.Al("kurtarma.ekle.sec");
                DiskleriGoster(kazanmaAyarlari: false);
                break;

            case Arac.Rapor:
                Title = metinler.Al("araclar.rapor");
                AracAciklamasi.Text = metinler.Al("araclar.rapor.alt");
                DiskPaneli.Visibility = Visibility.Collapsed;
                KazanmaAyarlari.Visibility = Visibility.Collapsed;
                await RaporuGosterAsync();
                break;
        }
    }

    /// <summary>
    /// Yalnizca USB bellekler listelenir. Gelismis mod bilerek yok:
    /// bu araclar bir tamir takimi degil, "USB'me bir sey yapmak
    /// istiyorum" dugmeleridir ve dahili disk gostermeleri gereksiz.
    /// </summary>
    private void DiskleriGoster(bool kazanmaAyarlari)
    {
        RaporPaneli.Visibility = Visibility.Collapsed;
        DiskPaneli.Visibility = Visibility.Visible;
        KazanmaAyarlari.Visibility = kazanmaAyarlari ? Visibility.Visible : Visibility.Collapsed;

        var diskler = ServisSaglayici.DiskServisi.DiskleriListele(gelismisMod: false);

        DiskListesi.ItemsSource = diskler.Select(d => new DiskSatiri(d)).ToList();
        DiskListesi.SelectedItem = null;

        var bosMu = diskler.Count == 0;

        BosListeKutusu.Message = ServisSaglayici.Metinler.Al("kazanma.bulunamadi");
        BosListeKutusu.Visibility = bosMu ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task RaporuGosterAsync()
    {
        var metinler = ServisSaglayici.Metinler;
        var donanim = await ServisSaglayici.DonanimServisi.RaporAlAsync();
        var uyumluluk = ServisSaglayici.UyumlulukServisi.Degerlendir(donanim);

        MakineMetni.Text = $"{donanim.AnakartUretici} {donanim.AnakartModel}";

        OzellikMetni.Text = string.Join("  ·  ",
        [
            donanim.Islemci,
            $"{BaytBicimlendirici.BellekBicimle(donanim.RamBayt)} RAM",
            donanim.UefiMi ? "UEFI" : metinler.Al("karsilama.bios.eski"),
            donanim.TpmSurumu is not null
                ? $"TPM {donanim.TpmSurumu}"
                : metinler.Al("karsilama.tpm.yok")
        ]);

        MaddeListesi.ItemsSource = uyumluluk.Maddeler.Select(m => new MaddeSatiri(m)).ToList();

        UyariKutusu.Severity = uyumluluk.Uyumlu
            ? InfoBarSeverity.Success
            : InfoBarSeverity.Warning;

        UyariKutusu.Title = uyumluluk.Uyumlu
            ? metinler.Al("uyumluluk.hazir")
            : uyumluluk.BiosAyariylaCozulur
                ? metinler.Al("uyumluluk.bios.cozulur")
                : metinler.Al("uyumluluk.uyumsuz");

        UyariKutusu.Message = string.Empty;
        UyariKutusu.Visibility = Visibility.Visible;

        RaporPaneli.Visibility = Visibility.Visible;
    }

    private void DiskSecildi(object gonderen, SelectionChangedEventArgs e)
    {
        if (DiskListesi.SelectedItem is not DiskSatiri satir)
            return;

        _secilen = satir.Disk;

        var metinler = ServisSaglayici.Metinler;

        // Yikici olan tek arac geri kazanma; uyari yalnizca onda cikar.
        if (_aktif == Arac.Kazanma)
        {
            UyariKutusu.Severity = InfoBarSeverity.Warning;
            UyariKutusu.Title = metinler.Al("kazanma.onay", satir.Disk.Ad);
            UyariKutusu.Message = satir.Disk.KullanilanBayt > 0
                ? metinler.Al(
                    "kazanma.veri.uyari", BaytBicimlendirici.Bicimle(satir.Disk.KullanilanBayt))
                : string.Empty;

            UyariKutusu.Visibility = Visibility.Visible;
        }

        CalistirDugmesi.Visibility = Visibility.Visible;
    }

    private async void CalistirTiklandi(object gonderen, RoutedEventArgs e)
    {
        if (_secilen is not { } hedef)
            return;

        IslemeBasla();

        try
        {
            switch (_aktif)
            {
                case Arac.Kazanma:
                    await KazanmayiCalistirAsync(hedef);
                    break;

                case Arac.Saglik:
                    await SagligiCalistirAsync(hedef);
                    break;

                case Arac.Kurtarma:
                    await KurtarmayiCalistirAsync(hedef);
                    break;
            }
        }
        finally
        {
            IslemiBitir();
        }
    }

    private async Task KazanmayiCalistirAsync(DiskBilgisi hedef)
    {
        var metinler = ServisSaglayici.Metinler;
        IslemMetni.Text = metinler.Al("kazanma.calisiyor");

        var ilerleme = new Progress<GeriKazanmaIlerlemesi>(r =>
        {
            IslemCubugu.Value = r.ToplamYuzde;
            IslemMetni.Text = r.Aciklama;
        });

        var sonuc = await ServisSaglayici.GeriKazanmaServisi.GeriKazanAsync(
            hedef, BicimSec(), EtiketSec(), ilerleme, _iptalKaynagi!.Token);

        if (sonuc.Basarili)
        {
            Basarili(
                metinler.Al("kazanma.tamamlandi"),
                sonuc.SurucuHarfi is { } harf
                    ? metinler.Al("kazanma.sonuc", hedef.Ad, harf.TrimEnd('\\'))
                    : metinler.Al("kazanma.sonuc.harfsiz", hedef.Ad));
        }
        else if (sonuc.Hata is { } hata)
        {
            Hatali(hata);
        }
    }

    private async Task SagligiCalistirAsync(DiskBilgisi hedef)
    {
        var metinler = ServisSaglayici.Metinler;

        IslemMetni.Text = metinler.Al("saglik.calisiyor");
        IslemNotu.Text = metinler.Al("saglik.sure.uyari");

        var surucu = ServisSaglayici.DiskErisimi.SurucuYoluBul(hedef.DiskNumarasi);

        if (surucu is null)
        {
            Hatali(new UWinHatasi(
                NeOldu: metinler.Al("saglik.edilemedi"),
                Neden: metinler.Al("saglik.hata.harf.neden"),
                NeYapmali: metinler.Al("saglik.hata.harf.yapmali")));
            return;
        }

        var ilerleme = new Progress<double>(y => IslemCubugu.Value = y);

        var sonuc = await ServisSaglayici.SaglikServisi.TestEtAsync(
            surucu, hedef.BoyutBayt, ilerleme, _iptalKaynagi!.Token);

        SaglikSonucunuGoster(sonuc);
    }

    private void SaglikSonucunuGoster(SaglikSonucu sonuc)
    {
        var metinler = ServisSaglayici.Metinler;
        var bildirilen = BaytBicimlendirici.Bicimle(sonuc.BildirilenBoyutBayt);

        switch (sonuc.Durum)
        {
            case SaglikDurumu.Saglam:
                Basarili(metinler.Al("saglik.saglam"), metinler.Al("saglik.saglam.aciklama"));
                break;

            case SaglikDurumu.SahteKapasite:
                SonucKutusu.Severity = InfoBarSeverity.Error;
                SonucKutusu.Title = metinler.Al("saglik.sahte");
                SonucKutusu.Message = metinler.Al(
                    "saglik.sahte.aciklama",
                    bildirilen,
                    BaytBicimlendirici.Bicimle(sonuc.GercekBoyutBayt));
                SonucKutusu.Visibility = Visibility.Visible;
                break;

            case SaglikDurumu.Bozuk:
                SonucKutusu.Severity = InfoBarSeverity.Error;
                SonucKutusu.Title = metinler.Al("saglik.bozuk");
                SonucKutusu.Message = metinler.Al("saglik.bozuk.aciklama");
                SonucKutusu.Visibility = Visibility.Visible;
                break;

            default:
                if (sonuc.Hata is { } hata)
                    Hatali(hata);
                break;
        }
    }

    private async Task KurtarmayiCalistirAsync(DiskBilgisi hedef)
    {
        var metinler = ServisSaglayici.Metinler;

        IslemMetni.Text = metinler.Al("kurtarma.ekle.calisiyor");
        IslemCubugu.IsIndeterminate = true;

        var surucu = ServisSaglayici.DiskErisimi.SurucuYoluBul(hedef.DiskNumarasi);

        if (surucu is null)
        {
            Hatali(new UWinHatasi(
                NeOldu: metinler.Al("kurtarma.yazilamadi"),
                Neden: metinler.Al("saglik.hata.harf.neden"),
                NeYapmali: metinler.Al("saglik.hata.harf.yapmali")));
            return;
        }

        var sonuc = await ServisSaglayici.KurtarmaServisi.UsbyeYazAsync(
            surucu, _iptalKaynagi!.Token);

        IslemCubugu.IsIndeterminate = false;

        if (sonuc.Basarili)
        {
            Basarili(
                metinler.Al("kurtarma.ekle.tamam"),
                metinler.Al("kurtarma.yazildi", UWin.Cekirdek.Servisler.KurtarmaServisi.KlasorAdi)
                + "  " + metinler.Al("kurtarma.nasil.aciklama"));
        }
        else if (sonuc.Hata is { } hata)
        {
            Hatali(hata);
        }
    }

    private void IslemeBasla()
    {
        _iptalKaynagi?.Dispose();
        _iptalKaynagi = new CancellationTokenSource();

        DiskPaneli.Visibility = Visibility.Collapsed;
        KazanmaAyarlari.Visibility = Visibility.Collapsed;
        UyariKutusu.Visibility = Visibility.Collapsed;
        CalistirDugmesi.Visibility = Visibility.Collapsed;
        GeriDugmesi.Visibility = Visibility.Collapsed;

        IslemCubugu.Value = 0;
        IslemNotu.Text = string.Empty;
        IslemPaneli.Visibility = Visibility.Visible;

        // Islem surerken pencere kapatilamaz: yarida kesilen bir
        // bicimlendirme bellegi kullanilamaz halde birakir.
        CloseButtonText = string.Empty;
    }

    private void IslemiBitir()
    {
        IslemPaneli.Visibility = Visibility.Collapsed;
        GeriDugmesi.Visibility = Visibility.Visible;
        CloseButtonText = ServisSaglayici.Metinler.Al("dugme.kapat");

        _iptalKaynagi?.Dispose();
        _iptalKaynagi = null;
    }

    private void Basarili(string baslik, string mesaj)
    {
        SonucKutusu.Severity = InfoBarSeverity.Success;
        SonucKutusu.Title = baslik;
        SonucKutusu.Message = mesaj;
        SonucKutusu.Visibility = Visibility.Visible;
    }

    private void Hatali(UWinHatasi hata)
    {
        SonucKutusu.Severity = InfoBarSeverity.Error;
        SonucKutusu.Title = hata.NeOldu;
        SonucKutusu.Message = $"{hata.Neden}\n\n{hata.NeYapmali}";
        SonucKutusu.Visibility = Visibility.Visible;
    }

    private void GeriTiklandi(object gonderen, RoutedEventArgs e)
    {
        _aktif = Arac.Menu;
        _secilen = null;

        AracPaneli.Visibility = Visibility.Collapsed;
        MenuPaneli.Visibility = Visibility.Visible;

        Title = ServisSaglayici.Metinler.Al("araclar.baslik");
    }

    private string BicimSec() => BicimSecimi.SelectedIndex switch
    {
        1 => "NTFS",
        2 => "FAT32",
        _ => "exFAT"
    };

    /// <summary>
    /// Birim etiketi. Bos birakilirsa "USB" kullanilir; diskpart bos
    /// etiketi kabul etmez ve islem ortasinda hata verirdi.
    /// </summary>
    private string EtiketSec()
    {
        var girilen = EtiketKutusu.Text.Trim();

        return string.IsNullOrEmpty(girilen) ? "USB" : girilen;
    }
}
