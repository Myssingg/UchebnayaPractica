using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace _3_WPF
{
    /// <summary>
    /// Логика взаимодействия для DialogAddServices.xaml
    /// </summary>
    public partial class DialogAddServices : Window
    {
        Entities db;
        orders orders;

        ObservableCollection<servicesInOrder> selectedServices = new ObservableCollection<servicesInOrder>();

        public DialogAddServices(Entities db, ref orders order)
        {
            InitializeComponent();

            this.db = db;
            this.orders = order;

            comboBox_services.ItemsSource = db.services.ToList();
            list.ItemsSource = selectedServices;
        }

        private void comboBox_services_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_services.SelectedItem != null)
            {
                textBox_Price.Text = (comboBox_services.SelectedItem as services).price.ToString();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_services.SelectedIndex == -1
                || String.IsNullOrEmpty(textBox_Count.Text))
            {
                MessageBox.Show("Заполните поля");
                return;
            }

            if (!int.TryParse(textBox_Count.Text, out int count))
            {
                MessageBox.Show("Ошибка конвертирования в число");
                return;
            }

            if (int.Parse(textBox_Count.Text) <= 0 || int.Parse(textBox_Count.Text) >= 1000)
            {
                Task.Run(() => MessageBox.Show("Введено некорректное количество"));
                return;
            }

            try
            {
                selectedServices.Add(new servicesInOrder
                {
                    service = (comboBox_services.SelectedItem as services).code,
                    count = int.Parse(textBox_Count.Text),
                    serviceStatusCode = 1,
                    services = (comboBox_services.SelectedItem as services)
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка добавления в лист. Подробнее в Debug.Log");
                Debug.WriteLine(ex);
                return;
            }


            try
            {
                textBox_sumPrice.Text = (double.Parse(textBox_sumPrice.Text) + (comboBox_services.SelectedItem as services).price * int.Parse(textBox_Count.Text)).ToString();
            }
            catch (Exception ex) {
                list.Background = Brushes.Orange;
                Thread.Sleep(1000);
                list.Background = null;
                Debug.WriteLine(ex);
                return; 
            }

            comboBox_services.SelectedIndex = -1;
            textBox_Price.Clear();
            textBox_Count.Clear();

            list.UpdateLayout();
        }

        private void btn_Next_Click(object sender, RoutedEventArgs e)
        {
            if (selectedServices.Count == 0)
            {
                MessageBox.Show("Выберите хотя-бы одну услугу");
                return;
            }

            orders.timeToComplete = selectedServices.Count; //Время выполнения заказа = количеству выбранных услуг (1 услуга = 1 день)

            MessageBoxResult mboxRes = MessageBox.Show($"Вы уверены что выбрали нужные услуги? \nСтоимость заказа = {textBox_sumPrice.Text}", "Подтверждение", MessageBoxButton.YesNo);

            if (mboxRes == MessageBoxResult.Yes)
                DialogResult = true;
            else { return; }
            Close();
        }

        public List<servicesInOrder> getList
        {
            get { return selectedServices.ToList(); }
        }
    }
}
