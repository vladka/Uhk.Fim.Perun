using System.Collections.Generic;
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
        public void Test001()
        {
            var disposed = false;
            var created = false;
            using (var ioc = new PerunContainer())
             {
                 IConfiguredPluginInfo<ITestDisposable> regInfo = ioc.RegisterType<ITestDisposable, MockDisposableClass>(ioc);
                 regInfo.AfterBuiltNewComponent+= (sender,args) =>
                                                       {
                                                           created = true;
                                                           args.Component.BeforeDisposeAction =() => disposed = true;
                                                           //((AfterBuiltComponentEventArgs)args).Component = new MockDisposableClass2();
                                                       };
                 var myComponent = ioc.GetService<ITestDisposable>();
                 Assert.IsTrue(created); 
                 regInfo.Dispose();

                 Assert.IsTrue(disposed);
                 Assert.IsFalse(ioc.IsConfiguredFor(typeof(ITestDisposable)));
             }
        }

        /// <summary>
        ///A test for AfterMissedComponent event, and GetService
        ///</summary>
        [TestMethod()]
        public void Test002()
        {
           
            using (var ioc = new PerunContainer())
            {
                ioc.AfterMissedComponent += (sender, args) =>
                                                {
                                                    if (args.RequieredComponentType == typeof(ITestDisposable))
                                                        ioc.RegisterType<ITestDisposable, MockDisposableClass>();
                                                            
                                                };
                
                var myComponent = ioc.GetService<ITestDisposable>();
                Assert.IsNotNull(myComponent);

            }
        }

        /// <summary>
        ///A test for AfterMissedComponent event, and GetService
        ///</summary>
        [TestMethod()]
        public void Test003()
        {
            bool released = false;
            using (var ioc = new PerunContainer())
            {
                
                ioc.RegisterType<ITestDisposable, MockDisposableClass>(ioc/*=singleton*/)
                    .BeforeReleaseComponent+=(sender,args)=>
                                                 {
                                                     released = true;
                                                     args.RunDispose = true;//but this behavior is default
                                                 };

                var myComponent = ioc.GetService<ITestDisposable>();
            }
            Assert.IsTrue(released);
        }

        private class MockCircular
        {
            public MockCircular(MockCircular p)
            {
                
            }
        }


        [TestMethod()]
        public void CircularDependecyTest()
        {
            return;
            using (var ioc = new PerunContainer())
            {
                //subtest1
                ioc.RegisterType<object>(ioc.GetService<object>);
                
                //subtest2
                ioc.RegisterType<MockCircular>();
                var myComponent = ioc.GetService<MockCircular>();
            }
        }


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

                ioc.RegisterType<ITestDisposable, MockDisposableClass>(ioc); //as singleton /* PerunConatiner implements IPersunScope*/
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
