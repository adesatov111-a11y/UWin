using UWin.Cekirdek.Arayuzler;

namespace UWin.Cekirdek.Testler.Sahteler;

/// <summary>Kaydedilmis WMI ciktilarini donduren test ikizi.</summary>
public sealed class SahteWmiOkuyucu : IWmiOkuyucu
{
    private readonly Dictionary<string, List<Dictionary<string, string>>> _veri = new();

    public SahteWmiOkuyucu Ekle(string sinif, Dictionary<string, string> satir)
    {
        if (!_veri.TryGetValue(sinif, out var liste))
        {
            liste = [];
            _veri[sinif] = liste;
        }

        liste.Add(satir);
        return this;
    }

    public IEnumerable<IReadOnlyDictionary<string, string>> Sorgula(string sinif, params string[] alanlar)
        => _veri.TryGetValue(sinif, out var liste)
            ? liste
            : [];
}
