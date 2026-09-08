using System.Reflection;
using Microsoft.UI.Xaml.Controls;
using UWin.Uygulama.Servisler;

namespace UWin.Uygulama;

/// <summary>Urun tanitimi, guvenlik ozeti ve site baglantisi.</summary>
public sealed partial class HakkindaPenceresi : ContentDialog
{
    public HakkindaPenceresi()
    {
        InitializeComponent();

        var metinler = ServisSaglayici.Metinler;

        Title = metinler.Al("hakkinda.baslik");
        CloseButtonText = metinler.Al("dugme.kapat");

        TanimMetni.Text = metinler.Al("hakkinda.tanim");
        AciklamaMetni.Text = metinler.Al("hakkinda.aciklama");

        OzellikBasligi.Text = metinler.Al("hakkinda.ozellik.baslik");
        AraclarBasligi.Text = metinler.Al("hakkinda.araclar.baslik");
        GuvenlikBasligi.Text = metinler.Al("hakkinda.guvenlik.baslik");

        // Sira, kullanicinin yasadigi siradir: once bilgisayarina
        // bakilir, sonra dosya, sonra yazma, sonra kurulum sonrasi.
        OzellikListesi.ItemsSource = new List<OzellikSatiri>
        {
            new(metinler.Al("hakkinda.ozellik.1.baslik"), metinler.Al("hakkinda.ozellik.1")),
            new(metinler.Al("hakkinda.ozellik.2.baslik"), metinler.Al("hakkinda.ozellik.2")),
            new(metinler.Al("hakkinda.ozellik.3.baslik"), metinler.Al("hakkinda.ozellik.3")),
            new(metinler.Al("hakkinda.ozellik.4.baslik"), metinler.Al("hakkinda.ozellik.4"))
        };

        AraclarListesi.ItemsSource = new[]
        {
            metinler.Al("hakkinda.araclar.1"),
            metinler.Al("hakkinda.araclar.2"),
            metinler.Al("hakkinda.araclar.3")
        };

        GuvenlikListesi.ItemsSource = new[]
        {
            metinler.Al("hakkinda.guvenlik.1"),
            metinler.Al("hakkinda.guvenlik.2"),
            metinler.Al("hakkinda.guvenlik.3"),
            metinler.Al("hakkinda.guvenlik.4"),
            metinler.Al("hakkinda.guvenlik.5")
        };

        SiteBaglantisi.Content = metinler.Al("uygulama.site");
        SurumMetni.Text = metinler.Al("hakkinda.surum", SurumNumarasi());
        TelifMetni.Text = metinler.Al("hakkinda.telif", DateTime.Now.Year.ToString());
    }

    /// <summary>Ne yaptigini anlatan tek asama.</summary>
    public sealed record OzellikSatiri(string Baslik, string Metin);

    private static string SurumNumarasi()
    {
        var surum = Assembly.GetExecutingAssembly().GetName().Version;

        return surum is null ? "1.0" : $"{surum.Major}.{surum.Minor}";
    }
}
