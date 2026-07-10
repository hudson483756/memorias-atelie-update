using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace MemoriasAtelie
{
    // Modelo para renderizar apenas os dias ocupados
    public class DiaAgendaItem
    {
        public int Dia { get; set; }
        public int Quantidade { get; set; }
        public string ListaClientes { get; set; } // Texto formatado que vai para o ToolTip
    }

    public partial class TelaAgendaMensal : UserControl
    {
        private int mesFocado;
        private int anoFocado;
        // ALTERADO: Agora puxa o caminho correto e dinâmico diretamente do GerenciadorBanco
        private readonly string stringConexao = GerenciadorBanco.ObterStringConexao();

        // Construtor que recebe de onde viemos
        public TelaAgendaMensal(int mes, int ano, string nomeMes)
        {
            InitializeComponent();
            this.mesFocado = mes;
            this.anoFocado = ano;

            TxtTituloMesAno.Text = $"{nomeMes} de {ano}";
            CarregarDiasOcupados();
        }

        private void CarregarDiasOcupados()
        {
            List<DiaAgendaItem> diasOcupados = new List<DiaAgendaItem>();

            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();

                    // Query adaptada para extrair o dia correto da string no formato yyyy-MM-dd
                    string query = @"
                SELECT 
                    CAST(strftime('%d', Data) AS INTEGER) AS Dia, 
                    COUNT(*) AS Quantidade
                FROM Encomendas 
                WHERE strftime('%m', Data) = @Mes AND strftime('%Y', Data) = @Ano
                GROUP BY Dia;";

                    using (var comando = new SqliteCommand(query, conexao))
                    {
                        comando.Parameters.AddWithValue("@Mes", mesFocado.ToString("D2"));
                        comando.Parameters.AddWithValue("@Ano", anoFocado.ToString());

                        using (var reader = comando.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int dia = reader.GetInt32(0);
                                int qtd = reader.GetInt32(1);

                                diasOcupados.Add(new DiaAgendaItem
                                {
                                    Dia = dia,
                                    Quantidade = qtd,
                                    ListaClientes = $"{qtd} encomenda(s) agendada(s)"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar a agenda mensal: " + ex.Message);
            }

            GridDiasComEncomenda.ItemsSource = diasOcupados;
        }

        private void BtnVoltar_Click(object sender, RoutedEventArgs e)
        {
            // Acessa a MainWindow e manda ela carregar a tela anual de volta
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.AreaConteudo.Content = new TelaAgendaAnual();
            }
        }

        // ADICIONE ESTE MÉTODO EXATAMENTE AQUI:
        private void CardDia_Click(object sender, RoutedEventArgs e)
        {
            var botao = sender as Button;
            if (botao?.DataContext is DiaAgendaItem diaSelecionado)
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    // Abre os detalhes passando os inteiros corretos do dia, mês e ano focados
                    TelaDetalhesDia telaDetalhes = new TelaDetalhesDia(diaSelecionado.Dia, mesFocado, anoFocado, TxtTituloMesAno.Text.Split(' ')[0]);
                    mainWindow.AreaConteudo.Content = telaDetalhes;
                }
            }
        }



    }
}