// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;

namespace Friflo.Json.Mapper.Graph
{
    public class JsonPath : IDisposable
    {
        private             JsonSerializer                  serializer;
            
        private             Bytes                           targetJson = new Bytes(128);
        private             JsonParser                      targetParser;
        
        private readonly    List<PathNode<SelectorResults>> nodeStack = new List<PathNode<SelectorResults>>();
        private readonly    JsonPathQuery                   pathSelectorQuery = new JsonPathQuery();

        public void Dispose() {
            targetParser.Dispose();
            targetJson.Dispose();
            serializer.Dispose();
        }

        public SelectorResults Select(string json, string path, bool pretty = false) {
            var pathList = new [] {path};
            var result = Select(json, pathList, pretty);
            return result[0];
        }

        public List<SelectorResults> Select(string json, IList<string> pathList, bool pretty = false) {
            pathSelectorQuery.CreateNodeTree(pathList);
            Select(json, pathSelectorQuery, pretty);
            return pathSelectorQuery.GetResult();
        }

        public JsonPathQuery Select(string json, JsonPathQuery selectorQuery, bool pretty = false) {
            selectorQuery.InitSelectorResults();
            nodeStack.Clear();
            nodeStack.Add(selectorQuery.rootNode);
            targetJson.Clear();
            targetJson.AppendString(json);
            targetParser.InitParser(targetJson);
            targetParser.NextEvent();
            serializer.SetPretty(pretty);
            
            TraceTree(ref targetParser);
            if (nodeStack.Count != 0)
                throw new InvalidOperationException("Expect nodeStack.Count == 0");
            return selectorQuery;
        }

        private void AddResult(SelectorResults results) {
            switch (targetParser.Event) {
                case JsonEvent.ObjectStart:
                    serializer.InitSerializer();
                    serializer.ObjectStart();
                    serializer.WriteObject(ref targetParser);
                    var json = serializer.json.ToString();
                    results.items.Add(new SelectorResult(ResultType.Object, json));
                    return;
                case JsonEvent.ArrayStart:
                    serializer.InitSerializer();
                    serializer.ArrayStart(true);
                    serializer.WriteArray(ref targetParser);
                    json = serializer.json.ToString();
                    results.items.Add(new SelectorResult(ResultType.Array, json));
                    return;
                case JsonEvent.ValueString:
                    results.items.Add(new SelectorResult(ResultType.String, targetParser.value.ToString()));
                    return;
                case JsonEvent.ValueNumber:
                    if (targetParser.isFloat)
                        results.items.Add(new SelectorResult(targetParser.ValueAsDouble(out bool _)));
                    else
                        results.items.Add(new SelectorResult(targetParser.ValueAsLong(out bool _)));
                    return;
                case JsonEvent.ValueBool:
                    results.items.Add(new SelectorResult(targetParser.boolValue));
                    return;
                case JsonEvent.ValueNull:
                    results.items.Add(new SelectorResult(ResultType.Null, null));
                    return;
            }
        }

        private bool TraceObject(ref JsonParser p) {
            while (JsonSerializer.NextObjectMember(ref p)) {
                string key = p.key.ToString();
                var node = nodeStack[nodeStack.Count - 1];
                if (!node.children.TryGetValue(key, out PathNode<SelectorResults> path)) {
                    targetParser.SkipEvent();
                    continue;
                }
                // found node
                if (path.result != null) {
                    AddResult(path.result);
                    continue;
                }
                switch (p.Event) {
                    case JsonEvent.ArrayStart:
                        nodeStack.Add(path);
                        TraceArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        nodeStack.Add(path);
                        TraceObject(ref p);
                        break;
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ValueNull:
                        break;
                    case JsonEvent.ObjectEnd:
                    case JsonEvent.ArrayEnd:
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        throw new InvalidOperationException("WriteObject() unreachable"); // because of behaviour of ContinueObject()
                }
            }
            switch (p.Event) {
                case JsonEvent.ObjectEnd:
                    nodeStack.RemoveAt(nodeStack.Count - 1);
                    break;
                case JsonEvent.Error:
                case JsonEvent.EOF:
                    return false;
            }
            return true;
        }
        
        private bool TraceArray(ref JsonParser p) {
            int index = -1;
            while (JsonSerializer.NextArrayElement(ref p)) {
                index++;
                var node = nodeStack[nodeStack.Count - 1];
                PathNode<SelectorResults> path;
                if (node.wildcardNode != null) {
                    path = node.wildcardNode;
                } else {
                    string key = index.ToString();
                    if (!node.children.TryGetValue(key, out path)) {
                        targetParser.SkipEvent();
                        continue;
                    }
                    // found node
                }
                if (path.result != null) {
                    AddResult(path.result);
                    continue;
                }
                switch (p.Event) {
                    case JsonEvent.ArrayStart:
                        nodeStack.Add(path);
                        TraceArray(ref p);
                        break;
                    case JsonEvent.ObjectStart:
                        nodeStack.Add(path);
                        TraceObject(ref p);
                        break;
                    case JsonEvent.ValueString:
                    case JsonEvent.ValueNumber:
                    case JsonEvent.ValueBool:
                    case JsonEvent.ValueNull:
                        break;
                    case JsonEvent.ObjectEnd:
                    case JsonEvent.ArrayEnd:
                    case JsonEvent.Error:
                    case JsonEvent.EOF:
                        throw new InvalidOperationException("TraceArray() unreachable"); // because of behaviour of ContinueArray()
                }
            }
            switch (p.Event) {
                case JsonEvent.ArrayEnd:
                    nodeStack.RemoveAt(nodeStack.Count - 1);
                    break;
                case JsonEvent.Error:
                case JsonEvent.EOF:
                    return false;
            }
            return true;
        }
        
        private bool TraceTree(ref JsonParser p) {
            switch (p.Event) {
                case JsonEvent.ObjectStart:
                    return TraceObject(ref p);
                case JsonEvent.ArrayStart:
                    return TraceArray(ref p);
                case JsonEvent.ValueString:
                    return true;
                case JsonEvent.ValueNumber:
                    return true;
                case JsonEvent.ValueBool:
                    return true;
                case JsonEvent.ValueNull:
                    return true;
            }
            return false;
        }

    }


}