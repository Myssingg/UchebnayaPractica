using System;
using System.Collections.Generic;
using System.Data.Entity;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace _3_WPF
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Entities db = new Entities();

        double timerTime = TimeSpan.FromMinutes(30).TotalSeconds; //30 минут для таймера

        DispatcherTimer timer = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();

            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            timerTime--;

            timerText.Text = timerTime.ToString(@"hh\:mm\:ss");

            if (timerTime == 0) //Время истекло
            {
                await Task.Run(() => MessageBox.Show($"Можно снова входить"));

                timer.Stop();
                timerTime = TimeSpan.FromMinutes(30).TotalSeconds; //30 минут для таймера

                btn_Login.IsEnabled = true;
            } 
        }

        private async void btn_Login_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(textBox_Login.Text) 
                || (String.IsNullOrEmpty(textBox_Password.Text) 
                && String.IsNullOrEmpty(passwordBox_Password.Password)))
            {
                MessageBox.Show("Заполните все данные");
                return;
            }

            users user = db.users.FirstOrDefault(x => x.login == textBox_Login.Text);
            
            if (user == null)
            {
                MessageBox.Show("Пользователь с таким логином не найден");
                return;
            }

            if (Captcha.Visibility == Visibility.Visible)
            {
                if (CaptchaText.Text != CaptchaBlock.Text)
                {
                    MessageBox.Show("Капча введена неправильно!");
                    CaptchaBlock.Text = createCaptcha(); // Обновляем текст в блоке
                    CaptchaText.Clear(); // Очищаем ввод пользователя
                    
                    btn_Login.IsEnabled = false;

                    await Task.Delay(5000);

                    btn_Login.IsEnabled = true;

                    return;
                }
            }

            string password = "";
            if (passwordBox_Password.Visibility == Visibility.Visible)
                password = passwordBox_Password.Password;
            else 
                password = textBox_Password.Text;


            if (user.password != password)
            {
                MessageBox.Show("Неверный пароль пользователя");

                IncorrectPassword.Visibility = Visibility.Visible;
                Captcha.Visibility = Visibility.Visible;
                CaptchaBlock.Text = createCaptcha();

                db.enterHistory.Add(new enterHistory
                {
                    userLogin = user.login,
                    userType = user.type,
                    dateTimeSeen = DateTime.Now,
                    success = false
                });
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка сохранения в историю входа");
                    Debug.WriteLine(ex);
                }

                return;
            }


            //Лаборант
            if (user.type == 1)
            {
                WindowForLaborant window = new WindowForLaborant(db, user, this);
                window.Show();
                this.Hide();

                await Task.Run(() => MessageBox.Show($"Вход под ролью \nЛаборант \nДобро пожаловать в систему", "Успешный вход"));
            }
            //Лаборант-исследователь
            if (user.type == 2)
            {
                WindowForLaborantExplorer window = new WindowForLaborantExplorer(db, user, this);
                this.Hide();
                window.Show();

                await Task.Run(() => MessageBox.Show($"Вход под ролью \nЛаборант-исследователь \nДобро пожаловать в систему", "Успешный вход"));
            }
            //Бухгалтер
            if (user.type == 3)
            {
                WindowForAccountant window = new WindowForAccountant(db, user, this);
                this.Hide();
                window.Show();

                await Task.Run(() => MessageBox.Show($"Вход под ролью \nБухгалтер \nДобро пожаловать в систему", "Успешный вход"));
            }
            //Администратор
            if (user.type == 4)
            {
                WindowForAdministrator window = new WindowForAdministrator(db, user, this);
                this.Hide();
                window.Show();

                await Task.Run(() => MessageBox.Show($"Вход под ролью \nАдминистратор \nДобро пожаловать в систему", "Успешный вход"));
            }

            clearFields();

            db.enterHistory.Add(new enterHistory
            {
                userLogin = user.login,
                userType = user.type,
                dateTimeSeen = DateTime.Now,
                success = true
            });
            try
            {
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения в историю входа");
                Debug.WriteLine(ex);
            }
        }


        private string createCaptcha()
        {
            string allowchar = "";
            allowchar = "A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z";
            allowchar += "a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z";
            allowchar += "1,2,3,4,5,6,7,8,9,0";

            string[] captchaSymbols = allowchar.Split(',');

            string captcha = "";
            Random rnd = new Random();

            int countSymbols = 4;

            for (int i = 0; i < countSymbols; i++)
            {
                captcha += captchaSymbols[rnd.Next(0, captchaSymbols.Length)];
            }

            return captcha;
        }

        private void clearFields()
        {
            textBox_Login.Text = String.Empty;
            textBox_Password.Text = String.Empty;
            passwordBox_Password.Password = String.Empty;
        }

        private void toggle_Checked(object sender, RoutedEventArgs e)
        {
            textBox_Password.Text = passwordBox_Password.Password;
            textBox_Password.Visibility = Visibility.Visible;
            passwordBox_Password.Visibility = Visibility.Collapsed;
        }

        private void toggle_Unchecked(object sender, RoutedEventArgs e)
        {
            passwordBox_Password.Visibility = Visibility.Visible;
            textBox_Password.Visibility = Visibility.Collapsed;
            passwordBox_Password.Password = textBox_Password.Text;
        }

        public void start30mintimer()
        {
            timer.Start();
            timerText.Visibility = Visibility.Visible;

            btn_Login.IsEnabled = false;
        }

        private void generateCaptcha_Click(object sender, RoutedEventArgs e)
        {
            CaptchaBlock.Text = createCaptcha();
        }
    }
}
