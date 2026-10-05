CREATE DATABASE IF NOT EXISTS login
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;   -- "ci" = não diferencia maiúsculas de minúsculas (Maria = maria)

USE login;

DROP TABLE IF EXISTS auditoria;
DROP TABLE IF EXISTS usuarios;

-- ---------------------------------------------------------------------
--  TABELA DE USUÁRIOS
-- ---------------------------------------------------------------------
CREATE TABLE usuarios (
    id                   INT          NOT NULL AUTO_INCREMENT,
    nome_completo        VARCHAR(120) NOT NULL,
    nome_usuario         VARCHAR(50)  NOT NULL,                 -- guardado SEM o @ (ex.: maria)
    email                VARCHAR(120) NOT NULL,
    senha_hash           VARCHAR(100) NOT NULL,                 -- só o HASH do BCrypt, nunca a senha
    tipo                 VARCHAR(20)  NOT NULL DEFAULT 'Comum', -- 'Administrador' ou 'Comum'
    status               VARCHAR(20)  NOT NULL DEFAULT 'Ativo', -- 'Ativo', 'Inativo' ou 'Bloqueado'
    avatar               VARCHAR(20)  NOT NULL,                 -- só o NOME do avatar (ex.: 'Avatar 01')
    tentativas_invalidas INT          NOT NULL DEFAULT 0,       -- senhas erradas seguidas
    data_criacao         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP, -- o banco preenche sozinho
    data_alteracao       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP, -- o programa atualiza com NOW()
    ultimo_login         DATETIME     NULL,                     -- NULL = nunca fez login

    PRIMARY KEY (id),
    UNIQUE KEY uk_usuarios_nome_usuario (nome_usuario),         -- não deixa repetir o nome de usuário
    UNIQUE KEY uk_usuarios_email (email),                       -- não deixa repetir o e-mail

    CONSTRAINT ck_usuarios_tipo   CHECK (tipo   IN ('Administrador', 'Comum')),
    CONSTRAINT ck_usuarios_status CHECK (status IN ('Ativo', 'Inativo', 'Bloqueado'))
);

-- ---------------------------------------------------------------------
--  TABELA DE AUDITORIA
--  Guarda as alterações (categoria 'Alteração') e os eventos de login
--  (categoria 'Autenticação'). Nos eventos de login a coluna novo_valor
--  guarda o resultado: 'Sucesso', 'Falha' ou 'Bloqueada'.
--  NUNCA é gravada senha nesta tabela.
-- ---------------------------------------------------------------------
CREATE TABLE auditoria (
    id               INT          NOT NULL AUTO_INCREMENT,
    data_hora        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP, -- o banco preenche sozinho
    responsavel      VARCHAR(60)  NOT NULL,   -- quem fez (ex.: @maria)
    operacao         VARCHAR(80)  NOT NULL,   -- o que foi feito (ex.: 'Exclusão de usuário')
    registro_afetado VARCHAR(60)  NOT NULL,   -- em quem foi feito (ex.: @joao)
    valor_anterior   VARCHAR(255) NULL,
    novo_valor       VARCHAR(255) NULL,
    categoria        VARCHAR(20)  NOT NULL,   -- 'Alteração' ou 'Autenticação'

    PRIMARY KEY (id),
    KEY ix_auditoria_data (data_hora)
);

-- Pronto! Não é preciso inserir nenhum usuário aqui:
-- ao abrir o programa com a tabela vazia, aparece a tela "Primeiro cadastro",
-- que cria o primeiro administrador com a senha já em hash (BCrypt).
