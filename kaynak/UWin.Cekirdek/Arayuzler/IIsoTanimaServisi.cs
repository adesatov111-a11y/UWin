using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

/// <summary>ISO'nun icine bakarak hangi Windows oldugunu soyler.</summary>
public interface IIsoTanimaServisi
{
    /// <summary>Dosyayi acar, kurulum dosyalarini arar. Hata firlatmaz; durumu sonucta doner.</summary>
    IsoKimligi Tani(string isoYolu);
}
