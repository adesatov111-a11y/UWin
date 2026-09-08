using UWin.Cekirdek.Servisler;
using UWin.Cekirdek.Yerel;

namespace UWin.Cekirdek.Testler;

/// <summary>
/// Yalnizca salt okuma yollarini dogrular. Yikici yollar (bolumleme,
/// bicimlendirme, boot yazma) gercek disk gerektirdigi icin burada
/// test edilmez - onlarin dogrulamasi manuel donanim listesindedir.
/// </summary>
public class Win32DiskErisimiTestleri
{
    [Fact]
    public void DiskleriListeleGercekSistemdeCokmez()
    {
        var diskler = new Win32DiskErisimi().DiskleriListele();

        Assert.NotNull(diskler);
    }

    [Fact]
    public void ListelenenHerDiskGecerliAlanlarTasir()
    {
        var diskler = new Win32DiskErisimi().DiskleriListele();

        Assert.All(diskler, d =>
        {
            Assert.True(d.DiskNumarasi >= 0);
            Assert.True(d.BoyutBayt > 0);
            Assert.NotNull(d.Model);
            Assert.NotNull(d.VeriYolu);
        });
    }

    [Fact]
    public void CalisanMakinedeSistemDiskiIsaretlenir()
    {
        var diskler = new Win32DiskErisimi().DiskleriListele();

        // Calisan bir Windows makinesinde en az bir sistem diski olmali.
        if (diskler.Count > 0)
            Assert.Contains(diskler, d => d.SistemDiskiMi);
    }

    [Fact]
    public void SistemDiskiVarsayilanListedeGorunmez()
    {
        // En kritik guvenlik kapisi, gercek donanim uzerinde dogrulanir.
        var servis = new DiskServisi(new Win32DiskErisimi());

        var varsayilan = servis.DiskleriListele();

        Assert.DoesNotContain(varsayilan, d => !d.YazilabilirMi);
    }
}
