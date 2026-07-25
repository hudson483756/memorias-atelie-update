using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MemoriasAtelie
{
    public partial class TelaAgendaAnual : UserControl
    {
        private readonly string stringConexao = GerenciadorBanco.ObterStringConexao();
        private int anoAtual = DateTime.Now.Year;
        private int? mesSelecionado = null;

        private readonly string[] nomesMeses = new string[]
        {
            "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
            "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
        };

        public TelaAgendaAnual()
        {
            InitializeComponent();
            CarregarTotaisDoAno();
        }

        private void CarregarTotaisDoAno()
        {
            // Inicializa todos os 12 meses zerados
            var listaMeses = Enumerable.Range(1, 12).Select(i => new ResumoMesModel
            {
                NumeroMes = i,
                NomeMes = nomesMeses[i - 1],
                TotalEntregas = 0
            }).ToList();

            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();

                    // OTIMIZAÇÃO: 1 única consulta agrupadora em vez de 12 consultas separadas
                    string sql = @"
                        SELECT 
                            CAST(strftime('%m', REPLACE(REPLACE(Data, CHAR(13), ''), CHAR(10), ' ')) AS INTEGER) AS Mes, 
                            COUNT(*) AS Total
                        FROM Encomendas 
                        WHERE Data IS NOT NULL AND TRIM(Data) != '' 
                          AND strftime('%Y', REPLACE(REPLACE(Data, CHAR(13), ''), CHAR(10), ' ')) = @ano
                        GROUP BY Mes;";

                    using (var cmd = new SqliteCommand(sql, conexao))
                    {
                        cmd.Parameters.AddWithValue("@ano", anoAtual.ToString());

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    int mes = reader.GetInt32(0);
                                    int total = reader.GetInt32(1);

                                    var item = listaMeses.FirstOrDefault(m => m.NumeroMes == mes);
                                    if (item != null)
                                    {
                                        item.TotalEntregas = total;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar totais da agenda: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            TxtAnoAtual.Text = anoAtual.ToString();
            ItemsMeses.ItemsSource = listaMeses;
        }

        private void CarregarEncomendasDoMes(int mes)
        {
            var listaCards = new List<CardAgendaModel>();
            string mesStr = mes.ToString("D2");

            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();
                    string sql = @"
                        SELECT 
                            e.Id, e.Produto, e.Descricao, e.Valor, e.Status, 
                            e.FotosCaminhos, c.Nome AS NomeCliente, c.Whatsapp AS WhatsappCliente
                        FROM Encomendas e
                        INNER JOIN Clientes c ON e.ClienteId = c.Id
                        WHERE strftime('%m', REPLACE(REPLACE(e.Data, CHAR(13), ''), CHAR(10), ' ')) = @mes 
                          AND strftime('%Y', REPLACE(REPLACE(e.Data, CHAR(13), ''), CHAR(10), ' ')) = @ano
                        ORDER BY e.Data ASC";

                    using (var cmd = new SqliteCommand(sql, conexao))
                    {
                        cmd.Parameters.AddWithValue("@mes", mesStr);
                        cmd.Parameters.AddWithValue("@ano", anoAtual.ToString());

                        using (var reader = cmd.ExecuteReader())
                        {
                            int ordId = reader.GetOrdinal("Id");
                            int ordProduto = reader.GetOrdinal("Produto");
                            int ordDescricao = reader.GetOrdinal("Descricao");
                            int ordValor = reader.GetOrdinal("Valor");
                            int ordStatus = reader.GetOrdinal("Status");
                            int ordFotos = reader.GetOrdinal("FotosCaminhos");
                            int ordCliente = reader.GetOrdinal("NomeCliente");
                            int ordWhatsapp = reader.GetOrdinal("WhatsappCliente");

                            while (reader.Read())
                            {
                                listaCards.Add(new CardAgendaModel
                                {
                                    Id = reader.GetInt32(ordId),
                                    Produto = reader.IsDBNull(ordProduto) ? string.Empty : reader.GetString(ordProduto),
                                    Descricao = reader.IsDBNull(ordDescricao) ? string.Empty : reader.GetString(ordDescricao),
                                    Valor = reader.IsDBNull(ordValor) ? 0.0 : reader.GetDouble(ordValor),
                                    Status = reader.IsDBNull(ordStatus) ? string.Empty : reader.GetString(ordStatus),
                                    NomeCliente = reader.IsDBNull(ordCliente) ? string.Empty : reader.GetString(ordCliente),
                                    WhatsappCliente = reader.IsDBNull(ordWhatsapp) ? string.Empty : reader.GetString(ordWhatsapp),
                                    NomeFoto = reader.IsDBNull(ordFotos) ? null : reader.GetString(ordFotos)
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar encomendas do mês: " + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            TxtSemEncomendas.Visibility = listaCards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            ItemsEncomendas.ItemsSource = listaCards;
        }

        // --- Navegação e Eventos ---

        private void BtnAnoAnterior_Click(object sender, RoutedEventArgs e)
        {
            anoAtual--;
            CarregarTotaisDoAno();
            if (mesSelecionado.HasValue)
            {
                CarregarEncomendasDoMes(mesSelecionado.Value);
            }
        }

        private void BtnProximoAno_Click(object sender, RoutedEventArgs e)
        {
            anoAtual++;
            CarregarTotaisDoAno();
            if (mesSelecionado.HasValue)
            {
                CarregarEncomendasDoMes(mesSelecionado.Value);
            }
        }

        private void CardMes_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is Border border && border.DataContext is ResumoMesModel mesModel)
                {
                    mesSelecionado = mesModel.NumeroMes;
                    TxtMesAnoSelecionado.Text = $"{mesModel.NomeMes} de {anoAtual}";

                    PanelNavegacaoAno.Visibility = Visibility.Collapsed;
                    ScrollViewerAnual.Visibility = Visibility.Collapsed;

                    PanelNavegacaoMes.Visibility = Visibility.Visible;
                    GridMensal.Visibility = Visibility.Visible;

                    CarregarEncomendasDoMes(mesModel.NumeroMes);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir o mês:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnVoltarAno_Click(object sender, RoutedEventArgs e)
        {
            mesSelecionado = null;

            PanelNavegacaoMes.Visibility = Visibility.Collapsed;
            GridMensal.Visibility = Visibility.Collapsed;

            PanelNavegacaoAno.Visibility = Visibility.Visible;
            ScrollViewerAnual.Visibility = Visibility.Visible;

            CarregarTotaisDoAno();
        }

        private void CardEncomenda_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is CardAgendaModel card)
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    // Pega o número do mês selecionado ou o mês atual como fallback
                    int mes = mesSelecionado ?? DateTime.Now.Month;
                    string nomeMes = nomesMeses[mes - 1];

                    // Instancia a TelaVisualizarEncomenda passando os parâmetros esperados pelo construtor
                    // (id, dia: 1, mes, ano, nomeMes, origem: "Agenda")
                    TelaVisualizarEncomenda telaVisualizar = new TelaVisualizarEncomenda(
                        card.Id,
                        1,
                        mes,
                        anoAtual,
                        nomeMes,
                        "Agenda"
                    );

                    // Carrega a tela no container principal da aplicação
                    mainWindow.AreaConteudo.Content = telaVisualizar;
                }
            }
        }


    }
}