using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isabel_Visualizador_Proj.Models
{
    public static class TextoAutoCAD
    {
        public static void DesenharTextoRotacionado(Graphics g, string texto, PointF ponto,
                                                float anguloGraus, float tamanhoFonte,
                                                Brush brush, StringFormat formato = null)
        {
            // Salvar o estado original do Graphics
            GraphicsState estadoOriginal = g.Save();

            try
            {
                // Converter ângulo para radianos
                float anguloRad = anguloGraus * (float)(Math.PI / 180);

                // Criar matriz de transformação
                g.TranslateTransform(ponto.X, ponto.Y);
                g.RotateTransform(anguloGraus);
                g.TranslateTransform(-ponto.X, -ponto.Y);

                // Criar fonte
                using (Font font = new Font("Arial", tamanhoFonte, FontStyle.Regular))
                {
                    // Medir texto
                    SizeF textSize = g.MeasureString(texto, font);

                    // Posicionar texto (centralizado)
                    PointF posTexto = new PointF(
                        ponto.X - textSize.Width / 2,
                        ponto.Y - textSize.Height / 2
                    );

                    // Desenhar texto sem fundo
                    g.DrawString(texto, font, brush, posTexto);
                }
            }
            finally
            {
                // Restaurar estado original
                g.Restore(estadoOriginal);
            }
        }

        public static void DesenharTextoRotacionadoOffset(Graphics g, string texto, PointF pontoBase,
                                                     float anguloGraus, float offset,
                                                     float tamanhoFonte, Brush brush)
        {
            // Calcular ponto deslocado perpendicularmente
            float anguloRad = anguloGraus * (float)(Math.PI / 180);
            float anguloPerpendicular = anguloGraus + 90;
            float anguloPerpRad = anguloPerpendicular * (float)(Math.PI / 180);

            // Ponto deslocado perpendicularmente à linha
            PointF pontoDeslocado = new PointF(
                pontoBase.X + (float)Math.Cos(anguloPerpRad) * offset,
                pontoBase.Y + (float)Math.Sin(anguloPerpRad) * offset
            );

            // Desenhar texto no ponto deslocado
            DesenharTextoRotacionado(g, texto, pontoDeslocado, anguloGraus, tamanhoFonte, brush);
        }
    }
}
