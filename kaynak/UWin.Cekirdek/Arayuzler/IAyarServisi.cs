using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

/// <summary>
/// Kalici kullanici tercihleri. Hicbir islem istisna firlatmaz:
/// ayar okunamaz veya yazilamazsa program yine calisir.
/// </summary>
public interface IAyarServisi
{
    Ayarlar Oku();

    void DilKaydet(string dil);
}
