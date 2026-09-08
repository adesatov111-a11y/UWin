using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using UWin.Cekirdek.Modeller;
using UWin.Uygulama.Bicimlendirme;
using UWin.Uygulama.GorunumModelleri;
using UWin.Uygulama.Servisler;

namespace UWin.Uygulama.Adimlar;

/// <summary>
/// Hedef disk secimi. Varsayilan olarak yalnizca USB bellekler gorunur;
/// gelismis mod dahili ve buyuk harici diskleri de acar. Sistem diski
/// listede gorunur ama secilemez.
/// </summary>
public sealed partial class Adim4Disk : Page
{
    private SihirbazDurumu? _durum;

    public Adim4Disk() => InitializeComponent();

    /// <summary>Listede gosterilen disk karti.</summary>
    public sealed record DiskSatiri(
        DiskBilgisi Disk, string? YarimUyarisi, long AsgariBoyut, bool IsodanGeldi)
    {
        public string Ad => Disk.Ad;

        public bool SecilebilirMi => Disk.YazilabilirMi;

        /// <summary>Segoe Fluent Icons glifleri.</summary>
        public string Simge => Disk.Sinif switch
        {
            DiskSinifi.UsbBellek => "",
            DiskSinifi.SistemDiski => "",
            _ => ""
        };

        public string SinifEtiketi => ServisSaglayici.Metinler.Al(Disk.Sinif switch
        {
            DiskSinifi.UsbBellek => "disk.sinif.usb",
            DiskSinifi.HariciDisk => "disk.sinif.harici",
            DiskSinifi.DahiliDisk => "disk.sinif.dahili",
            _ => "disk.sinif.sistem"
        });

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

        /// <summary>Bu diske dair kullanicinin bilmesi gereken en onemli uyari.</summary>
        public string Uyari
        {
            get
            {
                if (!Disk.YazilabilirMi)
                    return ServisSaglayici.Metinler.Al("disk.sistem.engel");

                if (Disk.BoyutBayt < AsgariBoyut)
                {
                    // Gereken boyut ISO'nun icinden hesaplandiysa bunu soyle:
                    // "secilen Windows" demek, henuz bir sey secmemis olan
                    // kullaniciya bos bir laf gibi geliyor.
                    return ServisSaglayici.Metinler.Al(
                        IsodanGeldi ? "disk.kucuk.iso" : "disk.kucuk",
                        BaytBicimlendirici.Bicimle(Disk.BoyutBayt),
                        BaytBicimlendirici.Bicimle(AsgariBoyut));
                }

                return YarimUyarisi ?? string.Empty;
            }
        }

        public Visibility UyariGorunurlugu =>
            string.IsNullOrEmpty(Uyari) ? Visibility.Collapsed : Visibility.Visible;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        _durum = (SihirbazDurumu)e.Parameter;

        var metinler = ServisSaglayici.Metinler;
        GelismisAnahtari.OffContent = metinler.Al("disk.gelismis.kapali");
        GelismisAnahtari.OnContent = metinler.Al("disk.gelismis.acik");
        GelismisAciklamasi.Text = metinler.Al("disk.gelismis.aciklama");

        GelismisAnahtari.IsOn = _durum.GelismisMod;

        await ListeyiYenileAsync();
    }

    private async Task ListeyiYenileAsync()
    {
        if (_durum is null)
            return;

        var asgari = _durum.GerekenUsbBoyutuBayt;
        var isodanGeldi = _durum.IsoKimligi is { GerekenUsbBoyutuBayt: > 0 };
        var diskler = ServisSaglayici.DiskServisi.DiskleriListele(_durum.GelismisMod);

        List<DiskSatiri> satirlar = [];

        foreach (var disk in diskler)
            satirlar.Add(new DiskSatiri(
                disk, await YarimUyarisiAlAsync(disk), asgari, isodanGeldi));

        DiskListesi.ItemsSource = satirlar;

        // Gereken boyut listenin ustunde de yazar: kullanici hangi USB'yi
        // arayacagini disklere tek tek bakmadan bilsin.
        GerekenBoyutMetni.Text = ServisSaglayici.Metinler.Al(
            "disk.gereken", BaytBicimlendirici.Bicimle(asgari));

        var bosMu = satirlar.Count == 0;
        BosListeKutusu.Visibility = bosMu ? Visibility.Visible : Visibility.Collapsed;

        if (bosMu)
            BosListeKutusu.Message = ServisSaglayici.Metinler.Al("disk.bulunamadi");

        // Onceki secim hala listedeyse korunur.
        if (_durum.SecilenDisk is { } onceki)
        {
            DiskListesi.SelectedItem = satirlar.FirstOrDefault(
                s => s.Disk.DiskNumarasi == onceki.DiskNumarasi
                     && s.Disk.SeriNumarasi == onceki.SeriNumarasi);
        }
    }

    /// <summary>Diskte yarim kalmis bir yazma varsa kullaniciya bildirilir.</summary>
    private static async Task<string?> YarimUyarisiAlAsync(DiskBilgisi disk)
    {
        var yol = ServisSaglayici.DiskErisimi.SurucuYoluBul(disk.DiskNumarasi);
        if (yol is null)
            return null;

        var isaret = await ServisSaglayici.DurumIsaretleyici.IsaretOkuAsync(yol);

        return isaret is null ? null : ServisSaglayici.Metinler.Al("disk.yarim");
    }

    private void DiskSecildi(object gonderen, SelectionChangedEventArgs e)
    {
        if (_durum is not null && DiskListesi.SelectedItem is DiskSatiri satir)
            _durum.SecilenDisk = satir.Disk;
    }

    private async void GelismisDegisti(object gonderen, RoutedEventArgs e)
    {
        if (_durum is null)
            return;

        _durum.GelismisMod = GelismisAnahtari.IsOn;
        await ListeyiYenileAsync();
    }
}
