namespace UWin.Cekirdek.Modeller;

/// <summary>Indirme sirasinda arayuze bildirilen anlik durum.</summary>
public sealed record IndirmeIlerlemesi(long IndirilenBayt, long ToplamBayt, double BaytBolumSaniye)
{
    public double Yuzde => ToplamBayt <= 0
        ? 0
        : Math.Clamp(IndirilenBayt * 100.0 / ToplamBayt, 0, 100);

    public TimeSpan? KalanSure => BaytBolumSaniye <= 0 || ToplamBayt <= 0
        ? null
        : TimeSpan.FromSeconds((ToplamBayt - IndirilenBayt) / BaytBolumSaniye);
}
