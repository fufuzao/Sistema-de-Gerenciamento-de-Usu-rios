using System.Collections.Generic;

namespace Sistema_de_Gerenciamento_de_Usuários
{
    public static class Operacoes
    {
        private const string SemPermissao = "Você não tem permissão para fazer esta operação.";

    

        // BANCO DE DADOS: lê de novo, no banco, quem está logado.
        // Assim, se outro administrador mudou o tipo ou o status dele,
        // a permissão já vale na hora. Devolve null se não puder continuar.
        private static Usuario LogadoAtualizado()
        {
            if (Sessao.UsuarioLogado == null)
            {
                return null;
            }

            Usuario atual = BancoDeDados.BuscarUsuarioPorId(Sessao.UsuarioLogado.Id);

            // foi excluído ou não está mais ativo: não pode fazer nada
            if (atual == null || atual.EstaAtivo == false)
            {
                return null;
            }

            Sessao.UsuarioLogado = atual;   // mantém a sessão em dia
            return atual;
        }

        // Devolve o administrador logado, ou null se quem está logado NÃO é administrador ativo
        private static Usuario AdministradorLogado()
        {
            Usuario logado = LogadoAtualizado();

            if (logado == null || logado.EhAdministrador == false)
            {
                return null;
            }

            return logado;
        }

        // True se a sessão ainda vale (o usuário existe e está ativo).
        // As telas chamam isto ao abrir e depois de uma alteração em si mesmo.
        public static bool SessaoContinuaValida()
        {
            return LogadoAtualizado() != null;
        }

        // True se quem está logado é administrador ATIVO (conferido no banco)
        public static bool PodeAdministrar()
        {
            return AdministradorLogado() != null;
        }

        // =================================================================
        //  CONSULTA DA AUDITORIA (só administrador)
        //  Devolve null se quem está logado não tem permissão.
        // =================================================================
        public static List<EventoAuditoria> ConsultarAuditoria(string categoria)
        {
            if (AdministradorLogado() == null)
            {
                return null;
            }

            // BANCO DE DADOS: lista os eventos da categoria escolhida
            return BancoDeDados.ListarAuditoria(categoria);
        }

        // =================================================================
        //  PRIMEIRO CADASTRO (só quando a tabela está vazia)
        // =================================================================
        public static string CriarPrimeiroAdministrador(Usuario novo, string senha, string confirmacao)
        {
            // "permissão" aqui é: a tabela precisa estar vazia
            // BANCO DE DADOS: conta os usuários
            if (BancoDeDados.ContarUsuarios() > 0)
            {
                return "O primeiro administrador já foi criado. Faça o login.";
            }

            // o primeiro usuário é SEMPRE Administrador e Ativo
            novo.Tipo = Constantes.TipoAdministrador;
            novo.Status = Constantes.StatusAtivo;

            string erro = ValidarDadosCadastro(novo, senha, confirmacao);
            if (erro != "")
            {
                return erro;
            }

            // BANCO DE DADOS: grava com a senha em HASH
            novo.Id = BancoDeDados.InserirUsuario(novo, Autenticacao.GerarHash(senha));

            // BANCO DE DADOS: auditoria (o responsável é o próprio novo administrador)
            BancoDeDados.RegistrarAuditoria(novo.Arroba, "Cadastro de usuário", novo.Arroba, "—",
                                            ResumoCadastro(novo) + " (primeiro acesso)", Constantes.CategoriaAlteracao);
            return "";
        }

        // =================================================================
        //  CADASTRO (só administrador)
        // =================================================================
        public static string CadastrarUsuario(Usuario novo, string senha, string confirmacao)
        {
            Usuario admin = AdministradorLogado();
            if (admin == null)
            {
                return SemPermissao;
            }

            string erro = ValidarDadosCadastro(novo, senha, confirmacao);
            if (erro != "")
            {
                return erro;
            }

            // BANCO DE DADOS: grava com a senha em HASH (BCrypt)
            novo.Id = BancoDeDados.InserirUsuario(novo, Autenticacao.GerarHash(senha));

            // BANCO DE DADOS: auditoria (sem a senha!)
            BancoDeDados.RegistrarAuditoria(admin.Arroba, "Cadastro de usuário", novo.Arroba, "—",
                                            ResumoCadastro(novo), Constantes.CategoriaAlteracao);
            return "";
        }

        // Validações comuns do cadastro e do primeiro cadastro (para no primeiro erro)
        private static string ValidarDadosCadastro(Usuario novo, string senha, string confirmacao)
        {
            novo.NomeCompleto = Limpar(novo.NomeCompleto);
            novo.NomeUsuario = Limpar(novo.NomeUsuario);
            novo.Email = Limpar(novo.Email);

            string erro = Validacao.ValidarNomeCompleto(novo.NomeCompleto);

            if (erro == "")
            {
                erro = Validacao.ValidarNomeUsuario(novo.NomeUsuario);
            }

            if (erro == "")
            {
                erro = Validacao.ValidarEmail(novo.Email);
            }

            if (erro == "")
            {
                erro = Validacao.ValidarSenha(senha, confirmacao, "Senha", "Confirmação da senha");
            }

            if (erro == "")
            {
                erro = Validacao.ValidarTipo(novo.Tipo);
            }

            if (erro == "")
            {
                erro = Validacao.ValidarStatus(novo.Status);
            }

            if (erro == "")
            {
                erro = Validacao.ValidarAvatar(novo.Avatar);
            }

            // BANCO DE DADOS: não deixa repetir nome de usuário nem e-mail
            if (erro == "" && BancoDeDados.NomeUsuarioEmUso(novo.NomeUsuario, 0))
            {
                erro = "Este nome de usuário já está em uso.";
            }

            if (erro == "" && BancoDeDados.EmailEmUso(novo.Email, 0))
            {
                erro = "Este e-mail já está cadastrado.";
            }

            return erro;
        }

        // =================================================================
        //  EDIÇÃO (só administrador; pode editar qualquer um, inclusive ele mesmo)
        //  A senha NÃO é editada aqui (existe a tela "Redefinir senha").
        // =================================================================
        public static string EditarUsuario(Usuario alterado)
        {
            Usuario admin = AdministradorLogado();
            if (admin == null)
            {
                return SemPermissao;
            }

            // BANCO DE DADOS: como o usuário está AGORA no banco (o "antes")
            Usuario antes = BancoDeDados.BuscarUsuarioPorId(alterado.Id);
            if (antes == null)
            {
                return "Este usuário não existe mais.";
            }

            alterado.NomeCompleto = Limpar(alterado.NomeCompleto);
            alterado.NomeUsuario = Limpar(alterado.NomeUsuario);
            alterado.Email = Limpar(alterado.Email);

            // ----- validações -----
            string erro = Validacao.ValidarNomeCompleto(alterado.NomeCompleto);

            if (erro == "")
            {
                erro = Validacao.ValidarNomeUsuario(alterado.NomeUsuario);
            }

            if (erro == "")
            {
                erro = Validacao.ValidarEmail(alterado.Email);
            }

            if (erro == "")
            {
                erro = Validacao.ValidarTipo(alterado.Tipo);
            }

            if (erro == "")
            {
                erro = Validacao.ValidarStatus(alterado.Status);
            }

            if (erro == "")
            {
                erro = Validacao.ValidarAvatar(alterado.Avatar);
            }

            // BANCO DE DADOS: duplicados (ignorando o próprio usuário)
            if (erro == "" && BancoDeDados.NomeUsuarioEmUso(alterado.NomeUsuario, alterado.Id))
            {
                erro = "Este nome de usuário já está em uso.";
            }

            if (erro == "" && BancoDeDados.EmailEmUso(alterado.Email, alterado.Id))
            {
                erro = "Este e-mail já está cadastrado.";
            }

            if (erro != "")
            {
                return erro;
            }

            // ----- regra do último administrador ativo -----
            // Se ele ERA administrador ativo e vai DEIXAR de ser (virar Comum ou sair do Ativo)...
            bool eraAdminAtivo = antes.EhAdministrador && antes.EstaAtivo;
            bool continuaAdminAtivo = alterado.EhAdministrador && alterado.EstaAtivo;

            if (eraAdminAtivo && continuaAdminAtivo == false)
            {
                // BANCO DE DADOS: ...e ele é o único, não pode.
                if (BancoDeDados.ContarAdministradoresAtivos() <= 1)
                {
                    return "Não é possível: este é o último administrador ativo. " +
                           "O sistema precisa ter pelo menos um administrador ativo.";
                }
            }

            // ----- grava -----
            // BANCO DE DADOS: atualiza o cadastro
            BancoDeDados.AtualizarUsuario(alterado);

            // Se o administrador tirou o bloqueio pela edição, zera as tentativas também
            if (antes.Status == Constantes.StatusBloqueado && alterado.Status == Constantes.StatusAtivo)
            {
                // BANCO DE DADOS: zera o contador de senhas erradas
                BancoDeDados.ZerarTentativas(alterado.Id);
            }

            // ----- auditoria: UMA linha para cada campo que mudou -----
            string quem = admin.Arroba;
            string registro = alterado.Arroba;
            string cat = Constantes.CategoriaAlteracao;

            // BANCO DE DADOS: cada RegistrarAuditoria abaixo grava uma linha
            if (antes.NomeCompleto != alterado.NomeCompleto)
            {
                BancoDeDados.RegistrarAuditoria(quem, "Alteração de usuário", registro,
                                                "Nome: " + antes.NomeCompleto, "Nome: " + alterado.NomeCompleto, cat);
            }

            if (antes.NomeUsuario != alterado.NomeUsuario)
            {
                BancoDeDados.RegistrarAuditoria(quem, "Alteração de usuário", registro,
                                                "Usuário: " + antes.Arroba, "Usuário: " + alterado.Arroba, cat);
            }

            if (antes.Email != alterado.Email)
            {
                BancoDeDados.RegistrarAuditoria(quem, "Alteração de usuário", registro,
                                                "E-mail: " + antes.Email, "E-mail: " + alterado.Email, cat);
            }

            if (antes.Tipo != alterado.Tipo)
            {
                // o tipo define o perfil e as permissões (nível de acesso)
                BancoDeDados.RegistrarAuditoria(quem, "Alteração de perfil e permissões", registro,
                                                antes.Tipo + " (" + antes.NivelAcesso + ")",
                                                alterado.Tipo + " (" + alterado.NivelAcesso + ")", cat);
            }

            if (antes.Status != alterado.Status)
            {
                BancoDeDados.RegistrarAuditoria(quem, NomeOperacaoStatus(antes.Status, alterado.Status), registro,
                                                antes.Status, alterado.Status, cat);
            }

            if (antes.Avatar != alterado.Avatar)
            {
                BancoDeDados.RegistrarAuditoria(quem, "Alteração da imagem de perfil", registro,
                                                antes.Avatar, alterado.Avatar, cat);
            }

            return "";
        }

        // Nome da operação de auditoria quando o status muda
        private static string NomeOperacaoStatus(string statusAntes, string statusNovo)
        {
            if (statusNovo == Constantes.StatusAtivo)
            {
                if (statusAntes == Constantes.StatusBloqueado)
                {
                    return "Desbloqueio de usuário";
                }

                return "Ativação de usuário";
            }

            if (statusNovo == Constantes.StatusInativo)
            {
                return "Desativação de usuário";
            }

            return "Bloqueio de usuário";
        }

        // =================================================================
        //  EXCLUSÃO (só administrador)
        //  A pergunta "Deseja realmente excluir este usuário?" é feita na tela.
        // =================================================================
        public static string ExcluirUsuario(int id)
        {
            Usuario admin = AdministradorLogado();
            if (admin == null)
            {
                return SemPermissao;
            }

            // não pode excluir a própria conta que está logada
            if (id == admin.Id)
            {
                return "Você não pode excluir a sua própria conta enquanto está conectado.";
            }

            // BANCO DE DADOS: busca quem vai ser excluído
            Usuario alvo = BancoDeDados.BuscarUsuarioPorId(id);
            if (alvo == null)
            {
                return "Este usuário não existe mais.";
            }

            // BANCO DE DADOS: não pode excluir o último administrador ativo
            if (alvo.EhAdministrador && alvo.EstaAtivo && BancoDeDados.ContarAdministradoresAtivos() <= 1)
            {
                return "Não é possível excluir o último administrador ativo do sistema.";
            }

            // BANCO DE DADOS: exclui e registra
            BancoDeDados.ExcluirUsuario(id);
            BancoDeDados.RegistrarAuditoria(admin.Arroba, "Exclusão de usuário", alvo.Arroba,
                                            ResumoCadastro(alvo), "—", Constantes.CategoriaAlteracao);
            return "";
        }

        // =================================================================
        //  DESBLOQUEIO (só administrador)
        // =================================================================
        public static string DesbloquearUsuario(int id)
        {
            Usuario admin = AdministradorLogado();
            if (admin == null)
            {
                return SemPermissao;
            }

            // BANCO DE DADOS: busca o usuário
            Usuario alvo = BancoDeDados.BuscarUsuarioPorId(id);
            if (alvo == null)
            {
                return "Este usuário não existe mais.";
            }

            if (alvo.EstaBloqueado == false)
            {
                return "Este usuário não está bloqueado.";
            }

            // BANCO DE DADOS: volta para Ativo, zera as tentativas e registra
            BancoDeDados.DesbloquearUsuario(id);
            BancoDeDados.RegistrarAuditoria(admin.Arroba, "Desbloqueio de usuário", alvo.Arroba,
                                            Constantes.StatusBloqueado, Constantes.StatusAtivo, Constantes.CategoriaAlteracao);
            return "";
        }

        // =================================================================
        //  REDEFINIR A SENHA DE QUALQUER USUÁRIO (só administrador)
        // =================================================================
        public static string RedefinirSenha(int idAlvo, string novaSenha, string confirmacao)
        {
            Usuario admin = AdministradorLogado();
            if (admin == null)
            {
                return SemPermissao;
            }

            // BANCO DE DADOS: busca o usuário
            Usuario alvo = BancoDeDados.BuscarUsuarioPorId(idAlvo);
            if (alvo == null)
            {
                return "Selecione o usuário.";
            }

            string erro = Validacao.ValidarSenha(novaSenha, confirmacao, "Nova senha", "Confirmação da nova senha");
            if (erro != "")
            {
                return erro;
            }

            // BANCO DE DADOS: grava só o HASH; na auditoria NÃO vai a senha
            BancoDeDados.AtualizarSenha(alvo.Id, Autenticacao.GerarHash(novaSenha));
            BancoDeDados.RegistrarAuditoria(admin.Arroba, "Redefinição de senha", alvo.Arroba,
                                            "—", "Senha redefinida pelo administrador", Constantes.CategoriaAlteracao);
            return "";
        }

        // =================================================================
        //  TROCAR A PRÓPRIA SENHA (qualquer usuário logado)
        //  O id vem SEMPRE da sessão, nunca da tela.
        // =================================================================
        public static string AlterarMinhaSenha(string senhaAtual, string novaSenha, string confirmacao)
        {
            Usuario logado = LogadoAtualizado();
            if (logado == null)
            {
                return SemPermissao;
            }

            if (senhaAtual == null || senhaAtual == "")
            {
                return "O campo Senha atual é obrigatório.";
            }

            // BANCO DE DADOS: confere a senha atual com o hash guardado
            if (Autenticacao.ConferirSenha(senhaAtual, BancoDeDados.BuscarHashSenha(logado.Id)) == false)
            {
                return "A senha atual está incorreta.";
            }

            string erro = Validacao.ValidarSenha(novaSenha, confirmacao, "Nova senha", "Confirmação da nova senha");
            if (erro != "")
            {
                return erro;
            }

            if (novaSenha == senhaAtual)
            {
                return "A nova senha deve ser diferente da senha atual.";
            }

            // BANCO DE DADOS: grava o novo hash e registra (sem a senha)
            BancoDeDados.AtualizarSenha(logado.Id, Autenticacao.GerarHash(novaSenha));
            BancoDeDados.RegistrarAuditoria(logado.Arroba, "Redefinição de senha", logado.Arroba,
                                            "—", "Senha alterada pelo próprio usuário", Constantes.CategoriaAlteracao);
            return "";
        }

        // =================================================================
        //  EDITAR O PRÓPRIO PERFIL (usuário comum)
        //  Só nome, e-mail e avatar. NUNCA usuário, tipo ou status.
        //  O id vem SEMPRE da sessão, nunca da tela.
        // =================================================================
        public static string AlterarMeuPerfil(string nomeCompleto, string email, string avatar)
        {
            Usuario logado = LogadoAtualizado();
            if (logado == null)
            {
                return SemPermissao;
            }

            nomeCompleto = Limpar(nomeCompleto);
            email = Limpar(email);

            string erro = Validacao.ValidarNomeCompleto(nomeCompleto);

            if (erro == "")
            {
                erro = Validacao.ValidarEmail(email);
            }

            if (erro == "")
            {
                erro = Validacao.ValidarAvatar(avatar);
            }

            // BANCO DE DADOS: o e-mail não pode ser de outra pessoa
            if (erro == "" && BancoDeDados.EmailEmUso(email, logado.Id))
            {
                erro = "Este e-mail já está cadastrado.";
            }

            if (erro != "")
            {
                return erro;
            }

            if (nomeCompleto == logado.NomeCompleto && email == logado.Email && avatar == logado.Avatar)
            {
                return "Nenhuma alteração para salvar.";
            }

            // BANCO DE DADOS: grava só os 3 campos permitidos
            BancoDeDados.AtualizarPerfilProprio(logado.Id, nomeCompleto, email, avatar);

            // BANCO DE DADOS: auditoria, uma linha por campo alterado
            string eu = logado.Arroba;
            string cat = Constantes.CategoriaAlteracao;

            if (nomeCompleto != logado.NomeCompleto)
            {
                BancoDeDados.RegistrarAuditoria(eu, "Alteração de perfil", eu,
                                                "Nome: " + logado.NomeCompleto, "Nome: " + nomeCompleto, cat);
            }

            if (email != logado.Email)
            {
                BancoDeDados.RegistrarAuditoria(eu, "Alteração de perfil", eu,
                                                "E-mail: " + logado.Email, "E-mail: " + email, cat);
            }

            if (avatar != logado.Avatar)
            {
                BancoDeDados.RegistrarAuditoria(eu, "Alteração da imagem de perfil", eu, logado.Avatar, avatar, cat);
            }

            // BANCO DE DADOS: atualiza a sessão com os dados novos
            Sessao.UsuarioLogado = BancoDeDados.BuscarUsuarioPorId(logado.Id);
            return "";
        }

        // =================================================================
        //  AJUDANTES
        // =================================================================

        // Tira os espaços do começo e do fim (e troca null por "")
        private static string Limpar(string texto)
        {
            if (texto == null)
            {
                return "";
            }

            return texto.Trim();
        }

        // Resumo de um cadastro para a auditoria (SEM senha)
        private static string ResumoCadastro(Usuario u)
        {
            return u.NomeCompleto + " | " + u.Email + " | " + u.Tipo + " | " + u.Status + " | " + u.Avatar;
        }
    }
}
