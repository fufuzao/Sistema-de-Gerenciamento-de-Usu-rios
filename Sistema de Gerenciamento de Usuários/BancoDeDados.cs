using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Sistema_de_Gerenciamento_de_Usuários
{
  
    public static class BancoDeDados
    {
        // BANCO DE DADOS: endereço do MySQL (porta 3307, banco "login", usuário root sem senha)
        public const string ConnectionString = "Server=localhost;Port=3307;Database=login;Uid=root;Pwd=;";

        // Colunas lidas sempre que buscamos usuários (a senha_hash NÃO está aqui de propósito)
        private const string ColunasUsuario =
            "id, nome_completo, nome_usuario, email, tipo, status, avatar, " +
            "tentativas_invalidas, data_criacao, data_alteracao, ultimo_login";

        // =================================================================
        //  AJUDANTES (usados só aqui dentro)
        // =================================================================

        // Abre uma conexão nova com o MySQL
        private static MySqlConnection AbrirConexao()
        {
            MySqlConnection conexao = new MySqlConnection(ConnectionString);
            conexao.Open();
            return conexao;
        }

        // Transforma a linha atual do leitor (reader) em um objeto Usuario
        private static Usuario LerUsuario(MySqlDataReader leitor)
        {
            Usuario u = new Usuario();
            u.Id = Convert.ToInt32(leitor["id"]);
            u.NomeCompleto = leitor["nome_completo"].ToString();
            u.NomeUsuario = leitor["nome_usuario"].ToString();
            u.Email = leitor["email"].ToString();
            u.Tipo = leitor["tipo"].ToString();
            u.Status = leitor["status"].ToString();
            u.Avatar = leitor["avatar"].ToString();
            u.TentativasInvalidas = Convert.ToInt32(leitor["tentativas_invalidas"]);
            u.DataCriacao = Convert.ToDateTime(leitor["data_criacao"]);
            u.DataAlteracao = Convert.ToDateTime(leitor["data_alteracao"]);

            // ultimo_login pode estar vazio (NULL) no banco = nunca fez login
            if (leitor["ultimo_login"] == DBNull.Value)
            {
                u.UltimoLogin = null;
            }
            else
            {
                u.UltimoLogin = Convert.ToDateTime(leitor["ultimo_login"]);
            }

            return u;
        }

        // =================================================================
        //  CONSULTAS DE USUÁRIOS
        // =================================================================

        // BANCO DE DADOS: quantos usuários existem (0 = banco vazio -> primeiro cadastro)
        public static int ContarUsuarios()
        {
            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand("SELECT COUNT(*) FROM usuarios", conexao))
            {
                return Convert.ToInt32(comando.ExecuteScalar());
            }
        }

        // BANCO DE DADOS: todos os usuários, em ordem alfabética
        public static List<Usuario> ListarUsuarios()
        {
            List<Usuario> lista = new List<Usuario>();
            string sql = "SELECT " + ColunasUsuario + " FROM usuarios ORDER BY nome_completo";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            using (MySqlDataReader leitor = comando.ExecuteReader())
            {
                while (leitor.Read())
                {
                    lista.Add(LerUsuario(leitor));
                }
            }

            return lista;
        }

        // BANCO DE DADOS: busca pelo id (devolve null se não achar)
        public static Usuario BuscarUsuarioPorId(int id)
        {
            string sql = "SELECT " + ColunasUsuario + " FROM usuarios WHERE id = @id";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@id", id);

                using (MySqlDataReader leitor = comando.ExecuteReader())
                {
                    if (leitor.Read())
                    {
                        return LerUsuario(leitor);
                    }
                }
            }

            return null;
        }

        // BANCO DE DADOS: busca pelo nome de usuário, sem o @ (devolve null se não achar)
        public static Usuario BuscarUsuarioPorNomeUsuario(string nomeUsuario)
        {
            string sql = "SELECT " + ColunasUsuario + " FROM usuarios WHERE nome_usuario = @nomeUsuario";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@nomeUsuario", nomeUsuario);

                using (MySqlDataReader leitor = comando.ExecuteReader())
                {
                    if (leitor.Read())
                    {
                        return LerUsuario(leitor);
                    }
                }
            }

            return null;
        }

        // BANCO DE DADOS: devolve o hash da senha (só o login e a troca de senha usam)
        public static string BuscarHashSenha(int id)
        {
            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand("SELECT senha_hash FROM usuarios WHERE id = @id", conexao))
            {
                comando.Parameters.AddWithValue("@id", id);
                object resultado = comando.ExecuteScalar();

                if (resultado == null)
                {
                    return "";
                }

                return resultado.ToString();
            }
        }

        // BANCO DE DADOS: true se o nome de usuário já é de OUTRA pessoa.
        // idIgnorar = id de quem está sendo editado (no cadastro novo, use 0)
        public static bool NomeUsuarioEmUso(string nomeUsuario, int idIgnorar)
        {
            string sql = "SELECT COUNT(*) FROM usuarios WHERE nome_usuario = @nomeUsuario AND id <> @id";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@nomeUsuario", nomeUsuario);
                comando.Parameters.AddWithValue("@id", idIgnorar);
                return Convert.ToInt32(comando.ExecuteScalar()) > 0;
            }
        }

        // BANCO DE DADOS: true se o e-mail já é de OUTRA pessoa
        public static bool EmailEmUso(string email, int idIgnorar)
        {
            string sql = "SELECT COUNT(*) FROM usuarios WHERE email = @email AND id <> @id";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@email", email);
                comando.Parameters.AddWithValue("@id", idIgnorar);
                return Convert.ToInt32(comando.ExecuteScalar()) > 0;
            }
        }

        // BANCO DE DADOS: quantos administradores ATIVOS existem
        public static int ContarAdministradoresAtivos()
        {
            string sql = "SELECT COUNT(*) FROM usuarios WHERE tipo = @tipo AND status = @status";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@tipo", Constantes.TipoAdministrador);
                comando.Parameters.AddWithValue("@status", Constantes.StatusAtivo);
                return Convert.ToInt32(comando.ExecuteScalar());
            }
        }

        // =================================================================
        //  ALTERAÇÕES DE USUÁRIOS
        //  data_criacao e data_alteracao são preenchidas pelo PRÓPRIO banco
        //  (DEFAULT CURRENT_TIMESTAMP no cadastro e NOW() nas alterações).
        // =================================================================

        // BANCO DE DADOS: grava um usuário novo e devolve o id que o banco gerou
        public static int InserirUsuario(Usuario u, string senhaHash)
        {
            string sql = "INSERT INTO usuarios (nome_completo, nome_usuario, email, senha_hash, tipo, status, avatar) " +
                         "VALUES (@nome, @nomeUsuario, @email, @hash, @tipo, @status, @avatar)";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@nome", u.NomeCompleto);
                comando.Parameters.AddWithValue("@nomeUsuario", u.NomeUsuario);
                comando.Parameters.AddWithValue("@email", u.Email);
                comando.Parameters.AddWithValue("@hash", senhaHash);
                comando.Parameters.AddWithValue("@tipo", u.Tipo);
                comando.Parameters.AddWithValue("@status", u.Status);
                comando.Parameters.AddWithValue("@avatar", u.Avatar);
                comando.ExecuteNonQuery();

                return (int)comando.LastInsertedId;
            }
        }

        // BANCO DE DADOS: o administrador altera os dados (NUNCA a senha por aqui)
        public static void AtualizarUsuario(Usuario u)
        {
            string sql = "UPDATE usuarios SET nome_completo = @nome, nome_usuario = @nomeUsuario, email = @email, " +
                         "tipo = @tipo, status = @status, avatar = @avatar, data_alteracao = NOW() " +
                         "WHERE id = @id";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@nome", u.NomeCompleto);
                comando.Parameters.AddWithValue("@nomeUsuario", u.NomeUsuario);
                comando.Parameters.AddWithValue("@email", u.Email);
                comando.Parameters.AddWithValue("@tipo", u.Tipo);
                comando.Parameters.AddWithValue("@status", u.Status);
                comando.Parameters.AddWithValue("@avatar", u.Avatar);
                comando.Parameters.AddWithValue("@id", u.Id);
                comando.ExecuteNonQuery();
            }
        }

        // BANCO DE DADOS: o próprio usuário altera o perfil.
        // Repare que tipo, status e nome_usuario NEM aparecem aqui: não tem como mudar.
        public static void AtualizarPerfilProprio(int id, string nomeCompleto, string email, string avatar)
        {
            string sql = "UPDATE usuarios SET nome_completo = @nome, email = @email, avatar = @avatar, " +
                         "data_alteracao = NOW() WHERE id = @id";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@nome", nomeCompleto);
                comando.Parameters.AddWithValue("@email", email);
                comando.Parameters.AddWithValue("@avatar", avatar);
                comando.Parameters.AddWithValue("@id", id);
                comando.ExecuteNonQuery();
            }
        }

        // BANCO DE DADOS: grava o NOVO HASH da senha (a senha pura nunca chega aqui)
        public static void AtualizarSenha(int id, string senhaHash)
        {
            string sql = "UPDATE usuarios SET senha_hash = @hash, data_alteracao = NOW() WHERE id = @id";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@hash", senhaHash);
                comando.Parameters.AddWithValue("@id", id);
                comando.ExecuteNonQuery();
            }
        }

        // BANCO DE DADOS: apaga o usuário
        public static void ExcluirUsuario(int id)
        {
            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand("DELETE FROM usuarios WHERE id = @id", conexao))
            {
                comando.Parameters.AddWithValue("@id", id);
                comando.ExecuteNonQuery();
            }
        }

        // BANCO DE DADOS: o administrador desbloqueia: volta para Ativo e zera as tentativas
        public static void DesbloquearUsuario(int id)
        {
            string sql = "UPDATE usuarios SET status = @status, tentativas_invalidas = 0, data_alteracao = NOW() WHERE id = @id";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@status", Constantes.StatusAtivo);
                comando.Parameters.AddWithValue("@id", id);
                comando.ExecuteNonQuery();
            }
        }

        // =================================================================
        //  LOGIN
        //  Estes UPDATEs NÃO mexem em data_alteracao, porque fazer login
        //  não é "alterar o cadastro". Por isso não tem NOW() nela aqui.
        // =================================================================

        // BANCO DE DADOS: soma 1 tentativa inválida e devolve quantas existem agora
        public static int RegistrarTentativaInvalida(int id)
        {
            using (MySqlConnection conexao = AbrirConexao())
            {
                using (MySqlCommand comando = new MySqlCommand(
                    "UPDATE usuarios SET tentativas_invalidas = tentativas_invalidas + 1 WHERE id = @id", conexao))
                {
                    comando.Parameters.AddWithValue("@id", id);
                    comando.ExecuteNonQuery();
                }

                using (MySqlCommand comando = new MySqlCommand(
                    "SELECT tentativas_invalidas FROM usuarios WHERE id = @id", conexao))
                {
                    comando.Parameters.AddWithValue("@id", id);
                    return Convert.ToInt32(comando.ExecuteScalar());
                }
            }
        }

        // BANCO DE DADOS: bloqueia a conta por excesso de tentativas
        public static void BloquearPorTentativas(int id)
        {
            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand("UPDATE usuarios SET status = @status WHERE id = @id", conexao))
            {
                comando.Parameters.AddWithValue("@status", Constantes.StatusBloqueado);
                comando.Parameters.AddWithValue("@id", id);
                comando.ExecuteNonQuery();
            }
        }

        // BANCO DE DADOS: login certo -> grava a data/hora do acesso e zera as tentativas
        public static void RegistrarLoginComSucesso(int id)
        {
            string sql = "UPDATE usuarios SET ultimo_login = NOW(), tentativas_invalidas = 0 WHERE id = @id";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@id", id);
                comando.ExecuteNonQuery();
            }
        }

        // BANCO DE DADOS: zera as tentativas (usado quando o admin muda Bloqueado -> Ativo na edição)
        public static void ZerarTentativas(int id)
        {
            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand("UPDATE usuarios SET tentativas_invalidas = 0 WHERE id = @id", conexao))
            {
                comando.Parameters.AddWithValue("@id", id);
                comando.ExecuteNonQuery();
            }
        }

        // =================================================================
        //  AUDITORIA
        // =================================================================

        // BANCO DE DADOS: grava uma linha na auditoria.
        // A data/hora quem coloca é o banco (DEFAULT CURRENT_TIMESTAMP).
        // NUNCA mande senha (nem hash) para cá.
        public static void RegistrarAuditoria(string responsavel, string operacao, string registroAfetado,
                                              string valorAnterior, string novoValor, string categoria)
        {
            string sql = "INSERT INTO auditoria (responsavel, operacao, registro_afetado, valor_anterior, novo_valor, categoria) " +
                         "VALUES (@responsavel, @operacao, @registro, @anterior, @novo, @categoria)";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                comando.Parameters.AddWithValue("@responsavel", Cortar(responsavel, 60));
                comando.Parameters.AddWithValue("@operacao", Cortar(operacao, 80));
                comando.Parameters.AddWithValue("@registro", Cortar(registroAfetado, 60));
                comando.Parameters.AddWithValue("@anterior", Cortar(valorAnterior, 255));
                comando.Parameters.AddWithValue("@novo", Cortar(novoValor, 255));
                comando.Parameters.AddWithValue("@categoria", categoria);
                comando.ExecuteNonQuery();
            }
        }

        // BANCO DE DADOS: lista a auditoria (os 500 mais recentes primeiro).
        // categoria = "Todos", "Alteração" ou "Autenticação"
        public static List<EventoAuditoria> ListarAuditoria(string categoria)
        {
            List<EventoAuditoria> lista = new List<EventoAuditoria>();

            string sql = "SELECT id, data_hora, responsavel, operacao, registro_afetado, valor_anterior, novo_valor, categoria " +
                         "FROM auditoria ";

            if (categoria != "Todos")
            {
                sql = sql + "WHERE categoria = @categoria ";
            }

            sql = sql + "ORDER BY data_hora DESC, id DESC LIMIT 500";

            using (MySqlConnection conexao = AbrirConexao())
            using (MySqlCommand comando = new MySqlCommand(sql, conexao))
            {
                if (categoria != "Todos")
                {
                    comando.Parameters.AddWithValue("@categoria", categoria);
                }

                using (MySqlDataReader leitor = comando.ExecuteReader())
                {
                    while (leitor.Read())
                    {
                        EventoAuditoria evento = new EventoAuditoria();
                        evento.Id = Convert.ToInt32(leitor["id"]);
                        evento.DataHora = Convert.ToDateTime(leitor["data_hora"]);
                        evento.Responsavel = leitor["responsavel"].ToString();
                        evento.Operacao = leitor["operacao"].ToString();
                        evento.RegistroAfetado = leitor["registro_afetado"].ToString();
                        evento.ValorAnterior = leitor["valor_anterior"].ToString();   // NULL vira ""
                        evento.NovoValor = leitor["novo_valor"].ToString();
                        evento.Categoria = leitor["categoria"].ToString();
                        lista.Add(evento);
                    }
                }
            }

            return lista;
        }

        // Corta um texto que passou do tamanho da coluna (para o INSERT não dar erro)
        private static string Cortar(string texto, int maximo)
        {
            if (texto == null)
            {
                return "";
            }

            if (texto.Length > maximo)
            {
                return texto.Substring(0, maximo);
            }

            return texto;
        }
    }
}
