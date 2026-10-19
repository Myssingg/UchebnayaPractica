using Mail_LIB;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Логика взаимодействия для WindowForAdministrator.xaml
    /// </summary>
    public partial class WindowForAdministrator : Window
    {
        Entities db;
        users user;
        MainWindow main;

        double timerTime = TimeSpan.FromHours(2.5).TotalSeconds; //2 часа 30 секунд для таймера

        DispatcherTimer timer = new DispatcherTimer();

        List<users> users;
        List<enterHistory> enterHistory;
        public WindowForAdministrator(Entities db, users user, MainWindow maim)
        {
            InitializeComponent();
            this.db = db;
            this.user = user;
            this.main = maim;

            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;

            timer.Start();

            loadData();
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

        private async void filterLoginUsers_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = filterLoginUsers.Text;

            try
            {
                await Task.Run(() =>
                {
                    var temp = users.Where(u => u.login.Contains(searchText)).ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                        listUsers.ItemsSource = temp
                    );
                });
            }
            catch
            {

            }
        }

        private async void loadData()
        {
            try
            {
                var admin = db.administrators.FirstOrDefault(x => x.users.login == user.login);
                if (admin == null)
                {
                    var tempAdmin = new administrators
                    {
                        userLogin = user.login
                    };
                    db.administrators.Add(tempAdmin);
                    try
                    {
                        user.lastenter = DateTime.Now;
                        db.SaveChanges();
                    }
                    catch (Exception ex) { Debug.WriteLine(ex); }
                    admin = tempAdmin;
                }


                userInfo.Text = $"{admin.users.login} - Администратор";


                avatar.Source = new BitmapImage(new Uri("pack://application:,,,/images/administrator.png"));

                users = await db.users.AsNoTracking().ToListAsync();

                listUsers.ItemsSource = users;

                enterHistory = await db.enterHistory.AsNoTracking().ToListAsync();

                listHistory.ItemsSource = enterHistory;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private async void filterLoginHistory_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = filterLoginHistory.Text;

            try
            {
                await Task.Run(() =>
                {
                    var temp = users.Where(u => u.login.Contains(searchText)).ToList();

                    Application.Current.Dispatcher.Invoke(() =>
                        listHistory.ItemsSource = temp
                    );
                });
            }
            catch
            {

            }
        }

        //Кнопка
        private void btn_addStaff_Click(object sender, RoutedEventArgs e)
        {
            //Проверка на верную заполненность всех полей
            if (comboBox_role.SelectedIndex == -1
                || String.IsNullOrEmpty(textBox_Surname.Text)
                || String.IsNullOrEmpty(textBox_Name.Text)
                || String.IsNullOrEmpty(textBox_Patronymic.Text)
                || String.IsNullOrEmpty(textBox_phoneNumber.Text)
                || String.IsNullOrEmpty(textBox_Email.Text)
                || String.IsNullOrEmpty(textBox_Login.Text)
                || String.IsNullOrEmpty(textBox_Password.Text))
            {
                MessageBox.Show("Заполните все поля");
                return;
            }

            // Твой способ форматирования ФИО (с исправлением для компиляции)
            string surname = textBox_Surname.Text.Trim().Length > 0 ? char.ToUpper(textBox_Surname.Text.Trim()[0]) + textBox_Surname.Text.Trim().Substring(1).ToLower() : "";
            string name = textBox_Name.Text.Trim().Length > 0 ? char.ToUpper(textBox_Name.Text.Trim()[0]) + textBox_Name.Text.Trim().Substring(1).ToLower() : "";
            string patronymic = textBox_Patronymic.Text.Trim().Length > 0 ? char.ToUpper(textBox_Patronymic.Text.Trim()[0]) + textBox_Patronymic.Text.Trim().Substring(1).ToLower() : "";

            Regex phone = new Regex(@"\D");
            string phoneNumber = phone.Replace(textBox_phoneNumber.Text, "");

            if (phoneNumber.Length != 11)
            {
                MessageBox.Show("Номер телефона не соответствует стандарту");
                return;
            }

            phoneNumber = "7" + phoneNumber.Substring(1);

            if (!Mail_Class.check_mail(textBox_Email.Text.Trim()))
            {
                MessageBox.Show("Email не соответствует стандарту");
                return;
            }

            if (!Mail_Class.check_login(textBox_Login.Text))
            {
                MessageBox.Show("Логин не соответствует стандарту \n(8 символов, большие, маленькие латинские буквы и цифры)");
                return;
            }

            if (!Mail_Class.check_password(textBox_Password.Text))
            {
                MessageBox.Show("Пароль не соответствует стандартам безопасности!");
                return;
            }

            //Проверка на заполненность панели для лаборанта
            if (comboBox_role.SelectedIndex == 0)
            {
                if (listServices.Items.Count == 0)
                {
                    MessageBox.Show("Выберите услуги, которые может оказывать лаборант");
                    return;
                }
            }

            if (db.users.FirstOrDefault(x => x.login == textBox_Login.Text) != null)
            {
                MessageBox.Show("Сотрудник с таким логином уже занят");
                return;
            }

            // Используем транзакцию, чтобы база очищалась при ошибках
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    users user = db.users.Add(new users
                    {
                        login = textBox_Login.Text,
                        password = textBox_Password.Text,
                        type = comboBox_role.SelectedIndex + 1,
                        lastenter = DateTime.Now
                    });
                    db.SaveChanges(); // Сохраняем юзера

                    //лаборант || лаборант-исследователь
                    if (comboBox_role.SelectedIndex == 0 || comboBox_role.SelectedIndex == 1)
                    {
                        if (db.laborants.FirstOrDefault(x => x.surname == surname && x.name == name && x.patronymic == patronymic) != null)
                        {
                            throw new Exception("Такой лаборант уже есть!");
                        }

                        laborants laborant = new laborants
                        {
                            surname = surname,
                            name = name,
                            patronymic = patronymic,
                            userLogin = user.login
                        };
                        db.laborants.Add(laborant);
                        db.SaveChanges(); // Сохраняем лаборанта

                        // ТВОЙ ИСПРАВЛЕННЫЙ ЦИКЛ УСЛУГ
                        foreach (services service in listServices.Items)
                        {
                            // Ищем в справочнике по serviceType
                            var labService = db.laboratoryServices.FirstOrDefault(s => s.serviceType == service.code);

                            if (labService == null)
                            {
                                labService = new laboratoryServices { serviceType = service.code };
                                db.laboratoryServices.Add(labService);
                                db.SaveChanges();
                            }

                            // Привязываем связь
                            db.laborantServices.Add(new laborantServices
                            {
                                laborantCode = laborant.code,
                                serviceCode = labService.code // ID из справочника
                            });
                        }
                        db.SaveChanges();
                    }
                    //бухгалтер
                    else if (comboBox_role.SelectedIndex == 2)
                    {
                        if (db.accountants.FirstOrDefault(x => x.surname == surname && x.name == name && x.patronymic == patronymic) != null)
                        {
                            throw new Exception("Такой бухгалтер уже есть");
                        }

                        db.accountants.Add(new accountants
                        {
                            surname = surname,
                            name = name,
                            patronymic = patronymic,
                            lastenter = DateTime.Now,
                            userLogin = user.login
                        });
                        db.SaveChanges();
                    }
                    //администратор
                    else
                    {
                        throw new Exception("Администратора невозможно добавить!");
                    }

                    transaction.Commit();
                    MessageBox.Show("Успешное добавление сотрудника");

                    // Твоя очистка полей
                    foreach (var tb in panel.Children.OfType<TextBox>())
                    {
                        tb.Clear();
                    }
                    comboBox_role.SelectedIndex = -1;
                    listServices.Items.Clear();
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); // Откат: база очистится сама
                    var inner = ex.InnerException;
                    while (inner?.InnerException != null) inner = inner.InnerException;
                    MessageBox.Show("Ошибка сохранения! Изменения отменены.\nПричина: " + (inner?.Message ?? ex.Message));

                    foreach (var entry in db.ChangeTracker.Entries())
                    {
                        entry.State = System.Data.Entity.EntityState.Detached;
                    }
                }
            }
        }





        //Вкладка
        private async void tab_addStaff_Loaded(object sender, RoutedEventArgs e)
        {
            comboBox_role.ItemsSource = await db.types.ToListAsync();
            comboBox_Service.ItemsSource = await db.services.ToListAsync();
        }

        private void comboBox_role_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox_role.SelectedIndex == 0)
                panelForLaborant.Visibility = Visibility.Visible;
            else
                panelForLaborant.Visibility = Visibility.Collapsed;
        }

        private void btn_addService_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_Service.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите услугу");
                return;
            }

            if (listServices.Items.Contains(comboBox_Service.SelectedItem as services))
            {
                Task.Run(() => { MessageBox.Show("Такая услуга уже выбрана"); });
                comboBox_Service.SelectedIndex = -1;
            }

            listServices.Items.Add(comboBox_Service.SelectedItem as services);
        }
    }
}
