using System;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace MemoriasAtelie
{
    public static class GerenciadorBanco
    {
        private static string stringConexao = "Data Source=memorias.db";

        public static void InicializarEstruturaPadrao()
        {
            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();

                    // ADICIONADA A COLUNA Medidas TEXT
                    string criarClientes = "CREATE TABLE IF NOT EXISTS Clientes (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nome TEXT NOT NULL, Whatsapp TEXT, Medidas TEXT);";
                    string criarProdutos = "CREATE TABLE IF NOT EXISTS Produtos (Id INTEGER PRIMARY KEY AUTOINCREMENT, Nome TEXT NOT NULL);";
                    string criarEncomendas = @"CREATE TABLE IF NOT EXISTS Encomendas (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            ClienteId INTEGER NOT NULL,
                            Produto TEXT,
                            Descricao TEXT,
                            FotosCaminhos TEXT, 
                            Valor REAL DEFAULT 0.0,
                            Status TEXT DEFAULT 'Pendente',
                            Data TEXT, 
                            FOREIGN KEY(ClienteId) REFERENCES Clientes(Id)
                          );";

                    using (var cmd = new SqliteCommand(criarClientes, conexao)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand(criarProdutos, conexao)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand(criarEncomendas, conexao)) cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro crítico na inicialização do banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                MessageBox.Show("✨ Banco de dados limpo e reestruturado com sucesso!\nO sistema está pronto para uso em produção.",
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

                    // MASSA DE TESTES ATUALIZADA COM AS MEDIDAS
                    string insertClientes = @"
                INSERT INTO Clientes (Id, Nome, Whatsapp, Medidas) VALUES (1, 'Ana Clara', '(61) 99999-9999', 'Busto: 90cm, Cintura: 70cm, Altura: 1.65m');
                INSERT INTO Clientes (Id, Nome, Whatsapp, Medidas) VALUES (2, 'Beatriz Souza', '(61) 88888-8888', 'Circunferência Cabeça: 42cm (Para Gorros)');
                INSERT INTO Clientes (Id, Nome, Whatsapp, Medidas) VALUES (3, 'Carlos Eduardo', '(61) 77777-7777', 'Mão: 18cm, Pulso: 16cm');";

                    string insertProdutos = @"
                INSERT INTO Produtos (Id, Nome) VALUES (1, 'Amigurumi Leão');
                INSERT INTO Produtos (Id, Nome) VALUES (2, 'Manta de Crochê');
                INSERT INTO Produtos (Id, Nome) VALUES (3, 'Bolsa Fio de Malha');";

                    string insertEncomendas = @"
                INSERT INTO Encomendas (ClienteId, Produto, Descricao, Valor, Status, Data) 
                VALUES (1, 'Amigurumi Leão', 'Tamanho M, cores neutras', 150.00, 'Entregue', '2026-06-10');

                INSERT INTO Encomendas (ClienteId, Produto, Descricao, Valor, Status, Data) 
                VALUES (2, 'Manta de Crochê', 'Casal, linha de algodão', 450.00, 'Em Produção', '2026-06-22');

                INSERT INTO Encomendas (ClienteId, Produto, Descricao, Valor, Status, Data) 
                VALUES (3, 'Bolsa Fio de Malha', 'Cor telha, com alça de couro', 120.00, 'Pendente', '2026-05-15');

                INSERT INTO Encomendas (ClienteId, Produto, Descricao, Valor, Status, Data) 
                VALUES (1, 'Amigurumi Leão', 'Chaveiro de brinde', 45.00, 'Concluído', '2026-04-02');";

                    using (var cmd = new SqliteCommand(insertClientes, conexao)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand(insertProdutos, conexao)) cmd.ExecuteNonQuery();
                    using (var cmd = new SqliteCommand(insertEncomendas, conexao)) cmd.ExecuteNonQuery();
                }

                MessageBox.Show("🎮 Banco de testes gerado com sucesso!\nDados fictícios foram carregados para testes de filtros e relatórios.",
                                "Ambiente de Teste", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao gerar banco de testes: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}