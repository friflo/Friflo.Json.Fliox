// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if !UNITY_5_3_OR_NEWER

using System;
using SIPSorcery.Net;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC.Impl
{
    internal sealed class DataChannel
    {
        private     readonly    RTCDataChannel      impl;
        internal    event       Action              OnOpen;
        internal    event       Action              OnClose;
        internal    event       Action<string>      OnError;
        internal    event       Action<byte[]>      OnMessage;
        internal                string              Label => impl.label;
        
        internal                DataChannelState    ReadyState { get {
            switch (impl.readyState) {
                case RTCDataChannelState.connecting:    return DataChannelState.connecting; 
                case RTCDataChannelState.open:          return DataChannelState.open; 
                case RTCDataChannelState.closing:       return DataChannelState.closing; 
                case RTCDataChannelState.closed:        return DataChannelState.closed; 
                default: throw new InvalidOperationException($"unexpected state: {impl.readyState}");
            }
        } }

        internal DataChannel (RTCDataChannel impl) {
            this.impl = impl;
            impl.onopen += () => {
                OnOpen?.Invoke();  
            };
            impl.onclose += () => {
                OnClose?.Invoke();  
            };
            impl.onerror += error => {
                OnError?.Invoke(error);
            };
            impl.onmessage += (dc, protocol, data) => {
                OnMessage?.Invoke(data);  
            };
        }
        
        internal void Close() {
            impl.close();
        }
        
        internal void Send(byte[] data) {
            impl.send(data);
        }
    }
}

#endif