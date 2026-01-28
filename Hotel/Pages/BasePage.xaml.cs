using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data.Entity;

namespace Hotel.Pages
{
    /// <summary>
    /// Логика взаимодействия для BasePage.xaml
    /// </summary>
    public partial class BasePage : Page
    {
        private int? _currentClientId = null; 

        public BasePage()
        {
            InitializeComponent();
            ApplyAccessControl();
            LoadData();
        }

        public BasePage(int? clientId)
        {
            _currentClientId = clientId;
            InitializeComponent();
            ApplyAccessControl();
            LoadData();
        }

        private void LoadData()
        {
            lvClientDetails.Visibility = Visibility.Collapsed;
            var context = Entities.GetContext();
            var paymentBillsQuery = context.PaymentBill
                                           .Include(b => b.Client)
                                           .Include(b => b.Booking)
                                           .Include(b => b.Booking.Room)
                                           .Include(b => b.Booking.Room.Category);

            if (_currentClientId.HasValue)
            {
                paymentBillsQuery = paymentBillsQuery.Where(b => b.clientId == _currentClientId);
            }

            var paymentBills = paymentBillsQuery.ToList();

            var clientList = new List<dynamic>();
            foreach (var bill in paymentBills)
            {
                string clientName = "Нет данных";
                if (bill.Client != null)
                {
                    clientName = $"{bill.Client.lastName} {bill.Client.firstName} {bill.Client.middleName}";
                }
                string roomNum = "-";
                string roomCat = "-";
                if (bill.Booking != null && bill.Booking.Room != null)
                {
                    roomNum = bill.Booking.Room.number.ToString();
                    if (bill.Booking.Room.Category != null)
                    {
                        roomCat = bill.Booking.Room.Category.name;
                    }
                }
                string dateRange = "Не указано";
                if (bill.Booking != null)
                {
                    dateRange = $"с {bill.Booking.dateFrom:dd.MM.yyyy}\nпо {bill.Booking.dateTill:dd.MM.yyyy}";
                }

                var servicesWithQuantities = new List<string>();
                if (_currentClientId.HasValue)
                {
                    var serviceInBillsForThisBill = context.ServiceInBill
                                                          .Include(sib => sib.Service)
                                                          .Where(sib => sib.billId == bill.id)
                                                          .ToList();

                    foreach (var sib in serviceInBillsForThisBill)
                    {
                        if (sib.Service != null)
                        {
                            servicesWithQuantities.Add($"{sib.Service.name} ({sib.amount} шт.)");
                        }
                    }
                }

                string servicesStr = servicesWithQuantities.Any() ? string.Join(", ", servicesWithQuantities) : "-";

                clientList.Add(new
                {
                    ClientName = clientName,
                    RoomNum = roomNum,
                    RoomCat = roomCat,
                    DateRange = dateRange,
                    Services = servicesStr,
                    Amount = bill.amount
                });
            }
            lvClientDetails.ItemsSource = clientList;

            IQueryable<Booking> bookingsQuery = Entities.GetContext().Booking.AsQueryable();

            if (_currentClientId.HasValue)
            {
                bookingsQuery = bookingsQuery.Where(b => b.clientId == _currentClientId);
            }

            bookingsQuery = bookingsQuery.Include(b => b.Room)
                                         .Include(b => b.Room.Category)
                                         .Include(b => b.PaymentBill); 

            if (dpStart.SelectedDate != null)
            {
                DateTime dateStart = dpStart.SelectedDate.Value;
                bookingsQuery = bookingsQuery.Where(b => b.dateFrom >= dateStart);
            }
            if (dpEnd.SelectedDate != null)
            {
                DateTime dateEnd = dpEnd.SelectedDate.Value;
                bookingsQuery = bookingsQuery.Where(b => b.dateFrom <= dateEnd);
            }

            List<Booking> bookings = bookingsQuery.ToList();
            dgBookings.ItemsSource = bookings; 
            dgBookings.SelectedItem = null;
        }
        private void btnFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void btnShowAll_Click(object sender, RoutedEventArgs e)
        {
            dpStart.SelectedDate = null;
            dpEnd.SelectedDate = null;
            LoadData();
        }

        private void btnCreate_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null && mainWindow.AccessLevel == "client" && mainWindow.AssociatedId.HasValue)
            {
                NavigationService.Navigate(new ClientBookingPage(mainWindow.AssociatedId.Value));
            }
            else
            {
                NavigationService.Navigate(new BookingPage());
            }
        }
        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                if (mainWindow.AccessLevel == "client" || mainWindow.AccessLevel == "admin")
                {
                    MessageBox.Show("Удаление бронирований недоступно.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            Booking booking = dgBookings.SelectedItem as Booking;
            if (booking != null)
            {
                var result = MessageBox.Show($"Удалить выбранное бронирование?", "Подтверждение удаления",
                                             MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Entities.GetContext().Booking.Remove(booking);
                    Entities.GetContext().SaveChanges();
                    MessageBox.Show("Бронирование удалено", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    dgBookings.SelectedItem = null;
                }
            }
            else
            {
                MessageBox.Show("Выберите запись для удаления", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void btnnavToClient_Click(object sender, RoutedEventArgs e)
        {
            if (_currentClientId.HasValue) 
            {
                MessageBox.Show("Просмотр списка всех клиентов недоступен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            ShowClientInfo();
        }

        private void btnShowBooking_Click(object sender, RoutedEventArgs e)
        {
            ShowBookingInfo();
        }

        private void btnRoomsBooked_Click(object sender, RoutedEventArgs e)
        {
            if (_currentClientId.HasValue) 
            {
                MessageBox.Show("Показатель степени загрузки недоступен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            CalcRoomBooked();
        }

        private void btnShowADR_Click(object sender, RoutedEventArgs e)
        {
            if (_currentClientId.HasValue) 
            {
                MessageBox.Show("Показатель ADR недоступен.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            CalcADR();
        }

        private void CalcADR()
        {
            var context = Entities.GetContext();
            decimal totalSellsAmount = context.PaymentBill.Sum(pb => pb.amount);
            double totalNightsDouble = 0;
            var bookings = context.Booking.ToList();
            if (bookings.Any())
            {
                totalNightsDouble = bookings
                    .Sum(b => (b.dateTill - b.dateFrom).TotalDays);
            }
            if (totalNightsDouble <= 0)
            {
                MessageBox.Show("Невозможно рассчитать ADR: нет данных о бронированиях или нулевое количество ночей");
                return;
            }
            decimal totalNights = Convert.ToDecimal(totalNightsDouble);
            decimal ADR = totalSellsAmount / totalNights;
            MessageBox.Show($"Показатель ADR на основании данных об общей выручке {totalSellsAmount}" +
                            $" и общем количестве ночей проживания {totalNights} составляет {ADR:F2}");
        }

        private void CalcRoomBooked()
        {
            var context = Entities.GetContext();
            int totalRooms = context.Room.Count();
            double totalNightsDouble = 0;
            var bookings = context.Booking.ToList();
            if (bookings.Any())
            {
                totalNightsDouble = bookings
                    .Sum(b => (b.dateTill - b.dateFrom).TotalDays);
            }
            if (totalNightsDouble <= 0)
            {
                MessageBox.Show("Невозможно рассчитать степень загрузки: нулевое количество ночей");
                return;
            }
            decimal totalNights = Convert.ToDecimal(totalNightsDouble);
            decimal loadPercent = totalNights / totalRooms * 100;
            MessageBox.Show($"Показатель загрузки номерного фонда  на основании данных об общем количестве номеров {totalRooms}" +
                            $" и общем количестве ночей проживания {totalNights} составляет {loadPercent:F2}%");
        }

        private void ShowBookingInfo()
        {
            spBooking.Visibility = Visibility.Visible;
            lvClientDetails.Visibility = Visibility.Collapsed;
            spBookingElements.IsEnabled = true;
        }

        private void ShowClientInfo()
        {
            spBooking.Visibility = Visibility.Collapsed;
            lvClientDetails.Visibility = Visibility.Visible;
            spBookingElements.IsEnabled = false;
        }

        private void ApplyAccessControl()
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                string level = mainWindow.AccessLevel;

                if (level == "admin") 
                {
                    Border analysisBorder = FindParentBorder(btnShowADR);
                    if (analysisBorder != null)
                    {
                        analysisBorder.Visibility = Visibility.Collapsed;
                    }
                    btnDelete.Visibility = Visibility.Collapsed;
                }
                else if (level == "owner") 
                {
                    Border analysisBorder = FindParentBorder(btnShowADR);
                    if (analysisBorder != null)
                    {
                        analysisBorder.Visibility = Visibility.Visible;
                    }
                    btnDelete.Visibility = Visibility.Visible;
                }
                else if (level == "client") 
                {
                    btnnavToClient.Visibility = Visibility.Collapsed;
                    btnShowBooking.Visibility = Visibility.Collapsed;
                    spBookingElements.Visibility = Visibility.Collapsed; 
                                                                         
                    btnDelete.Visibility = Visibility.Collapsed;

                    btnCreate.Visibility = Visibility.Visible; 

                    ShowBookingInfo();
                }
            }
        }
        private Border FindParentBorder(DependencyObject child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is Border border)
                {
                    return border;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }


        private void DgBookings_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow == null || mainWindow.AccessLevel != "client")
            {
                return;
            }

            Booking selectedBooking = dgBookings.SelectedItem as Booking;
            if (selectedBooking != null)
            {
                PaymentBill relatedBill = null;
                if (selectedBooking.PaymentBill != null && selectedBooking.PaymentBill.Any())
                {
                    relatedBill = selectedBooking.PaymentBill.First();
                }
                else
                {
                    var context = Entities.GetContext();
                    relatedBill = context.PaymentBill.FirstOrDefault(pb => pb.bookingId == selectedBooking.id);
                }

                if (relatedBill != null)
                {
                    NavigationService.Navigate(new ClientBookingDetailsPage(selectedBooking, relatedBill));
                }
                else
                {
                    MessageBox.Show("Не удалось найти информацию о счете для этого бронирования.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

    }
}