namespace Vividia.Display;

public static class DisplayIdentifier
{
    public static void Show(IEnumerable<DisplayTarget> displays, int milliseconds = 2500)
    {
        var overlays = new List<Form>();

        foreach (var display in displays)
        {
            if (display.Bounds.Width <= 0 || display.Bounds.Height <= 0)
                continue;

            overlays.Add(CreateOverlay(display));
        }

        if (overlays.Count == 0)
            return;

        foreach (var overlay in overlays)
            overlay.Show();

        var timer = new System.Windows.Forms.Timer { Interval = milliseconds };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            timer.Dispose();
            foreach (var overlay in overlays)
            {
                overlay.Close();
                overlay.Dispose();
            }
        };
        timer.Start();
    }

    private static Form CreateOverlay(DisplayTarget display)
    {
        var form = new Form
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            ShowInTaskbar = false,
            TopMost = true,
            BackColor = Color.FromArgb(18, 18, 22),
            Opacity = 0.88,
            Size = new Size(360, 260),
        };

        form.Location = new Point(
            display.Bounds.X + (display.Bounds.Width - form.Width) / 2,
            display.Bounds.Y + (display.Bounds.Height - form.Height) / 2);

        var number = new Label
        {
            Text = display.Number.ToString(),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 120f, FontStyle.Bold, GraphicsUnit.Pixel),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
        };

        var caption = new Label
        {
            Text = display.MonitorName,
            ForeColor = Color.FromArgb(190, 190, 200),
            Font = new Font("Segoe UI", 16f, FontStyle.Regular, GraphicsUnit.Pixel),
            Dock = DockStyle.Bottom,
            Height = 46,
            TextAlign = ContentAlignment.MiddleCenter,
        };

        form.Controls.Add(number);
        form.Controls.Add(caption);
        return form;
    }
}
