using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Hotel.Controls 
{
    public partial class NumericUpDown : UserControl 
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(NumericUpDown),
                new PropertyMetadata(1, OnValueChanged, CoerceValue));

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private static object CoerceValue(DependencyObject d, object baseValue)
        {
            int value = (int)baseValue;
            if (value < 1) return 1;
            return value;
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NumericUpDown ctrl = (NumericUpDown)d;
            ctrl.txtValue.Text = ctrl.Value.ToString();
        }

        public NumericUpDown()
        {
            InitializeComponent();
            txtValue.Text = Value.ToString();
        }

        private void TxtValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(txtValue.Text, out int val))
            {
                Value = val;
            }
            else
            {
                Value = 1;
                txtValue.Text = "1";
            }
        }

        private void TxtValue_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"[0-9]+");
        }

        private void BtnUp_Click(object sender, RoutedEventArgs e)
        {
            Value++;
        }

        private void BtnDown_Click(object sender, RoutedEventArgs e)
        {
            if (Value > 1)
            {
                Value--;
            }
        }
    }
}