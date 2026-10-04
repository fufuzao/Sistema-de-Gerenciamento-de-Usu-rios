using System;
using System.Windows.Media.Imaging;

namespace Sistema_de_Gerenciamento_de_Usuários
{
    // =====================================================================
    //  MODELOS
    //  Classes simples que só guardam dados. Cada objeto Usuario é uma
    //  linha da tabela "usuarios"; cada EventoAuditoria é uma linha da
    //  tabela "auditoria". As telas mostram esses objetos nos cards e na
    //  tabela da auditoria através de {Binding NomeDaPropriedade}.
    // =====================================================================

    // Textos fixos usados no sistema inteiro (assim não erramos a digitação)
    public static class Constantes
    {
        public const string TipoAdministrador = "Administrador";
        public const string TipoComum = "Comum";

        public const string StatusAtivo = "Ativo";
        public const string StatusInativo = "Inativo";
        public const string StatusBloqueado = "Bloqueado";

        public const string CategoriaAlteracao = "Alteração";
        public const string CategoriaAutenticacao = "Autenticação";

        // Quantas senhas erradas seguidas bloqueiam a conta
        public const int MaximoTentativas = 3;

        // Tamanhos mínimos pedidos no documento
        public const int MinimoNomeUsuario = 3;
        public const int MinimoSenha = 8;
    }

    // Um usuário do sistema (uma linha da tabela "usuarios")
    public class Usuario
    {
        // ---------- dados que vêm do banco ----------
        public int Id { get; set; }
        public string NomeCompleto { get; set; }
        public string NomeUsuario { get; set; }          // guardado SEM o @ (ex.: maria)
        public string Email { get; set; }
        public string Tipo { get; set; }                 // "Administrador" ou "Comum"
        public string Status { get; set; }               // "Ativo", "Inativo" ou "Bloqueado"
        public string Avatar { get; set; }               // só o nome (ex.: "Avatar 01")
        public int TentativasInvalidas { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime DataAlteracao { get; set; }
        public DateTime? UltimoLogin { get; set; }       // o "?" deixa ficar vazio (null) = nunca logou

        // ---------- propriedades calculadas (só para mostrar na tela) ----------

        // Nome de usuário com o @ na frente (ex.: @maria)
        public string Arroba
        {
            get { return "@" + NomeUsuario; }
        }

        // Texto do card (igual à imagem): "Último login: 21/09/2026 18:45"
        public string TextoUltimoLogin
        {
            get
            {
                if (UltimoLogin == null)
                {
                    return "Nenhum acesso registrado";
                }

                return "Último login: " + UltimoLogin.Value.ToString("dd/MM/yyyy HH:mm");
            }
        }

        // Texto completo do bloco do usuário logado: "Último login: 21/09/2026 às 18:45"
        public string TextoUltimoLoginCompleto
        {
            get
            {
                if (UltimoLogin == null)
                {
                    return "Nenhum acesso registrado";
                }

                // as aspas simples em 'às' fazem o texto aparecer do jeito que está
                return "Último login: " + UltimoLogin.Value.ToString("dd/MM/yyyy 'às' HH:mm");
            }
        }

        // Nível de acesso, conforme o tipo
        public string NivelAcesso
        {
            get
            {
                if (Tipo == Constantes.TipoAdministrador)
                {
                    return "Acesso total";
                }

                return "Acesso restrito";
            }
        }

        // Texto usado nas listas de escolha (ex.: "Maria Silva (@maria)")
        public string TextoEscolha
        {
            get { return NomeCompleto + " (" + Arroba + ")"; }
        }

        // Datas formatadas para a tela de edição
        public string TextoDatas
        {
            get
            {
                return "Criado em " + DataCriacao.ToString("dd/MM/yyyy HH:mm") +
                       "  •  Alterado em " + DataAlteracao.ToString("dd/MM/yyyy HH:mm");
            }
        }

        // A imagem do avatar (procurada pelo nome na classe Avatares)
        public BitmapImage ImagemAvatar
        {
            get { return Avatares.Imagem(Avatar); }
        }

        public bool EhAdministrador
        {
            get { return Tipo == Constantes.TipoAdministrador; }
        }

        public bool EstaAtivo
        {
            get { return Status == Constantes.StatusAtivo; }
        }

        public bool EstaBloqueado
        {
            get { return Status == Constantes.StatusBloqueado; }
        }

        // Usado na listagem (Dashboard e MainWindow): true se este usuário
        // passa na busca e nos dois filtros escolhidos na tela.
        // busca: texto já em minúsculas ("" = sem busca)
        // perfil e status: "Todos" = não filtra
        public bool CombinaComFiltro(string busca, string perfil, string status)
        {
            // 1) busca por nome, usuário ou e-mail
            if (busca != "")
            {
                bool achouNoNome = NomeCompleto.ToLower().Contains(busca);
                bool achouNoUsuario = Arroba.ToLower().Contains(busca);
                bool achouNoEmail = Email.ToLower().Contains(busca);

                if (achouNoNome == false && achouNoUsuario == false && achouNoEmail == false)
                {
                    return false;
                }
            }

            // 2) filtro de perfil (tipo)
            if (perfil != "Todos" && Tipo != perfil)
            {
                return false;
            }

            // 3) filtro de status
            if (status != "Todos" && Status != status)
            {
                return false;
            }

            return true;
        }

        // Texto que aparece quando o objeto é mostrado direto numa lista
        public override string ToString()
        {
            return TextoEscolha;
        }

        // Cria uma cópia (usada para comparar "antes" e "depois" na edição)
        public Usuario Copiar()
        {
            Usuario copia = new Usuario();
            copia.Id = Id;
            copia.NomeCompleto = NomeCompleto;
            copia.NomeUsuario = NomeUsuario;
            copia.Email = Email;
            copia.Tipo = Tipo;
            copia.Status = Status;
            copia.Avatar = Avatar;
            copia.TentativasInvalidas = TentativasInvalidas;
            copia.DataCriacao = DataCriacao;
            copia.DataAlteracao = DataAlteracao;
            copia.UltimoLogin = UltimoLogin;
            return copia;
        }
    }

    // Uma linha da tabela "auditoria"
    public class EventoAuditoria
    {
        public int Id { get; set; }
        public DateTime DataHora { get; set; }
        public string Responsavel { get; set; }
        public string Operacao { get; set; }
        public string RegistroAfetado { get; set; }
        public string ValorAnterior { get; set; }
        public string NovoValor { get; set; }       // nos eventos de login guarda o resultado
        public string Categoria { get; set; }       // "Alteração" ou "Autenticação"

        // Data e hora em texto para a tabela da tela
        public string TextoDataHora
        {
            get { return DataHora.ToString("dd/MM/yyyy HH:mm:ss"); }
        }
    }
}
