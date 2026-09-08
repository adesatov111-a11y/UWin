using UWin.Cekirdek.Modeller;
using UWin.Cekirdek.Servisler;
using UWin.Cekirdek.Testler.Sahteler;

namespace UWin.Cekirdek.Testler;

public class DiskServisiTestleri
{
    private static HamDiskGirisi Ham(
        int no, bool cikarilabilir, string veriYolu, bool sistemMi,
        string model = "Disk", long boyut = 32_000_000_000)
        => new(no, model, $"SN-{no}", boyut, cikarilabilir, veriYolu, sistemMi, KullanilanBayt: 0);

    /// <summary>Tipik makine: sistem NVMe, bir USB bellek, bir harici HDD, bir ikincil dahili disk.</summary>
    private static SahteDiskErisimi TipikMakine() => new SahteDiskErisimi()
        .DiskEkle(Ham(0, cikarilabilir: false, "NVMe", sistemMi: true, "Samsung 980", 512_000_000_000))
        .DiskEkle(Ham(1, cikarilabilir: true, "USB", sistemMi: false, "SanDisk Ultra"))
        .DiskEkle(Ham(2, cikarilabilir: false, "USB", sistemMi: false, "WD Elements", 2_000_000_000_000))
        .DiskEkle(Ham(3, cikarilabilir: false, "SATA", sistemMi: false, "Seagate Barracuda", 1_000_000_000_000));

    [Fact]
    public void VarsayilanModdaSadeceUsbBellekGorunur()
    {
        var diskler = new DiskServisi(TipikMakine()).DiskleriListele();

        Assert.Single(diskler);
        Assert.Equal(DiskSinifi.UsbBellek, diskler[0].Sinif);
        Assert.Equal("SanDisk Ultra", diskler[0].Model);
    }

    [Fact]
    public void GelismisModdaSistemDiskiDahilHepsiListelenir()
    {
        var diskler = new DiskServisi(TipikMakine()).DiskleriListele(gelismisMod: true);

        Assert.Equal(4, diskler.Count);
    }

    [Fact]
    public void SistemDiskiGelismisModdaBileYazilamaz()
    {
        var sistem = new DiskServisi(TipikMakine())
            .DiskleriListele(gelismisMod: true)
            .Single(d => d.Sinif == DiskSinifi.SistemDiski);

        Assert.False(sistem.YazilabilirMi);
    }

    [Theory]
    [InlineData(1, DiskSinifi.UsbBellek)]
    [InlineData(2, DiskSinifi.HariciDisk)]
    [InlineData(3, DiskSinifi.DahiliDisk)]
    [InlineData(0, DiskSinifi.SistemDiski)]
    public void DiskleriDogruSiniflandirir(int diskNumarasi, DiskSinifi beklenen)
    {
        var disk = new DiskServisi(TipikMakine())
            .DiskleriListele(gelismisMod: true)
            .Single(d => d.DiskNumarasi == diskNumarasi);

        Assert.Equal(beklenen, disk.Sinif);
    }

    [Fact]
    public void CikarilabilirSistemDiskiYineSistemDiskiSayilir()
    {
        // Sistem diski kontrolu her zaman once gelir - cikarilabilir olsa bile.
        var sahte = new SahteDiskErisimi()
            .DiskEkle(Ham(0, cikarilabilir: true, "USB", sistemMi: true, "Tuhaf Sistem"));

        var disk = new DiskServisi(sahte).DiskleriListele(gelismisMod: true).Single();

        Assert.Equal(DiskSinifi.SistemDiski, disk.Sinif);
        Assert.False(disk.YazilabilirMi);
    }

    [Fact]
    public void HedefDogrulamasiDiskCikarilincaBasarisiz()
    {
        var sahte = TipikMakine();
        var servis = new DiskServisi(sahte);
        var secilen = servis.DiskleriListele().Single();

        sahte.DiskiCikar(1);

        Assert.False(servis.HedefHalaGecerliMi(secilen));
    }

    [Fact]
    public void HedefDegismedigindeDogrulamaBasarili()
    {
        var servis = new DiskServisi(TipikMakine());
        var secilen = servis.DiskleriListele().Single();

        Assert.True(servis.HedefHalaGecerliMi(secilen));
    }

    [Fact]
    public void BaskaAygitTakilirsaDogrulamaBasarisiz()
    {
        var sahte = TipikMakine();
        var servis = new DiskServisi(sahte);
        var secilen = servis.DiskleriListele().Single();

        // Ayni disk numarasi, farkli aygit: kullanici USB'yi degistirmis.
        sahte.DiskiCikar(1);
        sahte.DiskEkle(Ham(1, cikarilabilir: true, "USB", sistemMi: false, "Kingston", 16_000_000_000));

        Assert.False(servis.HedefHalaGecerliMi(secilen));
    }

    [Theory]
    [InlineData(32_000_000_000, DiskSinifi.UsbBellek)]
    [InlineData(128_000_000_000, DiskSinifi.UsbBellek)]
    [InlineData(1_000_200_000_000, DiskSinifi.HariciDisk)]
    [InlineData(2_000_000_000_000, DiskSinifi.HariciDisk)]
    public void BuyukCikarilabilirAygitHariciDiskSayilir(long boyut, DiskSinifi beklenen)
    {
        // WMI, USB harici HDD'leri de "Removable Media" bildirir. Boyut esigi
        // olmadan 1 TB'lik bir arsiv diski varsayilan listeye girer ve
        // kullanici onu ad yazma kilidi olmadan secebilirdi.
        var sahte = new SahteDiskErisimi()
            .DiskEkle(Ham(1, cikarilabilir: true, "USB", sistemMi: false, "WD Elements", boyut));

        var disk = new DiskServisi(sahte).DiskleriListele(gelismisMod: true).Single();

        Assert.Equal(beklenen, disk.Sinif);
    }

    [Fact]
    public void BuyukHariciDiskVarsayilanListedeGorunmez()
    {
        var sahte = new SahteDiskErisimi()
            .DiskEkle(Ham(1, cikarilabilir: true, "USB", sistemMi: false, "WD Elements", 1_000_200_000_000));

        Assert.Empty(new DiskServisi(sahte).DiskleriListele());
    }

    [Fact]
    public void BulunamayanDiskNullDoner()
    {
        Assert.Null(new DiskServisi(TipikMakine()).DiskBul(99));
    }
}
