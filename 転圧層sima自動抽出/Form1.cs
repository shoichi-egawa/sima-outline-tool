using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Linq;
using netDxf;
using netDxf.Entities;

namespace いきなりSIMAと外周線_ver2._0
{
    public partial class Form1 : Form
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, string lParam);

        private string selectedFilePath = "";
        private bool isLandXmlMode = false;

        private const double TOLERANCE = 0.01;
        private const double MIN_AREA = 1.0;

        public Form1()
        {
            InitializeComponent();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            SendMessage(txtKoujimei.Handle, 0x1501, 1, "工事名を入力してください。例：〇〇工事");

            // 小さな画面でボタンが見切れないよう自動スクロールを有効化
            this.AutoScroll = true;

            // ヘルプイベントの紐づけ（？ボタンやF1キー対応）
            this.HelpRequested += new HelpEventHandler(Form1_HelpRequested);
        }

        /// <summary>
        /// タイトルバーの「？」ボタンやF1キーが押された際、
        /// exe内に埋め込まれたPDFマニュアルを展開して開く処理
        /// </summary>
        private void Form1_HelpRequested(object sender, HelpEventArgs hlpevent)
        {
            try
            {
                // 組み込みリソース名（プロジェクトのデフォルト名前空間.ファイル名.拡張子）
                string resourceName = "いきなりSIMAと外周線_ver2._0.いきなりSIMAと外周線ver2.1とりせつ.pdf";

                // 一時フォルダ（Temp）に書き出すパスを作成
                string tempPdfPath = Path.Combine(Path.GetTempPath(), "manual_temp.pdf");

                Assembly assembly = Assembly.GetExecutingAssembly();

                // exe内部からPDFリソースをストリームとして読み込む
                using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        // リソース名が見つからない場合のフォールバック（埋め込まれている全リソース名から.pdfを自動探索）
                        string? foundName = assembly.GetManifestResourceNames()
                            .FirstOrDefault(n => n.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

                        if (foundName != null)
                        {
                            using (Stream fallbackStream = assembly.GetManifestResourceStream(foundName)!)
                            using (FileStream fileStream = new FileStream(tempPdfPath, FileMode.Create, FileAccess.Write))
                            {
                                fallbackStream.CopyTo(fileStream);
                            }
                        }
                        else
                        {
                            MessageBox.Show("埋め込まれたPDFマニュアルが見つかりませんでした。\nソリューションエクスプローラーでPDFの『ビルド アクション』が『埋め込まれたリソース』になっているか確認してください。",
                                            "ヘルプ表示エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            hlpevent.Handled = true;
                            return;
                        }
                    }
                    else
                    {
                        using (FileStream fileStream = new FileStream(tempPdfPath, FileMode.Create, FileAccess.Write))
                        {
                            stream.CopyTo(fileStream);
                        }
                    }
                }

                // 一時ファイルとして吐き出したPDFをOSの標準PDFビューアー（Edge, Chrome, Acrobat等）で開く
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempPdfPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"マニュアルの表示中にエラーが発生しました:\n\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // 標準のWindowsヘルプポップアップ動作をキャンセル
            hlpevent.Handled = true;
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "対応図面/3Dデータ (*.dxf;*.xml)|*.dxf;*.xml|DXFファイル (*.dxf)|*.dxf|LandXMLファイル (*.xml)|*.xml";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    selectedFilePath = ofd.FileName;
                    lblFilePath.Text = Path.GetFileName(selectedFilePath);
                    isLandXmlMode = Path.GetExtension(selectedFilePath).ToLower() == ".xml";

                    if (isLandXmlMode) LoadLandXmlSurfaces(selectedFilePath);
                    else LoadDxfLayers(selectedFilePath);
                }
            }
        }

        private void LoadDxfLayers(string path)
        {
            try
            {
                chkLayerList.Items.Clear();
                if (!File.Exists(path)) return;

                DxfDocument doc = DxfDocument.Load(path);
                if (doc == null)
                {
                    MessageBox.Show("DXFファイルの解析に失敗しました。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                HashSet<string> layers = new HashSet<string>();
                if (doc.Layers != null) foreach (var layer in doc.Layers) if (!string.IsNullOrEmpty(layer.Name)) layers.Add(layer.Name);

                foreach (var line in doc.Entities.Lines) if (line.Layer != null) layers.Add(line.Layer.Name);
                foreach (var poly in doc.Entities.Polylines2D) if (poly.Layer != null) layers.Add(poly.Layer.Name);
                foreach (var poly3d in doc.Entities.Polylines3D) if (poly3d.Layer != null) layers.Add(poly3d.Layer.Name);
                foreach (var face in doc.Entities.Faces3D) if (face.Layer != null) layers.Add(face.Layer.Name);
                foreach (var txt in doc.Entities.Texts) if (txt.Layer != null) layers.Add(txt.Layer.Name);
                foreach (var mtxt in doc.Entities.MTexts) if (mtxt.Layer != null) layers.Add(mtxt.Layer.Name);

                foreach (var layer in layers.OrderBy(l => l)) chkLayerList.Items.Add(layer, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"レイヤー読み込み中に例外が発生しました:\n\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadLandXmlSurfaces(string path)
        {
            try
            {
                chkLayerList.Items.Clear();
                XDocument doc = XDocument.Load(path);
                XNamespace ns = doc.Root?.Name.Namespace ?? "";
                var surfaces = doc.Descendants(ns + "Surface").Select(s => (string?)s.Attribute("name")).Where(n => !string.IsNullOrEmpty(n)).Distinct().OrderBy(s => s).ToList();
                foreach (var sName in surfaces) chkLayerList.Items.Add(sName, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"LandXML読み込み中に例外が発生しました:\n\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSelectAll_Click(object sender, EventArgs e) { for (int i = 0; i < chkLayerList.Items.Count; i++) chkLayerList.SetItemChecked(i, true); }
        private void btnDeselectAll_Click(object sender, EventArgs e) { for (int i = 0; i < chkLayerList.Items.Count; i++) chkLayerList.SetItemChecked(i, false); }

        private void btnRun_Click(object sender, EventArgs e)
        {
            string koujimei = txtKoujimei.Text.Trim();
            if (string.IsNullOrEmpty(koujimei) || string.IsNullOrEmpty(selectedFilePath))
            {
                MessageBox.Show("工事名またはファイルパスが未入力です。", "確認", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedLayers = chkLayerList.CheckedItems.Cast<string>().ToList();
            if (selectedLayers.Count == 0)
            {
                MessageBox.Show("対象のレイヤー（またはサーフェス）が選択されていません。", "確認", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                DxfDocument? dxfDoc = !isLandXmlMode ? DxfDocument.Load(selectedFilePath) : null;
                Dictionary<string, byte[]> individualZipFiles = new Dictionary<string, byte[]>();
                List<Tuple<int, string, double, double, double>> combinedAllPoints = new List<Tuple<int, string, double, double, double>>();
                int globalTenBan = 1;
                Dictionary<string, List<List<Tuple<double, double, double>>>> processedPolygons = new Dictionary<string, List<List<Tuple<double, double, double>>>>();

                List<string> failedBridgeLayers = new List<string>();

                foreach (string layerName in selectedLayers)
                {
                    var layerLines = isLandXmlMode ? GetLinesFromLandXmlSurface(selectedFilePath, layerName) : GetLinesFromLayer(dxfDoc, layerName);
                    if (layerLines.Count == 0) continue;

                    var outerPolygons = FindOuterPolygons(layerLines);
                    if (outerPolygons.Count == 0) continue;

                    if (chkSimplify.Checked)
                    {
                        double toleranceMeters = (double)numTolerance.Value / 100.0;
                        outerPolygons = outerPolygons.Select(p => SimplifyPolygon(p, toleranceMeters)).Where(p => p.Count >= 3).ToList();
                        if (outerPolygons.Count == 0) continue;
                    }

                    if (chkBridgeIslands.Checked && outerPolygons.Count > 1)
                    {
                        try
                        {
                            var mergedPolygon = ConnectAllPolygonsWithBridges(outerPolygons, 0.10);
                            if (mergedPolygon != null && mergedPolygon.Count >= 3)
                            {
                                outerPolygons = new List<List<Tuple<double, double, double>>> { mergedPolygon };
                            }
                            else
                            {
                                failedBridgeLayers.Add(layerName);
                            }
                        }
                        catch
                        {
                            failedBridgeLayers.Add(layerName);
                        }
                    }

                    processedPolygons[layerName] = outerPolygons;

                    for (int polyIdx = 0; polyIdx < outerPolygons.Count; polyIdx++)
                    {
                        var ordered = ReorderClockwiseFromNortheast(outerPolygons[polyIdx]);
                        string prefix = outerPolygons.Count == 1 ? layerName : $"{layerName}-{polyIdx + 1}";
                        string outputFilename = outerPolygons.Count == 1 ? $"{layerName}.sim" : $"{layerName}-{polyIdx + 1}.sim";

                        List<Tuple<int, string, double, double, double>> layerPoints = new List<Tuple<int, string, double, double, double>>();
                        for (int j = 0; j < ordered.Count; j++)
                        {
                            int localTenBan = j + 1;
                            string tenMei = $"{prefix}-{localTenBan}";
                            layerPoints.Add(Tuple.Create(localTenBan, tenMei, ordered[j].Item1, ordered[j].Item2, ordered[j].Item3));
                            combinedAllPoints.Add(Tuple.Create(globalTenBan++, tenMei, ordered[j].Item1, ordered[j].Item2, ordered[j].Item3));
                        }
                        byte[] simaBytes = Encoding.GetEncoding("shift_jis").GetBytes(WriteSimaContent(koujimei, layerPoints));
                        individualZipFiles[outputFilename] = simaBytes;
                    }
                }

                if (individualZipFiles.Count == 0)
                {
                    MessageBox.Show("選択されたレイヤーから有効な図形（線分やポリゴン）が抽出できませんでした。\nレイヤー名や図面データの内容をご確認ください。", "データなし", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string shortKoujimei = koujimei.Length > 10 ? koujimei.Substring(0, 10) : koujimei;

                // 1. 個別SIMA保存
                using (SaveFileDialog sfdZip = new SaveFileDialog() { Title = "個別層SIMA(ZIP)の保存", Filter = "ZIPファイル|*.zip", FileName = $"{shortKoujimei}個別層SIMA.zip" })
                {
                    if (sfdZip.ShowDialog() == DialogResult.OK)
                    {
                        using (var zipStream = new FileStream(sfdZip.FileName, FileMode.Create))
                        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                        {
                            foreach (var file in individualZipFiles)
                            {
                                var zipEntry = archive.CreateEntry(file.Key, CompressionLevel.Optimal);
                                using (var entryStream = zipEntry.Open()) entryStream.Write(file.Value, 0, file.Value.Length);
                            }
                        }
                    }
                }

                // 2. 全層まとめSIMA保存
                if (MessageBox.Show("全層まとめSIMAを出力しますか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    using (SaveFileDialog sfdSim = new SaveFileDialog() { Filter = "SIMAファイル|*.sim", FileName = $"{shortKoujimei}_全層まとめ.sim" })
                    {
                        if (sfdSim.ShowDialog() == DialogResult.OK)
                            File.WriteAllBytes(sfdSim.FileName, Encoding.GetEncoding("shift_jis").GetBytes(WriteSimaContent($"{koujimei}_全層", combinedAllPoints)));
                    }
                }

                // 3. 外周抽出DXF保存
                if (MessageBox.Show("外周抽出DXF図面を出力しますか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    using (SaveFileDialog sfdDxf = new SaveFileDialog() { Filter = "DXFファイル|*.dxf", FileName = $"{shortKoujimei}_外周抽出図面.dxf" })
                    {
                        if (sfdDxf.ShowDialog() == DialogResult.OK)
                        {
                            SaveNewDxfFromPolygonsWithLegend(processedPolygons, sfdDxf.FileName, dxfDoc, selectedLayers);
                        }
                    }
                }

                // ▼ LandXMLモードの場合のみ、サーフェス毎の個別のLandXML(ZIP)を出力する ▼
                if (isLandXmlMode)
                {
                    if (MessageBox.Show("選択されたサーフェスごとに分割した個別LandXML(ZIP)を出力しますか？", "確認", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        ExportIndividualLandXmlZip(selectedFilePath, selectedLayers, shortKoujimei);
                    }
                }

                if (failedBridgeLayers.Count > 0)
                {
                    string failMsg = "以下のレイヤーで小島ブリッジ結合処理に失敗したため、個別ポリゴンのまま出力しました:\n\n"
                                   + string.Join("\n", failedBridgeLayers);
                    MessageBox.Show(failMsg, "ブリッジ接続スキップ通知", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                MessageBox.Show("出力完了！", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void ExportIndividualLandXmlZip(string xmlPath, List<string> selectedSurfaces, string shortKoujimei)
        {
            try
            {
                Dictionary<string, string> xmlFilesContent = new Dictionary<string, string>();

                foreach (string sName in selectedSurfaces)
                {
                    XDocument docCopy = XDocument.Load(xmlPath);
                    XNamespace ns = docCopy.Root?.Name.Namespace ?? "";

                    var allSurfaces = docCopy.Descendants(ns + "Surface").ToList();

                    foreach (var s in allSurfaces)
                    {
                        string? name = (string?)s.Attribute("name");
                        if (!string.Equals(name, sName, StringComparison.OrdinalIgnoreCase))
                        {
                            s.Remove();
                        }
                    }

                    if (docCopy.Descendants(ns + "Surface").Any())
                    {
                        string fileName = $"{sName}.xml";
                        xmlFilesContent[fileName] = docCopy.ToString();
                    }
                }

                if (xmlFilesContent.Count == 0)
                {
                    MessageBox.Show("個別のサーフェス要素を抽出できませんでした。", "確認", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                using (SaveFileDialog sfdXmlZip = new SaveFileDialog()
                {
                    Title = "個別サーフェスLandXML(ZIP)の保存",
                    Filter = "ZIPファイル|*.zip",
                    FileName = $"{shortKoujimei}個別サーフェスLandXML.zip"
                })
                {
                    if (sfdXmlZip.ShowDialog() == DialogResult.OK)
                    {
                        using (var zipStream = new FileStream(sfdXmlZip.FileName, FileMode.Create))
                        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                        {
                            foreach (var kvp in xmlFilesContent)
                            {
                                var entry = archive.CreateEntry(kvp.Key, CompressionLevel.Optimal);
                                using (var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false)))
                                {
                                    writer.Write(kvp.Value);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"個別LandXML出力中にエラーが発生しました:\n\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private double GetPerpendicularDistance(Tuple<double, double, double> pt, Tuple<double, double, double> lineStart, Tuple<double, double, double> lineEnd)
        {
            double y0 = pt.Item1, x0 = pt.Item2;
            double y1 = lineStart.Item1, x1 = lineStart.Item2;
            double y2 = lineEnd.Item1, x2 = lineEnd.Item2;

            double dx = x2 - x1;
            double dy = y2 - y1;
            double lenSq = dx * dx + dy * dy;

            if (lenSq < 1e-9) return GetDistance(pt, lineStart);

            double num = Math.Abs(dy * x0 - dx * y0 + x2 * y1 - y2 * x1);
            return num / Math.Sqrt(lenSq);
        }

        private List<Tuple<double, double, double>> SimplifyPolygon(List<Tuple<double, double, double>> pts, double toleranceMeters)
        {
            if (pts.Count <= 3) return pts;

            int idxA = 0, idxB = 0;
            double maxD = -1;
            for (int i = 0; i < pts.Count; i++)
            {
                for (int j = i + 1; j < pts.Count; j++)
                {
                    double d = GetDistance(pts[i], pts[j]);
                    if (d > maxD)
                    {
                        maxD = d;
                        idxA = i;
                        idxB = j;
                    }
                }
            }

            if (idxA > idxB) { int tmp = idxA; idxA = idxB; idxB = tmp; }

            var path1 = new List<Tuple<double, double, double>>();
            for (int i = idxA; i <= idxB; i++) path1.Add(pts[i]);

            var path2 = new List<Tuple<double, double, double>>();
            for (int i = idxB; i < pts.Count; i++) path2.Add(pts[i]);
            for (int i = 0; i <= idxA; i++) path2.Add(pts[i]);

            var simp1 = DouglasPeucker(path1, toleranceMeters);
            var simp2 = DouglasPeucker(path2, toleranceMeters);

            var result = new List<Tuple<double, double, double>>();
            result.AddRange(simp1.Take(simp1.Count - 1));
            result.AddRange(simp2.Take(simp2.Count - 1));

            return result.Count >= 3 ? result : pts;
        }

        private List<Tuple<double, double, double>> DouglasPeucker(List<Tuple<double, double, double>> pts, double tolerance)
        {
            if (pts.Count <= 2) return new List<Tuple<double, double, double>>(pts);

            double maxDist = 0;
            int maxIndex = 0;
            var start = pts[0];
            var end = pts[pts.Count - 1];

            for (int i = 1; i < pts.Count - 1; i++)
            {
                double dist = GetPerpendicularDistance(pts[i], start, end);
                if (dist > maxDist)
                {
                    maxDist = dist;
                    maxIndex = i;
                }
            }

            if (maxDist > tolerance)
            {
                var left = DouglasPeucker(pts.GetRange(0, maxIndex + 1), tolerance);
                var right = DouglasPeucker(pts.GetRange(maxIndex, pts.Count - maxIndex), tolerance);

                var result = new List<Tuple<double, double, double>>(left);
                result.RemoveAt(result.Count - 1);
                result.AddRange(right);
                return result;
            }
            else
            {
                return new List<Tuple<double, double, double>> { start, end };
            }
        }

        private netDxf.AciColor GetGroupedDistinctColor(int sequenceIndex)
        {
            double[] hues = new double[] { 0.0, 55.0, 120.0, 180.0, 240.0, 300.0, 30.0 };

            int group = (sequenceIndex - 1) / 7;
            int colorInGroup = (sequenceIndex - 1) % 7;

            double hue = hues[colorInGroup];
            double sat = 1.0;
            double val = 1.0;

            if (group == 0)
            {
                sat = 1.0;
                val = 1.0;
            }
            else if (group == 1)
            {
                sat = 0.85;
                val = 0.70;
            }
            else if (group == 2)
            {
                sat = 0.75;
                val = 0.45;
            }
            else
            {
                sat = 0.8;
                val = 0.30 + ((group * 0.08) % 0.25);
            }

            return ColorFromHsv(hue, sat, val);
        }

        private netDxf.AciColor ColorFromHsv(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);
            double p = value * (1 - saturation);
            double q = value * (1 - f * saturation);
            double t = value * (1 - (1 - f) * saturation);

            double r = 0, g = 0, b = 0;
            if (hi == 0) { r = value; g = t; b = p; }
            else if (hi == 1) { r = q; g = value; b = p; }
            else if (hi == 2) { r = p; g = value; b = t; }
            else if (hi == 3) { r = p; g = q; b = value; }
            else if (hi == 4) { r = t; g = p; b = value; }
            else if (hi == 5) { r = value; g = p; b = q; }

            return new netDxf.AciColor((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private void SaveNewDxfFromPolygonsWithLegend(
            Dictionary<string, List<List<Tuple<double, double, double>>>> processedPolygons,
            string outputPath,
            DxfDocument? originalDoc,
            List<string> selectedLayers)
        {
            DxfDocument doc = new DxfDocument();

            if (originalDoc != null)
            {
                HashSet<string> selSet = new HashSet<string>(selectedLayers.Select(s => s.Trim()), StringComparer.OrdinalIgnoreCase);

                foreach (var ts in originalDoc.TextStyles)
                {
                    if (!doc.TextStyles.Contains(ts.Name))
                    {
                        doc.TextStyles.Add((netDxf.Tables.TextStyle)ts.Clone());
                    }
                }

                foreach (var l in originalDoc.Layers)
                {
                    if (!selSet.Contains(l.Name.Trim()) && !doc.Layers.Contains(l.Name))
                    {
                        doc.Layers.Add((netDxf.Tables.Layer)l.Clone());
                    }
                }

                foreach (var entity in originalDoc.Entities.Lines)
                {
                    if (!selSet.Contains(entity.Layer?.Name?.Trim() ?? "0"))
                        doc.Entities.Add((netDxf.Entities.Line)entity.Clone());
                }

                foreach (var entity in originalDoc.Entities.Polylines2D)
                {
                    if (!selSet.Contains(entity.Layer?.Name?.Trim() ?? "0"))
                        doc.Entities.Add((netDxf.Entities.Polyline2D)entity.Clone());
                }

                foreach (var entity in originalDoc.Entities.Polylines3D)
                {
                    if (!selSet.Contains(entity.Layer?.Name?.Trim() ?? "0"))
                        doc.Entities.Add((netDxf.Entities.Polyline3D)entity.Clone());
                }

                foreach (var entity in originalDoc.Entities.Texts)
                {
                    if (!selSet.Contains(entity.Layer?.Name?.Trim() ?? "0"))
                    {
                        var copyText = (netDxf.Entities.Text)entity.Clone();
                        if (copyText.Style != null && !doc.TextStyles.Contains(copyText.Style.Name))
                        {
                            doc.TextStyles.Add((netDxf.Tables.TextStyle)copyText.Style.Clone());
                        }
                        doc.Entities.Add(copyText);
                    }
                }

                foreach (var entity in originalDoc.Entities.MTexts)
                {
                    if (!selSet.Contains(entity.Layer?.Name?.Trim() ?? "0"))
                    {
                        var copyMText = (netDxf.Entities.MText)entity.Clone();
                        if (copyMText.Style != null && !doc.TextStyles.Contains(copyMText.Style.Name))
                        {
                            doc.TextStyles.Add((netDxf.Tables.TextStyle)copyMText.Style.Clone());
                        }
                        doc.Entities.Add(copyMText);
                    }
                }

                foreach (var entity in originalDoc.Entities.Circles)
                {
                    if (!selSet.Contains(entity.Layer?.Name?.Trim() ?? "0"))
                        doc.Entities.Add((netDxf.Entities.Circle)entity.Clone());
                }

                foreach (var entity in originalDoc.Entities.Arcs)
                {
                    if (!selSet.Contains(entity.Layer?.Name?.Trim() ?? "0"))
                        doc.Entities.Add((netDxf.Entities.Arc)entity.Clone());
                }

                foreach (var entity in originalDoc.Entities.Hatches)
                {
                    if (!selSet.Contains(entity.Layer?.Name?.Trim() ?? "0"))
                        doc.Entities.Add((netDxf.Entities.Hatch)entity.Clone());
                }

                foreach (var entity in originalDoc.Entities.Solids)
                {
                    if (!selSet.Contains(entity.Layer?.Name?.Trim() ?? "0"))
                        doc.Entities.Add((netDxf.Entities.Solid)entity.Clone());
                }

                foreach (var entity in originalDoc.Entities.Inserts)
                {
                    if (!selSet.Contains(entity.Layer?.Name?.Trim() ?? "0"))
                        doc.Entities.Add((netDxf.Entities.Insert)entity.Clone());
                }
            }

            var sortedLayers = processedPolygons.Keys
                .Select(layerName => {
                    var match = Regex.Match(layerName, @"\d+");
                    int numVal = 0;
                    bool hasNum = match.Success && int.TryParse(match.Value, out numVal);
                    bool hasAlpha = !string.IsNullOrEmpty(layerName) && layerName.Any(char.IsLetter);
                    double totalArea = processedPolygons[layerName].Sum(p => CalculateArea(p));
                    return new
                    {
                        LayerName = layerName,
                        HasNum = hasNum ? 0 : 1,
                        NumVal = numVal,
                        HasAlpha = hasAlpha ? 0 : 1,
                        LayerNameStr = layerName,
                        AreaDesc = -totalArea
                    };
                })
                .OrderBy(x => x.HasNum)
                .ThenBy(x => x.NumVal)
                .ThenBy(x => x.HasAlpha)
                .ThenBy(x => x.LayerNameStr)
                .ThenBy(x => x.AreaDesc)
                .Select(x => x.LayerName)
                .ToList();

            double minX = double.MaxValue;
            double maxY = double.MinValue;
            foreach (var layerName in sortedLayers)
            {
                foreach (var polyPts in processedPolygons[layerName])
                {
                    foreach (var pt in polyPts)
                    {
                        if (pt.Item2 < minX) minX = pt.Item2;
                        if (pt.Item1 > maxY) maxY = pt.Item1;
                    }
                }
            }
            if (minX == double.MaxValue) { minX = 0; maxY = 0; }

            double legendRightX = minX - 10.0;
            double lineLength = 5.0;
            double startX = legendRightX - lineLength;
            double currentY = maxY;
            double rowPitch = 2.0;
            double textHeight = 0.8;

            int colorIndex = 1;
            foreach (string layerName in sortedLayers)
            {
                var polygons = processedPolygons[layerName];
                netDxf.AciColor layerColor = GetGroupedDistinctColor(colorIndex);

                netDxf.Tables.Layer layer;
                if (doc.Layers.Contains(layerName))
                {
                    layer = doc.Layers[layerName];
                    layer.Color = layerColor;
                }
                else
                {
                    layer = new netDxf.Tables.Layer(layerName) { Color = layerColor };
                    doc.Layers.Add(layer);
                }

                foreach (var polyPts in polygons)
                {
                    var vertexes = polyPts.Select(pt => new Polyline2DVertex(pt.Item2, pt.Item1)).ToList();
                    Polyline2D newPoly = new Polyline2D(vertexes, isClosed: true)
                    {
                        Layer = layer,
                        Color = layerColor
                    };
                    doc.Entities.Add(newPoly);
                }

                var lineStart = new Vector2(startX, currentY);
                var lineEnd = new Vector2(legendRightX, currentY);
                var legendLine = new netDxf.Entities.Line(lineStart, lineEnd)
                {
                    Layer = layer,
                    Color = layerColor
                };
                doc.Entities.Add(legendLine);

                var textPos = new Vector2(legendRightX + 1.0, currentY - (textHeight / 2.0));
                var legendText = new netDxf.Entities.Text(layerName, textPos, textHeight)
                {
                    Layer = layer,
                    Color = layerColor
                };
                doc.Entities.Add(legendText);

                currentY -= rowPitch;
                colorIndex++;
            }

            doc.Save(outputPath);
        }

        private List<Tuple<double, double, double>> ConnectTwoPolygonsBothSides(
            List<Tuple<double, double, double>> polyA,
            List<Tuple<double, double, double>> polyB, double widthMeters = 0.10)
        {
            if (polyA.Count < 3 || polyB.Count < 3) return polyA;

            int bestIdxA = 0, bestIdxB = 0;
            double minDist = double.MaxValue;
            for (int i = 0; i < polyA.Count; i++)
            {
                for (int j = 0; j < polyB.Count; j++)
                {
                    double d = GetDistance(polyA[i], polyB[j]);
                    if (d < minDist) { minDist = d; bestIdxA = i; bestIdxB = j; }
                }
            }

            var ptA = polyA[bestIdxA];
            var ptB = polyB[bestIdxB];

            double dx = ptB.Item2 - ptA.Item2, dy = ptB.Item1 - ptA.Item1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.0001) return polyA;

            double nx = (-dy / len) * widthMeters, ny = (dx / len) * widthMeters;
            var pL1 = Tuple.Create(ptA.Item1 + ny, ptA.Item2 + nx, ptA.Item3);
            var pL2 = Tuple.Create(ptB.Item1 + ny, ptB.Item2 + nx, ptB.Item3);
            var pR1 = Tuple.Create(ptA.Item1 - ny, ptA.Item2 - nx, ptA.Item3);
            var pR2 = Tuple.Create(ptB.Item1 - ny, ptB.Item2 - nx, ptB.Item3);

            int edgeA_L, edgeB_L, edgeA_R, edgeB_R;
            var interL_A = FindIntersection(pL1, pL2, polyA, ptA, out edgeA_L);
            var interL_B = FindIntersection(pL1, pL2, polyB, ptB, out edgeB_L);
            var interR_A = FindIntersection(pR1, pR2, polyA, ptA, out edgeA_R);
            var interR_B = FindIntersection(pR1, pR2, polyB, ptB, out edgeB_R);

            Tuple<double, double, double> retA, retB;
            int tgtEdgeA, tgtEdgeB;

            if (interL_A != null && interL_B != null)
            {
                retA = interL_A; tgtEdgeA = edgeA_L;
                retB = interL_B; tgtEdgeB = edgeB_L;
            }
            else if (interR_A != null && interR_B != null)
            {
                retA = interR_A; tgtEdgeA = edgeA_R;
                retB = interR_B; tgtEdgeB = edgeB_R;
            }
            else
            {
                retA = pL1; tgtEdgeA = bestIdxA;
                retB = pL2; tgtEdgeB = bestIdxB;
            }

            var cA1 = Tuple.Create(ptA, bestIdxA, true);
            var cA2 = Tuple.Create(retA, tgtEdgeA, false);
            bool a1First = (cA1.Item2 < cA2.Item2) || (cA1.Item2 == cA2.Item2 && cA1.Item3);

            var aOut = a1First ? cA1 : cA2;
            var aIn = a1First ? cA2 : cA1;
            var cB1 = Tuple.Create(ptB, bestIdxB, true);
            var cB2 = Tuple.Create(retB, tgtEdgeB, false);
            var bIn = a1First ? cB1 : cB2;
            var bOut = a1First ? cB2 : cB1;

            var candForward = BuildRayPathSmart(polyA, polyB, aOut, aIn, bIn, bOut, true);
            var candBackward = BuildRayPathSmart(polyA, polyB, aOut, aIn, bIn, bOut, false);

            return CalculateArea(candForward) > CalculateArea(candBackward) ? candForward : candBackward;
        }

        private Tuple<double, double, double>? FindIntersection(
            Tuple<double, double, double> p1, Tuple<double, double, double> p2,
            List<Tuple<double, double, double>> poly, Tuple<double, double, double> refPt, out int hitEdgeIdx)
        {
            hitEdgeIdx = -1;
            Tuple<double, double, double>? bestPt = null;
            double minD = double.MaxValue;
            for (int i = 0; i < poly.Count; i++)
            {
                var pt = GetIntersection(p1, p2, poly[i], poly[(i + 1) % poly.Count]);
                if (pt != null)
                {
                    double d = GetDistance(pt, refPt);
                    if (d < minD) { minD = d; bestPt = pt; hitEdgeIdx = i; }
                }
            }
            return bestPt;
        }

        private Tuple<double, double, double>? GetIntersection(
            Tuple<double, double, double> p1, Tuple<double, double, double> p2,
            Tuple<double, double, double> p3, Tuple<double, double, double> p4)
        {
            double denom = (p4.Item1 - p3.Item1) * (p2.Item2 - p1.Item2) - (p4.Item2 - p3.Item2) * (p2.Item1 - p1.Item1);
            if (Math.Abs(denom) < 1e-9) return null;
            double ua = ((p4.Item2 - p3.Item2) * (p1.Item1 - p3.Item1) - (p4.Item1 - p3.Item1) * (p1.Item2 - p3.Item2)) / denom;
            double ub = ((p2.Item2 - p1.Item2) * (p1.Item1 - p3.Item1) - (p2.Item1 - p1.Item1) * (p1.Item2 - p3.Item2)) / denom;
            if (ub >= 0.0 && ub <= 1.0) return Tuple.Create(p1.Item1 + ua * (p2.Item1 - p1.Item1), p1.Item2 + ua * (p2.Item2 - p1.Item2), p3.Item3);
            return null;
        }

        private List<Tuple<double, double, double>> BuildRayPathSmart(
            List<Tuple<double, double, double>> polyA, List<Tuple<double, double, double>> polyB,
            Tuple<Tuple<double, double, double>, int, bool> aOut, Tuple<Tuple<double, double, double>, int, bool> aIn,
            Tuple<Tuple<double, double, double>, int, bool> bIn, Tuple<Tuple<double, double, double>, int, bool> bOut, bool bForward)
        {
            var res = new List<Tuple<double, double, double>>();

            for (int i = 0; i <= aOut.Item2; i++) res.Add(polyA[i]);
            if (!aOut.Item3) res.Add(aOut.Item1);

            res.Add(bIn.Item1);

            int nB = polyB.Count;
            if (bForward)
            {
                int curr = (bIn.Item2 + 1) % nB, count = 0;
                while (count <= nB + 1)
                {
                    if (bOut.Item3 && curr == bOut.Item2) break;
                    if (!bOut.Item3 && curr == (bOut.Item2 + 1) % nB) break;
                    res.Add(polyB[curr]);
                    curr = (curr + 1) % nB; count++;
                }
            }
            else
            {
                int curr = bIn.Item3 ? (bIn.Item2 - 1 + nB) % nB : bIn.Item2, count = 0;
                while (count <= nB + 1)
                {
                    if (bOut.Item3 && curr == bOut.Item2) break;
                    if (!bOut.Item3 && curr == bOut.Item2) { res.Add(polyB[curr]); break; }
                    res.Add(polyB[curr]);
                    curr = (curr - 1 + nB) % nB; count++;
                }
            }

            res.Add(bOut.Item1);
            res.Add(aIn.Item1);

            for (int i = aIn.Item2 + 1; i < polyA.Count; i++) res.Add(polyA[i]);

            return res;
        }

        private List<Tuple<Tuple<double, double, double>, Tuple<double, double, double>>> GetLinesFromLandXmlSurface(string xmlPath, string surfaceName)
        {
            var lines = new List<Tuple<Tuple<double, double, double>, Tuple<double, double, double>>>();
            XDocument doc = XDocument.Load(xmlPath);
            XNamespace ns = doc.Root?.Name.Namespace ?? "";
            var surface = doc.Descendants(ns + "Surface").FirstOrDefault(s => (string?)s.Attribute("name") == surfaceName);
            if (surface == null) return lines;
            var pntsDict = new Dictionary<string, Tuple<double, double, double>>();
            foreach (var pnt in surface.Descendants(ns + "P"))
            {
                string? id = (string?)pnt.Attribute("id");
                if (string.IsNullOrEmpty(id)) continue;
                string[] coords = pnt.Value.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (coords.Length >= 3) pntsDict[id] = Tuple.Create(double.Parse(coords[0]), double.Parse(coords[1]), double.Parse(coords[2]));
            }
            foreach (var face in surface.Descendants(ns + "F"))
            {
                string[] ids = face.Value.Trim().Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (ids.Length >= 3 && pntsDict.ContainsKey(ids[0]) && pntsDict.ContainsKey(ids[1]) && pntsDict.ContainsKey(ids[2]))
                {
                    if (GetDistance(pntsDict[ids[0]], pntsDict[ids[1]]) > 0.0001) lines.Add(Tuple.Create(pntsDict[ids[0]], pntsDict[ids[1]]));
                    if (GetDistance(pntsDict[ids[1]], pntsDict[ids[2]]) > 0.0001) lines.Add(Tuple.Create(pntsDict[ids[1]], pntsDict[ids[2]]));
                    if (GetDistance(pntsDict[ids[2]], pntsDict[ids[0]]) > 0.0001) lines.Add(Tuple.Create(pntsDict[ids[2]], pntsDict[ids[0]]));
                }
            }
            return lines;
        }

        private List<Tuple<double, double, double>> ConnectAllPolygonsWithBridges(List<List<Tuple<double, double, double>>> polygons, double widthMeters = 0.10)
        {
            var currentMerged = polygons[0];
            var remainingList = new List<List<Tuple<double, double, double>>>(polygons.Skip(1));
            while (remainingList.Count > 0)
            {
                int bestIdx = 0; double globalMinDist = double.MaxValue;
                for (int k = 0; k < remainingList.Count; k++)
                {
                    double d = double.MaxValue;
                    foreach (var pA in currentMerged) foreach (var pB in remainingList[k]) { double dist = GetDistance(pA, pB); if (dist < d) d = dist; }
                    if (d < globalMinDist) { globalMinDist = d; bestIdx = k; }
                }
                currentMerged = ConnectTwoPolygonsBothSides(currentMerged, remainingList[bestIdx], widthMeters);
                remainingList.RemoveAt(bestIdx);
            }
            return currentMerged;
        }

        private List<Tuple<Tuple<double, double, double>, Tuple<double, double, double>>> GetLinesFromLayer(DxfDocument? doc, string layer)
        {
            var lines = new List<Tuple<Tuple<double, double, double>, Tuple<double, double, double>>>();
            if (doc == null || string.IsNullOrEmpty(layer)) return lines;

            string targetLayer = layer.Trim();

            // 1. LINEの抽出
            foreach (var line in doc.Entities.Lines)
            {
                string entityLayer = line.Layer?.Name?.Trim() ?? "";
                if (string.Equals(entityLayer, targetLayer, StringComparison.OrdinalIgnoreCase))
                {
                    if (GetDistance(T(line.StartPoint), T(line.EndPoint)) > 0.0001)
                        lines.Add(Tuple.Create(T(line.StartPoint), T(line.EndPoint)));
                }
            }

            // 2. POLYLINE 2D の抽出
            foreach (var poly in doc.Entities.Polylines2D)
            {
                string entityLayer = poly.Layer?.Name?.Trim() ?? "";
                if (string.Equals(entityLayer, targetLayer, StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = 0; i < poly.Vertexes.Count; i++)
                    {
                        if (i < poly.Vertexes.Count - 1 || poly.IsClosed)
                        {
                            lines.Add(Tuple.Create(
                                T(poly.Vertexes[i].Position),
                                T(poly.Vertexes[(i + 1) % poly.Vertexes.Count].Position)
                            ));
                        }
                    }
                }
            }

            // 3. POLYLINE 3D の抽出
            foreach (var poly3d in doc.Entities.Polylines3D)
            {
                string entityLayer = poly3d.Layer?.Name?.Trim() ?? "";
                if (string.Equals(entityLayer, targetLayer, StringComparison.OrdinalIgnoreCase))
                {
                    for (int i = 0; i < poly3d.Vertexes.Count; i++)
                    {
                        if (i < poly3d.Vertexes.Count - 1 || poly3d.IsClosed)
                        {
                            lines.Add(Tuple.Create(
                                T(poly3d.Vertexes[i]),
                                T(poly3d.Vertexes[(i + 1) % poly3d.Vertexes.Count])
                            ));
                        }
                    }
                }
            }

            return lines.Where(l => GetDistance(l.Item1, l.Item2) > 0.0001).ToList();
        }

        private Tuple<double, double, double> T(Vector2 v) => Tuple.Create(v.Y, v.X, 0.0);
        private Tuple<double, double, double> T(Vector3 v) => Tuple.Create(v.Y, v.X, v.Z);

        private double GetDistance(Tuple<double, double, double> p1, Tuple<double, double, double> p2) => Math.Sqrt(Math.Pow(p1.Item1 - p2.Item1, 2) + Math.Pow(p1.Item2 - p2.Item2, 2));

        private List<List<Tuple<double, double, double>>> FindOuterPolygons(List<Tuple<Tuple<double, double, double>, Tuple<double, double, double>>> lines)
        {
            var nodes = new List<Tuple<double, double, double>>();
            var adj = new Dictionary<int, List<int>>();
            Func<Tuple<double, double, double>, int> GetId = pt => {
                for (int i = 0; i < nodes.Count; i++) if (GetDistance(nodes[i], pt) <= TOLERANCE) return i;
                nodes.Add(pt); adj[nodes.Count - 1] = new List<int>(); return nodes.Count - 1;
            };
            foreach (var line in lines)
            {
                int id1 = GetId(line.Item1), id2 = GetId(line.Item2);
                if (id1 != id2) { if (!adj[id1].Contains(id2)) adj[id1].Add(id2); if (!adj[id2].Contains(id1)) adj[id2].Add(id1); }
            }

            var unvisited = new HashSet<int>(Enumerable.Range(0, nodes.Count));
            var outerPolygons = new List<List<Tuple<double, double, double>>>();
            while (unvisited.Count > 0)
            {
                int startNode = unvisited.OrderBy(id => nodes[id].Item2).ThenBy(id => nodes[id].Item1).First();
                var component = new HashSet<int>(); var queue = new Queue<int>();
                queue.Enqueue(startNode); component.Add(startNode);
                while (queue.Count > 0) foreach (int nb in adj[queue.Dequeue()]) if (component.Add(nb)) queue.Enqueue(nb);

                List<int> path = new List<int> { startNode };
                int curr = startNode; double prevAng = Math.PI;
                for (int iter = 0; iter < component.Count * 10; iter++)
                {
                    int bestNb = -1; double minDiff = double.MaxValue;
                    foreach (int nb in adj[curr])
                    {
                        double diff = (Math.Atan2(nodes[nb].Item1 - nodes[curr].Item1, nodes[nb].Item2 - nodes[curr].Item2) - prevAng + Math.PI * 2) % (Math.PI * 2);
                        if (diff < 1e-7) diff = Math.PI * 2;
                        if (diff < minDiff) { minDiff = diff; bestNb = nb; }
                    }
                    if (bestNb == -1 || (path.Count > 1 && bestNb == startNode)) { path.Add(startNode); break; }
                    path.Add(bestNb);
                    prevAng = Math.Atan2(nodes[curr].Item1 - nodes[bestNb].Item1, nodes[curr].Item2 - nodes[bestNb].Item2);
                    curr = bestNb;
                }

                if (path.Count >= 4)
                {
                    var pts = path.Take(path.Count - 1).Select(id => nodes[id]).ToList();
                    if (CalculateArea(pts) > MIN_AREA) outerPolygons.Add(pts);
                }
                foreach (int id in component) unvisited.Remove(id);
            }
            return outerPolygons.OrderByDescending(CalculateArea).ToList();
        }

        private double CalculateArea(List<Tuple<double, double, double>> pts)
        {
            double area = 0;
            for (int i = 0; i < pts.Count; i++) area += pts[i].Item1 * pts[(i + 1) % pts.Count].Item2 - pts[(i + 1) % pts.Count].Item1 * pts[i].Item2;
            return Math.Abs(area / 2.0);
        }

        private List<Tuple<double, double, double>> ReorderClockwiseFromNortheast(List<Tuple<double, double, double>> pts)
        {
            int maxIdx = pts.Select((p, i) => new { p, i }).OrderByDescending(x => x.p.Item1 + x.p.Item2).First().i;
            var reordered = pts.Skip(maxIdx).Concat(pts.Take(maxIdx)).ToList();
            if (CalculateSignedArea(reordered) < 0) reordered = new[] { reordered[0] }.Concat(reordered.Skip(1).Reverse()).ToList();
            return reordered;
        }

        private double CalculateSignedArea(List<Tuple<double, double, double>> pts)
        {
            double area = 0;
            for (int i = 0; i < pts.Count; i++) area += pts[i].Item1 * pts[(i + 1) % pts.Count].Item2 - pts[(i + 1) % pts.Count].Item1 * pts[i].Item2;
            return area / 2.0;
        }

        private string WriteSimaContent(string koujimei, List<Tuple<int, string, double, double, double>> pts)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"G00,04,{koujimei},\r\nZ00,\r\nZ01,2,\r\nA00,\r\n");
            foreach (var p in pts) sb.Append($"A01,{p.Item1},{p.Item2},{p.Item3:F3},{p.Item4:F3},{(chkKeepZ.Checked ? $"{p.Item5:F3}" : "0.000")},\r\n");
            return sb.Append("END").ToString();
        }

        // GUI events
        private void lblFilePath_Click(object sender, EventArgs e) { }
        private void checkBox1_CheckedChanged(object sender, EventArgs e) { }
        private void chkSimplify_CheckedChanged(object sender, EventArgs e) { }
        private void numTolerance_ValueChanged(object sender, EventArgs e) { }
        private void lblToleranceUnit_Click(object sender, EventArgs e) { }
        private void chkBridgeIslands_CheckedChanged(object sender, EventArgs e) { }
        private void Form1_Load(object sender, EventArgs e) { }

        // Windowsメッセージを監視し、タイトルバーの「？」ボタンクリックを直接検出する
        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CONTEXTHELP = 0xF180;

            // タイトルバーの「？」ボタンが押された瞬間をキャッチ
            if (m.Msg == WM_SYSCOMMAND && (m.WParam.ToInt32() & 0xFFF0) == SC_CONTEXTHELP)
            {
                // ヘルプ要求イベント（Form1_HelpRequested）を直接呼び出してPDFを開く
                HelpEventArgs args = new HelpEventArgs(System.Drawing.Point.Empty);
                Form1_HelpRequested(this, args);

                // Windows標準の「カーソルを？にする処理」をキャンセルして終了
                return;
            }

            base.WndProc(ref m);
        }
    }
}