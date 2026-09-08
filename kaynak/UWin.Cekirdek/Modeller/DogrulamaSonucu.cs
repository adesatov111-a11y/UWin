namespace UWin.Cekirdek.Modeller;

/// <summary>Tek bir dosyanin USB'deki hali ile ISO'daki hali karsilastirmasi.</summary>
public enum DosyaDogrulamaDurumu
{
    Eslesti,

    /// <summary>Dosya USB'de hic yok.</summary>
    Eksik,

    /// <summary>Dosya var ama boyutu tutmuyor - kopyalama yarim kalmis.</summary>
    BoyutTutmuyor,

    /// <summary>Boyut ayni ama icerik bozuk - sessiz yazma hatasi.</summary>
    IcerikBozuk
}

public sealed record DosyaDogrulamasi(string GoreliYol, DosyaDogrulamaDurumu Durum);

/// <summary>
/// Yazma sonrasi geri okuma raporu.
///
/// Ucuz USB'lerin en sinsi hatasi sessiz yazmadir: dosya yaziliyor,
/// hicbir hata donmuyor, ama geri okununca icerik farkli cikiyor. Hata
/// ancak BIOS'ta "no bootable device" olarak goruluyor ve kullanici
/// sebebini asla bulamiyor. Bu yuzden yazma bitince geri okunur.
/// </summary>
public sealed record DogrulamaSonucu(
    IReadOnlyList<DosyaDogrulamasi> Dosyalar,
    bool Tamamlanabildi = true)
{
    public IReadOnlyList<DosyaDogrulamasi> Sorunlular
        => Dosyalar.Where(d => d.Durum != DosyaDogrulamaDurumu.Eslesti).ToList();

    public bool Basarili => Tamamlanabildi && Sorunlular.Count == 0;
}
