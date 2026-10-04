using System;
using System.Windows;

namespace Sistema_de_Gerenciamento_de_Usuários
{
    // =====================================================================
    //  TELA DE LOGIN (é a primeira janela que abre - StartupUri do App.xaml)
    //  1) Ao abrir: se o banco não tem nenhum usuário, vai para o
    //     "Primeiro cadastro". Se o banco não responde, avisa e desativa
    //     o botão Entrar.
    //  2) Ao clicar em Entrar: a classe Autenticacao faz todas as
    //     conferências. Se deu certo, o TIPO do usuário decide a janela:
    //     Administrador -> Dashboard; Comum -> MainWindow.
    // =====================================================================
    public partial class teladelogin : Window
    {
        public teladelogin()
        {
            InitializeComponent();
        }

        // Roda quando a janela termina de abrir
        private void Janela_Loaded(object sender, RoutedEventArgs e)
        {
            VerificarBanco();
        }

        // Confere se o banco responde e se já existe algum usuário
        private void VerificarBanco()
        {
            int totalUsuarios;

            try
            {
                // BANCO DE DADOS: conta os usuários
                totalUsuarios = BancoDeDados.ContarUsuarios();
            }
            catch (Exception ex)
            {
                // o banco não respondeu: não dá para fazer login
                btnEntrar.IsEnabled = false;
                btnTentarNovamente.Visibility = Visibility.Visible;
                MostrarMensagem("Não foi possível conectar ao banco de dados. " +
                                "Verifique se o MySQL está ligado (porta 3306) e se o banco \"login\" foi criado.\n" +
                                "Detalhe: " + ex.Message, true);
                return;
            }

            // o banco respondeu: deixa tudo normal
            btnEntrar.IsEnabled = true;
            btnTentarNovamente.Visibility = Visibility.Collapsed;
            MostrarMensagem("", false);

            if (totalUsuarios == 0)
            {
                // nenhum usuário ainda: abre o primeiro cadastro (cria o administrador)
                primeirocadastro telaCadastro = new primeirocadastro();
                telaCadastro.Show();
                this.Close();
                return;
            }

            txtUsuario.Focus();
        }

        private void BtnTentarNovamente_Click(object sender, RoutedEventArgs e)
        {
            VerificarBanco();
        }

        private void BtnEntrar_Click(object sender, RoutedEventArgs e)
        {
            string mensagemErro;

            try
            {
                // BANCO DE DADOS: a Autenticacao consulta o usuário, confere o hash,
                // conta as tentativas, bloqueia, grava o último login e a auditoria.
                mensagemErro = Autenticacao.Entrar(txtUsuario.Text, pwdSenha.Password);
            }
            catch (Exception ex)
            {
                MostrarMensagem("Erro ao acessar o banco de dados: " + ex.Message, true);
                return;
            }

            if (mensagemErro != "")
            {
                // deu errado: mostra a mensagem e limpa só a senha
                MostrarMensagem(mensagemErro, true);
                pwdSenha.Password = "";
                pwdSenha.Focus();
                return;
            }

            // deu certo: o TIPO lido do banco decide qual janela abre
            if (Sessao.EhAdministrador())
            {
                Dashboard telaAdmin = new Dashboard();
                telaAdmin.Show();
            }
            else
            {
                MainWindow telaComum = new MainWindow();
                telaComum.Show();
            }

            this.Close();
        }

        // Mostra a mensagem embaixo do botão (vermelho claro = erro, verde claro = sucesso)
        private void MostrarMensagem(string texto, bool ehErro)
        {
            if (ehErro)
            {
                txtMensagem.Foreground = (System.Windows.Media.Brush)FindResource("CorErro");
            }
            else
            {
                txtMensagem.Foreground = (System.Windows.Media.Brush)FindResource("CorSucesso");
            }

            txtMensagem.Text = texto;
        }
    }
}
