using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace UWin.Uygulama.Servisler;

/// <summary>
/// Modern Windows dosya acma penceresi (IFileOpenDialog).
///
/// WinRT'nin FileOpenPicker'i yonetici yetkisiyle calisan paketlenmemis
/// uygulamalarda guvenilir degildir; eski GetOpenFileName ise Windows XP
/// gorunumunde acilir. IFileOpenDialog Explorer'in kendi penceresidir:
/// hem modern gorunur hem yukseltilmis sureclerde calisir.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class DosyaSecici
{
    /// <summary>Kullanici mevcut bir dosya secmeli.</summary>
    private const uint DosyaVarOlmali = 0x00001000;

    /// <summary>Kisayollar hedefine cozulur.</summary>
    private const uint YolVarOlmali = 0x00000800;

    /// <summary>Yalnizca gercek dosya sistemi ogeleri secilebilir.</summary>
    private const uint DosyaSistemi = 0x00000040;

    /// <summary>SIGDN_FILESYSPATH: tam disk yolu.</summary>
    private const uint DosyaYoluBicimi = 0x80058000;

    private const int IptalEdildi = unchecked((int)0x800704C7);

    /// <summary>
    /// Dosya secme penceresi acar. Kullanici vazgecerse veya bir sorun
    /// olusursa null doner - hicbir kosulda istisna firlatmaz.
    /// </summary>
    internal static string? Sec(nint sahipPencere, string baslik, string suzgecAdi, string uzanti)
        => Sec(sahipPencere, baslik, suzgecAdi, uzanti, pencereyiGoster: true);

    /// <summary>
    /// Asil uygulama. <paramref name="pencereyiGoster"/> yalnizca testler
    /// icin false verilir: COM nesnesinin kurulmasi ve suzgeclerin
    /// ayarlanmasi dogrulanabilsin ama ekrana gercek bir pencere gelmesin.
    /// Aksi halde her "dotnet test" kosusunda dosya gezgini aciliyor ve
    /// biri kapatana kadar testler bekliyordu.
    /// </summary>
    internal static string? Sec(
        nint sahipPencere, string baslik, string suzgecAdi, string uzanti, bool pencereyiGoster)
    {
        IFileOpenDialog? diyalog = null;

        try
        {
            diyalog = (IFileOpenDialog)new FileOpenDialog();

            SuzgeciAyarla(diyalog, suzgecAdi, uzanti);

            diyalog.SetTitle(baslik);
            diyalog.SetDefaultExtension(uzanti.TrimStart('.'));

            diyalog.GetOptions(out var secenekler);
            diyalog.SetOptions(secenekler | DosyaVarOlmali | YolVarOlmali | DosyaSistemi);

            if (!pencereyiGoster)
                return null;

            diyalog.Show(sahipPencere);
            diyalog.GetResult(out var oge);

            try
            {
                oge.GetDisplayName(DosyaYoluBicimi, out var yol);
                return string.IsNullOrWhiteSpace(yol) ? null : yol;
            }
            finally
            {
                Marshal.ReleaseComObject(oge);
            }
        }
        catch (COMException e) when (e.HResult == IptalEdildi)
        {
            // Kullanici vazgecti; bu bir hata degildir.
            return null;
        }
        catch (Exception e) when (e is COMException or InvalidCastException or NotSupportedException)
        {
            // Diyalog acilamazsa kullanici dosya secemez ama program cokmez.
            return null;
        }
        finally
        {
            if (diyalog is not null)
                Marshal.ReleaseComObject(diyalog);
        }
    }

    private static void SuzgeciAyarla(IFileOpenDialog diyalog, string suzgecAdi, string uzanti)
    {
        var suzgecler = new[]
        {
            new SuzgecGirisi { Ad = suzgecAdi, Desen = $"*{uzanti}" },
            new SuzgecGirisi { Ad = ServisSaglayici.Metinler.Al("dosya.tumu"), Desen = "*.*" }
        };

        diyalog.SetFileTypes((uint)suzgecler.Length, suzgecler);
        diyalog.SetFileTypeIndex(1);
    }

    [ComImport]
    [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
    private class FileOpenDialog;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SuzgecGirisi
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string Ad;
        [MarshalAs(UnmanagedType.LPWStr)] public string Desen;
    }

    /// <summary>
    /// IFileOpenDialog. Uye sirasi COM sozlesmesinden gelir ve degistirilemez:
    /// IModalWindow, sonra IFileDialog uyeleri.
    /// </summary>
    [ComImport]
    [Guid("d57c7288-d4ad-4768-be02-9d969532d960")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileOpenDialog
    {
        // IModalWindow
        [PreserveSig] int Show(nint sahipPencere);

        // IFileDialog
        void SetFileTypes(uint sayi, [MarshalAs(UnmanagedType.LPArray)] SuzgecGirisi[] suzgecler);
        void SetFileTypeIndex(uint sira);
        void GetFileTypeIndex(out uint sira);
        void Advise(nint dinleyici, out uint jeton);
        void Unadvise(uint jeton);
        void SetOptions(uint secenekler);
        void GetOptions(out uint secenekler);
        void SetDefaultFolder(IShellItem klasor);
        void SetFolder(IShellItem klasor);
        void GetFolder(out IShellItem klasor);
        void GetCurrentSelection(out IShellItem oge);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string ad);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string ad);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string baslik);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string etiket);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string etiket);
        void GetResult(out IShellItem oge);
        void AddPlace(IShellItem oge, int konum);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string uzanti);
        void Close(int sonuc);
        void SetClientGuid(in Guid kimlik);
        void ClearClientData();
        void SetFilter(nint suzgec);
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(nint baglam, in Guid bolum, in Guid arayuz, out nint nesne);
        void GetParent(out IShellItem ust);
        void GetDisplayName(uint bicim, [MarshalAs(UnmanagedType.LPWStr)] out string ad);
        void GetAttributes(uint istenen, out uint nitelikler);
        void Compare(IShellItem digeri, uint ipucu, out int sonuc);
    }
}
