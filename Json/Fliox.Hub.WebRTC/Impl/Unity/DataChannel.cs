// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

#if UNITY_5_3_OR_NEWER

using System;
using Unity.Collections;
using Unity.WebRTC;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.WebRTC.Impl
{
    internal sealed class DataChannel
    {
        private     readonly    RTCDataChannel      impl;
        internal                string              Label => impl.Label;
        internal                DataChannelState    ReadyState => GetReadyState();
        
        private     Action          onOpen;
        internal    Action          OnOpen      { get => onOpen; set => SetOnOpen (value); }
        
        private     Action          onClose;
        internal    Action          OnClose     { get => onClose; set => SetOnClose(value); }
        
        private     Action<byte[]>  onMessage;
        internal    Action<byte[]>  OnMessage   { get => onMessage; set => SetOnMessage(value); }
        
        internal    Action<string>  OnError;
        
        private void SetOnOpen(Action action) {
            onOpen         = action;
            impl.OnOpen    = () => {
                action();
            };
        }
        
        private void SetOnClose(Action action) {
            onClose         = action;
            impl.OnClose    = () => {
                action();
            };
        }
        
        private void SetOnMessage(Action<byte[]> action) {
            onMessage         = action;
            impl.OnMessage    = bytes => {
                action(bytes);
            };
        }

        internal DataChannel (RTCDataChannel impl) {
            this.impl = impl;
        }
        
        internal void Close() {
            impl.Close();
        }
        
        
        private readonly    object              sendBufferLock  = new object();
        private             NativeArray<byte>   sendBuffer      = new NativeArray<byte>(64, Allocator.Persistent);
            
        internal void Send(byte[] data, int offset, int count) {
            lock(sendBufferLock) {
                if (sendBuffer.Length < count) {
                    sendBuffer.Dispose();
                    sendBuffer = new NativeArray<byte>(count + 32, Allocator.Persistent, NativeArrayOptions.UninitializedMemory); // + 32 to avoid many reallocation
                }
                NativeArray<byte>.Copy(data, offset, sendBuffer, 0, count);
                var slice = sendBuffer.Slice(0, count);
                impl.Send(slice);
            }
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