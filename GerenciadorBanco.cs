using System;
using System.IO;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace MemoriasAtelie
{
    public static class GerenciadorBanco
    {
        private static string stringConexao;

        // Construtor estático: Executa automaticamente assim que a aplicação inicia
        static GerenciadorBanco()
        {
            try
            {
                // Obtém dinamicamente a pasta Documentos do usuário atual (ex: C:\Users\Nome\Documents)
                string pastaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                // Monta o caminho completo estável: Documentos\MemoriasAtelie\BancoDados
                string pastaBanco = Path.Combine(pastaDocumentos, "MemoriasAtelie", "BancoDados");
                string caminhoCompletoBanco = Path.Combine(pastaBanco, "memorias.db");

                // Cria os diretórios no computador do cliente se eles não existirem
                if (!Directory.Exists(pastaBanco))
                {
                    Directory.CreateDirectory(pastaBanco);
                }

                stringConexao = $"Data Source={caminhoCompletoBanco}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao configurar o diretório do banco de dados: {ex.Message}\nO sistema usará um banco temporário local na pasta do programa.",
                                "Aviso de Diretório", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Fallback de segurança caso a pasta de Documentos esteja inacessível
                stringConexao = "Data Source=memorias.db";
            }
        }

        // Método público para que todas as outras telas (como a CadastroEncomenda) consumam a mesma conexão
        public static string ObterStringConexao()
        {
            return stringConexao;
        }

        public static void InicializarEstruturaPadrao()
        {
            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();

                    // 1. Cria a estrutura base das tabelas se ainda não existirem
                    string criarClientes = @"CREATE TABLE IF NOT EXISTS Clientes (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                            Nome TEXT NOT NULL, 
                            Whatsapp TEXT, 
                            Medidas TEXT,
                            UltimaAtualizacao TEXT DEFAULT CURRENT_TIMESTAMP,
                            DispositivoOrigem TEXT DEFAULT 'Desconhecido'
                        );";

                    string criarProdutos = @"CREATE TABLE IF NOT EXISTS Produtos (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                            Nome TEXT NOT NULL,
                            UltimaAtualizacao TEXT DEFAULT CURRENT_TIMESTAMP,
                            DispositivoOrigem TEXT DEFAULT 'Desconhecido'
                        );";

                    string criarEncomendas = @"CREATE TABLE IF NOT EXISTS Encomendas (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ClienteId INTEGER NOT NULL,
                            Produto TEXT,
                            Descricao TEXT,
                            FotosCaminhos TEXT, 
                            Valor REAL DEFAULT 0.0,
                            Status TEXT DEFAULT 'Pendente',
                            Data TEXT, 
                            UltimaAtualizacao TEXT DEFAULT CURRENT_TIMESTAMP,
                            DispositivoOrigem TEXT DEFAULT 'Desconhecido',
                            FOREIGN KEY(ClienteId) REFERENCES Clientes(Id)
                          );";

                    using (var cmd = new SqliteCommand(criarClientes, conexao)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand(criarProdutos, conexao)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand(criarEncomendas, conexao)) cmd.ExecuteNonQuery();

                    // 2. Roda a atualização para garantir que bancos de dados antigos que já existem recebam as novas colunas
                    AtualizarEstruturaTabelasExistentes(conexao);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro crítico na inicialização do banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Método de migração segura. Percorre as tabelas existentes e adiciona as novas colunas
        /// sem corromper ou perder os registros antigos já cadastrados.
        /// </summary>
        private static void RedirectOuAtualizarTabela(SqliteConnection conexao, string tabela)
        {
            // Verifica e adiciona a coluna 'UltimaAtualizacao' se ela não existir
            if (!ColunaExiste(conexao, tabela, "UltimaAtualizacao"))
            {
                // CURRENT_TIMESTAMP popula automaticamente com a data/hora UTC atual no SQLite
                string query = $"ALTER TABLE {tabela} ADD COLUMN UltimaAtualizacao TEXT DEFAULT CURRENT_TIMESTAMP;";
                using (var cmd = new SqliteCommand(query, conexao)) cmd.ExecuteNonQuery();
            }

            // Verifica e adiciona a coluna 'DispositivoOrigem' se ela não existir
            if (!ColunaExiste(conexao, tabela, "DispositivoOrigem"))
            {
                // Atribui 'Desconhecido' para todos os registros antigos existentes
                string query = $"ALTER TABLE {tabela} ADD COLUMN DispositivoOrigem TEXT DEFAULT 'Desconhecido';";
                using (var cmd = new SqliteCommand(query, conexao)) cmd.ExecuteNonQuery();
            }
        }

        private static void AtualizarEstruturaTabelasExistentes(SqliteConnection conexao)
        {
            RedirectOuAtualizarTabela(conexao, "Clientes");
            RedirectOuAtualizarTabela(conexao, "Produtos");
            RedirectOuAtualizarTabela(conexao, "Encomendas");
        }

        /// <summary>
        /// Consulta os metadados da tabela para saber se a coluna informada já existe.
        /// </summary>
        private static bool ColunaExiste(SqliteConnection conexao, string tabela, string coluna)
        {
            string query = $"PRAGMA table_info({tabela});";
            using (var cmd = new SqliteCommand(query, conexao))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // A segunda coluna (índice 1) do PRAGMA retorna o nome das colunas da tabela
                        if (reader.GetString(1).Equals(coluna, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static void CriarBancoVazio()
        {
            var resultado = MessageBox.Show("Aviso: Isso apagará permanentemente todos os registros atuais do seu sistema para iniciar do zero.\n\nDeseja continuar?",
                                            "Confirmar Reset", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (resultado != MessageBoxResult.Yes) return;

            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();
                    using (var cmd = new SqliteCommand("DROP TABLE IF EXISTS Encomendas; DROP TABLE IF EXISTS Clientes; DROP TABLE IF EXISTS Produtos;", conexao))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                InicializarEstruturaPadrao();
                MessageBox.Show("✨ Banco de dados limpo e reestruturado com sucesso na pasta Documentos!\nO sistema está pronto para uso em produção.",
                                "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao resetar banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void CriarBancoTeste()
        {
            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();
                    using (var cmd = new SqliteCommand("DROP TABLE IF EXISTS Encomendas; DROP TABLE IF EXISTS Clientes; DROP TABLE IF EXISTS Produtos;", conexao))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }

                InicializarEstruturaPadrao();

                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();

                    // MASSA DE TESTES ATUALIZADA COM AS MEDIDAS, DATAS E MARCAÇÃO DE DISPOSITIVOS
                    string insertClientes = @"
                INSERT INTO Clientes (Id, Nome, Whatsapp, Medidas, UltimaAtualizacao, DispositivoOrigem) VALUES (1, 'Ana Clara', '(61) 99999-9999', 'Busto: 90cm, Cintura: 70cm, Altura: 1.65m', '2026-07-16 10:00:00', 'Windows');
                INSERT INTO Clientes (Id, Nome, Whatsapp, Medidas, UltimaAtualizacao, DispositivoOrigem) VALUES (2, 'Beatriz Souza', '(61) 88888-8888', 'Circunferência Cabeça: 42cm (Para Gorros)', '2026-07-16 11:30:00', 'Android');
                INSERT INTO Clientes (Id, Nome, Whatsapp, Medidas, UltimaAtualizacao, DispositivoOrigem) VALUES (3, 'Carlos Eduardo', '(61) 77777-7777', 'Mão: 18cm, Pulso: 16cm', '2026-07-16 12:15:00', 'Windows');";

                    string insertProdutos = @"
                INSERT INTO Produtos (Id, Nome, UltimaAtualizacao, DispositivoOrigem) VALUES (1, 'Amigurumi Leão', '2026-07-16 09:00:00', 'Windows');
                INSERT INTO Produtos (Id, Nome, UltimaAtualizacao, DispositivoOrigem) VALUES (2, 'Manta de Crochê', '2026-07-16 09:05:00', 'Windows');
                INSERT INTO Produtos (Id, Nome, UltimaAtualizacao, DispositivoOrigem) VALUES (3, 'Bolsa Fio de Malha', '2026-07-16 09:10:00', 'Android');";

                    string insertEncomendas = @"
                INSERT INTO Encomendas (ClienteId, Produto, Descricao, Valor, Status, Data, UltimaAtualizacao, DispositivoOrigem) 
                VALUES (1, 'Amigurumi Leão', 'Tamanho M, cores neutras', 150.00, 'Entregue', '2026-06-10', '2026-07-16 14:00:00', 'Windows');

                INSERT INTO Encomendas (ClienteId, Produto, Descricao, Valor, Status, Data, UltimaAtualizacao, DispositivoOrigem) 
                VALUES (2, 'Manta de Crochê', 'Casal, linha de algodão', 450.00, 'Em Produção', '2026-06-22', '2026-07-16 14:05:00', 'Android');

                INSERT INTO Encomendas (ClienteId, Produto, Descricao, Valor, Status, Data, UltimaAtualizacao, DispositivoOrigem) 
                VALUES (3, 'Bolsa Fio de Malha', 'Cor telha, com alça de couro', 120.00, 'Pendente', '2026-05-15', '2026-07-16 14:10:00', 'Windows');

                INSERT INTO Encomendas (ClienteId, Produto, Descricao, Valor, Status, Data, UltimaAtualizacao, DispositivoOrigem) 
                VALUES (1, 'Amigurumi Leão', 'Chaveiro de brinde', 45.00, 'Concluído', '2026-04-02', '2026-07-16 14:12:00', 'Android');";

                    using (var cmd = new SqliteCommand(insertClientes, conexao)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand(insertProdutos, conexao)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand(insertEncomendas, conexao)) cmd.ExecuteNonQuery();
                }

                MessageBox.Show("🎮 Banco de testes gerado com sucesso nos Documentos!\nDados fictícios foram carregados para testes de filtros e relatórios.",
                                "Ambiente de Teste", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao gerar banco de testes: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}