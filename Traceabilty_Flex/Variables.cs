using Asm.As.Oib.DisplayService.Contracts.Data;
using Asm.As.Oib.DisplayService.Proxy.Architecture.Objects;
using Asm.As.Oib.Monitoring.Proxy.Architecture.Objects;
using Asm.As.Oib.SiplacePro.Contracts.Data.Business.Objects;
using Asm.As.Oib.SiplacePro.Optimizer.Proxy.Business.Objects;
using Asm.As.Oib.WS.Eventing.Contracts.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using TraceabilityTestGui;

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow _mWindow;

        private string MonitorCallbackEndpoint = string.Format("http://{0}:{1}/RecipeMonitoring", Environment.MachineName, "33333");
        private string SubscriptionMonitorManagerEndpoint = "http://mignt048:1405/Asm.As.Oib.WS.Eventing.Services/SubscriptionManager";
        private Subscriber _subscriber;
         private LocationPcbCycle test= new LocationPcbCycle();
        private ReliableReceiver _receiver;
        private Subscription _currentSubscription;
        private Optimizer _optimizer;
        private readonly System.Timers.Timer _timer = new System.Timers.Timer(TimeSpan.FromHours(1).TotalMilliseconds);
        private readonly System.Timers.Timer _timerLost = new System.Timers.Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
        private const string DisplayHostName = "mignt048";
        private const string DisplayClientName = "DisplayClient";
        private const int DisplayPort = 33335;
        private DisplayServiceClient _client;
        private string _dir = @"C:\Tmp\Events";
        private DispatcherTimer _pingTimer;
        public int _delay = 60;
        public bool _checkLine = true;

        private bool PingResult;

        internal const string _patUnitID = @"\d{10}\/\d{4}";
        internal const string _patBatch = @"^\d{10}\z";

        DateTime LastTimeFromQMS;

        private const string progName = "Traceability Line Control";
        private string prefix = "System\\{0}\\{1}";

        public string baseAddressTraceability = string.Format("http://{0}:{1}/TraceabilityService", Environment.MachineName, "33333");

        private const string TraceabilityNotifyTopic = "SIPLACE:OIB:Traceability:Notify";
        //private readonly Regex directoryRegex = new Regex(@"\w*[:]*[\w+\\]+");
        private readonly string TimeFormat = "yyyy-MM-dd HH:mm:ss";

        private static ServiceHost m_SiplaceTraceabilityNotifyReceiver;
        private int m_MessageCount;
        private const int m_MaxMessageCount = 200;

        internal DataTable DTActiveLines;

        TextBlock[] FlashTextBlocs;
        ComboBox[] ComboList;
        TextBlock[] TextBlockList;
        TextBlock[] TextBlockStations;
        string[] TrustedHosts;

        List<Comp> listComp = new List<Comp>();
        List<Comp> listComPilot = new List<Comp>();

        static readonly int LIM = 50;
        private int _count;

        internal static string[] PartsException;
        internal static string[] CustomerList;
        internal static Button[] _buttons = null;
        internal static List<FlexLine> LineCollection = new List<FlexLine>();

        BackgroundWorker recipeWorker = new BackgroundWorker();
        BackgroundWorker monitorWorker = new BackgroundWorker();
        BackgroundWorker displayWorker = new BackgroundWorker();
        BackgroundWorker qmsWorker = new BackgroundWorker();

        internal static string User;
        internal static string Password;
        private static int Level = 1;
        internal static bool _mainservice = false;
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
            {"Line-P", false }
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
            {"Line-P", "" }
        };

        internal static List<string> RecipeList = new List<string>()
            {"Line-A", "Line-B","Line-C", "Line-D", "Line-E", "Line-F", "Line-G", "Line-H", "Line-I", "Line-J", "Line-K","Line-L", "Line-M", "Line-N", "Line-O","Line-P" };

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
            "System\\Line-P"
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
            "net.tcp://smt-i-:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-j:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-k:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-l:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-m:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-n:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-o:1406/Asm.As.Oib.SiplacePro.LineControl/reliable",
            "net.tcp://smt-p:1406/Asm.As.Oib.SiplacePro.LineControl/reliable"
        };

        private Dictionary<string, string> LineDic = new Dictionary<string, string>()
        {
            {"smt-a","10.229.5.65" },
            {"smt-b","10.229.5.55" },
            {"smt-c","10.229.5.52" },
            {"smt-d","10.229.5.53" },
            {"smt-e","10.229.5.51" },
            {"smt-f","10.229.5.58" },
            {"smt-g","10.229.5.56" },
            {"smt-h","10.229.5.57" },
            {"smt-i","10.229.5.59" },
            {"smt-j","10.229.5.54" },
            {"smt-k","10.229.5.60" },
            {"smt-l","10.229.5.61" },
            {"smt-m","10.229.5.62" },
            {"smt-n","10.229.5.63" },
            {"smt-o","10.229.5.64" },
            {"smt-p","10.229.5.50" }
        };

        internal static Dictionary<string, string> LocationDic = new Dictionary<string, string>() { { "1", "1" }, { "2", "3" }, { "3", "2" }, { "0", "4" } };
        internal static Dictionary<string, string> DivisionDic = new Dictionary<string, string>() { { "0", "1" }, { "1", "2" }, { "2", "3" } };
        private  Dictionary<string, List<string>> FifoDictionary = new Dictionary<string, List<string>>();
        private List<Lost> LostPallets = new List<Lost>();

        public struct Lost
        {
            public string line;
            public string pallet;
            public DateTime time;
        }
    }
}