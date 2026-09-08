namespace UWin.Cekirdek.Modeller;

/// <summary>Diskin risk sinifi. Gorunurluk ve yazilabilirlik bundan turer.</summary>
public enum DiskSinifi
{
    /// <summary>Cikarilabilir USB bellek. Varsayilan olarak listelenir.</summary>
    UsbBellek,

    /// <summary>USB/Thunderbolt uzerinden bagli sabit disk. Gelismis modda gorunur.</summary>
    HariciDisk,

    /// <summary>SATA/NVMe ic veri yolundaki sabit disk. Gelismis modda gorunur.</summary>
    DahiliDisk,

    /// <summary>Windows'un kurulu oldugu disk. Hicbir modda yazilamaz.</summary>
    SistemDiski
}
