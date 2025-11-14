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
using Microsoft.Extensions.DependencyInjection;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.WPF;
using LiveChartsCore.Measure;

namespace CoinCraft.App.Views;

public partial class DashboardWindow : Window
{
    private readonly DashboardViewModel _vm = App.Services!.GetRequiredService<DashboardViewModel>();
    private PieChart? _pieChart;
    private CartesianChart? _netWorthChart;
    private CartesianChart? _comparisonChart;
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
                await _vm.LoadRecentAsync();
                _vm.UpdateNetWorthSeries(GetSelectedNetWorthMonths());
                RenderPieChart();
                RenderNetWorthChart();
                RenderComparisonChart();
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
        // Removido: barras manuais; gráfico é renderizado via LiveCharts
    }

    // Carregamento de recentes agora é responsabilidade do ViewModel


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
        _vm.UpdateNetWorthSeries(GetSelectedNetWorthMonths());
        RenderNetWorthChart();
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
            await _vm.LoadRecentAsync();
            _vm.UpdateNetWorthSeries(GetSelectedNetWorthMonths());
            RenderPieChart();
            RenderNetWorthChart();
            RenderComparisonChart();
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

    private void RenderPieChart()
    {
        if (PieHost is null) return;
        _pieChart ??= new PieChart { LegendPosition = LegendPosition.Right };
        _pieChart.Series = _vm.PieSeries?.ToList() ?? new System.Collections.Generic.List<ISeries>();
        PieHost.Children.Clear();
        PieHost.Children.Add(_pieChart);
    }

    private void RenderNetWorthChart()
    {
        if (NetWorthHost is null) return;
        _netWorthChart ??= new CartesianChart { Height = 160 };
        _netWorthChart.Series = _vm.NetWorthSeries?.ToList() ?? new System.Collections.Generic.List<ISeries>();
        _netWorthChart.XAxes = _vm.NetWorthXAxis ?? System.Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();
        NetWorthHost.Children.Clear();
        NetWorthHost.Children.Add(_netWorthChart);
    }

    private void RenderComparisonChart()
    {
        if (ComparisonHost is null) return;
        _comparisonChart ??= new CartesianChart { Height = 160 };
        _comparisonChart.Series = _vm.ComparisonSeries?.ToList() ?? new System.Collections.Generic.List<ISeries>();
        _comparisonChart.XAxes = _vm.ComparisonXAxis ?? System.Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();
        ComparisonHost.Children.Clear();
        ComparisonHost.Children.Add(_comparisonChart);
    }
}