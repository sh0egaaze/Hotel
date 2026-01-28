using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Data.Entity;
using System.ComponentModel;
using System.Windows.Data;

namespace Hotel.Pages
{
    public class SelectedServiceItem : INotifyPropertyChanged
    {
        public Service Service { get; set; }
        private int _quantity = 1;
        public int Quantity
        {
            get { return _quantity; }
            set
            {
                if (value >= 1)
                {
                    _quantity = value;
                    OnPropertyChanged(nameof(Quantity));
                    OnPropertyChanged(nameof(DisplayText)); 
                }
            }
        }

        public string DisplayText => $"{Service.name} (Цена: {(Service.price ?? 0):F2} руб./{Service.measureUnit ?? "шт"})";

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class ClientBookingPage : Page
    {
        private int _clientId;
        private Dictionary<int, decimal> _servicePrices = new Dictionary<int, decimal>(); 
        private decimal _baseRoomPrice = 0; 

        public ClientBookingPage(int clientId)
        {
            _clientId = clientId;
            InitializeComponent();
            LoadAvailableRooms();
            LoadServices();
        }

        private void LoadAvailableRooms()
        {
            var context = Entities.GetContext();
            var allRooms = context.Room
                                   .Include(r => r.Category) 
                                   .ToList(); 

            var availableRooms = new List<object>();

            if (dpFrom.SelectedDate.HasValue && dpTill.SelectedDate.HasValue)
            {
                DateTime startDate = dpFrom.SelectedDate.Value.Date;
                DateTime endDate = dpTill.SelectedDate.Value.Date;

                if (endDate <= startDate)
                {
                    MessageBox.Show("Дата выселения должна быть позже даты заезда.");
                    return;
                }

                var bookedRoomIds = context.Booking
                                           .Where(b =>
                                               !(endDate <= b.dateFrom || startDate >= b.dateTill)
                                           )
                                           .Select(b => b.roomId)
                                           .Distinct()
                                           .ToList();

                foreach (var room in allRooms)
                {
                    if (!bookedRoomIds.Contains(room.id))
                    {
                        var categoryPrice = context.RoomPrice
                                                    .FirstOrDefault(rp => rp.categoryId == room.categoryId); 
                        decimal price = categoryPrice?.price ?? 0;

                        availableRooms.Add(new
                        {
                            id = room.id,
                            displayInfo = $"{room.number} ({room.Category?.name ?? "Без категории"}) - {price} руб./сутки"
                        });
                    }
                }
            }
            else
            {
                foreach (var room in allRooms)
                {
                    var categoryPrice = context.RoomPrice
                                                .FirstOrDefault(rp => rp.categoryId == room.categoryId);
                    decimal price = categoryPrice?.price ?? 0;
                    availableRooms.Add(new
                    {
                        id = room.id,
                        displayInfo = $"{room.number} ({room.Category?.name ?? "Без категории"}) - {price} руб./сутки"
                    });
                }
            }

            cmbRoom.ItemsSource = availableRooms;
        }

        private void LoadServices()
        {
            var context = Entities.GetContext();
            var services = context.Service.ToList();
            _servicePrices.Clear();
            foreach (var service in services)
            {
                _servicePrices[service.id] = service.price ?? 0; 
            }

            var initialServiceList = services.Select(s => new SelectedServiceItem { Service = s, Quantity = 1 }).ToList();

            lvServices.ItemsSource = initialServiceList;
            var collectionView = CollectionViewSource.GetDefaultView(lvServices.ItemsSource) as ICollectionView;
            if (collectionView != null)
            {
                foreach (INotifyPropertyChanged item in collectionView.SourceCollection)
                {
                    item.PropertyChanged += SelectedService_PropertyChanged;
                }
            }
        }

        private void SelectedService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SelectedServiceItem.Quantity))
            {
                CalculateAmount(); 
            }
        }

        private void CalculateAmount()
        {
            if (dpFrom.SelectedDate.HasValue && dpTill.SelectedDate.HasValue && cmbRoom.SelectedItem != null)
            {
                var selectedRoomData = cmbRoom.SelectedItem as dynamic;
                int roomId = (int)selectedRoomData.id;
                var context = Entities.GetContext();

                var room = context.Room
                                  .FirstOrDefault(r => r.id == roomId);

                if (room == null)
                {
                    lblAmount.Content = "0";
                    return;
                }

                var categoryPrice = context.RoomPrice
                                            .FirstOrDefault(rp => rp.categoryId == room.categoryId);
                decimal roomPricePerDay = categoryPrice?.price ?? 0;
                _baseRoomPrice = roomPricePerDay;

                var days = (dpTill.SelectedDate.Value.Date - dpFrom.SelectedDate.Value.Date).TotalDays;
                if (days <= 0) { lblAmount.Content = "0"; return; }

                decimal totalAmount = roomPricePerDay * (decimal)days;

                var selectedServiceItems = lvServices.ItemsSource as IEnumerable<SelectedServiceItem>;
                if (selectedServiceItems != null)
                {
                    foreach (var item in selectedServiceItems)
                    {
                        if (_servicePrices.ContainsKey(item.Service.id))
                        {
                            totalAmount += _servicePrices[item.Service.id] * item.Quantity;
                        }
                    }
                }

                lblAmount.Content = totalAmount.ToString("F2");
            }
            else
            {
                lblAmount.Content = "0";
            }
        }


        private void dpFrom_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAvailableRooms(); 
            CalculateAmount();
        }

        private void dpTill_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAvailableRooms(); 
            CalculateAmount();
        }

        private void cmbRoom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CalculateAmount(); 
        }

        private void btnBook_Click(object sender, RoutedEventArgs e)
        {
            if (!dpFrom.SelectedDate.HasValue || !dpTill.SelectedDate.HasValue)
            {
                MessageBox.Show("Пожалуйста, выберите даты заезда и выселения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (cmbRoom.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите номер.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedRoomData = cmbRoom.SelectedItem as dynamic;
            int roomId = (int)selectedRoomData.id;
            DateTime dateFrom = dpFrom.SelectedDate.Value.Date;
            DateTime dateTill = dpTill.SelectedDate.Value.Date;

            if (dateTill <= dateFrom)
            {
                MessageBox.Show("Дата выселения должна быть позже даты заезда.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var context = Entities.GetContext();

            var existingBooking = context.Booking
                                         .Any(b => b.roomId == roomId &&
                                                   !(dateTill <= b.dateFrom || dateFrom >= b.dateTill));

            if (existingBooking)
            {
                MessageBox.Show("Выбранный номер уже забронирован на указанные даты.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var room = context.Room
                              .FirstOrDefault(r => r.id == roomId);
            if (room == null)
            {
                MessageBox.Show("Ошибка: выбранный номер не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var categoryPrice = context.RoomPrice
                                        .FirstOrDefault(rp => rp.categoryId == room.categoryId);
            decimal roomPricePerDay = categoryPrice?.price ?? 0;

            var days = (dateTill - dateFrom).TotalDays;
            decimal totalRoomAmount = roomPricePerDay * (decimal)days;

            Booking newBooking = new Booking
            {
                clientId = _clientId,
                roomId = roomId,
                dateFrom = dateFrom,
                dateTill = dateTill,
                amount = totalRoomAmount 
            };

            try
            {
                context.Booking.Add(newBooking);
                PaymentBill bill = new PaymentBill
                {
                    clientId = _clientId,
                    creationDate = DateTime.Now,
                    amount = totalRoomAmount, 
                    respEmployeeId = null,
                    bookingId = newBooking.id
                };
                context.PaymentBill.Add(bill);

                var selectedServiceItems = lvServices.ItemsSource as IEnumerable<SelectedServiceItem>;
                if (selectedServiceItems != null)
                {
                    foreach (var item in selectedServiceItems)
                    {
                        if (item.Quantity > 0)
                        {
                            ServiceInBill serviceInBill = new ServiceInBill
                            {
                                billId = bill.id,
                                seviceId = item.Service.id,
                                amount = item.Quantity 
                            };
                            context.ServiceInBill.Add(serviceInBill);
                            bill.amount += (_servicePrices[item.Service.id] * item.Quantity);
                        }
                    }
                }

                context.SaveChanges(); 
                MessageBox.Show("Бронирование успешно оформлено!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
                else
                {
                    NavigationService.Navigate(new BasePage(_clientId));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении бронирования: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
            else
            {
                var mainWindow = Application.Current.MainWindow as MainWindow;
                if (mainWindow != null && mainWindow.AccessLevel == "client")
                {
                    NavigationService.Navigate(new BasePage(_clientId));
                }
                else
                {
                    NavigationService.Navigate(new BasePage());
                }
            }
        }
    }
}