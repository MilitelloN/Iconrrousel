using Iconrrousel.Main.Configuration;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Iconrrousel.Main
{
    public partial class TimeRangeDialog : Window
    {
        public TimeRange Result { get; private set; }

        public TimeRangeDialog()
        {
            LoggingConfig.Log("Initializing TimeRangeDialog");
            DataContext = App.Data;
            InitializeComponent();

            StartHourBox.Text = "09";
            StartMinuteBox.Text = "00";
            EndHourBox.Text = "17";
            EndMinuteBox.Text = "00";
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("OK button clicked");
            try
            {
                if (!int.TryParse(StartHourBox.Text, out int startHour) || startHour < 0 || startHour > 23)
                {
                    System.Windows.Forms.MessageBox.Show("Start hour must be between 0 and 23", "Invalid Input", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(StartMinuteBox.Text, out int startMinute) || startMinute < 0 || startMinute > 59)
                {
                    System.Windows.Forms.MessageBox.Show("Start minute must be between 0 and 59", "Invalid Input", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(EndHourBox.Text, out int endHour) || endHour < 0 || endHour > 23)
                {
                    System.Windows.Forms.MessageBox.Show("End hour must be between 0 and 23", "Invalid Input", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    return;
                }

                if (!int.TryParse(EndMinuteBox.Text, out int endMinute) || endMinute < 0 || endMinute > 59)
                {
                    System.Windows.Forms.MessageBox.Show("End minute must be between 0 and 59", "Invalid Input", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Warning);
                    return;
                }

                var startTime = new TimeSpan(startHour, startMinute, 0);
                var endTime = new TimeSpan(endHour, endMinute, 0);
                Result = new TimeRange(startTime, endTime);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                LoggingConfig.Log("Error creating time range", ex: ex);
                System.Windows.Forms.MessageBox.Show("Error creating time range: " + ex.Message, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            LoggingConfig.Log("Cancel button clicked");
            DialogResult = false;
            Close();
        }
    }
}
