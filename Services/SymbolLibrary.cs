using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isabel_Visualizador_Proj.Services
{
    public class SymbolLibrary
    {
        // 1. POSTE
        public void DesenharPoste(Graphics g, PointF centro, bool IsExistente, float zoom)
        {
            float tamanho = 12f / zoom;
            float largura = tamanho;
            float altura = tamanho * 0.6f;

            // losango base
            PointF[] losango =
            {
                new PointF(centro.X, centro.Y - altura),
                new PointF(centro.X + largura, centro.Y),
                new PointF(centro.X, centro.Y + altura),
                new PointF(centro.X - largura, centro.Y)
            };

            using (Pen pen = new Pen(Color.Blue, 1.5f / zoom))
            {
                g.DrawPolygon(pen, losango);

                // Interno conforme tipo
                if (IsExistente) // EXISTENTE - X completo
                {
                    g.DrawLine(pen, centro.X - largura * 0.7f, centro.Y - altura * 0.7f, centro.X + largura * 0.7f, centro.Y + altura * 0.7f);
                    g.DrawLine(pen, centro.X + largura * 0.7f, centro.Y - altura * 0.7f, centro.X - largura * 0.7f, centro.Y + altura * 0.7f);
                    g.DrawLine(pen, centro.X, centro.Y - altura * 0.8f, centro.X, centro.Y + largura * 0.8f);
                    g.DrawLine(pen, centro.X - largura * 0.8f, centro.Y, centro.X + largura * 0.8f, centro.Y);

                }
                else // NÃO-EXISTENTE - apenas diagonais
                {
                    g.DrawLine(pen, centro.X - largura * 0.6f, centro.Y - altura * 0.6f, centro.X + largura * 0.6f, centro.Y + altura * 0.6f);
                    g.DrawLine(pen, centro.X + largura * 0.6f, centro.Y - altura * 0.6f, centro.X - largura * 0.6f, centro.Y + altura * 0.6f);
                }
            }
        }

        public void DesenharTransformador(Graphics g, PointF centro, float zoom)
        {
            float raio = 8f / zoom;

            // Círculo vermelho
            g.FillEllipse(Brushes.Red, centro.X - raio, centro.Y - raio, raio * 2, raio * 2);
            g.DrawEllipse(Pens.Black, centro.X - raio, centro.Y - raio, raio * 2, raio * 2);

            // Letra "T" branca
            using (Font fonte = new Font("Arial", 7f / zoom, FontStyle.Bold))
            {
                SizeF tamanhoTexto = g.MeasureString("T", fonte);
                g.DrawString("T", fonte, Brushes.White, centro.X - tamanhoTexto.Width / 2, centro.Y - tamanhoTexto.Height / 2);
            }
        }

        // 3. CHAVE FUSÍVEL (FU)
        public void DesenharChaveFusivel(Graphics g, PointF centro, float zoom)
        {
            float tamanho = 10f / zoom;

            // Retângulo amarelo vertical
            RectangleF retangulo = new RectangleF(centro.X - tamanho / 2, centro.Y - tamanho, tamanho, tamanho * 2);

            g.FillRectangle(Brushes.Yellow, retangulo);
            g.DrawRectangle(Pens.Black, retangulo.X, retangulo.Y, retangulo.Width, retangulo.Height);
            g.DrawLine(new Pen(Color.Red, 1f / zoom), centro.X - tamanho / 3, centro.Y, centro.X + tamanho / 3, centro.Y);

            // Letra "F"
            using (Font fonte = new Font("Arial", 6f / zoom, FontStyle.Bold))
            {
                SizeF tamanhoF = g.MeasureString("F", fonte);
                g.DrawString("F", fonte, Brushes.Black, centro.X - tamanhoF.Width / 2, centro.Y - tamanhoF.Height / 2);
            }
        }

        // 4. CHAVE FACA
        public void DesenharChaveFaca(Graphics g, PointF centro, float zoom)
        {
            float tamanho = 10f / zoom;

            // Círculo
            g.DrawEllipse(Pens.Black, centro.X - tamanho / 2, centro.Y - tamanho / 2, tamanho, tamanho);

            // Linha diagonal (faca)
            g.DrawLine(new Pen(Color.Red, 1.5f / zoom),
                centro.X - tamanho / 3, centro.Y - tamanho / 3,
                centro.X + tamanho / 3, centro.Y + tamanho / 3);

            // Letra C
            using (Font fonte = new Font("Arial", 6f, FontStyle.Bold))
            {
                SizeF tamanhoC = g.MeasureString("C", fonte);
                g.DrawString("C", fonte, Brushes.Black,
                    centro.X - tamanhoC.Width / 2,
                    centro.Y - tamanhoC.Height / 2);
            }
        }

        // PARA-RAIOS
        public void DesenharParaRaios(Graphics g, PointF basePoste, float zoom)
        {
            float tamanho = 8f / zoom;
            PointF topo = new PointF(basePoste.X, basePoste.Y - 20f / zoom);

            // Triangulo amarelao apontando para cima
            PointF[] triangulo =
            {
                new PointF(topo.X, topo.Y - tamanho*1.5f),
                new PointF(topo.X - tamanho, topo.Y + tamanho / 2),
                new PointF(topo.X + tamanho, topo.Y + tamanho / 2)
            };

            g.FillPolygon(Brushes.Yellow, triangulo);
            g.DrawPolygon(Pens.Black, triangulo);

            // Raio (Linha zig-zag)
            using (Pen penRaio = new Pen(Color.Orange, 1f / zoom))
            {
                float x = topo.X;
                float y = topo.Y - tamanho * 1.5f;

                for (int i = 0; i < 2; i++)
                {
                    g.DrawLine(penRaio, x, y, x - tamanho / 2, y + tamanho / 3);
                    y += tamanho / 3;
                    x -= tamanho / 2;

                    g.DrawLine(penRaio, x, y, x + tamanho / 2, y + tamanho / 3);
                    y += tamanho / 3;
                    x += tamanho / 2;
                }
            }
        }

        // 6. ATERRMENTO
        public void DesenharAterramento(Graphics g, PointF basePoste, float zoom)
        {
            float tamanho = 10f / zoom;
            PointF inicio = new PointF(basePoste.X, basePoste.Y + 15f / zoom);

            // Linha vertical
            g.DrawLine(Pens.Green,
                inicio.X, inicio.Y,
                inicio.X, inicio.Y + tamanho * 1.5f
            );

            // Três linhas horizontais (terra)
            for (int i = 0; i < 3; i++)
            {
                float y = inicio.Y + tamanho * 1.0f + i * tamanho / 3;
                float comprimento = tamanho * (0.8f - i * 0.2f);

                g.DrawLine(Pens.Green,
                    inicio.X - comprimento / 2, y,
                    inicio.X + comprimento / 2, y
                );
            }

            // Símbolo de terra (T)
            using (Font fonte = new Font("Arial", 8f / zoom, FontStyle.Bold))
            {
                g.DrawString("T", fonte, Brushes.Green,
                    inicio.X - 4f / zoom,
                    inicio.Y + tamanho * 0.8f + 2f
                );
            }
        }

        // 7. MEDIDOR (CONSUMIDOR)
        public void DesenharMedidor(Graphics g, PointF posicao, float zoom)
        {
            float tamanho = 6f / zoom;

            // Círculo azul
            g.FillEllipse(Brushes.Blue,
                posicao.X - tamanho, posicao.Y - tamanho, tamanho * 2, tamanho * 2);
            g.DrawEllipse(Pens.Black,
                posicao.X - tamanho, posicao.Y - tamanho, tamanho * 2, tamanho * 2);


            // Letra "M"
            using (Font fonte = new Font("Arial", 5f / zoom, FontStyle.Bold))
            {
                SizeF tamanhoM = g.MeasureString("M", fonte);
                g.DrawString("M", fonte, Brushes.White,
                    posicao.X - tamanhoM.Width / 2,
                    posicao.Y - tamanhoM.Height / 2);
            }
        }

        // 8. SETA (para ramais, indicações)
        public void DesenharSeta(Graphics g, PointF inicio, PointF fim, float tamanho, Color cor)
        {
            using (Pen pen = new Pen(cor, 1.5f))
            {
                // Linha principal
                g.DrawLine(pen, inicio, fim);

                // Cálculo do ângulo da seta
                float angulo = (float)Math.Atan2(fim.Y - inicio.Y, fim.X - inicio.X);
                float setaAngulo = (float)(Math.PI / 6); // 30 graus

                // Ponta da seta - lado direito
                PointF seta1 = new PointF(
                    fim.X - tamanho * (float)Math.Cos(angulo - setaAngulo),
                    fim.Y - tamanho * (float)Math.Sin(angulo - setaAngulo)
                );

                PointF seta2 = new PointF(
                    fim.X - tamanho * (float)Math.Cos(angulo + setaAngulo),
                    fim.Y - tamanho * (float)Math.Sin(angulo + setaAngulo)
                );

                // Desenha as linhas da ponta da seta
                g.DrawLine(pen, fim, seta1);
                g.DrawLine(pen, fim, seta2);
            }
        }
    }
}
