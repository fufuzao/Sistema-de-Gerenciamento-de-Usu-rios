using System;
using System.Windows;
using System.Windows.Media;

namespace Sistema_de_Gerenciamento_de_Usuários
{

    public partial class primeirocadastro : Window
    {
        public primeirocadastro()
        {
            InitializeComponent();

            // as 5 imagens do sistema na lista de escolha
            lstAvatares.ItemsSource = Avatares.Lista();
        }

        // Segurança: se já existe usuário, esta tela não pode ser usada
        private void Janela_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // BANCO DE DADOS: conta os usuários
                if (BancoDeDados.ContarUsuarios() > 0)
                {
                    IrParaLogin();
                    return;
                }
            }
            catch (Exception ex)
            {
                MostrarMensagem("Erro ao acessar o banco de dados: " + ex.Message, true);
                btnCriar.IsEnabled = false;
                return;
            }

            txtNome.Focus();
        }

        private void BtnCriar_Click(object sender, RoutedEventArgs e)
        {
            // monta o usuário com o que foi digitado
            Usuario novo = new Usuario();
            novo.NomeCompleto = txtNome.Text;
            novo.NomeUsuario = txtUsuario.Text;
            novo.Email = txtEmail.Text;
            novo.Avatar = AvatarEscolhido();
            // o tipo (Administrador) e o status (Ativo) são definidos na classe Operacoes

            string erro;

            try
            {
                // BANCO DE DADOS: valida, grava com a senha em hash e registra na auditoria
                erro = Operacoes.CriarPrimeiroAdministrador(novo, pwdSenha.Password, pwdConfirmacao.Password);
            }
            catch (Exception ex)
            {
                MostrarMensagem("Erro ao acessar o banco de dados: " + ex.Message, true);
                return;
            }

            if (erro != "")
            {
                MostrarMensagem(erro, true);
                return;
            }

            MessageBox.Show("Administrador criado com sucesso! Agora faça o login.", "Primeiro cadastro",
                            MessageBoxButton.OK, MessageBoxImage.Information);
            IrParaLogin();
        }

        // Nome do avatar selecionado ("" se nenhum foi escolhido)
        private string AvatarEscolhido()
        {
            OpcaoAvatar opcao = lstAvatares.SelectedItem as OpcaoAvatar;   // "as" devolve null se nada foi escolhido

            if (opcao == null)
            {
                return "";
            }

            return opcao.Nome;
        }

        private void IrParaLogin()
        {
            teladelogin telaLogin = new teladelogin();
            telaLogin.Show();
            this.Close();
        }

        private void MostrarMensagem(string texto, bool ehErro)
        {
            if (ehErro)
            {
                txtMensagem.Foreground = (Brush)FindResource("CorErro");
            }
            else
            {
                txtMensagem.Foreground = (Brush)FindResource("CorSucesso");
            }

            txtMensagem.Text = texto;
        }
    }
}
