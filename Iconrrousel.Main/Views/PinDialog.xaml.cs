using Iconrrousel.Main.Configuration;
using System;
using System.Windows;
using System.Windows.Input;

namespace Iconrrousel.Main
{
    // Pide el PIN de 4 digitos. En modo "crear" pide confirmarlo dos veces.
    public partial class PinDialog : Window
    {
        private readonly bool _createMode;

        public string Pin { get; private set; }

        public PinDialog(bool createMode)
        {
            LoggingConfig.Log("Initializing PinDialog");
            DataContext = App.Data;
            InitializeComponent();

            _createMode = createMode;
            TitleText.Text = createMode
                ? "Crear PIN de 4 digitos para proteger los rangos"
                : "Ingresar PIN";
            ConfirmLabel.Visibility = createMode ? Visibility.Visible : Visibility.Collapsed;
            ConfirmBox.Visibility = createMode ? Visibility.Visible : Visibility.Collapsed;

            Loaded += (_, __) => PinBox.Focus();
        }

        private void PinBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsAsciiDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("PIN OK clicked");
            try
            {
                if (!PinLock.IsValidFormat(PinBox.Password))
                {
                    System.Windows.Forms.MessageBox.Show("El PIN debe tener 4 digitos", "PIN invalido", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    return;
                }

                if (_createMode && PinBox.Password != ConfirmBox.Password)
                {
                    System.Windows.Forms.MessageBox.Show("Los PIN no coinciden", "PIN invalido", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    return;
                }

                Pin = PinBox.Password;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error validating PIN", ex: ex);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("PIN cancel clicked");
            DialogResult = false;
            Close();
        }
    }
}
