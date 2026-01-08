using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isabel_Visualizador_Proj
{
    public class RenderService
    {
        // Configuraççoes de desenho
        public Color CorFundo { get; set; } = Color.White;
        public Color CorTextoPoste { get; set; } = Color.Black;
        public Color CorTextoCabo { get; set; } = Color.Blue;

        // Biblioteca de simbolos
        private readonly Services.SymbolLibrary symbolLibrary = new Services.SymbolLibrary();

        public void RenderizarCena(Graphics g, Viewport viewport, List<Poste> postes, List<Trecho> trechos)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(CorFundo);

            // Aplica transformações de viewport
            g.Transform = viewport.ObterTransformacao();

            // 1. Desenha trechos (cabos) - primeiro plano de fundo
            foreach (var trecho in trechos)
            {
                DesenharTrecho(g, trecho, viewport.Zoom);
            }

            // 2. Desenha postes - por cima de tudo
            foreach (var poste in postes.OrderBy(p => p.Y)) // Ordena por Y para sobreposição correta
            {
                DesenharPosteCompleto(g, poste, viewport.Zoom);
            }

            // 3. Textos sobre cabos - por cima de tudo
            foreach (var trecho in trechos)
            {
                DesenharTextoTrecho(g, trecho, viewport);
            }

            // 4. Textos dos postes - por cima de tudo
            foreach (var poste in postes)
            {
                DesenharTextoPoste(g, poste, viewport);
            }
        }

        private void DesenharPosteCompleto(Graphics g, Poste poste, float zoom)
        {
            PointF posicao = poste.ObterPosicao();

            // Desenha o símbolo do poste
            symbolLibrary.DesenharPoste(g, posicao, poste.IsExistente, zoom);

            // Desenha os equipamentos no poste
            foreach (var equipamento in poste.Equipamentos)
            {
                switch (equipamento.Tipo)
                {
                    case "IT":
                        symbolLibrary.DesenharTransformador(g, posicao, zoom);
                        break;

                    case "FU":
                        symbolLibrary.DesenharChaveFusivel(g, posicao, zoom);
                        break;

                    case "FC":
                        symbolLibrary.DesenharChaveFaca(g, posicao, zoom);
                        break;
                }
            }

            // Elementos adicionais podem ser desenhados aqui
            if (poste.TemParaRaios)
            {
                symbolLibrary.DesenharParaRaios(g, posicao, zoom);
            }

            if (poste.TemAterramento)
            {
                symbolLibrary.DesenharAterramento(g, posicao, zoom);
            }
        }
        private void DesenharTrecho(Graphics g, Trecho trecho, float zoom)
        {
            if (trecho.Origem == null || trecho.Destino == null) return;

            PointF origem = trecho.Origem.ObterPosicao();
            PointF destino = trecho.Destino.ObterPosicao();

            using (Pen pen = new Pen(trecho.ObterCor(), trecho.ObterEspessura() / zoom))
            {
                g.DrawLine(pen, origem, destino);
            }
        }

        private void DesenharTextoPoste(Graphics g, Poste poste, Viewport viewport)
        {
            PointF posicaoTela = viewport.MundopParaTela(poste.ObterPosicao());
            float offset = 20f / viewport.Zoom;

            // Texto superior (número do poste)
            string textoSuperior = poste.ObterTextoSuperior();

            if (!string.IsNullOrEmpty(textoSuperior))
            {
                DesenharTextoComFundo(g, textoSuperior, posicaoTela.X,
                    posicaoTela.Y - offset, ContentAlignment.TopCenter,
                    CorTextoPoste, viewport.Zoom, FontStyle.Bold);
            }

            // Texto inferior (tipo do poste)
            string textoInferior = poste.ObterTextoInferior();
            if (!string.IsNullOrEmpty(textoInferior))
            {
                DesenharTextoComFundo(g, textoInferior, posicaoTela.X,
                    posicaoTela.Y + offset, ContentAlignment.BottomCenter,
                    Color.Blue, viewport.Zoom, FontStyle.Regular);
            }
        }

        private void DesenharTextoTrecho(Graphics g, Trecho trecho, Viewport viewport)
        {
            if (trecho.Origem == null || trecho.Destino == null) return;

            PointF meio = trecho.ObterPontoMedio();
            PointF meioTela = viewport.MundopParaTela(meio);

            // Calcula o ponto médio
            string texto = $"{trecho.CalcularComprimento():0.0} m";
            if (!string.IsNullOrEmpty(trecho.Bitola))
                texto += $" {trecho.Bitola}";
            if (!string.IsNullOrEmpty(trecho.Fases))
                texto += $" {trecho.Fases}";

            DesenharTextoAngulo(g, texto, meioTela, trecho.Origem.ObterPosicao(),
                trecho.Destino.ObterPosicao(), CorTextoCabo, viewport.Zoom);
        }

        private void DesenharTextoComFundo(Graphics g, string texto, float x, float y,
            ContentAlignment alinhamento, Color corTexto, float zoom, FontStyle estilo)
        {
            float tamanhoFonte = Math.Max(8f, 10f / zoom);

            using (Font fonte = new Font("Arial", tamanhoFonte, estilo))
            {
                SizeF tamanhoTexto = g.MeasureString(texto, fonte);
                RectangleF retangulo = CriarRetanguloTexto(x, y, tamanhoTexto, alinhamento);

                // Fundo
                g.FillRectangle(Brushes.White, retangulo);
                g.DrawRectangle(Pens.LightGray, retangulo.X, retangulo.Y,
                    retangulo.Width, retangulo.Height);

                // Texto
                using (Brush textoBrush = new SolidBrush(corTexto))
                {
                    g.DrawString(texto, fonte, textoBrush, retangulo);
                }
            }
        }

        private RectangleF CriarRetanguloTexto(float x, float y, SizeF tamanhoTexto,
            ContentAlignment alinhamento)
        {
            switch (alinhamento)
            {
                case ContentAlignment.TopCenter:
                    return new RectangleF(x - tamanhoTexto.Width / 2, y, tamanhoTexto.Width, tamanhoTexto.Height);

                case ContentAlignment.BottomCenter:
                    return new RectangleF(x - tamanhoTexto.Width / 2, y - tamanhoTexto.Height,
                    tamanhoTexto.Width, tamanhoTexto.Height);

                case ContentAlignment.MiddleLeft:
                    return new RectangleF(x, y - tamanhoTexto.Height / 2, tamanhoTexto.Width, tamanhoTexto.Height);

                case ContentAlignment.MiddleRight:
                    return new RectangleF(x - tamanhoTexto.Width, y - tamanhoTexto.Height / 2, tamanhoTexto.Width, tamanhoTexto.Height);

                default:
                    return new RectangleF(x, y, tamanhoTexto.Width, tamanhoTexto.Height);
            }
        }

        private void DesenharTextoAngulo(Graphics g, string texto, PointF posicaoTela,
            PointF origem, PointF destino, Color corTexto, float zoom)
        {
            // Calcula o ângulo do trecho
            float angulo = (float)Math.Atan2(destino.Y - origem.Y, destino.X - origem.X) * 180f / (float)Math.PI;
            float tamanhoFonte = Math.Max(7f, 8f / zoom);

            using (Font fonte = new Font("Arial", tamanhoFonte))
            {
                // Salva o estado atual do gráfico
                var estado = g.Save();

                // Translada e rotaciona
                g.TranslateTransform(posicaoTela.X, posicaoTela.Y);
                g.RotateTransform(angulo);

                SizeF tamanhoTexto = g.MeasureString(texto, fonte);

                // Desenha o texto centralizado
                RectangleF retangulo = new RectangleF(
                    -tamanhoTexto.Width / 2,
                    -tamanhoTexto.Height / 2,
                    tamanhoTexto.Width,
                    tamanhoTexto.Height);

                // Fundo
                g.FillRectangle(Brushes.White, retangulo);

                // Texto
                using (Brush textoBrush = new SolidBrush(corTexto))
                {
                    g.DrawString(texto, fonte, textoBrush, -tamanhoTexto.Width / 2, -tamanhoTexto.Height / 2);
                }
                // Restaura o estado original
                g.Restore(estado);
            }
        }

        // HUD (informações na tela)
        public void DesenharHUD(Graphics g, Viewport viewport, int totalPostes, int totalTrechos)
        {
            string info = $"Postes: {totalPostes} | Trechos: {totalTrechos} | Zoom: {viewport.Zoom:0.00}x";
            string controles = "Rodinha: Zoom | Botão Esquerdo: Pan | +/-: Zoom | Home: Ajustar vista";

            using (Font fonte = new Font("Arial", 9f))
            {
                g.DrawString(info, fonte, Brushes.Black, 10, 10);
                g.DrawString(controles, fonte, Brushes.Black, 10, 30);

                // Legenda
                g.DrawString("Legenda:", fonte, Brushes.Black, 10, 60);
                g.DrawString(" Azul: Trecho novo", fonte, Brushes.Blue, 20, 80);
                g.DrawString(" Magenta: Trecho existente", fonte, Brushes.Magenta, 20, 100);
                g.DrawString(" Linha grossa: MT | Linha fina: BT", fonte, Brushes.Black, 20, 120);
            }
        }
    }
}
