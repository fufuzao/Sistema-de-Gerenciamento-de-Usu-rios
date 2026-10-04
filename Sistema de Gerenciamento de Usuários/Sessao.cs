namespace Sistema_de_Gerenciamento_de_Usuários
{
    // =====================================================================
    //  SESSÃO
    //  Guarda QUEM está logado enquanto o programa estiver aberto.
    //  "static" quer dizer que existe uma só, compartilhada por todas as
    //  janelas: o login preenche, o Dashboard e a MainWindow leem.
    // =====================================================================
    public static class Sessao
    {
        // O usuário que fez login (null = ninguém logado)
        public static Usuario UsuarioLogado { get; set; }

        // True se tem alguém logado
        public static bool EstaLogado()
        {
            if (UsuarioLogado == null)
            {
                return false;
            }

            return true;
        }

        // True se quem está logado é administrador
        public static bool EhAdministrador()
        {
            if (UsuarioLogado == null)
            {
                return false;
            }

            return UsuarioLogado.EhAdministrador;
        }

        // Texto usado na auditoria como "responsável" (ex.: @maria)
        public static string Responsavel()
        {
            if (UsuarioLogado == null)
            {
                return "(sem sessão)";
            }

            return UsuarioLogado.Arroba;
        }

        // Sair: esquece quem estava logado
        public static void Encerrar()
        {
            UsuarioLogado = null;
        }
    }
}
