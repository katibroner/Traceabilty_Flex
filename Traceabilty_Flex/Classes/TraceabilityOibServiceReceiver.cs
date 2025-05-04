using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using TraceabilityTestGui;
using Traceabilty_Flex.Classes;
using www.siplace.com.OIB._2012._03.Traceability.Contracts.Data;
using www.siplace.com.OIB._2012._03.Traceability.Contracts.Service;

namespace Traceabilty_Flex
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class TraceabilityOibServiceReceiver : ITraceabilityDataDuplex
    {
        private readonly MainWindow _mainForm;
        private const string Endpoint = "http://smtoib:1405/Asm.As.Oib.WS.Eventing.Services/SubscriptionManager";
        public SQLdb sqlValidSide = new SQLdb(@"10.229.1.144\SMT", "Traceability", "aoi", "$Flex2016");

        public TraceabilityOibServiceReceiver(MainWindow form)
        {
            _mainForm = form;
            InitiallizeProxy();
        }

        private void InitiallizeProxy()
        {
            var binding = new NetTcpBinding
            {
                Security = { Mode = SecurityMode.None },
                CloseTimeout = TimeSpan.FromMinutes(10),
                OpenTimeout = TimeSpan.FromMinutes(10),
                ReceiveTimeout = TimeSpan.FromMinutes(10)
            };

            binding.ReliableSession.InactivityTimeout = binding.SendTimeout = TimeSpan.MaxValue;
            binding.ReliableSession.Enabled = true;
            binding.PortSharingEnabled = true;

            // Create the endpoint
        }

        public NewTraceabilityDataResponse NewTraceabilityData(TraceabilityDataRequest request)
        {
            var line = string.Empty;
            NewTraceabilityDataResponse response = null;
            try
            {
                response = new NewTraceabilityDataResponse();
                var trcData = request.TraceabilityData;
                var board = string.Empty;
                var boardSide = string.Empty;
                line = string.Empty;
                var station = string.Empty;
                var pallet = string.Empty;
                var setup = string.Empty;
                var recipe = string.Empty;
                var stationID = string.Empty;
                var panels = 0;
                var Lane = string.Empty;



                if (_mainForm != null)
                {
                    if (trcData != null)
                    {
                        if (trcData.Jobs != null)
                        {
                            line = MainWindow.GetLeaf(trcData.Line);
                            pallet = trcData.BoardID == null ? "" : trcData.BoardID;
                            board = MainWindow.GetLeaf(trcData.Jobs[0].BoardName);
                            boardSide = MainWindow.GetLeaf(trcData.Jobs[0].BoardSide);
                            setup = MainWindow.GetLeaf(trcData.Jobs[0].Setup);
                            recipe = MainWindow.GetLeaf(trcData.Jobs[0].Recipe);
                            stationID = trcData.MachineID;
                            panels = trcData.Panels != null ? trcData.Panels.Length : 0;
                            Lane = trcData.Lane;
                            if (line == "Line-R" || line == "Line-S" || line == "Line-Q")
                                line = GetLineSide(line, Lane);

                            ShowActivity(trcData, line, station, pallet, recipe);
                            if (!MainWindow.StatusDictionary[line])
                                return response;
                        }

                        station = trcData.Station != null ? MainWindow.GetLeaf(trcData.Station) : "";

                        if ((trcData.ErrorCodes != null) && (trcData.ErrorCodes.ErrorCodesList != null))
                        {
                            foreach (var error in trcData.ErrorCodes.ErrorCodesList)
                            {
                                if (error.ErrorReasons != null)
                                {
                                    foreach (var reason in error.ErrorReasons)
                                    {
                                        _mainForm.ErrorOut(" ErrorLevel  = " + error.ErrorLevel + " " + line + " Program: " + board + " Station: " + station + " Pallet: " + pallet + "  ErrorReason = " + reason.Reason + " Source = " + reason.Source);
                                    }
                                }
                            }
                        }
                    }
                }
                checkSideValidation(board, pallet, line);
                CheckSideValidationByPlacement(board, pallet, line, boardSide);
                if (pallet.StartsWith("NO_PCB_BARCODE"))//NO_PCB_BARCODE: 
                {
                    if (_mainForm != null && _mainForm.CheckStation(line, station))
                    {
                        _mainForm.ErrorOut(line + ", " + station + ", " + "NO_PCB_BARCODE");
                        _mainForm.EmergencyStopMethod(line + " " + station, null, null, recipe, "NO_PCB_BARCODE", true, "", "", "");

                    }
                    return response;
                }
                if (MainWindow._mainservice)
                {
                    if (recipe != board)
                        return response;//Only For dual line, temporery.

                    WriteTraceToDbLines(line, trcData, station);
                    TurnOnLightLine(line);

                    if (_mainForm != null)
                    {
                        _mainForm.RegisterPallet(line, pallet, station, board, setup, recipe, out var last, out var over);

                        if (last)
                        {
                            GetActiveLines();
                            var task = Task.Run(() => CompareResults(line, pallet, board, setup, true, recipe, false, _mainForm._delay, Lane, boardSide));
                        }
                        _mainForm.TrackPallet(line, station, pallet);
                    }
                }
                else
                {
                    ShowActivity(trcData, line, station, pallet, recipe);
                    if (_mainForm != null)
                    {
                        _mainForm.RegisterPalletA(line, pallet, station);
                        _mainForm.TrackPallet(line, station, pallet);
                    }
                }
            }
            catch (Exception dumpRequestException)
            {
                _mainForm?.ExceptionOut("Exception during printing information in NewTraceabilityData + Line " + line, dumpRequestException, false);
            }
            return response;
        }

        /// <summary>
        /// Выполняет валидацию платы на наличие второй стороны (ps или cs) в зависимости от порядка сборки.
        /// Определяет тип платы через GetAssemblyOrderInfo и вызывает аварийную остановку, если сторона не найдена.
        /// </summary>
        /// <param name="board">Имя платы с компонентами (например, SK-FAB8046A_A04).</param>
        /// <param name="pallet">Штрихкод паллеты (уникальный идентификатор платы в производстве).</param>
        /// <param name="line">Имя производственной линии, на которой идёт проверка.</param>
        private void checkSideValidation(string board, string pallet, string line)
        {
            // Получаем порядок сборки платы: CS->PS, PS->CS, CS, PS или null
            string assemblyOrder = GetAssemblyOrderInfo(board);

            // Если данные не получены или плата односторонняя — ничего не делаем
            if (string.IsNullOrEmpty(assemblyOrder) || assemblyOrder == "PS" || assemblyOrder == "CS")
                return;

            //  сначала собрали Component Side (CS), значит теперь нужно найти Print Side (PS)
            if (assemblyOrder == "CS->PS")
            {
                // Проверяем наличие стороны PS (например, в базе SIPLACE или Traceability)
                if (!HasValidPsSide(board, pallet))
                {
                    string message = $"Assembly ps side not found on board: {pallet} => {line}";
                    _mainForm.ErrorOut(message); // Показываем ошибку в интерфейсе
                    _mainForm.EmergencyStopMethod(line, null, null, " ", message, true, "", "", board); // Аварийная остановка
                }
            }
            // сначала собрали Print Side (PS), значит теперь нужно найти Component Side (CS)
            else if (assemblyOrder == "PS->CS")
            {
                // Проверяем наличие стороны CS
                if (!HasValidComponentSide(board, pallet))
                {
                    string message = $"Assembly cs side not found on board: {pallet} => {line}";
                    _mainForm.ErrorOut(message); // Показываем ошибку в интерфейсе
                    _mainForm.EmergencyStopMethod(line, null, null, " ", message, true, "", "", board); // Аварийная остановка
                }
            }
        }
        ///////////////
        /////////////////
        /// <summary>
        /// Получает направление сборки платы (ASSEMBLY_ORDER) по имени программы (board).
        /// Сначала извлекает артикул платы (PCB_PN) и её ревизию (PCB_Rev) из базы SMT_Monitor,
        /// затем ищет направление сборки в базе PROD.
        /// </summary>
        /// <param name="board">Имя программы платы (например, SK-FAB8046A_A04).</param>
        /// <returns>
        /// Возвращает одно из значений: "PS->CS", "CS->PS", "PS", "CS" или null, если ничего не найдено.
        /// </returns>
        private string GetAssemblyOrderInfo(string board)
        {
            try
            {
                // Шаг 1: Получаем PCB_PN и PCB_Rev из базы SMT_Monitor по имени программы board
                var sqlMonitor = new SqlClass("pcb_pn");

                // SQL-запрос на получение артикула платы и ревизии
                string queryPcbPn = $"SELECT PCB_PN, PCB_Rev FROM [RecipeCurrent] WHERE Program = '{board}'";
                var pcbPnResult = sqlMonitor.SelectDb(queryPcbPn, out string _);

                // Если плата не найдена — сообщаем об ошибке и возвращаем null
                if (pcbPnResult.Rows.Count == 0)
                {
                    MainWindow._mWindow.ErrorOut($"Не найден PCB_PN для Program = {board}");
                    return null;
                }

                // Извлекаем артикул и ревизию из результатов запроса
                string pcb_pn = pcbPnResult.Rows[0]["PCB_PN"].ToString();
                string pcb_rev = pcbPnResult.Rows[0]["PCB_Rev"].ToString();

                // Шаг 2: Локальная функция для получения списка направлений сборки по PN и REV
                List<string> GetAssemblyOrders(string pn, string rev)
                {
                    var resultList = new List<string>();
                    var sqlProd = new SqlClass("prod");

                    // SQL-запрос с параметрами для получения ASSEMBLY_ORDER
                    string query = @"
                SELECT ASSEMBLY_ORDER 
                FROM [BZ_SMT_INSTR_PCB_ALL_DATA] 
                WHERE PCB_PN = @pcbPn 
                  AND REVISION = @revision
                  AND ASSEMBLY_ORDER IS NOT NULL 
                  AND LTRIM(RTRIM(ASSEMBLY_ORDER)) <> ''";

                    using (var conn = new SqlConnection(sqlProd.ConnectionString))
                    using (var cmd = new SqlCommand(query, conn))
                    {
                        // Передаём параметры в запрос
                        cmd.Parameters.AddWithValue("@pcbPn", pn);
                        cmd.Parameters.AddWithValue("@revision", rev);

                        try
                        {
                            conn.Open();

                            // Считываем результаты и очищаем от пробелов
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string order = reader["ASSEMBLY_ORDER"].ToString().Trim().ToUpper();
                                    resultList.Add(order);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MainWindow._mWindow.ErrorOut("Ошибка при получении ASSEMBLY_ORDER: " + ex.Message);
                        }
                    }

                    return resultList;
                }

                // Основная попытка — использовать исходный PCB_PN
                var orders = GetAssemblyOrders(pcb_pn, pcb_rev);

                // Резервная попытка — если артикул начинается с "NP", пробуем "ML" вместо него
                if (orders.Count == 0 && pcb_pn.StartsWith("NP"))
                {
                    string altPcbPn = "ML" + pcb_pn.Substring(2);
                    orders = GetAssemblyOrders(altPcbPn, pcb_rev);

                    if (orders.Count > 0)
                    {
                        MainWindow._mWindow.ErrorOut($"Альтернативный PCB_PN: {altPcbPn} (вместо {pcb_pn})");
                    }
                }

                // Если не нашли ASSEMBLY_ORDER — возвращаем null
                if (orders.Count == 0)
                {
                    MainWindow._mWindow.ErrorOut($"ASSEMBLY_ORDER не найден для PCB_PN = {pcb_pn}, REV = {pcb_rev}");
                    return null;
                }

                // Приоритет: сначала ищем двухсторонние форматы, затем односторонние
                if (orders.Any(o => o.Replace(" ", "") == "PS->CS")) return "PS->CS";
                if (orders.Any(o => o.Replace(" ", "") == "CS->PS")) return "CS->PS";
                if (orders.Any(o => o.Replace(" ", "") == "PS")) return "PS";
                if (orders.Any(o => o.Replace(" ", "") == "CS")) return "CS";

                // Если формат неизвестен — логируем и возвращаем null
                MainWindow._mWindow.ErrorOut($"Неизвестный формат ASSEMBLY_ORDER: {string.Join(", ", orders)}");
                return null;
            }
            catch (Exception ex)
            {
                // В случае исключения — логируем и возвращаем null
                MainWindow._mWindow.ErrorOut("Ошибка в GetAssemblyOrderInfo: " + ex.Message);
                return null;
            }
        }        /// <summary>
                 /// Проверяет, есть ли у платы вторая сторона (ps), если первой была собрана сторона cs.
                 /// </summary>
                 /// <param name="board">Имя платы (с компонентами).</param>
                 /// <param name="pallet">Штрихкод поддона.</param>
                 /// <returns>true — если найдена сторона ps, иначе false.</returns>
        private static bool HasValidPsSide(string board, string pallet)
        {
            if (string.IsNullOrWhiteSpace(board) || !board.Contains("cs"))
                return true; // если не содержит cs — не двухсторонняя (или неформат)

            string boardBaseName = board.Substring(0, board.IndexOf("cs"));

            try
            {
                // 1. Поиск в базе SIPLACE Pro
                SiplacePro.openConnection();
                string query = "SELECT TOP 100 AliasName.ObjectName FROM dbo.CBoard " +
                                "INNER JOIN dbo.AliasName ON CBoard.OID = AliasName.PID " +
                                "WHERE AliasName.ObjectName LIKE @pattern";

                SiplacePro.cmd.CommandText = query;
                SiplacePro.cmd.Parameters.Clear();
                SiplacePro.cmd.Parameters.AddWithValue("@pattern", "%" + boardBaseName + "%");

                using (var reader = SiplacePro.cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader[0].ToString().Contains("ps"))
                            return true;
                    }
                }

                // 2. Проверка в SideValidation
                using (var conn = new SqlConnection(@"Data Source=migsqlclu4\smt;Initial Catalog=Traceability;Persist Security Info=True;User ID=aoi;Password=$Flex2016"))
                using (var cmd = new SqlCommand("SELECT 1 FROM [Traceability].[dbo].[SideValidation] WHERE Pallet = @pallet", conn))
                {
                    cmd.Parameters.AddWithValue("@pallet", pallet);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                        return true;
                }
            }
            catch (Exception ex)
            {
                MainWindow._mWindow.ErrorOut("Error in HasValidPsSide: " + ex.Message);
            }

            return false;
        }
        /// <summary>
        /// Проверяет, есть ли у платы сторона cs (component side),
        /// если сборка началась с ps (print side).
        /// </summary>
        /// <param name="board">Имя платы, начинающееся с ps.</param>
        /// <param name="pallet">Штрихкод паллеты.</param>
        /// <returns>true — если сторона cs найдена, иначе false.</returns>
        private static bool HasValidComponentSide(string board, string pallet)
        {
            if (string.IsNullOrWhiteSpace(board) || !board.Contains("ps"))
                return true;

            string boardBaseName = board.Substring(0, board.IndexOf("ps"));

            try
            {
                // 1. Поиск в SIPLACE Pro
                SiplacePro.openConnection();
                string query = "SELECT TOP 100 AliasName.ObjectName FROM dbo.CBoard " +
                                "INNER JOIN dbo.AliasName ON CBoard.OID = AliasName.PID " +
                                "WHERE AliasName.ObjectName LIKE @pattern";

                SiplacePro.cmd.CommandText = query;
                SiplacePro.cmd.Parameters.Clear();
                SiplacePro.cmd.Parameters.AddWithValue("@pattern", "%" + boardBaseName + "%");

                using (var reader = SiplacePro.cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader[0].ToString().Contains("cs"))
                            return true;
                    }
                }

                // 2. Проверка в SideValidation
                using (var conn = new SqlConnection(@"Data Source=migsqlclu4\smt;Initial Catalog=Traceability;Persist Security Info=True;User ID=aoi;Password=$Flex2016"))
                using (var cmd = new SqlCommand("SELECT 1 FROM [Traceability].[dbo].[SideValidation] WHERE Pallet = @pallet", conn))
                {
                    cmd.Parameters.AddWithValue("@pallet", pallet);
                    conn.Open();
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                        return true;
                }
            }
            catch (Exception ex)
            {
                MainWindow._mWindow.ErrorOut("Error in HasValidComponentSide: " + ex.Message);
            }

            return false;
        }
        /// <summary>
        /// Проверяет правильность сборки второй стороны платы (Top или Bottom),
        /// на основе направления сборки (PS->CS или CS->PS).
        /// </summary>
        private void CheckSideValidationByPlacement(string board, string pallet, string line, string boardSide)
        {
            // Получаем порядок сборки платы (например: "CS->PS", "PS->CS", "CS" или "PS")
            string assemblyOrder = GetAssemblyOrderInfo(board);

            // Если плата односторонняя или данных нет — не выполняем проверку
            if (string.IsNullOrEmpty(assemblyOrder) || assemblyOrder == "PS" || assemblyOrder == "CS")
                return;

            // Проверяем, действительно ли у платы есть две стороны (две сборки/размещения)
            if (CheckBoardSideByPlacement(board))
            {
                // === Случай 1: Сначала была собрана сторона PS (нижняя) ===
                // Значит сейчас должна быть TOP (то есть Component Side / CS)
                if (assemblyOrder == "PS->CS" &&
                    !(boardSide.IndexOf("Top", StringComparison.OrdinalIgnoreCase) >= 0)) // если текущая сторона не Top
                {
                    // Получаем записи по паллете и плате из базы
                    DataTable palletInTraceDB = GetPalletData(board, pallet);

                    // Если записей меньше двух — вторая сторона не собрана → ошибка
                    if (palletInTraceDB.Rows.Count < 2)
                    {
                        LogErrorAndStop(line, pallet, board, $"Сторона TOP (CS) не найдена на плате - {board}: {pallet} => {line}");
                    }
                }

                // === Случай 2: Сначала была собрана сторона CS (верхняя) ===
                // Значит сейчас должна быть BOTTOM (то есть Print Side / PS)
                else if (assemblyOrder == "CS->PS" &&
                    !(boardSide.IndexOf("Bottom", StringComparison.OrdinalIgnoreCase) >= 0)) // если текущая сторона не Bottom
                {
                    // Получаем записи из базы
                    DataTable palletInTraceDB = GetPalletData(board, pallet);

                    // Если записей меньше двух — вторая сторона не собрана → ошибка
                    if (palletInTraceDB.Rows.Count < 2)
                    {
                        LogErrorAndStop(line, pallet, board, $"Сторона BOTTOM (PS) не найдена на плате - {board}: {pallet} => {line}");
                    }
                }
            }
        }


        /// <summary>
        /// Проверяет, есть ли у платы две стороны на основе количества уникальных списков размещения (PlacementList).
        /// </summary>
        private static bool CheckBoardSideByPlacement(string boardName)
        {
            try
            {
                // Открываем подключение к базе SiplacePro
                SiplacePro.openConnection();

                // SQL-запрос: выбираем количество уникальных размещений (PlacementRef), привязанных к двум сторонам платы
                string sql = @"
        SELECT COUNT(*) FROM (
            SELECT DISTINCT 
                dbo.AliasName.ObjectName,  
                dbo.CBoardSide.bstrName, 
                COALESCE(dbo.CPanel.spPlacementListRef, dbo.CPlacementList.OID) AS PlacementListRef
            FROM dbo.CBoard 
            INNER JOIN dbo.AliasName ON dbo.CBoard.OID = dbo.AliasName.PID 
            INNER JOIN dbo.CBoardSide ON dbo.CBoard.OID = dbo.CBoardSide.PID 
            LEFT JOIN dbo.CPanelMatrix ON dbo.CBoardSide.OID = dbo.CPanelMatrix.PID 
            LEFT JOIN dbo.CPanel ON dbo.CPanelMatrix.OID = dbo.CPanel.PID 
            LEFT JOIN dbo.CPlacementList ON dbo.CBoardSide.spPlacementListRef = dbo.CPlacementList.OID
            WHERE dbo.AliasName.ObjectName = @BoardName
            AND COALESCE(dbo.CPanel.spPlacementListRef, dbo.CPlacementList.OID) IS NOT NULL
        ) AS SubQuery";

                // Создаём SQL-команду с параметром
                using (SqlCommand cmd = new SqlCommand(sql, SiplacePro.con))
                {
                    // Привязываем имя платы к параметру
                    cmd.Parameters.AddWithValue("@BoardName", boardName);

                    // Выполняем запрос и получаем число уникальных размещений
                    int count = (int)cmd.ExecuteScalar();

                    // Если два — значит плата двухсторонняя
                    return count == 2;
                }
            }
            catch (Exception)
            {
                // В случае ошибки возвращаем false
                return false;
            }
            finally
            {
                // Закрываем соединение в любом случае
                SiplacePro.closeConnection();
            }
        }


        /// <summary>
        /// Получает информацию о поддоне и плате (включая сторону BoardSide) из базы Trace.
        /// </summary>
        private static DataTable GetPalletData(string board, string pallet)
        {
            // Создаём результирующую таблицу
            DataTable resultTable = new DataTable();

            // SQL-запрос: ищем записи по штрихкоду поддона и части названия платы
            string query = @"
    SELECT DISTINCT TOP (100) PERCENT 
        dbo.PCBBarcode.Barcode AS PCBBarcode, 
        SUBSTRING(dbo.Recipe.Name, 15, LEN(dbo.Recipe.Name)) AS Recipe, 
        dbo.Job.BoardSide,
        dbo.Board.Name AS Board 
    FROM dbo.Board 
    INNER JOIN dbo.Setup 
    INNER JOIN dbo.Recipe 
    INNER JOIN dbo.Job 
    INNER JOIN dbo.TraceData 
    INNER JOIN dbo.TraceJob ON dbo.TraceData.Id = dbo.TraceJob.TraceDataId 
        ON dbo.Job.Id = dbo.TraceJob.JobId 
        ON dbo.Recipe.id = dbo.Job.RecipeId 
    INNER JOIN dbo.Station ON dbo.TraceData.StationId = dbo.Station.Id 
        ON dbo.Setup.id = dbo.Job.SetupId 
    INNER JOIN dbo.vOrder5 ON dbo.Job.OrderId = dbo.vOrder5.id 
        ON dbo.Board.id = dbo.Job.BoardId 
    FULL OUTER JOIN dbo.PCBBarcode ON dbo.TraceData.PCBBarcodeId = dbo.PCBBarcode.Id 
    WHERE dbo.PCBBarcode.Barcode = @pallet 
    AND dbo.Board.Name LIKE @board";

            // Подключение к базе данных ASMPTTraceabilityDb
            using (SqlConnection connection = new SqlConnection(ASMPTTraceabilityDb.GetConnectionString()))
            {
                connection.Open();

                // Создаём SQL-команду
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    // Подставляем параметры в запрос
                    command.Parameters.AddWithValue("@pallet", pallet);
                    command.Parameters.AddWithValue("@board", "%" + board + "%");

                    // Загружаем результат в таблицу
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(resultTable);
                    }
                }
            }

            // Возвращаем таблицу
            return resultTable;
        }



        /// <summary>
        /// Записывает сообщение об ошибке в лог и вызывает аварийную остановку линии.
        /// </summary>
        private void LogErrorAndStop(string line, string pallet, string board, string errorMessage)
        {
            // Вывод ошибки на экран (или лог)
            _mainForm.ErrorOut(errorMessage);

            // Вызываем остановку линии
            _mainForm.EmergencyStopMethod(
                line,       // линия
                null,       // список деталей (не используется здесь)
                null,       // список пропущенных компонентов
                " ",        // рецепт
                errorMessage, // сообщение ошибки
                true,       // активен ли EmergencyStop
                "", "", board); // дополнительные параметры и имя платы
        }
        private string GetLineSide(string line, string Convayer)
        {
            if (line.Contains("Line-R"))// Line R Convayer
            {
                if (Convayer == "Right")
                    return "Line-R1";
                else if (Convayer == "Left")
                    return "Line-R2";
            }
            if (line.Contains("Line-S"))// Line S Convayer
            {
                if (Convayer == "Right")
                    return "Line-S1";
                else if (Convayer == "Left")
                    return "Line-S2";
            }
            if (line.Contains("Line-Q"))// Line Q Convayer
            {
                if (Convayer == "Right")
                    return "Line-Q1";
                else if (Convayer == "Left")
                    return "Line-Q2";
            }
            return "";
        }

        private void GetActiveLines()
        {
            var sql = new SqlClass("trace");
            var query = @"SELECT * FROM Lines";

            _mainForm.DTActiveLines = sql.SelectDb(query, out var result);
            if (result != null)
                _mainForm.ErrorOut(result);
        }

        private void ShowActivity(TraceabilityData trcData, string line, string station, string pallet, string board)
        {

            var time = DateTime.Now.ToString("HH:mm:ss");
            _mainForm.MessageOut(time + "\tProgram:  " + board + "\tStation:  " + station + "\tPallet:  " + pallet);

            if (board == MainWindow.RecipeDictionary[line]) return;
            _mainForm.FillRecipeDt();

            var d = GetRecipe(board, line);
            _mainForm.SetFirstLastInLine(d, line);
        }

        private void TurnOnLightLine(string line)
        {
            var index = MainWindow.LineCollection.FindIndex(a => a.Name == line);
            MainWindow._buttons[index].Background = Brushes.Green;
        }

        private void ClearTraceLine(string line, string pallet)
        {
            var sql = new SqlClass("trace");
            var query = string.Format("DELETE FROM TraceList WHERE line='{0}' and pallet = '{1}'", line, pallet);

            sql.Update(query);
        }

        private void CompareResults(string line, string pallet, string board, string setup, bool b, string recipe, bool StartdelayFlag, int delay, string Lane, string boardSide)
        {
            var st = new Stopwatch();
            st.Start();

            if (line == "Line-C" || line == "Line-N" || line == "Line-G")
                line = line;
            var d1 = GetDtFromDbRecipe(line);
            var d2 = GetDtFromDbTrace(line, pallet);

            if (d1.Rows.Count == 0)
            {
                _mainForm.FillOneRecipe(new string[] { setup, recipe, line });
                d1 = GetDtFromDbRecipe(line);
                if (d1.Rows.Count == 0)
                {
                    _mainForm.ErrorOut(line + " Recipe is empty.");
                    return;
                }
            }
            if (d2.Rows.Count == 0)
            {
                _mainForm.ErrorOut(line + " Trace is empty.");
                return;
            }
            DataTable d = null;
            var thisLock = new Object();
            lock (thisLock)
            {
                d = GetDifferentRecords(d1, d2);
            }
            var s = "";
            DataTable dRet = null;
            DataTable dDiff = null;

            var diff = d.Rows.Count;
            var last_ch = false;

            if (diff > 0)
            {
                List<DataRow> toDelete = new List<DataRow>();

                foreach (DataRow item in d.Rows)
                {
                    if (Array.IndexOf(MainWindow.PartsException, item[0].ToString().Trim()) != -1)
                    {
                        toDelete.Add(item);
                    }
                }

                foreach (DataRow dr in toDelete)
                {
                    d.Rows.Remove(dr);
                }

                if (d.Rows.Count == 0) return;


                if (!LastChance(pallet, recipe, board, line, d1, StartdelayFlag, out dRet, out dDiff))
                {
                    _mainForm.FillOneRecipe(new string[] { setup, recipe, line }); // this is for restarting the program
                    d1 = GetDtFromDbRecipe(line);

                    if (!StartdelayFlag)
                    {
                        var ti = DateTime.Now.ToString("HH:mm:ss");

                        StartDelay(line, pallet, board, setup, b, recipe, delay, Lane, boardSide);

                        _mainForm.MessageOut(ti
                    + "   " + line + "\tDiff:\t0"
                    + "\t" + pallet
                    + "\tRecipe:\t" + d1.Rows.Count.ToString()
                    + "\tTrace:\t" + d2.Rows.Count.ToString()
                    + "\tTime:\t" + st.ElapsedMilliseconds + "\t"
                    + "Delayed");
                        _mainForm.ErrorOut("Pallet " + pallet + " delayed, " + "Line: " + line);
                        return;
                    }

                    //var task2 = Task.Run(() => PrintToFile(d, pallet, line, d1, dRet));

                    var lt = new List<string>();
                    var ms = "Pallet: " + pallet;

                    foreach (DataRow dr in d.Rows)
                        ms = ms + Environment.NewLine + dr[0].ToString().Trim() + "\t" + dr[1].ToString().Trim() + "\t" + dr[2].ToString().Trim() + "\tLocation: " + dr[3].ToString().Trim() + "\tFeeder: " + dr[4].ToString().Trim() + "\tTrack: " + dr[5].ToString().Trim();

                    lt.Add(ms);
                    s = GetMissedStations(d);
                    var MissedList = GetMissedArray(line, pallet, d);
                    var check2 = (bool)_mainForm.DTActiveLines.Select("Line = '" + line + "'")[0]["Active"];
                    _mainForm.EmergencyStopMethod(line, lt, MissedList, recipe, "Missed components at stations:" + Environment.NewLine + s, check2, Lane, boardSide, board);
                }
                else
                {
                    d2 = dRet;
                    diff = 0;
                    last_ch = true;
                }
            }

            st.Stop();

            var time = DateTime.Now.ToString("HH:mm:ss");
            s = last_ch ? "Rechecked" : (s == "" ? "" : ("  Missed stations: " + s));

            _mainForm.MessageOut(time + "   " + line + "\tDiff: " + diff.ToString()
                + "\t" + pallet
                + "\tRecipe:\t" + d1.Rows.Count.ToString()
                + "\tTrace:\t" + d2.Rows.Count.ToString()
                + "\tTime:\t" + st.ElapsedMilliseconds + "\t"
                + s);

            ClearTraceLine(line, pallet);
            _mainForm.CheckIfDBEmpty(line);
        }


        private void StartDelay(string line, string pallet, string board, string setup, bool b, string recipe, int delay, string Lane, string boardSide)
        {
            try
            {
                var barInvoker = new BackgroundWorker();
                barInvoker.DoWork += delegate
                {
                    //Thread.Sleep(TimeSpan.FromSeconds(delay));
                    Thread.Sleep(TimeSpan.FromSeconds(480));

                    var task = Task.Run(() => CompareResults(line, pallet, board, setup, b, recipe, true, delay, Lane, boardSide));
                };
                barInvoker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                _mainForm.ErrorOut(ex.Message);
            }
        }

        private bool LastChance(string pallet, string recipe, string board, string line, DataTable d1, bool StartdelayFlag, out DataTable d2, out DataTable dDiff)// use for switch to new  trace_db in SQLCLass.cs
        {
            if (line == "Line-A" || line == "Line-E" || line == "Line-L" || line == "Line-N" || line == "Line-O" || line == "Line-C" || line == "Line-B" || line == "Line-K" || line == "Line-I" || line == "Line-M" || line == "Line-H" || line == "Line-P" || line == "Line-S" || line == "Line-F" || line == "Line-D" || line == "Line-G" || line == "Line-R" || line == "Line-Q")
            {
                var sql = new SqlClass("setup_trace_new");

                var query = string.Format(@"SELECT TOP (100) PERCENT dbo.PackagingUnit.ComponentBarcode AS PN, dbo.RefDesignator.Name AS RefDes, dbo.PackagingUnit.PackagingUniqueId AS PUID, 
		                   '0' as Stam1,'0' as Stam2, '0' as Stam3
                            FROM dbo.Placement INNER JOIN
		                    dbo.TracePlacement ON dbo.Placement.PlacementGroupId = dbo.TracePlacement.PlacementGroupId FULL OUTER JOIN
		                    dbo.Recipe INNER JOIN
		                    dbo.Job INNER JOIN
		                    dbo.TraceData INNER JOIN
		                    dbo.TraceJob ON dbo.TraceData.Id = dbo.TraceJob.TraceDataId ON dbo.Job.Id = dbo.TraceJob.JobId ON dbo.Recipe.id = dbo.Job.RecipeId FULL OUTER JOIN
		                    dbo.PCBBarcode ON dbo.TraceData.PCBBarcodeId = dbo.PCBBarcode.Id ON dbo.TracePlacement.TraceDataId = dbo.TraceData.Id FULL OUTER JOIN
		                    dbo.RefDesignator ON dbo.Placement.RefDesignatorId = dbo.RefDesignator.Id FULL OUTER JOIN
		                    dbo.Charge ON dbo.Placement.ChargeId = dbo.Charge.Id FULL OUTER JOIN
		                    dbo.PackagingUnit ON dbo.Charge.PackagingUnitId = dbo.PackagingUnit.Id
                            WHERE (dbo.PCBBarcode.Barcode = N'{0}') and (dbo.Recipe.Name like N'%{1}')", pallet, recipe);

                d2 = sql.SelectDb(query, out var Result);
                if (Result != null)
                    _mainForm.ErrorOut(Result);

                dDiff = null;

                if (d2.Rows.Count == 0 || d1.Rows.Count == 0)
                    return false;

                var thisLock = new Object();
                lock (thisLock)
                {
                    dDiff = GetDifferentRecords(d1, d2);
                }
                if (dDiff.Rows.Count > 0)
                {
                    var ms = Environment.NewLine + "Pallet: " + pallet + ", recipe: " + recipe;

                    foreach (DataRow dr in dDiff.Rows)
                        ms = ms + Environment.NewLine + dr[0].ToString().Trim() + "\t" + dr[1].ToString().Trim() + "\t" + dr[2].ToString().Trim() + "\tLocation: " + dr[3].ToString().Trim() + "\tFeeder: " + dr[4].ToString().Trim() + "\tTrack: " + dr[5].ToString().Trim();

                    if (StartdelayFlag)
                        LogWriter.WriteLogTest("LastChance Not OK, " + ms, "c:\\tmp\\Traceability\\Traceability_LastChance.txt");
                    return false;
                }
                else
                {
                    if (StartdelayFlag)
                        LogWriter.WriteLogTest("LastChance OK, Pallet: " + pallet + ", Line: " + line + ", recipe: " + recipe, "c:\\tmp\\Traceability\\Traceability_LastChance.txt");
                    return true;
                }
            }
            else
            {
                var sql = new SqlClass("setup_trace");

                var query = string.Format(@"SELECT TOP (100) PERCENT dbo.PackagingUnit.ComponentBarcode AS PN, dbo.RefDesignator.Name AS RefDes, dbo.PackagingUnit.PackagingUniqueId AS PUID, 
		                   '0' as Stam1,'0' as Stam2, '0' as Stam3
                            FROM dbo.Placement INNER JOIN
		                    dbo.TracePlacement ON dbo.Placement.PlacementGroupId = dbo.TracePlacement.PlacementGroupId FULL OUTER JOIN
		                    dbo.Recipe INNER JOIN
		                    dbo.Job INNER JOIN
		                    dbo.TraceData INNER JOIN
		                    dbo.TraceJob ON dbo.TraceData.Id = dbo.TraceJob.TraceDataId ON dbo.Job.Id = dbo.TraceJob.JobId ON dbo.Recipe.id = dbo.Job.RecipeId FULL OUTER JOIN
		                    dbo.PCBBarcode ON dbo.TraceData.PCBBarcodeId = dbo.PCBBarcode.Id ON dbo.TracePlacement.TraceDataId = dbo.TraceData.Id FULL OUTER JOIN
		                    dbo.RefDesignator ON dbo.Placement.RefDesignatorId = dbo.RefDesignator.Id FULL OUTER JOIN
		                    dbo.Charge ON dbo.Placement.ChargeId = dbo.Charge.Id FULL OUTER JOIN
		                    dbo.PackagingUnit ON dbo.Charge.PackagingUnitId = dbo.PackagingUnit.Id
                            WHERE (dbo.PCBBarcode.Barcode = N'{0}') and (dbo.Recipe.Name like N'%{1}')", pallet, recipe);

                d2 = sql.SelectDb(query, out var Result);
                if (Result != null)
                    _mainForm.ErrorOut(Result);

                dDiff = null;

                if (d2.Rows.Count == 0 || d1.Rows.Count == 0)
                    return false;

                var thisLock = new Object();
                lock (thisLock)
                {
                    dDiff = GetDifferentRecords(d1, d2);
                }
                if (dDiff.Rows.Count > 0)
                {
                    var ms = Environment.NewLine + "Pallet: " + pallet + ", recipe: " + recipe;

                    foreach (DataRow dr in dDiff.Rows)
                        ms = ms + Environment.NewLine + dr[0].ToString().Trim() + "\t" + dr[1].ToString().Trim() + "\t" + dr[2].ToString().Trim() + "\tLocation: " + dr[3].ToString().Trim() + "\tFeeder: " + dr[4].ToString().Trim() + "\tTrack: " + dr[5].ToString().Trim();

                    if (StartdelayFlag)
                        LogWriter.WriteLogTest("LastChance Not OK, " + ms, "c:\\tmp\\Traceability\\Traceability_LastChance.txt");
                    return false;
                }
                else
                {
                    if (StartdelayFlag)
                        LogWriter.WriteLogTest("LastChance OK, Pallet: " + pallet + ", Line: " + line + ", recipe: " + recipe, "c:\\tmp\\Traceability\\Traceability_LastChance.txt");
                    return true;
                }
            }

        }

        private List<string[]> GetMissedArray(string line, string pallet, DataTable d)
        {
            try
            {
                var list = new List<string[]>();
                var result = from r in d.AsEnumerable()
                             group r by new { placeCol = r[0], station = r[2] } into groupby
                             select new
                             {
                                 Value = groupby.Key,
                                 ColumnValues = groupby
                             };

                foreach (var item in result)
                {
                    var comp = item.Value.placeCol.ToString().Trim().Replace(" ", "");
                    var station = item.Value.station.ToString().Trim().Replace(" ", "");
                    var str = new string[] { pallet, "", comp, station, "", "", "", "", "" };
                    var ms = "Pallet: " + pallet + "; " + "PN: " + comp;
                    list.Add(str);
                }
                return list;
            }
            catch (Exception ex)
            {
                LogWriter.WriteLog(ex.Message);
                return null;
            }
        }

        private static void PrintToFile(DataTable d, string pallet, string line, DataTable d1, DataTable d2)
        {
            const string dir = @"C:\Tmp\Traceability\Logs\";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var fil = Path.Combine(dir, pallet.Replace("/", "_")) + "(" + line + ")" + ".txt";
            using (var sw = new StreamWriter(fil))
            {
                foreach (DataRow dr in d.Rows)
                {
                    sw.WriteLine(dr[0].ToString().Trim() + "\t" + dr[1].ToString().Trim() + "\t" + dr[2].ToString().Trim());
                }
            }
        }

        private string GetMissedStations(DataTable d)
        {
            var s = "";
            try
            {
                var groupedData = from b in d.AsEnumerable()
                                  group b by b.Field<string>("Station") into g
                                  select new
                                  {
                                      station = g.Key,
                                      List = g.ToList(),
                                  };

                s = groupedData.Aggregate(s, (current, a) => current + a.station.Trim() + " ");
            }
            catch (Exception ex) { _mainForm.ErrorOut(ex.Message); }
            return s;
        }

        private DataTable GetDifferentRecords(DataTable FirstDataTable, DataTable SecondDataTable)
        {
            //Create Empty Table  
            var ResultDataTable = new DataTable("ResultDataTable");

            //use a Dataset to make use of a DataRelation object  
            using (var ds = new DataSet())
            {
                //Add tables  
                ds.Tables.AddRange(new DataTable[] { FirstDataTable.Copy(), SecondDataTable.Copy() });

                //Get Columns for DataRelation  
                var firstColumns = new DataColumn[2];
                firstColumns[0] = ds.Tables[0].Columns[0];
                firstColumns[1] = ds.Tables[0].Columns[1];

                var secondColumns = new DataColumn[2];
                secondColumns[0] = ds.Tables[1].Columns[0];
                secondColumns[1] = ds.Tables[1].Columns[1];

                //Create DataRelation  
                var r1 = new DataRelation(string.Empty, firstColumns, secondColumns, false);
                ds.Relations.Add(r1);

                var r2 = new DataRelation(string.Empty, secondColumns, firstColumns, false);
                ds.Relations.Add(r2);

                //Create columns for return table  
                for (var i = 0; i < SecondDataTable.Columns.Count; i++)
                {
                    ResultDataTable.Columns.Add(SecondDataTable.Columns[i].ColumnName, SecondDataTable.Columns[i].DataType);
                }

                //If FirstDataTable Row not in SecondDataTable, Add to ResultDataTable.  
                ResultDataTable.BeginLoadData();
                foreach (DataRow parentrow in ds.Tables[0].Rows)
                {
                    var childrows = parentrow.GetChildRows(r1);
                    if (childrows == null || childrows.Length == 0)
                        ResultDataTable.LoadDataRow(parentrow.ItemArray, true);
                }

                //If SecondDataTable Row not in FirstDataTable, Add to ResultDataTable.  
                foreach (DataRow parentrow in ds.Tables[1].Rows)
                {
                    var childrows = parentrow.GetChildRows(r2);
                    if (childrows == null || childrows.Length == 0)
                        ResultDataTable.LoadDataRow(parentrow.ItemArray, true);
                }
                ResultDataTable.EndLoadData();
            }
            return ResultDataTable;
        }

        private DataTable GetDtFromDbTrace(string line, string pallet)
        {
            var query = string.Format("SELECT [pn], [rf], [station], [loc], [track], [div], [unitID] FROM TraceList  WHERE line='{0}' and pallet = '{1}'", line, pallet);

            var sql = new SqlClass("trace");
            var d = sql.SelectDb(query, out var result);

            if (result != null)
                _mainForm.ErrorOut(result);
            return d;
        }

        private DataTable GetDtFromDbRecipe(string line)
        {
            var query = string.Format("SELECT [pn], [rf], [station], [loc], [track], [div] FROM RecipeList WHERE line='{0}'", line);
            var sql = new SqlClass("trace");
            var d = sql.SelectDb(query, out var result);

            if (result != null)
                _mainForm.ErrorOut(result);
            return d;
        }

        private void WriteTraceToDbLines(string line, TraceabilityData trcData, string station)
        {
            if (trcData == null || trcData.BoardID == null) return;

            var pallet = trcData.BoardID.Trim().Length > 10 ? trcData.BoardID.Trim() : "";
            var recipe = trcData.Jobs[0].BoardName;

            var dicComp = FillCompDictionary(trcData, out var wrongList, line, station, out var specialstop);
            var sql = new SqlClass("trace");

            if (trcData.Panels != null)
            {
                foreach (var p in trcData.Panels)
                {
                    if (p.Packagings == null) continue;
                    foreach (var c in p.Packagings)
                    {
                        if (c.ReferenceDesignators == null) continue;

                        var pp = c.ReferenceDesignators;
                        var cm = dicComp[c.PackagingRefID];
                        foreach (var t in pp)
                        {
                            var rf = t.Name;
                            var query = string.Format(@"INSERT INTO TraceList ([line],[station],[pn],[rf],[loc],[track],[div],[tower],[lvl],[pallet],[unitID],[batch]) 
                                                      VALUES('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}')",
                                                      line, station, cm.Pn, rf, cm.Location.ToString(), cm.Track, cm.Division.ToString(), cm.Tower.ToString(), cm.Level.ToString(), pallet, cm.UnitId.Trim(), cm.Batch.Trim());
                            sql.Insert(query);
                        }
                    }
                }
            }

            if (wrongList != null && wrongList.Count > 0)
                CallEmergencyStop(wrongList, station, line, pallet, recipe);
        }

        private void CallEmergencyStop(List<Comp2> wrongList, string station, string line, string pallet, string recipe)
        {
            var list = new List<string[]>();
            var lt = new List<string>();

            foreach (var item in wrongList)
            {
                var str = new string[] {
                    pallet,
                    item.Batch,
                    item.Pn,
                    station,
                    item.Location.ToString(),
                    item.Division.ToString(),
                    item.Tower.ToString(),
                    item.Level.ToString(),
                    item.Track.ToString()
                };
                list.Add(str);

                var ms = line + "; Station: " + station + "; Pallet: " + pallet + "; PN: " + item.Pn + "; UnitID: " + item.UnitId + "; Batch: " + item.Batch;
                lt.Add(ms);
            }

            var check2 = (bool)_mainForm.DTActiveLines.Select("Line = '" + line + "'")[0]["Active"];

            new LogWriter("adam has stoped in line:" + line + "line Activation" + check2.ToString(), "error");
            _mainForm.EmergencyStopMethod(line, lt, list, recipe, "Part does not have Unique ID", true, "", "", "");
        }

        private Dictionary<string, Comp2> FillCompDictionary(TraceabilityData trcData, out List<Comp2> wrongList, string line, string station, out bool specialflag)
        {
            var dic = new Dictionary<string, Comp2>();
            wrongList = new List<Comp2>();
            specialflag = true;

            if (trcData.Locations != null)
            {
                foreach (var location in trcData.Locations)
                {
                    var loc = location.Loc;

                    if (location.Positions != null)
                    {
                        foreach (var position in location.Positions)
                        {
                            for (var i = 0; i < position.PackagingUnits.Length; i++)
                            {
                                var track = position.Track;
                                var div = position.Div;
                                var tower = position.Tower;
                                var level = position.Level;
                                var pn = position.PackagingUnits[i].ComponentBarcode;
                                var key = position.PackagingUnits[i].Id;
                                var pID = position.PackagingUnits[i].PackagingId;
                                var pBatch = position.PackagingUnits[i].BatchId;
                                var c = new Comp2(pn, loc, track, div, tower, level, i, pID, string.IsNullOrEmpty(pBatch) ? "" : pBatch);
                                dic.Add(key, c);
                                var is_skid = true;
                                new LogWriter(line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "");
                                if (is_skid)
                                {
                                    if (tower > 0)
                                    {
                                        if (pID.Length != 10)
                                        {
                                            if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                            {
                                                new LogWriter("**********ERROR (IS_SKID is TRUE tower > 0!)************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
                                                wrongList.Add(c);
                                            }
                                        }

                                        //if (pBatch == null)
                                        //{
                                        //    if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                        //    {
                                        //        new LogWriter("**********ERROR (IS_SKID is TRUE tower > 0!)************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
                                        //        wrongList.Add(c);
                                        //    }
                                        //}

                                        //else if (!Regex.IsMatch(pBatch, MainWindow._patBatch) && !pBatch.Contains("_"))
                                        //{
                                        //    MainWindow._mWindow.ErrorOut("error:  ( SKID IS TRUE!)" + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level);
                                        //    new LogWriter("**********ERROR (IS_SKID is TRUE tower > 0!)************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
                                        //    wrongList.Add(c);
                                        //}
                                    }
                                    else
                                    {
                                        if (!Regex.IsMatch(pID, MainWindow._patBatch))
                                        {
                                            if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                            {
                                                new LogWriter("**********ERROR (IS_SKID is TRUE tower <= 0!)************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
                                                MainWindow._mWindow.ErrorOut("error:  ( SKID IS TRUE!)" + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level);
                                                wrongList.Add(c);
                                            }
                                        }
                                    }
                                }
                                else if (!is_skid)
                                    if (tower > 0)
                                    {
                                        if (pBatch == null || (!Regex.IsMatch(pBatch, MainWindow._patUnitID) && !Regex.IsMatch(pBatch, MainWindow._patBatch)))
                                        {
                                            if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                            {
                                                new LogWriter("**********ERROR (IS_SKID is FALSE!) tower > 0 !************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
                                                MainWindow._mWindow.ErrorOut("error:  ( SKID IS FALSE!)" + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level);
                                                wrongList.Add(c);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (!Regex.IsMatch(pID, MainWindow._patUnitID))
                                        {
                                            if (Array.IndexOf(MainWindow.PartsException, pn) == -1)
                                            {
                                                new LogWriter("**********ERROR (IS_SKID is FALSE!) tower <= 0************* : " + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level, "error");
                                                MainWindow._mWindow.ErrorOut("error:  ( SKID IS FALSE!)" + line + ": \n " + "packaging id - " + pID + " pBatch -  " + pBatch + "\n station -: " + station + "\n location - " + loc + " track - " + track + " div - " + div + " tower - " + tower + " level - " + level);
                                                wrongList.Add(c);
                                            }
                                        }
                                    }
                            } // end for
                        }
                    }
                }
            }
            return dic;
        }

        private DataTable GetRecipe(string board, string line)
        {
            line = line.Contains("S") ? "Line-S" : line.Contains("R") ? "Line-R" : line.Contains("Q") ? "Line-Q" : line;
            var query = string.Format(@"SELECT TOP (100) PERCENT AliasName_3.ObjectName AS Setup, dbo.CFolder.bstrDisplayName AS Line, dbo.AliasName.ObjectName AS RecipeName, 
					  dbo.CComponentPlacement.bstrRefDesignator AS RefDes, AliasName_2.ObjectName AS PN, AliasName_1.ObjectName AS Station, dbo.CHeadSchedule.lHeadIndex AS Location, 
					  dbo.CPickupLink.lTrack AS Track, dbo.CPickupLink.lReserve AS Division, dbo.CPickupLink.lTower AS Tower, dbo.CPickupLink.lLevel AS [Level]
                      FROM dbo.CFolder INNER JOIN
					  dbo.CRecipe INNER JOIN
					  dbo.AliasName ON dbo.CRecipe.OID = dbo.AliasName.PID INNER JOIN
					  dbo.CHeadSchedule ON dbo.CRecipe.OID = dbo.CHeadSchedule.PID INNER JOIN
					  dbo.AliasName AS AliasName_1 ON dbo.CHeadSchedule.spStation = AliasName_1.PID INNER JOIN
					  dbo.CHeadStep ON dbo.CHeadSchedule.OID = dbo.CHeadStep.PID INNER JOIN
					  dbo.CPickupLink ON dbo.CRecipe.OID = dbo.CPickupLink.PID AND dbo.CHeadStep.lPickupLink = dbo.CPickupLink.lIndex INNER JOIN
					  dbo.AliasName AS AliasName_2 ON dbo.CPickupLink.spComponentRef = AliasName_2.PID INNER JOIN
					  dbo.CPlacementLink ON dbo.CRecipe.OID = dbo.CPlacementLink.PID AND dbo.CHeadStep.lPlacementLink = dbo.CPlacementLink.lIndex INNER JOIN
					  dbo.CComponentPlacement ON dbo.CPlacementLink.spComponentPlacement = dbo.CComponentPlacement.OID ON dbo.CFolder.OID = dbo.AliasName.FolderID INNER JOIN
					  dbo.AliasName AS AliasName_3 ON dbo.CRecipe.spSetupRef = AliasName_3.PID
                      WHERE (dbo.AliasName.ObjectName = N'{0}') AND (dbo.CFolder.bstrDisplayName = N'{1}')
                      ORDER BY PN", board, line);

            var sql = new SqlClass();
            var d = sql.SelectDb(query, out var result);
            if (result != null)
                _mainForm.ErrorOut(result);
            return d;
        }
    }
}