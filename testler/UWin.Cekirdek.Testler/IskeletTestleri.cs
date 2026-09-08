namespace UWin.Cekirdek.Testler;

public class IskeletTestleri
{
    [Fact]
    public void CekirdekProjesineReferansVerilebiliyor()
    {
        Assert.Equal("UWin.Cekirdek", typeof(UWin.Cekirdek.Isaret).Namespace);
    }

    [Fact]
    public void UrunKimligiDogru()
    {
        Assert.Equal("UWin", Isaret.UrunAdi);
        Assert.Equal("ugilabs", Isaret.Uretici);
        Assert.Equal("ugilabs.com", Isaret.Site);
    }
}
