using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SerializerTests.Interfaces;
using SerializerTests.Nodes;

namespace SerializerTests.Implementations;

public class NemashkaloSerializer : IListSerializer
{
    class ListNodeShort
    {
        public int Id { get; set; }
        public int? RandomId { get; set; }
        public string? Data { get; set; }
    }

    public Task<ListNode> DeepCopy(ListNode head)
    {
        var nodeDict = new Dictionary<ListNode, ListNode>();
        var randomNodeDict = new Dictionary<ListNode, ListNode>();

        var currentNode = head;
        while (currentNode != null)
        {
            var newNode = new ListNode
            {
                Data = currentNode.Data
            };

            if(currentNode.Random != null)
            {
                if (nodeDict.TryGetValue(currentNode.Random, out var value))
                {
                    newNode.Random = value;
                }
                else
                {
                    randomNodeDict.Add(newNode, currentNode.Random);
                }
            }

            if(currentNode != head)
            {
                newNode.Previous = nodeDict[currentNode.Previous];
                newNode.Previous.Next = newNode;
            }

            nodeDict.Add(currentNode, newNode);
            currentNode = currentNode.Next;
        }

        foreach(var (newNode, randomNode) in randomNodeDict)
        {
            newNode.Random = nodeDict[randomNode];
        }

        return Task.FromResult(nodeDict[head]);
    }

    public async Task<ListNode> Deserialize(Stream s)
    {
        if(s == null || !s.CanRead) throw new ArgumentException("Invalid stream for deserialization to ListNode");

        var list = new List<ListNode>();
        var randomNodeIdDict = new Dictionary<int,int>();

        try
        {
            using (var reader = new StreamReader(s))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var i = 0;
                while (await jsonReader.ReadAsync())
                {
                    if (jsonReader.TokenType != JsonToken.StartObject) continue;

                    var obj = await JObject.LoadAsync(jsonReader);
                    var listNode = new ListNode
                    {
                        Data = obj.Value<string>("Data")
                    };

                    list.Add(listNode);

                    var id = obj.Value<int?>("Id");
                    if(id == null)
                    {
                        throw new ArgumentException();
                    }
                    var randomId = obj.Value<int?>("RandomId");

                    if (randomId != null)
                    {
                        if (randomId > i)
                        {
                            randomNodeIdDict.Add(id.Value, randomId.Value);
                        }
                        else
                        {
                            listNode.Random = list[randomId.Value];
                        }
                    }

                    if (i > 0)
                    {
                        list[i - 1].Next = listNode;
                        listNode.Previous = list[i - 1];
                    }

                    i++;
                }

                if (i == 0) return null;
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid data for deserialization to ListNode", ex);
        }

        foreach (var (id, randomId) in randomNodeIdDict)
        {
            list[id].Random = list[randomId];
        }

        return list[0];
    }

    public async Task Serialize(ListNode node, Stream stream)
    {
        if (stream == null || !stream.CanWrite) throw new ArgumentException("Invalid stream for serialization to ListNode");
        var nodeDict = new Dictionary<ListNode, int>();

        var i = 0;
        var currentNode = node;
        while (currentNode != null)
        {
            nodeDict.Add(currentNode, i);
            currentNode = currentNode.Next;
            i++;
        }

        using (var writer = new StreamWriter(stream))
        using (var jsonWriter = new JsonTextWriter(writer))
        {
            var ser = new JsonSerializer();
            await jsonWriter.WriteStartArrayAsync();
            foreach (var (listNode, id) in nodeDict)
            {
                var listNodeShort = new ListNodeShort()
                {
                    Id = id,
                    Data = listNode.Data                    
                };

                if (listNode.Random != null)
                {
                    listNodeShort.RandomId = nodeDict[listNode.Random];
                }
                
                ser.Serialize(jsonWriter, listNodeShort);
            }

            await jsonWriter.WriteEndArrayAsync();
            await jsonWriter.FlushAsync();
        }
    }
}