// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Graph.Select;

namespace Friflo.Json.Flow.Graph
{
    public class JsonSelector : IDisposable
    {
        // private          JsonSerializer                  serializer;
            
        private             Bytes                           targetJson = new Bytes(128);
        private             JsonParser                      targetParser;
        
        private readonly    List<PathNode<SelectorResult>>  nodeStack = new List<PathNode<SelectorResult>>();
        private readonly    JsonSelect                      reusedSelect = new JsonSelect();

        public void Dispose() {
            targetParser.Dispose();
            targetJson.Dispose();
            // serializer.Dispose();
        }

        public SelectorResult Select(string json, string selector, bool pretty = false) {
            var select = new JsonSelect(new [] {selector});
            var result = Select(json, select, pretty);
            return result[0];
        }

        public List<SelectorResult> Select(string json, JsonSelect jsonSelect, bool pretty = false) {
            jsonSelect.InitSelectorResults();
            nodeStack.Clear();
            nodeStack.Add(jsonSelect.nodeTree.rootNode);
            targetJson.Clear();
            targetJson.AppendString(json);
            targetParser.InitParser(targetJson);
            targetParser.NextEvent();
            //serializer.SetPretty(pretty);
            
            TraceTree(ref targetParser);
            if (nodeStack.Count != 0)
                throw new InvalidOperationException("Expect nodeStack.Count == 0");
            
            // refill result list cause application code may mutate between Select() calls
            var results = jsonSelect.results; 
            results.Clear();
            foreach (var selector in jsonSelect.nodeTree.selectors) {
                results.Add(selector.result);
            }
            return results;
        }

        private void AddPathNodeResult(PathNode<SelectorResult> node) {
            var selectors = node.selectors;
            switch (targetParser.Event) {
                case JsonEvent.ObjectStart:
                    //serializer.InitSerializer();
                    //serializer.ObjectStart();
                    //serializer.WriteObject(ref targetParser);
                    //var json = serializer.json.ToString();
                    SelectorResult.Add(new Scalar(ScalarType.Object, "(object)"), selectors);
                    return;
                case JsonEvent.ArrayStart:
                    //serializer.InitSerializer();
                    //serializer.ArrayStart(true);
                    //serializer.WriteArray(ref targetParser);
                    //json = serializer.json.ToString();
                    SelectorResult.Add(new Scalar(ScalarType.Array, "(array)"), selectors);
                    return;
                case JsonEvent.ValueString:
                    var str = targetParser.value.ToString();
                    SelectorResult.Add(new Scalar(str), selectors);
                    return;
                case JsonEvent.ValueNumber:
                    if (targetParser.isFloat) {
                        var dbl = targetParser.ValueAsDouble(out bool _);
                        SelectorResult.Add(new Scalar(dbl), selectors);
                        return;
                    }
                    var lng = targetParser.ValueAsLong(out bool _);
                    SelectorResult.Add(new Scalar(lng), selectors);
                    return;
                case JsonEvent.ValueBool:
                    SelectorResult.Add(targetParser.boolValue ? Scalar.True : Scalar.False, selectors);
                    return;
                case JsonEvent.ValueNull:
                    SelectorResult.Add(Scalar.Null, selectors);
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
                if (path.selectors.Count > 0) {
                    AddPathNodeResult(path);
                    // continue;
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
                    path.arrayIndex = index;
                } else {
                    string key = index.ToString();
                    if (!node.children.TryGetValue(key, out path)) {
                        targetParser.SkipEvent();
                        continue;
                    }
                    // found node
                }
                if (path.selectors.Count > 0) {
                    AddPathNodeResult(path);
                    // continue;
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
