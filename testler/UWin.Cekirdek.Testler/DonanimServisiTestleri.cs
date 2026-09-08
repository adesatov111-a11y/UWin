using UWin.Cekirdek.Servisler;
using UWin.Cekirdek.Testler.Sahteler;

namespace UWin.Cekirdek.Testler;

public class DonanimServisiTestleri
{
    /// <summary>Win11'e uygun tipik bir makine: MSI B550, TPM 2.0, UEFI, 16 GB.</summary>
    private static SahteWmiOkuyucu UyumluMakine() => new SahteWmiOkuyucu()
        .Ekle("Win32_BaseBoard", new() { ["Manufacturer"] = "Micro-Star International Co., Ltd.", ["Product"] = "B550-A PRO" })
        .Ekle("Win32_Processor", new() { ["Name"] = "AMD Ryzen 5 5600" })
        .Ekle("Win32_ComputerSystem", new() { ["TotalPhysicalMemory"] = "17179869184" })
        .Ekle("Win32_Tpm", new() { ["SpecVersion"] = "2.0, 0, 1.38" })
        .Ekle("Win32_DiskDrive", new() { ["Size"] = "512110190592", ["Index"] = "0" });

    [Fact]
    public async Task UyumluMakineWindows11Kurulabilir()
    {
        var servis = new DonanimServisi(UyumluMakine(), uefiMi: true, secureBootDestekli: true);

        var rapor = await servis.RaporAlAsync();

        Assert.True(rapor.Windows11Uyumlu);
        Assert.Empty(rapor.UyumsuzlukNedenleri);
        Assert.Equal("MSI", rapor.AnakartUretici);
        Assert.Equal("B550-A PRO", rapor.AnakartModel);
    }

    [Fact]
    public async Task TpmYoksaUyumsuzVeNedenBildirilir()
    {
        var wmi = new SahteWmiOkuyucu()
            .Ekle("Win32_BaseBoard", new() { ["Manufacturer"] = "ASUSTeK COMPUTER INC.", ["Product"] = "PRIME B450M" })
            .Ekle("Win32_Processor", new() { ["Name"] = "AMD Ryzen 5 2600" })
            .Ekle("Win32_ComputerSystem", new() { ["TotalPhysicalMemory"] = "8589934592" })
            .Ekle("Win32_DiskDrive", new() { ["Size"] = "256060514304", ["Index"] = "0" });

        var rapor = await new DonanimServisi(wmi, uefiMi: true, secureBootDestekli: true).RaporAlAsync();

        Assert.False(rapor.Windows11Uyumlu);
        Assert.Contains(rapor.UyumsuzlukNedenleri, n => n.Contains("TPM"));
        Assert.Equal("ASUS", rapor.AnakartUretici);
    }

    [Fact]
    public async Task LegacyBiosVeAzRamIkiNedenBildirir()
    {
        var wmi = new SahteWmiOkuyucu()
            .Ekle("Win32_BaseBoard", new() { ["Manufacturer"] = "Gigabyte Technology Co., Ltd.", ["Product"] = "H61M-S1" })
            .Ekle("Win32_Processor", new() { ["Name"] = "Intel Core i3-2100" })
            .Ekle("Win32_ComputerSystem", new() { ["TotalPhysicalMemory"] = "2147483648" })
            .Ekle("Win32_DiskDrive", new() { ["Size"] = "500107862016", ["Index"] = "0" });

        var rapor = await new DonanimServisi(wmi, uefiMi: false, secureBootDestekli: false).RaporAlAsync();

        Assert.False(rapor.Windows11Uyumlu);
        Assert.Contains(rapor.UyumsuzlukNedenleri, n => n.Contains("UEFI"));
        Assert.Contains(rapor.UyumsuzlukNedenleri, n => n.Contains("RAM"));
        Assert.Equal("Gigabyte", rapor.AnakartUretici);
    }

    [Fact]
    public async Task BilinmeyenUreticiHamDegerleKalir()
    {
        var wmi = new SahteWmiOkuyucu()
            .Ekle("Win32_BaseBoard", new() { ["Manufacturer"] = "Tuhaf Marka A.S.", ["Product"] = "XYZ" })
            .Ekle("Win32_Processor", new() { ["Name"] = "Intel Core i5" })
            .Ekle("Win32_ComputerSystem", new() { ["TotalPhysicalMemory"] = "17179869184" })
            .Ekle("Win32_Tpm", new() { ["SpecVersion"] = "2.0" })
            .Ekle("Win32_DiskDrive", new() { ["Size"] = "512110190592", ["Index"] = "0" });

        var rapor = await new DonanimServisi(wmi, uefiMi: true, secureBootDestekli: true).RaporAlAsync();

        Assert.Equal("Tuhaf Marka A.S.", rapor.AnakartUretici);
    }

    [Fact]
    public async Task WmiHicVeriDondurmezseServisCokmez()
    {
        var rapor = await new DonanimServisi(new SahteWmiOkuyucu(), uefiMi: true, secureBootDestekli: true)
            .RaporAlAsync();

        Assert.False(rapor.Windows11Uyumlu);
        Assert.NotEmpty(rapor.AnakartUretici);
    }
}
