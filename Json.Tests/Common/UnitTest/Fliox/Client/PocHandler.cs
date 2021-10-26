// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.DB.Host;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client
{
    public class PocHandler : DatabaseHandler {
        public PocHandler() {
            AddCommandHandler<TestCommand, bool>(TestCommand); // todo add handler via scanning DatabaseHandler
        }
        
        private static bool TestCommand(Command<TestCommand> command) {
            return true;
        }
    }
}