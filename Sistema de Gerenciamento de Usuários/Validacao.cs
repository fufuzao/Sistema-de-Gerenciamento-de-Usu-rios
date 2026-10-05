namespace Sistema_de_Gerenciamento_de_Usuários
{
    public static class Validacao
    {
        public static string ValidarNomeCompleto(string nome)
        {
            if (nome == null || nome.Trim() == "")
            {
                return "O campo Nome completo é obrigatório.";
            }

            if (nome.Trim().Length > 120)
            {
                return "O nome completo pode ter no máximo 120 caracteres.";
            }

            return "";
        }

        public static string ValidarNomeUsuario(string nomeUsuario)
        {
            if (nomeUsuario == null || nomeUsuario.Trim() == "")
            {
                return "O campo Nome de usuário é obrigatório.";
            }

            string texto = nomeUsuario.Trim();

            if (texto.Length < Constantes.MinimoNomeUsuario)
            {
                return "O nome de usuário deve ter no mínimo " + Constantes.MinimoNomeUsuario + " caracteres.";
            }

            if (texto.Length > 50)
            {
                return "O nome de usuário pode ter no máximo 50 caracteres.";
            }

            // o @ é colocado pelo sistema na hora de mostrar; não pode digitar
            if (texto.IndexOf(' ') >= 0 || texto.IndexOf('@') >= 0)
            {
                return "O nome de usuário não pode ter espaços nem @.";
            }

            return "";
        }

        public static string ValidarEmail(string email)
        {
            if (email == null || email.Trim() == "")
            {
                return "O campo E-mail é obrigatório.";
            }

            if (EmailValido(email.Trim()) == false)
            {
                return "Informe um e-mail válido (exemplo: nome@empresa.com).";
            }

            if (email.Trim().Length > 120)
            {
                return "O e-mail pode ter no máximo 120 caracteres.";
            }

            return "";
        }

        // Confere o formato do e-mail SEM usar regex, só com IndexOf/LastIndexOf.
        // Regras: sem espaços, um único @ que não está no começo,
        // e pelo menos um ponto depois do @ (com algo entre eles e depois dele).
        public static bool EmailValido(string email)
        {
            // 1) não pode ter espaço
            if (email.IndexOf(' ') >= 0)
            {
                return false;
            }

            // 2) precisa ter um @, e só um
            int posicaoArroba = email.IndexOf('@');

            if (posicaoArroba <= 0)                   // -1 = não tem; 0 = está no começo
            {
                return false;
            }

            if (posicaoArroba != email.LastIndexOf('@'))  // tem mais de um @
            {
                return false;
            }

            // 3) precisa ter um ponto depois do @ (e não colado nele)
            int posicaoPonto = email.LastIndexOf('.');

            if (posicaoPonto < posicaoArroba + 2)
            {
                return false;
            }

            // 4) o ponto não pode ser o último caractere
            if (posicaoPonto == email.Length - 1)
            {
                return false;
            }

            return true;
        }

        // Usado no cadastro e nas telas de senha.
        // nomeCampoSenha / nomeCampoConfirmacao: o nome que aparece na mensagem
        public static string ValidarSenha(string senha, string confirmacao, string nomeCampoSenha, string nomeCampoConfirmacao)
        {
            if (senha == null || senha == "")
            {
                return "O campo " + nomeCampoSenha + " é obrigatório.";
            }

            if (senha.Length < Constantes.MinimoSenha)
            {
                return "A senha deve ter no mínimo " + Constantes.MinimoSenha + " caracteres.";
            }

            if (confirmacao == null || confirmacao == "")
            {
                return "O campo " + nomeCampoConfirmacao + " é obrigatório.";
            }

            if (senha != confirmacao)
            {
                return "A senha e a confirmação devem ser iguais.";
            }

            return "";
        }

        public static string ValidarAvatar(string avatar)
        {
            if (avatar == null || avatar == "" || Avatares.Existe(avatar) == false)
            {
                return "Selecione uma imagem de perfil.";
            }

            return "";
        }

        public static string ValidarTipo(string tipo)
        {
            if (tipo == Constantes.TipoAdministrador || tipo == Constantes.TipoComum)
            {
                return "";
            }

            return "Selecione o tipo de usuário.";
        }

        public static string ValidarStatus(string status)
        {
            if (status == Constantes.StatusAtivo || status == Constantes.StatusInativo || status == Constantes.StatusBloqueado)
            {
                return "";
            }

            return "Selecione o status.";
        }
    }
}
