using UWin.Cekirdek.Modeller;

namespace UWin.Cekirdek.Arayuzler;

/// <summary>Kurulum amacini, kullaniciya verilecek tavsiyeye cevirir.</summary>
public interface IAmacServisi
{
    AmacTavsiyesi TavsiyeGetir(KurulumAmaci amac);
}
