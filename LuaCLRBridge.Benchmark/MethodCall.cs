/* LuaCLRBridge
 * Copyright 2014 Sandia Corporation.
 * Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
 * the U.S. Government retains certain rights in this software.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
namespace LuaCLRBridge.Benchmark
{
    using System;

    [BenchmarkClass]
    public class MethodCall
    {
        private LuaBridge lua;

        private LuaFunction o_f1_p1;
        private LuaFunction o_f2_p1;
        private LuaFunction o_f3_p1;

        [ClassInitialize]
        public void Initialize()
        {
            lua = new LuaBridge();

            lua["o"] = new MethodsClass();
            lua["p1"] = 1;

            o_f1_p1 = lua.Load("for i = 0, 10 do o.f1(p1) end");
            o_f2_p1 = lua.Load("for i = 0, 10 do o.f2(p1) end");
            o_f3_p1 = lua.Load("for i = 0, 10 do o.f3(p1) end");
        }

        [ClassCleanup]
        public void Cleanup()
        {
            lua.Dispose();
        }

        class MethodsClass
        {
            public void f1( int i ) { }

            public void f2( int i ) { }
            public void f2( object o ) { }

            public void f3( int i ) { }
            public void f3( object o ) { }
            public void f3( Action a ) { }
            public void f3( Delegate d ) { }
            public void f3( Enum e ) { }
            public void f3( Func<int> f ) { }
            public void f3( Func<object> f ) { }
        }

        [BenchmarkMethod(secondsToRun: 3, IterationsPerCall = 10)]
        public void CallMethod()
        {
            o_f1_p1.Call();
        }

        [BenchmarkMethod(secondsToRun: 3, IterationsPerCall = 10)]
        public void CallOverloadedMethod()
        {
            o_f2_p1.Call();
        }

        [BenchmarkMethod(secondsToRun: 3, IterationsPerCall = 10)]
        public void CallVeryOverloadedMethod()
        {
            o_f3_p1.Call();
        }
    }
}
