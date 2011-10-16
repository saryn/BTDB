using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BTDB.Service;
using NUnit.Framework;

namespace BTDBTest
{
    [TestFixture]
    public class ConnectedServiceTest
    {
        PipedTwoChannels _pipedTwoChannels;
        IService _first;
        IService _second;

        [SetUp]
        public void Setup()
        {
            _pipedTwoChannels = new PipedTwoChannels();
            _first = new Service(_pipedTwoChannels.First);
            _second = new Service(_pipedTwoChannels.Second);
        }

        [TearDown]
        public void TearDown()
        {
            Disconnect();
            _first.Dispose();
            _second.Dispose();
        }

        public void Disconnect()
        {
            _pipedTwoChannels.Disconnect();
        }

        public interface IAdder
        {
            int Add(int a, int b);
        }

        public class Adder : IAdder
        {
            public int Add(int a, int b)
            {
                return a + b;
            }
        }

        [Test]
        public void BasicTest()
        {
            _first.RegisterMyService(new Adder());
            Assert.AreEqual(3, _second.QueryOtherService<IAdder>().Add(1, 2));
        }

        public interface IIface1
        {
            int Meth1(string param);
            string Meth2();
            bool Meth3(bool a, bool b);
            void Meth4();
        }

        public interface IIface1Async
        {
            Task<int> Meth1(string param);
            Task<string> Meth2();
            Task<bool> Meth3(bool a, bool b);
            Task Meth4();
        }

        public class Class1 : Adder, IIface1
        {
            internal bool Meth4Called;

            public int Meth1(string param)
            {
                return param.Length;
            }

            public string Meth2()
            {
                return "Hello World";
            }

            public bool Meth3(bool a, bool b)
            {
                return a && b;
            }

            public void Meth4()
            {
                Meth4Called = true;
            }
        }

        [Test]
        public void ServiceWithIAdderAndIIface1()
        {
            var service = new Class1();
            _first.RegisterMyService(service);
            Assert.AreEqual(5, _second.QueryOtherService<IAdder>().Add(10000, -9995));
            var i1 = _second.QueryOtherService<IIface1>();
            Assert.AreEqual(2, i1.Meth1("Hi"));
            Assert.AreEqual("Hello World", i1.Meth2());
            Assert.AreEqual(true, i1.Meth3(true, true));
            i1.Meth4();
            Assert.True(service.Meth4Called);
        }

        [Test]
        public void AutomaticConversionToAsyncIface()
        {
            var service = new Class1();
            _first.RegisterMyService(service);
            var i1 = _second.QueryOtherService<IIface1Async>();
            AssertIIface1Async(i1);
            Assert.True(service.Meth4Called);
        }

        static void AssertIIface1Async(IIface1Async i1)
        {
            Assert.AreEqual(2, i1.Meth1("Hi").Result);
            Assert.AreEqual("Hello World", i1.Meth2().Result);
            Assert.AreEqual(true, i1.Meth3(true, true).Result);
            i1.Meth4().Wait();
        }

        [Test]
        public void DelegateAsService()
        {
            _first.RegisterMyService((Func<double, double, double>)((a, b) => a + b));
            var d = _second.QueryOtherService<Func<double, double, double>>();
            Assert.AreEqual(31.0, d(10.5, 20.5), 1e-10);
        }

        public class Class1Async : IIface1Async
        {
            internal bool Meth4Called;

            public Task<int> Meth1(string param)
            {
                var tco = new TaskCompletionSource<int>();
                tco.SetResult(param.Length);
                return tco.Task;
            }

            public Task<string> Meth2()
            {
                return Task.Factory.StartNew(() => "Hello World");
            }

            public Task<bool> Meth3(bool a, bool b)
            {
                return Task.Factory.StartNew(() => a && b);
            }

            public Task Meth4()
            {
                return Task.Factory.StartNew(() => { Meth4Called = true; });
            }
        }

        [Test]
        public void AsyncIfaceOnServerSide()
        {
            var service = new Class1Async();
            _first.RegisterMyService(service);
            var i1 = _second.QueryOtherService<IIface1Async>();
            AssertIIface1Async(i1);
            Assert.True(service.Meth4Called);
        }

        [Test]
        public void MultipleServicesAndIdentity()
        {
            _first.RegisterMyService(new Adder());
            _first.RegisterMyService(new Class1Async());
            var adder = _second.QueryOtherService<IAdder>();
            Assert.AreSame(adder, _second.QueryOtherService<IAdder>());
            var i1 = _second.QueryOtherService<IIface1>();
            Assert.AreSame(i1, _second.QueryOtherService<IIface1>());
        }

        [Test]
        public void ClientServiceDeallocedWhenNotneeded()
        {
            _first.RegisterMyService(new Adder());
            var weakAdder = new WeakReference(_second.QueryOtherService<IAdder>());
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.False(weakAdder.IsAlive);
            var adder = _second.QueryOtherService<IAdder>();
            Assert.AreEqual(2, adder.Add(1, 1));
        }

        [Test]
        public void IfaceToDelegateConversion()
        {
            _first.RegisterMyService(new Adder());
            var adder = _second.QueryOtherService<Func<int, int, int>>();
            Assert.AreEqual(5, adder(2, 3));
        }

        [Test]
        public void DelegateToIfaceConversion()
        {
            _first.RegisterMyService((Func<int, int, int>)((a, b) => a + b));
            Assert.AreEqual(30, _second.QueryOtherService<IAdder>().Add(10, 20));
        }

        public interface IIface2
        {
            int Invoke(string param);
            int Invoke(int param);
        }

        public class Class2 : IIface2
        {
            public int Invoke(string param)
            {
                return param.Length;
            }

            public int Invoke(int param)
            {
                return param * param;
            }
        }

        [Test]
        public void BasicMethodOverloading()
        {
            _first.RegisterMyService(new Class2());
            var i2 = _second.QueryOtherService<IIface2>();
            Assert.AreEqual(9, i2.Invoke(3));
            Assert.AreEqual(2, i2.Invoke("Hi"));
        }

        public interface IIface3A
        {
            int Invoke(string param, string param2);
            int Invoke(int param2, int param);
        }

        public interface IIface3B
        {
            int Invoke(string param2, string param);
            int Invoke(int param, int param2);
        }

        public class Class3 : IIface3A
        {
            public int Invoke(string param, string param2)
            {
                return param.Length + 2 * (param2 ?? "").Length;
            }

            public int Invoke(int param2, int param)
            {
                return 2 * param2 + param;
            }
        }

        [Test]
        public void ChangingNumberAndOrderOfParameters()
        {
            _first.RegisterMyService(new Class3());
            var i2 = _second.QueryOtherService<IIface2>();
            Assert.AreEqual(3, i2.Invoke(3));
            Assert.AreEqual(2, i2.Invoke("Hi"));
            var i3 = _second.QueryOtherService<IIface3A>();
            Assert.AreEqual(10, i3.Invoke(3, 4));
            Assert.AreEqual(8, i3.Invoke("Hi", "Dev"));
            var i3O = _second.QueryOtherService<IIface3B>();
            Assert.AreEqual(10, i3O.Invoke(4, 3));
            Assert.AreEqual(7, i3O.Invoke("Hi", "Dev"));
        }

        [Test]
        public void ByteArraySupport()
        {
            _first.RegisterMyService((Func<byte[], byte[]>)(p => p.AsEnumerable().Reverse().ToArray()));
            var d = _second.QueryOtherService<Func<byte[], byte[]>>();
            Assert.AreEqual(new byte[] { 255, 3, 2, 1, 0 }, d(new byte[] { 0, 1, 2, 3, 255 }));
        }

        [Test]
        public void SimpleConversion()
        {
            _first.RegisterMyService((Func<int, int>)(p => p * p));
            var d = _second.QueryOtherService<Func<short, string>>();
            Assert.AreEqual("81", d(9));
        }

        [Test]
        public void PassingException()
        {
            _first.RegisterMyService((Func<int>)(() => { throw new ArgumentException("msg", "te" + "st"); }));
            var d = _second.QueryOtherService<Func<int>>();
            var e = Assert.Catch(() => d());
            Assert.IsInstanceOf<AggregateException>(e);
            Assert.AreEqual(1, ((AggregateException)e).InnerExceptions.Count);
            e = ((AggregateException)e).InnerExceptions[0];
            Assert.IsInstanceOf<ArgumentException>(e);
            Assert.That(e.Message, Is.StringStarting("msg"));
            Assert.AreEqual("test", ((ArgumentException)e).ParamName);
        }

        [Test]
        public void DisconnectionShouldCancelRunningMethods()
        {
            var e = new AutoResetEvent(false);
            _first.RegisterMyService((Func<Task>)(() => Task.Factory.StartNew(() => e.WaitOne(1000))));
            var d = _second.QueryOtherService<Func<Task>>();
            var task = d();
            task = task.ContinueWith(t => Assert.True(t.IsCanceled));
            Disconnect();
            task.Wait(1000);
            e.Set();
        }

        [Test]
        public void SupportIListAsParameters()
        {
            _first.RegisterMyService((Func<IList<long>, IList<long>>)(p => p.Reverse().ToList()));
            var d = _second.QueryOtherService<Func<IList<long>, IList<long>>>();
            Assert.AreEqual(new List<long> { 3, 2, 1 }, d(new List<long> { 1, 2, 3 }));
        }

        public class SimpleObject
        {
            public string Name { get; set; }
            public double Number { get; set; }
        }

        [Test, Ignore]
        public void SupportSimpleObjectsAsParameters()
        {
            SimpleObject received = null;
            _first.RegisterMyService((Action<SimpleObject>)(a => received = a));
            var d = _second.QueryOtherService<Action<SimpleObject>>();
            d(new SimpleObject { Name = "Text", Number = 3.14 });
            Assert.NotNull(received);
            Assert.AreEqual("Text", received.Name);
            Assert.AreEqual(3.14, received.Number, 1e-14);
        }

    }
}