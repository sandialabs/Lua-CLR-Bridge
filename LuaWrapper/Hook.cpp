/* LuaCLRBridge
 * Copyright 2014 Sandia Corporation.
 * Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
 * the U.S. Government retains certain rights in this software.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
#include "lua.h"

#include "ldebug.h"
#include "lstate.h"

int luaW_presethook( lua_State* L, lua_Hook func )
{
	if (isLua(L->ci))
		L->oldpc = L->ci->u.l.savedpc;
	L->hook = func;
	L->basehookcount = 1;
	resethookcount(L);
	L->hookmask = cast_byte(0);
	return 1;
}

int luaW_enablehook( lua_State* L )
{
	L->hookmask = cast_byte(LUA_MASKCALL | LUA_MASKRET | LUA_MASKCOUNT);
	return 1;
}

int luaW_disablehook( lua_State* L )
{
	L->hookmask = cast_byte(0);
	return 1;
}
