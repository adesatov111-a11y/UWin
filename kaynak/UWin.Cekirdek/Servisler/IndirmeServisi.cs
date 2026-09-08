using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>
/// ISO indirir. Kesintide Range basligiyla kaldigi yerden devam eder,
/// bitince SHA-256 dogrular. Ozet uyusmazsa dosyayi siler.
/// </summary>
public sealed class IndirmeServisi : IIndirmeServisi
{
    private const int TamponBoyutu = 81_920;
    private const int AzamiDeneme = 5;

    private readonly HttpClient _http;
    private readonly IMetinSaglayici _metinler;

    /// <param name="metinler">
    /// Hata mesajlari buradan gelir; verilmezse varsayilan saglayici
    /// kullanilir.
    /// </param>
    public IndirmeServisi(HttpClient http, IMetinSaglayici? metinler = null)
    {
        _http = http;
        _metinler = metinler ?? new MetinSaglayici();
    }

    /// <param name="yarimDosyayiKoru">
    /// true ise iptal/kesinti sonrasi indirilen kisim diskte kalir ve sonraki
    /// deneme kaldigi yerden devam eder. false ise yarim dosya silinir -
    /// kullanici islemi bilerek iptal ettiginde disk kirletilmez.
    /// </param>
    public async Task<IndirmeSonucu> IndirAsync(
        string baglanti,
        string hedefYol,
        string? beklenenSha256,
        IProgress<IndirmeIlerlemesi>? ilerleme = null,
        CancellationToken iptal = default,
        bool yarimDosyayiKoru = true)
    {
        var geciciYol = hedefYol + ".indiriliyor";

        try
        {
            await ParcaliIndirAsync(baglanti, geciciYol, ilerleme, iptal);

            var ozet = await Sha256HesaplaAsync(geciciYol, null, iptal);

            if (beklenenSha256 is not null &&
                !ozet.Equals(beklenenSha256, StringComparison.OrdinalIgnoreCase))
            {
                Sil(geciciYol);

                return new IndirmeSonucu(false, null, null, new UWinHatasi(
                    NeOldu: _metinler.Al("indirme.hata.bozuk"),
                    Neden: _metinler.Al("indirme.hata.bozuk.neden"),
                    NeYapmali: _metinler.Al("indirme.hata.bozuk.yapmali")));
            }

            File.Move(geciciYol, hedefYol, overwrite: true);
            return new IndirmeSonucu(true, hedefYol, ozet, null);
        }
        catch (OperationCanceledException)
        {
            if (!yarimDosyayiKoru)
                Sil(geciciYol);

            return new IndirmeSonucu(false, null, null, new UWinHatasi(
                NeOldu: _metinler.Al("indirme.hata.iptal"),
                Neden: _metinler.Al("indirme.hata.iptal.neden"),
                NeYapmali: _metinler.Al(yarimDosyayiKoru
                    ? "indirme.hata.iptal.devam"
                    : "indirme.hata.iptal.silindi")));
        }
        catch (Exception e)
        {
            return new IndirmeSonucu(false, null, null, new UWinHatasi(
                NeOldu: _metinler.Al("indirme.hata.genel"),
                Neden: _metinler.Al("indirme.hata.genel.neden"),
                NeYapmali: _metinler.Al("indirme.hata.genel.yapmali"),
                TeknikAyrinti: e.Message));
        }
    }

    /// <summary>Kesintide Range basligiyla kaldigi yerden devam ederek indirir.</summary>
    private async Task ParcaliIndirAsync(
        string baglanti,
        string geciciYol,
        IProgress<IndirmeIlerlemesi>? ilerleme,
        CancellationToken iptal)
    {
        long toplam = 0;

        for (var deneme = 0; deneme < AzamiDeneme; deneme++)
        {
            iptal.ThrowIfCancellationRequested();

            var mevcut = File.Exists(geciciYol) ? new FileInfo(geciciYol).Length : 0;

            using var istek = new HttpRequestMessage(HttpMethod.Get, baglanti);
            if (mevcut > 0)
                istek.Headers.Range = new RangeHeaderValue(mevcut, null);

            using var yanit = await _http.SendAsync(istek, HttpCompletionOption.ResponseHeadersRead, iptal);
            yanit.EnsureSuccessStatusCode();

            // Toplam boyut: kismi yanitta Content-Range'in tamami, tam yanitta
            // Content-Length. Kesilen bir yanitin ContentLength'i yalnizca o
            // parcanin uzunlugudur - ona guvenmek dosyayi erken tamamlanmis sayar.
            var bildirilenToplam = yanit.Content.Headers.ContentRange?.Length
                                   ?? mevcut + (yanit.Content.Headers.ContentLength ?? 0);

            if (bildirilenToplam > toplam)
                toplam = bildirilenToplam;

            await using (var kaynak = await yanit.Content.ReadAsStreamAsync(iptal))
            await using (var hedef = new FileStream(
                geciciYol, FileMode.Append, FileAccess.Write, FileShare.None, TamponBoyutu, useAsync: true))
            {
                var tampon = new byte[TamponBoyutu];
                var indirilen = mevcut;
                var kronometre = Stopwatch.StartNew();
                int okunan;

                while ((okunan = await kaynak.ReadAsync(tampon, iptal)) > 0)
                {
                    await hedef.WriteAsync(tampon.AsMemory(0, okunan), iptal);
                    indirilen += okunan;

                    var hiz = kronometre.Elapsed.TotalSeconds > 0
                        ? (indirilen - mevcut) / kronometre.Elapsed.TotalSeconds
                        : 0;

                    ilerleme?.Report(new IndirmeIlerlemesi(indirilen, toplam, hiz));
                }

                await hedef.FlushAsync(iptal);
            }

            // Dosya tamamlandiysa dongu biter; eksikse bir sonraki deneme
            // Range basligiyla kaldigi yerden devam eder.
            if (toplam > 0 && new FileInfo(geciciYol).Length >= toplam)
                return;
        }

        throw new IOException("Download failed: retry limit exceeded.");
    }

    public async Task<string> Sha256HesaplaAsync(
        string dosyaYolu,
        IProgress<double>? ilerleme = null,
        CancellationToken iptal = default)
    {
        await using var akis = new FileStream(
            dosyaYolu, FileMode.Open, FileAccess.Read, FileShare.Read, TamponBoyutu, useAsync: true);

        using var sha = SHA256.Create();

        var tampon = new byte[TamponBoyutu];
        var toplam = akis.Length;
        long okunanToplam = 0;
        int okunan;

        while ((okunan = await akis.ReadAsync(tampon, iptal)) > 0)
        {
            sha.TransformBlock(tampon, 0, okunan, null, 0);
            okunanToplam += okunan;

            if (toplam > 0)
                ilerleme?.Report(okunanToplam * 100.0 / toplam);
        }

        sha.TransformFinalBlock([], 0, 0);
        return Convert.ToHexString(sha.Hash!).ToLowerInvariant();
    }

    private static void Sil(string yol)
    {
        try
        {
            if (File.Exists(yol))
                File.Delete(yol);
        }
        catch (IOException)
        {
            // Dosya kilitliyse silinemez; bu durumda gecici dosya diskte kalir
            // ama hedef yola tasinmadigi icin kullaniciya bozuk ISO sunulmaz.
        }
    }
}
