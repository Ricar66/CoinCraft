using System.Windows;
using CoinCraft.App.ViewModels;
using CoinCraft.Domain;
using System;

namespace CoinCraft.App.Views;

public partial class RecurringWindow : Window
{
    private readonly RecurringViewModel _vm = new();

    public RecurringWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += async (_, __) =>
        {
            await _vm.LoadAsync();
            AccountFilter.ItemsSource = _vm.Accounts;
            CategoryFilter.ItemsSource = _vm.Categories;
            FrequencyFilter.ItemsSource = Enum.GetValues(typeof(RecurrenceFrequency));
        };
    }

    private async void OnRefreshClick(object sender, RoutedEventArgs e)
    {
        await _vm.LoadAsync();
    }

    private async void OnAddClick(object sender, RoutedEventArgs e)
    {
        var editor = new RecurringEditWindow(_vm) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            var r = editor.ResultRecurring;
            if (r is not null)
            {
                await _vm.AddAsync(r);
                await _vm.LoadAsync();
            }
        }
    }

    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        var editor = new RecurringEditWindow(_vm, _vm.Selected) { Owner = this };
        if (editor.ShowDialog() == true)
        {
            var r = editor.ResultRecurring;
            if (r is not null)
            {
                r.Id = _vm.Selected.Id;
                await _vm.UpdateAsync(r);
                await _vm.LoadAsync();
            }
        }
    }

    private async void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (_vm.Selected is null) return;
        if (MessageBox.Show("Excluir recorrente?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            await _vm.DeleteAsync(_vm.Selected.Id);
            await _vm.LoadAsync();
        }
    }

    private async void OnApplyFilters(object sender, RoutedEventArgs e)
    {
        _vm.FilterAccountId = (int?)AccountFilter.SelectedValue;
        _vm.FilterCategoryId = (int?)CategoryFilter.SelectedValue;
        _vm.FilterFrequency = FrequencyFilter.SelectedItem is RecurrenceFrequency rf ? rf : null;
        _vm.FilterNome = string.IsNullOrWhiteSpace(NameFilter.Text) ? null : NameFilter.Text.Trim();
        _vm.FilterFrom = FromPicker.SelectedDate;
        _vm.FilterTo = ToPicker.SelectedDate;
        await _vm.LoadAsync();
    }
}