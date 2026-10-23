using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace _2DProjectileMotionSimulator
{
    public partial class Form1 : Form
    {
        private TextBox txtSpeed, txtAngle, txtMass, txtRadius;
        private Button btnLaunch;
        private Label lblStatus;
        private ComboBox cmbMedium;
        private BufferedPanel graphCanvas;

        private double angle, speed, grav = 9.81, mass = 1.0, radius = 0.05;
        private List<double[]> pathPoints = new List<double[]>();
        private System.Windows.Forms.Timer animationTimer;
        private int currentFrameIndex;
        private double[] selectedPoint;

        private float scaleX = 5f, scaleY = 5f;
        private float offsetX = 60f, offsetY = 50f;
        private bool isDragging;
        private Point lastMousePos;
        private const double AirDensity = 1.225;       // kg/m^3, near sea level
        private const double WaterDensity = 1000.0;   // kg/m^3
        private const double SphereDragCoefficient = 0.47;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomUI();

            animationTimer = new System.Windows.Forms.Timer { Interval = 15 };
            animationTimer.Tick += AnimationTimer_Tick;

            graphCanvas.Paint += GraphCanvas_Paint;
            graphCanvas.MouseDown += GraphCanvas_MouseDown;
            graphCanvas.MouseMove += GraphCanvas_MouseMove;
            graphCanvas.MouseUp += GraphCanvas_MouseUp;
        }

        private void InitializeCustomUI()
        {
            Text = "2D Projectile Motion Simulator";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1100, 700);
            MinimumSize = new Size(900, 600);

            var controlPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 88,
                BackColor = Color.FromArgb(242, 244, 247)
            };
            Controls.Add(controlPanel);

            graphCanvas = new BufferedPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 25)
            };
            Controls.Add(graphCanvas);
            controlPanel.BringToFront();

            AddFieldLabel(controlPanel, "Speed (m/s)", 12, 8, 90);
            txtSpeed = AddTextBox(controlPanel, "50", 12, 29, 85);

            AddFieldLabel(controlPanel, "Angle (°)", 110, 8, 75);
            txtAngle = AddTextBox(controlPanel, "45", 110, 29, 70);

            AddFieldLabel(controlPanel, "Mass (kg)", 195, 8, 75);
            txtMass = AddTextBox(controlPanel, "1", 195, 29, 70);

            AddFieldLabel(controlPanel, "Sphere radius (m)", 280, 8, 120);
            txtRadius = AddTextBox(controlPanel, "0.05", 280, 29, 100);

            AddFieldLabel(controlPanel, "Medium", 395, 8, 75);
            cmbMedium = new ComboBox
            {
                Left = 395,
                Top = 29,
                Width = 105,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbMedium.Items.AddRange(new object[] { "Air", "Water", "Vacuum" });
            cmbMedium.SelectedIndex = 0;
            controlPanel.Controls.Add(cmbMedium);

            btnLaunch = new Button
            {
                Text = "Launch",
                Left = 515,
                Top = 27,
                Width = 95,
                Height = 30,
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnLaunch.Click += BtnLaunch_Click;
            controlPanel.Controls.Add(btnLaunch);

            lblStatus = new Label
            {
                Text = "Ready — left-click the trajectory to inspect a point; drag elsewhere to pan.",
                Left = 12,
                Top = 63,
                Width = 850,
                Height = 20,
                ForeColor = Color.FromArgb(45, 55, 70)
            };
            controlPanel.Controls.Add(lblStatus);

            offsetX = 60f;
            offsetY = 500f;
        }

        private static void AddFieldLabel(Control parent, string text, int left, int top, int width)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Left = left,
                Top = top,
                Width = width,
                Height = 19,
                Font = new Font("Segoe UI", 8.5f)
            });
        }

        private static TextBox AddTextBox(Control parent, string value, int left, int top, int width)
        {
            var box = new TextBox
            {
                Text = value,
                Left = left,
                Top = top,
                Width = width,
                Font = new Font("Segoe UI", 9f)
            };
            parent.Controls.Add(box);
            return box;
        }

        private static bool TryReadNumber(TextBox box, out double value)
        {
            // Accept either the user's regional decimal separator or a dot.
            return double.TryParse(box.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
                || double.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private void BtnLaunch_Click(object sender, EventArgs e)
        {
            double degrees;
            if (!TryReadNumber(txtSpeed, out speed) ||
                !TryReadNumber(txtAngle, out degrees) ||
                !TryReadNumber(txtMass, out mass) ||
                !TryReadNumber(txtRadius, out radius) ||
                double.IsNaN(speed) || double.IsInfinity(speed) || speed <= 0 ||
                double.IsNaN(mass) || double.IsInfinity(mass) || mass <= 0 ||
                double.IsNaN(radius) || double.IsInfinity(radius) || radius <= 0 ||
                double.IsNaN(degrees) || double.IsInfinity(degrees) ||
                degrees <= 0 || degrees >= 90)
            {
                MessageBox.Show(
                    "Enter valid numbers. Speed, mass and radius must be positive; angle must be between 0° and 90°.",
                    "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            angle = degrees * Math.PI / 180.0;
            double density;
            switch (cmbMedium.SelectedItem.ToString())
            {
                case "Water": density = WaterDensity; break;
                case "Vacuum": density = 0.0; break;
                default: density = AirDensity; break;
            }

            // For quadratic drag: F_drag = k*v^2, where k = 1/2 * density * Cd * frontal area.
            double area = Math.PI * radius * radius;
            double dragK = 0.5 * density * SphereDragCoefficient * area;

            animationTimer.Stop();
            selectedPoint = null;

            var motion = new MotionMath(speed, angle, grav, mass, dragK);
            pathPoints = motion.GetCoordinates();
            if (pathPoints.Count < 2)
            {
                lblStatus.Text = "No trajectory could be calculated.";
                graphCanvas.Invalidate();
                return;
            }

            double maxRange = Math.Max(1.0, pathPoints[pathPoints.Count - 1][0]);
            double maxHeight = 1.0;
            foreach (double[] pt in pathPoints)
                if (pt[1] > maxHeight) maxHeight = pt[1];

            float availableWidth = Math.Max(100, graphCanvas.ClientSize.Width - 110);
            float availableHeight = Math.Max(100, graphCanvas.ClientSize.Height - 100);
            float fitScale = (float)Math.Min(availableWidth / maxRange, availableHeight / maxHeight);
            fitScale = Math.Max(0.1f, Math.Min(fitScale, 100f));
            scaleX = fitScale;
            scaleY = fitScale;

            offsetX = 60f;
            offsetY = graphCanvas.ClientSize.Height - 55f;
            currentFrameIndex = 1;
            lblStatus.Text = string.Format(
                CultureInfo.CurrentCulture,
                "{0}: k = {1:G4} kg/m. Left-click the path for coordinates; drag away from it to pan.",
                cmbMedium.SelectedItem, dragK);

            graphCanvas.Invalidate();
            animationTimer.Start();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (currentFrameIndex < pathPoints.Count - 1)
            {
                currentFrameIndex = Math.Min(currentFrameIndex + 3, pathPoints.Count - 1);
                graphCanvas.Invalidate();
            }
            else
            {
                animationTimer.Stop();
                lblStatus.Text = "Simulation complete. Click the trajectory to inspect coordinates.";
                graphCanvas.Invalidate();
            }
        }

        private void GraphCanvas_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(graphCanvas.BackColor);

            DrawGridAndAxes(g);

            if (pathPoints.Count < 2) return;

            int limit = Math.Min(currentFrameIndex + 1, pathPoints.Count);
            if (limit > 1)
            {
                var screenPoints = new List<PointF>(limit);
                for (int i = 0; i < limit; i++)
                    screenPoints.Add(ToScreen(pathPoints[i][0], pathPoints[i][1]));

                using (var pathPen = new Pen(Color.FromArgb(52, 152, 219), 2.5f))
                    g.DrawLines(pathPen, screenPoints.ToArray());
            }

            if (currentFrameIndex >= 0 && currentFrameIndex < pathPoints.Count)
            {
                double[] pt = pathPoints[currentFrameIndex];
                PointF ball = ToScreen(pt[0], pt[1]);
                float ballRadiusPx = Math.Max(5f, Math.Min(12f, (float)(radius * scaleX)));
                using (var ballBrush = new SolidBrush(Color.FromArgb(231, 76, 60)))
                using (var ballOutline = new Pen(Color.White, 1.2f))
                {
                    g.FillEllipse(ballBrush, ball.X - ballRadiusPx, ball.Y - ballRadiusPx,
                        ballRadiusPx * 2, ballRadiusPx * 2);
                    g.DrawEllipse(ballOutline, ball.X - ballRadiusPx, ball.Y - ballRadiusPx,
                        ballRadiusPx * 2, ballRadiusPx * 2);
                }

                string coordinates = string.Format(
                    CultureInfo.CurrentCulture, "x = {0:F2} m\ny = {1:F2} m", pt[0], pt[1]);
                DrawCoordinateTag(g, coordinates, ball.X + ballRadiusPx + 9, ball.Y - 24);
            }

            if (selectedPoint != null)
            {
                PointF p = ToScreen(selectedPoint[0], selectedPoint[1]);
                using (var pen = new Pen(Color.FromArgb(241, 196, 15), 2f))
                using (var brush = new SolidBrush(Color.FromArgb(241, 196, 15)))
                {
                    g.DrawEllipse(pen, p.X - 8, p.Y - 8, 16, 16);
                    g.FillEllipse(brush, p.X - 3, p.Y - 3, 6, 6);
                }
                string coordinates = string.Format(
                    CultureInfo.CurrentCulture, "Selected\nx = {0:F2} m\ny = {1:F2} m",
                    selectedPoint[0], selectedPoint[1]);
                DrawCoordinateTag(g, coordinates, p.X + 12, p.Y + 10);
            }
        }

        private void DrawCoordinateTag(Graphics g, string text, float x, float y)
        {
            using (var font = new Font("Consolas", 9f, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Color.White))
            using (var background = new SolidBrush(Color.FromArgb(220, 35, 42, 52)))
            using (var border = new Pen(Color.FromArgb(90, 170, 200), 1f))
            {
                SizeF size = g.MeasureString(text, font);
                float w = size.Width + 12, h = size.Height + 8;
                x = Math.Max(2, Math.Min(x, graphCanvas.ClientSize.Width - w - 2));
                y = Math.Max(2, Math.Min(y, graphCanvas.ClientSize.Height - h - 2));
                var rect = new RectangleF(x, y, w, h);
                g.FillRectangle(background, rect);
                g.DrawRectangle(border, rect.X, rect.Y, rect.Width, rect.Height);
                g.DrawString(text, font, textBrush, x + 6, y + 4);
            }
        }

        private void DrawGridAndAxes(Graphics g)
        {
            using (var gridPen = new Pen(Color.FromArgb(40, 50, 62), 1f))
            using (var axisPen = new Pen(Color.FromArgb(150, 165, 185), 1.8f))
            using (var tickPen = new Pen(Color.FromArgb(130, 145, 165), 1f))
            using (var font = new Font("Consolas", 8f))
            using (var textBrush = new SolidBrush(Color.FromArgb(170, 185, 205)))
            {
                const int interval = 20;
                for (int m = 0; m <= 1000; m += interval)
                {
                    PointF p = ToScreen(m, 0);
                    if (p.X >= 0 && p.X <= graphCanvas.ClientSize.Width)
                    {
                        g.DrawLine(gridPen, p.X, 0, p.X, graphCanvas.ClientSize.Height);
                        g.DrawLine(tickPen, p.X, offsetY - 4, p.X, offsetY + 4);
                        g.DrawString(m.ToString(), font, textBrush, p.X + 2, offsetY + 5);
                    }

                    PointF q = ToScreen(0, m);
                    if (q.Y >= 0 && q.Y <= graphCanvas.ClientSize.Height)
                    {
                        g.DrawLine(gridPen, 0, q.Y, graphCanvas.ClientSize.Width, q.Y);
                        g.DrawLine(tickPen, offsetX - 4, q.Y, offsetX + 4, q.Y);
                        if (m > 0)
                            g.DrawString(m.ToString(), font, textBrush, offsetX + 6, q.Y - 12);
                    }
                }

                g.DrawLine(axisPen, 0, offsetY, graphCanvas.ClientSize.Width, offsetY);
                g.DrawLine(axisPen, offsetX, 0, offsetX, graphCanvas.ClientSize.Height);
                g.DrawString("x (m)", font, textBrush, graphCanvas.ClientSize.Width - 42, offsetY + 8);
                g.DrawString("y (m)", font, textBrush, offsetX + 7, 4);
                g.DrawString("(0, 0)", font, textBrush, offsetX + 5, offsetY + 5);
            }
        }

        private PointF ToScreen(double simX, double simY)
        {
            return new PointF(offsetX + (float)(simX * scaleX),
                              offsetY - (float)(simY * scaleY));
        }

        private PointF ToSimulation(float screenX, float screenY)
        {
            return new PointF((screenX - offsetX) / scaleX,
                              (offsetY - screenY) / scaleY);
        }

        private void GraphCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Select the nearest path point if the click is close enough in screen space.
                double bestDistance = double.MaxValue;
                double[] closest = null;
                foreach (double[] pt in pathPoints)
                {
                    PointF screen = ToScreen(pt[0], pt[1]);
                    double dx = screen.X - e.X;
                    double dy = screen.Y - e.Y;
                    double distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        closest = pt;
                    }
                }

                if (closest != null && bestDistance <= 14)
                {
                    selectedPoint = closest;
                    graphCanvas.Invalidate();
                    return;
                }
            }

            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)
            {
                isDragging = true;
                lastMousePos = e.Location;
                graphCanvas.Cursor = Cursors.SizeAll;
            }
        }

        private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDragging) return;

            offsetX += e.X - lastMousePos.X;
            offsetY += e.Y - lastMousePos.Y;
            lastMousePos = e.Location;
            graphCanvas.Invalidate();
        }

        private void GraphCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            graphCanvas.Cursor = Cursors.Default;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (animationTimer != null) animationTimer.Dispose();
            base.OnFormClosed(e);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }

    // A double-buffered canvas paints to an off-screen buffer before showing the frame.
    public class BufferedPanel : Panel
    {
        public BufferedPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();
        }
    }
}
