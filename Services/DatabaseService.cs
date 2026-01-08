using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;

namespace Isabel_Visualizador_Proj.Services
{
    public class DatabaseService
    {
        private string connectionString;

        public DatabaseService()
        {
            // CORREÇÃO 1: Caminho correto para o Access
            string databasePath = Application.StartupPath + @"\EletroPocket.mdb";

            // CORREÇÃO 2: Provider correto (depende da versão do Access)
            if (System.IO.File.Exists(databasePath))
            {
                connectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + databasePath + ";";
            }
            else
            {
                // Tenta ACE OLEDB para Access 2007+
                connectionString = @"Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + databasePath + ";";
            }
        }

        public List<Poste> CarregarPostes()
        {
            MessageBox.Show("Tentando conectar ao banco...\n" + connectionString,
                "Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);

            List<Poste> postes = new List<Poste>();

            if (string.IsNullOrEmpty(connectionString))
            {
                MessageBox.Show("Banco não encontrado. Usando dados de teste.",
                    "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return postes; // Vazio = Paint.cs usará dados de teste
            }

            try
            {
                using (var conexao = new OleDbConnection(connectionString))
                {
                    conexao.Open();

                    // **TENTA LER COORDENADAS DIRETAMENTE DOS PONTOS_COLETADOS**
                    string sql = @"
                SELECT 
                    PG.PGF_ID,
                    PG.PGF_BARRAMENTO,
                    PG.TB_CT_ID,
                    PG.TB_PR_ID,
                    PG.PGF_IND_ATER,
                    PG.FLAG_EXIST,
                    PC.UTMX,
                    PC.UTMY
                FROM 
                    PONTO_GEOGRAFICO PG
                    INNER JOIN PONTOS_COLETADOS PC 
                    ON CStr(PG.PGF_ID) = PC.PGF_ID
                WHERE 
                    PC.UTMX IS NOT NULL 
                    AND PC.UTMY IS NOT NULL
                    AND TRIM(PC.UTMX) <> ''
                    AND TRIM(PC.UTMY) <> ''
                    AND PC.UTMX <> '0'
                    AND PC.UTMY <> '0'";

                    var comando = new OleDbCommand(sql, conexao);

                    using (var reader = comando.ExecuteReader())
                    {
                        int contador = 0;
                        while (reader.Read())
                        {
                            try
                            {
                                // ID do poste
                                string idStr = reader["PGF_ID"]?.ToString();
                                if (string.IsNullOrEmpty(idStr)) continue;

                                if (!int.TryParse(idStr, out int id))
                                    continue;

                                // COORDENADAS REAIS
                                string xStr = reader["UTMX"]?.ToString();
                                string yStr = reader["UTMY"]?.ToString();

                                if (!TryParseCoordenada(xStr, out float x) ||
                                    !TryParseCoordenada(yStr, out float y))
                                {
                                    // Se coordenadas inválidas, usa posição simulada
                                    x = 100 + (contador * 150);
                                    y = 100 + ((contador % 4) * 120);
                                }


                                // ============ CORREÇÃO AQUI ============
                                // INVERTE o eixo Y para correção de visualização
                                y = -y; // ADICIONE ESTA LINHA
                                        // =======================================

                                // DEBUG: Verificar valores
                                Debug.WriteLine($"Poste {id}: X={x}, Y={y} (Y invertido)");



                                // Tipo do poste
                                string tbCtId = reader["TB_CT_ID"]?.ToString() ?? "";
                                ParseTB_CT_ID(tbCtId, out string material, out int altura, out int esforco);

                                var poste = new Poste
                                {
                                    Id = id,
                                    Numero = reader["PGF_BARRAMENTO"]?.ToString() ?? $"P-{id}",
                                    X = x,
                                    Y = y,
                                    Material = material,
                                    Altura = altura,
                                    Esforco = esforco,
                                    TemParaRaios = (reader["TB_PR_ID"]?.ToString() ?? "0") == "3",
                                    TemAterramento = (reader["PGF_IND_ATER"]?.ToString() ?? "N").ToUpper() == "S",
                                    IsExistente = (reader["FLAG_EXIST"]?.ToString() ?? "N").ToUpper() == "S",
                                    Equipamentos = new List<Equipamento>()
                                };

                                poste.AtualizarCor();
                                postes.Add(poste);
                                contador++;

                                // Limita para teste
                                //if (contador >= 100) break; // Máximo 100 postes para teste
                            }
                            catch (Exception regEx)
                            {
                                Debug.WriteLine($"Erro no poste {contador}: {regEx.Message}");
                                continue;
                            }
                        }

                        MessageBox.Show($"Carregados: {postes.Count} postes COM COORDENADAS REAIS",
                            "Sucesso!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}\nUsando dados de teste.",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return new List<Poste>(); // Vazio = usa dados de teste
            }

            return postes;

        }

        public List<Trecho> CarregarTrechos()
        {
            //var trechos = new List<Trecho>();

            //try
            //{
            //    using (var conexao = new OleDbConnection(connectionString))
            //    {
            //        conexao.Open();

            //        string sql = @"
            //        SELECT 
            //            TRC_ID,
            //            PGF_ID,
            //            PGF_ID_P,
            //            TB_TR_ID,
            //            TB_BT_ID_FASE,
            //            TB_CDF_ID,
            //            STATUS
            //        FROM TRECHO_DE_REDE
            //        WHERE PGF_ID IS NOT NULL 
            //        AND PGF_ID_P IS NOT NULL
            //        AND PGF_ID <> ''
            //        AND PGF_ID_P <> ''";

            //        var comando = new OleDbCommand(sql, conexao);
            //        using (var reader = comando.ExecuteReader())
            //        {
            //            while (reader.Read())
            //            {
            //                // CORREÇÃO 7: Converter IDs corretamente
            //                int id, posteOrigemId, posteDestinoId;

            //                if (!int.TryParse(reader["TRC_ID"].ToString(), out id) ||
            //                    !int.TryParse(reader["PGF_ID"].ToString(), out posteOrigemId) ||
            //                    !int.TryParse(reader["PGF_ID_P"].ToString(), out posteDestinoId))
            //                    continue;

            //                var trecho = new Trecho
            //                {
            //                    Id = id,
            //                    PosteOrigemId = posteOrigemId,
            //                    PosteDestinoId = posteDestinoId,
            //                    IsMT = (reader["TB_TR_ID"]?.ToString() ?? "").ToUpper() == "PA",
            //                    IsBT = (reader["TB_TR_ID"]?.ToString() ?? "").ToUpper() == "AS",
            //                    Bitola = reader["TB_BT_ID_FASE"]?.ToString() ?? "",
            //                    Fases = reader["TB_CDF_ID"]?.ToString() ?? "",
            //                    IsNovo = (reader["STATUS"]?.ToString() ?? "").ToUpper() == "N"
            //                };

            //                trechos.Add(trecho);
            //            }
            //        }
            //    }

            //    MessageBox.Show($"Carregados: {trechos.Count} trechos do banco.",
            //        "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"Erro ao carregar trechos: {ex.Message}",
            //        "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //}

            //return trechos;



            var trechos = new List<Trecho>();

            if (string.IsNullOrEmpty(connectionString))
            {
                return trechos;
            }

            try
            {
                using (var conexao = new OleDbConnection(connectionString))
                {
                    conexao.Open();

                    // **QUERY MAIS SIMPLES POSSÍVEL**
                    string sql = "SELECT TRC_ID, PGF_ID, PGF_ID_P FROM TRECHO_DE_REDE";

                    var comando = new OleDbCommand(sql, conexao);
                    using (var reader = comando.ExecuteReader())
                    {
                        int contador = 0;
                        while (reader.Read()) // REMOVA: && contador < 35
                        {
                            try
                            {
                                // Tenta ler como string primeiro
                                string idStr = reader["TRC_ID"]?.ToString();
                                string origemStr = reader["PGF_ID"]?.ToString();
                                string destinoStr = reader["PGF_ID_P"]?.ToString();

                                if (string.IsNullOrEmpty(idStr) ||
                                    string.IsNullOrEmpty(origemStr) ||
                                    string.IsNullOrEmpty(destinoStr))
                                    continue;

                                // Tenta converter para inteiros
                                int id, origemId, destinoId;

                                if (!int.TryParse(idStr, out id)) continue;
                                if (!int.TryParse(origemStr, out origemId)) continue;
                                if (!int.TryParse(destinoStr, out destinoId)) continue;

                                var trecho = new Trecho
                                {
                                    Id = id,
                                    PosteOrigemId = origemId,
                                    PosteDestinoId = destinoId,
                                    IsMT = true,
                                    IsBT = false,
                                    Bitola = "S-10",
                                    Fases = "ABC",
                                    IsNovo = false
                                };

                                trechos.Add(trecho);
                                contador++;

                                // Opcional: Debug a cada 20 trechos
                                if (contador % 20 == 0)
                                {
                                    Debug.WriteLine($"Carregado trecho {contador}: ID={id}");
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }
                    }

                    // Mostra quantidade REAL carregada
                    MessageBox.Show($"Carregados: {trechos.Count} trechos de REDE",
                        "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Debug.WriteLine($"=== TOTAL TRECHOS CARREGADOS: {trechos.Count} ===");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro em trechos: {ex.Message}");
                MessageBox.Show($"Erro ao carregar trechos: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return trechos;







            //var trechos = new List<Trecho>();

            //if (string.IsNullOrEmpty(connectionString))
            //{
            //    return trechos;
            //}

            //try
            //{
            //    using (var conexao = new OleDbConnection(connectionString))
            //    {
            //        conexao.Open();

            //        // **QUERY MAIS SIMPLES POSSÍVEL**
            //        string sql = "SELECT TRC_ID, PGF_ID, PGF_ID_P FROM TRECHO_DE_REDE";

            //        var comando = new OleDbCommand(sql, conexao);
            //        using (var reader = comando.ExecuteReader())
            //        {
            //            int contador = 0;
            //            while (reader.Read() && contador < 35) // Limita a 20 trechos
            //            {
            //                try
            //                {
            //                    // Tenta ler como string primeiro
            //                    string idStr = reader["TRC_ID"]?.ToString();
            //                    string origemStr = reader["PGF_ID"]?.ToString();
            //                    string destinoStr = reader["PGF_ID_P"]?.ToString();

            //                    if (string.IsNullOrEmpty(idStr) ||
            //                        string.IsNullOrEmpty(origemStr) ||
            //                        string.IsNullOrEmpty(destinoStr))
            //                        continue;

            //                    // Tenta converter para inteiros
            //                    int id, origemId, destinoId;

            //                    if (!int.TryParse(idStr, out id)) continue;
            //                    if (!int.TryParse(origemStr, out origemId)) continue;
            //                    if (!int.TryParse(destinoStr, out destinoId)) continue;

            //                    var trecho = new Trecho
            //                    {
            //                        Id = id,
            //                        PosteOrigemId = origemId,
            //                        PosteDestinoId = destinoId,
            //                        IsMT = true, // Default
            //                        IsBT = false, // Default
            //                        Bitola = "S-10",
            //                        Fases = "ABC",
            //                        IsNovo = false
            //                    };

            //                    trechos.Add(trecho);
            //                    contador++;
            //                }
            //                catch
            //                {
            //                    // Ignora erro e continua
            //                    continue;
            //                }
            //            }
            //        }

            //        if (trechos.Count > 0)
            //        {
            //            MessageBox.Show($"Carregados: {trechos.Count} trechos",
            //                "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    // **NÃO MOSTRA ERRO - APENAS USA LISTA VAZIA**
            //    Debug.WriteLine($"Erro em trechos (silencioso): {ex.Message}");
            //    // Retorna lista vazia - não bloqueia o sistema
            //}

            //return trechos;
        }

        private void CarregarEquipamentos(OleDbConnection conexao, List<Poste> postes)
        {
            try
            {
                string sql = @"
                SELECT 
                    INT_ID,
                    PGF_ID,
                    TB_IN_ID,
                    INT_NUM,
                    TB_ELO_ID,
                    TB_CAP_ID
                FROM INSTALACAO
                WHERE PGF_ID IS NOT NULL 
                AND PGF_ID <> ''";

                var comando = new OleDbCommand(sql, conexao);
                using (var reader = comando.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // CORREÇÃO 8: Converter ID do poste
                        string posteIdStr = reader["PGF_ID"]?.ToString();
                        if (string.IsNullOrEmpty(posteIdStr)) continue;

                        if (!int.TryParse(posteIdStr, out int posteId))
                            continue;

                        // CORREÇÃO 9: Encontrar poste corretamente
                        var poste = postes.Find(p => p.Id == posteId);
                        if (poste == null) continue;

                        // CORREÇÃO 10: Converter ID do equipamento
                        string equipIdStr = reader["INT_ID"]?.ToString();
                        if (string.IsNullOrEmpty(equipIdStr)) continue;

                        if (!int.TryParse(equipIdStr, out int equipId))
                            continue;

                        var equipamento = new Equipamento
                        {
                            Id = equipId,
                            PosteId = posteId,
                            Tipo = reader["TB_IN_ID"]?.ToString() ?? "",
                            Numero = reader["INT_NUM"]?.ToString() ?? "",
                            EloFusivel = reader["TB_ELO_ID"]?.ToString() ?? "",
                            Capacidade = Convert.ToInt32(reader["TB_CAP_ID"] ?? 0)
                        };

                        poste.Equipamentos.Add(equipamento);
                    }
                }

                MessageBox.Show($"Equipamentos carregados para {postes.Count} postes.",
                    "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar equipamentos: {ex.Message}",
                    "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool TryParseCoordenada(string valor, out float resultado)
        {

            resultado = 0;

            if (string.IsNullOrEmpty(valor))
                return false;

            // Limpa a string
            valor = valor.Trim();

            // Remove "UTM", "X:", "Y:" se existir
            valor = valor.Replace("UTM", "").Replace("X:", "").Replace("Y:", "").Replace(" ", "");

            // Substitui vírgula por ponto
            valor = valor.Replace(',', '.');

            // Remove caracteres não numéricos (exceto ponto e sinal negativo)
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in valor)
            {
                if (char.IsDigit(c) || c == '.' || c == '-')
                    sb.Append(c);
            }

            valor = sb.ToString();

            if (string.IsNullOrEmpty(valor))
                return false;

            // Tenta parse
            return float.TryParse(valor, NumberStyles.Float, CultureInfo.InvariantCulture, out resultado);
        }

        private void ParseTB_CT_ID(string tbCtId, out string material, out int altura, out int esforco)
        {
            // CORREÇÃO 12: Valores padrão mais realistas
            material = "DESCONHECIDO";
            altura = 11; // Altura padrão mais comum
            esforco = 1000;

            if (string.IsNullOrEmpty(tbCtId) || tbCtId.Length < 2)
                return;

            // Primeiro caractere: material
            char primeiroChar = tbCtId[0];
            switch (primeiroChar)
            {
                case 'C': material = "CONCRETO"; break;
                case 'M': material = "MADEIRA"; break;
                case 'F': material = "FIBRA"; break;
                case 'A': material = "AÇO"; break;
                default: material = "DESCONHECIDO"; break;
            }

            // Altura (próximos 2 caracteres)
            if (tbCtId.Length >= 3)
            {
                string alturaStr = tbCtId.Substring(1, 2);
                if (!int.TryParse(alturaStr, out altura))
                    altura = 11;
            }

            // Esforço (restante)
            if (tbCtId.Length > 3)
            {
                string esforcoStr = tbCtId.Substring(3);
                if (!int.TryParse(esforcoStr, out esforco))
                    esforco = 1000;
            }
        }

        public bool TestarConexao()
        {
            try
            {
                using (var conexao = new OleDbConnection(connectionString))
                {
                    conexao.Open();
                    bool conectado = conexao.State == ConnectionState.Open;

                    if (conectado)
                    {
                        MessageBox.Show("Conexão com o banco Access estabelecida com sucesso!",
                            "Conexão OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    return conectado;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha na conexão: {ex.Message}",
                    "Erro de Conexão", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
