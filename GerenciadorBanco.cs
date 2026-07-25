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

                // Monta o caminho completo estável com o NOVO NOME do banco Windows
                string pastaBanco = Path.Combine(pastaDocumentos, "MemoriasAtelie", "BancoDados");
                string caminhoCompletoBanco = Path.Combine(pastaBanco, "memoriasWindows.db");

                // Cria os diretórios no computador se eles não existirem
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
                stringConexao = "Data Source=memorias_win.db";
            }
        }

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

                    // 1. Cria a estrutura base das tabelas com a nova coluna ValorPago
                    string criarClientes = @"CREATE TABLE IF NOT EXISTS Clientes (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                            Nome TEXT NOT NULL, 
                            Whatsapp TEXT, 
                            Medidas TEXT,
                            UltimaAtualizacao TEXT DEFAULT CURRENT_TIMESTAMP,
                            DispositivoOrigem TEXT DEFAULT 'Windows'
                        );";

                    string criarProdutos = @"CREATE TABLE IF NOT EXISTS Produtos (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT, 
                            Nome TEXT NOT NULL,
                            UltimaAtualizacao TEXT DEFAULT CURRENT_TIMESTAMP,
                            DispositivoOrigem TEXT DEFAULT 'Windows'
                        );";

                    string criarEncomendas = @"CREATE TABLE IF NOT EXISTS Encomendas (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ClienteId INTEGER NOT NULL,
                            Produto TEXT,
                            Descricao TEXT,
                            FotosCaminhos TEXT, 
                            Valor REAL DEFAULT 0.0,
                            ValorPago REAL DEFAULT 0.0,
                            Status TEXT DEFAULT 'Pendente',
                            Data TEXT, 
                            UltimaAtualizacao TEXT DEFAULT CURRENT_TIMESTAMP,
                            DispositivoOrigem TEXT DEFAULT 'Windows',
                            FOREIGN KEY(ClienteId) REFERENCES Clientes(Id)
                          );";

                    using (var cmd = new SqliteCommand(criarClientes, conexao)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand(criarProdutos, conexao)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand(criarEncomendas, conexao)) cmd.ExecuteNonQuery();

                    // 2. Garante que bancos/estruturas já existentes recebam as novas colunas
                    AtualizarEstruturaTabelasExistentes(conexao);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro crítico na inicialização do banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void RedirectOuAtualizarTabela(SqliteConnection conexao, string tabela)
        {
            // Adiciona UltimaAtualizacao se não existir
            if (!ColunaExiste(conexao, tabela, "UltimaAtualizacao"))
            {
                string query = $"ALTER TABLE {tabela} ADD COLUMN UltimaAtualizacao TEXT DEFAULT CURRENT_TIMESTAMP;";
                using (var cmd = new SqliteCommand(query, conexao)) cmd.ExecuteNonQuery();
            }

            // Adiciona DispositivoOrigem se não existir
            if (!ColunaExiste(conexao, tabela, "DispositivoOrigem"))
            {
                string query = $"ALTER TABLE {tabela} ADD COLUMN DispositivoOrigem TEXT DEFAULT 'Windows';";
                using (var cmd = new SqliteCommand(query, conexao)) cmd.ExecuteNonQuery();
            }

            // Adiciona ValorPago especificamente se for a tabela Encomendas
            if (tabela.Equals("Encomendas", StringComparison.OrdinalIgnoreCase) && !ColunaExiste(conexao, tabela, "ValorPago"))
            {
                string query = "ALTER TABLE Encomendas ADD COLUMN ValorPago REAL DEFAULT 0.0;";
                using (var cmd = new SqliteCommand(query, conexao)) cmd.ExecuteNonQuery();
            }
        }

        private static void AtualizarEstruturaTabelasExistentes(SqliteConnection conexao)
        {
            RedirectOuAtualizarTabela(conexao, "Clientes");
            RedirectOuAtualizarTabela(conexao, "Produtos");
            RedirectOuAtualizarTabela(conexao, "Encomendas");
        }

        private static bool ColunaExiste(SqliteConnection conexao, string tabela, string coluna)
        {
            string query = $"PRAGMA table_info({tabela});";
            using (var cmd = new SqliteCommand(query, conexao))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
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
            var resultado = MessageBox.Show("Aviso: Isso apagará permanentemente todos os registros do banco local do Windows para iniciar do zero.\n\nDeseja continuar?",
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
                MessageBox.Show("✨ Banco Windows ('memoriasWindows.db') limpo e reestruturado com sucesso!",
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

                    string insertClientes = @"
                INSERT INTO Clientes (Id, Nome, Whatsapp, Medidas, UltimaAtualizacao, DispositivoOrigem) VALUES (1, 'Ana Clara', '(61) 99999-9999', 'Busto: 90cm, Cintura: 70cm', '2026-07-16 10:00:00', 'Windows');
                INSERT INTO Clientes (Id, Nome, Whatsapp, Medidas, UltimaAtualizacao, DispositivoOrigem) VALUES (2, 'Beatriz Souza', '(61) 88888-8888', 'Cabeça: 42cm', '2026-07-16 11:30:00', 'Android');";

                    string insertProdutos = @"
                INSERT INTO Produtos (Id, Nome, UltimaAtualizacao, DispositivoOrigem) VALUES (1, 'Amigurumi Leão', '2026-07-16 09:00:00', 'Windows');
                INSERT INTO Produtos (Id, Nome, UltimaAtualizacao, DispositivoOrigem) VALUES (2, 'Manta de Crochê', '2026-07-16 09:05:00', 'Windows');";

                    string insertEncomendas = @"
                INSERT INTO Encomendas (ClienteId, Produto, Descricao, Valor, ValorPago, Status, Data, UltimaAtualizacao, DispositivoOrigem) 
                VALUES (1, 'Amigurumi Leão', 'Tamanho M, cores neutras', 150.00, 150.00, 'Entregue', '2026-06-10', '2026-07-16 14:00:00', 'Windows');

                INSERT INTO Encomendas (ClienteId, Produto, Descricao, Valor, ValorPago, Status, Data, UltimaAtualizacao, DispositivoOrigem) 
                VALUES (2, 'Manta de Crochê', 'Casal, linha de algodão', 450.00, 200.00, 'Em Produção', '2026-06-22', '2026-07-16 14:05:00', 'Android');";

                    using (var cmd = new SqliteCommand(insertClientes, conexao)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand(insertProdutos, conexao)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand(insertEncomendas, conexao)) cmd.ExecuteNonQuery();
                }

                MessageBox.Show("🎮 Banco de testes gerado com sucesso com o arquivo 'memorias_win.db'!",
                                "Ambiente de Teste", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao gerar banco de testes: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}