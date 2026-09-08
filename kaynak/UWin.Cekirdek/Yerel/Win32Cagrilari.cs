using System.Runtime.InteropServices;

namespace UWin.Cekirdek.Yerel;

/// <summary>
/// Win32 disk API bildirimleri. Tek yerde toplanir; bu dosya disinda
/// hicbir yerde DllImport bulunmaz.
/// </summary>
internal static partial class Win32Cagrilari
{
    internal const uint GenericRead = 0x80000000;
    internal const uint GenericWrite = 0x40000000;
    internal const uint FileShareRead = 0x00000001;
    internal const uint FileShareWrite = 0x00000002;
    internal const uint OpenExisting = 3;

    internal const uint FsctlLockVolume = 0x00090018;
    internal const uint FsctlUnlockVolume = 0x0009001C;
    internal const uint FsctlDismountVolume = 0x00090020;

    internal static readonly nint GecersizTanitici = -1;

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW",
        StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    internal static partial nint CreateFile(
        string dosyaAdi,
        uint erisim,
        uint paylasim,
        nint guvenlik,
        uint olusturma,
        uint bayraklar,
        nint sablon);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool DeviceIoControl(
        nint aygit,
        uint kod,
        nint girisTampon,
        uint girisBoyut,
        nint cikisTampon,
        uint cikisBoyut,
        out uint donenBoyut,
        nint ortusen);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool CloseHandle(nint tanitici);
}
