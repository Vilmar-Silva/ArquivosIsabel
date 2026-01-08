using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isabel_Visualizador_Proj
{
    public class Trecho
    {
        public int Id { get; set; }                     // TRC_ID
        public int PosteOrigemId { get; set; }          // PGF_ID
        public int PosteDestinoId { get; set; }      // PGF_ID_P

        // Referencias (carregadas depois)
        public Poste Origem { get; set; }               
        public Poste Destino { get; set; }

        // Características do cabo
        public bool IsMT { get; set; }                  // TB_TR_ID == "PA"
        public bool IsBT { get; set; }                  // TB_TR_ID == "AS"
        public string Bitola { get; set; }              // TB_BT_ID_FASE (S-10, S-02, S04, ETC)
        public string Fases { get; set; }               // TB_CDF_ID (ABC, AB, BC, ETC)
        public bool IsNovo { get; set; }

        // NOVO: Propriedades para desenho
        public float EspessuraDesenho { get; set; } = 2.0f;
        public bool Selecionado { get; set; } = false;
        public float Comprimento { get; private set; } = 0f;

        // Construtor
        public Trecho()
        {
            AtualizarPropriedades();
        }

        // Método para atualizar propriedades calculadas
        public void AtualizarPropriedades()
        {
            if (Origem != null && Destino != null)
            {
                Comprimento = CalcularComprimento();

                // Adicionar este trecho às listas dos postes
                if (!Origem.Trechos.Contains(this))
                    Origem.Trechos.Add(this);

                if (!Destino.Trechos.Contains(this))
                    Destino.Trechos.Add(this);
            }
        }

        // Métodos auxiliares
        public float CalcularComprimento()
        {
            if (Origem == null || Destino == null) return 0;
            float dX = Destino.X - Origem.X;
            float dY = Destino.Y - Origem.Y;
            return (float)Math.Sqrt(dX * dX + dY * dY);
        }

        public PointF ObterPontoMedio()
        {
            if (Origem == null || Destino == null) return PointF.Empty;
            return new PointF(
                (Origem.X + Destino.X) / 2,
                (Origem.Y + Destino.Y) / 2
            );
        }

        public PointF ObterPontoMedioComDeslocamento(float deslocamento = 10f)
        {
            if (Origem == null || Destino == null) return PointF.Empty;

            PointF meio = ObterPontoMedio();

            // Calcular vetor perpendicular ao trecho
            float dx = Destino.X - Origem.X;
            float dy = Destino.Y - Origem.Y;
            float comprimento = CalcularComprimento();

            if (comprimento > 0)
            {
                // Vetor perpendicular normalizado
                float px = -dy / comprimento;
                float py = dx / comprimento;

                // Aplicar deslocamento
                meio.X += px * deslocamento;
                meio.Y += py * deslocamento;
            }

            return meio;
        }

        public Color ObterCor()
        {
            if (Selecionado)
                return Color.Yellow;

            if (IsNovo)
                return Color.Blue;      // Trecho novo = AZUL
            else
                return IsMT ? Color.Red : Color.Green;  // MT=VERMELHO, BT=VERDE
        }

        public float ObterEspessura()
        {
            
            // Valores FIXOS (não mudam com zoom)
            if (Selecionado)
                return 3.0f;  // Trecho selecionado = mais grosso

            if (IsMT)
                return 1.5f;  // MT = 2.0 pixels (sempre)
            else
                return 1.0f;  // BT = 1.5 pixels (sempre)

            // Ou se quiser uma única espessura:
            // return 1.5f; // Todos os trechos com mesma espessura


            //return IsMT ? 1.5f : 1.0f;
        }

        public string ObterDescricao()
        {
            string tipo = IsMT ? "MT" : "BT";
            string status = IsNovo ? "NOVO" : "EXISTENTE";
            return $"{tipo} {Bitola} {Fases} ({status}) - {Comprimento:F1}m";
        }

        // NOVO: Calcular ângulo do trecho (em graus)
        public float CalcularAngulo()
        {
            if (Origem == null || Destino == null) return 0;

            float dx = Destino.X - Origem.X;
            float dy = Destino.Y - Origem.Y;
            return (float)(Math.Atan2(dy, dx) * (180 / Math.PI));
        }

        // NOVO: Verificar se ponto está próximo ao trecho
        public bool EstaProximo(PointF ponto, float tolerancia = 5f)
        {
            if (Origem == null || Destino == null) return false;

            // Distância do ponto à linha (fórmula geometria analítica)
            float x1 = Origem.X, y1 = Origem.Y;
            float x2 = Destino.X, y2 = Destino.Y;
            float x0 = ponto.X, y0 = ponto.Y;

            float numerador = Math.Abs((y2 - y1) * x0 - (x2 - x1) * y0 + x2 * y1 - y2 * x1);
            float denominador = (float)Math.Sqrt(Math.Pow(y2 - y1, 2) + Math.Pow(x2 - x1, 2));

            if (denominador == 0) return false;

            float distancia = numerador / denominador;
            return distancia <= tolerancia;
        }

        // NOVO: Método para debug
        public override string ToString()
        {
            return $"Trecho {Id}: P{PosteOrigemId} → P{PosteDestinoId} - {ObterDescricao()}";
        }
    }
}
