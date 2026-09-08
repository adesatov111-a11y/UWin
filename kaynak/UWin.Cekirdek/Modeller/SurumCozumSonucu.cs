namespace UWin.Cekirdek.Modeller;

/// <summary>Indirme baglantisi cozme denemesinin sonucu.</summary>
public enum BaglantiDurumu
{
    Basarili,

    /// <summary>Microsoft istegi geri cevirdi. Genelde gecicidir.</summary>
    Reddedildi,

    /// <summary>
    /// IP adresi cok fazla indirme istegi nedeniyle engellenmis.
    /// Yeniden denemek fayda etmez; bir sure beklemek gerekir.
    /// </summary>
    AdresEngellendi,

    /// <summary>Uc noktaya ulasilamadi veya yanit anlasilamadi.</summary>
    Erisilemedi
}

/// <summary>Cozumleme sonucu: basariliysa surum bilgisi dolu gelir.</summary>
public sealed record SurumCozumSonucu(BaglantiDurumu Durum, WindowsSurumu? Surum)
{
    public bool Basarili => Durum == BaglantiDurumu.Basarili && Surum is not null;
}
