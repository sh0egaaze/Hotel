using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;

namespace Hotel.Pages
{
    // Вспомогательный класс для строки таблицы детализации
    public class DetailLineItem
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public int Quantity { get; set; }
        public string Unit { get; set; }
        public decimal PricePerUnit { get; set; }
        public decimal TotalLineAmount { get; set; }
    }

    public partial class ClientBookingDetailsPage : Page
    {
        private Booking _currentBooking;
        private PaymentBill _currentBill;

        public ClientBookingDetailsPage(Booking booking, PaymentBill bill)
        {
            InitializeComponent();
            _currentBooking = booking;
            _currentBill = bill;
            LoadDetails();
        }

        private void LoadDetails()
        {
            if (_currentBooking == null || _currentBill == null)
            {
                MessageBox.Show("Ошибка: Данные о бронировании или счете отсутствуют.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
                return;
            }

            var context = Entities.GetContext();

            string clientFullName = "Неизвестный клиент";
            if (_currentBill.Client != null) 
            {
                clientFullName = $"{_currentBill.Client.lastName} {_currentBill.Client.firstName} {_currentBill.Client.middleName}";
            }
            string roomInfo = "Неизвестный номер";
            string categoryInfo = "Неизвестная категория";
            if (_currentBooking.Room != null)
            {
                roomInfo = _currentBooking.Room.number.ToString();
                if (_currentBooking.Room.Category != null)
                {
                    categoryInfo = _currentBooking.Room.Category.name;
                }
            }
            lblBookingInfo.Content = $"Бронирование №{_currentBooking.id} для {clientFullName}";
            lblBookingDetails.Content = $"Номер: {roomInfo} (Категория: {categoryInfo}), с {_currentBooking.dateFrom:dd.MM.yyyy} по {_currentBooking.dateTill:dd.MM.yyyy}";

            var days = (_currentBooking.dateTill - _currentBooking.dateFrom).TotalDays;
            if (days <= 0) days = 1;

            decimal roomPricePerNight = 0;
            if (_currentBooking.Room?.categoryId != null)
            {
                var roomPriceRecord = context.RoomPrice
                                              .FirstOrDefault(rp => rp.categoryId == _currentBooking.Room.categoryId);
                roomPricePerNight = roomPriceRecord?.price ?? 0;
            }

            var detailLines = new List<DetailLineItem>();

            if (roomPricePerNight > 0)
            {
                detailLines.Add(new DetailLineItem
                {
                    Index = 1,
                    Name = categoryInfo,
                    Quantity = (int)days,
                    Unit = "Сутки",
                    PricePerUnit = roomPricePerNight,
                    TotalLineAmount = roomPricePerNight * (decimal)days
                });
            }

            var serviceInBillRecords = context.ServiceInBill
                                              .Include(sib => sib.Service)
                                              .Where(sib => sib.billId == _currentBill.id)
                                              .ToList();

            int serviceIndex = detailLines.Count > 0 ? detailLines.Count + 1 : 1;
            decimal totalServicesAmount = 0;
            foreach (var sib in serviceInBillRecords)
            {
                if (sib.Service != null)
                {
                    decimal servicePrice = sib.Service.price ?? 0;
                    decimal lineTotal = servicePrice * sib.amount;
                    totalServicesAmount += lineTotal;

                    detailLines.Add(new DetailLineItem
                    {
                        Index = serviceIndex++,
                        Name = sib.Service.name,
                        Quantity = sib.amount,
                        Unit = sib.Service.measureUnit ?? "шт",
                        PricePerUnit = servicePrice,
                        TotalLineAmount = lineTotal
                    });
                }
            }

            dgServices.ItemsSource = detailLines;

            decimal calculatedTotal = (roomPricePerNight * (decimal)days) + totalServicesAmount;
            lblTotalAmount.Content = calculatedTotal.ToString("C");
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null && mainWindow.AccessLevel == "client" && mainWindow.AssociatedId.HasValue)
                {
                    NavigationService.Navigate(new BasePage(mainWindow.AssociatedId.Value));
                }
                else
                {
                }
            }
        }
    }
}