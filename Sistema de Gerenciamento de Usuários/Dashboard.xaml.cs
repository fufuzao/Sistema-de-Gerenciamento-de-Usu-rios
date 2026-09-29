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
using System.Windows.Media.TextFormatting;
using System.Windows.Shapes;

namespace Sistema_de_Gerenciamento_de_Usuários
{
    /// <summary>
    /// Lógica interna para Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Window
    {

        // public string connectionString (para banco de dados)
        public Dashboard()
        {
            InitializeComponent();
        }

        //botao usuarios (lista)
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            gridAuditoria.Visibility = Visibility.Collapsed;
            gridSair.Visibility = Visibility.Collapsed;
            gridCadastro.Visibility = Visibility.Collapsed;
            gridSenha.Visibility = Visibility.Collapsed;
            gridLista.Visibility = Visibility.Visible;
            
         
        }
        //Botao redefinir senha 
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            gridLista.Visibility = Visibility.Collapsed;
            gridAuditoria.Visibility = Visibility.Collapsed;
            gridSair.Visibility = Visibility.Collapsed;
            gridCadastro.Visibility = Visibility.Collapsed;
            gridSenha.Visibility = Visibility.Visible;
        }
        // botao de voltar 
        private void Anterior_Click(object sender, RoutedEventArgs e)
        {

        }
        // botao de avançar 
        private void Proxima_Click(object sender, RoutedEventArgs e)
        {

        }
        //Botao cadastrar usuario
        private void cadastrousuario_Click(object sender, RoutedEventArgs e)
        {
            
            gridAuditoria.Visibility = Visibility.Collapsed;
            gridSair.Visibility = Visibility.Collapsed;
            gridSenha.Visibility = Visibility.Collapsed;
            gridLista.Visibility = Visibility.Collapsed;
            gridCadastro.Visibility = Visibility.Visible;
        }
        //Botao auditoria 
        private void auditoria_Click(object sender, RoutedEventArgs e)
        {
            gridSair.Visibility = Visibility.Collapsed;
            gridSenha.Visibility = Visibility.Collapsed;
            gridLista.Visibility = Visibility.Collapsed;
            gridCadastro.Visibility = Visibility.Collapsed;
            gridAuditoria.Visibility = Visibility.Visible;
        }
        //Botao sair 
        private void sair_Click(object sender, RoutedEventArgs e)
        {
           
            gridSenha.Visibility = Visibility.Collapsed;
            gridLista.Visibility = Visibility.Collapsed;
            gridCadastro.Visibility = Visibility.Collapsed;
            gridAuditoria.Visibility = Visibility.Collapsed;
            gridSair.Visibility = Visibility.Visible;
        }

        
        // filtro 
        private void AplicarFiltro(object sender, TextChangedEventArgs e)
        {

        }
    }
}
