# 🍳 Omletz Miner

**A GPU miner for CapStash (CAP) — WhirlpoolX / WPXF — pre-configured for the [Hash'n'Omletz](https://hashnomletz.com) pool.**

Omletz Miner is a streamlined fork of [ccminer](https://github.com/tpruvot/ccminer) (GPLv3)
that does one thing well: mine **CapStash (CAP)** on NVIDIA GPUs. It ships with a dark
terminal-style Windows GUI and a headless Linux engine for dedicated rigs, servers, and
compute-rental services like Vast.ai.

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
| **Windows (GUI)** | Windows 10 (1607+) or Windows 11, 64-bit. Self-contained — no .NET install needed. |
| **Linux (engine)** | 64-bit, any distro, with a CUDA-capable NVIDIA driver. No CUDA toolkit needed — the runtime is built into the binary. |

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

## 📦 Download

Grab the latest release from the [Releases page](https://github.com/Omletz/omletz-miner/releases).

| File | Platform | What it is |
|---|---|---|
| `OmletzMiner.exe` | Windows | GUI dashboard — double-click to start |
| `ccminer.exe` | Windows | Mining engine — must sit next to the GUI |
| `ccminer-linux` | Linux | Headless mining engine |
| `run-vast.sh` | Linux | One-line quick-start helper script |

---

## 🚀 Quick start — Windows (GUI)

1. Add the Defender exclusion (see above) so the engine isn't quarantined.
2. Put `OmletzMiner.exe` and `ccminer.exe` in the **same folder**.
3. Double-click **`OmletzMiner.exe`**.
4. Paste your **CAP wallet address** into the WALLET field.
5. (Optional) set a **Worker** name and an **Intensity** (leave on `Auto` if unsure).
6. Click **▶ START MINING**.

You'll see accepted shares roll in and your live hashrate, plus a pool dashboard
(blocks found, network stats) that refreshes automatically.

## 🚀 Quick start — Windows (command line)

```cmd
ccminer.exe -a whirlpoolx -o stratum+tcp://stratum.hashnomletz.com:10433 -u YOUR_CAP_WALLET.rig1 -p x --no-longpoll -i 22
```

---

## 🐧 Quick start — Linux

The Linux engine is a single self-contained binary — no CUDA toolkit install needed. You only
need two small runtime libraries (`libcurl4` and `libjansson4`) which are standard on most distros.

**1. Download the binary and make it executable:**
```bash
wget https://github.com/Omletz/omletz-miner/releases/latest/download/ccminer-linux -O ccminer
chmod +x ccminer
```

**2. Install runtime dependencies** (skip if already installed):
```bash
# Debian / Ubuntu / most Vast.ai images
sudo apt-get install -y libcurl4 libjansson4

# RHEL / Fedora / CentOS
sudo dnf install -y libcurl jansson
```

**3. Start mining:**
```bash
./ccminer -a whirlpoolx -o stratum+tcp://stratum.hashnomletz.com:10433 -u YOUR_CAP_WALLET.rig1 -p x --no-longpoll -i 22
```

> Replace `YOUR_CAP_WALLET` with your CAP address and `rig1` with any worker label you like.
> Multiple GPUs are used automatically. Restrict to specific cards with `-d 0,1`.
> Try intensity `-i 21` to `-i 23` — higher can be faster but may be less stable.

**To keep mining after closing your terminal** (use `screen` or `tmux`):
```bash
screen -S omletz
./ccminer -a whirlpoolx -o stratum+tcp://stratum.hashnomletz.com:10433 -u YOUR_CAP_WALLET.rig1 -p x --no-longpoll -i 22
# Detach: Ctrl+A then D   |   Reattach: screen -r omletz
```

### ☁️ Vast.ai quick start

Vast.ai instances work exactly like any Linux system. The easiest way:

```bash
wget https://github.com/Omletz/omletz-miner/releases/latest/download/ccminer-linux -O ccminer
wget https://github.com/Omletz/omletz-miner/releases/latest/download/run-vast.sh
bash run-vast.sh YOUR_CAP_WALLET worker 22
```

The `run-vast.sh` script handles `chmod`, installs the libs, and starts mining in one step.

---

## ⚙️ Configuration / common flags

| Flag | Meaning |
|---|---|
| `-o <url>` | Pool URL (pre-configured to Hash'n'Omletz) |
| `-u <wallet[.worker]>` | Your CAP wallet address, optional `.worker` label |
| `-p x` | Password (the pool ignores it — always `x`) |
| `-i <N>` | Intensity (`20`–`23`); higher = more threads, omit for auto |
| `-d <list>` | GPUs to use, e.g. `-d 0,1,2` |
| `--no-longpoll` | Recommended — keeps a single clean pool connection |

---

## 🛠️ Troubleshooting

| Problem | Fix |
|---|---|
| `ccminer.exe` disappears / flagged as virus | False positive — add a Defender exclusion or restore from quarantine (see above) |
| `Your nVidia driver is too old…` | Update NVIDIA driver (RTX 50-series needs 570+) |
| GUI says `ccminer.exe not found` | Keep `ccminer.exe` in the **same folder** as `OmletzMiner.exe` |
| All shares rejected | Check your wallet is a valid CAP address |
| `error while loading shared libraries: libcurl.so.4` | `sudo apt-get install -y libcurl4 libjansson4` |
| Linux binary won't run: `Permission denied` | `chmod +x ./ccminer` |

---

## 🔧 Building from source

- **Windows:** see [`ccminer/BUILD.md`](ccminer/BUILD.md) (Visual Studio 2022 + CUDA 12.8) and the GUI in `OmletzMiner/`
- **Linux:** see [`ccminer/BUILD-LINUX.md`](ccminer/BUILD-LINUX.md) (WSL2 or native Linux + CUDA 12.8)

---

## 📜 License & credits

Omletz Miner is licensed under the **GNU GPL v3**. It is a fork of
**[ccminer](https://github.com/tpruvot/ccminer)** by Christian Buchner, Christian H.,
tpruvot, and contributors. Full source is available; modified files are marked.

Built for the **Hash'n'Omletz** mining community — https://hashnomletz.com 🍳
