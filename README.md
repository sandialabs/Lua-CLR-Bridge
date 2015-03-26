# Lua-CLR Bridge #

Lua-CLR Bridge is a bridge between Lua and the .NET runtime (CLR).

## Features ##

 * Manipulate .NET objects from Lua code.
 * Manipulate Lua values from .NET (ex. C#) code.
 * Can run in a permission-restricted `AppDomain`-based sandbox.

## Getting Started ##

Lua-CLR Bridge provides mechanisms to create and manipulate a Lua state.

### Hello, world! ###

Let's fire up Lua in an application to run a one-line Lua script:

~~~.cs
using LuaCLRBridge;

class Program
{
    static void Main( string[] args )
    {
        using (var lua = new LuaBridge())
        {
            lua.LoadLib("_G");  // for "basic functions"
            lua.Do("print 'Hello, world!'");
        }
    }
}
~~~

### Manipulating .NET Objects ###

How about something a bit more substantial that demonstrates manipulating CLR objects from Lua:

~~~.cs
using System;
using System.Collections.Generic;
using LuaCLRBridge;

// The CLR type that the Lua code will be manipulating.
public class TimeyWimeyDetector
{
    public TimeyWimeyDetector()
    {
        Handlers = new Dictionary<Stuff, Handler>();
    }

    public Dictionary<Stuff, Handler> Handlers { get; private set; }

    public void Process( Stuff stuff, float distancePaces )
    {
        var handler = Handlers[stuff];
        if (handler != null)
            handler(distancePaces);
    }

    public delegate void Handler( float distancePaces );
    public enum Stuff { TemporalAnomaly, Egg, }
    public class BoiledEggException : Exception { }
}

class Program
{
    static void Main( string[] args )
    {
        using (var lua = new LuaBridge())
        {
            lua.LoadLib("_G");
            lua["detector"] = new TimeyWimeyDetector();
            try
            {
                lua.Do(@"local Stuff = CLR.Static.TimeyWimeyDetector.Stuff
                         local BoiledEggException =
                             CLR.Static.TimeyWimeyDetector.BoiledEggException
                         detector.Handlers.Item[Stuff.TemporalAnomaly] = 
                             function( ) print 'Ding!' end
                         detector.Handlers.Item[Stuff.Egg] = 
                             function( distPaces ) if distPaces <= 30 then
                                 error(BoiledEggException()) end end
                         detector.Process(Stuff.Egg, 40)
                         detector.Process(Stuff.TemporalAnomaly, 13)
                         detector.Process(Stuff.Egg, 27)", "example.lua");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
~~~

Let's look at the Lua code passed to `Do`.  Lines 1-3 set up aliases for a couple of CLR types.  Lines 4-5 create a CLR delegate from a Lua function and stores it in a `Dictionary` using an enumeration value as the key.  Lines 6-8 show the same but this time constructing and throwing a CLR exception using the Lua error mechanism.  Lines  9-11 perform calls to a CLR instance method that, in turn, calls the delegates that were previously created.

And the output:

    Ding!
    TimeyWimeyDetector+BoiledEggException: Exception of type 'TimeyWimeyDetector+BoiledEggException' was thrown.
            [C]: in function 'error'
            [string "example.lua"]:8: in function <[string "example.lua"]:7>
       at LuaCLRBridge.LuaFunctionBase.Call(ObjectTranslator objectTranslator, IntPtr L, Int32 retCount, Object[] args)
       at LuaCLRBridge.LuaFunctionBase.CallExpectingResults(Int32 resultCount, Object[] args)
       at <>LuaBridge_Handler(Closure , Single )
       at TimeyWimeyDetector.Process(Stuff stuff, Single distancePaces)
            [C]: in function 'Process'
            [string "example.lua"]:11: in main chunk
       at LuaCLRBridge.LuaFunctionBase.Call(ObjectTranslator objectTranslator, IntPtr L, Int32 retCount, Object[] args)
       at LuaCLRBridge.LuaFunctionBase.Call(LuaBridgeBase bridge, Object[] args)
       at LuaCLRBridge.LuaBridgeBase.Do(String buff, String name)
       at Program.Main(String[] args)

Tada!  It blew up (intentionally) with a long but reasonably useful stack trace with interlaced CLR and Lua stack frames.

Read the [Using Lua-CLR Bridge from within Lua](UsingLuaCLRBridgeFromWithinLua.md) document for a full explanation of how Lua programs can manipulate .NET objects.

## Manipulating Lua objects from .NET ##

Read the auto-generated Lua-CLR Bridge documentation to get an idea of what kind of interface Lua-CLR Bridge provides — start with `LuaCLRBridge.LuaBridge`.

If you are familiar with the [Lua 5.2 Reference Manual](http://www.lua.org/manual/5.2/manual.html), you will notice that Lua-CLR Bridge doesn't provide the same low-level stack manipulators that Lua does, but it does provide wrappers around most-if-not-all of what you will want to do.
