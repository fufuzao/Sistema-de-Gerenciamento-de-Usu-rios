using System;

namespace Sistema_de_Gerenciamento_de_Usuários
{

    public static class Autenticacao
    {
        // Mensagem GENÉRICA: não conta se o erro foi no usuário ou na senha
        // (assim um invasor não descobre quais usuários existem)
        public const string MensagemGenerica = "Usuário ou senha inválidos.";

        // Faz o login.
        // Devolve "" (vazio) se deu certo, ou a mensagem de erro para mostrar.
        // Se deu certo, Sessao.UsuarioLogado fica preenchido.
        public static string Entrar(string nomeDigitado, string senha)
        {
            // limpa espaços e tira o @ caso a pessoa tenha digitado "@maria"
            string nomeUsuario = "";
            if (nomeDigitado != null)
            {
                nomeUsuario = nomeDigitado.Trim().TrimStart('@');
            }

            if (nomeUsuario == "" || senha == null || senha == "")
            {
                return "Informe o usuário e a senha.";
            }

            // ---------- 1) O usuário existe? ----------
            // BANCO DE DADOS: procura o usuário pelo nome
            Usuario usuario = BancoDeDados.BuscarUsuarioPorNomeUsuario(nomeUsuario);

            if (usuario == null)
            {
                // BANCO DE DADOS: registra a tentativa (com o nome que foi digitado)
                RegistrarEvento("@" + nomeUsuario, "Tentativa de login inválida", "Usuário não encontrado", "Falha");
                return MensagemGenerica;
            }

            // ---------- 2) A senha confere com o hash? ----------
            // BANCO DE DADOS: pega o hash guardado
            string hash = BancoDeDados.BuscarHashSenha(usuario.Id);
            bool senhaCorreta = ConferirSenha(senha, hash);

            if (senhaCorreta == false)
            {
                // Só conta tentativa (e bloqueia) se a conta estiver Ativa.
                // Conta inativa ou já bloqueada não precisa contar.
                if (usuario.EstaAtivo)
                {
                    // BANCO DE DADOS: soma +1 tentativa inválida
                    int tentativas = BancoDeDados.RegistrarTentativaInvalida(usuario.Id);

                    RegistrarEvento(usuario.Arroba, "Tentativa de login inválida",
                                    "Tentativa " + tentativas + " de " + Constantes.MaximoTentativas, "Falha");

                    if (tentativas >= Constantes.MaximoTentativas)
                    {
                        // BANCO DE DADOS: bloqueia a conta
                        BancoDeDados.BloquearPorTentativas(usuario.Id);
                        RegistrarEvento(usuario.Arroba, "Bloqueio por tentativas inválidas",
                                        Constantes.StatusAtivo, "Bloqueada");
                    }
                }
                else
                {
                    RegistrarEvento(usuario.Arroba, "Tentativa de login inválida",
                                    "Conta " + usuario.Status.ToLower(), "Falha");
                }

                return MensagemGenerica;
            }

            // A partir daqui a senha está CERTA. Só agora contamos o motivo real
            // de uma recusa (decisão do projeto: conta inativa/bloqueada tem mensagem própria).

            // ---------- 3) Está ativo? ----------
            if (usuario.Status == Constantes.StatusInativo)
            {
                RegistrarEvento(usuario.Arroba, "Login recusado: usuário inativo", Constantes.StatusInativo, "Falha");
                return "Este usuário está inativo. Procure um administrador.";
            }

            // ---------- 4) Está bloqueado? ----------
            if (usuario.Status == Constantes.StatusBloqueado)
            {
                RegistrarEvento(usuario.Arroba, "Login recusado: usuário bloqueado", Constantes.StatusBloqueado, "Bloqueada");
                return "Este usuário está bloqueado por excesso de tentativas. Procure um administrador.";
            }

            // ---------- Deu tudo certo ----------
            // BANCO DE DADOS: grava a data/hora do login e zera as tentativas
            BancoDeDados.RegistrarLoginComSucesso(usuario.Id);
            RegistrarEvento(usuario.Arroba, "Login com sucesso", "—", "Sucesso");

            // ---------- 5) Permissões ----------
            // BANCO DE DADOS: lê de novo para pegar o último login atualizado.
            // O TIPO lido do banco é o que decide as permissões e a tela que abre.
            Sessao.UsuarioLogado = BancoDeDados.BuscarUsuarioPorId(usuario.Id);

            return "";
        }

        // Compara a senha digitada com o hash do banco usando BCrypt.
        // Se o hash estiver estragado, o BCrypt lança erro: tratamos como senha errada.
        public static bool ConferirSenha(string senha, string hash)
        {
            if (hash == null || hash == "")
            {
                return false;
            }

            try
            {
                return BCrypt.Net.BCrypt.Verify(senha, hash);
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Gera o hash BCrypt de uma senha (a senha pura NUNCA vai para o banco)
        public static string GerarHash(string senha)
        {
            return BCrypt.Net.BCrypt.HashPassword(senha);
        }

        // BANCO DE DADOS: grava um evento de login na auditoria (categoria Autenticação).
        // Aqui o responsável e o registro afetado são o próprio usuário que tentou entrar.
        private static void RegistrarEvento(string usuario, string evento, string detalhe, string resultado)
        {
            BancoDeDados.RegistrarAuditoria(usuario, evento, usuario, detalhe, resultado, Constantes.CategoriaAutenticacao);
        }
    }
}
