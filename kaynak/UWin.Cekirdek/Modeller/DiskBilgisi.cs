namespace UWin.Cekirdek.Modeller;

/// <summary>Tek bir fiziksel diskin kullaniciya gosterilecek tanimi.</summary>
public sealed record DiskBilgisi(
    int DiskNumarasi,
    string Ad,
    string Model,
    long BoyutBayt,
    DiskSinifi Sinif,
    string SeriNumarasi,
    long KullanilanBayt)
{
    /// <summary>
    /// Sistem diski hicbir kosulda hedef olamaz - teknisyen modunda dahi.
    /// Calisan sistemin diskini bicimlendirmek mesru bir islem degildir.
    /// </summary>
    public bool YazilabilirMi => Sinif != DiskSinifi.SistemDiski;
}
