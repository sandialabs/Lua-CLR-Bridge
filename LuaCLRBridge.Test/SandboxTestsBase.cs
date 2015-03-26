/* LuaCLRBridge
 * Copyright 2014 Sandia Corporation.
 * Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
 * the U.S. Government retains certain rights in this software.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
namespace LuaCLRBridge.Test
{
    using LuaCLRBridge;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public abstract class SandboxTestsBase
    {
#if NO_SANDBOX
        protected static LuaBridge CreateLuaBridge( bool registerBridge = true, System.Text.Encoding encoding = null )
        {
            return new LuaBridge(registerBridge, encoding);
        }

        protected InstrumentedLuaBridge CreateInstrumentedLuaBridge( Instrumentations instrumentations, bool registerBridge = true, System.Text.Encoding encoding = null )
        {
            return new InstrumentedLuaBridge(instrumentations, registerBridge, encoding);
        }
#else
        private static LuaCLRBridge.Test.Sandbox.Sandbox sandbox = new LuaCLRBridge.Test.Sandbox.Sandbox();

        protected static LuaBridge CreateLuaBridge( string clrBridge = null, System.Text.Encoding encoding = null )
        {
            return sandbox.CreateLuaBridge(clrBridge, encoding);
        }

        protected InstrumentedLuaBridge CreateInstrumentedLuaBridge( Instrumentations instrumentations, string clrBridge = null, System.Text.Encoding encoding = null )
        {
            return sandbox.CreateInstrumentedLuaBridge(instrumentations, clrBridge, encoding);
        }
#endif
    }
}
