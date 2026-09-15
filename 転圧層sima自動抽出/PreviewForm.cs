using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

// 型の曖昧さを解消するためのエイリアス定義
using Point = System.Drawing.Point;

namespace いきなりSIMAと外周線_ver2._0
{
    public partial class PreviewForm : Form
    {
        // 抽出された外周ポリゴン群
        private List<List<Tuple<double, double, double>>> polygonList;

        // クリックした任意の実世界座標（X=北, Y=東）
        public Tuple<double, double>? SelectedClickPoint { get; private set; } = null;

        private double minNorth, maxNorth, minEast, maxEast, centerNorth, centerEast;
        private double zoomFactor = 1.0;
        private PointF panOffset = new PointF(0, 0);
        private bool isPanning = false;
        private Point panStartMousePt;
        private PointF panStartOffset;

        // Visual Studio デザイナー表示用の引数なしコンストラクタ
        public PreviewForm()
        {
            InitializeComponent();
            this.polygonList = new List<List<Tuple<double, double, double>>>();
        }

        // 実際の呼び出し用コンストラクタ
        public PreviewForm(List<List<Tuple<double, double, double>>> extractedPolygons)
        {
            InitializeComponent();

            this.polygonList = extractedPolygons ?? new List<List<Tuple<double, double, double>>>();

            CalculateBounds();

            picPreview.Paint += PicPreview_Paint;
            picPreview.MouseDown += PicPreview_MouseDown;
            picPreview.MouseMove += PicPreview_MouseMove;
            picPreview.MouseUp += PicPreview_MouseUp;
            picPreview.MouseWheel += PicPreview_MouseWheel;
            picPreview.MouseClick += PicPreview_MouseClick;
            btnResetView.Click += (s, e) => { zoomFactor = 1.0; panOffset = new PointF(0, 0); picPreview.Refresh(); };
        }

        private void CalculateBounds()
        {
            var allPts = polygonList.SelectMany(p => p).ToList();
            if (allPts.Count == 0) return;

            minNorth = allPts.Min(p => p.Item1);
            maxNorth = allPts.Max(p => p.Item1);
            minEast = allPts.Min(p => p.Item2);
            maxEast = allPts.Max(p => p.Item2);

            centerNorth = (minNorth + maxNorth) / 2.0;
            centerEast = (minEast + maxEast) / 2.0;
        }

        #region マウス操作

        private void PicPreview_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle)
            {
                isPanning = true;
                panStartMousePt = e.Location;
                panStartOffset = panOffset;
            }
        }

        private void PicPreview_MouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning)
            {
                panOffset.X = panStartOffset.X + (e.X - panStartMousePt.X);
                panOffset.Y = panStartOffset.Y + (e.Y - panStartMousePt.Y);
                picPreview.Refresh();
            }
        }

        private void PicPreview_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Middle) isPanning = false;
        }

        private void PicPreview_MouseWheel(object sender, MouseEventArgs e)
        {
            var allPts = polygonList.SelectMany(p => p).ToList();
            if (allPts.Count == 0) return;

            double oldZoom = zoomFactor;
            zoomFactor *= (e.Delta > 0 ? 1.25 : 0.8);
            zoomFactor = Math.Max(0.1, Math.Min(100.0, zoomFactor));

            Point mousePt = e.Location;
            float screenCenterX = picPreview.Width / 2.0f;
            float screenCenterY = picPreview.Height / 2.0f;

            panOffset.X = (float)((panOffset.X - (mousePt.X - screenCenterX)) * (zoomFactor / oldZoom) + (mousePt.X - screenCenterX));
            panOffset.Y = (float)((panOffset.Y - (mousePt.Y - screenCenterY)) * (zoomFactor / oldZoom) + (mousePt.Y - screenCenterY));

            picPreview.Refresh();
        }

        private void PicPreview_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            double scale = GetCurrentScale();
            float screenCenterX = picPreview.Width / 2.0f + panOffset.X;
            float screenCenterY = picPreview.Height / 2.0f + panOffset.Y;

            // 画面ピクセルから測量座標へ変換（Item1=北, Item2=東）
            double clickedEast = centerEast + (e.X - screenCenterX) / scale;
            double clickedNorth = centerNorth - (e.Y - screenCenterY) / scale;

            SelectedClickPoint = Tuple.Create(clickedNorth, clickedEast);

            lblStatus.Text = $"【プロット完了】 クリック位置 X(北): {clickedNorth:F3}, Y(東): {clickedEast:F3}";
            lblStatus.ForeColor = Color.DarkGreen;

            picPreview.Refresh();
        }

        #endregion

        #region 描画処理（抽出外周ポリゴンのみ7色表示 + プロットマーク + 方位）

        private double GetCurrentScale()
        {
            double rangeNorth = maxNorth - minNorth;
            double rangeEast = maxEast - minEast;
            if (rangeNorth <= 0 || rangeEast <= 0) return 1.0;

            double drawW = picPreview.Width - 80;
            double drawH = picPreview.Height - 80;
            if (drawW <= 0 || drawH <= 0) return 1.0;

            return Math.Min(drawW / rangeEast, drawH / rangeNorth) * zoomFactor;
        }

        private void PicPreview_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            double scale = GetCurrentScale();
            float screenCenterX = picPreview.Width / 2.0f + panOffset.X;
            float screenCenterY = picPreview.Height / 2.0f + panOffset.Y;

            // 7色定義（鮮明色）
            Color[] distinctColors = new Color[]
            {
                Color.Red,
                Color.Yellow,
                Color.Lime,
                Color.Cyan,
                Color.Orange,
                Color.DeepPink,
                Color.DodgerBlue
            };

            // 1. 抽出された「外周線ポリゴン」だけを層ごとに色分け描画
            for (int polyIdx = 0; polyIdx < polygonList.Count; polyIdx++)
            {
                var pts = polygonList[polyIdx];
                if (pts.Count < 2) continue;

                Color polyColor = distinctColors[polyIdx % distinctColors.Length];

                using (Pen polyPen = new Pen(polyColor, 2f))
                {
                    for (int i = 0; i < pts.Count; i++)
                    {
                        var p1 = pts[i];
                        var p2 = pts[(i + 1) % pts.Count];

                        float sx1 = (float)(screenCenterX + (p1.Item2 - centerEast) * scale);
                        float sy1 = (float)(screenCenterY - (p1.Item1 - centerNorth) * scale);
                        float sx2 = (float)(screenCenterX + (p2.Item2 - centerEast) * scale);
                        float sy2 = (float)(screenCenterY - (p2.Item1 - centerNorth) * scale);

                        g.DrawLine(polyPen, sx1, sy1, sx2, sy2);
                    }
                }
            }

            // 2. クリック位置のプロットマーカー（赤二重丸＋十字照準線）
            if (SelectedClickPoint != null)
            {
                float sx = (float)(screenCenterX + (SelectedClickPoint.Item2 - centerEast) * scale);
                float sy = (float)(screenCenterY - (SelectedClickPoint.Item1 - centerNorth) * scale);

                g.FillEllipse(Brushes.Red, sx - 4, sy - 4, 8, 8);

                using (Pen pOuter = new Pen(Color.Yellow, 2f))
                using (Pen pInner = new Pen(Color.Red, 2f))
                {
                    g.DrawEllipse(pOuter, sx - 16, sy - 16, 32, 32);
                    g.DrawEllipse(pInner, sx - 10, sy - 10, 20, 20);

                    g.DrawLine(pInner, sx - 25, sy, sx + 25, sy);
                    g.DrawLine(pInner, sx, sy - 25, sx, sy + 25);
                }
            }

            DrawNorthArrow(g);
        }

        private void DrawNorthArrow(Graphics g)
        {
            int arrowX = 35, arrowY = 45;
            g.FillEllipse(new SolidBrush(Color.FromArgb(180, 30, 30, 30)), arrowX - 20, arrowY - 20, 40, 40);
            g.DrawEllipse(Pens.White, arrowX - 20, arrowY - 20, 40, 40);

            PointF[] northArrow = { new PointF(arrowX, arrowY - 16), new PointF(arrowX - 6, arrowY + 8), new PointF(arrowX, arrowY + 3) };
            PointF[] southArrow = { new PointF(arrowX, arrowY - 16), new PointF(arrowX + 6, arrowY + 8), new PointF(arrowX, arrowY + 3) };

            g.FillPolygon(Brushes.Red, northArrow);
            g.FillPolygon(Brushes.White, southArrow);

            using (Font font = new Font("Arial", 10, FontStyle.Bold))
            {
                g.DrawString("N", font, Brushes.Red, arrowX - 6, arrowY - 36);
            }
        }

        #endregion
    }
}