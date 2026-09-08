using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

public interface IYolculukServisi
{
    /// <summary>
    /// USB sonrasi yolun tamamini doner. Donanim raporu yoksa genel
    /// yonergeler uretilir - kullanici bu ekrani sihirbazin basinda da acabilir.
    /// </summary>
    Yolculuk YolculukGetir(DonanimRaporu? donanim);
}
