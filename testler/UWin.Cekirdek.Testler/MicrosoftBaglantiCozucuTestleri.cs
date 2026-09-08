using UWin.Cekirdek.Servisler;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Koruma betiginden okunan degerlerin desenleri, uc noktanin en kirilgan
/// parcasidir. Bu testler gercek bir mdt.js ciktisi uzerinde calisir;
/// Microsoft bicimi degistirirse burada kirilir ve neyi duzeltecegimizi bilir.
/// </summary>
public class MicrosoftBaglantiCozucuTestleri
{
    /// <summary>2026-09 tarihinde ov-df.microsoft.com'dan alinmis gercek yanit.</summary>
    private const string GercekBetik =
        """
        function SendBack(url,callback){callback(url)}window.dfp={url:"https://ov-df.microsoft.com/?session_id=5a034b08-bd86-4c22-8457-8f5b75dabeee&CustomerId=560dc9f3-1aa5-4a2f-b63c-9e18f8d0e175&PageId=si&w=8DF0C7F8F29BAB7",sessionId:"5a034b08-bd86-4c22-8457-8f5b75dabeee",customerId:"560dc9f3-1aa5-4a2f-b63c-9e18f8d0e175",dc:"westeurope"};window.dfp.doFpt=function(doc){var start,frm,src;if(true){start=Date.now();frm=doc.createElement("IFRAME");src="https://ov-df.microsoft.com/?session_id=5a034b08-bd86-4c22-8457-8f5b75dabeee&CustomerId=560dc9f3-1aa5-4a2f-b63c-9e18f8d0e175&PageId=si&w=8DF0C7F8F29BAB7";src+="&mdt="+start;src+="&rticks="+1788744611215;}};
        """;

    [Fact]
    public void WDegeriBetiktenOkunur()
    {
        var eslesme = MicrosoftBaglantiCozucu.WDegeriDeseni.Match(GercekBetik);

        Assert.True(eslesme.Success);
        Assert.Equal("8DF0C7F8F29BAB7", eslesme.Groups[1].Value);
    }

    [Fact]
    public void RticksBetiktenOkunur()
    {
        // Betikte JS birlestirmesi olarak gelir: src+="&rticks="+1788744611215;
        var eslesme = MicrosoftBaglantiCozucu.RticksDeseni.Match(GercekBetik);

        Assert.True(eslesme.Success);
        Assert.Equal("1788744611215", eslesme.Groups[1].Value);
    }

    [Fact]
    public void BosBetikteDesenlerEslesmez()
    {
        Assert.False(MicrosoftBaglantiCozucu.WDegeriDeseni.IsMatch("hicbir sey"));
        Assert.False(MicrosoftBaglantiCozucu.RticksDeseni.IsMatch("hicbir sey"));
    }

    [Fact]
    public async Task ErisilemeyenUcNoktaBasarisizDoner()
    {
        // Uc nokta cevap vermezse cozucu cokmemeli, durumu bildirmeli.
        using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(1) };

        var sonuc = await new MicrosoftBaglantiCozucu(http).CozAsync(3321, "tr-TR");

        Assert.NotEqual(MicrosoftBaglantiCozucu.CozumDurumu.Basarili, sonuc.Durum);
        Assert.Null(sonuc.Baglanti);
    }
}
