using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Testler.Sahteler;

namespace UWin.Cekirdek.Testler;

/// <summary>Sahte disk, uzerine kurulacak tum yazma testlerinin temeli - once o dogrulanir.</summary>
public class SahteDiskErisimiTestleri
{
    private static HamDiskGirisi Usb(int no = 1) => new(
        DiskNumarasi: no, Model: "SanDisk Ultra", SeriNumarasi: "SN-USB-1",
        BoyutBayt: 32_000_000_000, CikarilabilirMi: true, VeriYolu: "USB",
        SistemDiskiMi: false, KullanilanBayt: 0);

    [Fact]
    public void EklenenDiskListelenir()
    {
        var sahte = new SahteDiskErisimi().DiskEkle(Usb());

        Assert.Single(sahte.DiskleriListele());
        Assert.Equal("SanDisk Ultra", sahte.DiskleriListele()[0].Model);
    }

    [Fact]
    public async Task CagrilarSirasiylaKaydedilir()
    {
        var sahte = new SahteDiskErisimi().DiskEkle(Usb());

        await sahte.BirimleriSokAsync(1);
        await sahte.BolumTablosuYazAsync(1, BolumTablosuTipi.Gpt);
        await sahte.BicimlendirAsync(1, "FAT32", "UWIN");

        Assert.Equal(
            ["BirimleriSok(1)", "BolumTablosuYaz(1, Gpt)", "Bicimlendir(1, FAT32, UWIN)"],
            sahte.Cagrilar);
    }

    [Fact]
    public async Task KilitBirakilincaAcilir()
    {
        var sahte = new SahteDiskErisimi().DiskEkle(Usb());

        var kilit = await sahte.DiskiKilitleAsync(1);
        Assert.True(sahte.KilitAcikMi);

        kilit.Dispose();
        Assert.False(sahte.KilitAcikMi);
    }

    [Fact]
    public async Task AyarlananCagriHataFirlatir()
    {
        var sahte = new SahteDiskErisimi().DiskEkle(Usb());
        sahte.BasarisizOlacakCagri = "Bicimlendir";

        await Assert.ThrowsAsync<InvalidOperationException>(() => sahte.BicimlendirAsync(1, "FAT32", "UWIN"));
    }

    [Fact]
    public void CikarilanDiskListedenDuser()
    {
        var sahte = new SahteDiskErisimi().DiskEkle(Usb());

        sahte.DiskiCikar(1);

        Assert.Empty(sahte.DiskleriListele());
    }

    [Fact]
    public async Task KopyalananDosyalarKaydedilir()
    {
        var sahte = new SahteDiskErisimi().DiskEkle(Usb());

        await sahte.DosyaKopyalaAsync(1, "C:/kaynak/bootmgr", "bootmgr");

        Assert.Equal("C:/kaynak/bootmgr", sahte.YazilanDosyalar["bootmgr"]);
    }
}
