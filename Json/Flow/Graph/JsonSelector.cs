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
        private             JsonSerializer                  serializer;
            
        private             Bytes                           targetJson = new Bytes(128);
        private             JsonParser                      targetParser;
        
        private readonly    List<PathNode<JsonResult>>      nodeStack = new List<PathNode<JsonResult>>();
        private readonly    JsonSelect                      reusedSelect = new JsonSelect();

        public void Dispose() {
            targetParser.Dispose();
            targetJson.Dispose();
            serializer.Dispose();
        }

        public List<JsonResult> Select(string json, JsonSelect scalarSelect, bool pretty = false) {
            scalarSelect.InitSelectorResults();
            nodeStack.Clear();
            nodeStack.Add(scalarSelect.nodeTree.rootNode);
            targetJson.Clear();
            targetJson.AppendString(json);
            targetParser.InitParser(targetJson);
            targetParser.NextEvent();
            serializer.SetPretty(pretty);
            
            TraceTree(ref targetParser);
            if (nodeStack.Count != 0)
                throw new InvalidOperationException("Expect nodeStack.Count == 0");
            
            // refill result list cause application code may mutate between Select() calls
            var results = scalarSelect.results; 
            results.Clear();
            foreach (var selector in scalarSelect.nodeTree.selectors) {
                results.Add(selector.result);
            }
            return results;
        }

        private void AddPathNodeResult(PathNode<JsonResult> node) {
            var selectors = node.selectors;
            switch (targetParser.Event) {
                case JsonEvent.ObjectStart:
                    serializer.InitSerializer();
                    serializer.ObjectStart();
                    serializer.WriteObject(ref targetParser);
                    var json = serializer.json.ToString();
                    JsonResult.Add(json, selectors);
                    return;
                case JsonEvent.ArrayStart:
                    serializer.InitSerializer();
                    serializer.ArrayStart(true);
                    serializer.WriteArray(ref targetParser);
                    json = serializer.json.ToString();
                    JsonResult.Add(json, selectors);
                    return;
                case JsonEvent.ValueString:
                    json = targetParser.value.ToString();
                    JsonResult.Add(json, selectors);
                    return;
                case JsonEvent.ValueNumber:
                    json = targetParser.value.ToString();
                    JsonResult.Add(json, selectors);
                    return;
                case JsonEvent.ValueBool:
                    JsonResult.Add(targetParser.boolValue ? "true" : "false", selectors);
                    return;
                case JsonEvent.ValueNull:
                    JsonResult.Add("null", selectors);
                    return;
            }
        }
        
        private bool TraceObject(ref JsonParser p) {
            while (JsonSerializer.NextObjectMember(ref p)) {
                string key = p.key.ToString();
                var node = nodeStack[nodeStack.Count - 1];
                if (!node.children.TryGetValue(key, out PathNode<JsonResult> path)) {
                    targetParser.SkipEvent();
                    continue;
                }
                // found node
                if (path.selectors.Count > 0) {
                    AddPathNodeResult(path);
                    continue;  // <- JsonSelector read JSON objects & arrays
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
                PathNode<JsonResult> path;
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
                    continue;  // <- JsonSelector read JSON objects & arrays
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
