using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

/// <summary>Donanim raporunu Windows 11 karnesine cevirir. Salt okuma.</summary>
public interface IUyumlulukServisi
{
    UyumlulukRaporu Degerlendir(DonanimRaporu donanim);
}
