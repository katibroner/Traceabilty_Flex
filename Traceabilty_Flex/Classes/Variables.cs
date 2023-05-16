using Asm.As.Oib.DisplayService.Contracts.Data;
using Asm.As.Oib.DisplayService.Proxy.Architecture.Objects;
using Asm.As.Oib.Monitoring.Proxy.Architecture.Objects;
using Asm.As.Oib.SiplacePro.Contracts.Data.Business.Objects;
using Asm.As.Oib.WS.Eventing.Contracts.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Traceabilty_Flex
{
    public partial class MainWindow : Window
    {
        public static MainWindow _mWindow;

        private string MonitorCallbackEndpoint = string.Format("http://{0}:{1}/TraceabilityMonitoringGUI", Environment.MachineName, "33333");
        private string SubscriptionMonitorManagerEndpoint = "http://smtoib:1405/Asm.As.Oib.WS.Eventing.Services/SubscriptionManager";
        private Subscriber _subscriber;
        private LocationPcbCycle test = new LocationPcbCycle();
        private ReliableReceiver _receiver;
        private Subscription _currentSubscription;
        private readonly System.Timers.Timer _timer = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds);
        private const string DisplayHostName = "smtoib";
        private const string DisplayClientName = "DisplayClient";
        private const int DisplayPort = 33335;
        private DisplayServiceClient _client;
        private string _dir = @"C:\Tmp\Traceability\Events";
        public int _delay = 60;
        public bool _checkLine = true;
        public DispatcherTimer Shrink_DB_Log = new DispatcherTimer();//refresh every 1 hour.

        internal const string _patUnitID = @"\d{10}\/\d{4}"; //1234567891/1234
        internal const string _patBatch = @"^\d{10}\z";//1234567891


        private const string progName = "Traceability Line Control";
        private string prefix = "System\\{0}\\{1}";

        public string baseAddressTraceability = string.Format("http://{0}:{1}/TraceabilityServiceGUI", Environment.MachineName, "33333");

        private const string TraceabilityNotifyTopic = "SIPLACE:OIB:Traceability:Notify";
        private readonly string TimeFormat = "yyyy-MM-dd HH:mm:ss";

        private static ServiceHost TraceabilityNotifyReceiver;
        private int m_MessageCount;
        private const int m_MaxMessageCount = 200;

        internal DataTable DTActiveLines;

        TextBlock[] FlashTextBlocs;
        ComboBox[] ComboList;
        TextBlock[] TextBlockList;
        TextBlock[] TextBlockStations;
        string[] TrustedHosts;

        internal static string[] PartsException;
        internal static string[] CustomerList;
        internal static string[] RecipeExceptionList;

        internal static Button[] _buttons = null;
        internal static List<FlexLine> LineCollection = new List<FlexLine>();

        BackgroundWorker recipeWorker = new BackgroundWorker();
        BackgroundWorker monitorWorker = new BackgroundWorker();
        BackgroundWorker displayWorker = new BackgroundWorker();

        internal static string User;
        internal static string Password;
        internal static int Level = 1;
        internal static bool _mainservice = true;
        internal bool _ready = false;
        internal bool _placement = false;
        internal bool _trusted = false;
        internal bool _started = false;
        internal bool _filled = false;

        internal static DataTable DTRecipes = new DataTable();

        internal static Dictionary<string, bool> StatusDictionary = new Dictionary<string, bool>()
        {
            {"Line-A", false },
            {"Line-B", false },
            {"Line-C", false },
            {"Line-D", false },
            {"Line-E", false },
            {"Line-F", false },
            {"Line-G", false },
            {"Line-H", false },
            {"Line-I", false },
            {"Line-J", false },
            {"Line-K", false },
            {"Line-L", false },
            {"Line-M", false },
            {"Line-N", false },
            {"Line-O", false },
            {"Line-P", false },
            {"Line-Q1", false },
            {"Line-Q2", false },
            {"Line-R1", false },
            {"Line-R2", false },
            {"Line-S1", false },
            {"Line-S2", false }
        };

        internal static Dictionary<string, string> RecipeDictionary = new Dictionary<string, string>()
        {
            {"Line-A", "" },
            {"Line-B", "" },
            {"Line-C", "" },
            {"Line-D", "" },
            {"Line-E", "" },
            {"Line-F", "" },
            {"Line-G", "" },
            {"Line-H", "" },
            {"Line-I", "" },
            {"Line-J", "" },
            {"Line-K", "" },
            {"Line-L", "" },
            {"Line-M", "" },
            {"Line-N", "" },
            {"Line-O", "" },
            {"Line-P", "" },
            {"Line-Q1", "" },
            {"Line-Q2", "" },
            {"Line-R1", "" },
            {"Line-R2", "" },
            {"Line-S1", "" },
            {"Line-S2", "" },
        };

        internal static List<string> LineList = new List<string>()
            {"Line-A", "Line-B","Line-C", "Line-D", "Line-E", "Line-F", "Line-G", "Line-H", "Line-I","Line-J", "Line-K","Line-L", "Line-M", "Line-N", "Line-O","Line-P","Line-Q1","Line-Q2","Line-R1","Line-R2","Line-S1","Line-S2"};

        private string[] _lines = new string[]
        {
            "System\\Line-A",
            "System\\Line-B",
            "System\\Line-C",
            "System\\Line-D",
            "System\\Line-E",
            "System\\Line-F",
            "System\\Line-G",
            "System\\Line-H",
            "System\\Line-I",
            "System\\Line-J",
            "System\\Line-K",
            "System\\Line-L",
            "System\\Line-M",
            "System\\Line-N",
            "System\\Line-O",
            "System\\Line-P",
            "System\\Line-Q",
            "System\\Line-R",
            "System\\Line-S"
        };

        private string[] _LineControlEndpointAddress = new string[]
        {
            "net.tcp://smt-a:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-b:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-c:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-d:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-e:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-f:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-g:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-h:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-i:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-j:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-k:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-l:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-m:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-n:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-o:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-p:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-q:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-r:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-s:1406/Asm.As.Oib.SiplacePro.LineControl/reliable"
        };

        internal static Dictionary<string, string> LocationDic = new Dictionary<string, string>() { { "1", "1" }, { "2", "3" }, { "3", "2" }, { "0", "4" } };
        internal static Dictionary<string, string> DivisionDic = new Dictionary<string, string>() { { "0", "1" }, { "1", "2" }, { "2", "3" } };
    }
}