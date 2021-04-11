// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;

namespace Friflo.Json.Mapper.Graph
{
    public class JsonSelector : IDisposable
    {
        private             JsonSerializer                  serializer;
            
        private             Bytes                           targetJson = new Bytes(128);
        private             JsonParser                      targetParser;
        
        private readonly    List<PathNode<SelectorResult>>  nodeStack = new List<PathNode<SelectorResult>>();
        private readonly    JsonSelectorQuery               selectorQuery = new JsonSelectorQuery();

        public void Dispose() {
            targetParser.Dispose();
            targetJson.Dispose();
            serializer.Dispose();
        }

        public SelectorResult Select(string json, string path, bool pretty = false) {
            var pathList = new [] {path};
            var result = Select(json, pathList, pretty);
            return result[0];
        }

        public List<SelectorResult> Select(string json, IList<string> pathList, bool pretty = false) {
            selectorQuery.CreateNodeTree(pathList);
            Select(json, selectorQuery, pretty);
            return selectorQuery.GetResult();
        }

        public JsonSelectorQuery Select(string json, JsonSelectorQuery selectorQuery, bool pretty = false) {
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

        private void AddResult(SelectorResult result) {
            switch (targetParser.Event) {
                case JsonEvent.ObjectStart:
                    serializer.InitSerializer();
                    serializer.ObjectStart();
                    serializer.WriteObject(ref targetParser);
                    var json = serializer.json.ToString();
                    result.values.Add(new Scalar(ScalarType.Object, json));
                    return;
                case JsonEvent.ArrayStart:
                    serializer.InitSerializer();
                    serializer.ArrayStart(true);
                    serializer.WriteArray(ref targetParser);
                    json = serializer.json.ToString();
                    result.values.Add(new Scalar(ScalarType.Array, json));
                    return;
                case JsonEvent.ValueString:
                    result.values.Add(new Scalar(targetParser.value.ToString()));
                    return;
                case JsonEvent.ValueNumber:
                    if (targetParser.isFloat)
                        result.values.Add(new Scalar(targetParser.ValueAsDouble(out bool _)));
                    else
                        result.values.Add(new Scalar(targetParser.ValueAsLong(out bool _)));
                    return;
                case JsonEvent.ValueBool:
                    result.values.Add(new Scalar(targetParser.boolValue));
                    return;
                case JsonEvent.ValueNull:
                    result.values.Add(new Scalar(ScalarType.Null, null));
                    return;
            }
        }

        private bool TraceObject(ref JsonParser p) {
            while (JsonSerializer.NextObjectMember(ref p)) {
                string key = p.key.ToString();
                var node = nodeStack[nodeStack.Count - 1];
                if (!node.children.TryGetValue(key, out PathNode<SelectorResult> path)) {
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
                PathNode<SelectorResult> path;
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
