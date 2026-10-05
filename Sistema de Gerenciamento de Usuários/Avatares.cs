using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;

namespace Sistema_de_Gerenciamento_de_Usuários
{
    // Uma opção de avatar para a lista de escolha (nome + imagem)
    public class OpcaoAvatar
    {
        public string Nome { get; set; }          // ex.: "Avatar 01" (é isto que vai para o banco)
        public BitmapImage Imagem { get; set; }   // a imagem pronta para mostrar
    }

    public static class Avatares
    {
        // Os nomes dos 5 avatares (a posição 0 é o Avatar 01, e assim por diante)
        private static readonly string[] nomes =
        {
            "Avatar 01", "Avatar 02", "Avatar 03", "Avatar 04", "Avatar 05"
        };

        // A lista é montada uma vez só (na primeira vez que alguém pedir)
        private static List<OpcaoAvatar> lista = null;

        // Devolve as 5 opções (usada no ItemsSource das listas de avatar)
        public static List<OpcaoAvatar> Lista()
        {
            if (lista == null)
            {
                lista = new List<OpcaoAvatar>();

                for (int i = 0; i < nomes.Length; i++)
                {
                    OpcaoAvatar opcao = new OpcaoAvatar();
                    opcao.Nome = nomes[i];
                    opcao.Imagem = CriarImagem(TextoBase64(i));
                    lista.Add(opcao);
                }
            }

            return lista;
        }

        // Procura a imagem pelo nome. Se o nome não existir, devolve null (fica sem imagem).
        public static BitmapImage Imagem(string nome)
        {
            List<OpcaoAvatar> opcoes = Lista();

            for (int i = 0; i < opcoes.Count; i++)
            {
                if (opcoes[i].Nome == nome)
                {
                    return opcoes[i].Imagem;
                }
            }

            return null;
        }

        // True se o nome é de um dos 5 avatares
        public static bool Existe(string nome)
        {
            for (int i = 0; i < nomes.Length; i++)
            {
                if (nomes[i] == nome)
                {
                    return true;
                }
            }

            return false;
        }

        // Posição do avatar na lista (para deixar selecionado na tela). -1 = não achou.
        public static int Posicao(string nome)
        {
            for (int i = 0; i < nomes.Length; i++)
            {
                if (nomes[i] == nome)
                {
                    return i;
                }
            }

            return -1;
        }

        // Transforma o texto base64 em uma imagem do WPF
        private static BitmapImage CriarImagem(string base64)
        {
            byte[] bytes = Convert.FromBase64String(base64);

            using (MemoryStream memoria = new MemoryStream(bytes))
            {
                BitmapImage imagem = new BitmapImage();
                imagem.BeginInit();
                imagem.CacheOption = BitmapCacheOption.OnLoad; // lê tudo agora (pode fechar a memória depois)
                imagem.StreamSource = memoria;
                imagem.EndInit();
                imagem.Freeze();                               // deixa a imagem "somente leitura" (mais leve)
                return imagem;
            }
        }

        // Devolve o texto base64 do avatar na posição pedida
        private static string TextoBase64(int posicao)
        {
            if (posicao == 0)
            {
                return Avatar01;
            }
            else if (posicao == 1)
            {
                return Avatar02;
            }
            else if (posicao == 2)
            {
                return Avatar03;
            }
            else if (posicao == 3)
            {
                return Avatar04;
            }

            return Avatar05;
        }

        // =====================================================================
        //  AS IMAGENS (PNG 96x96 em base64). Não precisa mexer aqui.
        // =====================================================================

        // Avatar 01: fundo verde-escuro, camiseta amarela
        private const string Avatar01 =
            "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";

        // Avatar 02: fundo azul, cabelo comprido preto
        private const string Avatar02 =
            "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";

        // Avatar 03: fundo roxo, camiseta verde-água
        private const string Avatar03 =
            "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";

        // Avatar 04: fundo laranja, cabelo loiro
        private const string Avatar04 =
            "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"
          + "tDZTo+AAAAAElFTkSuQmCC";

        // Avatar 05: fundo cinza, camiseta verde
        private const string Avatar05 =
            "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";
    }
}
