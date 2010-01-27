using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace mAdcOW.DataStructures.Test
{
    /// <summary>
    /// Summary description for RecursiveTest
    /// </summary>
    [TestClass]
    public class RecursiveTest
    {
        public class cSandboxFileInfo
        {
            public string La { get; set; }
            public string Lu { get; set; }
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }


        [TestMethod]
        public void TestMethod1()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            DataStructures.Dictionary<string, cSandboxFileInfo> dictSandboxFileInfo = new Dictionary<string, cSandboxFileInfo>(path, 1000);
            cSandboxFileInfo c = new cSandboxFileInfo {La = "lalalalala", Lu = "lululululu"};
            dictSandboxFileInfo.Add("somekey", c);


            DataStructures.Dictionary<string, string> keyDict = new Dictionary<string, string>(path, 1000);
            KeyValuePair<string, string> kvp = new KeyValuePair<string, string>("somekey", "bbbb");
            keyDict.Add(kvp);

            foreach (KeyValuePair<string, string> deSandboxFile in keyDict)
            {
                var val = dictSandboxFileInfo[deSandboxFile.Key];
            }            

        }
    }
}
