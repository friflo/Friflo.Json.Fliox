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
        
        private readonly    List<PathNode<SelectorPath>>    nodeStack = new List<PathNode<SelectorPath>>();
        private readonly    PathSelector                    pathSelector = new PathSelector();

        public void Dispose() {
            targetParser.Dispose();
            targetJson.Dispose();
            serializer.Dispose();
        }

        public string Select(string json, string path, bool pretty = false) {
            var pathList = new [] {path};
            var result = Select(json, pathList, pretty);
            return result[0];
        }

        public IList<string> Select(string json, IList<string> pathList, bool pretty = false) {
            pathSelector.CreateSelector(pathList);
            Select(json, pathSelector, pretty);
            return pathSelector.GetResult();
        }

        public PathSelector Select(string json, PathSelector selector, bool pretty = false) {
            selector.InitSelector();
            nodeStack.Clear();
            nodeStack.Add(selector.rootNode);
            targetJson.Clear();
            targetJson.AppendString(json);
            targetParser.InitParser(targetJson);
            targetParser.NextEvent();
            serializer.SetPretty(pretty);
            
            TraceTree(ref targetParser);
            if (nodeStack.Count != 0)
                throw new InvalidOperationException("Expect nodeStack.Count == 0");
            return selector;
        }

        private void AddResult(SelectorPath select) {
            serializer.InitSerializer();
            serializer.WriteTree(ref targetParser);
            var json = serializer.json.ToString();
            if (select.arrayResult != null) {
                if (select.itemCount > 0) {
                    select.arrayResult.Append(',');
                }
                select.arrayResult.Append(json);
                select.itemCount++;
                return;
            }
            select.jsonResult = json;
        }

        private bool TraceObject(ref JsonParser p) {
            while (JsonSerializer.NextObjectMember(ref p)) {
                string key = p.key.ToString();
                var node = nodeStack[nodeStack.Count - 1];
                if (!node.children.TryGetValue(key, out PathNode<SelectorPath> path)) {
                    targetParser.SkipEvent();
                    continue;
                }
                // found node
                if (path.selector != null) {
                    AddResult(path.selector);
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
                PathNode<SelectorPath> path;
                var wildcard = node.children["[*]"];
                if (wildcard != null) {
                    path = wildcard;
                } else {
                    string key = index.ToString();
                    if (!node.children.TryGetValue(key, out path)) {
                        targetParser.SkipEvent();
                        continue;
                    }
                    // found node
                }
                if (path.selector != null) {
                    AddResult(path.selector);
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