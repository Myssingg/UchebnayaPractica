using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
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
    /// Логика взаимодействия для WindowForLaborantExplorer.xaml
    /// </summary>
    public partial class WindowForLaborantExplorer : Window
    {
        Entities db;
        users user;
        MainWindow main;


        double timerTime = TimeSpan.FromHours(2.5).TotalSeconds; //2 часа 30 секунд для таймера

        DispatcherTimer timer = new DispatcherTimer();
        public WindowForLaborantExplorer(Entities db, users user, MainWindow main)
        {
            InitializeComponent();
            this.db = db;
            this.user = user;
            this.main = main;

            var laborant = db.laborants.FirstOrDefault(x => x.users.login == user.login);
            if (laborant != null)
            {
                userInfo.Text = $"{laborant.surname} {laborant.name} {laborant.patronymic} - Лаборант-исследователь";
            }
            else
            {
                userInfo.Text = $"Безымянный лаборант-исследователь - Лаборант-исследователь";
            }
            avatar.Source = new BitmapImage(new Uri("pack://application:,,,/images/laborant_explorer.png"));
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            timer.Start();

            comboBox_Analyzers.ItemsSource = db.analyzers.ToList();
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
        private async void btnSendToAnalyzer_Click(object sender, RoutedEventArgs e)
        {
            var selectedService = dgServices.SelectedItem as servicesInOrder;
            if (selectedService == null)
            {
                MessageBox.Show("Сначала выберите услугу в списке!");
                return;
            }

            if (comboBox_Analyzers.SelectedItem == null)
            {
                MessageBox.Show("Необходимо выбрать анализатор!");
                return;
            }

            string analyzerName = comboBox_Analyzers.Text;

            btnSendToAnalyzer.IsEnabled = false;
            mainProgressBar.Visibility = Visibility.Visible;

            bool isDone = false;
            while (!isDone)
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create($"http://localhost:5000/api/analyzer/{analyzerName}");
                    request.Method = "GET";

                    using (var response = (HttpWebResponse)await request.GetResponseAsync())
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        string json = reader.ReadToEnd();
                        var serializer = new JavaScriptSerializer();
                        var data = serializer.Deserialize<GetAnalizator>(json);

                        if (data.progress > 0 && data.progress < 100)
                        {
                            mainProgressBar.Value = data.progress;
                            txt_Info.Text = $"Выполнение: {data.progress}%";
                        }
                        else if (data.services != null && data.services.Count > 0)
                        {
                            mainProgressBar.Value = 100;
                            isDone = true;

                            string resultValue = data.services[0].serviceStatuses.description;

                            MessageBox.Show($"Анализ завершен! Результат: {resultValue}");

                            btnApprove.IsEnabled = true;
                            txt_Info.Text = "Ожидание одобрения результата";

                            if (double.TryParse(resultValue.Replace('.', ','), out double actualResult))
                            {
                                double averageValue = 1.0;

                                double allowedDeviation = (double)selectedService.laboratoryServices.deviation;

                                if (Math.Abs(actualResult - averageValue) > (allowedDeviation * 3))
                                {
                                    MessageBox.Show("Внимание! Результат отклоняется от нормы более чем в 3 раза.",
                                                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    txt_Info.Text = "Связь с анализатором устанавливается...";
                    System.Diagnostics.Debug.WriteLine(ex.Message);
                }

                if (!isDone) await Task.Delay(2000);
            }
        }


        private void btnApprove_Click(object sender, RoutedEventArgs e)
        {
            // 1. Получаем выбранную в таблице услугу
            var selectedItem = dgServices.SelectedItem as servicesInOrder;
            if (selectedItem == null) return;

            // 2. Получаем выбранный в ComboBox анализатор
            var selectedAnalyzer = comboBox_Analyzers.SelectedItem as analyzers;
            if (selectedAnalyzer == null)
            {
                MessageBox.Show("Ошибка: анализатор не определен.");
                return;
            }

            try
            {
                // 3. Обновляем статус услуги в заказе на "Выполнена" (ID = 1 согласно скрипту)
                var serviceInDb = db.servicesInOrder.FirstOrDefault(x => x.code == selectedItem.code);
                if (serviceInDb != null)
                {
                    serviceInDb.serviceStatusCode = 1;
                }

                // 4. Добавляем запись в таблицу истории оказанных услуг
                db.providedServises.Add(new providedServises
                {
                    serviceCode = selectedItem.service, // ID самой услуги
                    datetime = DateTime.Now,
                    laborantCode = db.laborants.FirstOrDefault(x => x.userLogin == user.login).code, // ID лаборанта (сохрани его при входе)
                    analyzerCode = selectedAnalyzer.code    // ID анализатора из БД
                });

                // 5. Сохраняем изменения
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка сохранения");
                    Debug.WriteLine(ex);
                    return;
                }

                MessageBox.Show("Результат успешно одобрен и сохранен в системе!");

                // Сбрасываем интерфейс для следующей услуги
                btnApprove.IsEnabled = false;
                btnSendToAnalyzer.IsEnabled = true;
                mainProgressBar.Visibility = Visibility.Hidden;
                txt_Info.Text = "Готов к следующему анализу";

                // Обновляем таблицу, чтобы статус изменился визуально
                dgServices.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message);
            }
        }
    }
}
