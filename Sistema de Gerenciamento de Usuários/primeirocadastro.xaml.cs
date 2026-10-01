using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sistema_de_Gerenciamento_de_Usuários
{
    /// <summary>
    /// Tela que cria o primeiro usuario (administrador).
    /// So aparece quando o banco nao tem nenhum usuario.
    /// </summary>
    public partial class primeirocadastro : Window
    {
        public primeirocadastro()
        {
            InitializeComponent();
            Loaded += primeirocadastro_Loaded;

            listaAvatares.ItemsSource = Avatares.Lista;
        }

        // Se ja existe usuario, esta tela nao deve aparecer: vai para o login
        private void primeirocadastro_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Banco.TemUsuarios() == true)
                {
                    IrParaLogin();
                }
            }
            catch (Exception ex)
            {
                Mensagem("Erro ao acessar o banco: " + ex.Message, true);
            }
        }

        private void IrParaLogin()
        {
            teladelogin login = new teladelogin();
            login.Show();
            this.Close();
        }

        private void Criar_Click(object sender, RoutedEventArgs e)
        {
            string nome = txtNome.Text.Trim();
            string usuario = txtUsuario.Text.Trim();
            string email = txtEmail.Text.Trim();
            string senha = pwdSenha.Password;
            string confirma = pwdConfirma.Password;
            AvatarItem avatar = listaAvatares.SelectedItem as AvatarItem;

            // ----- validacoes (para no primeiro erro) -----
            string erro = Validacao.ErroNome(nome);

            if (erro == null)
            {
                erro = Validacao.ErroUsuario(usuario);
            }

            if (erro == null)
            {
                erro = Validacao.ErroEmail(email);
            }

            if (erro == null)
            {
                erro = Validacao.ErroSenha(senha, confirma, "Senha", "Confirmação da senha");
            }

            if (erro == null && avatar == null)
            {
                erro = "Selecione uma imagem de perfil.";
            }

            if (erro != null)
            {
                Mensagem(erro, true);
                return;
            }

            try
            {
                // seguranca: se alguem ja criou o primeiro usuario, nao deixa criar outro administrador por aqui
                if (Banco.TemUsuarios() == true)
                {
                    IrParaLogin();
                    return;
                }

                UsuarioCard novo = new UsuarioCard();
                novo.Nome = nome;
                novo.Usuario = "@" + usuario;
                novo.Email = email;
                novo.Tipo = "Administrador";   // o primeiro usuario e sempre administrador
                novo.Status = "Ativo";
                novo.AvatarNome = avatar.Nome;

                // a senha vira hash (BCrypt) antes de ir para o banco
                string hash = BCrypt.Net.BCrypt.HashPassword(senha);

                Banco.CriarUsuario(novo, hash);
                Banco.RegistrarAuditoria(usuario, "Cadastro de usuário", "@" + usuario, "—", "Administrador / Ativo", "Alteração");
            }
            catch (Exception ex)
            {
                Mensagem("Erro ao acessar o banco: " + ex.Message, true);
                return;
            }

            MessageBox.Show("Administrador criado! Agora faça o login.", "Primeiro cadastro", MessageBoxButton.OK, MessageBoxImage.Information);
            IrParaLogin();
        }

        // Mensagem na tela (vermelha = erro, verde = sucesso)
        private void Mensagem(string texto, bool erro)
        {
            txtMsg.Foreground = erro ? Brushes.LightPink : Brushes.LightGreen;
            // é a mesma coisa que:
            // if (erro == true)
            // {
            //     txtMsg.Foreground = Brushes.LightPink;
            // }
            // else
            // {
            //     txtMsg.Foreground = Brushes.LightGreen;
            // }

            txtMsg.Text = texto;
        }
    }
}
