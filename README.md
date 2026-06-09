# 🍳 Omletz Miner

**A GPU miner for CapStash (CAP) — WhirlpoolX / WPXF — pre-configured for the [Hash'n'Omletz](https://hashnomletz.com) pool.**

Omletz Miner is a streamlined fork of [ccminer](https://github.com/tpruvot/ccminer) (GPLv3)
that does one thing well: mine **CapStash (CAP)** on NVIDIA GPUs. It ships with a dark
terminal-style Windows GUI and a headless Linux engine for mining rigs and compute-rental
services like Vast.ai.

- **Pool:** `stratum+tcp://stratum.hashnomletz.com:10433`
- **Algorithm:** WhirlpoolX (WPXF)
- **Coin:** CapStash (CAP)

---

## ✅ Compatibility

### Supported GPUs (NVIDIA only)

| GPU family | Example cards | Supported |
|---|---|---|
| Maxwell | GTX 750 Ti, 900 series | ✅ |
| Pascal | GTX 10 series (1060–1080 Ti) | ✅ |
| Volta | Titan V | ✅ |
| Turing | GTX 16 series, RTX 20 series | ✅ |
| Ampere | RTX 30 series | ✅ |
| Ada | RTX 40 series | ✅ |
| Blackwell | RTX 50 series | ✅ |
| Kepler | GTX 600 / 700 series | ❌ not supported |
| AMD / Intel | any | ❌ not supported |

One binary covers every card from the **GTX 750 (2014)** to the **RTX 5090 (2025)**.

### System requirements

| | Requirement |
|---|---|
| **GPU** | NVIDIA, Maxwell or newer (see table) |
| **NVIDIA driver** | A current driver. RTX 50-series (Blackwell) needs driver **570+**. |
| **Windows (GUI)** | Windows 10 (1607+) or Windows 11, 64-bit. The GUI is self-contained — no .NET install needed. |
| **Linux (engine)** | 64-bit, with a CUDA 12.x-capable driver. Used for rigs and Vast.ai. |

> Not supported: AMD/Intel GPUs, Kepler and older NVIDIA cards, macOS.

---

## ⚠️ Antivirus / Windows Defender note (please read)

**Windows Defender (and most antivirus) will flag this miner as a threat and may quarantine
or delete `ccminer.exe`. This is a false positive.** *Every* GPU miner triggers it, because
the same kind of mining code is also abused by malware. Omletz Miner is open source and built
from the published source — it is not malware.

To run it, add an exclusion for the folder you put it in:

1. **Windows Security** → **Virus & threat protection**
2. **Manage settings** → scroll to **Exclusions** → **Add or remove exclusions**
3. **Add an exclusion** → **Folder** → select the Omletz Miner folder

Or, in an **Administrator** PowerShell:
```powershell
Add-MpPreference -ExclusionPath "C:\path\to\OmletzMiner"
```

If it was already quarantined: **Windows Security → Protection history → Restore.**

---

## 📦 Download & contents

Your Omletz Miner folder should contain:

| File | What it is |
|---|---|
| `OmletzMiner.exe` | The Windows GUI (dark terminal dashboard) |
| `ccminer.exe` | The mining engine for Windows — must sit next to the GUI |
| `ccminer` | The mining engine for Linux (rigs / Vast.ai) |

---

## 🚀 Quick start — Windows (GUI)

1. Add the Defender exclusion (see above) so the engine isn't quarantined.
2. Double-click **`OmletzMiner.exe`**.
3. Paste your **CAP wallet address** into the WALLET field.
4. (Optional) set a **Worker** name and an **Intensity** (leave on `Auto` if unsure).
5. Click **▶ START MINING**.

You'll see accepted shares roll in and your live hashrate, plus a pool dashboard
(blocks found, network stats) that refreshes automatically.

## 🚀 Quick start — Windows (command line)

```cmd
ccminer.exe -a whirlpoolx -o stratum+tcp://stratum.hashnomletz.com:10433 -u YOUR_CAP_WALLET -p x --no-longpoll
```
Add a worker name with a dot: `-u YOUR_CAP_WALLET.rig1`. Set intensity with `-i 22`.

---

## ☁️ Mining on Vast.ai (rented GPUs)

CapStash can be mined on rented compute like **Vast.ai** — you rent a GPU instance and run
the Linux engine on it. The CUDA runtime is **built into** the binary, so the instance's CUDA
version doesn't matter — **any NVIDIA GPU instance with a working driver works.**

**1. Upload the Linux `ccminer` to your instance** (from your PC). Replace `PORT` and `HOST`
with the SSH details Vast.ai shows for your instance:
```bash
scp -P PORT ccminer root@HOST:/root/
```

**2. Connect to the instance and prepare it:**
```bash
ssh -p PORT root@HOST
chmod +x /root/ccminer
apt-get update && apt-get install -y libcurl4 libjansson4
```

**3. Start mining** (replace with your wallet; `screen` keeps it running after you disconnect):
```bash
screen -S omletz
/root/ccminer -a whirlpoolx -o stratum+tcp://stratum.hashnomletz.com:10433 -u YOUR_CAP_WALLET.vast -p x --no-longpoll -i 22
```
> `-i 22` is a good intensity for modern cards; try `21`–`23` to find the best
> hashrate for the GPU you rented. (Or just use `bash run-vast.sh WALLET worker 22`.)
Detach from `screen` with **Ctrl+A** then **D**; reattach later with `screen -r omletz`.

> Multiple GPUs on the instance are used automatically. Restrict to specific cards with
> `-d 0,1`. If you see `error while loading shared libraries: libcurl.so.4` (or `libjansson`),
> run the `apt-get install` line in step 2.

---

## ⚙️ Configuration / common flags

| Flag | Meaning |
|---|---|
| `-o <url>` | Pool URL (default already set to Hash'n'Omletz) |
| `-u <wallet[.worker]>` | Your CAP wallet, optional `.worker` label |
| `-p x` | Password (the pool ignores it — always `x`) |
| `-i <N>` | Intensity (e.g. `20`–`23`); higher can be faster, omit for auto |
| `-d <list>` | GPUs to use, e.g. `-d 0,1` |
| `--no-longpoll` | Keep a single pool connection (recommended) |

---

## 🛠️ Troubleshooting

| Problem | Fix |
|---|---|
| `ccminer.exe` disappears / flagged as virus | False positive — add a Defender exclusion or restore from quarantine (see above). |
| `Your nVidia driver is too old…` | Update your NVIDIA driver (RTX 50-series needs 570+). |
| GUI says `ccminer.exe not found` | Keep `ccminer.exe` in the **same folder** as `OmletzMiner.exe`. |
| All shares rejected | Check your wallet address is a valid CAP address. |
| Linux: `error while loading shared libraries: libcurl.so.4` | `apt-get install -y libcurl4 libjansson4` |

---

## 🔧 Building from source

- **Windows:** see [`ccminer/BUILD.md`](ccminer/BUILD.md) (Visual Studio 2022 + CUDA 12.8) and
  the GUI in `OmletzMiner/`.
- **Linux:** see [`ccminer/BUILD-LINUX.md`](ccminer/BUILD-LINUX.md) (WSL2 or any Linux + CUDA 12.8).

---

## 📜 License & credits

Omletz Miner is licensed under the **GNU GPL v3**. It is a fork of
**[ccminer](https://github.com/tpruvot/ccminer)** by Christian Buchner, Christian H.,
tpruvot, and contributors. Full source is available; modified files are marked.

Built for the **Hash'n'Omletz** mining community — https://hashnomletz.com 🍳
