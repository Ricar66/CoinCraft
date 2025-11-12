using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Globalization;
using CoinCraft.App.ViewModels;
using CoinCraft.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using CoinCraft.Services;

namespace CoinCraft.App.Views;

public partial class DashboardWindow : Window
{
    private readonly DashboardViewModel _vm = new();
    public DashboardWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += async (_, __) =>
        {
            try
            {
                _vm.IsLoading = true;

                // Defaults for current month
                var today = DateTime.Today;
                var firstDay = new DateTime(today.Year, today.Month, 1);
                var lastDay = firstDay.AddMonths(1).AddDays(-1);
                DashFromPicker.SelectedDate = firstDay;
                DashToPicker.SelectedDate = lastDay;
                _vm.FilterFrom = firstDay;
                _vm.FilterTo = lastDay;

                await _vm.LoadAsync();
                await LoadRecentAsync();
                DrawPie();
                DrawBars();
                DrawNetWorthBars();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Erro ao carregar Dashboard", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _vm.IsLoading = false;
            }
        };
        SizeChanged += (_, __) => { DrawPie(); DrawBars(); DrawNetWorthBars(); };
    }

    private void DrawPie()
    {
        if (PieCanvas is null) return;
        PieCanvas.Children.Clear();
        var total = _vm.DespesasPorCategoria.Sum(s => s.Total);
        if (total <= 0) return;

        double cx = PieCanvas.ActualWidth > 0 ? PieCanvas.ActualWidth / 2 : 200;
        double cy = PieCanvas.ActualHeight > 0 ? PieCanvas.ActualHeight / 2 : 200;
        double radius = Math.Min(cx, cy) - 10;
        double startAngle = 0;

        foreach (var slice in _vm.DespesasPorCategoria)
        {
            var portion = (double)(slice.Total / total);
            var sweepAngle = portion * 360.0;
            var path = CreatePieSlice(cx, cy, radius, startAngle, sweepAngle, (Color)ColorConverter.ConvertFromString(slice.ColorHex));
            PieCanvas.Children.Add(path);
            startAngle += sweepAngle;
        }
    }

    private Path CreatePieSlice(double cx, double cy, double r, double startDeg, double sweepDeg, Color color)
    {
        double startRad = startDeg * Math.PI / 180.0;
        double endRad = (startDeg + sweepDeg) * Math.PI / 180.0;

        var x1 = cx + r * Math.Cos(startRad);
        var y1 = cy + r * Math.Sin(startRad);
        var x2 = cx + r * Math.Cos(endRad);
        var y2 = cy + r * Math.Sin(endRad);

        bool isLargeArc = sweepDeg > 180;

        var figure = new PathFigure { StartPoint = new System.Windows.Point(cx, cy) };
        figure.Segments.Add(new LineSegment(new System.Windows.Point(x1, y1), true));
        figure.Segments.Add(new ArcSegment(new System.Windows.Point(x2, y2), new System.Windows.Size(r, r), 0, isLargeArc, SweepDirection.Clockwise, true));
        figure.Segments.Add(new LineSegment(new System.Windows.Point(cx, cy), true));

        var geometry = new PathGeometry();
        geometry.Figures.Add(figure);

        var path = new Path
        {
            Fill = new SolidColorBrush(color),
            Stroke = Brushes.White,
            StrokeThickness = 1,
            Data = geometry
        };
        return path;
    }

    private async Task LoadRecentAsync()
    {
        // Carregar últimos lançamentos diretamente do banco (Top 10)
        using var db = new CoinCraftDbContext();

        var from = _vm.FilterFrom ?? DateTime.Today.AddDays(-30);
        var to = _vm.FilterTo ?? DateTime.Today;

        var accounts = await db.Accounts.ToDictionaryAsync(a => a.Id, a => a.Nome);
        var categories = await db.Categories.ToDictionaryAsync(c => c.Id, c => c.Nome);

        var recent = await db.Transactions
            .Where(t => t.Data >= from && t.Data <= to)
            .OrderByDescending(t => t.Data)
            .Take(10)
            .Select(t => new TransactionItem
            {
                Id = t.Id,
                Data = t.Data,
                Tipo = t.Tipo,
                Valor = t.Valor,
                AccountId = t.AccountId,
                CategoryId = t.CategoryId,
                Descricao = t.Descricao,
                OpostoAccountId = t.OpostoAccountId,
                AccountName = accounts.ContainsKey(t.AccountId) ? accounts[t.AccountId] : $"#{t.AccountId}",
                CategoryName = t.CategoryId.HasValue && categories.ContainsKey(t.CategoryId.Value) ? categories[t.CategoryId.Value] : null
            })
            .ToListAsync();

        _vm.RecentTransactions = new ObservableCollection<TransactionItem>(recent);
    }

    private void DrawBars()
    {
        if (BarsGrid is null || ReceitasBar is null || DespesasBar is null) return;
        var total = (double)(_vm.TotalReceitas + _vm.TotalDespesas);
        if (total <= 0)
        {
            ReceitasBar.Width = 0;
            DespesasBar.Width = 0;
            return;
        }
        var width = BarsGrid.ActualWidth > 0 ? BarsGrid.ActualWidth : 420;
        ReceitasBar.Width = width * (double)_vm.TotalReceitas / total;
        DespesasBar.Width = width * (double)_vm.TotalDespesas / total;
    }

    private void DrawNetWorthBars()
    {
        if (NetWorthCanvas is null) return;
        NetWorthCanvas.Children.Clear();
        var months = GetSelectedNetWorthMonths();
        var points = new ReportService().GetNetWorthHistory(months);
        if (points is null || points.Count == 0) return;
        double width = NetWorthCanvas.ActualWidth > 0 ? NetWorthCanvas.ActualWidth : 800;
        double height = NetWorthCanvas.ActualHeight > 0 ? NetWorthCanvas.ActualHeight : 160;
        double topPadding = 4;
        double bottomLabelArea = 18;
        double plotHeight = height - topPadding - bottomLabelArea;

        var min = points.Min(p => p.NetWorth);
        var max = points.Max(p => p.NetWorth);
        var rangeDec = Math.Max(1m, max - min);
        double range = (double)rangeDec;

        int n = points.Count;
        double barWidth = Math.Max(12, width / n - 6);
        double gap = Math.Max(4, (width - n * barWidth) / (n + 1));
        double x = gap;

        // Zero line position
        double zeroRatio = (double)((0m - min) / rangeDec); // 0..1 within [min,max]
        double zeroY = topPadding + (1 - zeroRatio) * plotHeight;

        // Draw zero reference line
        var zeroLine = new Line
        {
            X1 = 0,
            X2 = width,
            Y1 = zeroY,
            Y2 = zeroY,
            Stroke = Brushes.Gray,
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 2, 2 }
        };
        NetWorthCanvas.Children.Add(zeroLine);

        // Scale labels (min, zero, max)
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        AddScaleLabel(2, topPadding, max.ToString("C", culture));
        AddScaleLabel(2, zeroY - 8, 0m.ToString("C", culture));
        AddScaleLabel(2, topPadding + plotHeight - 12, min.ToString("C", culture));

        for (int i = 0; i < n; i++)
        {
            var p = points[i];
            var value = p.NetWorth;

            double valueRatio = (double)((value - min) / rangeDec); // 0..1
            double valueY = topPadding + (1 - valueRatio) * plotHeight;

            // Height relative to zero
            double h = Math.Abs(valueY - zeroY);
            h = Math.Max(2, h);

            bool positive = value >= 0m;
            var fill = positive
                ? (Brush)new BrushConverter().ConvertFromString("#42A5F5")
                : (Brush)new BrushConverter().ConvertFromString("#EF5350");

            var rect = new Rectangle
            {
                Width = barWidth,
                Height = h,
                Fill = fill,
                Stroke = Brushes.Transparent
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, positive ? valueY : zeroY);
            NetWorthCanvas.Children.Add(rect);

            ToolTipService.SetToolTip(rect, $"{p.Month:00}/{p.Year}: {value.ToString("C", culture)}");

            var label = new System.Windows.Controls.TextBlock
            {
                Text = $"{p.Month:00}/{p.Year % 100}",
                FontSize = 10,
                Foreground = Brushes.Gray
            };
            Canvas.SetLeft(label, x);
            Canvas.SetTop(label, height - 16);
            NetWorthCanvas.Children.Add(label);

            x += barWidth + gap;
        }
    }

    private void AddScaleLabel(double x, double y, string text)
    {
        var tb = new System.Windows.Controls.TextBlock
        {
            Text = text,
            FontSize = 10,
            Foreground = Brushes.Gray
        };
        Canvas.SetLeft(tb, x);
        Canvas.SetTop(tb, y);
        NetWorthCanvas.Children.Add(tb);
    }

    private int GetSelectedNetWorthMonths()
    {
        try
        {
            if (NetWorthPeriodCombo?.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out var months))
                return months;
        }
        catch { }
        return 12;
    }

    private void OnNetWorthPeriodChanged(object sender, SelectionChangedEventArgs e)
    {
        DrawNetWorthBars();
    }

    private async void OnApplyDashFilters(object sender, RoutedEventArgs e)
    {
        if (_vm.IsLoading) return;
        try
        {
            _vm.IsLoading = true;
            _vm.FilterFrom = DashFromPicker.SelectedDate;
            _vm.FilterTo = DashToPicker.SelectedDate;
            await _vm.LoadAsync();
            await LoadRecentAsync();
            DrawPie();
            DrawBars();
            DrawNetWorthBars();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao aplicar filtros", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _vm.IsLoading = false;
        }
    }
}