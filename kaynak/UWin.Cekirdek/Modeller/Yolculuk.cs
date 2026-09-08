namespace UWin.Cekirdek.Modeller;

/// <summary>Yolculugun bir bolumu: BIOS, kurulum veya kurulum sonrasi.</summary>
public sealed record YolculukBolumu(string Baslik, IReadOnlyList<RehberAdimi> Adimlar);

/// <summary>
/// USB hazir olduktan sonra kullaniciyi bekleyen butun yol.
///
/// Kullanici bu ozeti USB hazirlamadan once de gorebildigi icin
/// donanim raporu olmadan da uretilebilir; o durumda Makine null olur
/// ve adimlar genel rehberden gelir.
/// </summary>
public sealed record Yolculuk(
    string? Makine,
    string GirisTusu,
    string? BootMenuTusu,
    string? SecureBootNotu,
    IReadOnlyList<YolculukBolumu> Bolumler);
