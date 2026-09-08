namespace UWin.Cekirdek.Arayuzler;

/// <summary>Kullaniciya gosterilen metinleri saglar. Metinler koda gomulmez.</summary>
public interface IMetinSaglayici
{
    string AktifDil { get; }

    /// <summary>
    /// Dil degistiginde tetiklenir. Arayuz bunu dinleyip acik olan
    /// sayfayi yeniden kurar.
    /// </summary>
    event EventHandler? DilDegisti;

    /// <summary>Anahtarin karsiligini doner; anahtar taninmazsa anahtarin kendisini doner.</summary>
    string Al(string anahtar);

    string Al(string anahtar, params object[] degerler);

    /// <summary>
    /// Desteklenmeyen dil verilirse Turkce'ye duser. Zaten aktif olan
    /// dile gecmek hicbir sey yapmaz.
    /// </summary>
    void DilDegistir(string dil);
}
