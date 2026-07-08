using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace MemoriasAtelie
{
    public partial class TelaGestaoFinanceira : UserControl
    {
        private readonly string stringConexao = "Data Source=memorias.db";
        private bool inicializado = false;
        private bool bloquearFiltro = false;

        public TelaGestaoFinanceira()
        {
            InitializeComponent();
            GarantirColunaValorPago();

            bloquearFiltro = true;
            PreencherFiltrosDatas();
            bloquearFiltro = false;

            inicializado = true;

            // Dispara a carga inicial de dados e dos novos ComboBoxes de filtro
            _ = InicializarDadosTelaAsync();
        }

        private async Task InicializarDadosTelaAsync()
        {
            // Carrega as listas dos filtros em segundo plano
            await Task.WhenAll(CarregarClientesFiltroAsync(), CarregarProdutosFiltroAsync());

            // Depois de carregar as listas, traz o relatório financeiro
            await CarReportFinanceiroAsync();
        }

        private void GarantirColunaValorPago()
        {
            using (var conexao = new SqliteConnection(stringConexao))
            {
                conexao.Open();
                try
                {
                    using (var cmd = new SqliteCommand("ALTER TABLE Encomendas ADD COLUMN ValorPago REAL DEFAULT 0;", conexao))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { }
            }
        }

        private void PreencherFiltrosDatas()
        {
            CboMes.Items.Add(new ComboBoxItem { Content = "Todos", Tag = "00" });
            CboMes.Items.Add(new ComboBoxItem { Content = "Janeiro", Tag = "01" });
            CboMes.Items.Add(new ComboBoxItem { Content = "Fevereiro", Tag = "02" });
            CboMes.Items.Add(new ComboBoxItem { Content = "Março", Tag = "03" });
            CboMes.Items.Add(new ComboBoxItem { Content = "Abril", Tag = "04" });
            CboMes.Items.Add(new ComboBoxItem { Content = "Maio", Tag = "05" });
            CboMes.Items.Add(new ComboBoxItem { Content = "Junho", Tag = "06" });
            CboMes.Items.Add(new ComboBoxItem { Content = "Julho", Tag = "07" });
            CboMes.Items.Add(new ComboBoxItem { Content = "Agosto", Tag = "08" });
            CboMes.Items.Add(new ComboBoxItem { Content = "Setembro", Tag = "09" });
            CboMes.Items.Add(new ComboBoxItem { Content = "Outubro", Tag = "10" });
            CboMes.Items.Add(new ComboBoxItem { Content = "Novembro", Tag = "11" });
            CboMes.Items.Add(new ComboBoxItem { Content = "Dezembro", Tag = "12" });

            CboMes.SelectedIndex = DateTime.Now.Month;

            int anoAtual = DateTime.Now.Year;
            for (int i = anoAtual - 1; i <= anoAtual + 2; i++)
            {
                CboAno.Items.Add(i.ToString());
            }
            CboAno.SelectedItem = anoAtual.ToString();
        }

        private async Task CarregarClientesFiltroAsync()
        {
            try
            {
                var listaClientes = await Task.Run(() =>
                {
                    var lista = new List<FiltroItemModel> { new FiltroItemModel { Nome = "Todos" } };
                    using (var conexao = new SqliteConnection(stringConexao))
                    {
                        conexao.Open();
                        using (var cmd = new SqliteCommand("SELECT Nome FROM Clientes ORDER BY Nome ASC;", conexao))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new FiltroItemModel { Nome = reader["Nome"].ToString() });
                            }
                        }
                    }
                    return lista;
                });

                CboBuscarCliente.ItemsSource = listaClientes;
                CboBuscarCliente.SelectedIndex = 0; // Seleciona "Todos" por padrão
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar clientes do filtro: " + ex.Message);
            }
        }

        private async Task CarregarProdutosFiltroAsync()
        {
            try
            {
                var listaProdutos = await Task.Run(() =>
                {
                    var lista = new List<FiltroItemModel> { new FiltroItemModel { Nome = "Todos" } };
                    using (var conexao = new SqliteConnection(stringConexao))
                    {
                        conexao.Open();
                        // Seleciona produtos distintos para não repetir itens no ComboBox
                        using (var cmd = new SqliteCommand("SELECT DISTINCT Produto FROM Encomendas WHERE Produto IS NOT NULL AND Produto != '' ORDER BY Produto ASC;", conexao))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                lista.Add(new FiltroItemModel { Nome = reader["Produto"].ToString() });
                            }
                        }
                    }
                    return lista;
                });

                CboBuscarProduto.ItemsSource = listaProdutos;
                CboBuscarProduto.SelectedIndex = 0; // Seleciona "Todos" por padrão
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar produtos do filtro: " + ex.Message);
            }
        }

        private async Task CarReportFinanceiroAsync()
        {
            if (!inicializado || bloquearFiltro) return;

            string mesSelecionado = (CboMes.SelectedItem as ComboBoxItem)?.Tag.ToString();
            string anoSelecionado = CboAno.SelectedItem?.ToString();
            string situacaoFiltro = (CboSituacaoPgto.SelectedItem as ComboBoxItem)?.Content.ToString();

            // Captura os valores selecionados nos novos ComboBoxes
            string clienteSelecionado = CboBuscarCliente.SelectedValue?.ToString();
            string produtoSelecionado = CboBuscarProduto.SelectedValue?.ToString();

            DgFinanceiro.IsEnabled = false;

            try
            {
                var resultado = await Task.Run(() =>
                {
                    var dadosTabela = new List<LinhaFinanceiroModel>();
                    double faturado = 0, recebido = 0, aReceber = 0, alertaCritico = 0;

                    using (var conexao = new SqliteConnection(stringConexao))
                    {
                        conexao.Open();
                        string query = @"SELECT e.Id, e.Data, c.Nome, e.Produto, e.Status, e.Valor, e.ValorPago 
                                         FROM Encomendas e
                                         INNER JOIN Clientes c ON e.ClienteId = c.Id
                                         WHERE strftime('%Y', e.Data) = @Ano";

                        if (mesSelecionado != "00") query += " AND strftime('%m', e.Data) = @Mes";

                        // Atualizado para filtro exato (removendo o LIKE) caso não seja "Todos"
                        if (!string.IsNullOrEmpty(clienteSelecionado) && clienteSelecionado != "Todos")
                            query += " AND c.Nome = @BuscaCliente";

                        if (!string.IsNullOrEmpty(produtoSelecionado) && produtoSelecionado != "Todos")
                            query += " AND e.Produto = @BuscaProduto";

                        query += " ORDER BY e.Data DESC;";

                        using (var cmd = new SqliteCommand(query, conexao))
                        {
                            cmd.Parameters.AddWithValue("@Ano", anoSelecionado);
                            if (mesSelecionado != "00") cmd.Parameters.AddWithValue("@Mes", mesSelecionado);

                            if (!string.IsNullOrEmpty(clienteSelecionado) && clienteSelecionado != "Todos")
                                cmd.Parameters.AddWithValue("@BuscaCliente", clienteSelecionado);

                            if (!string.IsNullOrEmpty(produtoSelecionado) && produtoSelecionado != "Todos")
                                cmd.Parameters.AddWithValue("@BuscaProduto", produtoSelecionado);

                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string dataBanco = reader["Data"].ToString();
                                    string statusPeca = reader["Status"].ToString();
                                    double valorTotal = reader["Valor"] != DBNull.Value ? Convert.ToDouble(reader["Valor"]) : 0;
                                    double valorPago = reader["ValorPago"] != DBNull.Value ? Convert.ToDouble(reader["ValorPago"]) : 0;

                                    string dataFormt = dataBanco;
                                    if (DateTime.TryParse(dataBanco, out DateTime dt)) dataFormt = dt.ToString("dd/MM/yyyy");

                                    string situacao = "Não Pago";
                                    if (valorPago >= valorTotal && valorTotal > 0) situacao = "Pago";
                                    else if (valorPago > 0 && valorPago < valorTotal) situacao = "Parcial";

                                    if (situacaoFiltro != "Todos" && situacao != situacaoFiltro) continue;

                                    faturado += valorTotal;
                                    recebido += valorPago;
                                    aReceber += (valorTotal - valorPago);

                                    if (statusPeca == "Entregue" && valorPago < valorTotal)
                                    {
                                        alertaCritico += (valorTotal - valorPago);
                                    }

                                    dadosTabela.Add(new LinhaFinanceiroModel
                                    {
                                        Id = Convert.ToInt32(reader["Id"]),
                                        DataFormatada = dataFormt,
                                        NomeCliente = reader["Nome"].ToString(),
                                        Produto = reader["Produto"].ToString(),
                                        Status = statusPeca,
                                        ValorTotal = valorTotal,
                                        ValorPago = valorPago,
                                        ValorPagoInput = valorPago.ToString("F2", CultureInfo.InvariantCulture),
                                        SituacaoFinanceira = situacao
                                    });
                                }
                            }
                        }
                    }

                    return new { dadosTabela, faturado, recebido, aReceber, alertaCritico };
                });

                var culturaPtBr = new CultureInfo("pt-BR");
                TxtTotalFaturado.Text = resultado.faturado.ToString("C2", culturaPtBr);
                TxtTotalRecebido.Text = resultado.recebido.ToString("C2", culturaPtBr);
                TxtTotalAReceber.Text = resultado.aReceber.ToString("C2", culturaPtBr);
                TxtTotalAlerta.Text = resultado.alertaCritico.ToString("C2", culturaPtBr);

                // ===================================================================
                // SEGURANÇA CONTRA LOOPS: Desvincula os novos ComboBoxes também
                // ===================================================================
                CboMes.SelectionChanged -= Filtro_Changed;
                CboAno.SelectionChanged -= Filtro_Changed;
                if (CboSituacaoPgto != null) CboSituacaoPgto.SelectionChanged -= Filtro_Changed;
                if (CboBuscarCliente != null) CboBuscarCliente.SelectionChanged -= Filtro_Changed;
                if (CboBuscarProduto != null) CboBuscarProduto.SelectionChanged -= Filtro_Changed;

                bloquearFiltro = true;

                DgFinanceiro.ItemsSource = null;
                if (resultado.dadosTabela != null)
                {
                    DgFinanceiro.ItemsSource = resultado.dadosTabela;
                }

                await Task.Delay(1);

                bloquearFiltro = false;

                // Devolve os eventos de escuta
                CboMes.SelectionChanged += Filtro_Changed;
                CboAno.SelectionChanged += Filtro_Changed;
                if (CboSituacaoPgto != null) CboSituacaoPgto.SelectionChanged += Filtro_Changed;
                if (CboBuscarCliente != null) CboBuscarCliente.SelectionChanged += Filtro_Changed;
                if (CboBuscarProduto != null) CboBuscarProduto.SelectionChanged += Filtro_Changed;
                // ===================================================================
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar dados: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                CboMes.SelectionChanged += Filtro_Changed;
                CboAno.SelectionChanged += Filtro_Changed;
                if (CboBuscarCliente != null) CboBuscarCliente.SelectionChanged += Filtro_Changed;
                if (CboBuscarProduto != null) CboBuscarProduto.SelectionChanged += Filtro_Changed;
                bloquearFiltro = false;
            }
            finally
            {
                DgFinanceiro.IsEnabled = true;
            }
        }

        private async void Filtro_Changed(object sender, RoutedEventArgs e)
        {
            await CarReportFinanceiroAsync();
        }

        private async void CboStatusProducaoGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!inicializado || bloquearFiltro) return;

            var combo = sender as ComboBox;
            var itemSelecionado = combo?.SelectedItem as ComboBoxItem;
            var linha = combo?.DataContext as LinhaFinanceiroModel;

            if (linha != null && itemSelecionado != null)
            {
                string novoStatusProducao = itemSelecionado.Content.ToString();

                if (linha.Status == novoStatusProducao) return;

                try
                {
                    await Task.Run(() =>
                    {
                        using (var conexao = new SqliteConnection(stringConexao))
                        {
                            conexao.Open();
                            string query = "UPDATE Encomendas SET Status = @Status WHERE Id = @Id;";
                            using (var cmd = new SqliteCommand(query, conexao))
                            {
                                cmd.Parameters.AddWithValue("@Status", novoStatusProducao);
                                cmd.Parameters.AddWithValue("@Id", linha.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    });

                    await CarReportFinanceiroAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao atualizar status de produção: " + ex.Message);
                }
            }
        }

        private async void TxtValorPagoGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!inicializado || bloquearFiltro) return;

            var txt = sender as TextBox;
            var linha = txt?.DataContext as LinhaFinanceiroModel;

            if (linha != null && txt != null)
            {
                string textoLimpo = txt.Text.Replace(",", ".");
                if (!double.TryParse(textoLimpo, NumberStyles.Any, CultureInfo.InvariantCulture, out double novoValorPago))
                {
                    MessageBox.Show("Por favor, introduza um valor numérico válido.", "Valor Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txt.Text = linha.ValorPago.ToString("F2", CultureInfo.InvariantCulture);
                    return;
                }

                if (Math.Abs(linha.ValorPago - novoValorPago) < 0.001) return;

                try
                {
                    await Task.Run(() =>
                    {
                        using (var conexao = new SqliteConnection(stringConexao))
                        {
                            conexao.Open();
                            string query = "UPDATE Encomendas SET ValorPago = @ValorPago WHERE Id = @Id;";
                            using (var cmd = new SqliteCommand(query, conexao))
                            {
                                cmd.Parameters.AddWithValue("@ValorPago", novoValorPago);
                                cmd.Parameters.AddWithValue("@Id", linha.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    });

                    await CarReportFinanceiroAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao atualizar pagamento: " + ex.Message);
                }
            }
        }
    }

    public class LinhaFinanceiroModel
    {
        public int Id { get; set; }
        public string DataFormatada { get; set; }
        public string NomeCliente { get; set; }
        public string Produto { get; set; }
        public string Status { get; set; }
        public double ValorTotal { get; set; }
        public double ValorPago { get; set; }
        public string ValorPagoInput { get; set; }
        public string Transitiai { get; set; }
        public string SituacaoFinanceira { get; set; }

        public string ValorTotalFormatado => ValorTotal.ToString("C2", new CultureInfo("pt-BR"));
        public string ValorPagoFormatado => ValorPago.ToString("C2", new CultureInfo("pt-BR"));
    }

    // Modelo simples para popular as listas de filtros
    public class FiltroItemModel
    {
        public string Nome { get; set; }
    }
}