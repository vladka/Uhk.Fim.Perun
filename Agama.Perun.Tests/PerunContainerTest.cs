using Agama.Perun;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Agama.Perun.Tests
{
    
    
    /// <summary>
    ///This is a test class for PerunContainer and is intended
    ///to contain all PerunContainerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class PerunContainerTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Dispose
        ///</summary>
        [TestMethod()]
        public void DisposeTest()
        {
            DisposePlugins();
        }


        

        /// <summary>
        /// Test, zda pri vyjmuti pluginu dojde k dojde k disposnutí držených instancí.
        /// </summary>
        public virtual void DisposePlugins()
        {
            bool disposed = false;
            using (var ioc = new PerunContainer())
            {

                ioc.RegisterType<ITestDisposable, TestDisposableClass>(ioc);
                var a = ioc.GetService<ITestDisposable>();
                a.BeforeDisposeAction = () => disposed = true;

                Assert.IsTrue(ioc.IsConfiguredFor(typeof(ITestDisposable)));

                ioc.DisposeService(typeof(ITestDisposable));

                Assert.IsTrue(disposed);
                Assert.IsFalse(ioc.IsConfiguredFor(typeof(ITestDisposable)));
            }
        }
    }
}
