// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
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
        internal                string              Label => impl.label;
        internal                DataChannelState    ReadyState => GetReadyState();
        
        private     Action          onOpen;
        internal    Action          OnOpen      { get => onOpen; set => SetOnOpen (value); }
        
        private     Action          onClose;
        internal    Action          OnClose     { get => onClose; set => SetOnClose(value); }
        
        private     Action<byte[]>  onMessage;
        internal    Action<byte[]>  OnMessage   { get => onMessage; set => SetOnMessage(value); }
        
        private     Action<string>  onError;
        internal    Action<string>  OnError    { get => onError; set => SetOnError(value); }
        
        private void SetOnOpen(Action action) {
            onOpen         = action;
            impl.onopen   += () => {
                action();
            };
        }
        
        private void SetOnClose(Action action) {
            onClose         = action;
            impl.onclose   += () => {
                action();
            };
        }
        
        private void SetOnMessage(Action<byte[]> action) {
            onMessage         = action;
            impl.onmessage   += (dc, protocol, data) => {
                action(data);
            };
        }
        
        private void SetOnError(Action<string> action) {
            onError         = action;
            impl.onerror   += error => {
                action(error);
            };
        }

        internal DataChannel (RTCDataChannel impl) {
            this.impl = impl;
        }
        
        internal void Close() {
            impl.close();
        }
        
        internal void Send(byte[] data, int offset, int count) {
            var array = new byte[count];
            Buffer.BlockCopy(data, offset, array, 0, count);
            impl.send(array);  // requires an individual byte[] :(
        }
        
        private DataChannelState GetReadyState() {
            switch (impl.readyState) {
                case RTCDataChannelState.connecting:    return DataChannelState.connecting; 
                case RTCDataChannelState.open:          return DataChannelState.open; 
                case RTCDataChannelState.closing:       return DataChannelState.closing; 
                case RTCDataChannelState.closed:        return DataChannelState.closed; 
                default: throw new InvalidOperationException($"unexpected state: {impl.readyState}");
            }
        }
    }
}

#endif