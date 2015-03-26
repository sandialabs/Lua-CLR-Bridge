/* LuaCLRBridge
 * Copyright 2014 Sandia Corporation.
 * Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
 * the U.S. Government retains certain rights in this software.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
#pragma once

#include <cstdlib>

private class PinnedString
{
protected:
	System::Runtime::InteropServices::GCHandle handle;

public:
	PinnedString( array<unsigned char>^ cs )
	{
		handle = cs == nullptr ?
			System::Runtime::InteropServices::GCHandle() :
			System::Runtime::InteropServices::GCHandle::Alloc(cs, System::Runtime::InteropServices::GCHandleType::Pinned);
	}

	~PinnedString()
	{
		if (handle.IsAllocated)
			handle.Free();
	}

private:
	PinnedString( const PinnedString& );
	PinnedString& operator=( const PinnedString& );

public:
	PinnedString( PinnedString&& other )
	{
		this->handle = other.handle;
		other.handle = System::Runtime::InteropServices::GCHandle();
	}

	PinnedString& operator=( PinnedString&& other )
	{
		System::Runtime::InteropServices::GCHandle handle = this->handle;
		this->handle = other.handle;
		other.handle = handle;
		return *this;
	}

	operator char*( void )
	{
		return handle.IsAllocated ?
			reinterpret_cast<char*>(handle.AddrOfPinnedObject().ToPointer()) :
			NULL;
	}
};
