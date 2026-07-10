using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace MemoriasAtelie
{
    public partial class TelaConsultaEncomendas : UserControl
    {
        // ALTERADO: Agora puxa o caminho correto e dinâmico diretamente do GerenciadorBanco
        private readonly string stringConexao = GerenciadorBanco.ObterStringConexao();

        public TelaConsultaEncomendas()
        {
            InitializeComponent();
            InicializarFiltros();
            ExecutarBuscaFiltrada();
        }

        private void InicializarFiltros()
        {
            int anoAtual = DateTime.Now.Year;
            CboFiltroAno.Items.Clear();
            CboFiltroAno.Items.Add("Todos");
            for (int i = 0; i < 5; i++)
            {
                CboFiltroAno.Items.Add((anoAtual - i).ToString());
            }
            CboFiltroAno.SelectedIndex = 0;

            var listaClientes = new List<ClienteItem> { new ClienteItem { Id = -1, Nome = "Todos" } };
            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();
                    string query = "SELECT Id, Nome FROM Clientes ORDER BY Nome ASC;";
                    using (var cmd = new SqliteCommand(query, conexao))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            listaClientes.Add(new ClienteItem
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Nome = reader["Nome"].ToString()
                            });
                        }
                    }
                }
                CboFiltroCliente.ItemsSource = listaClientes;
                CboFiltroCliente.SelectedIndex = 0;
            }
            catch { }

            var listaProdutos = new List<string> { "Todos" };
            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();
                    string query = "SELECT Nome FROM Produtos ORDER BY Nome ASC;";
                    using (var cmd = new SqliteCommand(query, conexao))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) listaProdutos.Add(reader.GetString(0));
                    }
                }
                CboFiltroProduto.ItemsSource = listaProdutos;
                CboFiltroProduto.SelectedIndex = 0;
            }
            catch { }
        }

        private void ExecutarBuscaFiltrada()
        {
            var detalhados = new List<LinhaConsultaModel>();

            string queryBase = @"SELECT e.Id, e.Data, c.Nome AS ClienteNome, e.Produto, e.Descricao, e.FotosCaminhos, e.Valor, e.Status 
                                 FROM Encomendas e
                                 INNER JOIN Clientes c ON e.ClienteId = c.Id
                                 WHERE 1=1";

            using (var conexao = new SqliteConnection(stringConexao))
            {
                using (var comando = new SqliteCommand("", conexao))
                {
                    if (CboFiltroMes.SelectedItem is ComboBoxItem mesSelecionado && mesSelecionado.Tag != null)
                    {
                        queryBase += " AND strftime('%m', e.Data) = @Mes";
                        comando.Parameters.AddWithValue("@Mes", mesSelecionado.Tag.ToString());
                    }

                    if (CboFiltroAno.SelectedItem != null && CboFiltroAno.SelectedItem.ToString() != "Todos")
                    {
                        queryBase += " AND strftime('%Y', e.Data) = @Ano";
                        comando.Parameters.AddWithValue("@Ano", CboFiltroAno.SelectedItem.ToString());
                    }

                    if (CboFiltroCliente.SelectedValue != null && (int)CboFiltroCliente.SelectedValue != -1)
                    {
                        queryBase += " AND e.ClienteId = @ClienteId";
                        comando.Parameters.AddWithValue("@ClienteId", CboFiltroCliente.SelectedValue);
                    }

                    if (CboFiltroProduto.SelectedItem != null && CboFiltroProduto.SelectedItem.ToString() != "Todos")
                    {
                        queryBase += " AND e.Produto = @ProdutoNome";
                        comando.Parameters.AddWithValue("@ProdutoNome", CboFiltroProduto.SelectedItem.ToString());
                    }

                    // FILTRO DE STATUS ATUALIZADO
                    if (CboFiltroStatus.SelectedItem is ComboBoxItem statusSelecionado && statusSelecionado.Content.ToString() != "Todos")
                    {
                        queryBase += " AND e.Status = @Status";
                        comando.Parameters.AddWithValue("@Status", statusSelecionado.Content.ToString());
                    }

                    queryBase += " ORDER BY e.Data DESC;";

                    try
                    {
                        comando.CommandText = queryBase;
                        conexao.Open();

                        using (var reader = comando.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string dataOriginal = reader["Data"].ToString();
                                string dataFormatada = dataOriginal;

                                if (DateTime.TryParse(dataOriginal, out DateTime dt))
                                {
                                    dataFormatada = dt.ToString("dd/MM/yyyy");
                                }

                                detalhados.Add(new LinhaConsultaModel
                                {
                                    Id = Convert.ToInt32(reader["Id"]),
                                    DataFormatada = dataFormatada,
                                    NomeCliente = reader["ClienteNome"].ToString(),
                                    Produto = reader["Produto"].ToString(),
                                    Descricao = reader["Descricao"].ToString(),
                                    FotosCaminhos = reader["FotosCaminhos"] != DBNull.Value ? reader["FotosCaminhos"].ToString() : "",
                                    Valor = reader["Valor"] != DBNull.Value ? Convert.ToDouble(reader["Valor"]) : 0.0,
                                    Status = reader["Status"] != DBNull.Value ? reader["Status"].ToString() : "Pendente"
                                });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erro ao processar consulta: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            var listaAgrupada = detalhados
                .GroupBy(x => x.Produto)
                .Select(g => new LinhaAgrupadaModel
                {
                    Produto = g.Key,
                    QuantidadeTotal = g.Count(),
                    FaturamentoTotal = g.Sum(x => x.Valor)
                })
                .OrderBy(x => x.Produto)
                .ToList();

            DgAgrupado.ItemsSource = listaAgrupada;
            DgResultados.ItemsSource = detalhados;

            TxtTotalPecas.Text = detalhados.Count.ToString();
            double faturamentoGlobal = detalhados.Sum(x => x.Valor);
            TxtFaturamentoTotal.Text = faturamentoGlobal.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
        }

        private void BtnVisualizarFotos_Click(object sender, RoutedEventArgs e)
        {
            if (DgResultados.SelectedItem is LinhaConsultaModel selecionada)
            {
                MainWindow principal = Window.GetWindow(this) as MainWindow;
                if (principal != null)
                {
                    // ADICIONADO: O parâmetro "Consulta" no final
                    var telaFotos = new TelaVisualizarEncomenda(selecionada.Id, 0, 0, 0, "", "Consulta");
                    principal.AreaConteudo.Content = telaFotos;
                }
            }
            else
            {
                MessageBox.Show("Por favor, selecione uma linha na aba 'Histórico Detalhado' para ver a galeria de imagens.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e) => ExecutarEdicao();

        private void DgResultados_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) => ExecutarEdicao();

        private void ExecutarEdicao()
        {
            if (DgResultados.SelectedItem is LinhaConsultaModel selecionada)
            {
                MainWindow principal = Window.GetWindow(this) as MainWindow;
                if (principal != null)
                {
                    var telaEdicao = new TelaCadastroEncomenda(selecionada);
                    principal.AreaConteudo.Content = telaEdicao;
                }
            }
            else
            {
                MessageBox.Show("Por favor, selecione uma linha na aba 'Histórico Detalhado' para poder editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnFiltrar_Click(object sender, RoutedEventArgs e) => ExecutarBuscaFiltrada();

        private void BtnLimpar_Click(object sender, RoutedEventArgs e)
        {
            CboFiltroMes.SelectedIndex = 0;
            CboFiltroAno.SelectedIndex = 0;
            CboFiltroCliente.SelectedIndex = 0;
            CboFiltroProduto.SelectedIndex = 0;
            CboFiltroStatus.SelectedIndex = 0; // Reseta o combo de status também
            ExecutarBuscaFiltrada();
        }
    }
}