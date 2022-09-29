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
    private IListSerializer ListSerializer => new NemashkaloSerializer();

    [TestMethod]
    public async Task NemashkaloSerializerTest__ReturnSuccess()
    {
        var rnd = new Random();
        var head = CreateList(10000, (x) => $"qwerty{x}", (x) => rnd.Next(10000));
        var ser = ListSerializer;

        var deepCopyHead = await ser.DeepCopy(head);
        var serHead = await SerializerAndDeserialize(head, ser);

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

    [TestMethod]
    public async Task SerializerTest__NullNode__ReturnNullSuccess()
    {
        var newHead = await SerializerAndDeserialize(null);
        Assert.IsNull(newHead);
    }

    [TestMethod]
    public async Task SerializerTest__DifferentData_WithRandom__ReturnSuccess()
    {
        Random rnd = new Random();
        var head = CreateList(10000, (x) => $"qwerty{x}", (x) => rnd.Next(10000));

        var newHead = await SerializerAndDeserialize(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task SerializerTest__DifferentData_WithoutRandom__ReturnSuccess()
    {
        var head = CreateList(10000, (x) => $"qwerty{x}", (x) => null);

        var newHead = await SerializerAndDeserialize(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task SerializerTest__EqualData_WithoutRandom__ReturnSuccess()
    {
        var head = CreateList(10000, (x) => $"qwerty", (x) => null);

        var newHead = await SerializerAndDeserialize(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task SerializerTest__NullData_WithoutRandom__ReturnSuccess()
    {
        var head = CreateList(10000, (x) => null, (x) => null);

        var newHead = await SerializerAndDeserialize(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task DeepCopyTest__DifferentData_WithRandom__ReturnSuccess()
    {
        var rnd = new Random();
        var head = CreateList(10000, (x) => $"qwerty{x}", (x) => rnd.Next(10000));

        var newHead = await ListSerializer.DeepCopy(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task DeepCopyTest__DifferentData_WithoutRandom__ReturnSuccess()
    {
        var head = CreateList(10000, (x) => $"qwerty{x}", (x) => null);

        var newHead = await ListSerializer.DeepCopy(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task DeepCopyTest__EqualData_WithoutRandom__ReturnSuccess()
    {
        var head = CreateList(10000, (x) => $"qwerty", (x) => null);

        var newHead = await ListSerializer.DeepCopy(head);

        AsserAreDataEqual(head, newHead);
    }

    [TestMethod]
    public async Task DeepCopyTest__NullData_WithoutRandom__ReturnSuccess()
    {
        var head = CreateList(10000, (x) => null, (x) => null);

        var newHead = await ListSerializer.DeepCopy(head);

        AsserAreDataEqual(head, newHead);
    }

    private async Task<ListNode> SerializerAndDeserialize(ListNode oldHead, IListSerializer ser = null)
    {
        byte[] data;
        ListNode newHead;
        ser ??= ListSerializer;
        using (var stream = new MemoryStream())
        {
            await ser.Serialize(oldHead, stream);
            data = stream.ToArray();
        }

        using (var newStream = new MemoryStream(data))
        {
            return await ser.Deserialize(newStream);
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