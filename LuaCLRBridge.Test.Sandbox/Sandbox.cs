/* LuaCLRBridge
 * Copyright 2014 Sandia Corporation.
 * Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
 * the U.S. Government retains certain rights in this software.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
namespace LuaCLRBridge.Test.Sandbox
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Remoting.Lifetime;
    using System.Security;
    using System.Security.Permissions;
    using System.Security.Policy;
    using System.Text;
    using Lua;
    using LuaCLRBridge;

    public sealed class Sandbox : IDisposable
    {
        public readonly AppDomain AppDomain;

        private readonly AppDomainBasedSponsor sponsor;

        public Sandbox()
            : this(new IPermission[0], new StrongName[0])
        {
            // nothing to do
        }

        public Sandbox( IPermission[] additionalPermissions, StrongName[] additionalFullTrustAssemblies )
        {
            AppDomain root = AppDomain.CurrentDomain;

            AppDomainSetup setup = new AppDomainSetup();
            setup.ApplicationBase = root.SetupInformation.ApplicationBase;

            PermissionSet permissionSet = new PermissionSet(PermissionState.None);
            permissionSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution |  // basic permission
                                                               SecurityPermissionFlag.SerializationFormatter));  // serialization
            permissionSet.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.MemberAccess |  // serialization
                                                                 ReflectionPermissionFlag.RestrictedMemberAccess));  // serialization
            permissionSet.AddPermission(new FileIOPermission(PermissionState.None) { AllFiles = FileIOPermissionAccess.PathDiscovery });  // for exception stacktrace

            foreach (var additionalPermission in additionalPermissions)
                permissionSet.AddPermission(additionalPermission);

            var strongNames = new List<StrongName>()
            {
                typeof(LuaBridge).Assembly.Evidence.GetHostEvidence<StrongName>(),
                typeof(LuaWrapper).Assembly.Evidence.GetHostEvidence<StrongName>(),
                typeof(SandboxTests).Assembly.Evidence.GetHostEvidence<StrongName>(),
            };

            strongNames.AddRange(additionalFullTrustAssemblies);

            this.AppDomain = AppDomain.CreateDomain("sandbox", null, setup, permissionSet, strongNames.ToArray());

            this.sponsor = CreateSponsor();
        }

        ~Sandbox()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private bool _disposed = false;

        private void Dispose( bool disposeManaged )
        {
            if (_disposed)
                return;

            if (disposeManaged)
            {
                AppDomain.Unload(this.AppDomain);
            }

            _disposed = true;
        }

        [SecuritySafeCritical]
        private AppDomainBasedSponsor CreateSponsor()
        {
            // any MarshalByRefObjects that go across the sandbox boundary (either direction) need a sponsor
            var sponsorHandle = Activator.CreateInstanceFrom(
                this.AppDomain,
                typeof(AppDomainBasedSponsor).Assembly.ManifestModule.FullyQualifiedName,
                typeof(AppDomainBasedSponsor).FullName,
                false, 0, null,
                new object[] { this.AppDomain },
                null, null);
            var sponsor = sponsorHandle.Unwrap() as AppDomainBasedSponsor;

            return sponsor;
        }

        [SecuritySafeCritical]
        public LuaBridge CreateLuaBridge( string clrBridge = null, Encoding encoding = null )
        {
            var luaHandle = Activator.CreateInstanceFrom(
                this.AppDomain,
                typeof(LuaBridge).Assembly.ManifestModule.FullyQualifiedName,
                typeof(LuaBridge).FullName,
                false, 0, null,
                new object[] { clrBridge, encoding },
                null, null);
            var lua = luaHandle.Unwrap() as LuaBridge;
            sponsor.Sponsor(lua);

            return lua;
        }

        [SecuritySafeCritical]
        public InstrumentedLuaBridge CreateInstrumentedLuaBridge( Instrumentations instrumentations, string clrBridge = null, Encoding encoding = null )
        {
            var luaHandle = Activator.CreateInstanceFrom(
                this.AppDomain,
                typeof(InstrumentedLuaBridge).Assembly.ManifestModule.FullyQualifiedName,
                typeof(InstrumentedLuaBridge).FullName,
                false, 0, null,
                new object[] { instrumentations, clrBridge, encoding },
                null, null);
            var lua = luaHandle.Unwrap() as InstrumentedLuaBridge;
            sponsor.Sponsor(lua);

            return lua;
        }

        public void Sponsor( MarshalByRefObject @object )
        {
            sponsor.Sponsor(@object);
        }

        private class AppDomainBasedSponsor : MarshalByRefObject
        {
            private readonly ClientSponsor sponsor = new ClientSponsor();

            [SecuritySafeCritical]
            public AppDomainBasedSponsor( AppDomain appDomain )
            {
                appDomain.DomainUnload += this.OnAppDomainUnload;
            }

            [SecurityCritical]
            public override object InitializeLifetimeService()
            {
                // live forever
                return null;
            }

            [SecuritySafeCritical]
            [SecurityPermission(SecurityAction.Assert, RemotingConfiguration = true)]
            public void Sponsor( MarshalByRefObject @object )
            {
                if (@object == null)
                    throw new ArgumentNullException("object");

                sponsor.Register(@object);
            }

            [SecuritySafeCritical]
            [SecurityPermission(SecurityAction.Assert, RemotingConfiguration = true)]
            public void OnAppDomainUnload( object sender, EventArgs eventArgs )
            {
                sponsor.Close();
            }
        }
    }
}
