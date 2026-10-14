namespace Sokoban.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    internal Sokoban.Models.Board Board { get; } = new Sokoban.Models.Board();
}
