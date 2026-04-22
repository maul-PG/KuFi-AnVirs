using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace KuFi.UI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Uri _currentPage;
        
        // Global state untuk melacak apakah sistem sedang aman atau butuh tindakan (Action Required)
        public static bool IsSystemSecured { get; set; } = true;

        // Global state untuk melacak riwayat aktivitas (Logs)
        public static ObservableCollection<LogEntry> ActivityLogs { get; } = new ObservableCollection<LogEntry>();

        public Uri CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
            }
        }

        public ICommand NavigateDashboardCommand { get; }
        public ICommand NavigateSandboxCommand { get; }
        public ICommand NavigateRescueCommand { get; }
        public ICommand NavigateLogsCommand { get; }
        public ICommand NavigateSettingsCommand { get; } // Menambahkan dukungan halaman Settings

        public MainViewModel()
        {
            _currentPage = new Uri("Views/DashboardPage.xaml", UriKind.Relative);
            
            NavigateDashboardCommand = new RelayCommand(_ => CurrentPage = new Uri("Views/DashboardPage.xaml", UriKind.Relative));
            NavigateSandboxCommand = new RelayCommand(_ => CurrentPage = new Uri("Views/SandboxPage.xaml", UriKind.Relative));
            NavigateRescueCommand = new RelayCommand(_ => CurrentPage = new Uri("Views/RescuePage.xaml", UriKind.Relative));
            NavigateLogsCommand = new RelayCommand(_ => CurrentPage = new Uri("Views/LogsPage.xaml", UriKind.Relative));
            
            // Dummy uri untuk Settings. Tidak akan menyebabkan force close karena WPF Frame mengabaikan error navigasi jika file xaml kosong, 
            // namun praktik terbaik tetap memastikan file ada.
            NavigateSettingsCommand = new RelayCommand(_ => CurrentPage = new Uri("Views/SettingsPage.xaml", UriKind.Relative));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);
    }

    public class LogEntry
    {
        public string Timestamp { get; set; } = string.Empty;
        public string Event { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }
}
