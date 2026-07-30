using System;
using System.IO;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace MemoriasAtelie
{
    public static class SincronizadorLocal
    {
        public static void SincronizarComAndroid()
        {
            try
            {
                string pastaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string pastaBanco = Path.Combine(pastaDocumentos, "MemoriasAtelie", "BancoDados");

                string caminhoBancoWindows = Path.Combine(pastaBanco, "memoriasWindows.db");
                string caminhoBancoAndroid = Path.Combine(pastaBanco, "memoriasAndroid.db");

                // Se o banco do Android ainda não existe na pasta de documentos/Drive, pula a sincronização
                if (!File.Exists(caminhoBancoWindows) || !File.Exists(caminhoBancoAndroid))
                {
                    return;
                }

                using (var conexaoWin = new SqliteConnection($"Data Source={caminhoBancoWindows}"))
                using (var conexaoAnd = new SqliteConnection($"Data Source={caminhoBancoAndroid}"))
                {
                    conexaoWin.Open();
                    conexaoAnd.Open();

                    using (var transacao = conexaoWin.BeginTransaction())
                    {
                        try
                        {
                            MesclarClientes(conexaoAnd, conexaoWin, transacao);
                            MesclarProdutos(conexaoAnd, conexaoWin, transacao);
                            MesclarEncomendas(conexaoAnd, conexaoWin, transacao);

                            transacao.Commit();
                        }
                        catch
                        {
                            transacao.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Registra/exibe falhas leves de sincronização sem travar o aplicativo
                System.Diagnostics.Debug.WriteLine($"Erro na sincronização automática do Android: {ex.Message}");
            }
        }

        private static void MesclarClientes(SqliteConnection fonte, SqliteConnection destino, SqliteTransaction transacao)
        {
            string selectQuery = "SELECT Id, Nome, Whatsapp, Medidas, UltimaAtualizacao, DispositivoOrigem FROM Clientes;";

            using (var cmdFonte = new SqliteCommand(selectQuery, fonte))
            using (var reader = cmdFonte.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string nome = reader.GetString(1);
                    string whatsapp = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    string medidas = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    string dataAndroidStr = reader.IsDBNull(4) ? "1970-01-01 00:00:00" : reader.GetString(4);
                    string dispositivoOrigem = reader.IsDBNull(5) ? "Android" : reader.GetString(5);

                    DateTime dataAndroid = DateTime.TryParse(dataAndroidStr, out var dtA) ? dtA : DateTime.MinValue;

                    string checkQuery = "SELECT UltimaAtualizacao FROM Clientes WHERE Id = @Id;";
                    using (var cmdCheck = new SqliteCommand(checkQuery, destino, transacao))
                    {
                        cmdCheck.Parameters.AddWithValue("@Id", id);
                        var res = cmdCheck.ExecuteScalar();

                        if (res == null)
                        {
                            string insert = @"INSERT INTO Clientes (Id, Nome, Whatsapp, Medidas, UltimaAtualizacao, DispositivoOrigem) 
                                             VALUES (@Id, @Nome, @Whatsapp, @Medidas, @UltimaAtualizacao, @DispositivoOrigem);";
                            using (var cmdIns = new SqliteCommand(insert, destino, transacao))
                            {
                                cmdIns.Parameters.AddWithValue("@Id", id);
                                cmdIns.Parameters.AddWithValue("@Nome", nome);
                                cmdIns.Parameters.AddWithValue("@Whatsapp", whatsapp);
                                cmdIns.Parameters.AddWithValue("@Medidas", medidas);
                                cmdIns.Parameters.AddWithValue("@UltimaAtualizacao", dataAndroidStr);
                                cmdIns.Parameters.AddWithValue("@DispositivoOrigem", dispositivoOrigem);
                                cmdIns.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            DateTime dataWin = DateTime.TryParse(res.ToString(), out var dtW) ? dtW : DateTime.MinValue;
                            if (dataAndroid > dataWin)
                            {
                                string update = @"UPDATE Clientes 
                                                 SET Nome = @Nome, Whatsapp = @Whatsapp, Medidas = @Medidas, 
                                                     UltimaAtualizacao = @UltimaAtualizacao, DispositivoOrigem = @DispositivoOrigem 
                                                 WHERE Id = @Id;";
                                using (var cmdUpd = new SqliteCommand(update, destino, transacao))
                                {
                                    cmdUpd.Parameters.AddWithValue("@Id", id);
                                    cmdUpd.Parameters.AddWithValue("@Nome", nome);
                                    cmdUpd.Parameters.AddWithValue("@Whatsapp", whatsapp);
                                    cmdUpd.Parameters.AddWithValue("@Medidas", medidas);
                                    cmdUpd.Parameters.AddWithValue("@UltimaAtualizacao", dataAndroidStr);
                                    cmdUpd.Parameters.AddWithValue("@DispositivoOrigem", dispositivoOrigem);
                                    cmdUpd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void MesclarProdutos(SqliteConnection fonte, SqliteConnection destino, SqliteTransaction transacao)
        {
            string selectQuery = "SELECT Id, Nome, UltimaAtualizacao, DispositivoOrigem FROM Produtos;";

            using (var cmdFonte = new SqliteCommand(selectQuery, fonte))
            using (var reader = cmdFonte.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string nome = reader.GetString(1);
                    string dataAndroidStr = reader.IsDBNull(2) ? "1970-01-01 00:00:00" : reader.GetString(2);
                    string dispositivoOrigem = reader.IsDBNull(3) ? "Android" : reader.GetString(3);

                    DateTime dataAndroid = DateTime.TryParse(dataAndroidStr, out var dtA) ? dtA : DateTime.MinValue;

                    string checkQuery = "SELECT UltimaAtualizacao FROM Produtos WHERE Id = @Id;";
                    using (var cmdCheck = new SqliteCommand(checkQuery, destino, transacao))
                    {
                        cmdCheck.Parameters.AddWithValue("@Id", id);
                        var res = cmdCheck.ExecuteScalar();

                        if (res == null)
                        {
                            string insert = @"INSERT INTO Produtos (Id, Nome, UltimaAtualizacao, DispositivoOrigem) 
                                             VALUES (@Id, @Nome, @UltimaAtualizacao, @DispositivoOrigem);";
                            using (var cmdIns = new SqliteCommand(insert, destino, transacao))
                            {
                                cmdIns.Parameters.AddWithValue("@Id", id);
                                cmdIns.Parameters.AddWithValue("@Nome", nome);
                                cmdIns.Parameters.AddWithValue("@UltimaAtualizacao", dataAndroidStr);
                                cmdIns.Parameters.AddWithValue("@DispositivoOrigem", dispositivoOrigem);
                                cmdIns.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            DateTime dataWin = DateTime.TryParse(res.ToString(), out var dtW) ? dtW : DateTime.MinValue;
                            if (dataAndroid > dataWin)
                            {
                                string update = @"UPDATE Produtos 
                                                 SET Nome = @Nome, UltimaAtualizacao = @UltimaAtualizacao, DispositivoOrigem = @DispositivoOrigem 
                                                 WHERE Id = @Id;";
                                using (var cmdUpd = new SqliteCommand(update, destino, transacao))
                                {
                                    cmdUpd.Parameters.AddWithValue("@Id", id);
                                    cmdUpd.Parameters.AddWithValue("@Nome", nome);
                                    cmdUpd.Parameters.AddWithValue("@UltimaAtualizacao", dataAndroidStr);
                                    cmdUpd.Parameters.AddWithValue("@DispositivoOrigem", dispositivoOrigem);
                                    cmdUpd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void MesclarEncomendas(SqliteConnection fonte, SqliteConnection destino, SqliteTransaction transacao)
        {
            string selectQuery = @"SELECT Id, ClienteId, Produto, Descricao, FotosCaminhos, Valor, ValorPago, Status, Data, UltimaAtualizacao, DispositivoOrigem 
                                   FROM Encomendas;";

            using (var cmdFonte = new SqliteCommand(selectQuery, fonte))
            using (var reader = cmdFonte.ExecuteReader())
            {
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    int clienteId = reader.GetInt32(1);
                    string produto = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    string descricao = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    string fotosCaminhos = reader.IsDBNull(4) ? "" : reader.GetString(4);
                    double valor = reader.IsDBNull(5) ? 0.0 : reader.GetDouble(5);
                    double valorPago = reader.IsDBNull(6) ? 0.0 : reader.GetDouble(6);
                    string status = reader.IsDBNull(7) ? "Pendente" : reader.GetString(7);
                    string data = reader.IsDBNull(8) ? "" : reader.GetString(8);
                    string dataAndroidStr = reader.IsDBNull(9) ? "1970-01-01 00:00:00" : reader.GetString(9);
                    string dispositivoOrigem = reader.IsDBNull(10) ? "Android" : reader.GetString(10);

                    DateTime dataAndroid = DateTime.TryParse(dataAndroidStr, out var dtA) ? dtA : DateTime.MinValue;

                    string checkQuery = "SELECT UltimaAtualizacao FROM Encomendas WHERE Id = @Id;";
                    using (var cmdCheck = new SqliteCommand(checkQuery, destino, transacao))
                    {
                        cmdCheck.Parameters.AddWithValue("@Id", id);
                        var res = cmdCheck.ExecuteScalar();

                        if (res == null)
                        {
                            string insert = @"INSERT INTO Encomendas (Id, ClienteId, Produto, Descricao, FotosCaminhos, Valor, ValorPago, Status, Data, UltimaAtualizacao, DispositivoOrigem) 
                                             VALUES (@Id, @ClienteId, @Produto, @Descricao, @FotosCaminhos, @Valor, @ValorPago, @Status, @Data, @UltimaAtualizacao, @DispositivoOrigem);";
                            using (var cmdIns = new SqliteCommand(insert, destino, transacao))
                            {
                                cmdIns.Parameters.AddWithValue("@Id", id);
                                cmdIns.Parameters.AddWithValue("@ClienteId", clienteId);
                                cmdIns.Parameters.AddWithValue("@Produto", produto);
                                cmdIns.Parameters.AddWithValue("@Descricao", descricao);
                                cmdIns.Parameters.AddWithValue("@FotosCaminhos", fotosCaminhos);
                                cmdIns.Parameters.AddWithValue("@Valor", valor);
                                cmdIns.Parameters.AddWithValue("@ValorPago", valorPago);
                                cmdIns.Parameters.AddWithValue("@Status", status);
                                cmdIns.Parameters.AddWithValue("@Data", data);
                                cmdIns.Parameters.AddWithValue("@UltimaAtualizacao", dataAndroidStr);
                                cmdIns.Parameters.AddWithValue("@DispositivoOrigem", dispositivoOrigem);
                                cmdIns.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            DateTime dataWin = DateTime.TryParse(res.ToString(), out var dtW) ? dtW : DateTime.MinValue;
                            if (dataAndroid > dataWin)
                            {
                                string update = @"UPDATE Encomendas 
                                                 SET ClienteId = @ClienteId, Produto = @Produto, Descricao = @Descricao, 
                                                     FotosCaminhos = @FotosCaminhos, Valor = @Valor, ValorPago = @ValorPago, 
                                                     Status = @Status, Data = @Data, UltimaAtualizacao = @UltimaAtualizacao, 
                                                     DispositivoOrigem = @DispositivoOrigem 
                                                 WHERE Id = @Id;";
                                using (var cmdUpd = new SqliteCommand(update, destino, transacao))
                                {
                                    cmdUpd.Parameters.AddWithValue("@Id", id);
                                    cmdUpd.Parameters.AddWithValue("@ClienteId", clienteId);
                                    cmdUpd.Parameters.AddWithValue("@Produto", produto);
                                    cmdUpd.Parameters.AddWithValue("@Descricao", descricao);
                                    cmdUpd.Parameters.AddWithValue("@FotosCaminhos", fotosCaminhos);
                                    cmdUpd.Parameters.AddWithValue("@Valor", valor);
                                    cmdUpd.Parameters.AddWithValue("@ValorPago", valorPago);
                                    cmdUpd.Parameters.AddWithValue("@Status", status);
                                    cmdUpd.Parameters.AddWithValue("@Data", data);
                                    cmdUpd.Parameters.AddWithValue("@UltimaAtualizacao", dataAndroidStr);
                                    cmdUpd.Parameters.AddWithValue("@DispositivoOrigem", dispositivoOrigem);
                                    cmdUpd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}