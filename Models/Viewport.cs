using Isabel_Visualizador_Proj;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;

public class Viewport
{
    // Propriedades principais
    private float _zoom = 1.0f;
    private PointF _offset = new PointF(0, 0);
    private PointF _offsetNormalizacao  = new PointF(0, 0);



    // Propriedades públicas para debug
    public float _Zoom => _zoom;
    public PointF _Offset => _offset;
    public PointF _OffsetNormalizacao => _offsetNormalizacao;

    // ... resto dos métodos


    // Controles de pan
    private PointF? _mouseInicioPan = null;
    public bool Panning { get; private set; } = false;

    // Propriedades públicas (com validação)
    public float Zoom
    {
        get => _zoom;
        set => _zoom = Math.Max(0.1f, Math.Min(100f, value)); // Limites: 0.1 a 100
    }

    public float OffsetX
    {
        get => _offset.X;
        set => _offset.X = value;
    }

    public float OffsetY
    {
        get => _offset.Y;
        set => _offset.Y = value;
    }

    public PointF Offset
    {
        get => _offset;
        set => _offset = value;
    }

    // Converte coordenadas da tela para mundo
    public PointF TelaParaMundo(float x, float y)
    {
        // 1. Remove offset da viewport
        float xSemOffset = (x - _offset.X) / _zoom;
        float ySemOffset = (y - _offset.Y) / _zoom;

        // 2. Adiciona o offset de normalização
        return new PointF(
            xSemOffset + _offsetNormalizacao.X,
            ySemOffset + _offsetNormalizacao.Y
        );
    }

    // Converte coordenadas do mundo para tela
    public PointF MundoParaTela(float x, float y)
    {
        // 1. Primeiro normaliza as coordenadas subtraindo o offset
        float xNormalizado = x - _offsetNormalizacao.X;
        float yNormalizado = y - _offsetNormalizacao.Y;

        // 2. Aplica zoom e offset da viewport
        return new PointF(
            xNormalizado * _zoom + _offset.X,
            yNormalizado * _zoom + _offset.Y
        );
    }

    // Zoom em um ponto específico (versão simplificada)
    public void ZoomParaPonto(PointF pontoTela, bool aumentar)
    {
        float zoomAntigo = _zoom;

        // Aplica zoom
        _zoom *= aumentar ? 1.2f : 1 / 1.2f;
        _zoom = Math.Max(0.01f, Math.Min(_zoom, 5f));

        // Mantém o ponto fixo na tela
        float fator = 1f - (_zoom / zoomAntigo);
        _offset.X += (pontoTela.X - _offset.X) * fator;
        _offset.Y += (pontoTela.Y - _offset.Y) * fator;



        //// Ponto do mundo ANTES do zoom (com normalização)
        //PointF pontoMundoAntes = TelaParaMundo(pontoTela);

        //// Aplica zoom
        //if (aumentar)
        //    _zoom *= 1.2f;
        //else
        //    _zoom /= 1.2f;

        //// Limites do Zoom
        //_zoom = Math.Max(0.1f, Math.Min(_zoom, 100f));

        //// Ponto do mundo DEPOIS do zoom (com mesmo offset)
        //// **CORREÇÃO:** Usamos cálculo manual para evitar chamar TelaParaMundo de novo
        //float deltaXMundo = pontoMundoAntes.X - ((pontoTela.X - _offset.X) / _zoom + _offsetNormalizacao.X);
        //float deltaYMundo = pontoMundoAntes.Y - ((pontoTela.Y - _offset.Y) / _zoom + _offsetNormalizacao.Y);

        //// **CORREÇÃO:** O cálculo do offset deve ser diferente
        //// Queremos que após o zoom, o ponto na tela corresponda ao mesmo ponto do mundo
        //// Fórmula correta: offset' = offset + (pontoTela - offset) * (1 - novoZoom/zoomAntigo)
        //float zoomAntigo = aumentar ? _zoom / 1.2f : _zoom * 1.2f;
        //float fator = 1f - (_zoom / zoomAntigo);

        //_offset.X += (pontoTela.X - _offset.X) * fator;
        //_offset.Y += (pontoTela.Y - _offset.Y) * fator;





        //PointF pontoMundoAntes = TelaParaMundo(pontoTela);

        //if (aumentar)
        //    _zoom *= 1.2f;
        //else
        //    _zoom /= 1.2f;

        //// Limites do Zoom
        //_zoom = Math.Max(0.1f, Math.Min(_zoom, 100f));

        //// Ajusta o Offset para manter o ponto fixo
        //PointF pontoMundoDepois = TelaParaMundo(pontoTela);

        //// Fórmula corrigida para manter ponto fixo
        //_offset.X += (pontoMundoAntes.X - pontoMundoDepois.X) * _zoom;
        //_offset.Y += (pontoMundoAntes.Y - pontoMundoDepois.Y) * _zoom;
    }

    // Método simplificado de zoom (sem manter ponto fixo)
    // Método simplificado de zoom (centrado)

    // Zoom com fator arbitrário
    public void AplicarZoom(float fator, PointF centroTela)
    {
        // Guarda o zoom anterior
        float zoomAntigo = _zoom;

        // Aplica zoom
        _zoom *= fator;
        _zoom = Math.Max(0.1f, Math.Min(_zoom, 5f));

        // **CORREÇÃO:** Fórmula simplificada e correta
        // Mantém o ponto fixo na tela
        float fatorCorrecao = 1f - (_zoom / zoomAntigo);

        _offset.X += (centroTela.X - _offset.X) * fatorCorrecao;
        _offset.Y += (centroTela.Y - _offset.Y) * fatorCorrecao;
    }

    // Pan (arrastar)
    public void IniciarPan(PointF mousePos)
    {
        Panning = true;
        _mouseInicioPan = mousePos;
    }

    public void AtualizarPan(PointF mousePos)
    {
        if (!Panning || !_mouseInicioPan.HasValue) return;

        float dx = mousePos.X - _mouseInicioPan.Value.X;
        float dy = mousePos.Y - _mouseInicioPan.Value.Y;

        _offset.X += dx;
        _offset.Y += dy;
        _mouseInicioPan = mousePos;
    }

    public void PararPan()
    {
        Panning = false;
        _mouseInicioPan = null;
    }

    // Método para arrastar (pan)
    public void Pan(float deltaX, float deltaY)
    {
        _offset.X += deltaX;
        _offset.Y += deltaY;
    }

    // Ajustar viewport para mostrar todas os elementos
    public void Centralizar(float minX, float maxX, float minY, float maxY, int larguraTela, int alturaTela, List<Poste> postes)
    {
        if (maxX <= minX || maxY <= minY || postes.Count == 0)
        {
            _zoom = 1.0f;
            _offset = new PointF(0, 0);
            _offsetNormalizacao = new PointF(0, 0); // Inicializa aqui também
            return;
        }

        // 1. Guarda os valores originais mínimos para normalização
        _offsetNormalizacao = new PointF(minX, minY);

        // 2. Normaliza as coordenadas (faz com que minX e minY virem 0)
        float larguraMundo = maxX - minX;
        float alturaMundo = maxY - minY;

        //// Agora trabalhamos com coordenadas normalizadas
        //maxX -= minX;
        //minX = 0;
        //maxY -= minY;
        //minY = 0;

        // 3. Calcula zoom normalmente
        float zoomX = larguraTela / larguraMundo;
        float zoomY = alturaTela / alturaMundo;

        _zoom = Math.Min(zoomX, zoomY);
        _zoom = Math.Max(0.01f, Math.Min(_zoom, 100f));

        // 4. Centro do mundo (já normalizado)
        float centroXMundo = larguraMundo / 2;
        float centroYMundo = alturaMundo / 2;

        // 5. Centro da tela
        float centroXTela = larguraTela / 2;
        float centroYTela = alturaTela / 2;

        // 6. Calcula offset para centralizar
        _offset.X = centroXTela - centroXMundo * _zoom;
        _offset.Y = centroYTela - centroYMundo * _zoom;



        //if (maxX <= minX || maxY <= minY || postes.Count == 0)
        //{
        //    _zoom = 1.0f;
        //    _offset = new PointF(0, 0);
        //    _offsetNormalizacao = new PointF(0, 0);  // Inicializa aqui também
        //    return;
        //}

        //// Primeiro, normaliza as coordenadas subtraindo o mínimo
        //float offsetNormalizacaoX = minX;
        //float offsetNormalizacaoY = minY;

        //float larguraMundo = maxX - minX;
        //float alturaMundo = maxY - minY;

        //// Agora as coordenadas começam em ~0
        //maxX -= offsetNormalizacaoX;
        //minX = 0;
        //maxY -= offsetNormalizacaoY;
        //minY = 0;

        //// Calcula zoom para caber tudo
        //float zoomX = larguraTela / larguraMundo;
        //float zoomY = alturaTela / alturaMundo;

        //_zoom = Math.Min(zoomX, zoomY);
        //_zoom = Math.Max(0.0001f, Math.Min(_zoom, 100f)); // Permite zoom menor

        //// Centro do mundo (já normalizado)
        //float centroXMundo = (minX + maxX) / 2;
        //float centroYMundo = (minY + maxY) / 2;

        //// Centro da tela
        //float centroXTela = larguraTela / 2;
        //float centroYTela = alturaTela / 2;

        //// Calcula offset para centralizar
        //_offset.X = centroXTela - centroXMundo * _zoom;
        //_offset.Y = centroYTela - centroYMundo * _zoom;

        //// IMPORTANTE: Guarda o offset de normalização para usar depois
        //_offsetNormalizacao = new PointF(offsetNormalizacaoX, offsetNormalizacaoY);





        // ==============================================================


        //if (maxX <= minX || maxY <= minY || postes.Count == 0)
        //{
        //    _zoom = 1.0f;
        //    _offset = new PointF(0, 0);
        //    return;
        //}

        //float larguraMundo = maxX - minX;
        //float alturaMundo = maxY - minY;

        //// NÃO adiciona margem aqui - já foi adicionada no AjustarViewport()
        //// float margem = 0.2f; // REMOVER
        //// larguraMundo *= (1 + margem); // REMOVER
        //// alturaMundo *= (1 + margem); // REMOVER

        //// Calcula zoom para caber tudo
        //float zoomX = larguraTela / larguraMundo;
        //float zoomY = alturaTela / alturaMundo;

        //_zoom = Math.Min(zoomX, zoomY);
        //_zoom = Math.Max(0.01f, Math.Min(_zoom, 100f));

        //// Centro do mundo (sem ajuste de margem)
        //float centroXMundo = (minX + maxX) / 2;
        //float centroYMundo = (minY + maxY) / 2;

        //// Centro da tela
        //float centroXTela = larguraTela / 2;
        //float centroYTela = alturaTela / 2;

        //// Calcula offset para centralizar
        //_offset.X = centroXTela - centroXMundo * _zoom;
        //_offset.Y = centroYTela - centroYMundo * _zoom;


        // DEBUG: Verificar cálculo final
        //Debug.WriteLine($"=== DEBUG CENTRALIZAR ===");
        //Debug.WriteLine($"larguraMundo={larguraMundo:F2}, alturaMundo={alturaMundo:F2}");
        //Debug.WriteLine($"zoomX={zoomX:F6}, zoomY={zoomY:F6}");
        //Debug.WriteLine($"Zoom final: {_zoom:F6}");
        //Debug.WriteLine($"Offset: X={_offset.X:F2}, Y={_offset.Y:F2}");
        //Debug.WriteLine($"centroXMundo={centroXMundo:F2}, centroYMundo={centroYMundo:F2}");
        //Debug.WriteLine($"centroXTela={centroXTela:F2}, centroYTela={centroYTela:F2}");



        //if (maxX <= minX || maxY <= minY || postes.Count == 0)
        //{
        //    // Dados inválidos, usa zoom padrão
        //    _zoom = 1.0f;
        //    _offset = new PointF(0, 0);
        //    return;
        //}

        //float larguraMundo = maxX - minX;
        //float alturaMundo = maxY - minY;

        //// Adiciona 20% de margem
        //float margem = 0.2f;
        //larguraMundo *= (1 + margem);
        //alturaMundo *= (1 + margem);

        //// Calcula zoom para caber tudo
        //float zoomX = larguraTela / larguraMundo;
        //float zoomY = alturaTela / alturaMundo;

        //_zoom = Math.Min(zoomX, zoomY);
        //_zoom = Math.Max(0.01f, Math.Min(_zoom, 100f)); // Limites mais amplos

        //// Centro do mundo
        //float centroXMundo = minX + (larguraMundo / (1 + margem)) / 2;
        //float centroYMundo = minY + (alturaMundo / (1 + margem)) / 2;

        //// Centro da tela
        //float centroXTela = larguraTela / 2;
        //float centroYTela = alturaTela / 2;

        //// Calcula offset para centralizar
        //_offset.X = centroXTela - centroXMundo * _zoom;
        //_offset.Y = centroYTela - centroYMundo * _zoom;


        //=================================================================================================

        //if (maxX <= minX || maxY <= minY)
        //{
        //    // Dados inválidos, usa valores padrão
        //    _zoom = 1.0f;
        //    _offset = new PointF(0, 0);
        //    return;
        //}

        //float larguraMundo = maxX - minX;
        //float alturaMundo = maxY - minY;

        //// Calcula zoom para caber tudo na tela (com 10% de margem)
        //float zoomX = (larguraTela * 0.9f) / larguraMundo;
        //float zoomY = (alturaTela * 0.9f) / alturaMundo;

        //_zoom = Math.Min(zoomX, zoomY);
        //_zoom = Math.Max(0.1f, Math.Min(_zoom, 100f)); // Limites

        //// Centro do mundo
        //float centroXMundo = minX + larguraMundo / 2;
        //float centroYMundo = minY + alturaMundo / 2;

        //// Centro da tela
        //float centroXTela = larguraTela / 2;
        //float centroYTela = alturaTela / 2;

        //// Calcula offset para centralizar
        //_offset.X = centroXTela - centroXMundo * _zoom;
        //_offset.Y = centroYTela - centroYMundo * _zoom;
    }

    // Método alternativo (usando RectangleF)
    public void AjustarParaElementos(RectangleF boundsMundo, Size tamanhoTela)
    {
        if (boundsMundo.Width == 0 || boundsMundo.Height == 0)
        {
            _zoom = 1.0f;
            _offset = new PointF(0, 0);
            return;
        }

        // Calcula zoom para caber tudo na tela (com 10% de margem)
        float zoomX = (tamanhoTela.Width * 0.9f) / boundsMundo.Width;
        float zoomY = (tamanhoTela.Height * 0.9f) / boundsMundo.Height;

        _zoom = Math.Min(zoomX, zoomY);
        _zoom = Math.Max(0.1f, Math.Min(_zoom, 100f)); // Limites

        // Centro do mundo
        float centroXMundo = boundsMundo.X + boundsMundo.Width / 2;
        float centroYMundo = boundsMundo.Y + boundsMundo.Height / 2;

        // Centro da tela
        float centroXTela = tamanhoTela.Width / 2;
        float centroYTela = tamanhoTela.Height / 2;

        // Calcula offset para centralizar
        _offset.X = centroXTela - centroXMundo * _zoom;
        _offset.Y = centroYTela - centroYMundo * _zoom;
    }

    // Obtém transformação para Graphics
    public Matrix ObterTransformacao()
    {
        Matrix matrix = new Matrix();
        matrix.Translate(_offset.X, _offset.Y);
        matrix.Scale(_zoom, _zoom);
        return matrix;
    }

    // Método para aplicar transformação diretamente no Graphics
    public void AplicarTransformacao(Graphics g)
    {
        g.TranslateTransform(_offset.X, _offset.Y);
        g.ScaleTransform(_zoom, _zoom);
    }

    // Reset para valores padrão
    public void Reset()
    {
        _zoom = 1.0f;
        _offset = new PointF(0, 0);
        Panning = false;
        _mouseInicioPan = null;
    }

    // Método para debug
    public override string ToString()
    {
        return $"Viewport: Zoom={_zoom:F2}, Offset=({_offset.X:F1}, {_offset.Y:F1})";
    }
}
