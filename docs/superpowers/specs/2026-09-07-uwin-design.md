# UWin — Tasarım Dokümanı

**Tarih:** 2026-09-07
**Ürün:** UWin — Windows kurulum USB hazırlama sihirbazı
**Üretici:** ugilabs (ugilabs.com)
**Sürüm kapsamı:** v1

---

## 1. Problem

Windows kurulum USB'si hazırlamak teknik olarak çözülmüş bir problem (Rufus, Ventoy, Media Creation Tool), ancak **kullanıcı deneyimi olarak çözülmemiş**. Mevcut araçlar üç yerde başarısız oluyor:

1. **Karar yükü kullanıcıda.** Rufus "MBR mi GPT mi?", "BIOS mu UEFI mi?", "Cluster boyutu?" diye soruyor. Sıfır bilgili kullanıcı bu soruların hiçbirini cevaplayamaz ve yanlış cevap sessizce başarısız bir USB üretir.
2. **ISO kaynağı belirsiz.** Araçlar ISO istiyor ama nereden bulunacağını söylemiyor. Kullanıcı arama motoruna yöneliyor ve virüslü/değiştirilmiş ISO indiriyor.
3. **USB hazır olduğunda hikâye bitiyor.** Kullanıcı elinde USB ile ortada kalıyor: BIOS'a nasıl girilir, boot sırası nedir, kurulumda hangi seçenek doğru — hiçbiri söylenmiyor.

UWin bu üçünü de çözer: kararları kullanıcı yerine verir, ISO'yu Microsoft'un kendi sunucusundan indirip doğrular, ve USB hazır olduktan sonra kullanıcıyı BIOS'tan kurulum sonuna kadar yönlendirir.

## 2. Hedef Kitle

İki kitleye aynı anda hizmet eder ve bu ikisi arasındaki gerilim tasarımın merkezindedir:

- **Birincil: teknik olmayan kullanıcı.** Bilgisayarını kendi kurmak isteyen, terimleri bilmeyen kişi. Varsayılan akış tamamen bu kişiye göre tasarlanır — hiçbir teknik soru sorulmaz.
- **İkincil: bilgisayar tamircisi / teknisyen.** Günde birden fazla kurulum yapan, harici HDD'ye yazan, ISO'yu elinde tutan profesyonel. Bu kitle için gelişmiş yetenekler **mevcut ama gizli** olmalıdır — varsayılan akışı hiç karmaşıklaştırmadan.

**Tasarım ilkesi:** Basit yol varsayılan, güçlü yol bir tık uzakta. Hiçbir gelişmiş özellik, temel kullanıcının gördüğü ekrana terim eklemez.

## 3. Kapsam

### v1 kapsamında
- Makine donanımını okuma ve uyumluluk raporu (TPM, UEFI/Legacy, Secure Boot, RAM, disk)
- Windows sürüm/dil seçimi ve Microsoft resmi sunucusundan ISO indirme
- İndirilen ISO'nun SHA-256 doğrulaması
- Kullanıcının kendi ISO'sunu kullanabilmesi
- Disk listeleme, risk sınıflandırma, güvenli seçim akışı
- Bootable USB yazma (GPT/UEFI ve MBR/Legacy)
- 4GB+ `install.wim` sorununun otomatik çözümü
- Anakart markasına özel BIOS rehberi (ekranda + USB'ye HTML olarak)
- Yarım kalmış USB'nin tespiti ve onarımı
- Türkçe ve İngilizce arayüz

### v1 kapsamı dışında (v2'ye mimari yer ayrıldı)
- Kurulum sonrası sürücü tespiti ve orijinal kaynaktan indirme
- Temel program kurulumu (tarayıcı, arşiv, medya oynatıcı vb.)
- unattend.xml ile otomatik kurulum
- Çoklu ISO (Ventoy tarzı) USB

### Açıkça kapsam dışı (yapılmayacak)
- BIOS ayarlarını program içinden değiştirmek. **Teknik olarak mümkün değildir** — firmware ayarlarının üretici-bağımsız standart bir yazma arayüzü yoktur. Yapılabilen tek müdahale, Windows'a "sonraki açılışta UEFI ayarlarına gir" talimatı vermektir (Gelişmiş Başlatma / `bcdedit` yolu). Bu, rehberin bir parçası olarak sunulur.
- Windows lisans anahtarı üretme/aktivasyon. Program yalnızca Microsoft'un serbestçe dağıttığı kurulum medyasını kullanır.
- Değiştirilmiş/özelleştirilmiş ISO dağıtımı.

## 4. Teknoloji Seçimi

| Katman | Seçim | Gerekçe |
|---|---|---|
| Dil / Çatı | C# / .NET 10 | Win32 disk API'lerine P/Invoke ile doğrudan ve okunaklı erişim; WMI/CIM ile donanım tespiti yerli; güçlü hata yönetimi |
| Arayüz | WinUI 3 (Windows App SDK 2.4) | 2026 standardında modern görünüm, Windows'a ait hissettiren tanıdık dil, Fluent tasarım |
| Paketleme | Tek dosya, self-contained `.exe` | Kurulum gerektirmez; .NET yüklü olmayan makinede de çalışır; taşınabilir |
| Test | xUnit + sahte disk/HTTP katmanları | Yıkıcı kodu gerçek disk silmeden test edebilmek |

**Neden Electron/Tauri değil:** Projedeki asıl risk disk yazma doğruluğu. Bu katmanı ikinci bir dile (Rust/C++) devretmek riski artırır ve iki dilli bir kod tabanı yaratır. C# hem arayüzü hem yerel erişimi tek dilde karşılıyor.

**Neden C++/Qt değil:** Geliştirme hızı ve hata yönetimi güvenliği C# lehine. Rufus'un C ile elde ettiği performans avantajı, dakikalar mertebesinde bir USB yazma işleminde kullanıcı için fark edilmez.

## 5. Mimari

Üç katman, tek yönlü bağımlılık: **Sunum → Servis → Yerel**. Sunum katmanı yerel katmanı hiç tanımaz.

```
┌─────────────────────────────────────────────────┐
│  Sunum (WinUI 3)                                │
│  Sihirbaz ekranları, ViewModel'ler              │
└───────────────────┬─────────────────────────────┘
                    │ arayüzler (IDiskServisi vb.)
┌───────────────────▼─────────────────────────────┐
│  Servis katmanı                                 │
│  DonanimServisi  SurumServisi   IndirmeServisi  │
│  DiskServisi     YazmaServisi   RehberServisi   │
└───────────────────┬─────────────────────────────┘
                    │ yalnızca Disk + Yazma servisleri
┌───────────────────▼─────────────────────────────┐
│  Yerel katman (P/Invoke, Win32)                 │
│  Ham disk erişimi, bölümleme, birim yönetimi    │
└─────────────────────────────────────────────────┘
```

### Servisler

Her servis tek bir sorumluluk taşır, bir arayüz arkasında durur ve bağımsız test edilir.

**`DonanimServisi`** — *Salt okuma, sıfır risk.*
WMI/CIM üzerinden okur: anakart üreticisi ve modeli, işlemci, RAM, TPM sürümü ve durumu, firmware tipi (UEFI/Legacy), Secure Boot durumu, disk tipi. Çıktısı bir `DonanimRaporu` nesnesi. Windows 11 uyumluluk kararını da bu servis verir (TPM 2.0 + UEFI + Secure Boot yeteneği + 4GB RAM + 64GB disk).

**`SurumServisi`**
Microsoft'un yayın kataloğundan mevcut Windows sürümlerini, dilleri ve mimarileri listeler. Seçime karşılık gelen indirme bağlantısını ve beklenen SHA-256 özetini üretir. Uç nokta erişilemezse servis "kullanılamıyor" döner ve arayüz sessizce "kendi ISO'nu seç" yoluna düşer — program çökmez.

**`IndirmeServisi`**
ISO'yu indirir. Parçalı indirme (`Range` başlığı) ile duraklatma/devam desteği, ilerleme ve kalan süre bildirimi, ağ kesintisinde otomatik yeniden deneme. İndirme bitince SHA-256 hesaplar ve beklenen değerle karşılaştırır. Uyuşmazsa dosya **silinir** ve kullanıcıya bildirilir — bozuk ISO diske hiç ulaşmaz.

**`DiskServisi`** — *Salt okuma; yazma yapmaz.*
Sistemdeki tüm diskleri listeler ve her birini sınıflandırır:

| Sınıf | Tanım | Varsayılan görünürlük |
|---|---|---|
| `UsbBellek` | Çıkarılabilir, USB veri yolu | Görünür |
| `HariciDisk` | Sabit disk, USB/Thunderbolt veri yolu | Gelişmiş modda |
| `DahiliDisk` | Sabit disk, SATA/NVMe iç veri yolu | Gelişmiş modda |
| `SistemDiski` | Windows'un kurulu olduğu disk | **Asla seçilemez** |

Her disk için ad, model, boyut, mevcut bölümler ve içindeki veri hacmi raporlanır. Sistem diski hiçbir modda seçilebilir değildir — bu tek istisnadır ve teknisyen modunda dahi kaldırılmaz, çünkü çalışan sistemin diskini biçimlendirmek hiçbir senaryoda meşru bir işlem değildir.

**`YazmaServisi`** — *Tek yıkıcı nokta.*
Sistemdeki yazma yapan tek bileşen. Sabit sıra:
1. Hedef diski **yeniden doğrula** (seri numarası + boyut, seçim anındakiyle aynı mı)
2. Diski kilitle, bağlı birimleri sök
3. Bölüm tablosu yaz — hedef makine UEFI ise GPT, Legacy ise MBR; belirsizse GPT+FAT32 (her ikisiyle de açılır)
4. Biçimlendir
5. ISO içeriğini kopyala
6. `install.wim` > 4GB ise: WIM'i `install.swm` parçalarına böl (FAT32 uyumlu, en geniş uyumluluk)
7. Boot dosyalarını yerleştir
8. Kilidi bırak, doğrula

Her adım ilerleme bildirir ve iptal edilebilir. İptal edilirse disk yarım kalır ve bu durum diske işaretlenir.

**`RehberServisi`**
Anakart markasına göre BIOS rehberi içeriğini üretir. İçerik veri olarak (JSON) gömülüdür, kodla karışmaz. Bilinen markalar: ASUS, MSI, Gigabyte, ASRock, HP, Dell, Lenovo, Acer, Casper, Monster, Toshiba, Samsung. Tanınmayan markada genel rehbere düşer. Aynı içeriği tek dosyalık HTML olarak USB'ye yazar.

## 6. Kullanıcı Akışı

Altı adım. Her adımda tek karar, her adımdan geri dönülebilir.

### Adım 1 — Karşılama ve Sistem Kontrolü
Program açılırken arka planda donanımı okur. Kullanıcı hiçbir şey yapmadan raporu görür:

> **Bilgisayarın hazır.**
> MSI B550-A PRO · Ryzen 5 5600 · 16 GB RAM
> UEFI ✓ · TPM 2.0 ✓ · Secure Boot destekli ✓
> **Windows 11 kurulabilir.**

Uyumsuzluk varsa neden ve ne yapılabileceği söylenir: *"TPM 2.0 bulunamadı. Windows 11 resmî olarak kurulamaz — Windows 10 öneriyoruz. TPM anakartında olabilir ama BIOS'ta kapalı olabilir; kontrol etmeni gösterelim mi?"*

Bu ekran kullanıcıyı saatler sonra duvara toslamaktan kurtarır.

### Adım 2 — Windows Sürümü
Windows 11 / Windows 10, dil, mimari. Makineye uygun olan zaten seçili gelir. Home/Pro ayrımı burada sorulmaz — Microsoft ISO'ları her ikisini içerir ve seçim kurulum sırasında yapılır; bu, rehberde açıklanır.

### Adım 3 — ISO Kaynağı
İki seçenek: **"İndir (önerilen)"** veya **"ISO dosyam var"**. İndirme sırasında ilerleme, hız, kalan süre. Bitince doğrulama:

> ✓ **Bu dosya Microsoft'un orijinal dosyası.**
> SHA-256 doğrulandı.

Kendi ISO'sunu seçen kullanıcının dosyası da doğrulanır — bilinen bir özetle eşleşirse yeşil rozet, eşleşmezse *"Bu dosyayı tanımıyoruz. Microsoft'un resmî dosyası olmayabilir. Yine de devam edebilirsin, ama indirmeni öneririz."* — engellemez, uyarır.

### Adım 4 — Hedef Disk
Varsayılan: yalnızca USB bellekler. Altta sade bir anahtar: **"Tüm diskleri göster"** (teknisyen için). Açıldığında harici ve dahili diskler de listelenir, her biri risk seviyesine göre renklendirilir. Sistem diski listede görünür ama seçilemez ve nedeni yazar.

Her disk kartı şunu gösterir: ad ve model, boyut, mevcut bölümler, içindeki veri miktarı.

### Adım 5 — Onay
Ne olacağının düz Türkçe özeti:

> **SanDisk Ultra 32 GB** silinecek ve Windows 11 kurulum diski yapılacak.
> Şu an içinde **12,4 GB veri** var. Bu veriler geri getirilemez.

USB dışı bir disk seçildiyse ek kilit: kullanıcı diskin **adını yazarak** onaylar. Sadece butona basmak yetmez. Bu, tamircinin işini yavaşlatmaz (bir kez yazar) ama yanlış diske yazmayı pratikte imkânsız kılar.

### Adım 6 — Yazma ve Sonrası
İlerleme adım adım gösterilir. Bittiğinde program **kapanmaz** — doğrudan BIOS rehberine geçer:

> **USB hazır. Sırada ne var?**
> 1. Bilgisayarı kapat, USB'yi tak
> 2. Aç ve hemen **DELETE** tuşuna bas *(senin MSI anakartın için)*
> 3. Boot sırasında USB'yi ilk sıraya al

Bu ekran programın bitişi değil, kullanıcının yolculuğunun ortasıdır. Rufus'un kaybettiği yer tam burasıdır.

## 7. Güvenlik ve Veri Koruma

Bu programın yapabileceği en büyük zarar yanlış diski silmektir. Korumalar katmanlı:

1. **Sistem diski asla seçilemez** — kod seviyesinde engel, hiçbir modda kaldırılmaz.
2. **Dahili diskler varsayılan olarak gizli** — kasıtlı bir tık gerektirir.
3. **Yazma öncesi yeniden doğrulama** — seçimden sonra USB değiştirilmişse işlem başlamaz.
4. **Ad yazarak onay** — USB dışı hedefler için.
5. **Veri hacmi gösterimi** — "boş sanıyordum" hatasını engeller.
6. **Tek yazma noktası** — `YazmaServisi` dışında hiçbir kod diske yazmaz; denetimi kolaylaştırır.

## 8. Hata Yönetimi

Her hata üç şeyi söyler: **ne oldu, neden oldu, şimdi ne yapmalısın.** Ham hata kodu asla kullanıcıya gösterilmez (ayrıntı için katlanabilir "teknik bilgi" alanında durur).

| Durum | Kullanıcıya |
|---|---|
| USB yazma korumalı | "USB'ye yazılamadı. Bazı belleklerde yan tarafta küçük bir kilit anahtarı olur — kapalı olabilir." |
| Disk çıkarıldı | "USB çıkarıldı. Tekrar tak, kaldığımız yerden devam edelim." |
| İndirme kesildi | "Bağlantı koptu. İnternet gelince kaldığı yerden devam edecek." (indirilen kısım korunur) |
| SHA-256 uyuşmadı | "İndirilen dosya bozuk çıktı. Güvenli olsun diye sildik, yeniden indiriyoruz." |
| Disk çok küçük | "Bu USB 4 GB. Windows 11 için en az 8 GB gerekiyor." |
| Yarım kalmış USB | "Bu USB'de yarım kalmış bir işlem var. Baştan yapalım mı?" |

## 9. Test Stratejisi

Yıkıcı kod **gerçek disk silmeden** test edilir:

- `DiskServisi` / `YazmaServisi` → sahte disk katmanı (`IYerelDiskErisimi` arayüzü) üzerinden. Tüm bölümleme ve yazma mantığı bellek içi sahte diskle doğrulanır.
- `DonanimServisi` → kaydedilmiş gerçek WMI çıktıları (farklı makinelerden toplanmış örnekler) üzerinden.
- `IndirmeServisi` → sahte HTTP sunucusu; kesinti, devam, bozuk özet senaryoları.
- `RehberServisi` → içerik bütünlüğü (her bilinen marka için eksiksiz rehber var mı).

Ayrıca gerçek USB ile yürütülen manuel doğrulama listesi tutulur: UEFI makinede boot, Legacy makinede boot, 4GB+ WIM'li ISO, yazma sırasında USB çıkarma.

**TDD:** Her servis testle başlar. `YazmaServisi` en son yazılır ve en yoğun test edilen bileşendir.

## 10. v2 İçin Ayrılan Yer

Mimari v2'yi baştan bekler; v1'de aşağıdakiler hazırlanır ama kullanılmaz:

- `DonanimServisi` zaten donanımı tanımlıyor — v2'deki `SurucuServisi` bunun çıktısını tüketecek, mevcut kod değişmeyecek.
- Yazma sırasında UWin **kendi kopyasını USB'nin köküne bırakır**. v1'de bu kopya çalıştırıldığında "Kurulum sonrası özellikler yakında" der; v2'de gerçek sürücü kurulumu açılır. Kullanıcı alışkanlığı v1'den itibaren oluşur.
- Sürücü kaynakları hakkında dürüst sınır: NVIDIA, AMD ve Intel makinece okunabilir sürücü kaynakları sunar. Anakart üreticileri (ASUS, MSI, Gigabyte) resmî API sunmaz — bu durumda v2, üreticinin resmî indirme sayfasına model-özel yönlendirme yapar, sahte bir otomasyon vaat etmez.

## 11. Marka ve Dil

- Ürün adı **UWin**, üretici **ugilabs** (ugilabs.com).
- Arayüz dili sade Türkçe; teknik terim kullanıldığında yanında bir cümlelik açıklaması durur.
- Görsel dil: WinUI 3 Fluent temeli, koyu/açık tema desteği, sistem vurgu rengine saygı. Sade, boşluğu cömert, tek ekranda tek karar.
