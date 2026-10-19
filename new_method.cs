public void OpenAmountPad() {
    if (popupStack.Count > 0 || commonDialogDepth > 0) { ShowMenu(); return; }
    GameCommand c = Selected();
    if (c == null || c.@params == null || c.@params.Count == 0 || c.@params[0].type == "select") {
     Notice("Choose a numeric feature such as Add hero gold, Royal treasury or Guild Seals first.", "Exact amount", MessageBoxButtons.OK, MessageBoxIcon.Information);
     return;
    }
    Parameter param = c.@params[0];
    if (param.max > 1000000000 || param.min < 0 || param.min > param.max) { Notice("This parameter cannot use the exact amount pad.", "CROWN"); return; }

    PopupForm f = new PopupForm();
    f.Text = "CROWN — Exact amount: " + c.title;
    f.ClientSize = new Size(600, 560);
    f.FormBorderStyle = FormBorderStyle.FixedDialog;
    f.MaximizeBox = false; f.MinimizeBox = false;
    f.ShowInTaskbar = false; f.KeyPreview = true;
    f.BackColor = Albion.Leather; f.ForeColor = Albion.Text; f.Font = Albion.Body;
    f.StartPosition = FormStartPosition.CenterParent;

    f.Controls.Add(MakeLabel("EXACT AMOUNT", Albion.H1, Albion.GoldBright, 22, 14, 500, 34));
    f.Controls.Add(MakeLabel(c.title + " — This sets the parameter value for EXECUTE or QUEUE. It does not change the game until you confirm.", Albion.Small, Albion.Muted, 22, 50, 550, 40));

    // Live Lua preview
    TextBox luaPreview = new TextBox();
    luaPreview.SetBounds(22, 90, 552, 80);
    luaPreview.Multiline = true; luaPreview.ReadOnly = true; luaPreview.ScrollBars = ScrollBars.Vertical;
    luaPreview.BackColor = Albion.LeatherHi; luaPreview.ForeColor = Albion.GoldDim; luaPreview.Font = new Font("Consolas", 8.5f);
    luaPreview.BorderStyle = BorderStyle.FixedSingle;
    f.Controls.Add(luaPreview);

    NumericUpDown amount = new NumericUpDown();
    amount.SetBounds(22, 180, 552, 38);
    amount.Minimum = (decimal)param.min;
    amount.Maximum = (decimal)param.max;
    amount.DecimalPlaces = param.step < 1 ? 2 : 0;
    amount.Increment = (decimal)Math.Max(param.step, 0.01);
    amount.ThousandsSeparator = true;
    amount.Font = Albion.Numeral;
    amount.BackColor = Albion.PanelHi; amount.ForeColor = Albion.GoldBright;
    double initial = param.value;
    try { initial = ParameterValue(c); } catch { }
    amount.Value = (decimal)Math.Max(param.min, Math.Min(param.max, initial));
    f.Controls.Add(amount);

    Action refreshPreview = delegate {
     try {
      string preview = Compile(c, (double)amount.Value);
      luaPreview.Text = "-- Preview of the Lua that will be sent:\r\n" + preview;
     } catch (Exception ex) { luaPreview.Text = "-- " + ex.Message; }
    };
    amount.ValueChanged += delegate { refreshPreview(); };
    refreshPreview();

    f.Controls.Add(MakeLabel("QUICK PRESETS — CLICK TO APPLY, THEN USE OR QUEUE", Albion.H3, Albion.Gold, 22, 226, 550, 22));
    FlowLayoutPanel quicks = new FlowLayoutPanel();
    quicks.SetBounds(20, 254, 560, 96);
    quicks.WrapContents = true; quicks.AutoScroll = false; quicks.BackColor = Color.Transparent;
    f.Controls.Add(quicks);

    // Smart presets based on the command type
    int[] quickVals = GetSmartPresets(c.id, param);
    foreach (int quickVal in quickVals) {
     if (quickVal < param.min || quickVal > param.max) continue;
     int captured = quickVal;
     AlbionButton b = new AlbionButton();
     b.Text = quickVal.ToString("N0"); b.Style = AlbionButtonStyle.Quiet; b.Font = Albion.SmallBold;
     b.Size = new Size(96, 34); b.Margin = new Padding(4);
     b.Click += delegate { amount.Value = captured; };
     quicks.Controls.Add(b);
    }

    AlbionSlider fine = new AlbionSlider();
    fine.SetBounds(22, 358, 552, 40);
    bool fineUsable = param.max <= 100000 && param.min >= 0 && (param.max - param.min) > 10;
    if (fineUsable) { fine.Minimum = param.min; fine.Maximum = param.max; fine.Step = Math.Max(param.step, 1); fine.Value = (double)amount.Value; }
    fine.Visible = fineUsable;
    f.Controls.Add(fine);

    Label fineLabel = MakeLabel("FINE CONTROL — drag or arrow keys, mouse wheel also works", Albion.Small, Albion.Muted, 22, 396, 550, 20);
    fineLabel.Visible = fineUsable;
    f.Controls.Add(fineLabel);

    if (fineUsable) {
     fine.ValueChanged += delegate { decimal v = (decimal)fine.Value; if (v < amount.Minimum) v = amount.Minimum; if (v > amount.Maximum) v = amount.Maximum; amount.Value = v; };
     amount.ValueChanged += delegate { double v = (double)amount.Value; if (v >= fine.Minimum && v <= fine.Maximum) fine.Value = v; };
    }

    // Parameter info
    f.Controls.Add(MakeLabel(
     "Range: " + param.min.ToString("0.#####", CultureInfo.InvariantCulture) + " – " + param.max.ToString("0.#####", CultureInfo.InvariantCulture) +
     "  |  Step: " + (param.step > 0 ? param.step.ToString("0.#####", CultureInfo.InvariantCulture) : "1") +
     (param.type == "number" ? "" : "  (" + param.type + ")"),
     Albion.Tiny, Albion.Muted, 22, 420, 550, 20));

    AlbionButton use = new AlbionButton(); use.Text = "USE VALUE"; use.Style = AlbionButtonStyle.Gold;
    use.SetBounds(22, 450, 160, 44);
    use.Click += delegate { SetEditorValue(amount.Value); f.Close(); };
    f.Controls.Add(use);

    AlbionButton queueIt = new AlbionButton(); queueIt.Text = "USE + QUEUE"; queueIt.Style = AlbionButtonStyle.Primary;
    queueIt.SetBounds(190, 450, 190, 44);
    queueIt.Click += delegate { SetEditorValue(amount.Value); QueueSelected(); f.Close(); };
    f.Controls.Add(queueIt);

    AlbionButton cancel = new AlbionButton(); cancel.Text = "CANCEL"; cancel.Style = AlbionButtonStyle.Ghost;
    cancel.SetBounds(392, 450, 180, 44);
    cancel.Click += delegate { f.Close(); };
    f.Controls.Add(cancel);

    f.Controls.Add(MakeLabel("KEYBOARD:  ←/→ adjust by step   ↑/↓ adjust by 10×step   Home/End jump to min/max   Enter = USE VALUE   Ctrl+Enter = USE + QUEUE   Esc = CANCEL", Albion.Tiny, Albion.Muted, 22, 500, 550, 28));
    f.Controls.Add(MakeLabel("PAD:  LB/RB ±1 step  •  LT/RT hold for 10×/100×  •  D-pad Left/Right presets  •  A = USE VALUE  •  X = USE + QUEUE  •  B = CANCEL", Albion.Tiny, Albion.Muted, 22, 528, 550, 28));

    int quickIndex = -1;
    RegisterPad(f, delegate(Native.XState st, ushort press) {
     int multiplier = st.rt >= 180 ? 100 : st.lt >= 180 ? 10 : 1;
     double step = (double)amount.Increment * multiplier;
     if ((press & Native.PAD_LB) != 0) amount.Value = Math.Max(amount.Minimum, amount.Value - (decimal)step);
     if ((press & Native.PAD_RB) != 0) amount.Value = Math.Min(amount.Maximum, amount.Value + (decimal)step);
     if ((press & Native.PAD_DPAD_UP) != 0) amount.Value = Math.Min(amount.Maximum, amount.Value + (decimal)(1000 * multiplier));
     if ((press & Native.PAD_DPAD_DOWN) != 0) amount.Value = Math.Max(amount.Minimum, amount.Value - (decimal)(1000 * multiplier));
     if ((press & 0x000C) != 0 && quicks.Controls.Count > 0) {
      quickIndex = Math.Max(0, Math.Min(quicks.Controls.Count - 1, quickIndex + (((press & Native.PAD_DPAD_RIGHT) != 0) ? 1 : -1)));
      ((AlbionButton)quicks.Controls[quickIndex]).PerformClick();
      quicks.Controls[quickIndex].Focus();
     }
     if ((press & Native.PAD_A) != 0) { use.PerformClick(); return; }
     if ((press & Native.PAD_X) != 0) { queueIt.PerformClick(); return; }
    });

    // Keyboard shortcuts for the amount pad
    f.KeyDown += delegate(object s, KeyEventArgs e) {
     double step = (double)amount.Increment;
     if (e.Control) step *= 10;
     if (e.Shift) step *= 100;
     if (e.KeyCode == Keys.Left) { amount.Value = Math.Max(amount.Minimum, amount.Value - (decimal)step); e.Handled = true; }
     else if (e.KeyCode == Keys.Right) { amount.Value = Math.Min(amount.Maximum, amount.Value + (decimal)step); e.Handled = true; }
     else if (e.KeyCode == Keys.Up) { amount.Value = Math.Min(amount.Maximum, amount.Value + (decimal)(step * 10)); e.Handled = true; }
     else if (e.KeyCode == Keys.Down) { amount.Value = Math.Max(amount.Minimum, amount.Value - (decimal)(step * 10)); e.Handled = true; }
     else if (e.KeyCode == Keys.Home) { amount.Value = amount.Minimum; e.Handled = true; }
     else if (e.KeyCode == Keys.End) { amount.Value = amount.Maximum; e.Handled = true; }
     else if (e.KeyCode == Keys.Enter && e.Control) { queueIt.PerformClick(); e.Handled = true; }
     else if (e.KeyCode == Keys.Enter) { use.PerformClick(); e.Handled = true; }
     else if (e.KeyCode == Keys.Escape) { cancel.PerformClick(); e.Handled = true; }
    };

    f.AcceptButton = use; f.CancelButton = cancel;
    ShowPopup(f);
   }

   int[] GetSmartPresets(string commandId, Parameter p) {
    // Command-specific smart presets
    switch (commandId) {
     case "gold": return new int[] { 1000, 5000, 10000, 25000, 50000, 100000, 250000, 500000, 1000000, 5000000 };
     case "treasury": return new int[] { 50000, 100000, 250000, 500000, 1000000, 2500000, 5000000, 10000000 };
     case "seals": return new int[] { 10, 50, 100, 250, 500, 1000, 2500, 5000, 10000 };
     case "renown": return new int[] { 1000, 5000, 10000, 25000, 50000, 100000 };
     case "xp-strength": case "xp-skill": case "xp-will": case "xp-general": return new int[] { 10000, 50000, 100000, 250000, 500000, 1000000 };
     case "followers-up": case "followers-down": return new int[] { 5000, 10000, 25000, 50000, 100000 };
     case "spawn-wolves": case "spawn-renegades": case "spawn-minions": return new int[] { 1, 2, 3, 5, 8, 10, 15, 20 };
     case "spawn-dervish": return new int[] { 1, 2, 3, 5, 8, 10 };
     case "morality": case "purity": case "primal": return new int[] { -1000, -500, -100, 0, 100, 500, 1000 };
     default:
      // Generic presets based on range
      double range = p.max - p.min;
      if (range <= 20) return Enumerable.Range((int)Math.Ceiling(p.min), (int)range + 1).ToArray();
      if (range <= 100) return new int[] { (int)p.min, (int)(p.min + range * 0.25), (int)(p.min + range * 0.5), (int)(p.min + range * 0.75), (int)p.max };
      return new int[] { 1, 10, 50, 100, 500, 1000, 5000, 10000, 25000, 50000, 100000, 1000000 };
    }
   }