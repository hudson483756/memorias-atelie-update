using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MemoriasAtelie
{
    // Modelo para renderizar os dias ocupados na agenda
    public class DiaAgendaItem
    {
        public int Dia { get; set; }
        public int Quantidade { get; set; }
        public string ListaClientes { get; set; } = string.Empty; // Evita aviso CS8618
    }

    public partial class TelaAgendaMensal : UserControl
    {
        private readonly int mesFocado;
        private readonly int anoFocado;
        private readonly string nomeMesFocado;
        private readonly string stringConexao = GerenciadorBanco.ObterStringConexao();

        // Construtor
        public TelaAgendaMensal(int mes, int ano, string nomeMes)
        {
            InitializeComponent();
            this.mesFocado = mes;
            this.anoFocado = ano;
            this.nomeMesFocado = string.IsNullOrWhiteSpace(nomeMes) ? "Mês" : nomeMes;

            TxtTituloMesAno.Text = $"{this.nomeMesFocado} de {ano}";

            // Adiciona o carregamento seguro após o componente ser renderizado
            this.Loaded += TelaAgendaMensal_Loaded;
        }

        private void TelaAgendaMensal_Loaded(object sender, RoutedEventArgs e)
        {
            CarregarDiasOcupados();
        }

        private void CarregarDiasOcupados()
        {
            var diasOcupados = new List<DiaAgendaItem>();

            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();

                    string query = @"
                        SELECT Data 
                        FROM Encomendas 
                        WHERE Data IS NOT NULL AND TRIM(Data) != '';";

                    var contagemDias = new Dictionary<int, int>();

                    using (var comando = new SqliteCommand(query, conexao))
                    using (var reader = comando.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader.IsDBNull(0)) continue;

                            string dataBruta = reader.GetString(0);

                            // TRATAMENTO PREVENTIVO:
                            // Limpa caracteres de quebra de linha (\r, \n) e pega apenas a parte da data
                            string dataLimpa = dataBruta.Replace("\r", "").Replace("\n", " ").Trim();

                            if (dataLimpa.Contains(" "))
                            {
                                dataLimpa = dataLimpa.Split(' ')[0];
                            }

                            if (DateTime.TryParse(dataLimpa, out DateTime dataConvertida))
                            {
                                if (dataConvertida.Month == mesFocado && dataConvertida.Year == anoFocado)
                                {
                                    int dia = dataConvertida.Day;
                                    if (contagemDias.ContainsKey(dia))
                                        contagemDias[dia]++;
                                    else
                                        contagemDias[dia] = 1;
                                }
                            }
                        }
                    }

                    foreach (var par in contagemDias)
                    {
                        diasOcupados.Add(new DiaAgendaItem
                        {
                            Dia = par.Key,
                            Quantidade = par.Value,
                            ListaClientes = $"{par.Value} encomenda(s) agendada(s)"
                        });
                    }
                }

                GridDiasComEncomenda.ItemsSource = diasOcupados;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar a agenda mensal:\n{ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnVoltar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    mainWindow.AreaConteudo.Content = new TelaAgendaAnual();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao navegar para a tela anual:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CardDia_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var botao = sender as Button;
                if (botao?.DataContext is DiaAgendaItem diaSelecionado)
                {
                    if (Window.GetWindow(this) is MainWindow mainWindow)
                    {
                        TelaDetalhesDia telaDetalhes = new TelaDetalhesDia(diaSelecionado.Dia, mesFocado, anoFocado, nomeMesFocado);
                        mainWindow.AreaConteudo.Content = telaDetalhes;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir detalhes do dia:\n{ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}