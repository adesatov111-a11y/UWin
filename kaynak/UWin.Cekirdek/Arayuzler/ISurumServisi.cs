using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

public interface ISurumServisi
{
    /// <summary>Gomulu katalogdan tum surum/dil/mimari kombinasyonlarini listeler. Internet gerektirmez.</summary>
    Task<IReadOnlyList<WindowsSurumu>> SurumleriGetirAsync(CancellationToken iptal = default);

    /// <summary>
    /// Canli indirme baglantisini cozer. Basarisizlik beklenen bir durumdur;
    /// sonuc neden basarisiz oldugunu da tasir, boylece kullaniciya dogru
    /// aciklama gosterilebilir.
    /// </summary>
    Task<SurumCozumSonucu> BaglantiCozAsync(
        string kimlik, string dil, string mimari, CancellationToken iptal = default);

    /// <summary>Verilen SHA-256 ozeti bilinen bir Microsoft dosyasina aitse surum adini doner.</summary>
    string? OzetIleTani(string sha256);
}
