namespace UWin.Cekirdek.Modeller;

/// <summary>
/// Kurtarma USB'sindeki tek bir onarim islemi.
///
/// <see cref="Komut"/> kullanicinin Komut Istemi'ne yazacagi satirdir;
/// USB'ye yazilan .bat dosyasi da bunu calistirir. Kullanicinin komutu
/// elle yazmasi gerekmez ama gormesi gerekir - ne calistirdigini
/// bilmeden onay veren biri, bir dahaki sefere ayni sorunu tek basina
/// cozemez.
/// </summary>
public sealed record KurtarmaAraci(
    string Kimlik,
    string Ad,
    string NeZamanKullanilir,
    string Komut,
    bool YoneticiGerekir = true,
    bool DiskeYazar = false);
