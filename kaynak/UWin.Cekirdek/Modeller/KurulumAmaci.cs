namespace UWin.Cekirdek.Modeller;

/// <summary>
/// Kullanicinin Windows'u neden kurdugu.
///
/// Bu secim kozmetik degildir: verilen tavsiyeler birbirine zittir.
/// Virusten kurtulmak icin kuran birine "Sil'e basma, dosyalarin gider"
/// demek zararlidir - virus tam da o birakilan Windows.old klasorunde
/// hayatta kalir. Buna karsilik dosyalarini korumak isteyen birine
/// "her seyi sil" demek geri alinamaz bir kayiptir.
/// </summary>
public enum KurulumAmaci
{
    /// <summary>Belirtilmemis; en temkinli tavsiyeler verilir.</summary>
    Belirtilmemis,

    /// <summary>Virus, zararli yazilim veya reklam bulasmasi.</summary>
    Virus,

    /// <summary>Bilgisayar yavasladi, sistem bozuldu, acilmiyor.</summary>
    Yavaslik,

    /// <summary>Yeni veya bos bir diske ilk kurulum.</summary>
    YeniDisk,

    /// <summary>Calisan bir Windows'tan yeni surume gecis.</summary>
    SurumYukseltme
}

/// <summary>
/// Kurulum amacina gore degisen tavsiye.
///
/// <see cref="BolumSilinmeli"/>, kurulumdaki disk secme ekraninda
/// kullaniciya "Sil" mi yoksa "Ileri" mi diyecegimizi belirler -
/// programin verdigi en kritik tavsiye budur ve amaca gore tersine doner.
/// </summary>
public sealed record AmacTavsiyesi(
    KurulumAmaci Amac,
    string Baslik,
    string DiskEkraniTavsiyesi,
    bool BolumSilinmeli,
    IReadOnlyList<string> EkAdimlar);
