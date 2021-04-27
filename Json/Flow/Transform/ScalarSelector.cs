// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Burst;
using Friflo.Json.Flow.Transform.Select;

namespace Friflo.Json.Flow.Transform
{
    public class ScalarSelector : IDisposable
    {
        private             Bytes                               targetJson = new Bytes(128);
        private             JsonParser                          targetParser;
        
        private readonly    List<PathNode<ScalarSelectResult>>  nodeStack = new List<PathNode<ScalarSelectResult>>();
        private readonly    ScalarSelect                        reusedSelect = new ScalarSelect();

        public void Dispose() {
            targetParser.Dispose();
            targetJson.Dispose();
        }

        public List<ScalarSelectResult> Select(string json, ScalarSelect scalarSelect) {
            scalarSelect.InitSelectorResults();
            nodeStack.Clear();
            nodeStack.Add(scalarSelect.nodeTree.rootNode);
            targetJson.Clear();
            targetJson.AppendString(json);
            targetParser.InitParser(targetJson);
            targetParser.NextEvent();
            
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

        private void AddPathNodeResult(PathNode<ScalarSelectResult> node) {
            var selectors = node.selectors;
            switch (targetParser.Event) {
                case JsonEvent.ObjectStart:
                    ScalarSelectResult.Add(new Scalar(ScalarType.Object, "(object)"), selectors);
                    return;
                case JsonEvent.ArrayStart:
                    ScalarSelectResult.Add(new Scalar(ScalarType.Array, "(array)"), selectors);
                    return;
                case JsonEvent.ValueString:
                    var str = targetParser.value.ToString();
                    ScalarSelectResult.Add(new Scalar(str), selectors);
                    return;
                case JsonEvent.ValueNumber:
                    if (targetParser.isFloat) {
                        var dbl = targetParser.ValueAsDouble(out bool _);
                        ScalarSelectResult.Add(new Scalar(dbl), selectors);
                        return;
                    }
                    var lng = targetParser.ValueAsLong(out bool _);
                    ScalarSelectResult.Add(new Scalar(lng), selectors);
                    return;
                case JsonEvent.ValueBool:
                    ScalarSelectResult.Add(targetParser.boolValue ? Scalar.True : Scalar.False, selectors);
                    return;
                case JsonEvent.ValueNull:
                    ScalarSelectResult.Add(Scalar.Null, selectors);
                    return;
            }
        }
        
        private bool TraceObject(ref JsonParser p) {
            while (JsonSerializer.NextObjectMember(ref p)) {
                var node = nodeStack[nodeStack.Count - 1];
                if (!node.FindByBytes(ref p.key, out PathNode<ScalarSelectResult> path)) {
                    targetParser.SkipEvent();
                    continue;
                }
                // found node
                if (path.selectors.Count > 0) {
                    AddPathNodeResult(path);
                    // continue;  <- ScalarSelector does not read JSON objects & arrays
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
                PathNode<ScalarSelectResult> path;
                if (node.wildcardNode != null) {
                    path = node.wildcardNode;
                    path.arrayIndex = index;
                } else {
                    if (!node.FindByIndex(index, out path)) {
                        targetParser.SkipEvent();
                        continue;
                    }
                    // found node
                }
                if (path.selectors.Count > 0) {
                    AddPathNodeResult(path);
                    // continue;  <- ScalarSelector does not read JSON objects & arrays
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
