using System.Windows;
using System.Windows.Controls;

namespace MLC_Index
{
    /// <summary>
    /// Logique d'interaction pour Resultats.xaml
    /// </summary>
    public partial class Resultats : Window
    {
        public Resultats()
        {
            InitializeComponent();
        }
        public Resultats(string text)
        {
            InitializeComponent();
            resultats.Text = text;
            resultats.IsReadOnly = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
