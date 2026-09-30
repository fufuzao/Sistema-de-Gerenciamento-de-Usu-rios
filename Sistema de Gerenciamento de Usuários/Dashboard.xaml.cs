using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    // Dados que aparecem em cada card
   

    /// <summary>
    /// Lógica interna para Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Window
    {
       

        // BANCO DE DADOS: coloque aqui a string de conexao
        private string connectionString = "";

        private const string Admin = "Administrador";
        private const string Comum = "Comum";
        private const string Ativo = "Ativo";
        private const string Bloqueado = "Bloqueado";

        private const string CatAlteracao = "Alteração";
        private const string CatAutenticacao = "Autenticação";

        private const int PorPagina = 4;
        private const int MinUsuario = 3;
        private const int MinSenha = 8;

        // QUEM ESTA LOGADO
        // BANCO DE DADOS: pegar essas informacoes da tela de login
        private bool souAdmin = true;
        private string usuarioLogado = "maria";

        // Listas na memoria
        private List<UsuarioCard> usuarios = new List<UsuarioCard>();
        private List<RegistroAuditoria> registros = new List<RegistroAuditoria>();

        private int paginaAtual = 1;
        private int totalPaginas = 1;

        // So deixa os filtros funcionarem depois que a tela terminou de carregar
        private bool pronto = false;

      

        public Dashboard()
        {
            InitializeComponent();

            nomeusuariotelainferior.Text = usuarioLogado;
            // BANCO DE DADOS: carregar a imagem do usuario logado (imagem_usuario_)

            AplicarPermissoes();
            CarregarAvatares();
            CarregarUsuarios();
            CarregarAuditoria();

            pronto = true;
            AtualizarLista();
        }

       

        // Esconde (Collapsed) o que o usuario comum nao pode usar
        private void AplicarPermissoes()
        {
            Visibility soAdmin = souAdmin ? Visibility.Visible : Visibility.Collapsed;

            cadastrousuario.Visibility = soAdmin;
            auditoria.Visibility = soAdmin;

            // Na tela de senha: admin escolhe o usuario, comum digita a senha atual
            painelUsuarioAlvo.Visibility = soAdmin;
            painelSenhaAtual.Visibility = souAdmin ? Visibility.Collapsed : Visibility.Visible;
        }

        // Avatares que o administrador pode escolher no cadastro
        private void CarregarAvatares()
        {
            List<string> avatares = new List<string>();

            for (int i = 1; i <= 5; i++)
            {
                avatares.Add($"pack://application:,,,/Avatares/avatar0{i}.png");
            }

            listaAvatares.ItemsSource = avatares;
        }

        private void CarregarUsuarios()
        {
           
        }

        

      

        // So uma tela fica Visible, todas as outras ficam Collapsed
        private void MostrarTela(Grid tela)
        {
            Grid[] todas = { gridLista, gridCadastro, gridAuditoria, gridSenha, gridSair };

            foreach (Grid g in todas)
            {
                g.Visibility = (g == tela) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        //botao usuarios (lista)
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            MostrarTela(gridLista);
        }

        //Botao redefinir senha
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            txtMsgSenha.Text = "";

            cmbUsuarioAlvo.ItemsSource = null;
            cmbUsuarioAlvo.ItemsSource = usuarios;

            MostrarTela(gridSenha);
        }

        //Botao cadastrar usuario
        private void cadastrousuario_Click(object sender, RoutedEventArgs e)
        {
            if (!souAdmin) return; // so administrador entra aqui

            txtMsgCadastro.Text = "";
            MostrarTela(gridCadastro);
        }

        //Botao auditoria
        private void auditoria_Click(object sender, RoutedEventArgs e)
        {
            if (!souAdmin) return; // so administrador entra aqui

            AtualizarAuditoria();
            MostrarTela(gridAuditoria);
        }

        //Botao sair
        private void sair_Click(object sender, RoutedEventArgs e)
        {
            MostrarTela(gridSair);
        }

        private void SairSim_Click(object sender, RoutedEventArgs e)
        {
           
        }

        private void SairNao_Click(object sender, RoutedEventArgs e)
        {
            MostrarTela(gridLista);
        }

        

        // filtro 
        private void AplicarFiltro(object sender, RoutedEventArgs e)
        {
            if (!pronto) return; 

            paginaAtual = 1;
            AtualizarLista();
        }

        // Le o texto do item selecionado de um ComboBox
        private string LerCombo(ComboBox combo)
        {
            return ((ComboBoxItem)combo.SelectedItem).Content.ToString();
        }

        // Devolve so os usuarios que passam na busca e nos filtros
        private List<UsuarioCard> ObterFiltrados()
        {
            string busca = txtBusca.Text.Trim().ToLower();
            string perfil = LerCombo(cmbPerfil);
            string status = LerCombo(cmbStatus);

            return usuarios.Where(u =>
                (busca == "" || u.Nome.ToLower().Contains(busca)
                             || u.Usuario.ToLower().Contains(busca)
                             || u.Email.ToLower().Contains(busca))
                && (perfil == "Todos" || u.Tipo == perfil)
                && (status == "Todos" || u.Status == status)
            ).ToList();
        }

        private void AtualizarLista()
        {
            List<UsuarioCard> filtrados = ObterFiltrados();

           
            totalPaginas = Math.Max(1, (int)Math.Ceiling(filtrados.Count / (double)PorPagina));

           
            paginaAtual = Math.Min(Math.Max(paginaAtual, 1), totalPaginas);

            
            listaCards.ItemsSource = null;
            listaCards.ItemsSource = filtrados
                .Skip((paginaAtual - 1) * PorPagina)
                .Take(PorPagina)
                .ToList();

            txtContador.Text = filtrados.Count == 1 ? "1 usuário" : $"{filtrados.Count} usuários";

            MontarBotoesPagina();
        }

        
        private void MontarBotoesPagina()
        {
            painelPaginas.Children.Clear();

            for (int i = 1; i <= totalPaginas; i++)
            {
                Button b = new Button();
                b.Content = i.ToString();
                b.Tag = i;
                b.Width = 30;
                b.Height = 28;
                b.Margin = new Thickness(3, 0, 3, 0);

                if (i == paginaAtual)
                {
                    b.FontWeight = FontWeights.Bold;
                    b.Background = Brushes.White;
                }

                b.Click += BotaoPagina_Click;
                painelPaginas.Children.Add(b);
            }

            
            btnAnterior.Visibility = paginaAtual > 1 ? Visibility.Visible : Visibility.Collapsed;
            btnProxima.Visibility = paginaAtual < totalPaginas ? Visibility.Visible : Visibility.Collapsed;
        }

        // Clicou em um numero de pagina
        private void BotaoPagina_Click(object sender, RoutedEventArgs e)
        {
            paginaAtual = (int)((Button)sender).Tag;
            AtualizarLista();
        }

        // botao de voltar
        private void Anterior_Click(object sender, RoutedEventArgs e)
        {
            paginaAtual--;
            AtualizarLista();
        }

        // botao de avançar
        private void Proxima_Click(object sender, RoutedEventArgs e)
        {
            paginaAtual++;
            AtualizarLista();
        }

        

        // Pega o usuario do card onde o botao foi clicado
        private UsuarioCard UsuarioDoBotao(object sender)
        {
            return (UsuarioCard)((FrameworkElement)sender).DataContext;
        }

        
        private bool EhUltimoAdmin(UsuarioCard u)
        {
            return u.Tipo == Admin && usuarios.Count(x => x.Tipo == Admin) == 1;
        }

        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            if (!souAdmin) return;

            UsuarioCard u = UsuarioDoBotao(sender);

            // Ainda nao tem tela de edicao
            MessageBox.Show("Editar: " + u.Nome);

            // ==========================================================
            // BANCO DE DADOS: atualizar os dados do usuario no banco
            // (lembre de registrar na auditoria o valor anterior e o novo)
            //
            // ==========================================================
        }

        private void Excluir_Click(object sender, RoutedEventArgs e)
        {
            if (!souAdmin) return;

            UsuarioCard u = UsuarioDoBotao(sender);

            if (u.Usuario == "@" + usuarioLogado)
            {
                MessageBox.Show("Você não pode excluir a própria conta enquanto estiver conectado.");
                return;
            }

            if (EhUltimoAdmin(u))
            {
                MessageBox.Show("Não é possível excluir o último administrador do sistema.");
                return;
            }

            MessageBoxResult resposta = MessageBox.Show("Deseja realmente excluir este usuário?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resposta != MessageBoxResult.Yes) return;

            try
            {
                // ==========================================================
                // BANCO DE DADOS: excluir o usuario "u" do banco (use u.Id)
                //
                //
                // ==========================================================
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao acessar o banco: " + ex.Message);
                return;
            }

            usuarios.Remove(u);
            RegistrarAuditoria("Exclusão de usuário", u.Usuario, u.Nome, "—");
            AtualizarLista();
        }

        private void Desbloquear_Click(object sender, RoutedEventArgs e)
        {
            if (!souAdmin) return;

            UsuarioCard u = UsuarioDoBotao(sender);

            // ==========================================================
            // BANCO DE DADOS: mudar o status para Ativo e zerar as tentativas de login
            //
            //
            // ==========================================================

            u.Status = Ativo;
            RegistrarAuditoria("Desbloqueio de usuário", u.Usuario, Bloqueado, Ativo);
            AtualizarLista();
        }

        #endregion

        #region Cadastro de usuario

        // Devolve a mensagem do primeiro erro encontrado, ou null se estiver tudo certo
        private string ValidarCadastro(string nome, string usuario, string email, string senha, string confirma)
        {
            if (nome == "") return "O campo Nome completo é obrigatório.";

            if (usuario == "") return "O campo Nome de usuário é obrigatório.";
            if (usuario.Length < MinUsuario) return $"O nome de usuário deve possuir no mínimo {MinUsuario} caracteres.";

            if (email == "") return "O campo E-mail é obrigatório.";
            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) return "Informe um e-mail em formato válido.";

            string erroSenha = ValidarSenha(senha, confirma, "Senha", "Confirmação da senha");
            if (erroSenha != null) return erroSenha;

            // BANCO DE DADOS: aqui voce pode consultar o banco em vez de olhar a lista
            if (usuarios.Any(u => u.Usuario.ToLower() == "@" + usuario.ToLower())) return "Este nome de usuário já está em uso.";
            if (usuarios.Any(u => u.Email.ToLower() == email.ToLower())) return "Este e-mail já está cadastrado.";

            if (listaAvatares.SelectedItem == null) return "Selecione uma imagem de perfil.";

            return null;
        }

        // Validacao de senha usada no cadastro e na redefinicao
        private string ValidarSenha(string senha, string confirma, string nomeCampo, string nomeConfirmacao)
        {
            if (senha == "") return $"O campo {nomeCampo} é obrigatório.";
            if (senha.Length < MinSenha) return $"A senha deve possuir no mínimo {MinSenha} caracteres.";
            if (confirma == "") return $"O campo {nomeConfirmacao} é obrigatório.";
            if (senha != confirma) return "A senha e a confirmação devem ser iguais.";

            return null;
        }

        private void Cadastrar_Click(object sender, RoutedEventArgs e)
        {
            if (!souAdmin) return; // so administrador cadastra

            string nome = txtNomeCad.Text.Trim();
            string usuario = txtUsuarioCad.Text.Trim();
            string email = txtEmailCad.Text.Trim();
            string senha = pwdSenhaCad.Password;
            string confirma = pwdConfirmaCad.Password;
            string tipo = LerCombo(cmbTipoCad);
            string status = LerCombo(cmbStatusCad);

            string erro = ValidarCadastro(nome, usuario, email, senha, confirma);
            if (erro != null)
            {
                Mensagem(txtMsgCadastro, erro, true);
                return;
            }

            UsuarioCard novo = new UsuarioCard
            {
                Nome = nome,
                Usuario = "@" + usuario,
                Email = email,
                Tipo = tipo,
                Status = status,
                UltimoLogin = "Nenhum acesso registrado",
                AvatarPath = listaAvatares.SelectedItem.ToString(),
                PodeEditar = souAdmin
            };

            // ==========================================================
            // SEGURANCA: transformar "senha" em hash (ex: BCrypt) antes de salvar.
            // A senha pura NUNCA pode ir para o banco.
            //
            //
            // ==========================================================

            try
            {
                // ==========================================================
                // BANCO DE DADOS: salvar o novo usuario (com o hash da senha)
                // e guardar o Id gerado em "novo.Id"
                //
                //
                // ==========================================================
            }
            catch (Exception ex)
            {
                Mensagem(txtMsgCadastro, "Erro ao acessar o banco: " + ex.Message, true);
                return;
            }

            usuarios.Add(novo);
            RegistrarAuditoria("Cadastro de usuário", novo.Usuario, "—", $"{tipo} / {status}");

            LimparCadastro();
            Mensagem(txtMsgCadastro, "Usuário cadastrado.", false);
            AtualizarLista();
        }

        private void LimparCadastro()
        {
            txtNomeCad.Text = "";
            txtUsuarioCad.Text = "";
            txtEmailCad.Text = "";
            pwdSenhaCad.Password = "";
            pwdConfirmaCad.Password = "";
            listaAvatares.SelectedItem = null;
        }

        #endregion

        #region Auditoria

        // Guarda uma linha nova na auditoria (NUNCA coloque senha aqui)
        private void RegistrarAuditoria(string operacao, string registro, string anterior, string novo, string categoria = CatAlteracao)
        {
            RegistroAuditoria r = new RegistroAuditoria
            {
                DataHora = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                Responsavel = usuarioLogado,
                Operacao = operacao,
                Registro = registro,
                ValorAnterior = anterior,
                NovoValor = novo,
                Categoria = categoria
            };

            // ==========================================================
            // BANCO DE DADOS: salvar este registro de auditoria no banco
            //
            //
            // ==========================================================

            registros.Insert(0, r); // insere no topo
        }

        // Mostra na tabela os registros (respeitando o filtro)
        private void AtualizarAuditoria()
        {
            string categoria = LerCombo(cmbCategoria);

            dgAuditoria.ItemsSource = null;
            dgAuditoria.ItemsSource = registros
                .Where(r => categoria == "Todos" || r.Categoria == categoria)
                .ToList();
        }

        // Mudou o filtro da auditoria
        private void FiltrarAuditoria(object sender, RoutedEventArgs e)
        {
            if (!pronto) return;

            AtualizarAuditoria();
        }

        #endregion

        #region Redefinir senha

        private void RedefinirSenha_Click(object sender, RoutedEventArgs e)
        {
            string alvo; // de quem e a senha que vai mudar

            if (souAdmin)
            {
                UsuarioCard escolhido = cmbUsuarioAlvo.SelectedItem as UsuarioCard;

                if (escolhido == null)
                {
                    Mensagem(txtMsgSenha, "Selecione o usuário.", true);
                    return;
                }

                alvo = escolhido.Usuario;
            }
            else
            {
                if (pwdAtual.Password == "")
                {
                    Mensagem(txtMsgSenha, "O campo Senha atual é obrigatório.", true);
                    return;
                }

                // ==========================================================
                // BANCO DE DADOS: comparar a senha atual com o hash salvo no banco
                // (se estiver errada, mostrar erro e dar return)
                //
                // ==========================================================

                alvo = "@" + usuarioLogado;
            }

            string nova = pwdNova.Password;
            string erro = ValidarSenha(nova, pwdConfirmaNova.Password, "Nova senha", "Confirmação da nova senha");

            if (erro != null)
            {
                Mensagem(txtMsgSenha, erro, true);
                return;
            }

            // ==========================================================
            // SEGURANCA: transformar "nova" em hash antes de salvar
            //
            //
            // ==========================================================

            try
            {
                // ==========================================================
                // BANCO DE DADOS: atualizar o hash da senha do usuario "alvo"
                //
                //
                // ==========================================================
            }
            catch (Exception ex)
            {
                Mensagem(txtMsgSenha, "Erro ao acessar o banco: " + ex.Message, true);
                return;
            }

            // a senha nao vai para a auditoria
            RegistrarAuditoria("Redefinição de senha", alvo, "—", "—");

            pwdAtual.Password = "";
            pwdNova.Password = "";
            pwdConfirmaNova.Password = "";

            Mensagem(txtMsgSenha, "Senha redefinida.", false);
        }

        #endregion

        #region Ajudantes

        // Mensagem na tela (vermelha = erro, verde = sucesso)
        private void Mensagem(TextBlock caixa, string texto, bool erro)
        {
            caixa.Foreground = erro ? Brushes.LightPink : Brushes.LightGreen;
            caixa.Text = texto;
        }

        #endregion
    }
}