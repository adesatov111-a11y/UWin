using System.Text;
using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// Kurulum USB'sini ayni zamanda bir kurtarma araci yapar.
///
/// Windows kurulum medyasi zaten WinRE tasir: USB'den acilip "Bilgisayarinizi
/// onarin" secildiginde Komut Istemi'ne ulasilabilir. Eksik olan sey arac
/// degil, bilgidir - o ekrana gelen kullanici hangi komutu yazacagini
/// bilmez ve internete bakmak icin de calisan bir bilgisayari yoktur.
///
/// Bu yuzden komutlar USB'ye .bat olarak yazilir: kullanici Komut
/// Istemi'nde tek satir yazip calistirir. Yaninda HTML rehber durur;
/// telefondan da okunabilir.
/// </summary>
public sealed class KurtarmaServisi : IKurtarmaServisi
{
    /// <summary>USB'de araclarin durdugu klasor.</summary>
    public const string KlasorAdi = "UWin-Kurtarma";

    private readonly IMetinSaglayici _metinler;

    /// <param name="metinler">
    /// Arac adlari, aciklamalari ve rehber metni buradan gelir. USB'ye
    /// yazilan dosyalar da bu dilde olur - kullanici onlari kurulum
    /// ortaminda, programin arayuzu olmadan okuyacak.
    /// </param>
    public KurtarmaServisi(IMetinSaglayici? metinler = null)
        => _metinler = metinler ?? new MetinSaglayici();

    /// <summary>
    /// Araclarin sabit tanimi: kimlik, komut ve diske yazip yazmadigi.
    /// Ad ve aciklama metin kaynagindan gelir.
    ///
    /// Sira onemlidir, en sik ise yarayan en ustte: acilmayan bir
    /// Windows'un buyuk cogunlugu onyukleme kaydi bozulmasindan
    /// kaynaklanir ve ilk madde tam da onu onarir.
    /// </summary>
    private static readonly (string Kimlik, string Komut, bool DiskeYazar)[] Tanimlar =
    [
        ("bootrec",
         "bootrec /fixmbr & bootrec /fixboot & bootrec /scanos & bootrec /rebuildbcd", true),

        ("sfc", "sfc /scannow /offbootdir=C:\\ /offwindir=C:\\Windows", true),

        ("chkdsk", "chkdsk C: /f /r", true),

        ("dosyalarim", "notepad", false),

        ("surumler", "diskpart /s liste.txt", false),

        ("guvenli-mod", "bcdedit /set {default} safeboot minimal", true)
    ];

    public IReadOnlyList<KurtarmaAraci> AraclariGetir() => Tanimlar
        .Select(t => new KurtarmaAraci(
            Kimlik: t.Kimlik,
            Ad: _metinler.Al($"kurtarma.{AnahtarAdi(t.Kimlik)}.ad"),
            NeZamanKullanilir: _metinler.Al($"kurtarma.{AnahtarAdi(t.Kimlik)}.nezaman"),
            Komut: t.Komut,
            DiskeYazar: t.DiskeYazar))
        .ToList();

    /// <summary>Metin anahtarlarinda tire kullanilmaz.</summary>
    private static string AnahtarAdi(string kimlik)
        => kimlik == "guvenli-mod" ? "guvenli" : kimlik;

    public async Task<KurtarmaSonucu> UsbyeYazAsync(
        string surucuYolu, CancellationToken iptal = default)
    {
        var klasor = Path.Combine(surucuYolu, KlasorAdi);

        try
        {
            Directory.CreateDirectory(klasor);

            foreach (var arac in AraclariGetir())
            {
                iptal.ThrowIfCancellationRequested();

                await BatYazAsync(klasor, arac, iptal);
            }

            // diskpart betigi ayri bir dosya ister; "surumler" araci bunu kullanir.
            await DosyaYazAsync(
                Path.Combine(klasor, "liste.txt"),
                "list disk\r\nlist volume\r\nexit\r\n",
                iptal);

            await DosyaYazAsync(Path.Combine(klasor, "OKU-BENI.html"), HtmlUret(), iptal, Encoding.UTF8);

            return new KurtarmaSonucu(true, klasor, null);
        }
        catch (OperationCanceledException)
        {
            return new KurtarmaSonucu(false, null, new UWinHatasi(
                NeOldu: _metinler.Al("hata.iptal"),
                Neden: _metinler.Al("kazanma.hata.iptal.neden"),
                NeYapmali: _metinler.Al("kurtarma.yazilamadi")));
        }
        catch (Exception e)
        {
            return new KurtarmaSonucu(false, null, new UWinHatasi(
                NeOldu: _metinler.Al("kurtarma.yazilamadi"),
                Neden: _metinler.Al("hata.yazilamadi.neden"),
                NeYapmali: _metinler.Al("kurtarma.nasil.aciklama"),
                TeknikAyrinti: e.Message));
        }
    }

    /// <summary>
    /// Tek bir aracin .bat dosyasi. ASCII ve CRLF zorunludur: cmd.exe
    /// UTF-8 okumaz (aksanli harfler bozulur) ve LF satir sonlu bir
    /// .bat "komut taninmadi" hatasi verir.
    /// </summary>
    private Task BatYazAsync(string klasor, KurtarmaAraci arac, CancellationToken iptal)
    {
        var s = new StringBuilder();
        var ad = Asciilestir(arac.Ad);

        s.Append("@echo off\r\n");
        s.Append("chcp 437 > nul\r\n");
        s.Append($"title UWin - {ad}\r\n");
        s.Append("echo.\r\n");
        s.Append($"echo  {ad}\r\n");
        s.Append("echo  ------------------------------------------------\r\n");
        s.Append($"echo  {Asciilestir(arac.NeZamanKullanilir)}\r\n");
        s.Append("echo.\r\n");

        if (arac.DiskeYazar)
        {
            s.Append($"echo  {Asciilestir(_metinler.Al("kurtarma.bat.dikkat"))}\r\n");
            s.Append("echo.\r\n");

            // choice /c harfleri dile gore degisir: Turkce E/H, Ingilizce Y/N.
            var secenekler = _metinler.AktifDil == "tr" ? "EH" : "YN";

            s.Append($"choice /c {secenekler} /n /m \"{Asciilestir(_metinler.Al("kurtarma.bat.devam"))}\"\r\n");
            s.Append("if errorlevel 2 exit /b\r\n");
            s.Append("echo.\r\n");
        }

        s.Append($"echo  {Asciilestir(_metinler.Al("kurtarma.bat.calistirilan"))} {Asciilestir(arac.Komut)}\r\n");
        s.Append("echo.\r\n");
        s.Append($"{arac.Komut}\r\n");
        s.Append("echo.\r\n");
        s.Append($"echo  {Asciilestir(_metinler.Al("kurtarma.bat.bitti"))}\r\n");
        s.Append("pause > nul\r\n");

        return DosyaYazAsync(Path.Combine(klasor, $"{arac.Kimlik}.bat"), s.ToString(), iptal, Encoding.ASCII);
    }

    private static Task DosyaYazAsync(
        string yol, string icerik, CancellationToken iptal, Encoding? kodlama = null)
        => File.WriteAllTextAsync(yol, icerik, kodlama ?? Encoding.ASCII, iptal);

    /// <summary>
    /// Aksanli harfleri ASCII karsiliklarina cevirir. Kurtarma
    /// ortamindaki Komut Istemi Turkce kod sayfasi yuklu gelmez;
    /// o harfler orada anlamsiz isaretlere donusur.
    /// </summary>
    private static string Asciilestir(string metin)
    {
        var s = new StringBuilder(metin.Length);

        foreach (var harf in metin)
        {
            s.Append(harf switch
            {
                'ı' => "i", 'İ' => "I",
                'ş' => "s", 'Ş' => "S",
                'ğ' => "g", 'Ğ' => "G",
                'ü' => "u", 'Ü' => "U",
                'ö' => "o", 'Ö' => "O",
                'ç' => "c", 'Ç' => "C",
                '—' => "-", '–' => "-",
                '“' or '”' => "\"",
                '‘' or '’' => "'",
                _ => harf < 128 ? harf.ToString() : "?"
            });
        }

        return s.ToString();
    }

    /// <summary>Telefondan da okunabilen rehber. Kurulum rehberiyle ayni bicimde.</summary>
    private string HtmlUret()
    {
        var s = new StringBuilder();
        var dil = _metinler.AktifDil;

        s.AppendLine("<!DOCTYPE html>");
        s.AppendLine($"<html lang=\"{dil}\"><head><meta charset=\"utf-8\">");
        s.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        s.AppendLine($"<title>UWin - {Kacis(_metinler.Al("kurtarma.araclar"))}</title>");
        s.AppendLine("<style>");
        s.AppendLine("  :root { color-scheme: light dark; }");
        s.AppendLine("  body { font-family: system-ui, -apple-system, 'Segoe UI', sans-serif;");
        s.AppendLine("         max-width: 42rem; margin: 0 auto; padding: 2rem 1.25rem; line-height: 1.6; }");
        s.AppendLine("  h1 { font-size: 1.5rem; margin-bottom: .25rem; }");
        s.AppendLine("  .alt { opacity: .7; font-size: .9rem; margin-bottom: 2rem; }");
        s.AppendLine("  .arac { border: 1px solid rgba(127,127,127,.3); border-radius: .5rem;");
        s.AppendLine("          padding: 1rem 1.25rem; margin-bottom: 1.25rem; }");
        s.AppendLine("  .arac h2 { font-size: 1.1rem; margin: 0 0 .35rem; }");
        s.AppendLine("  .dosya { font-family: ui-monospace, monospace; font-weight: 600; }");
        s.AppendLine("  .uyari { color: #b3261e; font-weight: 600; font-size: .9rem; }");
        s.AppendLine("  .not { background: rgba(127,127,127,.12); padding: 1rem;");
        s.AppendLine("         border-radius: .5rem; margin: 2rem 0; }");
        s.AppendLine("  ol li { margin-bottom: .75rem; }");
        s.AppendLine("  footer { margin-top: 3rem; padding-top: 1rem;");
        s.AppendLine("           border-top: 1px solid rgba(127,127,127,.3); opacity: .7; font-size: .85rem; }");
        s.AppendLine("</style></head><body>");

        s.AppendLine($"<h1>{Kacis(_metinler.Al("kurtarma.html.baslik"))}</h1>");
        s.AppendLine($"<p class=\"alt\">{Kacis(_metinler.Al("kurtarma.html.alt"))}</p>");

        s.AppendLine($"<div class=\"not\"><b>{Kacis(_metinler.Al("kurtarma.html.nasil"))}</b><ol>");

        // Adim 1-4 duz metin, 5. adimda komut var.
        for (var i = 1; i <= 4; i++)
            s.AppendLine($"<li>{_metinler.Al($"kurtarma.html.adim{i}")}</li>");

        s.AppendLine($"<li>{_metinler.Al("kurtarma.html.adim5")} "
                   + $"<span class=\"dosya\">D:</span> &rsaquo; "
                   + $"<span class=\"dosya\">cd {KlasorAdi}</span></li>");

        s.AppendLine($"<li>{_metinler.Al("kurtarma.html.adim6")}</li>");
        s.AppendLine("</ol>");
        s.AppendLine($"<p>{Kacis(_metinler.Al("kurtarma.html.harf"))}</p></div>");

        foreach (var arac in AraclariGetir())
        {
            s.AppendLine("<div class=\"arac\">");
            s.AppendLine($"<h2>{Kacis(arac.Ad)}</h2>");
            s.AppendLine($"<p>{Kacis(arac.NeZamanKullanilir)}</p>");
            s.AppendLine($"<p>{Kacis(_metinler.Al("kurtarma.html.yazilacak"))} "
                       + $"<span class=\"dosya\">{Kacis(arac.Kimlik)}.bat</span></p>");

            if (arac.DiskeYazar)
                s.AppendLine($"<p class=\"uyari\">{Kacis(_metinler.Al("kurtarma.html.uyari"))}</p>");

            s.AppendLine("</div>");
        }

        s.AppendLine($"<div class=\"not\"><b>{Kacis(_metinler.Al("kurtarma.html.son.baslik"))}</b><br>");
        s.AppendLine(_metinler.Al("kurtarma.html.son"));
        s.AppendLine("</div>");

        s.AppendLine("<footer>UWin · ugilabs.com</footer>");
        s.AppendLine("</body></html>");

        return s.ToString();
    }

    /// <summary>
    /// Yalnizca isaretlemeyi bozan karakterleri kacirir. WebUtility.HtmlEncode
    /// kullanilmaz: o, aksanli harfleri de sayisal varliga cevirir ve
    /// "Önyükleme" kaynakta "&#214;ny&#252;kleme" olarak durur. Sayfa zaten
    /// UTF-8 bildirdigi icin bu ne gerekli ne de okunabilir.
    /// </summary>
    private static string Kacis(string metin) => metin
        .Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal)
        .Replace(">", "&gt;", StringComparison.Ordinal)
        .Replace("\"", "&quot;", StringComparison.Ordinal);
}
