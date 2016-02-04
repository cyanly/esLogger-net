using System;

using esLogger;

namespace esLogger.ConsoleDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.Info("Test Info");
            Logger.Warn("Test Warn");
            Logger.Error("Test Error", new ApplicationException());

            Logger.Info(new
            {
                number = 12345,
                message = "test 1"
            });
            Logger.Warn(new
            {
                number = 54321,
                message = "test 2"
            });
            Logger.Error(new
            {
                value = 789.12,
                message = "test 3"
            }, new ApplicationException());


            Logger.ConnectElasticSearch();

            Logger.Info(new
            {
                number = 12345,
                message = "test info"
            });
            
            Logger.Warn(new
            {
                number = 54321,
                message = "test warning"
            });
            Logger.Error(new
            {
                value = 789.12,
                message = "test"
            }, new ApplicationException());

            System.Console.ReadLine();

        }
    }
}
