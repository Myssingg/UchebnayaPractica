using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace _3_WPF
{
    /// <summary>
    /// Логика взаимодействия для WindowForLaborant.xaml
    /// </summary>
    public partial class WindowForLaborant : Window
    {
        Entities db;
        users user;
        MainWindow main;

        double timerTime = TimeSpan.FromHours(2.5).TotalSeconds; //2 часа 30 секунд для таймера

        DispatcherTimer timer = new DispatcherTimer();
        public WindowForLaborant(Entities db, users user, MainWindow main)
        {
            InitializeComponent();
            this.db = db;
            this.user = user;
            this.main = main;

            var laborant = db.laborants.FirstOrDefault(x => x.users.login == user.login);

            if (laborant != null)
            {
                userInfo.Text = $"{laborant.surname} {laborant.name} {laborant.patronymic} - Лаборант";
            }
            else
                userInfo.Text = "Безымянный лаборант - Лаборант";
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

        private async void loadData()
        {
            try
            {
                var laborant = db.laborants.FirstOrDefault(x => x.users.login == user.login);
                if (laborant == null)
                {
                    userInfo.Text = "Безымянный лаборант - Лаборант";
                }
                else
                    userInfo.Text = $"{laborant.surname} {laborant.name} {laborant.patronymic} - Лаборант";


                avatar.Source = new BitmapImage(new Uri("pack://application:,,,/images/laborant.jpeg"));

                user.lastenter = DateTime.Now;
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка сохранения последнего времени посещения");
                    Debug.WriteLine(ex);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        string generateBarCode = "";
        string fileName = "CustomBarcode.pdf";
        private void btn_createBarCode_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(code.Text))
            {
                MessageBox.Show("Заполните поле со штрих-кодом");
                return;
            }

            if (code.Text.Length != 6)
            {
                MessageBox.Show("Длина кода пробирки должна быть равна 6!");
                return;
            }

            orders lastOrder = db.orders.OrderByDescending(o => o.code).FirstOrDefault();
            if (lastOrder != null)
            {
                generateBarCode =
                (lastOrder.code + 1).ToString()
                + DateTime.Today.Date.ToString("ddMMyy")
                + code.Text;
            }
            else
            {
                generateBarCode =
                1.ToString()
                + DateTime.Today.Date.ToString("ddMMyy")
                + code.Text;
            }

            MessageBox.Show("Сгенерированный код - " + generateBarCode);

            patientInfo.Visibility = Visibility.Visible;

            // 1. Генерируем штрихкод (только полоски)
            var writer = new ZXing.BarcodeWriterPixelData
            {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = 120,
                    Width = 400,
                    Margin = 10
                }
            };
            var pixels = writer.Write(generateBarCode);
            var barcodeBmp = System.Windows.Media.Imaging.BitmapSource.Create(
                pixels.Width, pixels.Height, 96, 96,
                System.Windows.Media.PixelFormats.Bgra32, null,
                pixels.Pixels, pixels.Width * 4);

            // 2. Дорисовываем текст средствами WPF
            System.Windows.Media.DrawingVisual visual = new System.Windows.Media.DrawingVisual();
            using (System.Windows.Media.DrawingContext dc = visual.RenderOpen())
            {
                // Рисуем штрихкод (в верхней части)
                dc.DrawImage(barcodeBmp, new System.Windows.Rect(0, 0, pixels.Width, pixels.Height));

                // Настраиваем текст
                var text = new System.Windows.Media.FormattedText(
                    generateBarCode,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Windows.FlowDirection.LeftToRight,
                    new System.Windows.Media.Typeface("Arial"),
                    18, // Размер шрифта
                    System.Windows.Media.Brushes.Black,
                    System.Windows.Media.VisualTreeHelper.GetDpi(visual).PixelsPerDip);

                // ОПУСКАЕМ ЦИФРЫ: Рисуем их с отступом 10 пикселей ниже края штрихкода
                // Это гарантирует отсутствие линий и наложений
                double xPosition = (pixels.Width - text.Width) / 2;
                double yPosition = pixels.Height + 10;

                dc.DrawText(text, new System.Windows.Point(xPosition, yPosition));
            }

            // Создаем итоговую картинку с запасом высоты под текст (Height + 50)
            System.Windows.Media.Imaging.RenderTargetBitmap finalBmp =
                new System.Windows.Media.Imaging.RenderTargetBitmap(pixels.Width, pixels.Height + 50, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            finalBmp.Render(visual);

            // 3. Сохраняем в PDF
            PdfSharp.Pdf.PdfDocument doc = new PdfSharp.Pdf.PdfDocument();
            var page = doc.AddPage();
            PdfSharp.Drawing.XGraphics gfx = PdfSharp.Drawing.XGraphics.FromPdfPage(page);

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                System.Windows.Media.Imaging.PngBitmapEncoder encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(finalBmp));
                encoder.Save(ms);

                // Вставляем картинку в PDF (координаты 50, 50, ширина 300, высота 120)
                gfx.DrawImage(PdfSharp.Drawing.XImage.FromStream(ms), 50, 50, 300, 120);
            }

            // 4. Сохранение и открытие
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "barcode_result.pdf");
            doc.Save(path);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });

        }


        private void btn_createOrder_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(textBox_Name.Text)
                || String.IsNullOrEmpty(textBox_Patronymic.Text)
                || String.IsNullOrEmpty(textBox_Surname.Text))
            {
                MessageBox.Show("Заполните все поля!");
                return;
            }

            orders order = new orders();
            patients patient = db.patients.
                FirstOrDefault(p => p.surname == textBox_Surname.Text
                && p.name == textBox_Name.Text
                && p.patronymic == textBox_Patronymic.Text);
            if (patient == null)
            {
                string surname = textBox_Surname.Text.Trim().Substring(0, 1).ToUpper() +
                textBox_Surname.Text.Trim().Substring(1).ToLower();
                string name = textBox_Name.Text.Trim().Substring(0, 1).ToUpper() +
                    textBox_Name.Text.Trim().Substring(1).ToLower();
                string patronymic = textBox_Patronymic.Text.Trim().Substring(0, 1).ToUpper() +
                    textBox_Patronymic.Text.Trim().Substring(1).ToLower();

                patient = new patients
                {
                    surname = surname,
                    name = name,
                    patronymic = patronymic
                };

                try
                {
                    db.patients.Add(patient);
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка создания пациента. Для уточнения информации посмотрите Debug.Log");
                    Debug.WriteLine(ex);
                    db.patients.Remove(patient);
                    return;
                }

                DialogCreatePatient create = new DialogCreatePatient(db, ref patient);
                create.ShowDialog();

                if (create.DialogResult == true)
                {
                    patient = create.getPatient;
                }
                else
                {
                    MessageBox.Show("Окно создания пользователя закрыто, отмена операции");
                    clearFields();
                    return;
                }
            }
            order.patientCode = patient.code;
            order.dateTime = DateTime.Now;

            DialogAddServices add = new DialogAddServices(db, ref order);
            add.ShowDialog();

            if (add.DialogResult == true)
            {
                order.orderStatusCode = 1;

                db.orders.Add(order);
                try
                {
                    db.SaveChanges();
                }   
                catch (DbEntityValidationException ex)
                {
                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            // Выведет в консоль отладки конкретное имя свойства и текст ошибки
                            System.Diagnostics.Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка сохранения заказа");
                    Debug.WriteLine(ex);
                    clearFields();
                    return;
                }

                List<servicesInOrder> servicesInOrders = add.getList;

                foreach (servicesInOrder service in servicesInOrders)
                {
                    service.order = order.code;
                }

                db.servicesInOrder.AddRange(servicesInOrders);
                try
                {
                    db.SaveChanges();
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var validationErrors in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            // Выведет в консоль отладки конкретное имя свойства и текст ошибки
                            System.Diagnostics.Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка сохранения услуг");
                    Debug.WriteLine(ex);
                    clearFields();
                    return;
                }

                Task.Run(() => { MessageBox.Show("Успешное сохранение"); });
                clearFields();
            }
            else
            {
                Task.Run(() => MessageBox.Show("Окно закрыто. Создание отменено"));

                try
                {
                    db.orders.Remove(order);
                    db.patients.Remove(patient);
                    db.passport.Remove(db.passport.FirstOrDefault(p => p.code == patient.code));
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка удаления кеша. Проверьте Debug");
                    Debug.WriteLine(ex);
                }
                return;
            }
        }

        private void clearFields()
        {
            code.Clear();
            textBox_Name.Clear();
            textBox_Surname.Clear();
            textBox_Patronymic.Clear();
        }
    }
}
