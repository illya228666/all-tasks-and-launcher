using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using CRaumschiff.WPF.Presentation;

namespace CRaumschiff.WPF
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel(PostToUi);
            DataContext = _viewModel;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void PostToUi(Action action)
        {
            if (!Dispatcher.HasShutdownStarted) Dispatcher.BeginInvoke(action);
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.PlayerX):
                case nameof(MainViewModel.PlayerY):
                case nameof(MainViewModel.EnemyX):
                case nameof(MainViewModel.EnemyY):
                    PositioniereSchiffe();
                    break;
                case nameof(MainViewModel.XError):
                case nameof(MainViewModel.YError):
                    if (_viewModel.XError != "") EnemyXBox.Focus();
                    else if (_viewModel.YError != "") EnemyYBox.Focus();
                    break;
            }
        }

        private void PositioniereSchiffe()
        {
            if (_viewModel == null) return;
            Positioniere(PlayerImage, _viewModel.PlayerX, _viewModel.PlayerY);
            Positioniere(EnemyImage, _viewModel.EnemyX, _viewModel.EnemyY);
        }

        private void Positioniere(Image bild, int x, int y)
        {
            Canvas.SetLeft(bild, x / 800.0 * Math.Max(0, SpaceCanvas.ActualWidth - bild.Width));
            Canvas.SetTop(bild, y / 600.0 * Math.Max(0, SpaceCanvas.ActualHeight - bild.Height));
        }

        private void SpaceCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            PositioniereSchiffe();
        }

        private void LogBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            LogBox.ScrollToEnd();
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            _viewModel.Dispose();
            base.OnClosed(e);
        }
    }
}
