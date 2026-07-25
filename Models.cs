using System;
using System.IO;
using System.Linq;

namespace MemoriasAtelie
{
    public class ResumoMesModel
    {
        public int NumeroMes { get; set; }
        public string NomeMes { get; set; }
        public int TotalEntregas { get; set; }
        public string TotalFormatado => $"{TotalEntregas} entregas";
    }

    public class CardAgendaModel
    {
        public int Id { get; set; }
        public string Produto { get; set; }
        public string Descricao { get; set; }
        public double Valor { get; set; }
        public string Status { get; set; }
        public string NomeCliente { get; set; }
        public string WhatsappCliente { get; set; }
        public string NomeFoto { get; set; }

        public bool TemImagem => !string.IsNullOrEmpty(CaminhoImagemCompleto);

        // CORREÇÃO: Propriedades diretas para controle de Visibilidade no WPF sem precisar de Converter
        public System.Windows.Visibility VisibilidadeImagem => TemImagem ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        public System.Windows.Visibility VisibilidadePlaceholder => TemImagem ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;

        // Constrói o caminho buscando na pasta oficial do Ateliê no Windows
        public string CaminhoImagemCompleto
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NomeFoto)) return null;

                string primeiroNome = NomeFoto.Split(';').FirstOrDefault();
                if (string.IsNullOrWhiteSpace(primeiroNome)) return null;

                // Suporte para retrocompatibilidade caso existam caminhos legados completos (C:\...)
                if (File.Exists(primeiroNome)) return primeiroNome;

                string pastaFotos = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "MemoriasAtelie", "Fotos", "Memorias", primeiroNome);

                return File.Exists(pastaFotos) ? pastaFotos : null;
            }
        }
    }
}