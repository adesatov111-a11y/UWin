using System.Globalization;

namespace UWin.Uygulama.Bicimlendirme;

/// <summary>
/// Bayt degerlerini kullanicinin okuyabilecegi bicime cevirir.
/// Aygit ureticileri gibi 1 GB = 1.000.000.000 bayt sayilir; boylece
/// "32 GB" yazan bir bellek arayuzde de 32 GB gorunur.
/// </summary>
public static class BaytBicimlendirici
{
    private static readonly CultureInfo Kultur = new("tr-TR");

    /// <summary>
    /// RAM icin ikili birim (1 GB = 2^30). Bellek her zaman boyle olculur:
    /// 16 GB'lik bir modul 17.179.869.184 bayttir ve kullanici onu "16 GB"
    /// olarak bilir. Disk birimiyle karistirilirsa 17,1 GB gibi gorunur.
    /// </summary>
    public static string BellekBicimle(long bayt)
    {
        if (bayt <= 0)
            return "0 GB";

        // Windows, entegre grafige ayrilan bellegi dusurek bildirir: 16 GB'lik
        // bir sistem 15,9 GB gorunur. Kullanici taktigi modulu bilir, bu yuzden
        // en yakin tam sayiya yuvarlanir.
        var gb = bayt / (1024.0 * 1024 * 1024);

        return string.Format(Kultur, "{0:0} GB", Math.Round(gb));
    }

    public static string Bicimle(long bayt)
    {
        if (bayt <= 0)
            return "0 B";

        if (bayt < 1_000)
            return $"{bayt} B";

        if (bayt < 1_000_000)
            return string.Format(Kultur, "{0:0} KB", bayt / 1_000.0);

        if (bayt < 1_000_000_000)
            return string.Format(Kultur, "{0:0} MB", bayt / 1_000_000.0);

        if (bayt < 1_000_000_000_000)
            return string.Format(Kultur, "{0:0.0} GB", bayt / 1_000_000_000.0);

        return string.Format(Kultur, "{0:0.00} TB", bayt / 1_000_000_000_000.0);
    }
}
