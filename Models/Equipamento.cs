using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isabel_Visualizador_Proj
{
    public class Equipamento
    {
        public int Id { get; set; }                     // INT_ID
        public int PosteId { get; set; }               // PGF_ID
        public string Tipo { get; set; }                // TB_IN_ID (IT, FU, FC)
        public string Numero { get; set; }              // INT_NUM
        public string EloFusivel { get; set; }          // TB_ELO_ID
        public int Capacidade { get; set; }             // TB_CAP_ID (KVA)

        public float X { get; set; }                    // Posição X relativa
        public float Y { get; set; }                    // Posição Y relativa
        public float Angulo { get; set; } = 0f;         // Ângulo de instalação (graus)

        // Propriedades para desenho
        public Color Cor {  get; set; } = Color.Black;
        public float TamanhoDesenho { get; set; } = 8f; // Tamanho em pixels
        public bool Selecionado { get; set; } = false;

        // Métodos auxiliares
        public bool IsTransformador() => Tipo == "IT";
        public bool IsChaveFusivel() => Tipo == "FU";
        public bool IsChaveFaca() => Tipo == "FC";

        public string ObterDescricao
        {
            get
            {
                switch (Tipo)
                {
                    case "IT":
                        return $"TR ({Capacidade} KVA)";
                    case "FU":
                        return $"FUS ({EloFusivel})";
                    case "FC":
                        return $"CHAVE FACA";
                    default:
                        return Tipo;
                };
            }
        }

        // NOVO: Obter posição absoluta baseada no poste
        public PointF ObterPosicaoAbsoluta(PointF posicaoPoste)
        {
            // Converter ângulo para radianos
            float anguloRad = Angulo * (float)(Math.PI / 180);

            // Calcular posição relativa rotacionada
            float xRel = (float)(X * Math.Cos(anguloRad) - Y * Math.Sin(anguloRad));
            float yRel = (float)(X * Math.Sin(anguloRad) + Y * Math.Cos(anguloRad));

            // Soma com posição do poste
            return new PointF(posicaoPoste.X + xRel, posicaoPoste.Y + yRel);
        }

        // NOVO: Obter retângulo para desenho/seleção
        public RectangleF ObterRetangulo(PointF posicaoPoste, float zoom = 1f)
        {
            PointF pos = ObterPosicaoAbsoluta(posicaoPoste);
            float tamanho = TamanhoDesenho / zoom;

            return new RectangleF(
                pos.X - tamanho / 2,
                pos.Y - tamanho / 2,
                tamanho,
                tamanho
            );
        }

        // NOVO: Verificar se ponto está sobre o equipamento
        public bool ContemPonto(PointF ponto, PointF posicaoPoste, float zoom = 1f)
        {
            var rect = ObterRetangulo(posicaoPoste, zoom);
            return rect.Contains(ponto);
        }

        // NOVO: Método para atualizar cor baseada no tipo
        public void AtualizarCor()
        {
            switch (Tipo)
            {
                case "IT": // Transformador
                    Cor = Color.DarkBlue;
                    break;
                case "FU": // Chave Fusível
                    Cor = Color.OrangeRed;
                    break;
                case "FC": // Chave Faca
                    Cor = Color.DarkGreen;
                    break;
                default:
                    Cor = Color.Black;
                    break;
            }
        }

        // NOVO: Obter símbolo para desenho
        public string ObterSimbolo()
        {
            switch (Tipo)
            {
                case "IT": return "T";
                case "FU": return "F";
                case "FC": return "C";
                default: return "?";
            }
        }

        // NOVO: Método para debug
        public override string ToString()
        {
            return $"Equipamento {Id}: {Tipo} {Numero} no poste {PosteId} - {ObterDescricao}";
        }

    }
}
