using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;
namespace MemoriasAtelie
{
    public class EncomendaDetalheItem
    {
        public int Id { get; set; }
        public string NomeCliente { get; set; }
        public string Produto { get; set; }
        public decimal Valor { get; set; }
        public string Status { get; set; }

        public string ValorFormatado => Valor.ToString("C2");
        public string CorStatus => Status?.ToLower() == "concluído" ? "#4CAF50" : (Status?.ToLower() == "em produção" ? "#2196F3" : "#FF9800");
    }

    public partial class TelaDetalhesDia : UserControl
    {
        private int diaFocado;
        private int mesFocado;
        private int anoFocado;
        private string nomeMesFocado;
        private string stringConexao = "Data Source=memorias.db";

        public TelaDetalhesDia(int dia, int mes, int ano, string nomeMes)
        {
            InitializeComponent();
            this.diaFocado = dia;
            this.mesFocado = mes;
            this.anoFocado = ano;
            this.nomeMesFocado = nomeMes;

            TxtTituloDia.Text = $"Entregas do Dia {dia} de {nomeMes}";
            BuscarEncomendasDoDia();
        }

        private void BuscarEncomendasDoDia()
        {
            List<EncomendaDetalheItem> listaPedidos = new List<EncomendaDetalheItem>();

            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();

                    string query = @"SELECT e.Id, c.Nome, e.Produto 
                             FROM Encomendas e
                             INNER JOIN Clientes c ON e.ClienteId = c.Id
                             WHERE CAST(strftime('%d', e.Data) AS INT) = @Dia 
                               AND CAST(strftime('%m', e.Data) AS INT) = @Mes 
                               AND CAST(strftime('%Y', e.Data) AS INT) = @Ano;";

                    using (var comando = new SqliteCommand(query, conexao))
                    {
                        comando.Parameters.AddWithValue("@Dia", diaFocado);
                        comando.Parameters.AddWithValue("@Mes", mesFocado);
                        comando.Parameters.AddWithValue("@Ano", anoFocado);

                        using (var reader = comando.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                listaPedidos.Add(new EncomendaDetalheItem
                                {
                                    Id = reader.GetInt32(0),
                                    NomeCliente = reader.GetString(1),
                                    Produto = reader.GetString(2),
                                    Valor = 0.00m,
                                    Status = "Em Produção"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar do banco: " + ex.Message, "Erro de Consulta", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ListaEncomendasDia.ItemsSource = listaPedidos;
        }

        private void CardEncomenda_Click(object sender, RoutedEventArgs e)
        {
            var botao = sender as Button;
            if (botao?.DataContext is EncomendaDetalheItem pedidoSelecionado)
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    // CORREÇÃO AQUI: Adicionado o sexto parâmetro "Agenda" no construtor
                    TelaVisualizarEncomenda telaPedido = new TelaVisualizarEncomenda(
                        pedidoSelecionado.Id, diaFocado, mesFocado, anoFocado, nomeMesFocado, "Agenda"
                    );
                    mainWindow.AreaConteudo.Content = telaPedido;
                }
            }
        }

        private void BtnVoltar_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.AreaConteudo.Content = new TelaAgendaMensal(mesFocado, anoFocado, nomeMesFocado);
            }
        }
    }
}