using System;
using System.Collections.Generic;
using System.Drawing;

namespace Isabel_Visualizador_Proj
{
    public class Poste
    {
        public int Id { get; set; }                 // PGF_ID
        public string Numero { get; set; }          // PGF_BARRAMENTO
        public float X { get; set; }                // Coordenadas X UTM
        public float Y { get; set; }                // Coordenadas Y UTM

        // Tipo do poste (TB_CT_ID Convertido)
        public string Material { get; set; }        // C=CONCTRETO, M=MADEIRA, F=FIBRA, A=AÇO
        public int  Altura { get; set; }            // 13, 12, 11, 10 (metros)
        public int Esforco { get; set; }            // 100, 600, 300, 150 (daN)

        // Elemento do poste
        public bool TemParaRaios { get; set; }      // TB_PR_ID == 3
        public bool TemAterramento { get; set; }    // PGF_IND_ATER == "S"
        public bool IsExistente { get; set; }       // FLAG_EXIST == "S"

        // Propriedades para desenho e calculo
        public float Rotacao { get; set; } = 0f;         // Ângulo de rotação em graus
        public Color Cor { get; set; }              // Cor do poste no desenho
        public float TamanhoDesenho { get; set; } = 10f;   // Tamanho em pixels para desenho

        // Relacionamentos
        public List<Equipamento> Equipamentos { get; set; } = new List<Equipamento>();
        public List<Trecho> Trechos { get; set; } = new List<Trecho>();

        // Construtor para inicializar cor
        public Poste()
        {
            AtualizarCor();
        }

        // Método para atualizar cor baseada nas propriedades
        public void AtualizarCor()
        {
            if (!IsExistente)
            {
                Cor = Color.Red; // Novo poste
            }
            else
            {
                // Poste existente: cor baseada no material
                switch (Material.ToUpper())
                {
                    case "C": // Concreto
                        Cor = Color.Gray;
                        break;
                    case "M": // Madeira
                        Cor = Color.Brown;
                        break;
                    case "F": // Fibra
                        Cor = Color.LightGray;
                        break;
                    case "A": // Aço
                        Cor = Color.SteelBlue;
                        break;
                    default:
                        Cor = Color.DarkGreen;
                        break;
                }
            }
        }

        // Métodos auxiliares
        public PointF ObterPosicao() => new PointF(X, Y);

        public string ObterTextoInferior()
        {
            // Exemplo: "U4/13/1000"
            // Material: C=Concreto, M=Madeira, F=Fibra
            string materialCode;

            switch (Material.ToUpper())
            {
                case "C":
                    materialCode = "C";
                    break;
                case "M":
                    materialCode = "M";
                    break;
                case "F":
                    materialCode = "F";
                    break;
                case "A":
                    materialCode = "A";
                    break;
                default:
                    materialCode = "U";
                    break;
            }

            //string estrutura = Estruturas.Count > 0 ? Estruturas[0].Tipo : "U4";
            return $"{materialCode}/{Altura}/{Esforco}";
        }

        public string ObterTextoSuperior() => !string.IsNullOrEmpty(Numero) ? Numero : $"P-{Id}";

        // NOVO: Calcular rotação baseada nos trechos conectados
        public void CalcularRotacaoAutomatica()
        {
            if (Trechos.Count == 0)
            {
                Rotacao = 0f;
                return;
            }

            if (Trechos.Count == 1)
            {
                // Alinhar com o único trecho
                var trecho = Trechos[0];
                var posteDestino = ObterPosteDestino(trecho);
                if (posteDestino != null)
                {
                    float dx = posteDestino.X - X;
                    float dy = posteDestino.Y - Y;
                    Rotacao = (float)(Math.Atan2(dy, dx) * (180 / Math.PI)) + 90;
                }
                return;
            }

            // Para 2+ trechos, calcular bissetriz
            float anguloTotal = 0f;
            int count = 0;

            foreach (var trecho in Trechos)
            {
                var posteDestino = ObterPosteDestino(trecho);
                if (posteDestino != null)
                {
                    float dx = posteDestino.X - X;
                    float dy = posteDestino.Y - Y;
                    float angulo = (float)(Math.Atan2(dy, dx) * (180 / Math.PI));

                    // Normalizar para 0-360
                    if (angulo < 0) angulo += 360;

                    anguloTotal += angulo;
                    count++;
                }
            }

            if (count > 0)
            {
                float anguloMedio = anguloTotal / count;
                Rotacao = anguloMedio + 90; // Poste vertical (+90 graus)
            }
        }

        // Método auxiliar para obter poste destino
        private Poste ObterPosteDestino(Trecho trecho)
        {
            // Esta lógica depende de como você armazena os postes
            // Você precisará implementar baseado no seu contexto
            return null; // Implemente isso
        }

        // NOVO: Obter retângulo para colisão/seleção
        public RectangleF ObterRetangulo(float zoom = 1f)
        {
            float tamanho = TamanhoDesenho / zoom;
            return new RectangleF(X - tamanho / 2, Y - tamanho / 2, tamanho, tamanho);
        }

        // NOVO: Verificar se ponto está dentro do poste
        public bool ContemPonto(PointF ponto, float zoom = 1f)
        {
            var rect = ObterRetangulo(zoom);
            return rect.Contains(ponto);
        }

        // NOVO: Método para debug
        public override string ToString()
        {
            return $"Poste {Id}: {Numero} @ ({X:F1}, {Y:F1}) - {Material}/{Altura}m/{Esforco}daN - {(IsExistente ? "EXISTENTE" : "NOVO")}";
        }
    }
}
