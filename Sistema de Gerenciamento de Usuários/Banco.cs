using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Sistema_de_Gerenciamento_de_Usuários
{
    // Tudo que mexe no MySQL fica aqui. As telas so chamam estes metodos.
    // Todos os comandos usam parametros (@algo) para evitar SQL injection.
    public static class Banco
    {
        // string de conexao do MySQL
        public static string connectionString = "Server=localhost;Port=3307;Database=login;Uid=root;Pwd=;";

        private const string Colunas = "id, nome, usuario, email, tipo, status, avatar, tentativas_invalidas, data_criacao, data_alteracao, ultimo_login";

        // =====================================================
        //  AJUDANTES
        // =====================================================

        private static MySqlConnection Abrir()
        {
            MySqlConnection con = new MySqlConnection(connectionString);
            con.Open();
            return con;
        }

        // Executa INSERT, UPDATE ou DELETE.
        // Os parametros vao de dois em dois: nome, valor, nome, valor...
        private static void Executar(string sql, params object[] parametros)
        {
            using (MySqlConnection con = Abrir())
            using (MySqlCommand cmd = new MySqlCommand(sql, con))
            {
                for (int i = 0; i < parametros.Length; i += 2)
                {
                    cmd.Parameters.AddWithValue(parametros[i].ToString(), parametros[i + 1]);
                }

                cmd.ExecuteNonQuery();
            }
        }

        // Executa um SELECT que devolve um unico valor (ex: COUNT)
        private static object Consultar(string sql, params object[] parametros)
        {
            using (MySqlConnection con = Abrir())
            using (MySqlCommand cmd = new MySqlCommand(sql, con))
            {
                for (int i = 0; i < parametros.Length; i += 2)
                {
                    cmd.Parameters.AddWithValue(parametros[i].ToString(), parametros[i + 1]);
                }

                return cmd.ExecuteScalar();
            }
        }

        // Data do banco pode vir vazia (NULL)
        private static DateTime? LerData(object valor)
        {
            if (valor == DBNull.Value)
            {
                return null;
            }

            return Convert.ToDateTime(valor);
        }

        // Transforma uma linha do banco em um UsuarioCard
        private static UsuarioCard LerUsuario(MySqlDataReader dr)
        {
            UsuarioCard u = new UsuarioCard();
            u.Id = Convert.ToInt32(dr["id"]);
            u.Nome = dr["nome"].ToString();
            u.Usuario = "@" + dr["usuario"].ToString();
            u.Email = dr["email"].ToString();
            u.Tipo = dr["tipo"].ToString();
            u.Status = dr["status"].ToString();
            u.AvatarNome = dr["avatar"].ToString();
            u.Tentativas = Convert.ToInt32(dr["tentativas_invalidas"]);
            u.DataCriacao = LerData(dr["data_criacao"]);
            u.DataAlteracao = LerData(dr["data_alteracao"]);
            u.DataUltimoLogin = LerData(dr["ultimo_login"]);
            return u;
        }

        // Busca um usuario por uma condicao (devolve null se nao achar)
        private static UsuarioCard BuscarUm(string condicao, object valor)
        {
            using (MySqlConnection con = Abrir())
            using (MySqlCommand cmd = new MySqlCommand("SELECT " + Colunas + " FROM usuarios WHERE " + condicao, con))
            {
                cmd.Parameters.AddWithValue("@valor", valor);

                using (MySqlDataReader dr = cmd.ExecuteReader())
                {
                    if (dr.Read())
                    {
                        return LerUsuario(dr);
                    }
                }
            }

            return null;
        }

        // =====================================================
        //  USUARIOS
        // =====================================================

        // True se ja existe pelo menos um usuario cadastrado
        public static bool TemUsuarios()
        {
            object total = Consultar("SELECT COUNT(*) FROM usuarios");
            return Convert.ToInt32(total) > 0;
        }

        public static List<UsuarioCard> ListarUsuarios()
        {
            List<UsuarioCard> lista = new List<UsuarioCard>();

            using (MySqlConnection con = Abrir())
            using (MySqlCommand cmd = new MySqlCommand("SELECT " + Colunas + " FROM usuarios ORDER BY nome", con))
            using (MySqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    lista.Add(LerUsuario(dr));
                }
            }

            return lista;
        }

        // Procura pelo nome de usuario (com ou sem @)
        public static UsuarioCard BuscarPorUsuario(string usuario)
        {
            return BuscarUm("usuario = @valor", usuario.TrimStart('@'));
        }

        public static UsuarioCard BuscarPorId(int id)
        {
            return BuscarUm("id = @valor", id);
        }

        // Devolve o hash da senha (so a tela de login e a troca de senha usam)
        public static string ObterHash(int id)
        {
            object hash = Consultar("SELECT senha_hash FROM usuarios WHERE id = @id", "@id", id);

            if (hash == null)
            {
                return "";
            }

            return hash.ToString();
        }

        // True se o nome de usuario ja existe (ignorarId = o proprio usuario, em caso de edicao)
        public static bool UsuarioExiste(string usuario, int ignorarId)
        {
            object total = Consultar("SELECT COUNT(*) FROM usuarios WHERE usuario = @usuario AND id <> @id",
                                     "@usuario", usuario.TrimStart('@'), "@id", ignorarId);
            return Convert.ToInt32(total) > 0;
        }

        public static bool EmailExiste(string email, int ignorarId)
        {
            object total = Consultar("SELECT COUNT(*) FROM usuarios WHERE email = @email AND id <> @id",
                                     "@email", email, "@id", ignorarId);
            return Convert.ToInt32(total) > 0;
        }

        // Quantos administradores ativos existem
        public static int ContarAdminsAtivos()
        {
            object total = Consultar("SELECT COUNT(*) FROM usuarios WHERE tipo = 'Administrador' AND status = 'Ativo'");
            return Convert.ToInt32(total);
        }

        // Salva um usuario novo e guarda o Id gerado em u.Id
        public static void CriarUsuario(UsuarioCard u, string senhaHash)
        {
            string sql = "INSERT INTO usuarios (nome, usuario, email, senha_hash, tipo, status, avatar) " +
                         "VALUES (@nome, @usuario, @email, @hash, @tipo, @status, @avatar)";

            using (MySqlConnection con = Abrir())
            using (MySqlCommand cmd = new MySqlCommand(sql, con))
            {
                cmd.Parameters.AddWithValue("@nome", u.Nome);
                cmd.Parameters.AddWithValue("@usuario", u.Usuario.TrimStart('@'));
                cmd.Parameters.AddWithValue("@email", u.Email);
                cmd.Parameters.AddWithValue("@hash", senhaHash);
                cmd.Parameters.AddWithValue("@tipo", u.Tipo);
                cmd.Parameters.AddWithValue("@status", u.Status);
                cmd.Parameters.AddWithValue("@avatar", u.AvatarNome);

                cmd.ExecuteNonQuery();
                u.Id = (int)cmd.LastInsertedId;
            }
        }

        // O administrador altera tudo (menos a senha)
        public static void AtualizarUsuario(UsuarioCard u)
        {
            Executar("UPDATE usuarios SET nome = @nome, usuario = @usuario, email = @email, tipo = @tipo, status = @status, avatar = @avatar WHERE id = @id",
                     "@nome", u.Nome,
                     "@usuario", u.Usuario.TrimStart('@'),
                     "@email", u.Email,
                     "@tipo", u.Tipo,
                     "@status", u.Status,
                     "@avatar", u.AvatarNome,
                     "@id", u.Id);
        }

        // O usuario comum so altera nome, e-mail e avatar do proprio perfil
        // (o tipo e o status nem aparecem neste comando, entao nao tem como mudar)
        public static void AtualizarPerfilProprio(int id, string nome, string email, string avatar)
        {
            Executar("UPDATE usuarios SET nome = @nome, email = @email, avatar = @avatar WHERE id = @id",
                     "@nome", nome, "@email", email, "@avatar", avatar, "@id", id);
        }

        public static void AtualizarSenha(int id, string senhaHash)
        {
            Executar("UPDATE usuarios SET senha_hash = @hash WHERE id = @id", "@hash", senhaHash, "@id", id);
        }

        public static void ExcluirUsuario(int id)
        {
            Executar("DELETE FROM usuarios WHERE id = @id", "@id", id);
        }

        // O administrador desbloqueia: volta para Ativo e zera as tentativas
        public static void Desbloquear(int id)
        {
            Executar("UPDATE usuarios SET status = 'Ativo', tentativas_invalidas = 0 WHERE id = @id", "@id", id);
        }

        public static void ZerarTentativas(int id)
        {
            Executar("UPDATE usuarios SET tentativas_invalidas = 0, data_alteracao = data_alteracao WHERE id = @id", "@id", id);
        }

        // =====================================================
        //  LOGIN
        //  (o "data_alteracao = data_alteracao" serve para o login nao
        //   mudar a data da ultima alteracao do cadastro)
        // =====================================================

        // Soma uma tentativa invalida e devolve quantas ja tem
        public static int AdicionarTentativa(int id)
        {
            Executar("UPDATE usuarios SET tentativas_invalidas = tentativas_invalidas + 1, data_alteracao = data_alteracao WHERE id = @id", "@id", id);

            object total = Consultar("SELECT tentativas_invalidas FROM usuarios WHERE id = @id", "@id", id);
            return Convert.ToInt32(total);
        }

        public static void Bloquear(int id)
        {
            Executar("UPDATE usuarios SET status = 'Bloqueado', data_alteracao = data_alteracao WHERE id = @id", "@id", id);
        }

        // Login certo: grava a data e a hora do acesso e zera as tentativas
        public static void RegistrarLoginSucesso(int id)
        {
            Executar("UPDATE usuarios SET ultimo_login = NOW(), tentativas_invalidas = 0, data_alteracao = data_alteracao WHERE id = @id", "@id", id);
        }

        // =====================================================
        //  AUDITORIA
        // =====================================================

        // A data e a hora o proprio banco coloca. NUNCA mande senha para ca.
        public static void RegistrarAuditoria(string responsavel, string operacao, string registro, string anterior, string novo, string categoria)
        {
            Executar("INSERT INTO auditoria (responsavel, operacao, registro, valor_anterior, novo_valor, categoria) " +
                     "VALUES (@responsavel, @operacao, @registro, @anterior, @novo, @categoria)",
                     "@responsavel", responsavel,
                     "@operacao", operacao,
                     "@registro", registro,
                     "@anterior", anterior,
                     "@novo", novo,
                     "@categoria", categoria);
        }

        // Os 500 registros mais recentes
        public static List<RegistroAuditoria> ListarAuditoria()
        {
            List<RegistroAuditoria> lista = new List<RegistroAuditoria>();

            string sql = "SELECT data_hora, responsavel, operacao, registro, valor_anterior, novo_valor, categoria " +
                         "FROM auditoria ORDER BY data_hora DESC, id DESC LIMIT 500";

            using (MySqlConnection con = Abrir())
            using (MySqlCommand cmd = new MySqlCommand(sql, con))
            using (MySqlDataReader dr = cmd.ExecuteReader())
            {
                while (dr.Read())
                {
                    RegistroAuditoria r = new RegistroAuditoria();
                    r.DataHora = Convert.ToDateTime(dr["data_hora"]).ToString("dd/MM/yyyy HH:mm");
                    r.Responsavel = dr["responsavel"].ToString();
                    r.Operacao = dr["operacao"].ToString();
                    r.Registro = dr["registro"].ToString();
                    r.ValorAnterior = dr["valor_anterior"].ToString();
                    r.NovoValor = dr["novo_valor"].ToString();
                    r.Categoria = dr["categoria"].ToString();
                    lista.Add(r);
                }
            }

            return lista;
        }
    }
}
