using System.Net;
using System.Net.Http.Headers;

namespace UWin.Cekirdek.Testler.Sahteler;

/// <summary>
/// Bellek icindeki bir icerigi HTTP uzerinden sunar. Range destegi,
/// yarida kesme ve hata senaryolari icin ayarlanabilir.
/// </summary>
public sealed class SahteHttpIsleyici : HttpMessageHandler
{
    private readonly byte[] _icerik;
    private readonly int? _kesmeNoktasi;
    private int _istekSayisi;

    public int IstekSayisi => _istekSayisi;

    /// <param name="kesmeNoktasi">Verilirse ilk istek bu bayttan sonra kesilir.</param>
    public SahteHttpIsleyici(byte[] icerik, int? kesmeNoktasi = null)
    {
        _icerik = icerik;
        _kesmeNoktasi = kesmeNoktasi;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage istek, CancellationToken iptal)
    {
        var sira = Interlocked.Increment(ref _istekSayisi);
        var baslangic = (int)(istek.Headers.Range?.Ranges.FirstOrDefault()?.From ?? 0);

        var kalan = _icerik.Length - baslangic;
        var uzunluk = _kesmeNoktasi is int k && sira == 1
            ? Math.Min(k, kalan)
            : kalan;

        var parca = new byte[uzunluk];
        Array.Copy(_icerik, baslangic, parca, 0, uzunluk);

        var yanit = new HttpResponseMessage(baslangic > 0 ? HttpStatusCode.PartialContent : HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(parca)
        };

        // Gercek sunucu, govdeyi yarida kesse bile Content-Length'te dosyanin
        // gercek uzunlugunu bildirir. Sahte de oyle davranmali - aksi halde
        // istemci yarim dosyayi "tamamlandi" sanir ve devam etme yolu hic denenmez.
        yanit.Content.Headers.ContentLength = kalan;

        if (baslangic > 0)
        {
            // Gercek sunucular kismi yanitta toplam boyutu Content-Range ile bildirir.
            yanit.Content.Headers.ContentRange =
                new ContentRangeHeaderValue(baslangic, baslangic + uzunluk - 1, _icerik.Length);
        }
        else
        {
            yanit.Headers.AcceptRanges.Add("bytes");
        }

        return Task.FromResult(yanit);
    }
}
