using System;
using System.ComponentModel;
using System.Windows.Input;

namespace KuFi.UI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private Uri _currentPage;
        
        // Global state untuk melacak apakah sistem sedang aman atau butuh tindakan (Action Required)
        public static bool IsSystemSecured { get; set; } = true;

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

        public RelayCommand(Action<object?> execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public bool CanExecute(object? parameter) => true; 
        public void Execute(object? parameter) => _execute(parameter);
        
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }
    }
}
