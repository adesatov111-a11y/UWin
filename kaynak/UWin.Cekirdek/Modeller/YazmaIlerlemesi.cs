namespace UWin.Cekirdek.Modeller;

/// <summary>
/// Yazma sirasinda arayuze bildirilen anlik durum.
///
/// Bayt sayaclari yuzdenin yani sira tasinir: kullaniciya "kac dakika
/// kaldi" diyebilmek icin yuzde yetmez, hizin bilinmesi gerekir. Yuzde
/// tek basina bekleyen birine hicbir sey soylemiyor - "%40" bir dakika
/// da olabilir yirmi dakika da.
/// </summary>
public sealed record YazmaIlerlemesi(
    YazmaAdimi Adim,
    double AdimYuzdesi,
    double ToplamYuzde,
    string Aciklama,
    long YazilanBayt = 0,
    long ToplamBayt = 0,
    double BaytBolumSaniye = 0)
{
    /// <summary>
    /// Kalan sure tahmini. Hiz olcelemediyse veya toplam bilinmiyorsa
    /// null doner - yanlis bir tahmin gostermektense hic gostermemek
    /// dogrudur; kullanici bir kere "2 dakika" deyip 20 dakika bekleyince
    /// bir daha hicbir tahmine inanmiyor.
    /// </summary>
    public TimeSpan? KalanSure
    {
        get
        {
            if (BaytBolumSaniye <= 0 || ToplamBayt <= 0 || YazilanBayt >= ToplamBayt)
                return null;

            return TimeSpan.FromSeconds((ToplamBayt - YazilanBayt) / BaytBolumSaniye);
        }
    }
}
