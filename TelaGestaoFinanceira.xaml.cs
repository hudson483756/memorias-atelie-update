using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MemoriasAtelie
{
    public partial class TelaGestaoFinanceira : UserControl
    {
        private readonly string connectionString = "Data Source=memorias.db";

        // CORRIGIDO: Nome da variável agora está correto e sem espaços
        private bool _telaCarregada = false;

        public ObservableCollection<LinhaFinanceiroModel> ListaFinanceiro { get; set; } = new ObservableCollection<LinhaFinanceiroModel>();
        public ObservableCollection<MesPendenteModel> ListaAReceberMeses { get; set; } = new ObservableCollection<MesPendenteModel>();
        public ObservableCollection<MesPendenteModel> ListaAlertaMeses { get; set; } = new ObservableCollection<MesPendenteModel>();

        public TelaGestaoFinanceira()
        {
            InitializeComponent();
            DataContext = this;

            DgFinanceiro.ItemsSource = ListaFinanceiro;
            LstAReceberMeses.ItemsSource = ListaAReceberMeses;
            LstAlertaMeses.ItemsSource = ListaAlertaMeses;

            Loaded += TelaGestaoFinanceira_Loaded;
        }

        private async void TelaGestaoFinanceira_Loaded(object sender, RoutedEventArgs e)
        {
            _telaCarregada = false;

            try
            {
                ConfigurarFiltrosIniciais();
                await CarregarFiltrosDinamicosAsync();

                // Indica que os filtros iniciais terminaram e a tela está estável
                _telaCarregada = true;

                // Roda o primeiro relatório com segurança
                await CarReportFinanceiroAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro crítico ao inicializar a tela: {ex.Message}\n\n{ex.StackTrace}", "Erro de Inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigurarFiltrosIniciais()
        {
            if (CboMes == null || CboAno == null) return;

            CboMes.Items.Clear();
            CboMes.Items.Add(new ComboBoxItem { Content = "Todos os Meses", Tag = "Todos" });
            string[] meses = { "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho", "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro" };
            for (int i = 0; i < meses.Length; i++)
            {
                CboMes.Items.Add(new ComboBoxItem { Content = meses[i], Tag = (i + 1).ToString("D2") });
            }
            CboMes.SelectedIndex = 0;

            CboAno.Items.Clear();
            int anoAtual = DateTime.Now.Year;
            for (int i = anoAtual - 2; i <= anoAtual + 2; i++)
            {
                CboAno.Items.Add(i.ToString());
            }
            CboAno.SelectedItem = anoAtual.ToString();
        }

        private async Task CarregarFiltrosDinamicosAsync()
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                await conn.OpenAsync();

                // Clientes
                var cmdClientes = new SqliteCommand("SELECT DISTINCT Nome FROM Clientes ORDER BY Nome", conn);
                var listaClientes = new List<ClienteFiltroModel> { new ClienteFiltroModel { Nome = "Todos os Clientes" } };
                using (var reader = await cmdClientes.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        listaClientes.Add(new ClienteFiltroModel { Nome = reader.GetString(0) });
                    }
                }
                if (CboBuscarCliente != null)
                {
                    CboBuscarCliente.ItemsSource = listaClientes;
                    CboBuscarCliente.SelectedIndex = 0;
                }

                // Produtos
                var cmdProdutos = new SqliteCommand("SELECT DISTINCT Nome FROM Produtos ORDER BY Nome", conn);
                var listaProdutos = new List<ProdutoFiltroModel> { new ProdutoFiltroModel { Nome = "Todos os Produtos" } };
                using (var reader = await cmdProdutos.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        listaProdutos.Add(new ProdutoFiltroModel { Nome = reader.GetString(0) });
                    }
                }
                if (CboBuscarProduto != null)
                {
                    CboBuscarProduto.ItemsSource = listaProdutos;
                    CboBuscarProduto.SelectedIndex = 0;
                }
            }
        }

        public async Task CarReportFinanceiroAsync()
        {
            // Bloqueia execuções indesejadas antes da hora
            if (!_telaCarregada) return;

            string mesSelecionado = (CboMes?.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Todos";
            string anoSelecionado = CboAno?.SelectedItem?.ToString() ?? DateTime.Now.Year.ToString();
            string situacaoFiltro = (CboSituacaoPgto?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";
            string clienteFiltro = (CboBuscarCliente?.SelectedItem as ClienteFiltroModel)?.Nome ?? "Todos os Clientes";
            string produtoFiltro = (CboBuscarProduto?.SelectedItem as ProdutoFiltroModel)?.Nome ?? "Todos os Produtos";
            string statusProdFiltro = (CboStatusProdFiltro?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Todos";

            double totalFaturadoAno = 0;
            double totalFaturadoMes = 0;
            double totalAReceberGeral = 0;
            double totalAlertaGeral = 0;

            var dadosMesesPendente = new Dictionary<string, double>();
            var dadosMesesAlerta = new Dictionary<string, double>();

            ListaFinanceiro.Clear();

            try
            {
                using (var conn = new SqliteConnection(connectionString))
                {
                    await conn.OpenAsync();

                    string query = @"SELECT E.Id, E.Data, C.Nome AS NomeCliente, E.Produto, E.Status, E.Valor, E.ValorPago 
                                     FROM Encomendas E
                                     INNER JOIN Clientes C ON E.ClienteId = C.Id
                                     WHERE E.Data LIKE @AnoFiltro";

                    var cmd = new SqliteCommand(query, conn);
                    cmd.Parameters.AddWithValue("@AnoFiltro", $"{anoSelecionado}%");

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            long id = reader.GetInt64(0);
                            string dataRaw = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            string nomeCliente = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            string produto = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            string statusPeca = reader.IsDBNull(4) ? "Pendente" : reader.GetString(4);
                            double valorTotal = reader.IsDBNull(5) ? 0 : reader.GetDouble(5);
                            double valorPago = reader.IsDBNull(6) ? 0 : reader.GetDouble(6);

                            if (!string.IsNullOrWhiteSpace(statusPeca))
                            {
                                statusPeca = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(statusPeca.Trim().ToLower());
                            }

                            string mesItem = "01";
                            if (dataRaw.Length >= 7) mesItem = dataRaw.Substring(5, 2);

                            string situacao = "Não Pago";
                            if (valorPago >= valorTotal && valorTotal > 0) situacao = "Pago";
                            else if (valorPago > 0 && valorPago < valorTotal) situacao = "Parcial";

                            double aReceberItem = valorTotal - valorPago;
                            if (aReceberItem < 0) aReceberItem = 0;

                            totalFaturadoAno += valorTotal;
                            if (mesItem == mesSelecionado || mesSelecionado == "Todos")
                            {
                                if (situacao == "Pago" || situacao == "Parcial")
                                {
                                    totalFaturadoMes += valorPago;
                                }
                            }

                            if (situacao != "Pago")
                            {
                                totalAReceberGeral += aReceberItem;

                                if (dadosMesesPendente.ContainsKey(mesItem)) dadosMesesPendente[mesItem] += aReceberItem;
                                else dadosMesesPendente[mesItem] = aReceberItem;

                                if (statusPeca == "Entregue" || statusPeca == "Concluído")
                                {
                                    totalAlertaGeral += aReceberItem;

                                    if (dadosMesesAlerta.ContainsKey(mesItem)) dadosMesesAlerta[mesItem] += aReceberItem;
                                    else dadosMesesAlerta[mesItem] = aReceberItem;
                                }
                            }

                            if (mesSelecionado != "Todos" && mesItem != mesSelecionado) continue;
                            if (situacaoFiltro != "Todos" && situacao != situacaoFiltro) continue;
                            if (clienteFiltro != "Todos os Clientes" && nomeCliente != clienteFiltro) continue;
                            if (produtoFiltro != "Todos os Produtos" && produto != produtoFiltro) continue;
                            if (statusProdFiltro != "Todos" && statusPeca != statusProdFiltro) continue;

                            ListaFinanceiro.Add(new LinhaFinanceiroModel
                            {
                                Id = id,
                                DataRaw = dataRaw,
                                NomeCliente = nomeCliente,
                                Produto = produto,
                                Status = statusPeca,
                                ValorTotal = valorTotal,
                                ValorPago = valorPago,
                                ValorPagoInput = valorPago.ToString("F2")
                            });
                        }
                    }
                }

                if (TxtTotalFaturadoAno != null) TxtTotalFaturadoAno.Text = totalFaturadoAno.ToString("C", CultureInfo.CurrentCulture);
                if (TxtTotalFaturadoMes != null) TxtTotalFaturadoMes.Text = totalFaturadoMes.ToString("C", CultureInfo.CurrentCulture);
                if (TxtTotalAReceberGeral != null) TxtTotalAReceberGeral.Text = totalAReceberGeral.ToString("C", CultureInfo.CurrentCulture);
                if (TxtTotalAlertaGeral != null) TxtTotalAlertaGeral.Text = totalAlertaGeral.ToString("C", CultureInfo.CurrentCulture);

                MontarListaMeses(dadosMesesPendente, ListaAReceberMeses);
                MontarListaMeses(dadosMesesAlerta, ListaAlertaMeses);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao processar relatório no banco: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MontarListaMeses(Dictionary<string, double> dicionario, ObservableCollection<MesPendenteModel> listaAlvo)
        {
            listaAlvo.Clear();
            var mesesOrdenados = new List<string>(dicionario.Keys);
            mesesOrdenados.Sort();

            string[] mesesAbv = { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" };

            foreach (var mesNum in mesesOrdenados)
            {
                int idx = int.Parse(mesNum) - 1;
                if (idx >= 0 && idx < 12 && dicionario[mesNum] > 0)
                {
                    listaAlvo.Add(new MesPendenteModel
                    {
                        MesAbv = mesesAbv[idx],
                        Valor = dicionario[mesNum]
                    });
                }
            }
        }

        private async void Filtro_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!_telaCarregada) return;

            await CarReportFinanceiroAsync();
        }

        private async void CboStatusProducaoGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_telaCarregada) return;

            if (sender is ComboBox cb && cb.DataContext is LinhaFinanceiroModel linha)
            {
                var item = cb.SelectedItem as ComboBoxItem;
                string novoStatus = item?.Content.ToString();

                if (string.IsNullOrEmpty(novoStatus) || novoStatus == linha.Status) return;

                linha.Status = novoStatus;

                try
                {
                    using (var conn = new SqliteConnection(connectionString))
                    {
                        await conn.OpenAsync();
                        var cmd = new SqliteCommand("UPDATE Encomendas SET Status = @Status WHERE Id = @Id", conn);
                        cmd.Parameters.AddWithValue("@Status", novoStatus);
                        cmd.Parameters.AddWithValue("@Id", linha.Id);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    await CarReportFinanceiroAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao salvar status: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void TxtValorPagoGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is LinhaFinanceiroModel linha)
            {
                var binding = tb.GetBindingExpression(TextBox.TextProperty);
                binding?.UpdateSource();

                if (double.TryParse(tb.Text, out double novoValorPago))
                {
                    if (novoValorPago == linha.ValorPago) return;

                    linha.ValorPago = novoValorPago;

                    try
                    {
                        using (var conn = new SqliteConnection(connectionString))
                        {
                            await conn.OpenAsync();
                            var cmd = new SqliteCommand("UPDATE Encomendas SET ValorPago = @ValorPago WHERE Id = @Id", conn);
                            cmd.Parameters.AddWithValue("@ValorPago", novoValorPago);
                            cmd.Parameters.AddWithValue("@Id", linha.Id);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        await CarReportFinanceiroAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao salvar valor pago: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Valor numérico inválido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    tb.Text = linha.ValorPago.ToString("F2");
                }
            }
        }
    }

    public class LinhaFinanceiroModel
    {
        public long Id { get; set; }
        public string DataRaw { get; set; }
        public string NomeCliente { get; set; }
        public string Produto { get; set; }
        public string Status { get; set; }
        public double ValorTotal { get; set; }
        public double ValorPago { get; set; }
        public string ValorPagoInput { get; set; }

        public string DataFormatada
        {
            get
            {
                if (DateTime.TryParse(DataRaw, out DateTime dt)) return dt.ToString("dd/MM/yyyy");
                return DataRaw;
            }
        }

        public string ValorTotalFormatado => ValorTotal.ToString("C", CultureInfo.CurrentCulture);
        public string SituacaoFinanceira
        {
            get
            {
                if (ValorPago >= ValorTotal && ValorTotal > 0) return "Pago";
                if (ValorPago > 0 && ValorPago < ValorTotal) return "Parcial";
                return "Não Pago";
            }
        }
    }

    public class MesPendenteModel
    {
        public string MesAbv { get; set; }
        public double Valor { get; set; }
        public string ValorFormatado => Valor.ToString("C", CultureInfo.CurrentCulture);
    }

    public class ClienteFiltroModel { public string Nome { get; set; } }
    public class ProdutoFiltroModel { public string Nome { get; set; } }
}