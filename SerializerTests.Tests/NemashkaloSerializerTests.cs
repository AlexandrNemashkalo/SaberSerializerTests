using Microsoft.VisualStudio.TestTools.UnitTesting;
using SerializerTests.Implementations;
using SerializerTests.Interfaces;
using SerializerTests.Nodes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SerializerTests.Tests;

[TestClass]
public class NemashkaloSerializerTests
{
    private const int ListLength = 100000;
    private const string BaseData = "qwerty";
    private IListSerializer ListSerializer => new NemashkaloSerializer();

    [TestMethod]
    public async Task NemashkaloSerializerTest__ReturnSuccess()
    {
        var rnd = new Random();
        var head = CreateList(ListLength, x => BaseData + x , x => rnd.Next(ListLength));
        var ser = ListSerializer;

        var deepCopyHead = await ser.DeepCopy(head);
        var serHead = await SerializeAndDeserialize(head, ser);

        AsserAreDataEqual(deepCopyHead, serHead);
    }

    [TestMethod]
    public async Task DeserializerTest__InvalidStream__ReturnArgumentException()
    {
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => ListSerializer.Deserialize(null));
        using (var stream = new MemoryStream())
        {
            stream.Close();
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => ListSerializer.Deserialize(stream));
        }
    }

    [DataRow("{}")]
    [DataRow("[}")]
    [DataRow("qwerty")]
    [DataRow("[{\"Data\":\"qwerty\"}]")]
    [DataRow("{\"Id\":0,\"Data\":\"qwerty0\"}{\"Id\":1,\"Data\":\"qwerty1\"}")]
    [DataRow("{\"Id\":0,\"Data\":\"qwerty0\"},{\"Id\":1,\"Data\":\"qwerty1\"}")]
    [DataRow("[{\"Id\":0,\"Data\":\"qwerty0\"}{\"Id\":1,\"Data\":\"qwerty1\"}]")]
    [DataTestMethod]
    public async Task DeserializerTest__InvalidData__ReturnArgumentException(string invalidData)
    {
        var bytes = Encoding.ASCII.GetBytes(invalidData);
        using (var stream = new MemoryStream(bytes))
        {
            await Assert.ThrowsExceptionAsync<ArgumentException>(() => ListSerializer.Deserialize(stream));
        }
    }

    [DataRow("{\"Id\":0}")]
    [DataRow("[{\"Id\":0}]")]
    [DataRow("[{\"Id\":0,\"Data\":\"qwerty\"}]")]
    [DataRow("[{\"Id\":0,\"Data\":\"qwerty0\"},{\"Id\":1,\"Data\":\"qwerty1\"}]")]
    [DataTestMethod]
    public async Task DeserializerTest__CorrectData__ReturnSuccess(string correctData)
    {
        var bytes = Encoding.ASCII.GetBytes(correctData);
        using (var stream = new MemoryStream(bytes))
        {
            var newHead = await ListSerializer.Deserialize(stream);
            Assert.IsNotNull(newHead);
        }
    }

    [DataRow("[{\"Id\":0,\"RandomId\":null,\"Data\":\"qwerty\"}]")]
    [DataTestMethod]
    public async Task DeserializerAndSerialize__CorrectData__ReturnSuccess(string correctData)
    {
        var bytes = Encoding.ASCII.GetBytes(correctData);
        var newBytes = await DeserializeAndSerialize(bytes);
        var newStr = Encoding.Default.GetString(newBytes);
        Assert.AreEqual(correctData, newStr.TrimEnd('\0'));
    }

    [TestMethod]
    public async Task SerializerTest__NullNode__ReturnNullSuccess()
    {
        var newHead = await SerializeAndDeserialize(null);
        Assert.IsNull(newHead);
    }

    [TestMethod]
    public async Task SerializerTest__DifferentData_WithRandom__ReturnSuccess()
    {
        Random rnd = new Random();
        var head = CreateList(ListLength, x => BaseData + x, x => rnd.Next(ListLength));

        var newHead = await SerializeAndDeserialize(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task SerializerTest__DifferentData_WithoutRandom__ReturnSuccess()
    {
        var head = CreateList(ListLength, x => BaseData + x , x => null);

        var newHead = await SerializeAndDeserialize(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task SerializerTest__EqualData_WithoutRandom__ReturnSuccess()
    {
        var head = CreateList(ListLength, x => BaseData, x => null);

        var newHead = await SerializeAndDeserialize(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task SerializerTest__NullData_WithoutRandom__ReturnSuccess()
    {
        var head = CreateList(ListLength, x => null, x => null);

        var newHead = await SerializeAndDeserialize(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task DeepCopyTest__DifferentData_WithRandom__ReturnSuccess()
    {
        var rnd = new Random();
        var head = CreateList(ListLength, x => BaseData + x, x => rnd.Next(ListLength));

        var newHead = await ListSerializer.DeepCopy(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task DeepCopyTest__DifferentData_WithoutRandom__ReturnSuccess()
    {
        var head = CreateList(ListLength, x => BaseData + x, x => null);

        var newHead = await ListSerializer.DeepCopy(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task DeepCopyTest__EqualData_WithoutRandom__ReturnSuccess()
    {
        var head = CreateList(ListLength, x => BaseData, x => null);

        var newHead = await ListSerializer.DeepCopy(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task DeepCopyTest__NullData_WithoutRandom__ReturnSuccess()
    {
        var head = CreateList(ListLength, x => null, x => null);

        var newHead = await ListSerializer.DeepCopy(head);

        AsserAreDataEqual(head, newHead);
    }

    private async Task<byte[]> DeserializeAndSerialize(byte[] data, IListSerializer ser = null)
    {
        ser ??= ListSerializer;
        using (var stream = new MemoryStream())
        {
            stream.Write(data, 0, data.Length);
            stream.Position = 0;
            var node = await ser.Deserialize(stream);
            stream.Position = 0;
            await ser.Serialize(node, stream);
            return stream.GetBuffer();
        }
    }

    private async Task<ListNode> SerializeAndDeserialize(ListNode oldHead, IListSerializer ser = null)
    {
        ser ??= ListSerializer;
        using (var stream = new MemoryStream())
        {
            await ser.Serialize(oldHead, stream);
            stream.Position = 0;
            return await ser.Deserialize(stream);
        }
    }

    private void AsserAreDataEqual(ListNode oldHead, ListNode newHead)
    {
        var currentNewNode = newHead;
        var currentOldNode = oldHead;
        while (currentNewNode != null && currentOldNode != null)
        {
            Assert.AreEqual(currentOldNode.Data, currentNewNode.Data, "currentNewNode.Data");
            Assert.AreEqual(currentOldNode.Random?.Data, currentNewNode.Random?.Data, "currentNewNode.Random.Data");
            Assert.AreEqual(currentOldNode.Next?.Data, currentNewNode.Next?.Data, "currentNewNode.Next.Data");
            Assert.AreEqual(currentOldNode.Previous?.Data, currentNewNode.Previous?.Data, "currentNewNode.Previous.Data");

            currentNewNode = currentNewNode.Next;
            currentOldNode = currentOldNode.Next;
        }
        Assert.AreEqual(null, currentNewNode);
        Assert.AreEqual(null, currentOldNode);
    }

    private ListNode CreateList(
        int length,
        Func<int,string> getData,
        Func<int,int?> getRandom
    )
    {
        var list = new List<ListNode>();
        for (var i = 0; i < length; i++)
        {
            var listNode = new ListNode()
            {
                Data = getData(i)
            };

            if (i > 0)
            {
                listNode.Previous = list[i - 1];
                list[i - 1].Next = listNode;
            }

            list.Add(listNode);
        }

        for (var i = 0; i < length; i++)
        {
            var randomIndex = getRandom(i);
            if (randomIndex != null)
            {
                list[i].Random = list[randomIndex.Value];
            }
        }

        return list[0];
    }
}