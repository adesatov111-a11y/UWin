using System.ComponentModel;
using System.Runtime.CompilerServices;
using UWin.Cekirdek.Modeller;

namespace UWin.Uygulama.GorunumModelleri;

public enum SihirbazAdimi
{
    Karsilama,
    Amac,
    Kaynak,
    Disk,
    Onay,
    Yazma
}

/// <summary>
/// Adimlar arasi tasinan secimler ve ilerleme kurallari.
/// Ilerleme mantigi XAML'den bagimsizdir ve testle korunur.
/// </summary>
public sealed class SihirbazDurumu : INotifyPropertyChanged
{
    private SihirbazAdimi _aktifAdim = SihirbazAdimi.Karsilama;
    private DonanimRaporu? _donanim;
    private UyumlulukRaporu? _uyumluluk;
    private KurulumAmaci _amac = KurulumAmaci.Belirtilmemis;
    private VeriRaporu? _veri;
    private bool _veriUyarisiOnaylandi;
    private bool _buBilgisayaraKurulacak = true;
    private WindowsSurumu? _secilenSurum;
    private string? _isoYolu;
    private bool _isoDogrulandi;
    private bool _kendiIsosu;
    private IsoKimligi? _isoKimligi;
    private bool _gelismisMod;
    private bool _kurtarmaAraclariEklensin = true;
    private string? _adOnayi;
    private DiskBilgisi? _secilenDisk;

    public event PropertyChangedEventHandler? PropertyChanged;

    public SihirbazAdimi AktifAdim
    {
        get => _aktifAdim;
        private set => Ata(ref _aktifAdim, value);
    }

    public DonanimRaporu? Donanim
    {
        get => _donanim;
        set => Ata(ref _donanim, value);
    }

    /// <summary>Windows 11 karnesi: madde madde, ne yapilabilecegiyle birlikte.</summary>
    public UyumlulukRaporu? Uyumluluk
    {
        get => _uyumluluk;
        set => Ata(ref _uyumluluk, value);
    }

    /// <summary>
    /// Kurulum amaci. Kurulum sirasinda verilecek tavsiyeler buna gore
    /// degisir; ayni ekran icin birbirine zit yonergeler soz konusudur.
    /// </summary>
    public KurulumAmaci Amac
    {
        get => _amac;
        set => Ata(ref _amac, value);
    }

    /// <summary>Bu bilgisayarda kurulumla birlikte gidecek dosyalarin ozeti.</summary>
    public VeriRaporu? Veri
    {
        get => _veri;
        set => Ata(ref _veri, value);
    }

    /// <summary>Kullanici veri kaybi uyarisini gordugunu onayladi mi.</summary>
    public bool VeriUyarisiOnaylandi
    {
        get => _veriUyarisiOnaylandi;
        set => Ata(ref _veriUyarisiOnaylandi, value);
    }

    /// <summary>
    /// Kurulum bu makineye mi yapilacak. false ise buradaki dosyalar
    /// tehlikede degildir ve veri uyarisi gosterilmez.
    /// </summary>
    public bool BuBilgisayaraKurulacak
    {
        get => _buBilgisayaraKurulacak;
        set => Ata(ref _buBilgisayaraKurulacak, value);
    }

    public WindowsSurumu? SecilenSurum
    {
        get => _secilenSurum;
        set => Ata(ref _secilenSurum, value);
    }

    public string? IsoYolu
    {
        get => _isoYolu;
        set => Ata(ref _isoYolu, value);
    }

    public bool IsoDogrulandi
    {
        get => _isoDogrulandi;
        set => Ata(ref _isoDogrulandi, value);
    }

    /// <summary>Kullanici ISO'yu kendi sectiyse true (indirmek yerine).</summary>
    public bool KendiIsosu
    {
        get => _kendiIsosu;
        set => Ata(ref _kendiIsosu, value);
    }

    /// <summary>Secilen ISO'nun icine bakilarak cikarilan kimligi.</summary>
    public IsoKimligi? IsoKimligi
    {
        get => _isoKimligi;
        set => Ata(ref _isoKimligi, value);
    }

    public bool GelismisMod
    {
        get => _gelismisMod;
        set => Ata(ref _gelismisMod, value);
    }

    /// <summary>
    /// Kurtarma araclari USB'ye yazilsin mi. Varsayilan acik: birkac yuz
    /// kilobayt yer kaplar ve acilmayan bir Windows karsisinda tek
    /// dayanak olabilir.
    /// </summary>
    public bool KurtarmaAraclariEklensin
    {
        get => _kurtarmaAraclariEklensin;
        set => Ata(ref _kurtarmaAraclariEklensin, value);
    }

    public string? AdOnayi
    {
        get => _adOnayi;
        set => Ata(ref _adOnayi, value);
    }

    /// <summary>Disk degistiginde ad onayi sifirlanir - eski onay yeni diske gecmez.</summary>
    public DiskBilgisi? SecilenDisk
    {
        get => _secilenDisk;
        set
        {
            if (Ata(ref _secilenDisk, value))
                AdOnayi = null;
        }
    }

    /// <summary>USB disi hedefler icin kullanicinin disk adini yazmasi zorunludur.</summary>
    public bool AdOnayiGerekiyorMu
        => SecilenDisk is not null && SecilenDisk.Sinif != DiskSinifi.UsbBellek;

    /// <summary>
    /// USB'nin en az ne kadar olmasi gerektigi. Elde gercek ISO varsa
    /// icindeki dosyalardan hesaplanan deger kullanilir - sabit 8 GB
    /// varsaymak, 7 GB'lik bir install.wim tasiyan ISO'da kullaniciyi
    /// yazmanin ortasinda hataya goturur.
    /// </summary>
    public long GerekenUsbBoyutuBayt
    {
        get
        {
            if (IsoKimligi is { GerekenUsbBoyutuBayt: > 0 } kimlik)
                return kimlik.GerekenUsbBoyutuBayt;

            return SecilenSurum?.AsgariUsbBoyutuBayt ?? 8L * 1000 * 1000 * 1000;
        }
    }

    /// <summary>Secilen disk, secilen kaynak icin yeterince buyuk mu.</summary>
    public bool DiskYeterinceBuyukMu
        => SecilenDisk is null
           || SecilenDisk.BoyutBayt >= GerekenUsbBoyutuBayt;

    public int ToplamAdimSayisi => Enum.GetValues<SihirbazAdimi>().Length;

    /// <summary>Aktif adimin kacincisi oldugu (1'den baslar).</summary>
    public int AdimSirasi => (int)AktifAdim + 1;

    /// <summary>
    /// Veri kaybi uyarisi gosterilmeli mi. Yalnizca bu makineye kurulum
    /// yapilacaksa ve gercekten silinecek dosya varsa anlamlidir.
    /// </summary>
    public bool VeriUyarisiGerekiyorMu
        => BuBilgisayaraKurulacak && Veri is { VeriVar: true };

    public bool IlerleyebilirMi() => AktifAdim switch
    {
        SihirbazAdimi.Karsilama => Donanim is not null,

        // Amac adimi hicbir secim zorunlu kilmaz: emin olmayan kullanici
        // "geç" diyebilmeli. Zorunlu kilmak, rastgele bir secim yapip
        // yanlis tavsiye almasina yol acardi.
        SihirbazAdimi.Amac => !VeriUyarisiGerekiyorMu || VeriUyarisiOnaylandi,

        SihirbazAdimi.Kaynak => !string.IsNullOrWhiteSpace(IsoYolu),
        SihirbazAdimi.Disk => SecilenDisk is { YazilabilirMi: true } && DiskYeterinceBuyukMu,
        SihirbazAdimi.Onay => OnayGecerliMi(),
        _ => false
    };

    private bool OnayGecerliMi()
    {
        if (SecilenDisk is not { YazilabilirMi: true } disk)
            return false;

        if (!DiskYeterinceBuyukMu)
            return false;

        if (!AdOnayiGerekiyorMu)
            return true;

        return AdOnayi is not null
            && AdOnayi.Trim().Equals(disk.Ad.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public void Ilerle()
    {
        if (!IlerleyebilirMi() || AktifAdim >= SihirbazAdimi.Yazma)
            return;

        AktifAdim++;
    }

    public void Geri()
    {
        if (AktifAdim <= SihirbazAdimi.Karsilama)
            return;

        AktifAdim--;
    }

    private bool Ata<T>(ref T alan, T deger, [CallerMemberName] string? ozellik = null)
    {
        if (EqualityComparer<T>.Default.Equals(alan, deger))
            return false;

        alan = deger;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ozellik));
        return true;
    }
}
