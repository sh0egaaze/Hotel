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
        public BasePage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            lvClientDetails.Visibility = Visibility.Collapsed;
            var context = Entities.GetContext();

            var paymentBills = context.PaymentBill
                .Include(b => b.Client)
                .Include(b => b.Booking)
                .Include(b => b.Booking.Room)
                .Include(b => b.Booking.Room.Category)
                .ToList();

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

                var services = (from sib in context.ServiceInBill
                                join s in context.Service on sib.seviceId equals s.id
                                where sib.billId == bill.id
                                select s.name).ToList();

                string servicesStr = services.Any() ? string.Join(", ", services) : "-";


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

            List<Booking> bookings = Entities.GetContext().Booking.ToList();

            if (dpStart.SelectedDate != null)
            {
                DateTime dateStart = dpStart.SelectedDate.Value;
                bookings = bookings.Where(b => b.dateFrom >= dateStart).ToList();
            }
            if (dpEnd.SelectedDate != null)
            {
                DateTime dateEnd = dpEnd.SelectedDate.Value;
                bookings = bookings.Where(b => b.dateFrom <= dateEnd).ToList();
            }

            dgBookings.ItemsSource = bookings;
            dgBookings.SelectedItem = null;
        }

        private void DeleteBooking()
        {
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
            NavigationService.Navigate(new BookingPage());
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteBooking();
        }

        private void btnnavToClient_Click(object sender, RoutedEventArgs e)
        {
            ShowClientInfo();
        }

        private void btnShowBooking_Click(object sender, RoutedEventArgs e)
        {
            ShowBookingInfo();
        }

        

        private void btnRoomsBooked_Click(object sender, RoutedEventArgs e)
        {
            CalcRoomBooked();
        }

        private void btnShowADR_Click(object sender, RoutedEventArgs e)
        {
            CalcADR();
        }
    }
}
