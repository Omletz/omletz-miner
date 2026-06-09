using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OmletzMiner;

public class MainForm : Form
{
    // ── phosphor palette (matches hashnomletz.com terminal) ───────────────
    static readonly Color BgDark      = Color.FromArgb( 4,  8,  5);   // page bg
    static readonly Color BgPanel     = Color.FromArgb( 6, 12,  8);   // panel bg
    static readonly Color BgInput     = Color.FromArgb(10, 20, 12);   // input bg
    static readonly Color Green       = Color.FromArgb( 0, 255,  65); // #00ff41 phosphor
    static readonly Color GreenDim    = Color.FromArgb( 0, 200,  80); // body text
    static readonly Color GreenFaint  = Color.FromArgb( 0,  90,  30); // borders / brackets
    static readonly Color Amber       = Color.FromArgb(255, 176,   0); // accents / blocks
    static readonly Color AmberDim    = Color.FromArgb(200, 140,   0);
    static readonly Color RedReject   = Color.FromArgb(255,  80,  80);
    static readonly Color Muted       = Color.FromArgb( 70, 110,  80); // dim labels
    static readonly Color White       = Color.FromArgb(210, 235, 215);
    static readonly Color Blue        = Color.FromArgb(110, 200, 255);

    static readonly Font FTitle = new Font("Consolas", 21f,  FontStyle.Bold);
    static readonly Font FTag   = new Font("Consolas",  9f,  FontStyle.Regular);
    static readonly Font FLabel = new Font("Consolas",  8f,  FontStyle.Bold);
    static readonly Font FVal   = new Font("Consolas", 14f,  FontStyle.Bold);
    static readonly Font FValSm = new Font("Consolas", 11f,  FontStyle.Bold);
    static readonly Font FHash  = new Font("Consolas", 15f,  FontStyle.Bold);
    static readonly Font FBody  = new Font("Consolas",  9f,  FontStyle.Regular);
    static readonly Font FBtn   = new Font("Consolas",  9.5f, FontStyle.Bold);
    static readonly Font FLog   = new Font("Consolas",  9f,  FontStyle.Regular);

    // ── controls ──────────────────────────────────────────────────────────
    PictureBox  _logo   = null!;
    TextBox     _txtWallet = null!, _txtWorker = null!, _txtPool = null!;
    Button      _btnStart  = null!, _btnStop  = null!;
    ComboBox    _cboIntensity = null!;
    Label       _hashDisplay = null!, _sharesDisplay = null!, _lblStatus = null!;
    Label       _cursorBlink = null!;
    int         _blinkCtr;
    RichTextBox _log = null!;

    // dashboard value labels
    Label _valBlocks = null!, _valLastBlock = null!, _valPoolHash = null!,
          _valNetHash = null!, _valNetDiff = null!, _valMiners = null!,
          _poolStatus = null!, _refreshLbl = null!;

    // ── state ─────────────────────────────────────────────────────────────
    Process? _miner;
    int _accepted, _rejected;

    static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
    System.Windows.Forms.Timer _poll = null!;
    System.Windows.Forms.Timer _flickerTimer = null!;
    const string StatsUrl = "https://hashnomletz.com/api/cap/stats";

    // CRT flicker: panels read _flick to vary scanline darkness / dim veil
    static int _flick;
    static readonly Random _rnd = new Random();
    readonly List<TermPanel> _crtPanels = new();

    // per-GPU rate, e.g. "GPU #1: <name>, 1337.63 MH/s" — captures index + rate.
    // (Unit is exact so the "GPU #0: 1965 MHz ... kH/W" monitor line never matches.)
    static readonly Regex _reGpuRate = new Regex(
        @"GPU #(\d+):.*?([\d.]+)\s*(GH/s|MH/s|kH/s|H/s)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    // benchmark prints a combined "Total: X MH/s"
    static readonly Regex _reTotal = new Regex(
        @"Total:\s*([\d.]+)\s*(GH/s|MH/s|kH/s|H/s)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    readonly System.Collections.Generic.Dictionary<int, double> _gpuRatesMHs = new(); // per-GPU MH/s

    // ── CRT panel: dark bg + scanlines + corner brackets + ▶ title ─────────
    sealed class TermPanel : Panel
    {
        public string Title = "";
        public Color Accent = Green;
        public Color Bracket = GreenFaint;
        public bool Scanlines = true;
        public TermPanel()
        {
            BackColor = BgPanel;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint
                   | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics; var r = ClientRectangle;
            using (var b = new SolidBrush(BackColor)) g.FillRectangle(b, r);
            if (Scanlines)
            {
                int f = _flick;
                using (var sl = new Pen(Color.FromArgb(Math.Min(105 + f, 160), 0, 0, 0)))
                    for (int y = 0; y < r.Height; y += 3) g.DrawLine(sl, 0, y, r.Width, y);
                // faint dim veil only during a flicker dip — gives the CRT shimmer
                if (f > 0)
                    using (var veil = new SolidBrush(Color.FromArgb(Math.Min(f, 30), 0, 0, 0)))
                        g.FillRectangle(veil, r);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics; var r = ClientRectangle;
            g.SmoothingMode = SmoothingMode.None;
            int len = 13, o = 1;
            using var bp = new Pen(Bracket, 1f);
            g.DrawLines(bp, new[] { new Point(o, o + len), new Point(o, o), new Point(o + len, o) });
            g.DrawLines(bp, new[] { new Point(r.Width - o - 1 - len, o), new Point(r.Width - o - 1, o), new Point(r.Width - o - 1, o + len) });
            g.DrawLines(bp, new[] { new Point(o, r.Height - o - 1 - len), new Point(o, r.Height - o - 1), new Point(o + len, r.Height - o - 1) });
            g.DrawLines(bp, new[] { new Point(r.Width - o - 1 - len, r.Height - o - 1), new Point(r.Width - o - 1, r.Height - o - 1), new Point(r.Width - o - 1, r.Height - o - 1 - len) });
            if (!string.IsNullOrEmpty(Title))
            {
                using var tf = new Font("Consolas", 8.5f, FontStyle.Bold);
                using var tb = new SolidBrush(Accent);
                g.DrawString("▶ " + Title, tf, tb, 9, 5);
            }
        }
    }

    // ── constructor ───────────────────────────────────────────────────────
    public MainForm()
    {
        Text            = "OMLETZ MINER // MINING TERMINAL";
        Size            = new Size(940, 880);
        MinimumSize     = new Size(840, 720);
        BackColor       = BgDark;
        ForeColor       = GreenDim;
        StartPosition   = FormStartPosition.CenterScreen;
        DoubleBuffered  = true;

        BuildLayout();
        WireEvents();

        Load += (_, _) => StartPolling();
    }

    // ── layout ────────────────────────────────────────────────────────────
    void BuildLayout()
    {
        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5,
            Padding = new Padding(14), BackColor = BgDark,
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));   // header
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));  // dashboard
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));  // config
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));   // local readout
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // log
        Controls.Add(outer);

        outer.Controls.Add(BuildHeader(),    0, 0);
        outer.Controls.Add(BuildDashboard(), 0, 1);
        outer.Controls.Add(BuildConfig(),    0, 2);
        outer.Controls.Add(BuildReadout(),   0, 3);
        outer.Controls.Add(BuildLogPanel(),  0, 4);
    }

    Control BuildHeader()
    {
        var p = new Panel { Dock = DockStyle.Fill, BackColor = BgDark, Margin = new Padding(0, 0, 0, 6) };

        _logo = new PictureBox
        {
            Size = new Size(64, 64), Location = new Point(2, 6),
            SizeMode = PictureBoxSizeMode.Zoom, BackColor = Color.Transparent,
        };
        TryLoadLogo();
        p.Controls.Add(_logo);

        p.Controls.Add(new Label
        {
            Text = "OMLETZ MINER", Font = FTitle, ForeColor = Green,
            BackColor = Color.Transparent, AutoSize = true, Location = new Point(78, 8),
        });
        // idle terminal cursor — blinks via the flicker timer (no separate timer)
        _cursorBlink = new Label
        {
            Text = "▮", Font = new Font("Consolas", 19f, FontStyle.Bold), ForeColor = Green,
            BackColor = Color.Transparent, AutoSize = true, Location = new Point(300, 12),
        };
        p.Controls.Add(_cursorBlink);
        p.Controls.Add(new Label
        {
            Text = "// CAPSTASH (CAP) · WHIRLPOOLX/WPXF · MINING OPERATIONS TERMINAL //",
            Font = FTag, ForeColor = AmberDim, BackColor = Color.Transparent,
            AutoSize = true, Location = new Point(80, 48),
        });

        p.Controls.Add(new Panel { Height = 1, Dock = DockStyle.Bottom, BackColor = GreenFaint });
        return p;
    }

    Control BuildDashboard()
    {
        var panel = new TermPanel { Dock = DockStyle.Fill, Title = "POOL · CAP NETWORK [SOLO]", Margin = new Padding(0, 0, 0, 6) };

        _poolStatus = new Label
        {
            Text = "● CONNECTING", Font = FLabel, ForeColor = Muted, BackColor = BgPanel,
            AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(panel.Width - 230, 6),
        };
        _refreshLbl = new Label
        {
            Text = "SYNC --:--:--", Font = FLabel, ForeColor = Muted, BackColor = BgPanel,
            AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, Location = new Point(panel.Width - 122, 6),
        };
        panel.Controls.Add(_poolStatus);
        panel.Controls.Add(_refreshLbl);

        // absolute-positioned cells (solid bg) so scanlines flicker in the gaps
        // around them while the text itself stays rock-steady
        int y = 42;
        _valBlocks    = AddStat(panel, "BLOCKS FOUND",   16,  y, Amber);
        _valLastBlock = AddStat(panel, "LAST BLOCK",     118, y, Green);
        _valPoolHash  = AddStat(panel, "POOL HASHRATE",  300, y, Green);
        _valNetHash   = AddStat(panel, "NETWORK HASH",   470, y, GreenDim);
        _valNetDiff   = AddStat(panel, "NET DIFFICULTY", 626, y, GreenDim);
        _valMiners    = AddStat(panel, "MINERS",         772, y, Green);

        _crtPanels.Add(panel);
        return panel;
    }

    Label AddStat(Control parent, string label, int x, int y, Color valColor)
    {
        parent.Controls.Add(new Label
        {
            Text = label, Font = FLabel, ForeColor = Muted, BackColor = BgPanel,
            AutoSize = true, Location = new Point(x, y),
        });
        var value = new Label
        {
            Text = "—", Font = FValSm, ForeColor = valColor, BackColor = BgPanel,
            AutoSize = true, Location = new Point(x, y + 16),
        };
        parent.Controls.Add(value);
        return value;
    }

    Control BuildConfig()
    {
        var p = new TermPanel { Dock = DockStyle.Fill, Title = "MINER CONFIG", Margin = new Padding(0, 0, 0, 6) };

        p.Controls.Add(MakeLabel("WALLET ADDRESS", new Point(14, 30)));
        _txtWallet = MakeInput(new Point(14, 48), 500);
        _txtWallet.PlaceholderText = "Your CAP wallet address";

        p.Controls.Add(MakeLabel("WORKER (optional)", new Point(528, 30)));
        _txtWorker = MakeInput(new Point(528, 48), 168);
        _txtWorker.PlaceholderText = "rig1";

        p.Controls.Add(MakeLabel("INTENSITY", new Point(708, 30)));
        _cboIntensity = new ComboBox
        {
            Location = new Point(708, 47), Width = 90, Font = FBody,
            BackColor = BgInput, ForeColor = White, FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        _cboIntensity.Items.AddRange(new object[] { "Auto", "20", "21", "22", "23" });
        _cboIntensity.SelectedIndex = 0;
        p.Controls.Add(_cboIntensity);

        p.Controls.Add(MakeLabel("POOL URL", new Point(14, 86)));
        _txtPool = MakeInput(new Point(14, 104), 600);
        _txtPool.Text = "stratum+tcp://stratum.hashnomletz.com:10433";

        _btnStart = MakeButton("[ ▶ START MINING ]", new Point(626, 100), new Size(160, 34), Green, GreenFaint);
        _btnStop  = MakeButton("[ ■ STOP ]", new Point(626, 100), new Size(160, 34), RedReject, Color.FromArgb(70, 20, 20));
        _btnStop.Visible = false;

        p.Controls.Add(_txtWallet); p.Controls.Add(_txtWorker); p.Controls.Add(_txtPool);
        p.Controls.Add(_btnStart);  p.Controls.Add(_btnStop);
        _crtPanels.Add(p);
        return p;
    }

    Control BuildReadout()
    {
        var p = new TermPanel { Dock = DockStyle.Fill, Title = "LOCAL STATUS", Margin = new Padding(0, 0, 0, 6) };

        p.Controls.Add(MakeLabel("HASHRATE", new Point(14, 26)));
        _hashDisplay = new Label { Text = "—", Font = FHash, ForeColor = Green, BackColor = BgPanel, AutoSize = true, Location = new Point(14, 40) };

        p.Controls.Add(MakeLabel("ACCEPTED / REJECTED", new Point(320, 26)));
        _sharesDisplay = new Label { Text = "0 / 0", Font = FHash, ForeColor = Amber, BackColor = BgPanel, AutoSize = true, Location = new Point(320, 40) };

        _lblStatus = new Label { Text = "● IDLE", Font = FValSm, ForeColor = Muted, BackColor = BgPanel, AutoSize = true, Location = new Point(640, 44) };

        p.Controls.Add(_hashDisplay); p.Controls.Add(_sharesDisplay); p.Controls.Add(_lblStatus);
        _crtPanels.Add(p);
        return p;
    }

    Control BuildLogPanel()
    {
        var p = new TermPanel { Dock = DockStyle.Fill, Title = "MINER OUTPUT", Scanlines = false };
        _log = new RichTextBox
        {
            Dock = DockStyle.Fill, BackColor = BgDark, ForeColor = GreenDim, Font = FLog,
            ReadOnly = true, BorderStyle = BorderStyle.None, ScrollBars = RichTextBoxScrollBars.Vertical,
            WordWrap = false, HideSelection = false, Margin = new Padding(0),
        };
        var host = new Panel { Dock = DockStyle.Fill, BackColor = BgDark, Padding = new Padding(8, 26, 8, 8) };
        host.Controls.Add(_log);
        p.Controls.Add(host);
        return p;
    }

    // ── helpers ───────────────────────────────────────────────────────────
    Label MakeLabel(string text, Point loc) => new Label
    {
        Text = text, Font = FLabel, ForeColor = Muted, BackColor = BgPanel, AutoSize = true, Location = loc,
    };

    TextBox MakeInput(Point loc, int width) => new TextBox
    {
        Location = loc, Width = width, Font = FBody, BackColor = BgInput, ForeColor = White, BorderStyle = BorderStyle.FixedSingle,
    };

    Button MakeButton(string text, Point loc, Size sz, Color fg, Color border)
    {
        var b = new Button
        {
            Text = text, Location = loc, Size = sz, Font = FBtn, ForeColor = fg,
            BackColor = BgInput, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor = border;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.MouseOverBackColor = Color.FromArgb(border.R / 3 + 6, border.G / 3 + 12, border.B / 3 + 6);
        return b;
    }

    void TryLoadLogo()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("OmletzMiner.Resources.logo.png");
            if (stream != null) { _logo.Image = Image.FromStream(stream); return; }
        }
        catch { }
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";
        var path = Path.Combine(exeDir, "logo.png");
        if (File.Exists(path)) { _logo.Image = Image.FromFile(path); return; }
        var bmp = new Bitmap(64, 64);
        using var g = Graphics.FromImage(bmp);
        g.Clear(BgDark);
        using var f = new Font("Consolas", 8f, FontStyle.Bold);
        g.DrawString("H\\O", f, new SolidBrush(Green), 4, 22);
        _logo.Image = bmp;
    }

    // ── pool dashboard polling ────────────────────────────────────────────
    void StartPolling()
    {
        _poll = new System.Windows.Forms.Timer { Interval = 15000 };
        _poll.Tick += async (_, _) => await FetchStats();
        _poll.Start();
        _ = FetchStats();

        // subtle wasteland-CRT flicker: vary scanline darkness, occasional dip
        _flickerTimer = new System.Windows.Forms.Timer { Interval = 110 };
        _flickerTimer.Tick += (_, _) =>
        {
            int roll = _rnd.Next(100);
            _flick = roll < 86 ? _rnd.Next(0, 6)      // mostly steady
                   : roll < 97 ? _rnd.Next(8, 18)     // light shimmer
                               : _rnd.Next(18, 30);   // rare dip
            // Invalidate(false): repaint only the panel background (scanlines
            // flicker) WITHOUT repainting child controls — text stays steady.
            foreach (var pnl in _crtPanels) pnl.Invalidate(false);
            // blink the terminal cursor ~every 550ms (5 × 110ms ticks)
            if (++_blinkCtr % 5 == 0)
                _cursorBlink.ForeColor = _cursorBlink.ForeColor == Green ? BgDark : Green;
        };
        _flickerTimer.Start();
    }

    async Task FetchStats()
    {
        try
        {
            var json = await _http.GetStringAsync(StatsUrl);
            using var doc = JsonDocument.Parse(json);
            var r = doc.RootElement;

            double poolH = GetD(r, "pool_hashrate");
            double netH  = GetD(r, "network_hashrate");
            double netD  = GetD(r, "network_difficulty");
            int miners   = GetI(r, "miners");
            int blocks   = GetI(r, "blocks_found");

            long lbH = 0, lbTs = 0;
            if (r.TryGetProperty("last_block", out var lb) && lb.ValueKind == JsonValueKind.Object)
            {
                if (lb.TryGetProperty("height", out var he)) lbH = he.GetInt64();
                if (lb.TryGetProperty("timestamp", out var te)) lbTs = te.GetInt64();
            }

            _valBlocks.Text    = blocks.ToString();
            _valLastBlock.Text = lbH > 0 ? $"#{lbH}  ·  {RelTime(lbTs)}" : "—";
            _valPoolHash.Text  = FmtHash(poolH);
            _valNetHash.Text   = FmtHash(netH);
            _valNetDiff.Text   = FmtNum(netD);
            _valMiners.Text    = miners.ToString();

            _poolStatus.Text = "● POOL ONLINE";
            _poolStatus.ForeColor = Green;
            _refreshLbl.Text = $"SYNC {DateTime.Now:HH:mm:ss}";
        }
        catch
        {
            _poolStatus.Text = "● POOL OFFLINE";
            _poolStatus.ForeColor = RedReject;
        }
    }

    static double GetD(JsonElement e, string k) => e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
    static int    GetI(JsonElement e, string k) => e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    static string FmtHash(double hs)
    {
        string[] u = { "H/s", "kH/s", "MH/s", "GH/s", "TH/s", "PH/s" };
        int i = 0; while (hs >= 1000 && i < u.Length - 1) { hs /= 1000; i++; }
        return $"{hs:0.##} {u[i]}";
    }

    static double ToMHs(double v, string unit) => unit.ToLowerInvariant() switch
    {
        "gh/s" => v * 1000.0,
        "kh/s" => v / 1000.0,
        "h/s"  => v / 1_000_000.0,
        _      => v,   // mh/s
    };

    static string FmtRate(double mhs)
    {
        if (mhs >= 1000)  return $"{mhs / 1000:0.##} GH/s";
        if (mhs >= 1)     return $"{mhs:0.##} MH/s";
        if (mhs >= 0.001) return $"{mhs * 1000:0.##} kH/s";
        return $"{mhs * 1_000_000:0.##} H/s";
    }

    static string FmtNum(double d)
    {
        if (d >= 1e12) return $"{d / 1e12:0.##}T";
        if (d >= 1e9)  return $"{d / 1e9:0.##}G";
        if (d >= 1e6)  return $"{d / 1e6:0.##}M";
        if (d >= 1e3)  return $"{d / 1e3:0.##}K";
        return $"{d:0.##}";
    }

    static string RelTime(long ms)
    {
        if (ms <= 0) return "—";
        var t = DateTimeOffset.FromUnixTimeMilliseconds(ms);
        var d = DateTimeOffset.UtcNow - t;
        if (d.TotalSeconds < 0) return "now";
        if (d.TotalSeconds < 60) return $"{(int)d.TotalSeconds}s ago";
        if (d.TotalMinutes < 60) return $"{(int)d.TotalMinutes}m ago";
        if (d.TotalHours   < 24) return $"{(int)d.TotalHours}h ago";
        return $"{(int)d.TotalDays}d ago";
    }

    // ── events ────────────────────────────────────────────────────────────
    void WireEvents()
    {
        _btnStart.Click += OnStartClick;
        _btnStop.Click  += OnStopClick;
        FormClosing     += (_, _) => StopMiner("window close");
    }

    void OnStartClick(object? sender, EventArgs e)
    {
        var wallet = _txtWallet.Text.Trim();
        var worker = _txtWorker.Text.Trim();
        var pool   = _txtPool.Text.Trim();

        if (string.IsNullOrEmpty(wallet)) { AppendLog("[ERROR] Please enter your wallet address.", RedReject); return; }
        if (string.IsNullOrEmpty(pool)) pool = "stratum+tcp://stratum.hashnomletz.com:10433";

        var user   = string.IsNullOrEmpty(worker) ? wallet : $"{wallet}.{worker}";
        var ccPath = FindCcminer();
        if (ccPath == null)
        {
            AppendLog("[ERROR] ccminer.exe not found next to OmletzMiner.exe.", RedReject);
            AppendLog("        Place ccminer.exe in the same folder as this application.", RedReject);
            return;
        }

        var intensity = _cboIntensity.SelectedItem as string ?? "Auto";

        _accepted = 0; _rejected = 0; _gpuRatesMHs.Clear(); UpdateShares(); _log.Clear();
        AppendLog("[Omletz Miner] Starting ccminer...", Green);
        AppendLog($"[Omletz Miner] Pool      : {pool}", GreenDim);
        AppendLog($"[Omletz Miner] User      : {user}", GreenDim);
        AppendLog("[Omletz Miner] Algo      : whirlpoolx (WPXF)", GreenDim);
        AppendLog($"[Omletz Miner] Intensity : {intensity}", GreenDim);
        AppendLog("", GreenDim);

        // --no-longpoll: keep a single stratum connection (avoids a 2nd pool session)
        var args = $"-a whirlpoolx -o {pool} -u {user} -p x --no-longpoll --no-color";
        if (intensity != "Auto")
            args += $" -i {intensity}";

        var psi = new ProcessStartInfo
        {
            FileName = ccPath, Arguments = args, UseShellExecute = false,
            RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(ccPath)!,
        };

        _miner = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _miner.OutputDataReceived += OnMinerOutput;
        _miner.ErrorDataReceived  += OnMinerOutput;
        _miner.Exited             += OnMinerExited;
        _miner.Start();
        _miner.BeginOutputReadLine();
        _miner.BeginErrorReadLine();
        SetMiningState(true);
    }

    void OnStopClick(object? sender, EventArgs e) => StopMiner("user request");

    void StopMiner(string reason)
    {
        if (_miner == null || _miner.HasExited) return;
        AppendLog($"\n[Omletz Miner] Stopping miner ({reason})...", Muted);
        try { _miner.Kill(entireProcessTree: true); _miner.WaitForExit(3000); } catch { }
        SetMiningState(false);
    }

    void OnMinerOutput(object sender, DataReceivedEventArgs e)
    {
        if (e.Data == null) return;
        var line = e.Data;
        if (InvokeRequired) { BeginInvoke(() => ProcessLine(line)); return; }
        ProcessLine(line);
    }

    void OnMinerExited(object? sender, EventArgs e)
    {
        if (InvokeRequired) { BeginInvoke(() => { AppendLog("[Omletz Miner] ccminer process exited.", Muted); SetMiningState(false); }); return; }
        AppendLog("[Omletz Miner] ccminer process exited.", Muted);
        SetMiningState(false);
    }

    void ProcessLine(string line)
    {
        line = Regex.Replace(line, @"\x1B\[[0-9;]*[mK]", "");
        var colour = ClassifyLine(line, out bool isAccepted, out bool isRejected);
        if (isAccepted) { _accepted++; UpdateShares(); }
        if (isRejected) { _rejected++; UpdateShares(); }

        // hashrate: a benchmark "Total:" line is already a sum; otherwise track
        // each "GPU #N:" rate and show the combined total (multi-GPU aware).
        var mt = _reTotal.Match(line);
        if (mt.Success)
        {
            _hashDisplay.Text = FmtRate(ToMHs(double.Parse(mt.Groups[1].Value), mt.Groups[2].Value));
            _hashDisplay.ForeColor = Green;
        }
        else
        {
            var mg = _reGpuRate.Match(line);
            if (mg.Success)
            {
                int idx = int.Parse(mg.Groups[1].Value);
                _gpuRatesMHs[idx] = ToMHs(double.Parse(mg.Groups[2].Value), mg.Groups[3].Value);
                double sum = 0; foreach (var v in _gpuRatesMHs.Values) sum += v;
                _hashDisplay.Text = FmtRate(sum) + (_gpuRatesMHs.Count > 1 ? $"  ({_gpuRatesMHs.Count} GPU)" : "");
                _hashDisplay.ForeColor = Green;
            }
        }

        AppendLog(line, colour);
    }

    Color ClassifyLine(string line, out bool accepted, out bool rejected)
    {
        accepted = false; rejected = false;
        var lo = line.ToLowerInvariant();
        if (lo.Contains("accepted") || lo.Contains("yes!"))
        {
            accepted = !lo.Contains("reject") && !lo.Contains("booooo");
            rejected = lo.Contains("reject") || lo.Contains("booooo");
            return accepted ? Amber : RedReject;
        }
        if (lo.Contains("rejected") || lo.Contains("invalid") || lo.Contains("does not validate") || lo.Contains("error"))
        { rejected = lo.Contains("reject"); return RedReject; }
        if (lo.Contains("mh/s") || lo.Contains("kh/s") || lo.Contains("gh/s")) return GreenDim;
        if (lo.Contains("stratum") || lo.Contains("connect") || lo.Contains("pool")) return Blue;
        if (lo.Contains("block") || lo.Contains("diff") || lo.Contains("job")) return Color.FromArgb(170, 230, 170);
        return GreenDim;
    }

    void AppendLog(string text, Color color)
    {
        const int MaxLines = 2000;
        _log.SuspendLayout();
        if (_log.Lines.Length > MaxLines)
        {
            int cutPos = _log.GetFirstCharIndexFromLine(_log.Lines.Length - MaxLines / 2);
            _log.Select(0, cutPos); _log.SelectedText = "";
        }
        _log.SelectionStart = _log.TextLength; _log.SelectionLength = 0; _log.SelectionColor = color;
        _log.AppendText(text + "\n");
        bool nearBottom = _log.SelectionStart >= _log.TextLength - 200;
        if (nearBottom) _log.ScrollToCaret();
        _log.ResumeLayout();
    }

    void SetMiningState(bool running)
    {
        _btnStart.Visible = !running;
        _btnStop.Visible  = running;
        _txtWallet.Enabled = !running; _txtWorker.Enabled = !running; _txtPool.Enabled = !running;
        _cboIntensity.Enabled = !running;
        if (running) { _lblStatus.Text = "● MINING"; _lblStatus.ForeColor = Green; }
        else { _lblStatus.Text = "● IDLE"; _lblStatus.ForeColor = Muted; _hashDisplay.Text = "—"; _gpuRatesMHs.Clear(); }
    }

    void UpdateShares()
    {
        _sharesDisplay.Text = $"{_accepted} / {_rejected}";
        _sharesDisplay.ForeColor = _rejected > 0 ? Color.FromArgb(255, 150, 80) : Amber;
    }

    static string? FindCcminer()
    {
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath) ?? ".";
        var c = Path.Combine(exeDir, "ccminer.exe");
        if (File.Exists(c)) return c;
        var parent = Path.GetDirectoryName(exeDir) ?? ".";
        c = Path.Combine(parent, "ccminer", "x64", "Release", "ccminer.exe");
        if (File.Exists(c)) return c;
        c = Path.Combine(parent, "ccminer", "Release", "ccminer.exe");
        if (File.Exists(c)) return c;
        return null;
    }
}
