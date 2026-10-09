using System.ComponentModel;
using System.Windows;
using EspController.Presentation;

namespace EspController;

public partial class MainWindow : Window
{
    private readonly ControllerViewModel _controller;
    private bool _closing, _readyToClose;

    public MainWindow() : this(new ControllerViewModel(System.Windows.Threading.Dispatcher.CurrentDispatcher)) { }

    internal MainWindow(ControllerViewModel controller)
    {
        InitializeComponent();
        _controller = controller;
        DataContext = controller;
        Loaded += async (_, _) => await _controller.StartAsync();
        Closing += CloseController;
    }

    private async void CloseController(object? sender, CancelEventArgs e)
    {
        if (_readyToClose || e.Cancel) return;
        e.Cancel = true;
        if (_closing) return;
        _closing = true;
        IsEnabled = false;
        try { await _controller.DisposeAsync(); }
        finally { _readyToClose = true; Close(); }
    }
}
