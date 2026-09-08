using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using UWin.Uygulama.Servisler;

namespace UWin.Uygulama;

/// <summary>
/// Dil secimi. Ilk acilista bir kez gosterilir, sonra sag ustteki
/// dugmeden acilir.
///
/// Basligi bilerek iki dilde ("Dil / Language"): kullanici bu ekrani
/// gordugunde arayuz henuz onun dilinde olmayabilir ve tek dilde bir
/// baslik, digerinin bunu anlamamasi demek.
/// </summary>
public sealed partial class DilPenceresi : ContentDialog
{
    /// <summary>Kullanicinin sectigi dil kodu.</summary>
    public string SecilenDil { get; private set; }

    public DilPenceresi(string aktifDil)
    {
        InitializeComponent();

        SecilenDil = aktifDil;

        var metinler = ServisSaglayici.Metinler;

        Title = metinler.Al("dil.secim.baslik");
        PrimaryButtonText = metinler.Al("dil.secim.devam");
        DefaultButton = ContentDialogButton.Primary;

        AciklamaMetni.Text = metinler.Al("dil.secim.aciklama");

        DilListesi.ItemsSource = UWin.Cekirdek.Servisler.MetinSaglayici.DilleriListele()
            .Select(kod => new DilSatiri(kod, metinler.Al($"dil.{kod}"), kod == aktifDil))
            .ToList();
    }

    /// <summary>Listede gosterilen tek dil.</summary>
    public sealed record DilSatiri(string Kod, string Ad, bool Secili);

    /// <summary>
    /// Secim aninda dil hemen degistirilir: kullanici "Devam et"
    /// dugmesine basmadan once secimin sonucunu gorur. Yanlis dile
    /// tikladiysa bunu okuyamadigi bir ekranda degil, hemen anlar.
    /// </summary>
    private void DilSecildi(object gonderen, RoutedEventArgs e)
    {
        if (gonderen is not RadioButton { Tag: string kod })
            return;

        SecilenDil = kod;

        ServisSaglayici.Metinler.DilDegistir(kod);

        Title = ServisSaglayici.Metinler.Al("dil.secim.baslik");
        PrimaryButtonText = ServisSaglayici.Metinler.Al("dil.secim.devam");
        AciklamaMetni.Text = ServisSaglayici.Metinler.Al("dil.secim.aciklama");
    }
}
