using System.Text.Json;
using UWin.Cekirdek.Arayuzler;
using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Servisler;

/// <summary>Yarim kalmis yazmayi USB'deki gizli bir isaret dosyasiyla izler.</summary>
public sealed class DurumIsaretleyici : IDurumIsaretleyici
{
    private const string IsaretDosyaAdi = ".uwin-yarim";

    public async Task IsaretBirakAsync(
        string surucuYolu, YazmaDurumIsareti isaret, CancellationToken iptal = default)
    {
        var yol = Path.Combine(surucuYolu, IsaretDosyaAdi);

        Directory.CreateDirectory(surucuYolu);
        await File.WriteAllTextAsync(yol, JsonSerializer.Serialize(isaret), iptal);
        File.SetAttributes(yol, FileAttributes.Hidden);
    }

    public async Task<YazmaDurumIsareti?> IsaretOkuAsync(string surucuYolu, CancellationToken iptal = default)
    {
        var yol = Path.Combine(surucuYolu, IsaretDosyaAdi);

        if (!File.Exists(yol))
            return null;

        try
        {
            var metin = await File.ReadAllTextAsync(yol, iptal);
            return JsonSerializer.Deserialize<YazmaDurumIsareti>(metin);
        }
        catch (Exception e) when (e is JsonException or IOException or UnauthorizedAccessException)
        {
            // Bozuk veya okunamayan isaret "isaret yok" sayilir:
            // kullaniciyi bir hata ekraniyla durdurmanin degeri yok.
            return null;
        }
    }

    public Task IsaretSilAsync(string surucuYolu, CancellationToken iptal = default)
    {
        var yol = Path.Combine(surucuYolu, IsaretDosyaAdi);

        try
        {
            if (File.Exists(yol))
            {
                File.SetAttributes(yol, FileAttributes.Normal);
                File.Delete(yol);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Silinemezse USB bir sonraki acilista "yarim" gorunur.
            // Bu guvenli taraftir: gereksiz bir onarim teklifi sunmak,
            // bozuk bir USB'yi saglam sanmasindan iyidir.
        }

        return Task.CompletedTask;
    }
}
