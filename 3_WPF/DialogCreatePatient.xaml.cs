using Mail_LIB;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
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

namespace _3_WPF
{
    /// <summary>
    /// Логика взаимодействия для DialogCreatePatient.xaml
    /// </summary>
    public partial class DialogCreatePatient : Window
    {
        Entities db;
        patients patient;

        bool success = false;
        public DialogCreatePatient(Entities db, ref patients patient)
        {
            InitializeComponent();

            this.db = db;
            this.patient = patient;

            textBox_Surname.Text = patient.surname;
            textBox_Name.Text = patient.name;
            textBox_Patronymic.Text = patient.patronymic;

            comboBox_InsuranceType.ItemsSource = db.insuranceTypes.ToList();
            comboBox_InsuranceCompany.ItemsSource = db.insuranceCompany.ToList();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (datePicker_DateOfBirth.SelectedDate == null
                || String.IsNullOrEmpty(textBox_PassportSeries.Text)
                || String.IsNullOrEmpty(textBox_PassportNumber.Text)
                || String.IsNullOrEmpty(textBox_Email.Text)
                || String.IsNullOrEmpty(textBox_PhoneNumber.Text)
                || String.IsNullOrEmpty(textBox_InsuranceNumber.Text)
                || comboBox_InsuranceCompany.SelectedIndex == -1
                || comboBox_InsuranceType.SelectedIndex == -1)
            {
                MessageBox.Show("Заполните все поля!", "Ошибка");
                return;
            }

            // Валидация возраста, почты и телефона
            if (datePicker_DateOfBirth.SelectedDate.Value > DateTime.Now.AddYears(-18))
            {
                MessageBox.Show("Несовершеннолетних нельзя регистрировать!");
                return;
            }

            string phoneNumber = Regex.Replace(textBox_PhoneNumber.Text.Trim(), @"\D", "");
            if (phoneNumber.Length != 11)
            {
                MessageBox.Show("Номер телефона должен содержать 11 цифр");
                return;
            }
            phoneNumber = "7" + phoneNumber.Substring(1);

            try
            {
                patient.email = textBox_Email.Text.Trim();
                patient.dateOfBirth = datePicker_DateOfBirth.SelectedDate.Value;
                patient.phoneNumber = phoneNumber;
                patient.insuranceNumber = textBox_InsuranceNumber.Text.Trim();
                patient.insuranceTypeCode = (comboBox_InsuranceType.SelectedItem as insuranceTypes).code;
                patient.incuranceCompanyCode = (comboBox_InsuranceCompany.SelectedItem as insuranceCompany).code;

                if (db.Entry(patient).State == EntityState.Detached)
                {
                    db.patients.Add(patient);
                }

                db.SaveChanges();

                passport newPassport = new passport
                {
                    code = patient.code, 
                    series = textBox_PassportSeries.Text.Trim(),
                    number = textBox_PassportNumber.Text.Trim()
                };

                db.passport.Add(newPassport);

                patient.passportCode = patient.code;

                db.SaveChanges();

                success = true;
                MessageBox.Show("Пациент и паспорт успешно созданы");
                this.DialogResult = true;
                Close();
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var eve in ex.EntityValidationErrors)
                    foreach (var ve in eve.ValidationErrors)
                        Debug.WriteLine($"Свойство: {ve.PropertyName}, Ошибка: {ve.ErrorMessage}");

                MessageBox.Show("Ошибка валидации. Проверьте длину полей в окне Output.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }



        public patients getPatient
        {
            get
            {
                if (success) return patient;
                else return null;
            }
        }
    }
}
