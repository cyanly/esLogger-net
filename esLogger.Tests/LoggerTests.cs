using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using esLogger;

namespace esLogger.Tests
{
    [TestClass]
    public class LoggerTests
    {
        [TestInitialize]
        public void Setup()
        {
        }

        [TestMethod]
        public void TestLogger()
        {
            Logger.Info("Test Info");
            Logger.Warn("Test Warn");
            Logger.Error("Test Error", new ApplicationException());

            Logger.Info(new
            {
                a = 1,
                b = "test"
            });
            Logger.Warn(new
            {
                a = 1,
                b = "test"
            });
            Logger.Error(new
            {
                a = 1,
                b = "test"
            }, new ApplicationException());

            Logger.ConnectElasticSearch();
            Logger.Info(new
            {
                a = 1,
                b = "test"
            });

            Logger.Flush();
        }
    }
}
