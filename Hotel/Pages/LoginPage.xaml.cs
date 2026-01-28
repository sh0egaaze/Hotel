using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Hotel.Pages
{
    public partial class LoginPage : Page
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = tbLogin.Text.Trim();
            string password = pbPassword.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                lblMessage.Content = "Пожалуйста, заполните все поля.";
                lblMessage.Visibility = Visibility.Visible;
                return;
            }

            var context = Entities.GetContext();
            string accessLevel = "unknown";
            int? associatedId = null; 

            var employeeAcc = context.EmployeeAccount
                                    .FirstOrDefault(acc => acc.login == login && acc.password == password);

            if (employeeAcc != null)
            {
                int roleId = employeeAcc.roleId ?? 0;
                string roleName = context.Role
                                        .Where(r => r.id == roleId)
                                        .Select(r => r.name)
                                        .FirstOrDefault();

                if (roleName == "Владелец")
                {
                    accessLevel = "owner";
                }
                else if (roleName == "Администратор")
                {
                    accessLevel = "admin";
                }
                else
                {
                    accessLevel = "employee";
                }
                associatedId = employeeAcc.employeeId; 
            }
            else
            {
                var clientAcc = context.ClientAccount
                                      .FirstOrDefault(acc => acc.login == login && acc.password == password);

                if (clientAcc != null)
                {
                    accessLevel = "client";
                    associatedId = clientAcc.clientId; 
                }
            }

            if (accessLevel != "unknown")
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.SetAccessLevel(accessLevel, associatedId);
                    if (accessLevel == "client")
                    {
                        this.NavigationService.Navigate(new BasePage(associatedId));
                    }
                    else
                    {
                        this.NavigationService.Navigate(new BasePage());
                    }
                }
            }
            else
            {
                lblMessage.Content = "Неверный логин или пароль.";
                lblMessage.Visibility = Visibility.Visible;
            }
        }
    }
}