namespace UWin.Cekirdek.Arayuzler;

/// <summary>WMI siniri. Testlerde sahtelenir, uretimde System.Management kullanir.</summary>
public interface IWmiOkuyucu
{
    IEnumerable<IReadOnlyDictionary<string, string>> Sorgula(string sinif, params string[] alanlar);
}
