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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;

    #if DEBUG
    #error Benchmarking Debug is not sensible.
    #endif

    class Program
    {
        static void Main( string[] args )
        {
            var process = Process.GetCurrentProcess();
            process.ProcessorAffinity = (process.ProcessorAffinity.ToInt64() & 2) == 0 ? new IntPtr(1) : new IntPtr(2);

            var noOp = RunBenchmark(new TimeSpan(0, 0, seconds: 1), 1, () => { });

            Console.WriteLine("NoOp:  {0} ticks/iter", noOp.CpuTimespan.Ticks / noOp.Iterations);

            foreach (var type in Assembly.GetEntryAssembly().GetExportedTypes())
            {
                foreach (var typeAttribute in type.GetCustomAttributes(typeof(BenchmarkClass), false))
                {
                    var classInitializers = new Dictionary<MethodInfo, ClassInitializeAttribute>();
                    var classCleanups = new Dictionary<MethodInfo, ClassCleanupAttribute>();
                    var benchmarkMethods = new Dictionary<MethodInfo, BenchmarkMethodAttribute>();

                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                    {
                        foreach (var attribute in method.GetCustomAttributes(typeof(ClassInitializeAttribute), false))
                            classInitializers.Add(method, attribute as ClassInitializeAttribute);

                        foreach (var attribute in method.GetCustomAttributes(typeof(ClassCleanupAttribute), false))
                            classCleanups.Add(method, attribute as ClassCleanupAttribute);

                        foreach (var attribute in method.GetCustomAttributes(typeof(BenchmarkMethodAttribute), false))
                            benchmarkMethods.Add(method, attribute as BenchmarkMethodAttribute);
                    }

                    var instance = Activator.CreateInstance(type);

                    foreach (var classInitializer in classInitializers)
                    {
                        var method = classInitializer.Key;
                        var attribute = classInitializer.Value;

                        try
                        {
                            method.Invoke(instance, null);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("{0} {1} threw unhandled exception:{2}{3}", type.FullName, method.Name, Environment.NewLine, ex);
                        }
                    }

                    foreach (var benchmarkMethod in benchmarkMethods)
                    {
                        var method = benchmarkMethod.Key;
                        var attribute = benchmarkMethod.Value;

                        var action = Delegate.CreateDelegate(typeof(Action), instance, method) as Action;

                        try
                        {
                            var result = RunBenchmark(attribute.RunFor, attribute.IterationsPerCall, action);

                            Console.WriteLine("{0}:", method.Name);
                            Console.WriteLine("\t{0} CPU  ticks/iter  ({1} / {2} iterations)", result.CpuTimespan.Ticks / result.Iterations, result.CpuTimespan, result.Iterations);
                            Console.WriteLine("\t{0} real ticks/iter  ({1} / {2} iterations)", result.RealTimespan.Ticks / result.Iterations, result.RealTimespan, result.Iterations);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("{0} {1} threw unhandled exception:{2}{3}", type.FullName, method.Name, Environment.NewLine, ex);
                        }
                    }

                    foreach (var classCleanup in classCleanups)
                    {
                        var method = classCleanup.Key;
                        var attribute = classCleanup.Value;

                        try
                        {
                            method.Invoke(instance, null);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("{0} {1} threw unhandled exception:{2}{3}", type.FullName, method.Name, Environment.NewLine, ex);
                        }
                    }
                }
            }

            Console.WriteLine("Press a key...");
            Console.ReadKey();
        }

        public struct Result
        {
            public TimeSpan CpuTimespan { get; set; }
            public TimeSpan RealTimespan { get; set; }
            public int Iterations { get; set; }
        }

        static Result RunBenchmark( TimeSpan minTimespan, int iterationPerCall, Action action )
        {
            // clean up
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Stopwatch watch;
            int calls;

            // warm up
            action();

            AppDomain.MonitoringIsEnabled = true;
            var appDomain = AppDomain.CurrentDomain;
            var startCpuTime = appDomain.MonitoringTotalProcessorTime;

            var end = DateTime.UtcNow + minTimespan;

            watch = Stopwatch.StartNew();

            for (calls = 0; DateTime.UtcNow < end; ++calls)
                action();

            watch.Stop();

            var endCpuTime = appDomain.MonitoringTotalProcessorTime;

            return new Result()
            {
                CpuTimespan = endCpuTime - startCpuTime,
                RealTimespan = watch.Elapsed,
                Iterations = calls * iterationPerCall,
            };
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class BenchmarkClass : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ClassInitializeAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ClassCleanupAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class BenchmarkMethodAttribute : Attribute
    {
        public BenchmarkMethodAttribute( int secondsToRun )
        {
            this.RunFor = new TimeSpan(0, 0, seconds: secondsToRun);
            this.IterationsPerCall = 1;
        }

        public TimeSpan RunFor { get; private set; }
        public int IterationsPerCall { get; set; }
    }
}
