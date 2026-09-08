<div align="center">

# UWin

**Windows kurulum USB'si hazırlama sihirbazı.**

Rufus'un yaptığı işi, teknik bilgisi olmayan birinin tıklayarak
yapabileceği hâle getirir — ve USB hazır olduğunda kullanıcıyı ortada
bırakmaz.

[![Lisans: MIT](https://img.shields.io/badge/lisans-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6.svg)](#gereksinimler)
[![Test](https://img.shields.io/badge/test-319%20geçiyor-brightgreen.svg)](#test)
[![Sürüm](https://img.shields.io/badge/sürüm-1.0.0-orange.svg)](../../releases/latest)

[English](README.md) · **Türkçe**

**ugilabs** · [ugilabs.com](https://ugilabs.com)

</div>

---
<img width="906" height="753" alt="image" src="https://github.com/user-attachments/assets/00e0fff1-9ca3-418d-8566-88be3fe33936" />

## Neden başka bir USB aracı?

Rufus mükemmel bir program — ama bir varsayımı var: ne yaptığınızı
biliyorsunuz. "Bölüm şeması: GPT mi MBR mi?", "Hedef sistem: UEFI mi
BIOS mu?", "Dosya sistemi: NTFS mi FAT32 mi?" Bu soruların doğru
cevapları var, ama soruyu anlamayan biri için doğru cevap görünmez.

Daha kötüsü: USB hazır olduğunda iş bitmiyor. Kullanıcının hâlâ
bilgisayarı USB'den başlatması gerekiyor — ve bunun nasıl yapıldığı
anakart markasına göre değişiyor. Çoğu insan tam burada takılıyor.

UWin bu iki boşluğu kapatır. Teknik kararları kullanıcı yerine verir,
sonra da BIOS'a nasıl girileceğini markasına özel olarak anlatır.

---

## Ne yapar

### Kurulum öncesi

- **Bilgisayarın Windows 11'e hazır olup olmadığını söyler.** Sadece
  "uyumlu değil" demez — eksik olan şey BIOS'tan açılabilir bir ayar mı,
  yoksa donanım sınırı mı ayırt eder. TPM kapalıysa, o ayarın *sizin
  anakartınızdaki adını* söyler (`AMD fTPM Switch` / `Intel Platform
  Trust Technology (PTT)`).

- **Neden kurduğunuzu sorar.** Virüs temizliği, yavaşlık, yeni disk,
  sürüm yükseltme — tavsiyeler buna göre değişir. Bu keyfi bir soru
  değil: virüs için kurana "bölümü sil" demek gerekir, dosyalarını
  korumak isteyene "silme". Aynı ekran için iki farklı doğru cevap var
  ve hangisinin doğru olduğu yalnızca niyete bağlı.

- **Bu bilgisayarda silinecek dosyaları sayar ve gösterir** — USB
  hazırlanmadan önce, kullanıcı hâlâ yedek alabilecekken.

### ISO

- Microsoft'un **kendi sunucusundan** indirir; üçüncü taraf ayna yok.
- **SHA-256 ile doğrular.** Bozuk indirme diske ulaşmaz.
- **İptal edilebilir, kaldığı yerden devam eder.**
- Elinizdeki bir ISO'yu da seçebilirsiniz.

### Yazma

- **UEFI/Legacy ve GPT/MBR kararını sizin yerinize verir.**
- **4 GB üstü `install.wim` sorununu** kullanıcı hiç bilmeden çözer.
  (FAT32 4 GB'tan büyük dosya tutamaz; modern Windows ISO'larının
  `install.wim` dosyası genellikle daha büyüktür. UWin dosyayı parçalara
  böler.)
- **Kalan süreyi gösterir** — yüzde değil, gerçek bayt hızından
  hesaplanan dakika.
- **Yazma bitince USB'yi geri okur ve doğrular.** Ucuz belleklerin sessiz
  yazma hatası BIOS'ta değil, burada yakalanır.

### Yazma sonrası

- **Anakart markasına özel BIOS rehberi** gösterir *ve USB'ye de yazar*.
  Bilgisayar kapalıyken telefondan okunabilir.
- **USB'ye kurtarma araçları yazar:** açılmayan bir Windows'u onarmak
  için hazır `.bat` dosyaları (`bootrec`, `sfc`, `chkdsk`, güvenli mod,
  dosyalara erişim, sürüm listesi).

### Araçlar menüsü

| Araç | Ne işe yarar |
|------|--------------|
| **USB'yi geri kazan** | Kurulum USB'sini normal kullanıma döndürür (exFAT + MBR) |
| **Sağlık testi** | Sahte kapasiteli USB belleklerini yakalar |
| **Kurtarma araçları ekle** | Var olan bir USB'ye onarım araçlarını sonradan yazar |
| **Donanım raporu** | Bu bilgisayarın tam donanım dökümü |

### Dil

**Türkçe ve İngilizce.** İlk açılışta dil sorulur (sistem diline göre
önceden seçili gelir), sonra sağ üstten tek tıkla değişir ve seçim
hatırlanır.

Yalnızca düğme yazıları değil: hata mesajları, kurulum tavsiyeleri,
uyumluluk karnesi, BIOS rehberi ve **USB'ye yazılan kurtarma araçları**
da seçilen dilde.

---

## Kurulum

1. [**Releases**](../../releases/latest) sayfasından `UWin.exe` dosyasını
   indirin.
2. Çift tıklayın.

Kurulum yok, .NET kurmanız gerekmiyor, kayıt defterine hiçbir şey
yazılmıyor. Tek dosya, ~90 MB (çalışma zamanı içeride).

> **Windows "bilinmeyen yayımcı" uyarısı verirse:** *Daha fazla bilgi* →
> *Yine de çalıştır*. Bu, programın kod imzalama sertifikası olmadığı
> anlamına gelir — sertifika yıllık ücretli bir üründür. Kaynak kodu
> burada, kendiniz derleyebilirsiniz.

> **Yönetici izni** ister. Diske doğrudan yazmak için gereklidir; başka
> bir yolu yoktur.

### Gereksinimler

| | |
|---|---|
| İşletim sistemi | Windows 10 sürüm 1809 (build 17763) veya üstü |
| Mimari | x64 |
| Yetki | Yönetici |
| USB | En az 8 GB (ISO'ya göre değişir, program size söyler) |

---

## Nasıl kullanılır

Program altı adımda ilerler; her adımda ne olacağını yazar.

```
1. Karşılama    →  Bilgisayarınız Windows 11'e hazır mı?
2. Amaç         →  Neden kuruyorsunuz?
3. Kaynak       →  ISO indir veya kendi dosyanı seç
4. Disk         →  Hangi USB?
5. Onay         →  Silinecekler burada listelenir
6. Yazma        →  Yaz, doğrula, BIOS rehberini göster
```

En kritik ekran **5. adım**. Orada yazanı okuyun: USB'deki her şey
silinir ve geri getirilemez.

---

## Güvenlik

Bu programın yapabileceği en büyük zarar yanlış diski silmektir.
Korumalar katmanlı:

1. **Sistem diski hiçbir modda seçilemez** — kod seviyesinde engel,
   ayarla açılamaz
2. **Dahili diskler ve 128 GB üstü harici diskler varsayılan olarak gizli**
3. **Yazma öncesi yeniden doğrulama** — USB çıkarılıp değiştirilmişse
   işlem başlamaz
4. **Ad yazarak onay** — USB dışı hedefler için diskin adı elle yazılmalı
5. **Veri hacmi gösterimi** — "boş sanıyordum" hatasını engeller
6. **Tek yazma noktası** — `YazmaServisi` dışında hiçbir kod diske yazmaz

Testlerde bellek içi sahte disk kullanılır: **hiçbir test gerçek disk
silmez.**

---

## Mimari

Üç katman, tek yönlü bağımlılık:

```
Sunum (WinUI 3)  →  Servis  →  Yerel (P/Invoke, WMI)
```

```
kaynak/
├── UWin.Cekirdek/          İş mantığı — arayüz bağımlılığı yok
│   ├── Arayuzler/          Servis sözleşmeleri
│   ├── Modeller/           Kayıt tipleri (record)
│   ├── Servisler/          19 servis
│   ├── Yerel/              P/Invoke, WMI
│   └── Kaynaklar/          Metinler.{tr,en}.json,
│                           bios-rehberleri.{tr,en}.json
└── UWin.Uygulama/          WinUI 3 arayüzü
    ├── Adimlar/            Altı sihirbaz adımı
    └── Servisler/          Bağımlılık bağlama
```

Tüm yıkıcı işlemler `IYerelDiskErisimi` arayüzünden geçer. Bu, hem
testlerin güvenli olmasını hem de yazma yolunun tek noktadan
denetlenebilmesini sağlar.

---

## Kaynaktan derleme

```bash
git clone https://github.com/<kullanici>/UWin.git
cd UWin

dotnet test                    # 319 test
pwsh -File yayinla.ps1         # yayin/UWin.exe üretir
```

`yayinla.bat` dosyasına çift tıklayarak da derleyebilirsiniz — testleri
çalıştırır, geçmezse sürüm çıkarmaz.

Gerekenler: [.NET 10 SDK](https://dotnet.microsoft.com/download) ve
Windows 10/11.

### Test

```bash
dotnet test                                    # tümü (319)
dotnet test testler/UWin.Cekirdek.Testler      # yalnızca çekirdek (251)
```

Test yaklaşımı: her servis önce testi yazılarak geliştirildi, testin
gerçekten başarısız olduğu görüldükten sonra kodlandı. Dil kapsamı,
metin anahtarı bütünlüğü ve servis bağlamaları da testle korunuyor —
yeni bir servis eklenip metin sağlayıcıya bağlanması unutulursa test
hangi servisin unutulduğunu adıyla söyler.

Yıkıcı yollar birim testiyle kapsanamaz;
[docs/manuel-dogrulama.md](docs/manuel-dogrulama.md) listesi (17 bölüm,
170 madde) sürüm öncesi gerçek donanımda yürütülmelidir.

---

## Bilinen sınırlar

**Microsoft ISO indirme uç noktası resmi bir API değildir.** Çalışıyor ve
doğrulandı, ancak iki şekilde kesintiye uğrayabilir:

- **IP engeli:** Kısa sürede çok fazla indirme isteği yapılırsa Microsoft
  adresi geçici olarak engeller (hata kodu 715-123130). Rufus/Fido
  programlarında da aynısı olur. UWin bunu tanır, yeniden denemez ve
  kullanıcıya beklemesi gerektiğini söyler.
- **Akış değişikliği:** Microsoft adımları değiştirirse çözümleme kırılır.
  Program çökmez; kullanıcı bilgilendirilir ve "kendi ISO'nu seç" yoluna
  yönlendirilir. En kırılgan parça olan regex desenleri gerçek bir yanıt
  örneği üzerinde test altındadır.

**BIOS ayarları program içinden değiştirilemez.** Firmware ayarlarının
üretici-bağımsız bir yazma arayüzü yoktur; UWin bunun yerine markaya özel
rehber gösterir.

**Yalnızca x64.** ARM64 Windows cihazlarda çalışmaz.

---

## Sık sorulanlar

**USB'mdeki dosyalar silinecek mi?**
Evet, tamamı. Geri getirilemez. Program bunu 5. adımda açıkça söyler.

**Bilgisayarımdaki dosyalar silinecek mi?**
USB hazırlarken hayır. Ama sonra Windows'u kurarken silinebilir — bu
tamamen kurulum ekranında ne seçtiğinize bağlı. UWin, 2. adımda
"neden kuruyorsun" sorusunun cevabına göre o ekranda ne yapmanız
gerektiğini söyler ve neyin silineceğini önceden listeler.

**Rufus'tan farkı ne?**
Rufus daha fazla seçenek sunar ve daha esnektir. UWin daha az soru sorar,
kararları sizin yerinize verir ve USB hazır olduktan *sonra* da yardım
etmeye devam eder (BIOS rehberi, kurtarma araçları). Rufus'u biliyorsanız
muhtemelen Rufus kullanmaya devam etmelisiniz.

**Linux ISO'su yazabilir miyim?**
v1.0 yalnızca Windows ISO dosyaları için tasarlandı.

**İnternet olmadan çalışır mı?**
Elinizde ISO varsa evet. İndirme dışında hiçbir özellik internet
gerektirmez.

**Antivirüs uyarı veriyor.**
Diske doğrudan yazan her program bunu tetikler — Rufus da dahil. Kaynak
kod burada; kendiniz derleyip kullanabilirsiniz.

---

## Katkı

Hata bildirimi ve öneriler için [issue açın](../../issues/new). Hata
bildirirken şunlar çok yardımcı olur:

- Windows sürümü (`winver`)
- Anakart / bilgisayar markası ve modeli
- Ne yapmaya çalıştığınız ve ne olduğu
- Varsa hata mesajının ekran görüntüsü

Kod katkısı için: kod ve yorumlar Türkçe (ASCII), testler önce yazılıyor.
Büyük bir değişiklik yapmadan önce bir issue açıp konuşalım.

---

## Belgeler

- [Tasarım dokümanı](docs/superpowers/specs/2026-09-07-uwin-design.md)
- [Uygulama planı](docs/superpowers/plans/2026-09-07-uwin-v1.md)
- [Manuel doğrulama listesi](docs/manuel-dogrulama.md)

---

## Lisans

[MIT](LICENSE) — kullanın, değiştirin, dağıtın.

Yazılım "olduğu gibi" sunulur, hiçbir garanti verilmez. Bu program
diskleri siler; kullanmadan önce ne yaptığınızdan emin olun.
