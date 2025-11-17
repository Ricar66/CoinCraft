using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Globalization;
using System.Windows.Input;
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
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.ToString(), "Erro ao carregar Dashboard", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
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
        _netWorthChart ??= new CartesianChart { Height = 140 };
        _netWorthChart.Series = _vm.NetWorthSeries?.ToList() ?? new System.Collections.Generic.List<ISeries>();
        _netWorthChart.XAxes = _vm.NetWorthXAxis ?? System.Array.Empty<LiveChartsCore.SkiaSharpView.Axis>();
        NetWorthHost.Children.Clear();
        NetWorthHost.Children.Add(_netWorthChart);
    }

    private void RenderComparisonChart()
    {
        if (ComparisonHost is null) return;
        ComparisonHost.Children.Clear();

        var receitas = Math.Max((double)_vm.TotalReceitas, 0);
        var despesas = Math.Max((double)_vm.TotalDespesas, 0);
        // Evita ambas zero para manter layout
        if (receitas == 0 && despesas == 0) { receitas = 1; }

        var barHeight = 24; // altura idêntica ao screenshot
        var bar = new Grid { Height = barHeight, VerticalAlignment = VerticalAlignment.Center };
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(receitas, GridUnitType.Star) });
        bar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(despesas, GridUnitType.Star) });

        // Cores equivalentes ao visual do screenshot
        var greenBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#66C37A");
        var redBrush = (SolidColorBrush)new BrushConverter().ConvertFromString("#E57373");
        var green = new Border { Background = greenBrush, Height = barHeight };
        var red = new Border { Background = redBrush, Height = barHeight };
        Grid.SetColumn(green, 0);
        Grid.SetColumn(red, 1);
        bar.Children.Add(green);
        bar.Children.Add(red);

        // Sem moldura, sem margem lateral — igual à imagem
        ComparisonHost.Children.Add(bar);
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key == Key.D) { OnOpenDashboardClick(this, new RoutedEventArgs()); e.Handled = true; return; }
            if (e.Key == Key.L) { OnOpenTransactionsClick(this, new RoutedEventArgs()); e.Handled = true; return; }
            if (e.Key == Key.A) { OnOpenAccountsClick(this, new RoutedEventArgs()); e.Handled = true; return; }
            if (e.Key == Key.C) { OnOpenCategoriesClick(this, new RoutedEventArgs()); e.Handled = true; return; }
            if (e.Key == Key.R) { OnOpenRecurringClick(this, new RoutedEventArgs()); e.Handled = true; return; }
            if (e.Key == Key.I) { OnOpenImportClick(this, new RoutedEventArgs()); e.Handled = true; return; }
            if (e.Key == Key.S) { OnOpenSettingsClick(this, new RoutedEventArgs()); e.Handled = true; return; }
        }
        if (e.Key == Key.Escape)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
        }
        else if (e.Key == Key.F11)
        {
            if (WindowStyle == WindowStyle.None)
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowStyle = WindowStyle.None;
                WindowState = WindowState.Maximized;
            }
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnToggleMaximizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnOpenDashboardClick(object sender, RoutedEventArgs e)
    {
        ShellFrame.Visibility = Visibility.Collapsed;
        MainContentGrid.Visibility = Visibility.Visible;
        WindowState = WindowState.Maximized;
        Activate();
    }

    private void OnOpenTransactionsClick(object sender, RoutedEventArgs e)
    {
        ShellFrame.Visibility = Visibility.Visible;
        MainContentGrid.Visibility = Visibility.Collapsed;
        ShellFrame.Content = new CoinCraft.App.Views.Controls.TransactionsControl();
    }

    private void OnOpenAccountsClick(object sender, RoutedEventArgs e)
    {
        ShellFrame.Visibility = Visibility.Visible;
        MainContentGrid.Visibility = Visibility.Collapsed;
        ShellFrame.Content = new CoinCraft.App.Views.Controls.AccountsControl();
    }

    private void OnOpenCategoriesClick(object sender, RoutedEventArgs e)
    {
        ShellFrame.Visibility = Visibility.Visible;
        MainContentGrid.Visibility = Visibility.Collapsed;
        ShellFrame.Content = new CoinCraft.App.Views.Controls.CategoriesControl();
    }

    private void OnOpenRecurringClick(object sender, RoutedEventArgs e)
    {
        ShellFrame.Visibility = Visibility.Visible;
        MainContentGrid.Visibility = Visibility.Collapsed;
        ShellFrame.Content = new CoinCraft.App.Views.Controls.RecurringControl();
    }

    private void OnOpenImportClick(object sender, RoutedEventArgs e)
    {
        ShellFrame.Visibility = Visibility.Visible;
        MainContentGrid.Visibility = Visibility.Collapsed;
        ShellFrame.Content = new CoinCraft.App.Views.Controls.ImportControl();
    }

    private void OnOpenSettingsClick(object sender, RoutedEventArgs e)
    {
        ShellFrame.Visibility = Visibility.Visible;
        MainContentGrid.Visibility = Visibility.Collapsed;
        ShellFrame.Content = new CoinCraft.App.Views.Controls.SettingsControl();
    }

    private void OnHamburgerClick(object sender, RoutedEventArgs e)
    {
        if (NavPanel is null) return;
        NavPanel.Visibility = NavPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }
}