using Microsoft.UI.Xaml;
using UWin.Uygulama.Servisler;

namespace UWin.Uygulama;

public partial class App : Application
{
    /// <summary>
    /// Dosya secici gibi Win32 tabanli dialoglar bir pencere tanitici ister;
    /// sayfalar ana pencereye buradan ulasir.
    /// </summary>
    public static Window? AnaPencereOrnegi { get; private set; }

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Kayitli dil, pencere kurulmadan once uygulanir. Pencere
        // metinlerini kurucusunda okudugu icin sonra uygulamak, ilk
        // karenin yanlis dilde cizilmesine yol acardi.
        //
        // Ayar okunamazsa program yine acilir: bir tercih dosyasi
        // yuzunden acilmayan bir program, yanlis dilde acilan bir
        // programdan cok daha kotudur.
        try
        {
            if (ServisSaglayici.AyarServisi.Oku().Dil is { } dil)
                ServisSaglayici.Metinler.DilDegistir(dil);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Varsayilan dille devam edilir.
        }

        AnaPencereOrnegi = new AnaPencere();
        AnaPencereOrnegi.Activate();
    }
}
