using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Sistema_de_Gerenciamento_de_Usuários
{
    /// Lógica interna para teladelogin.xaml

    public partial class teladelogin : Window
    {
        public teladelogin()
        {
            InitializeComponent();
        }
        // codigo do banco de dados

        private void botaologar_Click(object sender, RoutedEventArgs e)
        {
            Dashboard dashboard = new Dashboard();
            dashboard.Show();
            this.Close();
        }

       
    }
}
