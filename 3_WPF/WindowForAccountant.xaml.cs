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
using System.Windows.Shapes;
using System.Windows.Threading;

namespace _3_WPF
{
    /// <summary>
    /// Логика взаимодействия для WindowForAccountant.xaml
    /// </summary>
    public partial class WindowForAccountant : Window
    {
        Entities db;
        users user;
        MainWindow main;

        double timerTime = TimeSpan.FromHours(2.5).TotalSeconds; //2 часа 30 секунд для таймера

        DispatcherTimer timer = new DispatcherTimer();

        public WindowForAccountant(Entities db, users user, MainWindow main)
        {
            InitializeComponent();
            this.db = db;
            this.user = user;
            this.main = main;

            var accountant = db.accountants.FirstOrDefault(x => x.userLogin == user.login);

            if (accountant != null)
            {
                userInfo.Text = $"{accountant.surname} {accountant.name} {accountant.patronymic} - Бухгалтер";
            }
            else
                userInfo.Text = "Безымянный бухгалтер - Бухгалтер";

            LoadData();

            avatar.Source = new BitmapImage(new Uri("pack://application:,,,/images/accountant.jpeg"));
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            timer.Start();
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            timerTime--;

            Timer.Text = TimeSpan.FromSeconds(timerTime).ToString(@"hh\:mm\:ss");

            if (timerTime == 0) //Время истекло
            {
                this.Close();
                main.Show();

                main.start30mintimer();

                timerTime = TimeSpan.FromHours(2.5).TotalSeconds; //2 часа 30 секунд для таймера
            }
            else
            {
                if (timerTime == TimeSpan.FromMinutes(15).TotalSeconds)
                {
                    await Task.Run(() => MessageBox.Show($"Осталось 15 минут до завершения сеанса!"));
                }
            }
        }

        private void TabItem_Click(object sender, RoutedEventArgs e)
        {
            main.Show();
            this.Close();
        }

        private void LoadData()
        {
            // Загружаем заказы вместе с данными пациентов и компаний
            dgOrders.ItemsSource = db.orders.Include("patients.insuranceCompany").ToList();

            // Загружаем счета
            dgBills.ItemsSource = db.accountantBills.Include("insuranceCompany").ToList();
        }

        private void btnCreateBill_Click(object sender, RoutedEventArgs e)
        {
            var selectedOrder = dgOrders.SelectedItem as orders;
            if (selectedOrder == null)
            {
                MessageBox.Show("Выберите заказ");
                return;
            }

            if (selectedOrder.patients.incuranceCompanyCode == null)
            {
                MessageBox.Show("У пациента не указана страховая компания");
                return;
            }

            try
            {
                int companyId = (int)selectedOrder.patients.incuranceCompanyCode;


                double totalSum = (double)db.servicesInOrder
                    .Where(s => s.order == selectedOrder.code)
                    .ToList() 
                    .Sum(s => s.services.price * s.count);


                var currentAccountant = db.accountants.FirstOrDefault(a => a.userLogin == user.login);

                if (currentAccountant == null)
                {
                    MessageBox.Show("Ошибка: Бухгалтер не определен");
                    return;
                }

                // 4. Запись в базу
                accountantBills newBill = new accountantBills
                {
                    insuranceCompanyCode = companyId,
                    accountantCode = currentAccountant.code,
                    summ = totalSum
                };

                db.accountantBills.Add(newBill);
                db.SaveChanges();

                MessageBox.Show($"Счет успешно создан на сумму: {totalSum:N2} руб.");

                LoadData(); // Обновляем таблицы
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

    }
}
