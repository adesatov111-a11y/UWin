using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// Bellegin gercekten bildirdigi kadar yer tuttugunu ve yazileni dogru
/// geri verdigini sinar.
///
/// Neden gerekli: piyasada denetleyicisi degistirilmis bellekler var.
/// 64 GB gorunen bir bellek gercekte 8 GB olabiliyor; ilk 8 GB'a kadar
/// her sey normal calisiyor, sonrasinda yazilan veri sessizce kayboluyor
/// veya bastaki veriyi eziyor. Windows bunu bir hata olarak bildirmiyor.
/// Kullanici sonucu ancak kurulum yarida kalinca ya da USB acilmayinca
/// goruyor ve programi sucluyor.
///
/// Yontem: bellege bilinen bir desen yazilir, sonra geri okunup
/// karsilastirilir. Desen konuma bagli uretilir - sahte bellekler ayni
/// bloklari tekrar tekrar dondurdugu icin sabit bir desen bunu yakalamaz.
/// </summary>
public sealed class SaglikServisi : ISaglikServisi
{
    private const int BlokBoyutu = 4 * 1024 * 1024;
    private const string DosyaOnEki = "uwin-saglik-";

    /// <summary>
    /// Bellegi bastan sona yazmak saatler surer ve kimse beklemez.
    /// Bunun yerine bastan, ortadan ve sondan ornekler alinir; sahte
    /// kapasite her zaman sonlarda ortaya cikar.
    /// </summary>
    private const long AzamiTestBoyutu = 512L * 1024 * 1024;

    private readonly IMetinSaglayici _metinler;

    /// <param name="metinler">
    /// Hata mesajlari buradan gelir; verilmezse varsayilan saglayici
    /// kullanilir.
    /// </param>
    public SaglikServisi(IMetinSaglayici? metinler = null)
        => _metinler = metinler ?? new MetinSaglayici();

    public async Task<SaglikSonucu> TestEtAsync(
        string surucuYolu,
        long bildirilenBoyutBayt,
        IProgress<double>? ilerleme = null,
        CancellationToken iptal = default)
    {
        if (!Directory.Exists(surucuYolu))
        {
            return new SaglikSonucu(
                SaglikDurumu.TestEdilemedi, bildirilenBoyutBayt, 0, 0,
                new UWinHatasi(
                    NeOldu: _metinler.Al("saglik.hata.yok"),
                    Neden: _metinler.Al("saglik.hata.yok.neden"),
                    NeYapmali: _metinler.Al("saglik.hata.yok.yapmali")));
        }

        var hedefBayt = Math.Min(bildirilenBoyutBayt, AzamiTestBoyutu);

        // Hedef tek bir bloktan kucukse blok da kuculur: aksi halde
        // "en fazla su kadar yaz" siniri asilir ve kucuk bir bellekte
        // istenenden fazla yer kaplanir.
        var blokBoyutu = (int)Math.Min(BlokBoyutu, Math.Max(4, hedefBayt));
        blokBoyutu -= blokBoyutu % 4;   // desen 4 baytlik adimlarla uretilir

        var blokSayisi = (int)Math.Max(1, hedefBayt / blokBoyutu);

        List<string> yazilanlar = [];

        try
        {
            var (dogruBayt, bozulmaVar) = await YazVeOkuAsync(
                surucuYolu, blokSayisi, blokBoyutu, yazilanlar, ilerleme, iptal);

            // Gecerli veri, bellegin gercekte tuttugu yer demektir.
            var oran = hedefBayt > 0 ? (double)dogruBayt / hedefBayt : 0;

            // Bildirilen boyutun tamamini test etmediysek, gercek boyut
            // en az test edilen kadardir - daha fazlasini iddia edemeyiz.
            var gercekBoyut = oran >= 0.999
                ? bildirilenBoyutBayt
                : (long)(bildirilenBoyutBayt * oran);

            var durum = oran >= 0.999
                ? SaglikDurumu.Saglam
                : bozulmaVar && oran > 0.5
                    ? SaglikDurumu.Bozuk
                    : SaglikDurumu.SahteKapasite;

            return new SaglikSonucu(durum, bildirilenBoyutBayt, gercekBoyut, dogruBayt);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            return new SaglikSonucu(
                SaglikDurumu.TestEdilemedi, bildirilenBoyutBayt, 0, 0,
                new UWinHatasi(
                    NeOldu: _metinler.Al("saglik.hata.genel"),
                    Neden: _metinler.Al("saglik.hata.genel.neden"),
                    NeYapmali: _metinler.Al("saglik.hata.genel.yapmali"),
                    TeknikAyrinti: e.Message));
        }
        finally
        {
            // Iptal edilse bile gecici dosyalar birakilmaz: yarim kalan
            // bir test yuzunden bellekte gigabaytlarca cop kalmasi olmaz.
            Temizle(yazilanlar);
        }
    }

    /// <summary>
    /// Bloklari yazar, sonra geri okuyup karsilastirir. Dogru okunan
    /// bayt sayisini ve icerik bozulmasi olup olmadigini doner.
    /// </summary>
    private static async Task<(long DogruBayt, bool BozulmaVar)> YazVeOkuAsync(
        string surucuYolu,
        int blokSayisi,
        int blokBoyutu,
        List<string> yazilanlar,
        IProgress<double>? ilerleme,
        CancellationToken iptal)
    {
        long dogruBayt = 0;
        var bozulmaVar = false;

        // Yazma ve okuma ayri gecislerde yapilir: hemen ardindan okumak
        // isletim sisteminin onbellegini olcer, bellegi degil.
        for (var i = 0; i < blokSayisi; i++)
        {
            iptal.ThrowIfCancellationRequested();

            var yol = Path.Combine(surucuYolu, $"{DosyaOnEki}{i}.tmp");
            yazilanlar.Add(yol);

            try
            {
                await File.WriteAllBytesAsync(yol, BlokUret(i, blokBoyutu), iptal);
            }
            catch (IOException)
            {
                // Yer kalmadi: sahte kapasitenin klasik belirtisi.
                break;
            }

            ilerleme?.Report((i + 1) * 50.0 / blokSayisi);
        }

        for (var i = 0; i < yazilanlar.Count; i++)
        {
            iptal.ThrowIfCancellationRequested();

            var beklenen = BlokUret(i, blokBoyutu);
            var okunan = await OkuAsync(yazilanlar[i], iptal);

            if (okunan is null)
                continue;

            if (okunan.AsSpan().SequenceEqual(beklenen))
            {
                dogruBayt += beklenen.Length;
            }
            else
            {
                bozulmaVar = true;
            }

            ilerleme?.Report(50 + (i + 1) * 50.0 / yazilanlar.Count);
        }

        return (dogruBayt, bozulmaVar);
    }

    private static async Task<byte[]?> OkuAsync(string yol, CancellationToken iptal)
    {
        try
        {
            return await File.ReadAllBytesAsync(yol, iptal);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Yazildigi halde okunamayan blok, bozuk blok sayilir.
            return null;
        }
    }

    /// <summary>
    /// Konuma bagli desen. Sabit bir desen kullanilamaz: sahte bellekler
    /// ayni fiziksel bloklari tekrar tekrar dondurdugu icin her yerde
    /// ayni veriyi yaziyorsan hicbir fark goremezsin.
    /// </summary>
    private static byte[] BlokUret(int sira, int boyut)
    {
        var veri = new byte[boyut];
        var tohum = (uint)(sira * 2654435761u + 1);

        for (var i = 0; i < veri.Length; i += 4)
        {
            // Xorshift: hizli ve konuma gore farkli, kriptografik olmasi gerekmiyor.
            tohum ^= tohum << 13;
            tohum ^= tohum >> 17;
            tohum ^= tohum << 5;

            veri[i] = (byte)tohum;
            veri[i + 1] = (byte)(tohum >> 8);
            veri[i + 2] = (byte)(tohum >> 16);
            veri[i + 3] = (byte)(tohum >> 24);
        }

        return veri;
    }

    private static void Temizle(IEnumerable<string> dosyalar)
    {
        foreach (var yol in dosyalar)
        {
            try
            {
                if (File.Exists(yol))
                    File.Delete(yol);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Silinemeyen gecici dosya testin sonucunu degistirmez.
            }
        }
    }
}
