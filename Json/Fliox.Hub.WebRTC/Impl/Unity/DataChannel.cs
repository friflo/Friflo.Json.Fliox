// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_5_3_OR_NEWER

using System;
using Unity.WebRTC;

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
        internal                string              Label => impl.Label;
        internal                DataChannelState    ReadyState => GetReadyState();

        internal DataChannel (RTCDataChannel impl) {
            this.impl = impl;
            impl.OnOpen += () => {
                OnOpen?.Invoke();  
            };
            impl.OnClose += () => {
                OnClose?.Invoke();  
            };
            /* impl.onerror += error => {
                OnError?.Invoke(error);
            }; */
            impl.OnMessage += bytes => {
                OnMessage?.Invoke(bytes);  
            };
        }
        
        internal void Close() {
            impl.Close();
        }
        
        internal void Send(byte[] data) {
            impl.Send(data);
        }
        
        private DataChannelState GetReadyState() {
            switch (impl.ReadyState) {
                case RTCDataChannelState.Connecting:    return DataChannelState.connecting; 
                case RTCDataChannelState.Open:          return DataChannelState.open; 
                case RTCDataChannelState.Closing:       return DataChannelState.closing; 
                case RTCDataChannelState.Closed:        return DataChannelState.closed; 
                default: throw new InvalidOperationException($"unexpected state: {impl.ReadyState}");
            }
        }
    }
}

#endif