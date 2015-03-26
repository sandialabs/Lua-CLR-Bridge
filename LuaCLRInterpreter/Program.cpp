/* LuaCLRBridge
 * Copyright 2014 Sandia Corporation.
 * Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
 * the U.S. Government retains certain rights in this software.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
using namespace System;
using namespace System::Reflection;
using namespace System::Runtime::InteropServices;
using namespace Lua;
using namespace LuaCLRBridge;

#include <msclr/auto_handle.h>

#include "lstate.h"

int main( array<System::String^>^ args )
{
	msclr::auto_handle<LuaBridge> lua = gcnew LuaBridge(nullptr, nullptr);
	
	int argc = args->Length + 1;
	
	char** argv = new char*[argc + 1];
	argv[0] = reinterpret_cast<char *>(Marshal::StringToHGlobalAnsi(Assembly::GetExecutingAssembly()->ManifestModule->Name).ToPointer());
	for (int i = 0; i < args->Length; ++i)
		argv[i + 1] = reinterpret_cast<char *>(Marshal::StringToHGlobalAnsi(args[i]).ToPointer());
	argv[argc] = nullptr;
	
	try
	{
		lua.get()->NewFunction(LuaWrapper::pmain)->Call(argc, IntPtr(argv));
	}
	catch (Exception^ ex)
	{
		Console::Error->WriteLine(ex->ToString());
		Console::ReadKey();
	}
	
	return 0;
}
