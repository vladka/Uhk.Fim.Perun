using System.Threading;
using Agama.IOC.Tests;
using Agama.Services.Api.IOC;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Agama.IOC.Perun.Tests
{
    [TestClass]
    public class PerunTest : IocAdapterBaseTest
    {
        protected override IIocContainer CreateContainer()
        {
            return new PerunAdapter();
        }

       


        [TestMethod]
        public override void HttpContextScope()
        {
            base.HttpContextScope();
        }

        [TestMethod]
        public override void ThreadScope()
        {
            base.ThreadScope();
        }

        [TestMethod]
        public override void TransientScope()
        {
            base.TransientScope();
        }
        [TestMethod]
        public override void SingletonScope()
        {
            base.SingletonScope();
        }
        [TestMethod]
        public override void RegisterInstance()
        {
            base.RegisterInstance();
        }

        [TestMethod]
        public override void IsConfiguredFor()
        {
            base.IsConfiguredFor();
        }

        [TestMethod]
        public override void GetServices()
        {
            base.GetServices();
        }

        [TestMethod]
        public override void Dispose1()
        {
            base.Dispose1();
        }

        [TestMethod]
        public override void WiringEnumerable()
        {
            base.WiringEnumerable();
        }
        [TestMethod]
        public override void WiringFunc()
        {
            base.WiringFunc();
        }
        [TestMethod]
        public override void WiringLazy()
        {
            base.WiringLazy();
        }

        [TestMethod]
        public override void Advanced1()
        {
            base.Advanced1();
        }

        [TestMethod]
        public override void Advanced1b()
        {
            base.Advanced1b();
        }

        [TestMethod]
        public override void Advanced2()
        {
            base.Advanced2();
        }
        
        [TestMethod]
        public override void Advanced3GetServices()
        {
            base.Advanced3GetServices();
        }
     

        [TestMethod]
        public override void Performace1()
        {
            base.Performace1();
        }
        [TestMethod]
        public override void Performace2()
        {
            base.Performace2();
        }

        [TestMethod]
        public override void Performace3()
        {
            base.Performace3();
        }
    }
}
