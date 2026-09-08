<div align="center">

# UWin

**A Windows installation USB wizard for people who don't know what UEFI is.**

It does what Rufus does, but makes every technical decision for you — and
keeps helping after the USB is ready.

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6.svg)](#requirements)
[![Tests](https://img.shields.io/badge/tests-319%20passing-brightgreen.svg)](#testing)
[![Version](https://img.shields.io/badge/version-1.0.0-orange.svg)](../../releases/latest)

**English** · [Türkçe](READMEtr.md)

**ugilabs** · [ugilabs.com](https://ugilabs.com)

</div>

---

## Why another USB tool?

Rufus is an excellent program — but it assumes you know what you're doing.
"Partition scheme: GPT or MBR?" "Target system: UEFI or BIOS?" "File
system: NTFS or FAT32?" These questions have correct answers, but the
correct answer is invisible to someone who doesn't understand the
question.

Worse: the job isn't done when the USB is ready. The user still has to
boot from it — and how you do that depends on the motherboard brand.
This is where most people get stuck.

UWin closes both gaps. It makes the technical decisions for you, then
tells you how to enter your BIOS specifically for your hardware.

**If you already know what GPT means, you should probably keep using
Rufus.** UWin is for the person you're helping over the phone.

---

## What it does

### Before you start

- **Tells you whether the machine is ready for Windows 11.** Not just
  "incompatible" — it distinguishes a setting you can turn on in the BIOS
  from a hardware limit you can't do anything about. If TPM is off, it
  names the setting *as it appears on your board* (`AMD fTPM Switch` /
  `Intel Platform Trust Technology (PTT)`).

- **Asks why you're reinstalling.** Virus, slowness, new drive, version
  upgrade — the advice changes accordingly. This isn't an arbitrary
  question: someone reinstalling to remove a virus needs to be told
  "delete the partition", and someone who wants to keep their files needs
  to be told "don't". The same installer screen has two opposite correct
  answers, and only intent decides which.

- **Counts and shows the files that will be erased on this computer** —
  before the USB is prepared, while there's still time to back up.

### The ISO

- Downloads from **Microsoft's own servers**; no third-party mirrors.
- **Verifies with SHA-256.** A corrupt download never reaches the drive.
- **Cancellable, and resumes where it left off.**
- You can also pick an ISO you already have.

### Writing

- **Decides UEFI/Legacy and GPT/MBR for you.**
- **Solves the >4 GB `install.wim` problem** without the user ever knowing
  it existed. (FAT32 can't hold a file larger than 4 GB; modern Windows
  ISOs usually exceed it. UWin splits the file.)
- **Shows time remaining** — real minutes computed from actual throughput,
  not a percentage bar.
- **Reads the USB back and verifies it** after writing. The silent write
  failures of cheap flash drives get caught here, not in the BIOS.

### After writing

- **Shows a BIOS guide specific to your motherboard brand** — *and writes
  it to the USB*, so it's readable from a phone while the computer is off.
- **Writes recovery tools to the USB:** ready-to-run `.bat` files for
  repairing a Windows that won't boot (`bootrec`, `sfc`, `chkdsk`, safe
  mode, file access, version listing).

### Tools menu

| Tool | What it's for |
|------|---------------|
| **Reclaim USB** | Returns an installation USB to everyday use (exFAT + MBR) |
| **Health test** | Catches fake-capacity flash drives |
| **Add recovery tools** | Writes the repair tools to an existing USB |
| **Hardware report** | A full dump of this machine's hardware |

### Languages

**English and Turkish.** The language is asked at first launch
(pre-selected from your system language), then switches with one click
from the top right, and the choice is remembered.

Not just button labels: error messages, installation advice, the
compatibility report, the BIOS guide, and **the recovery tools written to
the USB** are all in the selected language.

---

## Install

1. Download `UWin.exe` from [**Releases**](../../releases/latest).
2. Double-click it.

No installer, no .NET prerequisite, nothing written to the registry. One
file, ~90 MB (the runtime is inside).

> **If Windows shows an "unknown publisher" warning:** *More info* → *Run
> anyway*. This means the program has no code-signing certificate —
> certificates are a paid annual product. The source is here; you can
> build it yourself.

> **Requires administrator rights.** Writing directly to a disk needs
> them; there is no way around it.

### Requirements

| | |
|---|---|
| OS | Windows 10 version 1809 (build 17763) or later |
| Architecture | x64 |
| Privileges | Administrator |
| USB drive | 8 GB minimum (depends on the ISO; the program tells you) |

---

## How to use it

Six steps, each explaining what's about to happen.

```
1. Welcome    →  Is this computer ready for Windows 11?
2. Purpose    →  Why are you reinstalling?
3. Source     →  Download an ISO, or pick your own
4. Drive      →  Which USB?
5. Confirm    →  Everything that will be erased, listed
6. Write      →  Write, verify, show the BIOS guide
```

**Step 5 is the one that matters.** Read it: everything on the USB drive
is erased and cannot be recovered.

---

## Safety

The worst thing this program can do is erase the wrong disk. The
protections are layered:

1. **The system disk can never be selected** — blocked in code, not
   behind a setting
2. **Internal disks and external disks over 128 GB are hidden by default**
3. **Re-validation before writing** — if the drive was swapped, the write
   doesn't start
4. **Type-the-name confirmation** for non-USB targets
5. **Data volume shown** — prevents the "I thought it was empty" mistake
6. **A single write point** — no code outside `YazmaServisi` touches a disk

Tests use an in-memory fake disk: **no test ever erases real hardware.**

---

## Architecture

Three layers, dependencies flowing one way:

```
Presentation (WinUI 3)  →  Service  →  Native (P/Invoke, WMI)
```

```
kaynak/
├── UWin.Cekirdek/          Business logic — no UI dependency
│   ├── Arayuzler/          Service contracts
│   ├── Modeller/           Record types
│   ├── Servisler/          19 services
│   ├── Yerel/              P/Invoke, WMI
│   └── Kaynaklar/          Metinler.{tr,en}.json,
│                           bios-rehberleri.{tr,en}.json
└── UWin.Uygulama/          WinUI 3 interface
    ├── Adimlar/            The six wizard steps
    └── Servisler/          Dependency wiring
```

Every destructive operation passes through the `IYerelDiskErisimi`
interface. That keeps the tests safe and makes the write path auditable
from a single place.

> **A note on the language:** the code, comments and identifiers are in
> Turkish, since that's the primary audience and the domain vocabulary
> stays consistent that way. `Cekirdek` is "core", `Uygulama` is "app",
> `Servisler` is "services", `Adimlar` is "steps". The public behaviour is
> fully bilingual.

---

## Building from source

```bash
git clone https://github.com/<user>/UWin.git
cd UWin

dotnet test                    # 319 tests
pwsh -File yayinla.ps1         # produces yayin/UWin.exe
```

You can also just double-click `yayinla.bat` — it runs the tests first and
refuses to produce a build if any fail.

You'll need the [.NET 10 SDK](https://dotnet.microsoft.com/download) and
Windows 10/11.

### Testing

```bash
dotnet test                                    # everything (319)
dotnet test testler/UWin.Cekirdek.Testler      # core only (251)
```

Every service was written test-first, with the test verified failing
before the implementation existed. Language coverage, text-key integrity
and service wiring are guarded by tests too — if a new service is added
and someone forgets to wire the shared text provider into it, a test names
the service that was missed.

Destructive paths can't be covered by unit tests;
[docs/manuel-dogrulama.md](docs/manuel-dogrulama.md) (17 sections, 170
items — in Turkish) is the manual checklist to run on real hardware before
a release.

---

## Known limits

**Microsoft's ISO download endpoint is not an official API.** It works and
it's verified, but it can break in two ways:

- **IP throttling:** too many download requests in a short window and
  Microsoft temporarily blocks the address (error 715-123130). The same
  happens with Rufus/Fido. UWin recognises this, doesn't retry, and tells
  the user to wait.
- **Flow changes:** if Microsoft changes the steps, the parsing breaks.
  The program doesn't crash — it says so and points the user at "choose
  your own ISO". The most fragile part, the regex patterns, is tested
  against a real captured response.

**BIOS settings cannot be changed from inside the program.** There's no
vendor-neutral way to write firmware settings, so UWin shows a
brand-specific guide instead.

**x64 only.** It won't run on ARM64 Windows devices.

---

## FAQ

**Will the files on my USB drive be erased?**
Yes, all of them, unrecoverably. The program says so plainly at step 5.

**Will the files on my computer be erased?**
Not while preparing the USB. They may be during the Windows installation
itself — that depends entirely on what you choose on the installer screen.
UWin tells you what to choose there based on your answer at step 2, and
lists what will be lost beforehand.

**How is this different from Rufus?**
Rufus has more options and is far more flexible. UWin asks fewer
questions, makes the decisions for you, and keeps helping *after* the USB
is written (BIOS guide, recovery tools). If you know Rufus, use Rufus.

**Can I write a Linux ISO?**
v1.0 is built for Windows ISOs only.

**Does it work offline?**
Yes, if you already have an ISO. Nothing but the download needs a network.

**My antivirus flags it.**
Any program that writes directly to a disk triggers this, Rufus included.
The source is right here — build it yourself if you'd rather.

---

## Contributing

Bug reports and suggestions: [open an issue](../../issues/new). What helps
most in a bug report:

- Windows version (`winver`)
- Motherboard / computer brand and model
- What you were trying to do, and what happened
- A screenshot of the error, if there is one

For code: comments and identifiers are Turkish ASCII, tests come first.
Please open an issue to discuss anything substantial before writing it.

---

## Documentation

- [Design document](docs/superpowers/specs/2026-09-07-uwin-design.md) (Turkish)
- [Implementation plan](docs/superpowers/plans/2026-09-07-uwin-v1.md) (Turkish)
- [Manual verification checklist](docs/manuel-dogrulama.md) (Turkish)

---

## License

[MIT](LICENSE) — use it, change it, ship it.

The software is provided "as is", with no warranty of any kind. This
program erases disks; be sure of what you're doing before you use it.
