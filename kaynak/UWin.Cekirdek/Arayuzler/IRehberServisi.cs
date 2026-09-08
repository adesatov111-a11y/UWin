using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

public interface IRehberServisi
{
    /// <summary>Markaya ozel rehberi doner; marka taninmazsa genel rehbere duser.</summary>
    BiosRehberi RehberGetir(string anakartUretici);

    /// <summary>Rehberi USB'ye yazilacak tek dosyalik HTML olarak uretir.</summary>
    string HtmlUret(BiosRehberi rehber, DonanimRaporu donanim);
}
