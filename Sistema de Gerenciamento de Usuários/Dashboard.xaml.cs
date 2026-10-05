using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sistema_de_Gerenciamento_de_Usuários
{
    
    public partial class Dashboard : Window
    {
        // Quantos cards aparecem por página (2 colunas x 2 linhas)
        private const int CardsPorPagina = 4;

        // Todos os usuários lidos do banco (a busca e os filtros trabalham em cima desta lista)
        private List<Usuario> todosUsuarios = new List<Usuario>();

        // Página que está sendo mostrada (começa na 1)
        private int paginaAtual = 1;

        // Usuário que está aberto na tela de edição
        private Usuario usuarioEmEdicao = null;

        // Fica true só depois que a janela abriu. Os ComboBox disparam
        // SelectionChanged enquanto a tela ainda está sendo montada; esta
        // variável evita que o código rode antes da hora.
        private bool telaPronta = false;

        public Dashboard()
        {
            InitializeComponent();

            // as 5 imagens do sistema nas listas de escolha
            lstAvataresCadastro.ItemsSource = Avatares.Lista();
            lstAvataresEdicao.ItemsSource = Avatares.Lista();
        }

        // =================================================================
        //  ABERTURA DA JANELA
        // =================================================================
        private void Janela_Loaded(object sender, RoutedEventArgs e)
        {
            // Segurança: só administrador logado pode ficar nesta janela
            bool podeFicar;

            try
            {
                // BANCO DE DADOS: confere no banco se ainda é administrador ativo
                podeFicar = Operacoes.PodeAdministrar();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao acessar o banco de dados: " + ex.Message, "Dashboard",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                podeFicar = false;
            }

            if (podeFicar == false)
            {
                VoltarParaLogin();
                return;
            }

            telaPronta = true;
            MostrarUsuarioLogado();
            CarregarUsuarios();
            MostrarTela(gridUsuarios, btnMenuUsuarios);
        }

   

        // Deixa só a tela pedida Visible e todas as outras Collapsed.
        // botaoDoMenu = botão do menu que fica destacado (pode ser null)
        private void MostrarTela(Grid tela, Button botaoDoMenu)
        {
            Grid[] telas = { gridUsuarios, gridCadastro, gridEdicao, gridAuditoria, gridSenha, gridSair };

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

            // o botão da tela atual recebe Tag="Ativo" (o estilo deixa ele mais claro)
            Button[] botoes = { btnMenuUsuarios, btnMenuCadastrar, btnMenuAuditoria, btnMenuSenha, btnMenuSair };

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
            MostrarAviso("", false);
            CarregarUsuarios();
            MostrarTela(gridUsuarios, btnMenuUsuarios);
        }

        private void BtnMenuCadastrar_Click(object sender, RoutedEventArgs e)
        {
            LimparCadastro();
            MostrarTela(gridCadastro, btnMenuCadastrar);
            txtNomeCadastro.Focus();
        }

        private void BtnMenuAuditoria_Click(object sender, RoutedEventArgs e)
        {
            MostrarTela(gridAuditoria, btnMenuAuditoria);
            CarregarAuditoria();
        }

        private void BtnMenuSenha_Click(object sender, RoutedEventArgs e)
        {
            CarregarUsuarios();   // lista atualizada para escolher de quem é a senha

            cmbUsuarioSenha.ItemsSource = null;
            cmbUsuarioSenha.ItemsSource = todosUsuarios;
            cmbUsuarioSenha.SelectedIndex = -1;
            pwdNovaSenha.Password = "";
            pwdConfirmacaoSenha.Password = "";
            MostrarMensagem(txtMensagemSenha, "", false);

            MostrarTela(gridSenha, btnMenuSenha);
        }

        private void BtnMenuSair_Click(object sender, RoutedEventArgs e)
        {
            MostrarTela(gridSair, btnMenuSair);
        }

        // Clique no bloco do usuário logado: abre "Meu perfil" (a edição dele mesmo)
        private void BtnBlocoUsuario_Click(object sender, RoutedEventArgs e)
        {
            AbrirEdicao(Sessao.UsuarioLogado.Id);
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

            // o avatar redondo: pinta o círculo com a imagem
            if (eu.ImagemAvatar != null)
            {
                ImageBrush pintura = new ImageBrush(eu.ImagemAvatar);
                pintura.Stretch = Stretch.UniformToFill;
                elpAvatarLogado.Fill = pintura;
            }
        }

        // =================================================================
        //  LISTA DE USUÁRIOS (cards, busca, filtros e páginas)
        // =================================================================

        // Lê os usuários do banco e redesenha os cards
        private void CarregarUsuarios()
        {
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

        // Aplica a busca e os filtros e mostra só os 4 cards da página atual
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

            // 2) contador ("1 usuário" no singular, "4 usuários" no plural)
            if (filtrados.Count == 1)
            {
                txtContador.Text = "1 usuário";
            }
            else
            {
                txtContador.Text = filtrados.Count + " usuários";
            }

            // 3) quantas páginas existem (divisão arredondando para cima)
            int totalPaginas = filtrados.Count / CardsPorPagina;

            if (filtrados.Count % CardsPorPagina != 0)
            {
                totalPaginas = totalPaginas + 1;   // sobrou um pedaço: mais uma página
            }

            if (totalPaginas == 0)
            {
                totalPaginas = 1;                  // lista vazia ainda mostra a página 1
            }

            // garante que a página atual existe
            if (paginaAtual > totalPaginas)
            {
                paginaAtual = totalPaginas;
            }

            if (paginaAtual < 1)
            {
                paginaAtual = 1;
            }

            // 4) pega só os usuários desta página
            List<Usuario> daPagina = new List<Usuario>();
            int inicio = (paginaAtual - 1) * CardsPorPagina;

            for (int i = inicio; i < filtrados.Count && i < inicio + CardsPorPagina; i++)
            {
                daPagina.Add(filtrados[i]);
            }

            icCards.ItemsSource = daPagina;

            // mensagem quando não sobrou ninguém
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

        // Cria um botão para cada página (1, 2, 3...) usando um for
        private void MontarBotoesDePagina(int totalPaginas)
        {
            pnlPaginas.Children.Clear();

            for (int numero = 1; numero <= totalPaginas; numero++)
            {
                Button botao = new Button();
                botao.Content = numero.ToString();
                botao.Style = (Style)FindResource("BotaoPagina");

                // a página atual fica em destaque branco (o estilo olha o Tag)
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

            // "<" só aparece se existe página anterior; ">" só se existe a próxima
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
            paginaAtual = int.Parse(botao.Content.ToString());   // o texto do botão é o número da página
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

        // Digitou na busca: esconde o texto de dica e filtra de novo
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

        // Mudou o filtro de Perfil ou de Status
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
        //  BOTÕES DOS CARDS
        // =================================================================

        // Descobre de qual card veio o clique (o DataContext do botão é o Usuario do card)
        private Usuario UsuarioDoCard(object sender)
        {
            FrameworkElement botao = (FrameworkElement)sender;
            return (Usuario)botao.DataContext;
        }

        private void BtnEditarCard_Click(object sender, RoutedEventArgs e)
        {
            Usuario u = UsuarioDoCard(sender);
            AbrirEdicao(u.Id);
        }

        private void BtnExcluirCard_Click(object sender, RoutedEventArgs e)
        {
            Usuario u = UsuarioDoCard(sender);

            // avisos antes da pergunta (a classe Operacoes confere de novo no banco)
            if (u.Id == Sessao.UsuarioLogado.Id)
            {
                MostrarAviso("Você não pode excluir a sua própria conta enquanto está conectado.", true);
                return;
            }

            MessageBoxResult resposta = MessageBox.Show("Deseja realmente excluir este usuário?\n\n" + u.TextoEscolha,
                                                        "Excluir usuário", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resposta != MessageBoxResult.Yes)
            {
                return;
            }

            string erro;

            try
            {
                // BANCO DE DADOS: confere permissão e regras, exclui e registra na auditoria
                erro = Operacoes.ExcluirUsuario(u.Id);
            }
            catch (Exception ex)
            {
                MostrarAviso("Erro ao acessar o banco de dados: " + ex.Message, true);
                return;
            }

            if (erro != "")
            {
                MostrarAviso(erro, true);
                return;
            }

            CarregarUsuarios();
            MostrarAviso("Usuário " + u.Arroba + " excluído.", false);
        }

        private void BtnDesbloquearCard_Click(object sender, RoutedEventArgs e)
        {
            Usuario u = UsuarioDoCard(sender);
            string erro;

            try
            {
                // BANCO DE DADOS: volta para Ativo, zera as tentativas e registra na auditoria
                erro = Operacoes.DesbloquearUsuario(u.Id);
            }
            catch (Exception ex)
            {
                MostrarAviso("Erro ao acessar o banco de dados: " + ex.Message, true);
                return;
            }

            if (erro != "")
            {
                MostrarAviso(erro, true);
                return;
            }

            CarregarUsuarios();
            MostrarAviso("Usuário " + u.Arroba + " desbloqueado.", false);
        }

        // =================================================================
        //  CADASTRO
        // =================================================================
        private void BtnCadastrar_Click(object sender, RoutedEventArgs e)
        {
            Usuario novo = new Usuario();
            novo.NomeCompleto = txtNomeCadastro.Text;
            novo.NomeUsuario = txtUsuarioCadastro.Text;
            novo.Email = txtEmailCadastro.Text;
            novo.Tipo = TextoDoCombo(cmbTipoCadastro);
            novo.Status = TextoDoCombo(cmbStatusCadastro);
            novo.Avatar = AvatarEscolhido(lstAvataresCadastro);

            string erro;

            try
            {
                // BANCO DE DADOS: confere permissão, valida, grava com hash e registra na auditoria
                erro = Operacoes.CadastrarUsuario(novo, pwdSenhaCadastro.Password, pwdConfirmacaoCadastro.Password);
            }
            catch (Exception ex)
            {
                MostrarMensagem(txtMensagemCadastro, "Erro ao acessar o banco de dados: " + ex.Message, true);
                return;
            }

            if (erro != "")
            {
                MostrarMensagem(txtMensagemCadastro, erro, true);
                return;
            }

            LimparCadastro();
            MostrarMensagem(txtMensagemCadastro, "Usuário " + novo.Arroba + " cadastrado com sucesso.", false);
            CarregarUsuarios();
        }

        private void BtnLimparCadastro_Click(object sender, RoutedEventArgs e)
        {
            LimparCadastro();
        }

        private void LimparCadastro()
        {
            txtNomeCadastro.Text = "";
            txtUsuarioCadastro.Text = "";
            txtEmailCadastro.Text = "";
            pwdSenhaCadastro.Password = "";
            pwdConfirmacaoCadastro.Password = "";
            cmbTipoCadastro.SelectedIndex = 1;      // Comum
            cmbStatusCadastro.SelectedIndex = 0;    // Ativo
            lstAvataresCadastro.SelectedIndex = -1; // nenhum avatar
            MostrarMensagem(txtMensagemCadastro, "", false);
        }

        // =================================================================
        //  EDIÇÃO (qualquer usuário, inclusive o próprio administrador)
        // =================================================================
        private void AbrirEdicao(int id)
        {
            Usuario u;

            try
            {
                // BANCO DE DADOS: lê o usuário atualizado
                u = BancoDeDados.BuscarUsuarioPorId(id);
            }
            catch (Exception ex)
            {
                MostrarAviso("Erro ao acessar o banco de dados: " + ex.Message, true);
                return;
            }

            if (u == null)
            {
                MostrarAviso("Este usuário não existe mais.", true);
                CarregarUsuarios();
                return;
            }

            usuarioEmEdicao = u;

            // preenche os campos (a senha NÃO aparece)
            txtNomeEdicao.Text = u.NomeCompleto;
            txtUsuarioEdicao.Text = u.NomeUsuario;
            txtEmailEdicao.Text = u.Email;
            SelecionarNoCombo(cmbTipoEdicao, u.Tipo);
            SelecionarNoCombo(cmbStatusEdicao, u.Status);
            lstAvataresEdicao.SelectedIndex = Avatares.Posicao(u.Avatar);
            txtInfoEdicao.Text = u.Arroba + "  •  " + u.TextoDatas + "  •  " + u.TextoUltimoLoginCompleto;
            MostrarMensagem(txtMensagemEdicao, "", false);

            // editando a si mesmo = "Meu perfil"
            if (u.Id == Sessao.UsuarioLogado.Id)
            {
                txtTituloEdicao.Text = "Meu perfil";
                MostrarTela(gridEdicao, null);
            }
            else
            {
                txtTituloEdicao.Text = "Editar usuário";
                MostrarTela(gridEdicao, btnMenuUsuarios);
            }
        }

        private void BtnSalvarEdicao_Click(object sender, RoutedEventArgs e)
        {
            if (usuarioEmEdicao == null)
            {
                return;
            }

            // copia o original e troca só o que a tela permite editar
            Usuario alterado = usuarioEmEdicao.Copiar();
            alterado.NomeCompleto = txtNomeEdicao.Text;
            alterado.NomeUsuario = txtUsuarioEdicao.Text;
            alterado.Email = txtEmailEdicao.Text;
            alterado.Tipo = TextoDoCombo(cmbTipoEdicao);
            alterado.Status = TextoDoCombo(cmbStatusEdicao);
            alterado.Avatar = AvatarEscolhido(lstAvataresEdicao);

            string erro;

            try
            {
                // BANCO DE DADOS: confere permissão e a regra do último administrador,
                // grava e registra UMA linha de auditoria para cada campo alterado
                erro = Operacoes.EditarUsuario(alterado);
            }
            catch (Exception ex)
            {
                MostrarMensagem(txtMensagemEdicao, "Erro ao acessar o banco de dados: " + ex.Message, true);
                return;
            }

            if (erro != "")
            {
                MostrarMensagem(txtMensagemEdicao, erro, true);
                return;
            }

            // Se o administrador editou a SI MESMO, confere se ele continua administrador ativo
            if (alterado.Id == Sessao.UsuarioLogado.Id)
            {
                bool continuaAdmin;

                try
                {
                    // BANCO DE DADOS: relê o usuário logado (também atualiza a Sessao)
                    continuaAdmin = Operacoes.PodeAdministrar();
                }
                catch (Exception)
                {
                    continuaAdmin = false;
                }

                if (continuaAdmin == false)
                {
                    MessageBox.Show("Você deixou de ser administrador ativo. Faça o login novamente.",
                                    "Meu perfil", MessageBoxButton.OK, MessageBoxImage.Information);
                    VoltarParaLogin();
                    return;
                }

                MostrarUsuarioLogado();   // nome/avatar novos no bloco da barra lateral
            }

            usuarioEmEdicao = null;
            CarregarUsuarios();
            MostrarTela(gridUsuarios, btnMenuUsuarios);
            MostrarAviso("Alterações de " + alterado.Arroba + " salvas.", false);
        }

        private void BtnCancelarEdicao_Click(object sender, RoutedEventArgs e)
        {
            usuarioEmEdicao = null;
            MostrarTela(gridUsuarios, btnMenuUsuarios);
        }

        // =================================================================
        //  AUDITORIA
        // =================================================================
        private void CarregarAuditoria()
        {
            List<EventoAuditoria> eventos;

            try
            {
                // BANCO DE DADOS: confere se é administrador e lista os eventos
                eventos = Operacoes.ConsultarAuditoria(TextoDoCombo(cmbCategoria));
            }
            catch (Exception ex)
            {
                dgAuditoria.ItemsSource = null;
                txtContadorAuditoria.Text = "Erro ao acessar o banco de dados: " + ex.Message;
                return;
            }

            if (eventos == null)
            {
                dgAuditoria.ItemsSource = null;
                txtContadorAuditoria.Text = "Você não tem permissão para consultar a auditoria.";
                return;
            }

            dgAuditoria.ItemsSource = eventos;
            txtContadorAuditoria.Text = eventos.Count + " registro(s) — os mais recentes primeiro";
        }

        private void CmbCategoria_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (telaPronta == false)
            {
                return;
            }

            CarregarAuditoria();
        }

        private void BtnAtualizarAuditoria_Click(object sender, RoutedEventArgs e)
        {
            CarregarAuditoria();
        }

        // =================================================================
        //  REDEFINIR SENHA (de qualquer usuário)
        // =================================================================
        private void BtnRedefinirSenha_Click(object sender, RoutedEventArgs e)
        {
            Usuario alvo = cmbUsuarioSenha.SelectedItem as Usuario;   // "as" devolve null se nada foi escolhido

            if (alvo == null)
            {
                MostrarMensagem(txtMensagemSenha, "Selecione o usuário.", true);
                return;
            }

            string erro;

            try
            {
                // BANCO DE DADOS: confere permissão, grava o HASH e registra (sem a senha)
                erro = Operacoes.RedefinirSenha(alvo.Id, pwdNovaSenha.Password, pwdConfirmacaoSenha.Password);
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

            pwdNovaSenha.Password = "";
            pwdConfirmacaoSenha.Password = "";
            MostrarMensagem(txtMensagemSenha, "Senha de " + alvo.Arroba + " redefinida com sucesso.", false);
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

        // Texto do item escolhido em um ComboBox ("" se nada escolhido)
        private string TextoDoCombo(ComboBox combo)
        {
            ComboBoxItem item = combo.SelectedItem as ComboBoxItem;

            if (item == null)
            {
                return "";
            }

            return item.Content.ToString();
        }

        // Seleciona no ComboBox o item que tem o texto pedido
        private void SelecionarNoCombo(ComboBox combo, string texto)
        {
            combo.SelectedIndex = -1;

            for (int i = 0; i < combo.Items.Count; i++)
            {
                ComboBoxItem item = (ComboBoxItem)combo.Items[i];

                if (item.Content.ToString() == texto)
                {
                    combo.SelectedIndex = i;
                }
            }
        }

        // Nome do avatar escolhido na lista ("" se nenhum)
        private string AvatarEscolhido(ListBox lista)
        {
            OpcaoAvatar opcao = lista.SelectedItem as OpcaoAvatar;

            if (opcao == null)
            {
                return "";
            }

            return opcao.Nome;
        }

        // Mensagem de um formulário (vermelho claro = erro, verde claro = sucesso)
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

        // Aviso na linha do contador, na tela dos cards
        private void MostrarAviso(string texto, bool ehErro)
        {
            MostrarMensagem(txtAvisoLista, texto, ehErro);

            // texto comprido: passar o mouse mostra inteiro (sem texto, sem dica)
            if (texto == "")
            {
                txtAvisoLista.ToolTip = null;
            }
            else
            {
                txtAvisoLista.ToolTip = texto;
            }
        }
    }
}
