using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MemoriasAtelie
{
    public class LinhaConsultaModel
    {
        public int Id { get; set; }
        public string DataFormatada { get; set; }
        public string NomeCliente { get; set; }
        public string Produto { get; set; }
        public string Descricao { get; set; }
        public string FotosCaminhos { get; set; } // Armazena apenas os nomes dos arquivos separados por ';'
        public double Valor { get; set; }
        public double ValorPago { get; set; }
        public string Status { get; set; }

        public string ValorFormatado => Valor.ToString("C2", new CultureInfo("pt-BR"));
        public string ValorPagoFormatado => ValorPago.ToString("C2", new CultureInfo("pt-BR"));

        // Monta e valida o caminho físico na pasta sincronizada com base no nome gravado no banco
        public string PrimeiraFoto
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FotosCaminhos)) return null;

                string primeiroNomeArquivo = FotosCaminhos.Split(';').FirstOrDefault();
                if (string.IsNullOrWhiteSpace(primeiroNomeArquivo)) return null;

                string pastaDocumentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string caminhoCompleto = Path.Combine(pastaDocumentos, "MemoriasAtelie", "Fotos", "Memorias", primeiroNomeArquivo);

                return File.Exists(caminhoCompleto) ? caminhoCompleto : null;
            }
        }
    }
}