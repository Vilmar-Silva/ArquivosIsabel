using Isabel_Visualizador_Proj.Models;
using Isabel_Visualizador_Proj.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;

namespace Isabel_Visualizador_Proj
{
    public partial class Paint : Form
    {
        private Viewport viewport = new Viewport();
        private bool _panningAtivo = false;
        private DatabaseService databaseService = new DatabaseService();

        private List<Poste> postes = new List<Poste>();
        private List<Trecho> trechos = new List<Trecho>();

        public Paint()
        {

            // DEBUG
            Debug.WriteLine("Formulário iniciado");

            // Chama o ajuste após carregar dados
            //this.Load += (s, e) =>
            //{
            //    if (postes.Count > 0)
            //    {
            //        AjustarViewport();
            //        this.Invalidate(); // Força redesenho
            //    }
            //}; 
            
            // Reduz flicker
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint, true);


            //// No construtor do formulário
            //this.MouseClick += (sender, e) =>
            //{
            //    if (viewport == null || postes.Count == 0) return;

            //    // Converte clique da tela para coordenadas do mundo
            //    PointF pontoMundo = viewport.TelaParaMundo(e.X, e.Y);

            //    // Verifica se clicou em algum poste
            //    float tolerancia = 10f / viewport.Zoom; // Tamanho da área clicável

            //    foreach (var poste in postes)
            //    {
            //        float distancia = (float)Math.Sqrt(
            //            Math.Pow(poste.X - pontoMundo.X, 2) +
            //            Math.Pow(poste.Y - pontoMundo.Y, 2));

            //        if (distancia <= tolerancia)
            //        {
            //            // Poste clicado!
            //            poste.Selecionado = !poste.Selecionado; // Alterna seleção
            //            this.Invalidate(); // Redesenha
            //            break;
            //        }
            //    }
            //};



            // NÃO CHAME InitializeComponent() - vamos fazer manualmente
            SetupForm();
            CarregarDados();
            AjustarViewport();

            VerificarCentralizacao();

            ConfigurarEventos();


            // Botão para debug de coordenadas
            var btnCoords = new Button
            {
                Text = "Ver Coordenadas",
                Location = new Point(10, 500),
                Size = new Size(120, 30)
            };

            btnCoords.Click += (s, ev) => {
                string info = $"Total postes: {postes.Count}\n";
                for (int i = 0; i < Math.Min(5, postes.Count); i++)
                {
                    info += $"Poste {i}: ID={postes[i].Id}, X={postes[i].X:F1}, Y={postes[i].Y:F1}\n";
                }
                MessageBox.Show(info, "Coordenadas Carregadas");
            };

            var btnTeste = new Button
            {
                Text = "Teste Manual",
                Location = new Point(10, 550),
                Size = new Size(120, 30)
            };

            btnTeste.Click += (s, ev) => {

                Debug.WriteLine($"=== TESTE MANUAL ===");
                Debug.WriteLine($"Postes na memória: {postes.Count}");
                Debug.WriteLine($"Trechos na memória: {trechos.Count}");

                if (trechos.Count > 0)
                {
                    Debug.WriteLine($"Primeiro trecho: Origem={trechos[0].PosteOrigemId}, Destino={trechos[0].PosteDestinoId}");
                    Debug.WriteLine($"Origem objeto: {trechos[0].Origem != null}");
                    Debug.WriteLine($"Destino objeto: {trechos[0].Destino != null}");
                }

                MessageBox.Show($"Postes: {postes.Count}\nTrechos: {trechos.Count}");
            };

            this.Controls.Add(btnCoords);
            this.Controls.Add(btnTeste);
        }

        private void SetupForm()
        {
            // Configurações básicas
            this.Text = "ISABEL CAD - Visualizador de Redes Elétricas";
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.DoubleBuffered = true;
            this.BackColor = Color.White;
            this.WindowState = FormWindowState.Maximized;

            // Criar menu simples
            var menuStrip = new MenuStrip();

            var fileMenu = new ToolStripMenuItem("Arquivo");
            fileMenu.DropDownItems.Add("Carregar Dados", null, (s, e) => CarregarDados());
            fileMenu.DropDownItems.Add("Sair", null, (s, e) => Application.Exit());

            var viewMenu = new ToolStripMenuItem("Visualização");
            viewMenu.DropDownItems.Add("Ajustar Vista (Home)", null, (s, e) => AjustarViewport());

            var helpMenu = new ToolStripMenuItem("Ajuda");
            helpMenu.DropDownItems.Add("Sobre", null, (s, e) =>
                MessageBox.Show("ISABEL CAD v1.0\nVisualizador de Redes Elétricas"));

            menuStrip.Items.AddRange(new[] { fileMenu, viewMenu, helpMenu });
            this.Controls.Add(menuStrip);
            this.MainMenuStrip = menuStrip;

        }

        private void CarregarDados()
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                // Testar conexão
                if (!databaseService.TestarConexao())
                {
                    //CriarDadosTeste();
                    MessageBox.Show("Usando dados de teste (banco não encontrado)",
                        "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Carregar do Access
                postes = databaseService.CarregarPostes();
                trechos = databaseService.CarregarTrechos();


                // DEBUG ADICIONADO AQUI:
                Debug.WriteLine($"=== CARREGAMENTO DO BANCO ===");
                Debug.WriteLine($"Postes carregados: {postes.Count}");
                Debug.WriteLine($"Trechos carregados: {trechos.Count}");


                if (postes.Count == 0)
                {
                    //CriarDadosTeste();
                    return;
                }

                // Conectar referências entre trechos e postes
                ConectarTrechosComPostes();

                // Calcular rotações dos postes
                CalcularRotacoesPostes();

                MessageBox.Show($"Carregado: {postes.Count} postes, {trechos.Count} trechos",
                    "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}\nUsando dados de teste",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //CriarDadosTeste();
            }
            finally
            {
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        private void ConectarTrechosComPostes()
        {

            if (postes.Count == 0 || trechos.Count == 0)
            {
                Debug.WriteLine("ConectarTrechosComPostes: Lista vazia");
                return;
            }

            // Cria dicionário para acesso rápido
            var dictPostes = postes.ToDictionary(p => p.Id, p => p);

            int conectados = 0;

            foreach (var trecho in trechos)
            {
                // Tenta encontrar origem
                if (dictPostes.TryGetValue(trecho.PosteOrigemId, out Poste origem))
                {
                    trecho.Origem = origem;
                }
                else
                {
                    Debug.WriteLine($"Poste origem não encontrado: ID={trecho.PosteOrigemId}");
                }

                // Tenta encontrar destino
                if (dictPostes.TryGetValue(trecho.PosteDestinoId, out Poste destino))
                {
                    trecho.Destino = destino;
                }
                else
                {
                    Debug.WriteLine($"Poste destino não encontrado: ID={trecho.PosteDestinoId}");
                }

                if (trecho.Origem != null && trecho.Destino != null)
                {
                    conectados++;
                }
            }

            Debug.WriteLine($"Conectados {conectados}/{trechos.Count} trechos com postes");




            //if (postes.Count == 0 || trechos.Count == 0) return;

            //int conexoesBemSucedidas = 0;

            //// Cria dicionário para acesso rápido aos postes por ID
            //Dictionary<int, Poste> dicionarioPostes = postes.ToDictionary(p => p.Id);

            //foreach (var trecho in trechos)
            //{
            //    // Encontra o poste de origem
            //    if (dicionarioPostes.TryGetValue(trecho.PosteOrigemId, out Poste origem))
            //    {
            //        trecho.Origem = origem;
            //    }

            //    // Encontra o poste de destino
            //    if (dicionarioPostes.TryGetValue(trecho.PosteDestinoId, out Poste destino))
            //    {
            //        trecho.Destino = destino;
            //    }

            //    // Se encontrou ambos, conta como conexão bem-sucedida
            //    if (trecho.Origem != null && trecho.Destino != null)
            //    {
            //        conexoesBemSucedidas++;

            //        // Opcional: Adiciona referência nos postes também
            //        if (!origem.Trechos.Contains(trecho))
            //            origem.Trechos.Add(trecho);

            //        if (!destino.Trechos.Contains(trecho))
            //            destino.Trechos.Add(trecho);
            //    }
            //}

            //Debug.WriteLine($"=== CONEXÕES: {conexoesBemSucedidas}/{trechos.Count} trechos conectados ===");
            //MessageBox.Show($"{conexoesBemSucedidas} trechos conectados a postes",
            //    "Conexões", MessageBoxButtons.OK, MessageBoxIcon.Information);


            //foreach (var trecho in trechos)
            //{
            //    trecho.Origem = postes.Find(p => p.Id == trecho.PosteOrigemId);
            //    trecho.Destino = postes.Find(p => p.Id == trecho.PosteDestinoId);

            //    // Atualizar propriedades do trecho
            //    if (trecho.Origem != null && trecho.Destino != null)
            //    {
            //        trecho.AtualizarPropriedades();
            //    }
            //}
        }

        private void CalcularRotacoesPostes()
        {
            foreach (var poste in postes)
            {
                // Coletar trechos conectados a este poste
                var trechosConectados = new List<Trecho>();
                foreach (var trecho in trechos)
                {
                    if (trecho.Origem == poste || trecho.Destino == poste)
                    {
                        trechosConectados.Add(trecho);
                    }
                }

                poste.Trechos = trechosConectados;
                poste.CalcularRotacaoAutomatica();
            }
        }

        //private void CriarDadosTeste()
        //{
        //    postes.Clear();
        //    trechos.Clear();

        //    // Postes de teste
        //    postes.Add(new Poste
        //    {
        //        Id = 1,
        //        X = 100,
        //        Y = 100,
        //        Numero = "61151",
        //        Altura = 13,
        //        Esforco = 1000,
        //        IsExistente = true,
        //        Material = "C"
        //    });

        //    postes.Add(new Poste
        //    {
        //        Id = 2,
        //        X = 400,
        //        Y = 100,
        //        Numero = "61152",
        //        Altura = 13,
        //        Esforco = 600,
        //        IsExistente = false,
        //        Material = "M"
        //    });

        //    postes.Add(new Poste
        //    {
        //        Id = 3,
        //        X = 100,
        //        Y = 400,
        //        Numero = "61153",
        //        Altura = 12,
        //        Esforco = 1000,
        //        IsExistente = true,
        //        Material = "F"
        //    });

        //    postes.Add(new Poste
        //    {
        //        Id = 4,
        //        X = 400,
        //        Y = 400,
        //        Numero = "61154",
        //        Altura = 12,
        //        Esforco = 600,
        //        IsExistente = false,
        //        Material = "A"
        //    });

        //    // Atualizar cores
        //    foreach (var poste in postes)
        //    {
        //        poste.AtualizarCor();
        //    }

        //    // Equipamentos
        //    postes[0].Equipamentos.Add(new Equipamento
        //    {
        //        Id = 1,
        //        Tipo = "IT",
        //        Capacidade = 100,
        //        PosteId = 1
        //    });

        //    postes[1].Equipamentos.Add(new Equipamento
        //    {
        //        Id = 2,
        //        Tipo = "FU",
        //        PosteId = 2
        //    });

        //    postes[2].TemParaRaios = true;
        //    postes[3].TemAterramento = true;

        //    // Trechos
        //    trechos.Add(new Trecho
        //    {
        //        Id = 1,
        //        PosteOrigemId = 1,
        //        PosteDestinoId = 2,
        //        IsNovo = true,
        //        IsMT = true,
        //        Bitola = "S-10",
        //        Fases = "ABC"
        //    });

        //    trechos.Add(new Trecho
        //    {
        //        Id = 2,
        //        PosteOrigemId = 2,
        //        PosteDestinoId = 4,
        //        IsNovo = false,
        //        IsMT = false,
        //        Bitola = "S-04",
        //        Fases = "AB"
        //    });

        //    trechos.Add(new Trecho
        //    {
        //        Id = 3,
        //        PosteOrigemId = 4,
        //        PosteDestinoId = 3,
        //        IsNovo = true,
        //        IsMT = true,
        //        Bitola = "S-10",
        //        Fases = "ABC"
        //    });

        //    trechos.Add(new Trecho
        //    {
        //        Id = 4,
        //        PosteOrigemId = 3,
        //        PosteDestinoId = 1,
        //        IsNovo = false,
        //        IsMT = false,
        //        Bitola = "S-04",
        //        Fases = "BC"
        //    });

        //    // Conectar
        //    ConectarTrechosComPostes();
        //    CalcularRotacoesPostes();
        //}

        private void AjustarViewport()
        {
            if (postes.Count == 0) return;

            // Calcula os limites dos dados
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            // Para postes
            foreach (var poste in postes)
            {
                minX = Math.Min(minX, poste.X);
                maxX = Math.Max(maxX, poste.X);
                minY = Math.Min(minY, poste.Y);
                maxY = Math.Max(maxY, poste.Y);
            }

            // Para trechos (verificar extremidades)
            foreach (var trecho in trechos)
            {
                if (trecho.Origem != null)
                {
                    minX = Math.Min(minX, trecho.Origem.X);
                    maxX = Math.Max(maxX, trecho.Origem.X);
                    minY = Math.Min(minY, trecho.Origem.Y);
                    maxY = Math.Max(maxY, trecho.Origem.Y);
                }

                if (trecho.Destino != null)
                {
                    minX = Math.Min(minX, trecho.Destino.X);
                    maxX = Math.Max(maxX, trecho.Destino.X);
                    minY = Math.Min(minY, trecho.Destino.Y);
                    maxY = Math.Max(maxY, trecho.Destino.Y);
                }
            }

            // Adicionar margem de 10%
            float marginX = (maxX - minX) * 0.1f;
            float marginY = (maxY - minY) * 0.1f;

            minX -= marginX;
            maxX += marginX;
            minY -= marginY;
            maxY += marginY;

            // ==========================================

            // DEBUG: Verificar os valores calculados
            Debug.WriteLine($"=== DEBUG AJUSTAR VIEWPORT ===");
            Debug.WriteLine($"Postes: {postes.Count}, Trechos: {trechos.Count}");
            Debug.WriteLine($"minX={minX:F2}, maxX={maxX:F2}, largura={maxX - minX:F2}");
            Debug.WriteLine($"minY={minY:F2}, maxY={maxY:F2}, altura={maxY - minY:F2}");
            Debug.WriteLine($"Tela: {this.ClientSize.Width}x{this.ClientSize.Height}");



            // =========================================




            // Centralizar na tela
            viewport.Centralizar(minX, maxX, minY, maxY, this.ClientSize.Width, this.ClientSize.Height, postes);
        }

        private void ConfigurarEventos()
        {
            //// Evento Paint
            //this.Paint += (sender, e) =>
            //{
            //    // Desenhar fundo
            //    e.Graphics.Clear(Color.White);

            //    // Salvar estado original
            //    GraphicsState estadoOriginal = e.Graphics.Save();

            //    try
            //    {
            //        // Aplicar transformações do Viewport
            //        e.Graphics.TranslateTransform(viewport.OffsetX, viewport.OffsetY);
            //        e.Graphics.ScaleTransform(viewport.Zoom, viewport.Zoom);

            //        // DESENHAR TRECHOS PRIMEIRO (linhas)
            //        DesenharTrechos(e.Graphics);

            //        // DESENHAR POSTES (sobre as linhas)
            //        //DesenharPostes(e.Graphics);

            //        // DESENHAR EQUIPAMENTOS (sobre os postes)
            //        DesenharEquipamentos(e.Graphics);

            //        // DESENHAR TEXTOS/INFOS (por último)
            //        DesenharInformacoes(e.Graphics);
            //    }
            //    finally
            //    {
            //        // Restaurar estado original
            //        e.Graphics.Restore(estadoOriginal);
            //    }

            //    // DESENHAR LEGENDA/HUD (não afetada pelo viewport)
            //    DesenharLegenda(e.Graphics);
            //};

            // Eventos de mouse
            // Substitua o evento MouseWheel:
            this.MouseWheel += (s, e) =>
            {
                // Ponto onde está o mouse
                PointF pontoMouse = e.Location;

                // Aplica zoom (positivo = zoom in, negativo = zoom out)
                float fator = e.Delta > 0 ? 1.2f : 1 / 1.2f;

                // Usa o método correto
                viewport.ZoomParaPonto(pontoMouse, e.Delta > 0);

                // Redesenha
                Invalidate();
            };

            this.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right)
                {
                    _panningAtivo = true;
                    viewport.IniciarPan(e.Location);
                    Cursor = Cursors.Hand;
                }
            };

            this.MouseMove += (s, e) =>
            {
                if (_panningAtivo)
                {
                    viewport.AtualizarPan(e.Location);
                    Invalidate();
                }
            };

            this.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Middle || e.Button == MouseButtons.Right)
                {
                    _panningAtivo = false;
                    viewport.PararPan();
                    Cursor = Cursors.Default;
                }
            };

            // No evento KeyDown, adicione:
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Home)
                {
                    AjustarViewport();
                    Invalidate();
                }
                if (e.KeyCode == Keys.F5)
                {
                    CarregarDados();
                }
                if (e.KeyCode == Keys.Add || e.KeyCode == Keys.Oemplus)
                {
                    // Zoom in no centro da tela
                    PointF centro = new PointF(this.ClientSize.Width / 2, this.ClientSize.Height / 2);
                    viewport.AplicarZoom(1.2f, centro);
                    Invalidate();
                }
                if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus)
                {
                    // Zoom out no centro da tela
                    PointF centro = new PointF(this.ClientSize.Width / 2, this.ClientSize.Height / 2);
                    viewport.AplicarZoom(1 / 1.2f, centro);
                    Invalidate();
                }
                if (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0)
                {
                    // Reset zoom
                    viewport.Zoom = 1.0f;
                    viewport.OffsetX = 0;
                    viewport.OffsetY = 0;
                    Invalidate();
                }
            };

            // Evento de redimensionamento
            this.Resize += (s, e) => Invalidate();
        }

        // MÉTODOS DE DESENHO
        private void DesenharTrechos(Graphics g)
        {

            if (trechos.Count == 0) return;

            // Suavizar
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // 1. Primeiro desenha TODAS as linhas
            foreach (var trecho in trechos)
            {
                if (trecho.Origem != null && trecho.Destino != null)
                {
                    PointF pontoOrigem = viewport.MundoParaTela(trecho.Origem.X, trecho.Origem.Y);
                    PointF pontoDestino = viewport.MundoParaTela(trecho.Destino.X, trecho.Destino.Y);

                    // Desenha a linha
                    using (Pen pen = new Pen(trecho.ObterCor(), trecho.ObterEspessura()))// * viewport.Zoom))
                    {
                        g.DrawLine(pen, pontoOrigem, pontoDestino);
                    }
                }
            }

            // 2. Depois desenha os textos (por cima das linhas)
            // Só mostra se o zoom for suficiente para ler
            if (viewport.Zoom > 0.4f)  // Ajuste este valor conforme necessário
            {
                DesenharComprimentosTrechos(g);
            }

            //foreach (var trecho in trechos)
            //{
            //    if (trecho.Origem == null || trecho.Destino == null)
            //        continue;

            //    using (Pen pen = new Pen(trecho.ObterCor(), trecho.ObterEspessura()))
            //    {
            //        g.DrawLine(pen,
            //            trecho.Origem.X, trecho.Origem.Y,
            //            trecho.Destino.X, trecho.Destino.Y);
            //    }

            //    // Desenhar texto do trecho (comprimento)
            //    PointF pontoMedio = trecho.ObterPontoMedioComDeslocamento(10);
            //    string texto = $"{trecho.CalcularComprimento():F1}m";

            //    using (Font font = new Font("Arial", 8f / viewport.Zoom))
            //    using (Brush brush = new SolidBrush(Color.DarkBlue))
            //    {
            //        g.DrawString(texto, font, brush, pontoMedio);
            //    }
            //}
        }

        private void DesenharComprimentosTrechos(Graphics g)
        {
            // DEBUG: Ver quantos trechos temos
            Debug.WriteLine($"=== DESENHAR COMPRIMENTOS ===");
            Debug.WriteLine($"Total trechos: {trechos.Count}");

            // Lista IDs para verificar
            foreach (var trecho in trechos)
            {
                Debug.WriteLine($"Trecho ID: {trecho.Id}, Origem: {trecho.PosteOrigemId}, Destino: {trecho.PosteDestinoId}");
            }


            // ============ CONFIGURAÇÕES AUTOCAD ============
            // Valores em UNIDADES DO MUNDO (não pixels)
            float tamanhoFonteBaseMundo = 0.8f;      // 0.8 unidades do mundo
            float deslocamentoBaseMundo = 1.2f;      // 1.2 unidades do mundo do centro da linha

            // Fatores relativos
            float fatorMargemFundo = 0.15f;          // 15% do tamanho da fonte para margem
            float fatorEspacoEntreTextos = 0.4f;     // 40% do tamanho da fonte entre textos

            // ============ PROCESSAR CADA TRECHO ============
            foreach (var trecho in trechos)
            {
                if (trecho.Origem == null || trecho.Destino == null) continue;

                // Converte coordenadas para tela
                PointF pontoOrigem = viewport.MundoParaTela(trecho.Origem.X, trecho.Origem.Y);
                PointF pontoDestino = viewport.MundoParaTela(trecho.Destino.X, trecho.Destino.Y);

                // Ponto médio do trecho
                PointF pontoMedio = new PointF(
                    (pontoOrigem.X + pontoDestino.X) / 2,
                    (pontoOrigem.Y + pontoDestino.Y) / 2
                );

                // Calcula ângulo do trecho (para posicionar texto perpendicular)
                float dx = pontoDestino.X - pontoOrigem.X;
                float dy = pontoDestino.Y - pontoOrigem.Y;
                float anguloRad = (float)Math.Atan2(dy, dx);
                float anguloGraus = anguloRad * (180f / (float)Math.PI);

                // Ajusta ângulo para sempre ficar legível (entre -90 e 90 graus)
                if (anguloGraus > 90) anguloGraus -= 180;
                if (anguloGraus < -90) anguloGraus += 180;

                // ============ TAMANHO DA FONTE (AUTOESCALÁVEL) ============
                float tamanhoFonte = tamanhoFonteBaseMundo * viewport.Zoom;

                // Limites opcionais (manter legibilidade)
                tamanhoFonte = Math.Max(6f, Math.Min(tamanhoFonte, 20f));

                // ============ TEXTO PRINCIPAL (COMPRIMENTO) ============
                using (Font fonte = new Font("Arial", tamanhoFonte, FontStyle.Bold))
                using (Brush brushTexto = new SolidBrush(Color.DarkBlue))
                {
                    // Formata comprimento
                    float comprimentoMetros = trecho.CalcularComprimento();
                    string texto = $"{comprimentoMetros:F0}m";  // 0 casas decimais

                    // Mede tamanho do texto
                    SizeF tamanhoTexto = g.MeasureString(texto, fonte);

                    // ============ DESLOCAMENTO RELATIVO (AUTOESCALÁVEL) ============
                    float deslocamento = deslocamentoBaseMundo * viewport.Zoom;

                    // Calcula vetor perpendicular à linha
                    float perpX = (float)Math.Sin(anguloRad) * deslocamento;
                    float perpY = -(float)Math.Cos(anguloRad) * deslocamento;

                    // Posição do texto (centralizado)
                    float posX = pontoMedio.X + perpX - tamanhoTexto.Width / 2;
                    float posY = pontoMedio.Y + perpY - tamanhoTexto.Height / 2;

                    // ============ FUNDO SEMI-TRANSPARENTE (COM MARGEM RELATIVA) ============
                    float margem = tamanhoFonte * fatorMargemFundo;
                    RectangleF fundo = new RectangleF(
                        posX - margem,
                        posY - margem / 2,
                        tamanhoTexto.Width + margem * 2,
                        tamanhoTexto.Height + margem
                    );

                    // Desenha fundo (se desejado - opcional)
                    using (Brush brushFundo = new SolidBrush(Color.FromArgb(220, 255, 255, 240)))
                    {
                        g.FillRectangle(brushFundo, fundo);
                    }
                    using (Pen penBorda = new Pen(Color.LightGray, 1f)) // ESPESSURA FIXA
                    {
                        g.DrawRectangle(penBorda, fundo.X, fundo.Y, fundo.Width, fundo.Height);
                    }

                    // ============ DESENHA TEXTO ============
                    g.DrawString(texto, fonte, brushTexto, posX, posY);

                    // ============ TEXTO SECUNDÁRIO (NOME/IDENTIFICAÇÃO) ============
                    if (!string.IsNullOrEmpty(trecho.Nome) || trecho.Id > 0)
                    {
                        string textoNome = !string.IsNullOrEmpty(trecho.Nome) ? trecho.Nome : $"T{trecho.Id}";
                        float tamanhoFonteNome = tamanhoFonte * 0.8f;

                        using (Font fonteNome = new Font("Arial", tamanhoFonteNome, FontStyle.Regular))
                        using (Brush brushNome = new SolidBrush(Color.DarkGreen))
                        {
                            SizeF tamanhoNome = g.MeasureString(textoNome, fonteNome);

                            // Posição abaixo do texto principal (distância relativa)
                            float espaco = tamanhoFonte * fatorEspacoEntreTextos;
                            float posNomeX = pontoMedio.X + perpX - tamanhoNome.Width / 2;
                            float posNomeY = posY + tamanhoTexto.Height + espaco;

                            // Fundo para o nome (opcional)
                            RectangleF fundoNome = new RectangleF(
                                posNomeX - margem,
                                posNomeY - margem / 2,
                                tamanhoNome.Width + margem * 2,
                                tamanhoNome.Height + margem
                            );

                            using (Brush brushFundoNome = new SolidBrush(Color.FromArgb(200, 240, 255, 240)))
                            {
                                g.FillRectangle(brushFundoNome, fundoNome);
                            }

                            g.DrawString(textoNome, fonteNome, brushNome, posNomeX, posNomeY);
                        }
                    }
                }
            }




            //using (Font fonte = new Font("Arial", 8f * viewport.Zoom, FontStyle.Bold))
            //using (Brush brushTexto = new SolidBrush(Color.DarkBlue))
            //using (Brush brushFundo = new SolidBrush(Color.FromArgb(220, 255, 255, 240))) // Fundo claro
            //{
            //    foreach (var trecho in trechos)
            //    {
            //        if (trecho.Origem == null || trecho.Destino == null) continue;

            //        // Converte coordenadas
            //        PointF pontoOrigem = viewport.MundoParaTela(trecho.Origem.X, trecho.Origem.Y);
            //        PointF pontoDestino = viewport.MundoParaTela(trecho.Destino.X, trecho.Destino.Y);

            //        // Calcula ponto médio do trecho
            //        PointF pontoMedio = new PointF(
            //            (pontoOrigem.X + pontoDestino.X) / 2,
            //            (pontoOrigem.Y + pontoDestino.Y) / 2
            //        );

            //        // Calcula ângulo do trecho para posicionar texto
            //        float angulo = (float)Math.Atan2(
            //            pontoDestino.Y - pontoOrigem.Y,
            //            pontoDestino.X - pontoOrigem.X);

            //        // Formata comprimento (em metros, com 1 casa decimal)
            //        float comprimentoMetros = trecho.Comprimento; // ou ComprimentoCalculado
            //        string texto = $"{comprimentoMetros:F1}m";

            //        // Mede tamanho do texto
            //        SizeF tamanhoTexto = g.MeasureString(texto, fonte);

            //        // Ajusta posição baseada no ângulo
            //        float offsetX = (float)Math.Sin(angulo) * 10f;
            //        float offsetY = -(float)Math.Cos(angulo) * 10f;

            //        RectangleF retanguloTexto = new RectangleF(
            //            pontoMedio.X + offsetX - tamanhoTexto.Width / 2,
            //            pontoMedio.Y + offsetY - tamanhoTexto.Height / 2,
            //            tamanhoTexto.Width + 6,
            //            tamanhoTexto.Height + 4
            //        );

            //        // Desenha fundo (opcional, melhora legibilidade)
            //        g.FillRectangle(brushFundo, retanguloTexto);
            //        g.DrawRectangle(Pens.LightGray,
            //            retanguloTexto.X, retanguloTexto.Y,
            //            retanguloTexto.Width, retanguloTexto.Height);

            //        // Desenha texto
            //        g.DrawString(texto, fonte, brushTexto,
            //            pontoMedio.X + offsetX - tamanhoTexto.Width / 2 + 3,
            //            pontoMedio.Y + offsetY - tamanhoTexto.Height / 2 + 2);
            //    }
            //}
        }

        private void DesenharPostes(Graphics g)
        {
            // No início do DesenharPostes ou OnPaint, adicione:
            Debug.WriteLine($"=== DEBUG: Postes: {postes.Count}, Trechos: {trechos.Count} ===");

            if (postes.Count == 0) return;

            // DEBUG: Verificar conversão do primeiro poste
            var primeiroPoste = postes[0];
            PointF pontoConvertido = viewport.MundoParaTela(primeiroPoste.X, primeiroPoste.Y);

            Debug.WriteLine($"=== DEBUG CONVERSÃO ===");
            Debug.WriteLine($"Poste 1 - Mundo: X={primeiroPoste.X}, Y={primeiroPoste.Y}");
            Debug.WriteLine($"Poste 1 - Tela: X={pontoConvertido.X:F0}, Y={pontoConvertido.Y:F0}");
            Debug.WriteLine($"Centro tela esperado: X={this.ClientSize.Width / 2}, Y={this.ClientSize.Height / 2}");



            // Fator de escala ÚNICO para tudo
            float escala = viewport.Zoom;

            // Limites para o fator de escala (ajuste conforme necessário)
            float escalaMinima = 0.3f;
            float escalaMaxima = 30f;
            escala = Math.Max(escalaMinima, Math.Min(escala, escalaMaxima));

            // Defina LARGURA e ALTURA DIFERENTES para o retângulo
            float larguraBase = 10f;   // LARGURA do retângulo (horizontal)
            float alturaBase = 6f;    // ALTURA do retângulo (vertical)

            // Tamanhos BASE (quando escala = 1)
            float tamanhoBaseRetangulo = 15f;      // Tamanho do lado do quadrado
            float espessuraBaseBorda = 1.0f;       // Espessura da borda
            float tamanhoBaseFonteSuperior = 4f;   // Fonte superior
            float tamanhoBaseFonteInferior = 4f;   // Fonte inferior
            float espacamentoBaseTexto = 6f;      // Espaço entre poste e texto

            // Escala LARGURA e ALTURA separadamente
            float largura = larguraBase * escala;
            float altura = alturaBase * escala;

            // Tamanhos REAIS (escalados)
            float tamanhoRetangulo = tamanhoBaseRetangulo * escala;
            float espessuraBorda = espessuraBaseBorda;
            float tamanhoFonteSuperior = tamanhoBaseFonteSuperior * escala;
            float tamanhoFonteInferior = tamanhoBaseFonteInferior * escala;

            float espacamentoTexto = espacamentoBaseTexto;


            // ============ FATORES RELATIVOS (estilo AutoCAD) ============
            // Valores em "unidades relativas" ao tamanho do elemento
            float fatorDistanciaSuperior = 0.8f;  // 80% da altura do retângulo
            float fatorDistanciaInferior = 0.6f;  // 60% da altura do retângulo
                                                  // ===========================================================


            foreach (var poste in postes)
            {
                // Converte coordenadas
                PointF ponto = viewport.MundoParaTela(poste.X, poste.Y);


                // ============ CALCULA ÂNGULO DE ROTAÇÃO E PADRÃO DE TRIÂNGULOS ============
                List<Trecho> trechosConectados = AnaliseTopologica.EncontrarTrechosConectados(poste, trechos);
                int numTrechos = trechosConectados.Count;
                float anguloMedio = AnaliseTopologica.CalcularAnguloMedio(trechosConectados, poste);

                // DEBUG
                Debug.WriteLine($"Poste {poste.Id}: {numTrechos} trechos, Ângulo: {anguloMedio * 180 / Math.PI:F0}°");
                // ============================================

                // ============ CALCULA ÂNGULO DE ROTAÇÃO DO POSTE ============


                float anguloRotacaoGraus = 0f;
                bool usarPadraoPadrao = true; // true = padrão original, false = invertido

                if (numTrechos == 1 && trechosConectados.Count > 0)
                {
                    // POSTE COM 1 TRECHO: Lado vazado oposto ao cabo
                    Trecho trecho = trechosConectados[0];
                    Poste outroPoste = (trecho.Origem.Id == poste.Id) ? trecho.Destino : trecho.Origem;

                    if (outroPoste != null)
                    {
                        float dx = outroPoste.X - poste.X;
                        float dy = outroPoste.Y - poste.Y;
                        float anguloRad = (float)Math.Atan2(dy, dx);
                        anguloRotacaoGraus = anguloRad * (180f / (float)Math.PI);

                        // Lado vazado oposto ao cabo (giro de 180°)
                        usarPadraoPadrao = false; // Inverte o padrão
                    }
                }
                else if (numTrechos >= 2)
                {
                    // POSTE COM 2+ TRECHOS: Analisa se estão alinhados
                    bool estaoAlinhados = VerificarSeTrechosEstaoAlinhados(trechosConectados, poste);

                    if (estaoAlinhados)
                    {
                        // TRECHOS EM LINHA RETA: Lados vazados para fora (perpendiculares à linha)
                        float dx = trechosConectados[0].Destino.X - trechosConectados[0].Origem.X;
                        float dy = trechosConectados[0].Destino.Y - trechosConectados[0].Origem.Y;
                        float anguloLinha = (float)Math.Atan2(dy, dx) * (180f / (float)Math.PI);

                        // Gira 90° para ficar perpendicular à linha
                        anguloRotacaoGraus = anguloLinha + 90f;
                        usarPadraoPadrao = true; // Padrão normal
                    }
                    else
                    {
                        // TRECHOS COM ÂNGULO: Lados vazados na bissetriz (fora do ângulo)
                        anguloRotacaoGraus = (anguloMedio * (180f / (float)Math.PI)) + 45f;
                        usarPadraoPadrao = true; // Padrão normal
                    }
                }

                // ============ DESENHA POSTE ROTACIONADO ============
                DesenharPosteRotacionado(g, ponto, largura, altura, anguloRotacaoGraus,
                                        Color.Black, Color.LightBlue, espessuraBorda, usarPadraoPadrao);
                //==================================================







                //float anguloRotacaoGraus = 0f;

                //if (numTrechos == 1 && trechosConectados.Count > 0)
                //{
                //    // POSTE COM 1 TRECHO: Alinhado com o cabo
                //    Trecho trecho = trechosConectados[0];

                //    // Determina a direção do trecho
                //    Poste outroPoste = (trecho.Origem.Id == poste.Id) ? trecho.Destino : trecho.Origem;

                //    if (outroPoste != null)
                //    {
                //        float dx = outroPoste.X - poste.X;
                //        float dy = outroPoste.Y - poste.Y;

                //        // Calcula ângulo em radianos, depois converte para graus
                //        float anguloRad = (float)Math.Atan2(dy, dx);
                //        anguloRotacaoGraus = anguloRad * (180f / (float)Math.PI);
                //    }
                //}
                //else if (numTrechos >= 2)
                //{
                //    // POSTE COM 2+ TRECHOS: Na bissetriz (45° do ângulo médio)
                //    anguloRotacaoGraus = (anguloMedio * (180f / (float)Math.PI)) + 45f;
                //}
                //// ============================================================





                //// ============ RETÂNGULO COMPLETO ============
                //// Define o retângulo
                //float metadeLargura = largura / 2;
                //float metadeAltura = altura / 2;

                //// Define o retângulo para DrawRectangle
                //RectangleF retangulo = new RectangleF(
                //    ponto.X - metadeLargura,  // X do canto superior esquerdo
                //    ponto.Y - metadeAltura,   // Y do canto superior esquerdo
                //    largura,                  // Largura
                //    altura                    // Altura
                //);

                //// ============ DESENHO DO RETÂNGULO COM ORIENTAÇÃO ============
                //// 1. Desenha o X (diagonais)
                //using (Pen penX = new Pen(Color.Black, espessuraBorda * 0.7f))
                //{
                //    g.DrawLine(penX, retangulo.Left, retangulo.Top, retangulo.Right, retangulo.Bottom);
                //    g.DrawLine(penX, retangulo.Right, retangulo.Top, retangulo.Left, retangulo.Bottom);
                //}

                //// 2. Desenha a borda
                //using (Pen penBorda = new Pen(Color.Black, espessuraBorda))
                //{
                //    g.DrawRectangle(penBorda, retangulo.X, retangulo.Y, retangulo.Width, retangulo.Height);
                //}

                //// 3. Preenche triângulos conforme a topologia
                //using (Brush brushPreenchido = new SolidBrush(Color.LightBlue))
                //{
                //    PointF centro = ponto;
                //    PointF cse = new PointF(retangulo.Left, retangulo.Top);
                //    PointF csd = new PointF(retangulo.Right, retangulo.Top);
                //    PointF cid = new PointF(retangulo.Right, retangulo.Bottom);
                //    PointF cie = new PointF(retangulo.Left, retangulo.Bottom);

                //    if (numTrechos == 1)
                //    {
                //        // POSTE COM 1 TRECHO: Triângulos alinhados com a rede
                //        DesenharTriangulosAlinhados(g, brushPreenchido, centro, cse, csd, cid, cie, anguloMedio);
                //    }
                //    else if (numTrechos >= 2)
                //    {
                //        // POSTE COM 2+ TRECHOS: Triângulos na bissetriz (padrão xadrez original)
                //        DesenharTriangulosBissetriz(g, brushPreenchido, centro, cse, csd, cid, cie, anguloMedio);
                //    }
                //    else
                //    {
                //        // POSTE SEM TRECHOS: Mantém padrão original
                //        DesenharTriangulosPadrao(g, brushPreenchido, centro, cse, csd, cid, cie);
                //    }
                //}
                //// ============ FIM DO RETÂNGULO ============

                #region

                //// 1. Desenha o X (diagonais) PRIMEIRO (para ficar atrás da borda)
                //using (Pen penX = new Pen(Color.Black, espessuraBorda * 0.7f))
                //{
                //    // Diagonal \ (superior esquerdo para inferior direito)
                //    g.DrawLine(penX,
                //        retangulo.Left, retangulo.Top,      // Canto superior esquerdo
                //        retangulo.Right, retangulo.Bottom); // Canto inferior direito

                //    // Diagonal / (superior direito para inferior esquerdo)
                //    g.DrawLine(penX,
                //        retangulo.Right, retangulo.Top,     // Canto superior direito
                //        retangulo.Left, retangulo.Bottom);  // Canto inferior esquerdo
                //}

                //// 2. Desenha o RETÂNGULO COMPLETO (borda)
                //using (Pen penBorda = new Pen(Color.Black, espessuraBorda))
                //{
                //    g.DrawRectangle(penBorda,
                //        retangulo.X, retangulo.Y,
                //        retangulo.Width, retangulo.Height);
                //}

                //// 3. Preenche 2 triângulos DIAGONALMENTE OPOSTOS
                //using (Brush brushPreenchido = new SolidBrush(Color.LightBlue))
                //{
                //    // Pontos dos cantos (apenas para referência)
                //    PointF cse = new PointF(retangulo.Left, retangulo.Top);      // Canto Superior Esquerdo
                //    PointF csd = new PointF(retangulo.Right, retangulo.Top);     // Canto Superior Direito
                //    PointF cid = new PointF(retangulo.Right, retangulo.Bottom);  // Canto Inferior Direito
                //    PointF cie = new PointF(retangulo.Left, retangulo.Bottom);   // Canto Inferior Esquerdo

                //    // Triângulo Superior Esquerdo
                //    PointF[] triangulo1 = new PointF[3] { cse, ponto, cie };
                //    g.FillPolygon(brushPreenchido, triangulo1);

                //    // Triângulo Inferior Direito
                //    PointF[] triangulo2 = new PointF[3] { ponto, cid, csd };
                //    g.FillPolygon(brushPreenchido, triangulo2);
                //}
                // ============ FIM DO RETÂNGULO ============





                //// 1. Retângulo vazado
                //using (Brush brushPreenchimento = new SolidBrush(Color.Green))
                //using (Pen penBorda = new Pen(Color.Blue, espessuraBorda)) // Use a variável calculada
                //{
                //    RectangleF retangulo = new RectangleF(
                //        ponto.X - tamanhoRetangulo / 2,
                //        ponto.Y - tamanhoRetangulo / 2,
                //        tamanhoRetangulo,
                //        tamanhoRetangulo
                //    );

                //    g.FillRectangle(brushPreenchimento, retangulo);
                //    g.DrawRectangle(penBorda,
                //        retangulo.X, retangulo.Y,
                //        retangulo.Width, retangulo.Height);
                //}

                //// 2. X no centro
                //using (Pen penX = new Pen(Color.Blue, espessuraBorda * 0.7f)) // Use a variável calculada
                //{
                //    // O tamanho do X é proporcional ao tamanho do retângulo
                //    float tamanhoX = tamanhoRetangulo * 0.6f; // 60% do tamanho do retângulo

                //    g.DrawLine(penX,
                //        ponto.X - tamanhoX / 2,
                //        ponto.Y - tamanhoX / 2,
                //        ponto.X + tamanhoX / 2,
                //        ponto.Y + tamanhoX / 2);

                //    g.DrawLine(penX,
                //        ponto.X + tamanhoX / 2,
                //        ponto.Y - tamanhoX / 2,
                //        ponto.X - tamanhoX / 2,
                //        ponto.Y + tamanhoX / 2);
                //}

                #endregion


                // 3. Textos
                if (escala > 0.4f)
                {
                    // Desenhar texto superior
                    using (Font fontSuperior = new Font("Arial", tamanhoFonteSuperior, FontStyle.Regular)) // Use a variável calculada
                    using (Brush brushSuperior = new SolidBrush(Color.Blue))
                    {
                        string textoSuperior = poste.ObterTextoSuperior();
                        if (!string.IsNullOrEmpty(textoSuperior))
                        {
                            SizeF textSizeSuperior = g.MeasureString(textoSuperior, fontSuperior);


                            // ============ POSIÇÃO RELATIVA (AutoCAD) ============
                            // Centralizado horizontalmente
                            float posXSuperior = ponto.X - textSizeSuperior.Width / 2;

                            // DISTÂNCIA RELATIVA: fator * altura do retângulo (ambos aumentam com zoom)
                            float distanciaRelativa = altura * fatorDistanciaSuperior;
                            float posYSuperior = ponto.Y - altura / 2 - distanciaRelativa - textSizeSuperior.Height;
                            // ====================================================

                            //// Posição: centro horizontal, acima do retângulo
                            //float posXSuperior = ponto.X - textSizeSuperior.Width / 2 + 105f;
                            //float posYSuperior = ponto.Y - tamanhoRetangulo / 2 - espacamentoTexto - textSizeSuperior.Height;

                            g.DrawString(textoSuperior, fontSuperior, brushSuperior, posXSuperior, posYSuperior);
                        }
                    }

                    // Desenhar texto inferior
                    using (Font fontInferior = new Font("Arial", tamanhoFonteInferior, FontStyle.Regular)) // 85% do tamanho superior
                    using (Brush brushInferior = new SolidBrush(Color.DarkRed))
                    {
                        string textoInferior = poste.ObterTextoInferior();
                        if (!string.IsNullOrEmpty(textoInferior))
                        {
                            SizeF textSizeInferior = g.MeasureString(textoInferior, fontInferior);

                            // ============ POSIÇÃO RELATIVA (AutoCAD) ============
                            // Centralizado horizontalmente
                            float posXInferior = ponto.X - textSizeInferior.Width / 2;

                            // DISTÂNCIA RELATIVA: fator * altura do retângulo
                            float distanciaRelativa = altura * fatorDistanciaInferior;
                            float posYInferior = ponto.Y + altura / 2 + distanciaRelativa;
                            // ====================================================

                            // Posição: centro horizontal, abaixo do retângulo
                            //float posXInferior = ponto.X - textSizeInferior.Width / 2 + 103f;
                            //float posYInferior = ponto.Y + tamanhoRetangulo / 2 + espacamentoTexto;

                            g.DrawString(textoInferior, fontInferior, brushInferior, posXInferior, posYInferior);
                        }
                    }


                    // 4. Símbolos especiais (precisa ajustar esses métodos também)
                    if (poste.TemParaRaios)
                    {
                        DesenharParaRaios(g, poste, new PointF(poste.X, poste.Y), escala);
                    }

                    if (poste.TemAterramento)
                    {
                        DesenharAterramento(g, poste, new PointF(poste.X, poste.Y), escala);
                    }
                }
            }
            // E também no CarregarPostes, no final:
            Debug.WriteLine($"Carregados {postes.Count} postes do banco");
        }

        private bool VerificarSeTrechosEstaoAlinhados(List<Trecho> trechosConectados, Poste posteAtual)
        {
            if (trechosConectados == null || trechosConectados.Count < 2) return false;

            // Pega os 2 primeiros trechos
            Trecho trecho1 = trechosConectados[0];
            Trecho trecho2 = trechosConectados[1];

            //// Encontra os postes conectados
            //Poste outroPoste1 = (trecho1.Origem.Id == posteAtual.Id) ? trecho1.Destino : trecho1.Origem;
            //Poste outroPoste2 = (trecho2.Origem.Id == posteAtual.Id) ? trecho2.Destino : trecho2.Origem;

            // Verifica se os trechos e seus postes são válidos
            if (trecho1 == null || trecho1.Origem == null || trecho1.Destino == null) return false;
            if (trecho2 == null || trecho2.Origem == null || trecho2.Destino == null) return false;

            // Encontra os postes conectados (que não são o poste atual)
            Poste outroPoste1 = null;
            Poste outroPoste2 = null;

            if (trecho1.Origem.Id == posteAtual.Id)
            {
                outroPoste1 = trecho1.Destino;
            }
            else if (trecho1.Destino.Id == posteAtual.Id)
            {
                outroPoste1 = trecho1.Origem;
            }

            if (trecho2.Origem.Id == posteAtual.Id)
            {
                outroPoste2 = trecho2.Destino;
            }
            else if (trecho2.Destino.Id == posteAtual.Id)
            {
                outroPoste2 = trecho2.Origem;
            }


            if (outroPoste1 == null || outroPoste2 == null) return false;

            // Calcula vetores
            float dx1 = outroPoste1.X - posteAtual.X;
            float dy1 = outroPoste1.Y - posteAtual.Y;

            float dx2 = outroPoste2.X - posteAtual.X;
            float dy2 = outroPoste2.Y - posteAtual.Y;

            // Calcula produto escalar (cos do ângulo)
            float produtoEscalar = dx1 * dx2 + dy1 * dy2;

            float mag1 = (float)Math.Sqrt(dx1 * dx1 + dy1 * dy1);
            float mag2 = (float)Math.Sqrt(dx2 * dx2 + dy2 * dy2);

            if (mag1 == 0 || mag2 == 0) return false;

            float cosAngulo = produtoEscalar / (mag1 * mag2);

            // Ângulo próximo de 180° significa cos próximo de -1
            // Verifica se cos está entre -0.9 e -1.0 (ângulo entre 154° e 180°)
            return cosAngulo < -0.9f;
        }


        //private bool VerificarSeTrechosEstaoAlinhados(List<Trecho> trechosConectados, Poste posteAtual)
        //{
        //    if (trechosConectados.Count < 2) return false;

        //    // Coleta todos os ângulos dos trechos (em graus para facilitar)
        //    List<float> angulosGraus = new List<float>();

        //    foreach (var trecho in trechosConectados)
        //    {
        //        Poste outroPoste = (trecho.Origem.Id == posteAtual.Id) ? trecho.Destino : trecho.Origem;

        //        if (outroPoste != null)
        //        {
        //            float dx = outroPoste.X - posteAtual.X;
        //            float dy = outroPoste.Y - posteAtual.Y;
        //            float anguloRad = (float)Math.Atan2(dy, dx);
        //            float anguloGraus = anguloRad * (180f / (float)Math.PI);
        //            angulosGraus.Add(anguloGraus);
        //        }
        //    }

        //    // Verifica se os ângulos estão opostos (180° de diferença)
        //    if (angulosGraus.Count == 2)
        //    {
        //        float angulo1 = angulosGraus[0];
        //        float angulo2 = angulosGraus[1];

        //        // Normaliza ângulos para 0-360
        //        angulo1 = NormalizarAngulo(angulo1);
        //        angulo2 = NormalizarAngulo(angulo2);

        //        // Calcula diferença absoluta
        //        float diff = Math.Abs(angulo1 - angulo2);

        //        // Normaliza para 0-180
        //        if (diff > 180f) diff = 360f - diff;

        //        // Considera alinhado se a diferença for próxima de 180° (±15°)
        //        return diff > 165f && diff < 195f;
        //    }

        //    return false;
        //}

        //// Método auxiliar para normalizar ângulo para 0-360
        //private float NormalizarAngulo(float anguloGraus)
        //{
        //    anguloGraus %= 360f;
        //    if (anguloGraus < 0) anguloGraus += 360f;
        //    return anguloGraus;
        //}


        //private bool VerificarSeTrechosEstaoAlinhados(List<Trecho> trechosConectados, Poste posteAtual)
        //{
        //    if (trechosConectados.Count < 2) return false;

        //    // Coleta todos os ângulos dos trechos
        //    List<float> angulos = new List<float>();

        //    foreach (var trecho in trechosConectados)
        //    {
        //        Poste outroPoste = (trecho.Origem.Id == posteAtual.Id) ? trecho.Destino : trecho.Origem;

        //        if (outroPoste != null)
        //        {
        //            float dx = outroPoste.X - posteAtual.X;
        //            float dy = outroPoste.Y - posteAtual.Y;
        //            float angulo = (float)Math.Atan2(dy, dx);
        //            angulos.Add(angulo);
        //        }
        //    }

        //    // Verifica se os ângulos estão opostos (180° de diferença)
        //    if (angulos.Count == 2)
        //    {
        //        float diff = Math.Abs(angulos[0] - angulos[1]);
        //        float diffNormalizada = Math.Abs(((diff + Math.PI) % (2 * Math.PI)) - Math.PI);

        //        // Considera alinhado se a diferença for próxima de 180° (±15°)
        //        return diffNormalizada > (165f * Math.PI / 180f) &&
        //               diffNormalizada < (195f * Math.PI / 180f);
        //    }

        //    return false;
        //}

        private void DesenharPosteRotacionado(Graphics g, PointF centro,
                                     float largura, float altura,
                                     float anguloRotacaoGraus,
                                     Color corBorda, Color corPreenchimento,
                                     float espessuraBorda, bool usarPadraoPadrao)
        {
            System.Drawing.Drawing2D.GraphicsState estadoOriginal = g.Save();
            g.TranslateTransform(centro.X, centro.Y);
            g.RotateTransform(anguloRotacaoGraus);

            float metadeLargura = largura / 2;
            float metadeAltura = altura / 2;

            RectangleF retangulo = new RectangleF(
                -metadeLargura,
                -metadeAltura,
                largura,
                altura
            );

            // 1. Desenha o X (diagonais)
            using (Pen penX = new Pen(Color.Black, espessuraBorda * 0.7f))
            {
                g.DrawLine(penX, retangulo.Left, retangulo.Top, retangulo.Right, retangulo.Bottom);
                g.DrawLine(penX, retangulo.Right, retangulo.Top, retangulo.Left, retangulo.Bottom);
            }

            // 2. Desenha a borda do retângulo
            using (Pen penBorda = new Pen(corBorda, espessuraBorda))
            {
                g.DrawRectangle(penBorda, retangulo.X, retangulo.Y, retangulo.Width, retangulo.Height);
            }

            // 3. Preenche triângulos conforme o padrão
            using (Brush brushPreenchido = new SolidBrush(corPreenchimento))
            {
                if (usarPadraoPadrao)
                {
                    // PADRÃO NORMAL: Superior esquerdo + Inferior direito preenchidos
                    g.FillPolygon(brushPreenchido, new PointF[] {
                new PointF(retangulo.Left, retangulo.Top),
                new PointF(0, 0),
                new PointF(retangulo.Left, retangulo.Bottom)
            });

                    g.FillPolygon(brushPreenchido, new PointF[] {
                new PointF(0, 0),
                new PointF(retangulo.Right, retangulo.Bottom),
                new PointF(retangulo.Right, retangulo.Top)
            });
                }
                else
                {
                    // PADRÃO INVERTIDO: Superior direito + Inferior esquerdo preenchidos
                    // (Lados vazados apontam na direção oposta)
                    g.FillPolygon(brushPreenchido, new PointF[] {
                new PointF(retangulo.Right, retangulo.Top),
                new PointF(0, 0),
                new PointF(retangulo.Right, retangulo.Bottom)
            });

                    g.FillPolygon(brushPreenchido, new PointF[] {
                new PointF(0, 0),
                new PointF(retangulo.Left, retangulo.Bottom),
                new PointF(retangulo.Left, retangulo.Top)
            });
                }
            }

            g.Restore(estadoOriginal);
        }

        //private void DesenharPosteRotacionado(Graphics g, PointF centro, float largura, float altura, float anguloRotacaoGraus,
        //                             Color corBorda, Color corPreenchimento, float espessuraBorda)
        //{
        //    // Salva o estado original do Graphics
        //    System.Drawing.Drawing2D.GraphicsState estadoOriginal = g.Save();

        //    // Aplica rotação no ponto central
        //    g.TranslateTransform(centro.X, centro.Y);
        //    g.RotateTransform(anguloRotacaoGraus);

        //    // Agora desenha o retângulo CENTRADO na origem (0,0)
        //    float metadeLargura = largura / 2;
        //    float metadeAltura = altura / 2;

        //    RectangleF retangulo = new RectangleF(
        //        -metadeLargura,  // X começa da esquerda do centro
        //        -metadeAltura,   // Y começa de cima do centro
        //        largura,
        //        altura
        //    );

        //    // 1. Desenha o X (diagonais)
        //    using (Pen penX = new Pen(Color.Black, espessuraBorda * 0.7f))
        //    {
        //        g.DrawLine(penX, retangulo.Left, retangulo.Top, retangulo.Right, retangulo.Bottom);
        //        g.DrawLine(penX, retangulo.Right, retangulo.Top, retangulo.Left, retangulo.Bottom);
        //    }

        //    // 2. Desenha a borda do retângulo
        //    using (Pen penBorda = new Pen(corBorda, espessuraBorda))
        //    {
        //        g.DrawRectangle(penBorda, retangulo.X, retangulo.Y, retangulo.Width, retangulo.Height);
        //    }

        //    // 3. Preenche 2 triângulos (padrão xadrez - fixo, não muda com rotação)
        //    using (Brush brushPreenchido = new SolidBrush(corPreenchimento))
        //    {
        //        // Triângulo Superior Esquerdo (sempre este, mesmo com rotação)
        //        g.FillPolygon(brushPreenchido, new PointF[] {
        //    new PointF(retangulo.Left, retangulo.Top),    // Canto superior esquerdo
        //    new PointF(0, 0),                             // Centro
        //    new PointF(retangulo.Left, retangulo.Bottom)  // Canto inferior esquerdo
        //});

        //        // Triângulo Inferior Direito (sempre este, mesmo com rotação)
        //        g.FillPolygon(brushPreenchido, new PointF[] {
        //    new PointF(0, 0),                             // Centro
        //    new PointF(retangulo.Right, retangulo.Bottom),// Canto inferior direito
        //    new PointF(retangulo.Right, retangulo.Top)    // Canto superior direito
        //});
        //    }

        //    // Restaura o estado original do Graphics
        //    g.Restore(estadoOriginal);
        //}

        // 1. Para postes com 1 trecho: Triângulos alinhados com a rede

        private void DesenharTriangulosAlinhados(Graphics g, Brush brush, PointF centro,
                                                PointF cse, PointF csd, PointF cid, PointF cie, float angulo)
        {
            // Converte ângulo para graus e normaliza
            float anguloGraus = angulo * (180f / (float)Math.PI);

            // Determina qual quadrante está a rede
            // 0° = Leste, 90° = Norte, 180° = Oeste, 270° = Sul
            int quadrante = (int)((anguloGraus + 45) / 90) % 4;
            if (quadrante < 0) quadrante += 4;

            switch (quadrante)
            {
                case 0: // Leste (0°-90°)
                        // Preenche triângulos do lado LESTE
                    g.FillPolygon(brush, new PointF[] { csd, centro, cid }); // Lado direito
                    break;

                case 1: // Norte (90°-180°)
                        // Preenche triângulos do lado NORTE
                    g.FillPolygon(brush, new PointF[] { cse, centro, csd }); // Lado superior
                    break;

                case 2: // Oeste (180°-270°)
                        // Preenche triângulos do lado OESTE
                    g.FillPolygon(brush, new PointF[] { cse, centro, cie }); // Lado esquerdo
                    break;

                case 3: // Sul (270°-360°)
                        // Preenche triângulos do lado SUL
                    g.FillPolygon(brush, new PointF[] { cie, centro, cid }); // Lado inferior
                    break;
            }
        }

        // 2. Para postes com 2+ trechos: Triângulos na bissetriz (padrão xadrez)
        private void DesenharTriangulosBissetriz(Graphics g, Brush brush, PointF centro,
                                                PointF cse, PointF csd, PointF cid, PointF cie, float anguloMedio)
        {
            // Usa o ângulo médio para rotacionar o padrão xadrez
            // Triângulos na diagonal do ângulo médio
            float anguloGraus = anguloMedio * (180f / (float)Math.PI);

            if (Math.Abs(Math.Sin(anguloMedio * 2)) > 0.5) // Ângulo ~45° ou ~135°
            {
                // Diagonal principal \
                g.FillPolygon(brush, new PointF[] { cse, centro, cie }); // Superior esquerdo
                g.FillPolygon(brush, new PointF[] { centro, cid, csd }); // Inferior direito
            }
            else
            {
                // Diagonal secundária /
                g.FillPolygon(brush, new PointF[] { csd, centro, cid }); // Superior direito
                g.FillPolygon(brush, new PointF[] { cie, centro, cse }); // Inferior esquerdo
            }
        }

        // 3. Para postes sem trechos: Padrão original
        private void DesenharTriangulosPadrao(Graphics g, Brush brush, PointF centro,
                                             PointF cse, PointF csd, PointF cid, PointF cie)
        {
            // Padrão xadrez original
            g.FillPolygon(brush, new PointF[] { cse, centro, cie }); // Superior esquerdo
            g.FillPolygon(brush, new PointF[] { centro, cid, csd }); // Inferior direito
        }

        private void DesenharInformacoesPoste(Graphics g, Poste poste, PointF ponto)
        {
            //float tamanho = 10f * viewport.Zoom;

            //// Lista de informações a mostrar
            //List<string> informacoes = new List<string>();

            //// Adicione conforme suas propriedades da classe Poste
            //if (!string.IsNullOrEmpty(poste.Numero))
            //    informacoes.Add($"Nº: {poste.Numero}");

            //if (!string.IsNullOrEmpty(poste.Barramento))
            //    informacoes.Add($"Barra: {poste.Barramento}");

            //if (!string.IsNullOrEmpty(poste.Tensao))
            //    informacoes.Add($"{poste.Tensao}kV");

            //if (poste.Altura > 0)
            //    informacoes.Add($"Alt: {poste.Altura}m");

            //// Se não tiver informações, não desenha nada
            //if (informacoes.Count == 0) return;

            //// Configurações de fonte
            //using (Font fonte = new Font("Arial", 7f * viewport.Zoom))
            //using (Brush brushTexto = new SolidBrush(Color.DarkBlue))
            //using (Brush brushFundo = new SolidBrush(Color.FromArgb(220, 255, 255, 255))) // Fundo branco semi-transparente
            //{
            //    float yAtual = ponto.Y - tamanho - 5 * viewport.Zoom;

            //    foreach (string info in informacoes)
            //    {
            //        if (string.IsNullOrEmpty(info)) continue;

            //        SizeF tamanhoTexto = g.MeasureString(info, fonte);

            //        // Fundo do texto
            //        RectangleF retanguloFundo = new RectangleF(
            //            ponto.X - tamanhoTexto.Width / 2 - 3,
            //            yAtual - 1,
            //            tamanhoTexto.Width + 6,
            //            tamanhoTexto.Height + 2
            //        );

            //        g.FillRectangle(brushFundo, retanguloFundo);
            //        g.DrawRectangle(Pens.LightGray,
            //            retanguloFundo.X, retanguloFundo.Y,
            //            retanguloFundo.Width, retanguloFundo.Height);

            //        // Texto
            //        g.DrawString(info, fonte, brushTexto,
            //            ponto.X - tamanhoTexto.Width / 2,
            //            yAtual);

            //        yAtual += tamanhoTexto.Height + 2;
            //    }
            //}
        }

        private void DesenharEquipamentos(Graphics g)
        {
            foreach (var poste in postes)
            {
                foreach (var equipamento in poste.Equipamentos)
                {
                    equipamento.AtualizarCor();

                    // Obter posição do equipamento (relativa ao poste)
                    PointF posicaoPoste = new PointF(poste.X, poste.Y);
                    PointF posicaoEquip = equipamento.ObterPosicaoAbsoluta(posicaoPoste);

                    float tamanho = 6f;

                    // Desenhar símbolo do equipamento
                    using (Brush brush = new SolidBrush(equipamento.Cor))
                    {
                        g.FillEllipse(brush,
                            posicaoEquip.X - tamanho / 2,
                            posicaoEquip.Y - tamanho / 2,
                            tamanho,
                            tamanho);
                    }

                    // Desenhar borda
                    using (Pen pen = new Pen(Color.Black, 1f))
                    {
                        g.DrawEllipse(pen,
                            posicaoEquip.X - tamanho / 2,
                            posicaoEquip.Y - tamanho / 2,
                            tamanho,
                            tamanho);
                    }

                    // Desenhar texto do equipamento
                    using (Font font = new Font("Arial", 7f / viewport.Zoom))
                    using (Brush brush = new SolidBrush(Color.Black))
                    {
                        string simbolo = equipamento.ObterSimbolo();
                        SizeF textSize = g.MeasureString(simbolo, font);
                        g.DrawString(simbolo, font, brush,
                            posicaoEquip.X - textSize.Width / 2,
                            posicaoEquip.Y - textSize.Height / 2);
                    }
                }
            }
        }

        private void DesenharParaRaios(Graphics g, Poste poste, PointF pontoMundo, float zoom)
        {
            // 'pontoMundo' já são coordenadas do mundo (X, Y do poste)
            // NÃO converte novamente com viewport.MundoParaTela!

            float tamanhoSimbolo = 8f * zoom;

            using (Pen pen = new Pen(Color.Red, 2f * zoom))
            {
                // Desenhar um raio saindo do poste
                float comprimento = 15f;
                float anguloRad = poste.Rotacao * (float)(Math.PI / 180);

                PointF fim = new PointF(
                    pontoMundo.X + (float)Math.Cos(anguloRad) * comprimento,
                    pontoMundo.Y + (float)Math.Sin(anguloRad) * comprimento
                );

                g.DrawLine(pen, pontoMundo.X, pontoMundo.Y, fim.X, fim.Y);
            }
        }

        private void DesenharAterramento(Graphics g, Poste poste, PointF pontoMundo, float zoom)
        {

            // 'pontoMundo' já são coordenadas do mundo (X, Y do poste)
            // NÃO converte novamente com viewport.MundoParaTela!

            float tamanhoSimbolo = 8f * zoom;

            using (Pen pen = new Pen(Color.Red, 2f * zoom))
            {
                // Desenhar um raio saindo do poste
                float comprimento = 15f;
                float anguloRad = poste.Rotacao * (float)(Math.PI / 180);

                PointF fim = new PointF(
                    pontoMundo.X + (float)Math.Cos(anguloRad) * comprimento,
                    pontoMundo.Y + (float)Math.Sin(anguloRad) * comprimento
                );

                g.DrawLine(pen, pontoMundo.X, pontoMundo.Y, fim.X, fim.Y);
            }

            //// PRIMEIRO converte as coordenadas
            ////PointF ponto = viewport.MundoParaTela(poste.X, poste.Y);

            //// Similar
            //float tamanhoSimbolo = 8f * zoom;

            //using (Pen pen = new Pen(Color.Green, 2f * zoom))
            //{
            //    // Desenhar símbolo de aterramento (T invertido)
            //    float tamanho = 10f;
            //    float anguloRad = poste.Rotacao * (float)(Math.PI / 180) + (float)(Math.PI / 2);

            //    PointF inicio = new PointF(
            //        poste.X + (float)Math.Cos(anguloRad) * tamanho,
            //        poste.Y + (float)Math.Sin(anguloRad) * tamanho
            //    );

            //    PointF fim1 = new PointF(inicio.X - tamanho / 2, inicio.Y + tamanho / 2);
            //    PointF fim2 = new PointF(inicio.X + tamanho / 2, inicio.Y + tamanho / 2);
            //    PointF fim3 = new PointF(inicio.X, inicio.Y + tamanho);

            //    g.DrawLine(pen, inicio, fim1);
            //    g.DrawLine(pen, inicio, fim2);
            //    g.DrawLine(pen, inicio, fim3);
            //}
        }

        private void DesenharInformacoes(Graphics g)
        {
            // Pode ser usado para texto adicional sobre a cena
        }

        private void DesenharLegenda(Graphics g)
        {
            // Desenhar HUD/legenda (coordenadas de tela, não afetadas pelo viewport)
            using (Font font = new Font("Arial", 9))
            using (Brush brush = new SolidBrush(Color.Black))
            {
                string info1 = $"Postes: {postes.Count} | Trechos: {trechos.Count} | Zoom: {viewport.Zoom:0.00}x";
                string info2 = $"Offset: ({viewport.OffsetX:F0}, {viewport.OffsetY:F0})";
                string info3 = "Rodinha: Zoom | Botão direito: Pan | Home: Ajustar | +/-: Zoom | 0: Reset";

                g.DrawString(info1, font, brush, 10, 10);
                g.DrawString(info2, font, brush, 10, 30);
                g.DrawString(info3, font, brush, 10, 50);

                // Legenda de cores
                int y = 60;
                string[] legendas = {
                "Postes existentes: CINZA(Concreto), MARROM(Madeira)",
                "Postes novos: VERMELHO",
                "Trechos MT: VERMELHO, Trechos BT: VERDE",
                "Trechos novos: AZUL",
                "Transformador: AZUL, Chave Fusível: LARANJA"
            };

                foreach (string legenda in legendas)
                {
                    g.DrawString(legenda, font, brush, 10, y);
                    y += 20;
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Debug.WriteLine($"=== ONPAINT INICIADO ===");
            Debug.WriteLine($"Postes: {postes.Count}, Trechos: {trechos.Count}");
            Debug.WriteLine($"Viewport - Zoom: {viewport.Zoom}, Offset: ({viewport.OffsetX}, {viewport.OffsetY})");



            //base.OnPaint(e);

            if (viewport == null || postes.Count == 0)
            {
                Debug.WriteLine("Viewport nulo ou sem postes");
                return;
            }

            // 1. Limpa a tela
            e.Graphics.Clear(Color.White);

            // 2. Suavizar o desenho
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // DEBUG: Verificar transformações
            Debug.WriteLine($"=== ONPAINT DEBUG ===");
            Debug.WriteLine($"Viewport: Zoom={viewport.Zoom}, OffsetX={viewport.OffsetX}, OffsetY={viewport.OffsetY}");
            Debug.WriteLine($"Tamanho tela: {this.ClientSize.Width}x{this.ClientSize.Height}");

            // ============ NÃO USE Transformações do Graphics! ============
            // REMOVA estas linhas:
            // GraphicsState estadoOriginal = e.Graphics.Save();
            // e.Graphics.TranslateTransform(viewport.OffsetX, viewport.OffsetY);
            // e.Graphics.ScaleTransform(viewport.Zoom, viewport.Zoom);
            // =============================================================

            // 3. DESENHA TRECHOS PRIMEIRO (com conversão manual)
            DesenharTrechos(e.Graphics);

            // 4. DESENHA POSTES (com conversão manual)
            DesenharPostes(e.Graphics);

            // 5. DESENHA EQUIPAMENTOS
            DesenharEquipamentos(e.Graphics);

            // 6. DESENHA INFORMAÇÕES
            DesenharInformacoes(e.Graphics);

            // ============ NÃO PRECISA RESTAURAR ============
            // e.Graphics.Restore(estadoOriginal);

            // 7. DESENHA LEGENDA/HUD
            DesenharLegenda(e.Graphics);



            //if (viewport == null || postes.Count == 0)
            //{
            //    Debug.WriteLine("Viewport nulo ou sem postes");
            //    return;
            //}

            //// 1. Limpa a tela
            //e.Graphics.Clear(Color.White);

            //// 2. Suavizar o desenho
            //e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            //e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            //// 3. Salva estado original do Graphics
            //GraphicsState estadoOriginal = e.Graphics.Save();

            //try
            //{
            //    // 4. Aplica transformações do Viewport
            //    e.Graphics.TranslateTransform(viewport.OffsetX, viewport.OffsetY);
            //    e.Graphics.ScaleTransform(viewport.Zoom, viewport.Zoom);

            //    // 5. DESENHA TRECHOS PRIMEIRO (com comprimentos)
            //    DesenharTrechos(e.Graphics);

            //    // 6. DESENHA POSTES (sobre os trechos)
            //    DesenharPostes(e.Graphics);

            //    // 7. DESENHA EQUIPAMENTOS (sobre os postes)
            //    DesenharEquipamentos(e.Graphics);

            //    // 8. DESENHA INFORMAÇÕES (opcional)
            //    DesenharInformacoes(e.Graphics);
            //}
            //finally
            //{
            //    // 9. Restaura estado original
            //    e.Graphics.Restore(estadoOriginal);
            //}

            //// 10. DESENHA LEGENDA/HUD (não afetada pelo viewport)
            //DesenharLegenda(e.Graphics);
        }

        private void VerificarCentralizacao()
        {
            if (postes.Count == 0) return;

            // Encontra min/max reais
            float minX = postes.Min(p => p.X);
            float maxX = postes.Max(p => p.X);
            float minY = postes.Min(p => p.Y);
            float maxY = postes.Max(p => p.Y);

            // Converte para tela
            PointF minTela = viewport.MundoParaTela(minX, minY);
            PointF maxTela = viewport.MundoParaTela(maxX, maxY);
            PointF centroMundo = viewport.MundoParaTela((minX + maxX) / 2, (minY + maxY) / 2);
            PointF centroTela = new PointF(this.ClientSize.Width / 2, this.ClientSize.Height / 2);

            Debug.WriteLine($"=== VERIFICAÇÃO ===");
            Debug.WriteLine($"Mundo: ({minX:F0}, {minY:F0}) a ({maxX:F0}, {maxY:F0})");
            Debug.WriteLine($"Tela: ({minTela.X:F0}, {minTela.Y:F0}) a ({maxTela.X:F0}, {maxTela.Y:F0})");
            Debug.WriteLine($"Centro Mundo->Tela: ({centroMundo.X:F0}, {centroMundo.Y:F0})");
            Debug.WriteLine($"Centro Tela: ({centroTela.X:F0}, {centroTela.Y:F0})");
            Debug.WriteLine($"Diferença centro: {Math.Abs(centroMundo.X - centroTela.X):F0}, " +
                           $"{Math.Abs(centroMundo.Y - centroTela.Y):F0}");
        }
    }
}
