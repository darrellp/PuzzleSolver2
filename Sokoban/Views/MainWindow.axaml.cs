using Avalonia.Controls;
using Avalonia.Interactivity;

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
}
