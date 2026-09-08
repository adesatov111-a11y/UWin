namespace UWin.Cekirdek.Modeller;

/// <summary>Geri kazanma isleminin sabit adim sirasi.</summary>
public enum GeriKazanmaAdimi
{
    Dogrulama,
    Kilitleme,
    BolumTablosu,
    Bicimlendirme,
    Tamamlandi
}

/// <summary>Geri kazanma sirasinda arayuze bildirilen anlik durum.</summary>
public sealed record GeriKazanmaIlerlemesi(
    GeriKazanmaAdimi Adim,
    double ToplamYuzde,
    string Aciklama);
