using System.Windows;
using Hotel.Pages;

namespace Hotel
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string AccessLevel { get; private set; } = "none";
        public int? AssociatedId { get; private set; } = null;

        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new LoginPage());
        }

        public void SetAccessLevel(string level, int? id)
        {
            AccessLevel = level;
            AssociatedId = id;
        }
    }
}