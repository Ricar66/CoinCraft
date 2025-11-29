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
        var vb = new Viewbox { Stretch = System.Windows.Media.Stretch.Uniform };
        vb.Child = _pieChart;
        PieHost.Children.Clear();
        PieHost.Children.Add(vb);
    }

    private void RenderNetWorthChart()
    {
        if (NetWorthHost is null) return;
        _netWorthChart ??= new CartesianChart { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        _netWorthChart.Series = _vm.NetWorthSeries?.ToList() ?? new System.Collections.Generic.List<ISeries>();
        _netWorthChart.XAxes = _vm.NetWorthXAxis ?? System.Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();
        var vb = new Viewbox { Stretch = System.Windows.Media.Stretch.Fill };
        vb.Child = _netWorthChart;
        NetWorthHost.Children.Clear();
        NetWorthHost.Children.Add(vb);
    }

    private void OnGoDashboard(object sender, RoutedEventArgs e)
    {
        Activate();
    }

    private void OnGoTransactions(object sender, RoutedEventArgs e)
    {
        var win = new TransactionsWindow { Owner = this };
        win.Show();
    }

    private void OnGoAccounts(object sender, RoutedEventArgs e)
    {
        var win = new AccountsWindow { Owner = this };
        win.Show();
    }

    private void OnGoCategories(object sender, RoutedEventArgs e)
    {
        var win = new CategoriesWindow { Owner = this };
        win.Show();
    }

    private void OnGoRecurring(object sender, RoutedEventArgs e)
    {
        var win = new RecurringWindow { Owner = this };
        win.Show();
    }

    private void OnGoImport(object sender, RoutedEventArgs e)
    {
        var win = new ImportWindow { Owner = this };
        win.Show();
    }

    private void OnGoSettings(object sender, RoutedEventArgs e)
    {
        var vm = App.Services!.GetRequiredService<CoinCraft.App.ViewModels.SettingsViewModel>();
        var win = new SettingsWindow(vm) { Owner = this };
        win.Show();
    }

    private void RenderComparisonChart()
    {
        // Cartesiano vinculado via XAML (ComparisonSeries / ComparisonXAxis)
        // Nada a renderizar manualmente aqui.
    }
}
