using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Elasticsearch.Net;
using Elasticsearch.Net.Connection;
using esLogger.Utils;
using System.Threading;

namespace esLogger
{

    public static class Logger
    {
        // Constructor
        static Logger()
        {
            // Beauty print console colors
            EscapeSequencer.Install();
            EscapeSequencer.Bold = true;
        }

        public static void Stop() {
            stopToken.Cancel();
        }
        
        private static bool consoleOnly = true;
        private static ElasticsearchClient client;
        private static CancellationTokenSource stopToken = new CancellationTokenSource();

        /// <summary>
        /// Connect to ElasticSearch server, before connected all log entries will be catched in queue
        /// </summary>
        /// <param name="url"></param>
        public static void ConnectElasticSearch(string url = "http://localhost:9200/")
        {
            var node = new Uri(url);
            var config = new ConnectionConfiguration(node)
                                .EnableTrace(false)
                                .EnableMetrics(false)
                                .UsePrettyRequests(false)
                                .UsePrettyResponses(false);

            client = new ElasticsearchClient(config);
            consoleOnly = false;

            CancellationToken ct = stopToken.Token;
            Task taskConsumeQueue = Task.Run(() =>
            {
                while (true)
                {
                    // Bulk insert cached logs every 1 second
                    while (logEntries.IsEmpty)
                    {
                        System.Threading.Thread.Sleep(1000);

                        // <- Stop()
                        if (ct.IsCancellationRequested)
                        {
                            return;
                        }
                    }

                    int batchLength = 0;
                    var builder = new StringBuilder();
                    var timestamp = "logger-" + DateTime.UtcNow.ToString("yyyy-MM-dd");
                    var indexOp = "{ \"index\" : { \"_index\" : \"" + timestamp + "\", \"_type\" : \"log\" } }";
                    while (logEntries.IsEmpty == false && batchLength < 100)
                    {
                        JObject entry;
                        if (logEntries.TryDequeue(out entry))
                        {
                            builder.AppendLine(indexOp);
                            builder.AppendLine(entry.ToString(Formatting.None));
                            batchLength++;
                        }
                    }
                    if (batchLength > 0)
                        client.BulkAsync(builder.ToString());
                }
            });
        }

        private static ConcurrentQueue<JObject> logEntries = new ConcurrentQueue<JObject>();
        private static void postLog(JObject entry)
        {
            if (consoleOnly == false)
            {
                logEntries.Enqueue(entry);
            }
        }
        
        private static JObject appendSystemInfo(JObject entry)
        {
            entry["host"] = machineName;
            entry["pid"] = processId;
            entry["assembly"] = entryAssemblyName;
            entry["timestamp"] = DateTime.UtcNow.ToString("o"); // JSON ISO-8601 time format
            return entry;
        }

        /// <summary>
        /// Shorten file path in log item to avoid logging full path for every message
        /// </summary>
        public static string ProgramPathPrefix = "";
        private static Dictionary<string, string> fileModuleDict = new Dictionary<string, string>();
        private static string extractFileName(string filepath)
        {
            if (string.IsNullOrWhiteSpace(ProgramPathPrefix))
                return filepath;

            // Caching shortened source code filepath
            if (fileModuleDict.ContainsKey(filepath))
                return fileModuleDict[filepath];

            var fileModuleName = filepath;
            var n = filepath.LastIndexOf(ProgramPathPrefix + "\\");
            if (n != -1)
                fileModuleName = filepath.Substring(n + ProgramPathPrefix.Length + 1);
            fileModuleDict[filepath] = fileModuleName;

            return fileModuleName;
        }

        private static string entryAssemblyName = System.Diagnostics.Process.GetCurrentProcess().ProcessName.Replace(".vshost", string.Empty);
        private static int processId = System.Diagnostics.Process.GetCurrentProcess().Id;
        private static string machineName = Environment.MachineName;

        public static void Info(dynamic message
                                , [CallerMemberName] string callerName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLine = -1)
        {
            System.Console.WriteLine(string.Format("\x1B[32m{0}\x1B[36m {1} \x1B[0m \x1B[39m{2}", DateTime.Now.ToString("HH:mm:ss.f"), "INFO", message));
            var entry = Newtonsoft.Json.Linq.JObject.FromObject(message);
            appendSystemInfo(entry);
            entry["level"] = "INFO";
            entry["module"] = extractFileName(callerFilePath);
            entry["func"] = callerName;
            entry["line"] = callerLine;
            postLog(entry);
        }
        public static void Info(string message
                                , [CallerMemberName] string callerName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLine = -1)
        {
            System.Console.WriteLine(string.Format("\x1B[32m{0}\x1B[36m {1} \x1B[0m \x1B[39m{2}", DateTime.Now.ToString("HH:mm:ss.f"), "INFO", message));
            var entry = Newtonsoft.Json.Linq.JObject.FromObject(new
            {
                message = message
            });
            appendSystemInfo(entry);
            entry["level"] = "INFO";
            entry["module"] = extractFileName(callerFilePath);
            entry["func"] = callerName;
            entry["line"] = callerLine;
            postLog(entry);
        }

        public static void Warn(dynamic message
                                , [CallerMemberName] string callerName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLine = -1)
        {
            System.Console.WriteLine(string.Format("\x1B[32m{0}\x1B[33m {1} \x1B[0m {2}\x1B[39m", DateTime.Now.ToString("HH:mm:ss.f"), "WARN", message));
            var entry = Newtonsoft.Json.Linq.JObject.FromObject(message);
            appendSystemInfo(entry);
            entry["level"] = "WARN";
            entry["module"] = extractFileName(callerFilePath);
            entry["func"] = callerName;
            entry["line"] = callerLine;
            postLog(entry);
        }
        public static void Warn(string message
                                , [CallerMemberName] string callerName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLine = -1)
        {
            System.Console.WriteLine(string.Format("\x1B[32m{0}\x1B[33m {1} \x1B[0m {2}\x1B[39m", DateTime.Now.ToString("HH:mm:ss.f"), "WARN", message));
            var entry = Newtonsoft.Json.Linq.JObject.FromObject(new
            {
                message = message
            });
            appendSystemInfo(entry);
            entry["level"] = "WARN";
            entry["module"] = extractFileName(callerFilePath);
            entry["func"] = callerName;
            entry["line"] = callerLine;
            postLog(entry);
        }

        public static void Error(dynamic message, Exception ex
                                , [CallerMemberName] string callerName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLine = -1)
        {
            System.Console.WriteLine(string.Format("\x1B[32m{0}\x1B[31m {1} \x1B[0m {2}\x1B[39m", DateTime.Now.ToString("HH:mm:ss.f"), "ERROR", message));
            System.Console.WriteLine(ex);
            var entry = Newtonsoft.Json.Linq.JObject.FromObject(message);
            appendSystemInfo(entry);
            entry["level"] = "ERROR";
            entry["module"] = extractFileName(callerFilePath);
            entry["func"] = callerName;
            entry["line"] = callerLine;
            entry["error"] = JObject.FromObject(ex);
            postLog(entry);
        }
        public static void Error(string message, Exception ex
                                , [CallerMemberName] string callerName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLine = -1)
        {
            System.Console.WriteLine(string.Format("\x1B[32m{0}\x1B[31m {1} \x1B[0m {2}\x1B[39m", DateTime.Now.ToString("HH:mm:ss.f"), "ERROR", message));
            System.Console.WriteLine(ex);
            var entry = Newtonsoft.Json.Linq.JObject.FromObject(new
            {
                message = message
            });
            appendSystemInfo(entry);
            entry["level"] = "ERROR";
            entry["module"] = extractFileName(callerFilePath);
            entry["func"] = callerName;
            entry["line"] = callerLine;
            entry["error"] = JObject.FromObject(ex);
            postLog(entry);
        }

        public static void Fatal(dynamic message, Exception ex
                                , [CallerMemberName] string callerName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLine = -1)
        {
            System.Console.WriteLine(string.Format("\x1B[32m{0}\x1B[31m {1} \x1B[0m {2}\x1B[39m", DateTime.Now.ToString("HH:mm:ss.f"), "FATAL", message));
            System.Console.WriteLine(ex);
            var entry = Newtonsoft.Json.Linq.JObject.FromObject(message);
            appendSystemInfo(entry);
            entry["level"] = "FATAL";
            entry["module"] = extractFileName(callerFilePath);
            entry["func"] = callerName;
            entry["line"] = callerLine;
            entry["error"] = JObject.FromObject(ex);
            postLog(entry);

            Flush();
        }
        public static void Fatal(string message, Exception ex
                                , [CallerMemberName] string callerName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLine = -1)
        {
            System.Console.WriteLine(string.Format("\x1B[32m{0}\x1B[31m {1} \x1B[0m {2}\x1B[39m", DateTime.Now.ToString("HH:mm:ss.f"), "FATAL", message));
            System.Console.WriteLine(ex);
            var entry = Newtonsoft.Json.Linq.JObject.FromObject(new
            {
                message = message
            });
            appendSystemInfo(entry);
            entry["level"] = "FATAL";
            entry["module"] = extractFileName(callerFilePath);
            entry["func"] = callerName;
            entry["line"] = callerLine;
            entry["error"] = JObject.FromObject(ex);
            postLog(entry);

            Flush();
        }

        /// <summary>
        /// Fallback to sync behavior to guranteed all pending log entries are written
        /// </summary>
        public static void Flush()
        {
            while (logEntries.IsEmpty == false)
            {
                System.Threading.Thread.Sleep(200);
            }
        }
    }
}
