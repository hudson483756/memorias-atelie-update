using System;
using System.Collections.Generic;

namespace MemoriasAtelie
{
    public class LinhaAgrupadaModel
    {
        public string ChaveAgrupamento { get; set; } // Nome do Cliente ou Data
        public List<LinhaConsultaModel> Encomendas { get; set; } = new List<LinhaConsultaModel>();
        public double ValorTotalGrupo { get; set; }
        public int QuantidadeTotal { get; set; }

        // PROPRIEDADES CORRIGIDAS:
        public string Produto { get; set; }
        public double FaturamentoTotal { get; set; }
        public string Status { get; set; }
    }
}