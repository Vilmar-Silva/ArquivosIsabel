using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Isabel_Visualizador_Proj.Models
{
    public class AnaliseTopologica
    {
        public static List<Trecho> EncontrarTrechosConectados(Poste poste, List<Trecho> todosTrechos)
        {
            List<Trecho> trechosConectados = new List<Trecho>();

            foreach (var trecho in todosTrechos)
            {
                if (trecho.Origem != null && trecho.Origem.Id == poste.Id)
                    trechosConectados.Add(trecho);

                if (trecho.Destino != null && trecho.Destino.Id == poste.Id)
                    trechosConectados.Add(trecho);
            }

            return trechosConectados;
        }

        public static float CalcularAnguloMedio(List<Trecho> trechosConectados, Poste posteAtual)
        {
            if (trechosConectados == null || trechosConectados.Count == 0) return 0f;

            float somaAngulos = 0f;
            int contador = 0;

            foreach (var trecho in trechosConectados)
            {
                // Verificar se os objetos necessários existem
                if (trecho == null || trecho.Origem == null || trecho.Destino == null)
                    continue; // Pula trechos inválidos

                // Determina qual é o outro poste (não o atual)
                Poste outroPoste = null;
                if (trecho.Origem.Id == posteAtual.Id)
                {
                    outroPoste = trecho.Destino;
                }
                else if (trecho.Destino.Id == posteAtual.Id)
                {
                    outroPoste = trecho.Origem;
                }

                // Se não encontrou o outro poste, continua
                if (outroPoste == null)
                    continue;

                // Cálculo do ângulo entre posteAtual e outroPoste
                float angulo = CalcularAnguloEntrePostes(posteAtual, outroPoste);
                somaAngulos += angulo;
                contador++;
            }

            return contador > 0 ? somaAngulos / contador : 0f;
        }

        // Método auxiliar para calcular ângulo entre dois postes
        private static float CalcularAnguloEntrePostes(Poste p1, Poste p2)
        {
            // Exemplo: calcular ângulo baseado nas coordenadas
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            return (float)Math.Atan2(dy, dx); // Retorna ângulo em radianos
        }


        //if (trechosConectados.Count == 0) return 0f;

        //float somaAngulos = 0f;
        //int contador = 0;

        //foreach (var trecho in trechosConectados)
        //{
        //    // Determina qual é o outro poste (não o atual)
        //    Poste outroPoste = (trecho.Origem.Id == posteAtual.Id) ? trecho.Destino : trecho.Origem;

        //    if (outroPoste != null)
        //    {
        //        // Calcula ângulo entre os postes
        //        float dx = outroPoste.X - posteAtual.X;
        //        float dy = outroPoste.Y - posteAtual.Y;
        //        float angulo = (float)Math.Atan2(dy, dx); // Radianos

        //        somaAngulos += angulo;
        //        contador++;
        //    }
        //}
        //return (contador > 0) ? somaAngulos / contador : 0f;
    }
}
