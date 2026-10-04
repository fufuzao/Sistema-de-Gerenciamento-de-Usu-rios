using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sistema_de_Gerenciamento_de_Usuários
{
    // =====================================================================
    //  MAINWINDOW (USUÁRIO COMUM)
    //  O usuário comum pode:
    //    - ver todos os usuários (cards SÓ PARA VER);
    //    - alterar o PRÓPRIO perfil: nome, e-mail e avatar;
    //    - trocar a PRÓPRIA senha (confirmando a senha atual).
    //  Ele NÃO cria, NÃO exclui, NÃO desbloqueia e NÃO muda permissões.
    //
    //  IMPORTANTE: o id de quem está sendo alterado vem SEMPRE da Sessao
    //  (dentro da classe Operacoes), nunca de algo que está na tela.
    // =====================================================================
    public partial class MainWindow : Window
    {
        private const int CardsPorPagina = 4;

        private List<Usuario> todosUsuarios = new List<Usuario>();
        private int paginaAtual = 1;

        // Evita que os eventos dos ComboBox rodem enquanto a tela é montada
        private bool telaPronta = false;

        public MainWindow()
        {
            InitializeComponent();

            lstAvataresPerfil.ItemsSource = Avatares.Lista();
        }

        // =================================================================
        //  ABERTURA DA JANELA
        // =================================================================
        private void Janela_Loaded(object sender, RoutedEventArgs e)
        {
            // Segurança: precisa ter alguém logado e com a conta ativa
            bool sessaoValida;

            try
            {
                // BANCO DE DADOS: relê o usuário logado e confere se ainda está ativo
                sessaoValida = Operacoes.SessaoContinuaValida();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao acessar o banco de dados: " + ex.Message, "Usuários",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                sessaoValida = false;
            }

            if (sessaoValida == false)
            {
                VoltarParaLogin();
                return;
            }

            telaPronta = true;
            MostrarUsuarioLogado();
            CarregarUsuarios();
            MostrarTela(gridUsuarios, btnMenuUsuarios);
        }

        // =================================================================
        //  TROCA DE TELAS (só uma Visible, as outras Collapsed)
        // =================================================================
        private void MostrarTela(Grid tela, Button botaoDoMenu)
        {
            Grid[] telas = { gridUsuarios, gridPerfil, gridSenha, gridSair };

            for (int i = 0; i < telas.Length; i++)
            {
                if (telas[i] == tela)
                {
                    telas[i].Visibility = Visibility.Visible;
                }
                else
                {
                    telas[i].Visibility = Visibility.Collapsed;
                }
            }

            Button[] botoes = { btnMenuUsuarios, btnMenuPerfil, btnMenuSenha, btnMenuSair };

            for (int i = 0; i < botoes.Length; i++)
            {
                if (botoes[i] == botaoDoMenu)
                {
                    botoes[i].Tag = "Ativo";
                }
                else
                {
                    botoes[i].Tag = "";
                }
            }
        }

        private void BtnMenuUsuarios_Click(object sender, RoutedEventArgs e)
        {
            CarregarUsuarios();
            MostrarTela(gridUsuarios, btnMenuUsuarios);
        }

        private void BtnMenuPerfil_Click(object sender, RoutedEventArgs e)
        {
            AbrirMeuPerfil();
        }

        private void BtnBlocoUsuario_Click(object sender, RoutedEventArgs e)
        {
            AbrirMeuPerfil();
        }

        private void BtnMenuSenha_Click(object sender, RoutedEventArgs e)
        {
            pwdSenhaAtual.Password = "";
            pwdNovaSenha.Password = "";
            pwdConfirmacaoSenha.Password = "";
            MostrarMensagem(txtMensagemSenha, "", false);

            MostrarTela(gridSenha, btnMenuSenha);
            pwdSenhaAtual.Focus();
        }

        private void BtnMenuSair_Click(object sender, RoutedEventArgs e)
        {
            MostrarTela(gridSair, btnMenuSair);
        }

        // =================================================================
        //  BLOCO DO USUÁRIO LOGADO (barra lateral)
        // =================================================================
        private void MostrarUsuarioLogado()
        {
            Usuario eu = Sessao.UsuarioLogado;

            txtNomeLogado.Text = eu.NomeCompleto;
            txtTipoLogado.Text = eu.Tipo;
            txtArrobaLogado.Text = eu.Arroba;
            txtNivelLogado.Text = "Nível: " + eu.NivelAcesso;
            txtUltimoLoginLogado.Text = eu.TextoUltimoLoginCompleto;

            if (eu.ImagemAvatar != null)
            {
                ImageBrush pintura = new ImageBrush(eu.ImagemAvatar);
                pintura.Stretch = Stretch.UniformToFill;
                elpAvatarLogado.Fill = pintura;
            }
        }

        // =================================================================
        //  LISTA DE USUÁRIOS (somente leitura)
        // =================================================================
        private void CarregarUsuarios()
        {
            MostrarAviso("", false);

            try
            {
                // BANCO DE DADOS: busca todos os usuários
                todosUsuarios = BancoDeDados.ListarUsuarios();
            }
            catch (Exception ex)
            {
                todosUsuarios = new List<Usuario>();
                MostrarAviso("Erro ao acessar o banco de dados: " + ex.Message, true);
            }

            AtualizarCards();
        }

        private void AtualizarCards()
        {
            string busca = txtBusca.Text.Trim().ToLower();
            string perfil = TextoDoCombo(cmbPerfil);
            string status = TextoDoCombo(cmbStatus);

            // 1) separa quem passa na busca e nos filtros
            List<Usuario> filtrados = new List<Usuario>();

            for (int i = 0; i < todosUsuarios.Count; i++)
            {
                if (todosUsuarios[i].CombinaComFiltro(busca, perfil, status))
                {
                    filtrados.Add(todosUsuarios[i]);
                }
            }

            // 2) contador
            if (filtrados.Count == 1)
            {
                txtContador.Text = "1 usuário";
            }
            else
            {
                txtContador.Text = filtrados.Count + " usuários";
            }

            // 3) total de páginas (arredondando para cima)
            int totalPaginas = filtrados.Count / CardsPorPagina;

            if (filtrados.Count % CardsPorPagina != 0)
            {
                totalPaginas = totalPaginas + 1;
            }

            if (totalPaginas == 0)
            {
                totalPaginas = 1;
            }

            if (paginaAtual > totalPaginas)
            {
                paginaAtual = totalPaginas;
            }

            if (paginaAtual < 1)
            {
                paginaAtual = 1;
            }

            // 4) só os 4 da página atual
            List<Usuario> daPagina = new List<Usuario>();
            int inicio = (paginaAtual - 1) * CardsPorPagina;

            for (int i = inicio; i < filtrados.Count && i < inicio + CardsPorPagina; i++)
            {
                daPagina.Add(filtrados[i]);
            }

            icCards.ItemsSource = daPagina;

            if (filtrados.Count == 0)
            {
                txtSemResultados.Visibility = Visibility.Visible;
            }
            else
            {
                txtSemResultados.Visibility = Visibility.Collapsed;
            }

            MontarBotoesDePagina(totalPaginas);
        }

        private void MontarBotoesDePagina(int totalPaginas)
        {
            pnlPaginas.Children.Clear();

            for (int numero = 1; numero <= totalPaginas; numero++)
            {
                Button botao = new Button();
                botao.Content = numero.ToString();
                botao.Style = (Style)FindResource("BotaoPagina");

                if (numero == paginaAtual)
                {
                    botao.Tag = "Atual";
                }
                else
                {
                    botao.Tag = "";
                }

                botao.Click += BotaoPagina_Click;
                pnlPaginas.Children.Add(botao);
            }

            if (paginaAtual > 1)
            {
                btnAnterior.Visibility = Visibility.Visible;
            }
            else
            {
                btnAnterior.Visibility = Visibility.Collapsed;
            }

            if (paginaAtual < totalPaginas)
            {
                btnProxima.Visibility = Visibility.Visible;
            }
            else
            {
                btnProxima.Visibility = Visibility.Collapsed;
            }
        }

        private void BotaoPagina_Click(object sender, RoutedEventArgs e)
        {
            Button botao = (Button)sender;
            paginaAtual = int.Parse(botao.Content.ToString());
            AtualizarCards();
        }

        private void BtnAnterior_Click(object sender, RoutedEventArgs e)
        {
            paginaAtual = paginaAtual - 1;
            AtualizarCards();
        }

        private void BtnProxima_Click(object sender, RoutedEventArgs e)
        {
            paginaAtual = paginaAtual + 1;
            AtualizarCards();
        }

        private void TxtBusca_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtBusca.Text == "")
            {
                txtDicaBusca.Visibility = Visibility.Visible;
            }
            else
            {
                txtDicaBusca.Visibility = Visibility.Collapsed;
            }

            if (telaPronta == false)
            {
                return;
            }

            paginaAtual = 1;
            AtualizarCards();
        }

        private void CmbFiltro_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (telaPronta == false)
            {
                return;
            }

            paginaAtual = 1;
            AtualizarCards();
        }

        // =================================================================
        //  MEU PERFIL (só nome, e-mail e avatar)
        // =================================================================
        private void AbrirMeuPerfil()
        {
            Usuario eu = Sessao.UsuarioLogado;

            txtNomePerfil.Text = eu.NomeCompleto;
            txtEmailPerfil.Text = eu.Email;
            txtUsuarioPerfil.Text = eu.Arroba;
            lstAvataresPerfil.SelectedIndex = Avatares.Posicao(eu.Avatar);

            // as etiquetas de tipo e status leem os dados do próprio usuário (Binding)
            brdTipoPerfil.DataContext = eu;
            brdStatusPerfil.DataContext = eu;

            txtInfoPerfil.Text = "Nível de acesso: " + eu.NivelAcesso + "  •  " + eu.TextoDatas;
            MostrarMensagem(txtMensagemPerfil, "", false);

            MostrarTela(gridPerfil, btnMenuPerfil);
        }

        private void BtnSalvarPerfil_Click(object sender, RoutedEventArgs e)
        {
            string avatar = "";
            OpcaoAvatar opcao = lstAvataresPerfil.SelectedItem as OpcaoAvatar;   // null se nada foi escolhido

            if (opcao != null)
            {
                avatar = opcao.Nome;
            }

            string erro;

            try
            {
                // BANCO DE DADOS: o id vem da Sessao (dentro de Operacoes);
                // grava só nome, e-mail e avatar e registra na auditoria
                erro = Operacoes.AlterarMeuPerfil(txtNomePerfil.Text, txtEmailPerfil.Text, avatar);
            }
            catch (Exception ex)
            {
                MostrarMensagem(txtMensagemPerfil, "Erro ao acessar o banco de dados: " + ex.Message, true);
                return;
            }

            if (erro != "")
            {
                MostrarMensagem(txtMensagemPerfil, erro, true);
                return;
            }

            // a Sessao já foi atualizada pela Operacoes: atualiza a barra lateral e a tela
            MostrarUsuarioLogado();
            AbrirMeuPerfil();
            MostrarMensagem(txtMensagemPerfil, "Perfil atualizado com sucesso.", false);
        }

        // =================================================================
        //  REDEFINIR A PRÓPRIA SENHA
        // =================================================================
        private void BtnAlterarSenha_Click(object sender, RoutedEventArgs e)
        {
            string erro;

            try
            {
                // BANCO DE DADOS: confere a senha atual (BCrypt.Verify), grava o novo hash
                // e registra na auditoria (sem a senha). O id vem da Sessao.
                erro = Operacoes.AlterarMinhaSenha(pwdSenhaAtual.Password, pwdNovaSenha.Password, pwdConfirmacaoSenha.Password);
            }
            catch (Exception ex)
            {
                MostrarMensagem(txtMensagemSenha, "Erro ao acessar o banco de dados: " + ex.Message, true);
                return;
            }

            if (erro != "")
            {
                MostrarMensagem(txtMensagemSenha, erro, true);
                return;
            }

            pwdSenhaAtual.Password = "";
            pwdNovaSenha.Password = "";
            pwdConfirmacaoSenha.Password = "";
            MostrarMensagem(txtMensagemSenha, "Senha alterada com sucesso.", false);
        }

        // =================================================================
        //  SAIR
        // =================================================================
        private void BtnSairSim_Click(object sender, RoutedEventArgs e)
        {
            VoltarParaLogin();
        }

        private void BtnSairNao_Click(object sender, RoutedEventArgs e)
        {
            MostrarTela(gridUsuarios, btnMenuUsuarios);
        }

        // Limpa a sessão, abre o login e fecha esta janela
        private void VoltarParaLogin()
        {
            Sessao.Encerrar();
            teladelogin telaLogin = new teladelogin();
            telaLogin.Show();
            this.Close();
        }

        // =================================================================
        //  AJUDANTES
        // =================================================================
        private string TextoDoCombo(ComboBox combo)
        {
            ComboBoxItem item = combo.SelectedItem as ComboBoxItem;

            if (item == null)
            {
                return "";
            }

            return item.Content.ToString();
        }

        private void MostrarMensagem(TextBlock caixa, string texto, bool ehErro)
        {
            if (ehErro)
            {
                caixa.Foreground = (Brush)FindResource("CorErro");
            }
            else
            {
                caixa.Foreground = (Brush)FindResource("CorSucesso");
            }

            caixa.Text = texto;
        }

        private void MostrarAviso(string texto, bool ehErro)
        {
            MostrarMensagem(txtAvisoLista, texto, ehErro);
        }
    }
}
