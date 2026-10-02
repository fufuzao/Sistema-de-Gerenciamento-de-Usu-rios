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
    public class UsuarioCard
    {
        public int Id { get; set; }                     // BANCO DE DADOS: id do usuario na tabela
        public string Nome { get; set; }
        public string Usuario { get; set; }             // sempre com @ na frente
        public string Email { get; set; }
        public string Tipo { get; set; }                // "Administrador" ou "Comum"
        public string Status { get; set; }              // "Ativo", "Inativo" ou "Bloqueado"
        public string AvatarPath { get; set; }

        // BANCO DE DADOS: estas tres datas vem do banco
        public DateTime? DataCriacao { get; set; }
        public DateTime? DataAlteracao { get; set; }
        public DateTime? DataUltimoLogin { get; set; }  // vazio = nunca fez login

        public bool PodeEditar { get; set; }            // true = mostra os botoes Editar/Excluir
        public bool EstaBloqueado => Status == "Bloqueado";

        // texto que aparece no card
        public string UltimoLogin
        {
            get
            {
                if (DataUltimoLogin.HasValue)
                {
                    return "Último login: " + DataUltimoLogin.Value.ToString("dd/MM/yyyy 'às' HH:mm");
                }

                return "Nenhum acesso registrado";
            }
        }
    }

    // Dados de cada linha da auditoria
    public class RegistroAuditoria
    {
        public string DataHora { get; set; }            // BANCO DE DADOS: vem do banco (ja formatada em texto)
        public string Responsavel { get; set; }
        public string Operacao { get; set; }
        public string Registro { get; set; }
        public string ValorAnterior { get; set; }
        public string NovoValor { get; set; }           // nos eventos de login guarda o resultado (Sucesso/Falha)
        public string Categoria { get; set; }           // "Alteração" ou "Autenticação"
    }

    /// <summary>
    /// Lógica interna para Dashboard.xaml
    /// </summary>
    public partial class Dashboard : Window
    {
        // BANCO DE DADOS: coloque aqui a string de conexao
        private string connectionString = "Server=localhost;Port=3307;Database=login;Uid=root;Pwd=;";

        private const string Admin = "Administrador";
        private const string Comum = "Comum";
        private const string Ativo = "Ativo";
        private const string Bloqueado = "Bloqueado";

        private const string CatAlteracao = "Alteração";
        private const string CatAutenticacao = "Autenticação";

        private const int PorPagina = 4;
        private const int MinUsuario = 3;
        private const int MinSenha = 8;

        private const string PadraoEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

     
        // BANCO DE DADOS: pegar essas informacoes da tela de login
        private bool souAdmin = true;
        private string usuarioLogado = "maria";   // sem o @

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

            AplicarPermissoes();
            CarregarAvatares();
            CarregarUsuarios();
            CarregarAuditoria();
            MostrarUsuarioLogado();

            pronto = true;
            AtualizarLista();
        }

        // =====================================================
        //  CARREGAMENTO INICIAL
        // =====================================================

        // Esconde (Collapsed) o que o usuario comum nao pode usar
        private void AplicarPermissoes()
        {
            Visibility soAdmin = souAdmin ? Visibility.Visible : Visibility.Collapsed;

            cadastrousuario.Visibility = soAdmin;
            auditoria.Visibility = soAdmin;

            // Na tela de senha: admin escolhe o usuario, comum digita a senha atual
            painelUsuarioAlvo.Visibility = soAdmin; //um if 
            painelSenhaAtual.Visibility = souAdmin ? Visibility.Collapsed : Visibility.Visible; // um else
        }

        // Define se os botoes do card aparecem (so admin)
        private void AplicarPermissaoNoCard(UsuarioCard u)
        {
            u.PodeEditar = souAdmin;
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
            usuarios.Clear();

            // ==========================================================
            // BANCO DE DADOS: buscar todos os usuarios e colocar na lista "usuarios"
            // (Id, Nome, Usuario com @, Email, Tipo, Status, AvatarPath e as 3 datas)
            // Para cada usuario carregado, chame: AplicarPermissaoNoCard(u);
            //
            //
            // ==========================================================
        }

        private void CarregarAuditoria()
        {
            registros.Clear();

            // ==========================================================
            // BANCO DE DADOS: buscar os registros de auditoria e colocar na lista "registros"
            // (inclui os eventos de login: sucesso, tentativa invalida e bloqueio)
            //
            //
            // ==========================================================
        }

        // Mostra na barra lateral o nome e a imagem de quem esta logado
        private void MostrarUsuarioLogado()
        {
            UsuarioCard logado = usuarios.FirstOrDefault(u => u.Usuario == "@" + usuarioLogado);

            if (logado == null)
            {
                // ainda nao veio do banco: mostra so o que ja sabemos
                nomeusuariotelainferior.Text = usuarioLogado;
                nomeusuariotelainferior.ToolTip = "@" + usuarioLogado + " - " + (souAdmin ? Admin : Comum);
                return;
            }

            nomeusuariotelainferior.Text = logado.Nome;

            // passar o mouse mostra o resto das informacoes do usuario logado
            nomeusuariotelainferior.ToolTip = logado.Usuario + " - " + logado.Tipo + "\n" + logado.UltimoLogin;

            imagem_usuario_.ImageSource = CarregarImagem(logado.AvatarPath);
        }

        // Cria a imagem a partir do caminho (se o arquivo nao existir, devolve null e nao quebra)
        private BitmapImage CarregarImagem(string caminho)
        {
            try
            {
                return new BitmapImage(new Uri(caminho, UriKind.Absolute));
            }
            catch
            {
                return null;
            }
        }

        // =====================================================
        //  TROCA DE TELAS
        // =====================================================

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

            // BANCO DE DADOS: buscar os registros de auditoria atualizados (CarregarAuditoria)

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
            teladelogin Vteladelogin = new teladelogin();
            Vteladelogin.Show();
            this.Close();
        }

        private void SairNao_Click(object sender, RoutedEventArgs e)
        {
            MostrarTela(gridLista);
        }

       
        private void AplicarFiltro(object sender, RoutedEventArgs e)
        {
            if (!pronto) return; // a tela ainda esta carregando

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
            string busca = txtBusca.Text.Trim().ToLower();  //texto digitado na caixa de busca
            string perfil = LerCombo(cmbPerfil);            //atalhos da combobox
            string status = LerCombo(cmbStatus);            //atalhos da combobox

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

            // Math.Ceiling arredonda para cima;
            // (double) transforma em decimal para a divisao nao arredondar sozinha;
            // PorPagina = numero fixo de usuarios por pagina; Math.Max garante que seja pelo menos 1;
            totalPaginas = Math.Max(1, (int)Math.Ceiling(filtrados.Count / (double)PorPagina));

            // garante que a pagina atual existe;
            paginaAtual = Math.Min(Math.Max(paginaAtual, 1), totalPaginas);

            // pega so os usuarios da pagina atual;
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
            {// cria um botao para cada pagina
                Button b = new Button();
                b.Content = i.ToString();     //texto do botao
                b.Tag = i;                    //numero da pagina (usado no clique)
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

            // if ternario: condicao ? valor se verdadeiro : valor se falso
            btnAnterior.Visibility = paginaAtual > 1 ? Visibility.Visible : Visibility.Collapsed;
            btnProxima.Visibility = paginaAtual < totalPaginas ? Visibility.Visible : Visibility.Collapsed;
        }

        //9
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

        // =====================================================
        //  BOTOES DENTRO DOS CARDS
        // =====================================================

        // Pega o usuario do card onde o botao foi clicado
        private UsuarioCard UsuarioDoBotao(object sender)
        {
            return (UsuarioCard)((FrameworkElement)sender).DataContext;
        }

        // Quantos administradores ATIVOS existem
        private int ContarAdminsAtivos()
        {
            return usuarios.Count(x => x.Tipo == Admin && x.Status == Ativo);
        }

        // True se "u" e o unico administrador ativo (nao pode ser excluido)
        private bool EhUltimoAdmin(UsuarioCard u)
        {
            return u.Tipo == Admin && u.Status == Ativo && ContarAdminsAtivos() == 1;
        }

        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            if (!souAdmin) return;

            UsuarioCard u = UsuarioDoBotao(sender);

            // Ainda nao tem tela de edicao. Quando tiver, abra ela aqui.
            //
            //
            //
            //
            //
            MessageBox.Show("Editar: " + u.Nome);
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

            try
            {
                // ==========================================================
                // BANCO DE DADOS: mudar o status para Ativo e zerar o contador
                // de tentativas de login invalidas (use u.Id)
                //
                // ==========================================================
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao acessar o banco: " + ex.Message);
                return;
            }

            u.Status = Ativo;
            RegistrarAuditoria("Desbloqueio de usuário", u.Usuario, Bloqueado, Ativo);
            AtualizarLista();
        }

        // =====================================================
        //  CADASTRO DE USUARIO
        // =====================================================

        // Devolve a mensagem do primeiro erro encontrado, ou null se estiver tudo certo
        private string ValidarCadastro(string nome, string usuario, string email, string senha, string confirma)
        {
            if (nome == "") return "O campo Nome completo é obrigatório.";

            if (usuario == "") return "O campo Nome de usuário é obrigatório.";
            if (usuario.Length < MinUsuario) return $"O nome de usuário deve possuir no mínimo {MinUsuario} caracteres.";

            if (email == "") return "O campo E-mail é obrigatório.";
            if (!Regex.IsMatch(email, PadraoEmail)) return "Informe um e-mail em formato válido.";

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
                AvatarPath = listaAvatares.SelectedItem.ToString()
                // BANCO DE DADOS: DataCriacao, DataAlteracao e DataUltimoLogin vem do banco
            };

            AplicarPermissaoNoCard(novo);

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

        // =====================================================
        //  AUDITORIA
        // =====================================================

        // Guarda uma linha nova na auditoria (NUNCA coloque senha aqui)
        private void RegistrarAuditoria(string operacao, string registro, string anterior, string novo, string categoria = CatAlteracao)
        {
            RegistroAuditoria r = new RegistroAuditoria
            {
                DataHora = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),   // BANCO DE DADOS: a data e hora vem do banco (apague esta linha quando ligar)
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

        // =====================================================
        //  REDEFINIR SENHA
        // =====================================================

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

        // =====================================================
        //  AJUDANTES
        // =====================================================

        // Mensagem na tela (vermelha = erro, verde = sucesso)
        private void Mensagem(TextBlock caixa, string texto, bool erro)
        {
            caixa.Foreground = erro ? Brushes.LightPink : Brushes.LightGreen;
            caixa.Text = texto;
        }
    }
}
