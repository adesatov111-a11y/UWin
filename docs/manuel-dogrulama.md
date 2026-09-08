# UWin — Manuel Donanım Doğrulama Listesi

Her sürüm öncesi gerçek donanımda yürütülür. Birim testleri yıkıcı
yolları kapsayamaz; bu listenin yerini hiçbir otomatik test tutmaz.

**Güvenlik kapıları bölümündeki herhangi bir madde başarısız olursa
sürüm çıkarılmaz.**

## Hazırlık

- [ ] En az iki USB bellek (biri 8 GB, biri 32 GB veya üstü)
- [ ] Bir harici HDD (tamirci senaryosu için)
- [ ] UEFI destekli bir makine
- [ ] Legacy BIOS destekli bir makine (veya UEFI'de CSM açık)
- [ ] `install.wim` 4 GB üstü olan bir Windows 11 ISO
- [ ] `install.wim` 4 GB altı olan bir Windows 10 ISO

## Güvenlik kapıları (en kritik bölüm)

- [ ] Varsayılan modda **yalnızca** USB bellekler listeleniyor
- [ ] 128 GB üstü "çıkarılabilir" harici disk varsayılan listede **görünmüyor**
- [ ] "Tüm diskleri göster" açıldığında dahili ve harici diskler görünüyor
- [ ] Sistem diski listede görünüyor ama **seçilemiyor**, nedeni yazıyor
- [ ] Harici HDD seçildiğinde ad yazma kutusu çıkıyor
- [ ] Yanlış ad yazıldığında Devam butonu kapalı kalıyor
- [ ] Doğru ad yazıldığında Devam açılıyor
- [ ] Disk değiştirildiğinde yazılan ad sıfırlanıyor
- [ ] Onay ekranında USB'deki mevcut veri miktarı doğru gösteriliyor
- [ ] 8 GB'tan küçük USB seçildiğinde uyarı çıkıyor ve ilerlenemiyor
- [ ] **Araçlar > USB'yi temizle** listesinde yalnızca USB bellekler var
- [ ] Geri kazanmada sistem diski hiçbir koşulda listelenmiyor
- [ ] Geri kazanma sırasında pencere kapatılamıyor (yarım kalmasın)
- [ ] Geri kazanma öncesi USB'deki veri miktarı doğru gösteriliyor

## Donanım tespiti

- [ ] Anakart üretici ve modeli doğru
- [ ] İşlemci adı doğru
- [ ] RAM miktarı takılı modülle eşleşiyor (16 GB → "16 GB")
- [ ] TPM 2.0 olan makinede TPM görünüyor
- [ ] TPM'siz makinede "TPM 2.0 bulunamadı" ve Windows 10 önerisi çıkıyor
- [ ] UEFI/Legacy doğru tespit ediliyor

## İndirme

- [ ] "İndir (önerilen)" seçilip başlatıldığında gerçekten indirme başlıyor
- [ ] İlerleme yüzdesi, boyut ve kalan süre görünüyor
- [ ] İndirilen dosya SHA-256 ile doğrulanıyor (yeşil rozet)
- [ ] Türkçe ve İngilizce sürümler ayrı ayrı indirilebiliyor

## Yazma

- [ ] 8 GB USB'ye Windows 10 yazılıyor, işlem tamamlanıyor
- [ ] 32 GB USB'ye Windows 11 yazılıyor (4 GB+ WIM bölünüyor)
- [ ] Bölünmüş kurulumda USB'de `install.swm` var, `install.wim` yok
- [ ] Yazma sırasında ilerleme adım adım güncelleniyor
- [ ] Yüzdenin yanında yazılan/toplam bayt görünüyor
- [ ] Kalan süre görünüyor ve azalıyor (artıp azalıp zıplamıyor)
- [ ] Kopyalama bitince kalan süre yazısı kayboluyor
- [ ] Yazma bitince "Yazılanlar kontrol ediliyor..." adımı görünüyor
- [ ] Kontrol geçince "Kontrol edildi, her şey yerinde" yeşil kutusu çıkıyor
- [ ] Bölünmüş WIM'li USB doğrulamayı geçiyor (install.wim yok diye hata vermiyor)

## Kurulum amacı

- [ ] Karşılamadan sonra "Neden kuruyorsun?" adımı geliyor
- [ ] Dört seçenek de var: virüs, yavaşlık, yeni disk, sürüm yükseltme
- [ ] Amaç seçmeden de devam edilebiliyor (zorunlu değil)
- [ ] **Virüs** seçilince tavsiye "Sil'e bas" diyor
- [ ] **Sürüm yükseltme** seçilince tavsiye "Sil'e basma" diyor
- [ ] Seçim geri dönüldüğünde korunuyor
- [ ] USB hazır olduktan sonra aynı tavsiye yazma ekranında tekrar görünüyor
- [ ] "Sırada ne var?" ekranında da amaca özel tavsiye ve ek adımlar görünüyor

## Veri kaybı uyarısı

- [ ] Masaüstü, Belgeler vb. taranıyor ve dosya sayısı/boyutu doğru
- [ ] Dosya varsa onay kutusu işaretlenmeden devam edilemiyor
- [ ] Onay verilince devam edilebiliyor
- [ ] "Başka bir bilgisayara kuracağım" işaretlenince uyarı kayboluyor ve
      devam edilebiliyor
- [ ] Klasörler boşsa uyarı hiç çıkmıyor
- [ ] Tarama sürerken devam düğmesi kilitlenmiyor

## Windows 11 karnesi

- [ ] Maddeler açılır kapanır bir bölümde **değil**, doğrudan ekranda
- [ ] Maddeler iki sütuna yerleşiyor, pencere daraldığında tek sütuna düşüyor
- [ ] Her maddenin solunda durum rengi şerit var
- [ ] Karşılama ekranında maddeler tek tek listeleniyor (TPM, Secure Boot,
      UEFI, RAM, disk)
- [ ] Karşılanan maddeler yeşil onay, karşılanmayanlar kırmızı çarpı
- [ ] TPM kapalı bir makinede "BIOS'tan açılabilir" (sarı) çıkıyor,
      "karşılanmıyor" değil
- [ ] AMD işlemcide BIOS ayarı "AMD fTPM Switch" yazıyor
- [ ] Intel işlemcide "Intel Platform Trust Technology (PTT)" yazıyor
- [ ] Eksiklerin hepsi BIOS'tan açılabiliyorsa başlık
      "Birkaç ayar açılırsa hazır" oluyor
- [ ] RAM veya disk yetersizse "karşılanmıyor" çıkıyor ve BIOS ayarı önerilmiyor
- [ ] Eksik varsa detay bölümü açık geliyor

## Kurtarma araçları

- [ ] Onay ekranında "Kurtarma araçlarını da yaz" kutusu var ve varsayılan işaretli
- [ ] Yazma bitince USB'de `UWin-Kurtarma` klasörü oluşuyor
- [ ] Klasörde altı `.bat` dosyası ve `OKU-BENI.html` var
- [ ] `OKU-BENI.html` telefonda düzgün görünüyor
- [ ] `.bat` dosyaları Not Defteri'nde açıldığında Türkçe karakter bozukluğu yok
- [ ] `bootrec.bat` çalıştırıldığında önce onay soruyor (E/H)
- [ ] Kutu işaretsizken klasör hiç oluşmuyor
- [ ] Kurtarma araçları yazılamasa bile kurulum USB'si çalışıyor

## Dil desteği

- [ ] **İlk açılışta** dil sorma ekranı çıkıyor
- [ ] Sistemin dili Türkçe ise Türkçe önceden seçili geliyor
- [ ] Seçim yapılırken arayüz anında o dile geçiyor (Devam'a basmadan)
- [ ] "Devam et" sonrası sihirbaz seçilen dilde açılıyor
- [ ] Program kapatılıp açıldığında dil hatırlanıyor, ekran tekrar çıkmıyor
- [ ] Sağ üstte aktif dilin adı yazan bir düğme var
- [ ] Düğmeye basınca dil penceresi açılıyor
- [ ] Dil değişince **açık olan adım** da yeni dile geçiyor
- [ ] Geri/Devam düğmeleri, adım sayacı ve başlık yeni dilde
- [ ] Hakkında, Araçlar ve "Sırada ne var" pencereleri yeni dilde açılıyor
- [ ] `%AppData%\UWin\ayarlar.json` dosyası oluşuyor

### İngilizce arayüzde

- [ ] 1. adımdaki uyumluluk maddeleri İngilizce (TPM açıklaması dahil)
- [ ] BIOS ayar adları İngilizce metinde de doğru ("AMD fTPM Switch")
- [ ] Kurulum amacı tavsiyeleri İngilizce
- [ ] Veri kaybı ekranındaki klasör adları İngilizce (Desktop, Documents)
- [ ] Yazma sırasındaki ilerleme açıklamaları İngilizce
- [ ] Bir hata oluştuğunda mesaj İngilizce
- [ ] USB'ye yazılan `UWin-Kurtarma` araçları İngilizce
- [ ] `OKU-BENI.html` İngilizce ve `lang="en"` taşıyor
- [ ] `.bat` dosyalarında onay sorusu Y/N (Türkçede E/H)

## Araçlar menüsü

- [ ] Araçlar düğmesi sağ üstte görünüyor
- [ ] Menüde dört araç listeleniyor
- [ ] Bir araç seçilince menü kayboluyor, başlık o aracın adı oluyor
- [ ] "Araçlara dön" bağlantısı menüye geri getiriyor
- [ ] İşlem sürerken pencere kapatılamıyor

## USB sağlık testi

- [ ] Sağlam bir bellekte "Bellek sağlam" çıkıyor
- [ ] Test bitince bellekte `uwin-saglik-*` dosyası kalmıyor
- [ ] Test sırasında ilerleme çubuğu doluyor
- [ ] Belleğin kendi dosyalarına dokunulmuyor
- [ ] Sahte kapasiteli bir bellek varsa yakalanıyor (bulunması zor, atlanabilir)

## Kurtarma araçlarını sonradan ekleme

- [ ] Hazır kurulum USB'si seçilip araçlar yazılabiliyor
- [ ] `UWin-Kurtarma` klasörü oluşuyor, kurulum dosyaları bozulmuyor
- [ ] Araçlar zaten varsa üzerine yazılıyor, hata vermiyor

## Bu bilgisayarın raporu

- [ ] Donanım özeti sihirbazın 1. adımıyla aynı bilgiyi gösteriyor
- [ ] Windows 11 maddeleri renk şeritleriyle listeleniyor
- [ ] Hiçbir şey değiştirmiyor (salt okuma)

## USB'yi geri kazanma

- [ ] Kurulum USB'si seçilip temizlendiğinde işlem tamamlanıyor
- [ ] Sonuçta yeni sürücü harfi yazıyor
- [ ] Temizlenen USB Explorer'da normal bellek olarak görünüyor
- [ ] exFAT seçilince 4 GB üstü dosya kopyalanabiliyor
- [ ] USB takılı değilken "Temizlenecek USB bulunamadı" çıkıyor
- [ ] Hızlı biçimlendirme uyarısı görünüyor (veri kurtarılabilir notu)

## Önyükleme (asıl doğrulama)

- [ ] UEFI makinede USB'den açılıyor ve Windows kurulumu başlıyor
- [ ] Legacy BIOS makinede USB'den açılıyor
- [ ] Bölünmüş WIM'li USB'de kurulum sorunsuz tamamlanıyor

## Hata yolları

- [ ] Yazma sırasında USB çıkarıldığında anlaşılır hata veriliyor, program çökmüyor
- [ ] Yazma iptal edildiğinde kilit bırakılıyor (USB tekrar kullanılabiliyor)
- [ ] Yarım kalmış USB tekrar takıldığında tanınıyor ve uyarı gösteriliyor
- [ ] İnternet kesildiğinde indirme kaldığı yerden devam ediyor
- [ ] İndirme sırasında "İptal et" butonu çalışıyor
- [ ] İptal sonrası yarım dosya diskte kalmıyor
- [ ] Bozuk ISO indirildiğinde dosya siliniyor ve kullanıcı bilgilendiriliyor
- [ ] Bağlantı çözülemezse kullanıcı bilgilendiriliyor ve ISO seçimine yönlendiriliyor
- [ ] Hiçbir hata mesajında ham hata kodu görünmüyor (teknik bilgi katlanabilir alanda)

## Rehber

- [ ] Anakart markası doğru tespit ediliyor
- [ ] Markaya özel BIOS tuşu doğru gösteriliyor
- [ ] `UWin-Rehber.html` USB'nin köküne yazılıyor
- [ ] HTML dosyası telefonda açıldığında düzgün görünüyor
- [ ] Tanınmayan markada genel rehber gösteriliyor

## ISO tanıma ve akış sırası

- [ ] Adım 2 "Kurulum dosyası", adım sayacı 5 üzerinden ilerliyor
- [ ] Resmî siteden inen bir Windows 11 ISO'su seçildiğinde **yeşil**
      "Bu bir Windows 11 kurulum dosyası" yazıyor (sarı "tanımıyoruz" uyarısı yok)
- [ ] ISO kontrolü birkaç saniyede bitiyor (dakikalarca sürmüyor)
- [ ] Tanıma sonucunda gereken USB boyutu yazıyor (örn. "en az 9,3 GB")
- [ ] "İndir" seçiliyken sürüm listesi indirme düğmesinin üstünde çıkıyor
- [ ] "ISO dosyam var" seçiliyken sürüm sorulmuyor
- [ ] Windows olmayan bir ISO seçilirse kırmızı uyarı çıkıyor ve "Devam et" kapalı kalıyor
- [ ] Bozuk / yarım inmiş bir dosya seçilirse "Bu dosya açılamadı" diyor, program çökmüyor
- [ ] Windows 7 veya 8.1 ISO'su seçilirse "epey eski bir sürüm" uyarısı çıkıyor

## Arayüz

- [ ] Tüm metinler düzgün Türkçe (ı, ğ, ü, ş, ö, ç harfleri doğru)
- [ ] "Hakkında" butonu sağ üstte görünüyor ve açılıyor
- [ ] Bütün adımlarda içerik aynı genişlikte ve aynı hizada başlıyor
- [ ] Kartların dolgusu ve köşe yuvarlaklığı her ekranda aynı
- [ ] Hakkında ekranında ugilabs.com bağlantısı çalışıyor
- [ ] Hakkında ekranında dört aşama (kurulum öncesi/dosya/yazma/sonrası) var
- [ ] Hakkında ekranında Araçlar menüsü anlatılıyor
- [ ] Koyu ve açık temada okunabilir
- [ ] Hakkında ekranında kayan çubuk metnin üstüne binmiyor
- [ ] "ISO dosyası seç" modern Explorer penceresini açıyor (program çökmüyor)
- [ ] Dosya penceresinde süzgeç "Windows ISO (*.iso)" olarak geliyor
- [ ] Başlık çubuğu programın zemin rengiyle aynı (sistemin beyaz çubuğu görünmüyor)
- [ ] Başlık çubuğunda UWin simgesi ve adı var
- [ ] Pencere başlık çubuğundan sürüklenerek taşınabiliyor
- [ ] Görev çubuğunda ve Alt+Tab'da UWin simgesi görünüyor
- [ ] Disk listesinin üstünde gereken USB boyutu yazıyor
- [ ] Küçük USB'de uyarı ISO'nun gerçek ihtiyacını söylüyor (sabit 8 GB değil)
- [ ] "Tüm diskleri göster" anahtarı kapalıyken bu yazıyor, açıkken
      "Tüm diskler görünüyor" yazıyor (ters değil)

## Sırada ne var

- [ ] "Sırada ne var?" butonu sağ üstte, "Hakkında"nın solunda
- [ ] USB hazırlanmadan açıldığında "Bu bir önizleme" uyarısı görünüyor
- [ ] Üç bölüm de görünüyor: USB'den başlat, Windows kurulumu, kurulum sonrası
- [ ] BIOS tuşu ve boot menü tuşu anakart markasına göre doğru geliyor
- [ ] Sürücü adımında anakart markasının adı geçiyor (örn. "Gigabyte sitesine gir")
- [ ] Anakart tanınmadığında genel yönergeler geliyor, `{0}` gibi yer tutucu görünmüyor
- [ ] Yazma bittikten sonra açıldığında "önizleme" uyarısı görünmüyor
- [ ] Anlatım teknik bilgisi olmayan biri için anlaşılır

## Paketleme

- [ ] `yayin/UWin.exe` tek dosya, yanında DLL gerekmiyor
- [ ] Çift tıklandığında UAC yönetici izni istiyor
- [ ] .NET kurulu olmayan bir makinede de açılıyor
