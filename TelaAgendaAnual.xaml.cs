using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.Sqlite;

namespace MemoriasAtelie
{
    // Modelo de dados para renderizar cada quadradinho de mês
    public class MesAgendaItem
    {
        public int NumeroMes { get; set; }
        public string NomeExtenso { get; set; }
        public int TotalEncomendas { get; set; }
    }

    public partial class TelaAgendaAnual : UserControl
    {
        private int anoAtualFoco = DateTime.Now.Year;
        // ALTERADO: Agora puxa o caminho correto e dinâmico diretamente do GerenciadorBanco
        private readonly string stringConexao = GerenciadorBanco.ObterStringConexao();

        public TelaAgendaAnual()
        {
            InitializeComponent();
            TxtAno.Text = anoAtualFoco.ToString();
            CarregarDadosDoAno();
        }

        private void CarregarDadosDoAno()
        {
            List<MesAgendaItem> listaMeses = new List<MesAgendaItem>();
            CultureInfo culturaPtBr = new CultureInfo("pt-BR");

            // Criamos a lista base com os 12 meses do ano
            for (int i = 1; i <= 12; i++)
            {
                string nomeMes = culturaPtBr.DateTimeFormat.GetMonthName(i);
                // Primeira letra em maiúscula
                nomeMes = culturaPtBr.TextInfo.ToTitleCase(nomeMes);

                listaMeses.Add(new MesAgendaItem
                {
                    NumeroMes = i,
                    NomeExtenso = nomeMes,
                    TotalEncomendas = ObterTotalEncomendasDoMes(i, anoAtualFoco)
                });
            }

            GridMeses.ItemsSource = listaMeses;
        }

        // Faz uma query rápida no SQLite contando os registros daquele mês/ano
        // Faz uma query rápida no SQLite contando os registros daquele mês/ano
        private int ObterTotalEncomendasDoMes(int mes, int ano)
        {
            int total = 0;

            try
            {
                using (var conexao = new SqliteConnection(stringConexao))
                {
                    conexao.Open();

                    // CORRIGIDO: Alterado de 'DataEntrega' para 'Data'
                    string query = @"SELECT COUNT(*) FROM Encomendas 
                             WHERE strftime('%m', Data) = @Mes 
                             AND strftime('%Y', Data) = @Ano;";

                    using (var comando = new SqliteCommand(query, conexao))
                    {
                        // Formata o mês com dois dígitos (ex: 5 -> "05")
                        comando.Parameters.AddWithValue("@Mes", mes.ToString("D2"));
                        comando.Parameters.AddWithValue("@Ano", ano.ToString());

                        total = Convert.ToInt32(comando.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                // Dica de depuração: Se ainda assim der zero, descomente a linha abaixo temporariamente para ver o erro real:
                // System.Windows.MessageBox.Show("Erro na contagem: " + ex.Message);
                total = 0;
            }

            return total;
        }

        private void BtnAnoAnterior_Click(object sender, RoutedEventArgs e)
        {
            anoAtualFoco--;
            TxtAno.Text = anoAtualFoco.ToString();
            CarregarDadosDoAno();
        }

        private void BtnProximoAno_Click(object sender, RoutedEventArgs e)
        {
            anoAtualFoco++;
            TxtAno.Text = anoAtualFoco.ToString();
            CarregarDadosDoAno();
        }

        // Quando clicar em um mês, navega para os dias dele
        private void CardMes_Click(object sender, RoutedEventArgs e)
        {
            var botao = sender as Button;
            if (botao?.DataContext is MesAgendaItem mesSelecionado)
            {
                if (Window.GetWindow(this) is MainWindow mainWindow)
                {
                    // Note que aqui usamos 'anoAtualFoco', que existe nesta classe!
                    TelaAgendaMensal telaMensal = new TelaAgendaMensal(mesSelecionado.NumeroMes, anoAtualFoco, mesSelecionado.NomeExtenso);
                    mainWindow.AreaConteudo.Content = telaMensal;
                }
            }
        }


    }
}