using Avalonia.Controls;
using Avalonia.Interactivity;
using Sokoban.ViewModels;

namespace Sokoban.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        BoardView.Focus();
    }

    private void OnResetClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Board.Reset();
            BoardView.InvalidateVisual();
            BoardView.Focus();
        }
    }

    private void OnUndoClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Board.Undo();
            BoardView.InvalidateVisual();
            BoardView.Focus();
        }
    }
}
