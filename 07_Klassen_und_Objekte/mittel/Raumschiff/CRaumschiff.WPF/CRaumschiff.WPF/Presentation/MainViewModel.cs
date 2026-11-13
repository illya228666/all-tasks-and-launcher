using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using RaumschiffModell = global::Raumschiff.Core.CRaumschiff;

namespace CRaumschiff.WPF.Presentation
{
    internal sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly Action<Action> _postToUi;
        private readonly RelayCommand _createPlayerCommand;
        private readonly RelayCommand _createEnemyCommand;
        private readonly RelayCommand _deleteCommand;
        private RaumschiffModell _player;
        private RaumschiffModell _enemy;
        private string _enemyXText = "310";
        private string _enemyYText = "300";
        private volatile bool _disposed;

        public MainViewModel(Action<Action> postToUi)
        {
            _postToUi = postToUi ?? throw new ArgumentNullException(nameof(postToUi));
            _createPlayerCommand = new RelayCommand(CreatePlayer, () => !_disposed && !HasPlayer);
            _createEnemyCommand = new RelayCommand(CreateEnemy, () => !_disposed && !HasEnemy);
            _deleteCommand = new RelayCommand(DeleteShips, () => !_disposed && (HasPlayer || HasEnemy));
            RaumschiffModell.OnMessage += ReceiveMessage;
        }

        public ICommand CreatePlayerCommand => _createPlayerCommand;
        public ICommand CreateEnemyCommand => _createEnemyCommand;
        public ICommand DeleteCommand => _deleteCommand;
        public bool HasPlayer => _player != null;
        public bool HasEnemy => _enemy != null;
        public bool CanEditEnemy => !_disposed && !HasEnemy;
        public int PlayerX => _player?.XPos ?? 0;
        public int PlayerY => _player?.YPos ?? 0;
        public int EnemyX => _enemy?.XPos ?? 0;
        public int EnemyY => _enemy?.YPos ?? 0;
        public string PlayerToolTip => $"Spieler · X: {PlayerX}, Y: {PlayerY}";
        public string EnemyToolTip => $"Gegner · X: {EnemyX}, Y: {EnemyY}";
        public string XError { get; private set; } = "";
        public string YError { get; private set; } = "";
        public string LogText { get; private set; } = "";

        public string EnemyXText
        {
            get { return _enemyXText; }
            set
            {
                if (_enemyXText == value) return;
                _enemyXText = value;
                OnPropertyChanged();
            }
        }

        public string EnemyYText
        {
            get { return _enemyYText; }
            set
            {
                if (_enemyYText == value) return;
                _enemyYText = value;
                OnPropertyChanged();
            }
        }

        private void CreatePlayer()
        {
            _player = new RaumschiffModell();
            _player.PropertyChanged += ShipPropertyChanged;
            RefreshShips();
        }

        private void CreateEnemy()
        {
            bool xValid = int.TryParse(EnemyXText, out int x);
            bool yValid = int.TryParse(EnemyYText, out int y);
            XError = xValid ? "" : "Bitte eine ganze Zahl eingeben.";
            YError = yValid ? "" : "Bitte eine ganze Zahl eingeben.";
            OnPropertyChanged(nameof(XError));
            OnPropertyChanged(nameof(YError));
            if (!xValid || !yValid) return;

            _enemy = new RaumschiffModell(x, y);
            _enemy.PropertyChanged += ShipPropertyChanged;
            RefreshShips();
        }

        private void DeleteShips()
        {
            ReleaseShips();
            RefreshShips();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void ReleaseShips()
        {
            if (_player != null) _player.PropertyChanged -= ShipPropertyChanged;
            if (_enemy != null) _enemy.PropertyChanged -= ShipPropertyChanged;
            _player = null;
            _enemy = null;
        }

        private void RefreshShips()
        {
            OnPropertyChanged(nameof(HasPlayer));
            OnPropertyChanged(nameof(HasEnemy));
            OnPropertyChanged(nameof(CanEditEnemy));
            NotifyCoordinates();
            _createPlayerCommand.RaiseCanExecuteChanged();
            _createEnemyCommand.RaiseCanExecuteChanged();
            _deleteCommand.RaiseCanExecuteChanged();
        }

        private void NotifyCoordinates()
        {
            OnPropertyChanged(nameof(PlayerX));
            OnPropertyChanged(nameof(PlayerY));
            OnPropertyChanged(nameof(EnemyX));
            OnPropertyChanged(nameof(EnemyY));
            OnPropertyChanged(nameof(PlayerToolTip));
            OnPropertyChanged(nameof(EnemyToolTip));
        }

        private void ShipPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.PropertyName)
                && e.PropertyName != nameof(RaumschiffModell.XPos)
                && e.PropertyName != nameof(RaumschiffModell.YPos)) return;

            _postToUi(() =>
            {
                if (!_disposed) NotifyCoordinates();
            });
        }

        private void ReceiveMessage(string message)
        {
            if (_disposed) return;

            // Asynchron, damit Finalisierer beim Warten auf sie nicht auf den UI-Thread warten.
            _postToUi(() =>
            {
                if (_disposed) return;
                LogText += message + Environment.NewLine;
                OnPropertyChanged(nameof(LogText));
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            RaumschiffModell.OnMessage -= ReceiveMessage;
            ReleaseShips();
            RefreshShips();
        }
    }
}
