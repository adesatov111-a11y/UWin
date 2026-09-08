using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

/// <summary>
/// Win32 disk islemlerinin siniri. Sistemdeki TUM yikici islemler bu
/// arayuzden gecer; testlerde bellek ici sahte uygulama kullanilir,
/// boylece hicbir test gercek disk silmez.
/// </summary>
public interface IYerelDiskErisimi
{
    /// <summary>Sistemdeki fiziksel diskleri ham haliyle listeler. Salt okuma.</summary>
    IReadOnlyList<HamDiskGirisi> DiskleriListele();

    /// <summary>Bicimlendirilmis diskin surucu yolunu doner (or. "E:\"). Bulunamazsa null.</summary>
    string? SurucuYoluBul(int diskNumarasi);

    /// <summary>Diski ozel erisime alir. Donen nesne birakildiginda kilit acilir.</summary>
    Task<IDisposable> DiskiKilitleAsync(int diskNumarasi, CancellationToken iptal = default);

    /// <summary>Diske bagli birimleri sistemden sokerek yazmaya hazirlar.</summary>
    Task BirimleriSokAsync(int diskNumarasi, CancellationToken iptal = default);

    /// <summary>Diski sifirlar ve verilen tipte yeni bolum tablosu yazar. YIKICI.</summary>
    Task BolumTablosuYazAsync(int diskNumarasi, BolumTablosuTipi tip, CancellationToken iptal = default);

    /// <summary>Diskteki bolumu bicimlendirir. YIKICI.</summary>
    Task BicimlendirAsync(int diskNumarasi, string dosyaSistemi, string etiket, CancellationToken iptal = default);

    /// <summary>Tek bir dosyayi diskteki goreli yola kopyalar.</summary>
    Task DosyaKopyalaAsync(
        int diskNumarasi,
        string kaynakYol,
        string hedefGoreliYol,
        IProgress<double>? ilerleme = null,
        CancellationToken iptal = default);

    /// <summary>Diski onyuklenebilir yapan boot kayitlarini yazar.</summary>
    Task BootYazAsync(int diskNumarasi, BolumTablosuTipi tip, CancellationToken iptal = default);
}
