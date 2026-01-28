using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Hotel.Pages
{
    public partial class BookingPage : Page
    {
        public BookingPage()
        {
            InitializeComponent();

            var clients = Entities.GetContext().Client.ToList();
            cmbClient.ItemsSource = clients.Select(c => new
            {
                id = c.id,
                fullName = c.lastName + " " + c.firstName + " " + c.middleName
            }).ToList();

            cmbRoom.ItemsSource = Entities.GetContext().Room.ToList();
        }

        private void CalculateAmount()
        {
            if (dpFrom.SelectedDate.HasValue && dpTill.SelectedDate.HasValue && cmbRoom.SelectedItem != null)
            {
                var room = cmbRoom.SelectedItem as Room;

                var price = Entities.GetContext().RoomPrice
                            .FirstOrDefault(p => p.categoryId == room.categoryId);

                if (price != null)
                {
                    var days = (dpTill.SelectedDate.Value - dpFrom.SelectedDate.Value).TotalDays;
                    lblAmount.Content = $"{price.price * (decimal)days}";
                }
                else
                {
                    lblAmount.Content = "Цена не найдена";
                }
            }
        }

        private void AddBooking()
        {
            StringBuilder errors = new StringBuilder();

            if (cmbClient.SelectedItem == null)
            {
                errors.AppendLine("Укажите клиента");
            }
            if (cmbRoom.SelectedItem == null)
            {
                errors.AppendLine("Укажите номер");
            }
            if (dpFrom.SelectedDate == null)
            {
                errors.AppendLine("Укажите дату въезда");
            }
            if (dpTill.SelectedDate == null)
            {
                errors.AppendLine("Укажите дату выселения");
            }

            if (errors.Length > 0)
            {
                MessageBox.Show(errors.ToString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                dynamic client = cmbClient.SelectedItem;

                var room = cmbRoom.SelectedItem as Room;

                var price = Entities.GetContext().RoomPrice
                            .FirstOrDefault(p => p.categoryId == room.categoryId);

                Booking booking = new Booking();

                booking.clientId = client.id;
                booking.Room = room;
                booking.dateFrom = dpFrom.SelectedDate.Value;
                booking.dateTill = dpTill.SelectedDate.Value;

                var days = (dpTill.SelectedDate.Value - dpFrom.SelectedDate.Value).TotalDays;

                if (price != null)
                {
                    booking.amount = price.price * (decimal)days;
                }
                else
                {
                    booking.amount = 0;
                }

                try
                {
                    Entities.GetContext().Booking.Add(booking);
                    Entities.GetContext().SaveChanges();
                    NavigationService.Navigate(new BasePage());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            AddBooking();
        }

        private void dpFrom_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateAmount();
        }

        private void dpTill_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateAmount();
        }

        private void cmbRoom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateAmount();
        }
    }
}