namespace UWin.Cekirdek.Modeller;

/// <summary>Yazma isleminin sabit adim sirasi. Arayuz bunu kullaniciya adim adim gosterir.</summary>
public enum YazmaAdimi
{
    Dogrulama,
    Kilitleme,
    BirimSokme,
    BolumTablosu,
    Bicimlendirme,
    DosyaKopyalama,
    WimBolme,
    BootYazma,

    /// <summary>Yazilan dosyalar USB'den geri okunup karsilastirilir.</summary>
    GeriOkuma,

    Tamamlandi
}
