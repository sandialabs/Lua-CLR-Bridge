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

// represents an unmanaged-heap allocation
template<typename T>
private class HGlobal
{
public:
	HGlobal( int n )
		: _p(static_cast<T*>(Marshal::AllocHGlobal(sizeof T * n).ToPointer()))
	{
	}

	~HGlobal()
	{
		if (_p != NULL)
			Marshal::FreeHGlobal(IntPtr(_p));
	}

	operator T*( void ) { return _p; }

private:
	HGlobal( const HGlobal& );
	HGlobal& operator=( const HGlobal& );

public:
	HGlobal( HGlobal&& other )
		: _p(other._p)
	{
		other._p = NULL;
	}

	HGlobal& operator=( HGlobal&& other )
	{
		char* p = this->_p;
		this->_p = other._p;
		other._p = p;
		return *this;
	}

protected:
	HGlobal( T* p ) : _p(p) {}

	T* _p;
};
