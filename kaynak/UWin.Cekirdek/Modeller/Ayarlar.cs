using System.Text.Json.Serialization;

namespace UWin.Cekirdek.Modeller;

/// <summary>
/// Kalici kullanici tercihleri.
/// <see cref="Dil"/> null ise kullanici henuz secim yapmamistir.
/// </summary>
public sealed record Ayarlar(string? Dil)
{
    /// <summary>
    /// Acilista dil secim ekrani gosterilmeli mi.
    ///
    /// Dosyaya yazilmaz: Dil alanindan hesaplanir ve iki yerde birden
    /// tutulan bir gercek, er ya da gec birbiriyle celisir.
    /// </summary>
    [JsonIgnore]
    public bool DilSorulmali => Dil is null;
}
