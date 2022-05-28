using System.Net;
using CommandLib.Commands.RemoteCommandItems;
using CommandLib.Commands.TransferCommandItems;
using NUnit.Framework;

namespace Tests.CommandLibTests
{
    public class ObjectTests
    {
        [SetUp]
        public void Setup() { }
        
        [Test]
        public void RemoteObjectToBytes()
        {
            var obj = new RemoteObject("1.1.1.1", 1);
            var bytes = obj.ToBytes();
            var newObj = RemoteObject.FromBytes(bytes, typeof(RemoteObject));
            var data1 = (IPEndPoint)obj.GetData();
            var data2 = (IPEndPoint)newObj.GetData();
            
            Assert.AreEqual(data1.Address, data2.Address);
        }

        [Test]
        public void TransferObjectToBytes()
        {
            var obj = new TransferObject("1", 1, "1");
            var bytes = obj.ToBytes();
            var newObj = RemoteObject.FromBytes(bytes, typeof(TransferObject));
            var data1 = ((IPEndPoint, string))obj.GetData();
            var data2 = ((IPEndPoint, string))newObj.GetData();
            
            Assert.AreEqual(data1.Item2, data2.Item2);
        }
    }
}