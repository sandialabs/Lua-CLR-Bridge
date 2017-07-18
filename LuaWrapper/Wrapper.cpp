/* LuaCLRBridge
 * Copyright 2014 Sandia Corporation.
 * Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
 * the U.S. Government retains certain rights in this software.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
#include "HGlobal.hpp"
#include "Hook.hpp"
#include "StackTrace.hpp"
#include "PinnedString.hpp"

#include "lua.h"
#include "lualib.h"
#include "lauxlib.h"

#include <cassert>
#include <cstdlib>
#include <cstring>

using namespace System;
using namespace System::Runtime::InteropServices;
using namespace System::Text;

// #define INCLUDE_UNIMPLEMENTED

extern int pmain( lua_State* L );

namespace Lua
{
	// helper for converting a CLR string to a C string
	static PinnedString toCString( String^ s, Encoding^ e )
	{
		if (s == nullptr)
			return PinnedString(nullptr);

		array<unsigned char>^ cs = e->GetBytes(s);
		Array::Resize(cs, cs->Length + 1);
		return PinnedString(cs);
	}

	// helper for converting a CLR string to a fixed-length string
	static inline array<unsigned char>^ toBytes( String^ s, Encoding^ e )
	{
		return s == nullptr ?
			nullptr :
			e->GetBytes(s);
	}

	// helper for converting a fixed-length string to a CLR string
	static inline String^ toCLRString( const char* s, int length, Encoding^ e )
	{
		return s == NULL ?
			nullptr :
			gcnew String(s, 0, length, e);
	}

	// helper for converting a C string to a CLR string
	static inline String^ toCLRString( const char* s, Encoding^ e )
	{
		return s == NULL ?
			nullptr :
			gcnew String(s, 0, strlen(s), e);
	}

#define UNMACRO(T, M) static T const (M##_) = (M);

	/*
	** lua.h
	*/

	UNMACRO(const char*, LUA_VERSION_MAJOR)
	UNMACRO(const char*, LUA_VERSION_MINOR)
	UNMACRO(unsigned int, LUA_VERSION_NUM)
	UNMACRO(const char*, LUA_VERSION_RELEASE)
	UNMACRO(const char*, LUA_VERSION)
	UNMACRO(const char*, LUA_RELEASE)
	UNMACRO(const char*, LUA_COPYRIGHT)
	UNMACRO(const char*, LUA_AUTHORS)
#undef LUA_VERSION_MAJOR
#undef LUA_VERSION_MINOR
#undef LUA_VERSION_NUM
#undef LUA_VERSION_RELEASE
#undef LUA_VERSION
#undef LUA_RELEASE
#undef LUA_COPYRIGHT
#undef LUA_AUTHORS

	UNMACRO(const char*, LUA_SIGNATURE)
#undef LUA_SIGNATURE

	UNMACRO(int, LUA_MULTRET)
#undef LUA_MULTRET

	UNMACRO(int, LUA_REGISTRYINDEX)
#undef LUA_REGISTRYINDEX

#pragma region LuaStatus_Unmacroing
	UNMACRO(int, LUA_OK)
	UNMACRO(int, LUA_YIELD)
	UNMACRO(int, LUA_ERRRUN)
	UNMACRO(int, LUA_ERRSYNTAX)
	UNMACRO(int, LUA_ERRMEM)
	UNMACRO(int, LUA_ERRGCMM)
	UNMACRO(int, LUA_ERRERR)
#undef LUA_OK
#undef LUA_YIELD
#undef LUA_ERRRUN
#undef LUA_ERRSYNTAX
#undef LUA_ERRMEM
#undef LUA_ERRGCMM
#undef LUA_ERRERR
#pragma endregion

	/* thread status */
	public enum class LuaStatus
	{
		LUA_OK = LUA_OK_,
		LUA_YIELD = LUA_YIELD_,
		LUA_ERRRUN = LUA_ERRRUN_,
		LUA_ERRSYNTAX = LUA_ERRSYNTAX_,
		LUA_ERRMEM = LUA_ERRMEM_,
		LUA_ERRGCMM = LUA_ERRGCMM_,
		LUA_ERRERR = LUA_ERRERR_,
	};

	typedef IntPtr LuaStatePtr;

	/*
	** Delegate for functions passed to Lua as function pointers
	*/
	[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
	public delegate int LuaCFunction( LuaStatePtr luaState );
	typedef IntPtr LuaCFunctionPtr;

	/*
	** Delegate for functions that read/write blocks when loading/dumping Lua chunks
	*/
	[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
	public delegate const char* LuaReader( lua_State* L, void* ud, size_t* sz );
	typedef IntPtr LuaReaderPtr;

	[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
	public delegate int LuaWriter( lua_State* L, const void* p, size_t sz, void* ud );
	typedef IntPtr LuaWriterPtr;

	/*
	** Delegate for memory-allocation functions
	*/
	[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
	public delegate void* LuaAlloc( void* ud, void* ptr, size_t osize, size_t nsize );
	typedef IntPtr LuaAllocPtr;

#pragma region LuaType_Unmacroing
	UNMACRO(int, LUA_TNONE)
	UNMACRO(int, LUA_TNIL)
	UNMACRO(int, LUA_TBOOLEAN)
	UNMACRO(int, LUA_TLIGHTUSERDATA)
	UNMACRO(int, LUA_TNUMBER)
	UNMACRO(int, LUA_TSTRING)
	UNMACRO(int, LUA_TTABLE)
	UNMACRO(int, LUA_TFUNCTION)
	UNMACRO(int, LUA_TUSERDATA)
	UNMACRO(int, LUA_TTHREAD)
#undef LUA_TNONE
#undef LUA_TNIL
#undef LUA_TBOOLEAN
#undef LUA_TLIGHTUSERDATA
#undef LUA_TNUMBER
#undef LUA_TSTRING
#undef LUA_TTABLE
#undef LUA_TFUNCTION
#undef LUA_TUSERDATA
#undef LUA_TTHREAD
#pragma endregion

	/*
	** basic types
	*/
	public enum class LuaType
	{
		LUA_TNONE = LUA_TNONE_,
		LUA_TNIL = LUA_TNIL_,
		LUA_TBOOLEAN = LUA_TBOOLEAN_,
		LUA_TLIGHTUSERDATA = LUA_TLIGHTUSERDATA_,
		LUA_TNUMBER = LUA_TNUMBER_,
		LUA_TSTRING = LUA_TSTRING_,
		LUA_TTABLE = LUA_TTABLE_,
		LUA_TFUNCTION = LUA_TFUNCTION_,
		LUA_TUSERDATA = LUA_TUSERDATA_,
		LUA_TTHREAD = LUA_TTHREAD_,
	};

	UNMACRO(int, LUA_NUMTAGS)
#undef LUA_NUMTAGS

	/* minimum Lua stack available to a C function */
	UNMACRO(int, LUA_MINSTACK)
#undef LUA_MINSTACK

	/* predefined values in the registry */
	UNMACRO(int, LUA_RIDX_LAST)
#undef LUA_RIDX_LAST
	UNMACRO(int, LUA_RIDX_MAINTHREAD)
#undef LUA_RIDX_MAINTHREAD
	UNMACRO(int, LUA_RIDX_GLOBALS)
#undef LUA_RIDX_GLOBALS

	/* type of numbers in Lua */
	typedef ::lua_Number lua_Number;

	/* type for integer functions */
	typedef LUA_INTEGER lua_Integer;

	/* unsigned integer type */
	typedef LUA_UNSIGNED lua_Unsigned;

#pragma region LuaArithOp_Unmacroing
	UNMACRO(int, LUA_OPADD)
	UNMACRO(int, LUA_OPSUB)
	UNMACRO(int, LUA_OPMUL)
	UNMACRO(int, LUA_OPDIV)
	UNMACRO(int, LUA_OPMOD)
	UNMACRO(int, LUA_OPPOW)
	UNMACRO(int, LUA_OPUNM)
#undef LUA_OPADD
#undef LUA_OPSUB
#undef LUA_OPMUL
#undef LUA_OPDIV
#undef LUA_OPMOD
#undef LUA_OPPOW
#undef LUA_OPUNM
#pragma endregion

	public enum class LuaArithOp
	{
		LUA_OPADD = LUA_OPADD_,
		LUA_OPSUB = LUA_OPSUB_,
		LUA_OPMUL = LUA_OPMUL_,
		LUA_OPDIV = LUA_OPDIV_,
		LUA_OPMOD = LUA_OPMOD_,
		LUA_OPPOW = LUA_OPPOW_,
		LUA_OPUNM = LUA_OPUNM_,
	};

#pragma region LuaCompareOp_Unmacroing
	UNMACRO(int, LUA_OPEQ)
	UNMACRO(int, LUA_OPLT)
	UNMACRO(int, LUA_OPLE)
#undef LUA_OPEQ
#undef LUA_OPLT
#undef LUA_OPLE
#pragma endregion

	public enum class LuaCompareOp
	{
		LUA_OPEQ = LUA_OPEQ_,
		LUA_OPLT = LUA_OPLT_,
		LUA_OPLE = LUA_OPLE_,
	};

#pragma region LuaGCOption_Unmacroing
	UNMACRO(int, LUA_GCSTOP)
	UNMACRO(int, LUA_GCRESTART)
	UNMACRO(int, LUA_GCCOLLECT)
	UNMACRO(int, LUA_GCCOUNT)
	UNMACRO(int, LUA_GCCOUNTB)
	UNMACRO(int, LUA_GCSTEP)
	UNMACRO(int, LUA_GCSETPAUSE)
	UNMACRO(int, LUA_GCSETSTEPMUL)
#undef LUA_GCSTOP
#undef LUA_GCRESTART
#undef LUA_GCCOLLECT
#undef LUA_GCCOUNT
#undef LUA_GCCOUNTB
#undef LUA_GCSTEP
#undef LUA_GCSETPAUSE
#undef LUA_GCSETSTEPMUL
#pragma endregion

	/*
	** garbage-collection function and options
	*/
	public enum class LuaGCOption
	{
		LUA_GCSTOP = LUA_GCSTOP_,
		LUA_GCRESTART = LUA_GCRESTART_,
		LUA_GCCOLLECT = LUA_GCCOLLECT_,
		LUA_GCCOUNT = LUA_GCCOUNT_,
		LUA_GCCOUNTB = LUA_GCCOUNTB_,
		LUA_GCSTEP = LUA_GCSTEP_,
		LUA_GCSETPAUSE = LUA_GCSETPAUSE_,
		LUA_GCSETSTEPMUL = LUA_GCSETSTEPMUL_,
	};

#pragma region LuaHookEvent_Unmacroing
	UNMACRO(int, LUA_HOOKCALL)
	UNMACRO(int, LUA_HOOKRET)
	UNMACRO(int, LUA_HOOKLINE)
	UNMACRO(int, LUA_HOOKCOUNT)
	UNMACRO(int, LUA_HOOKTAILCALL)
	UNMACRO(int, LUA_MASKCALL)
	UNMACRO(int, LUA_MASKRET)
	UNMACRO(int, LUA_MASKLINE)
	UNMACRO(int, LUA_MASKCOUNT)
#undef LUA_HOOKCALL
#undef LUA_HOOKRET
#undef LUA_HOOKLINE
#undef LUA_HOOKCOUNT
#undef LUA_HOOKTAILCALL
#undef LUA_MASKCALL
#undef LUA_MASKRET
#undef LUA_MASKLINE
#undef LUA_MASKCOUNT
#pragma endregion

	/*
	** Event codes
	*/
	public enum class LuaHookEventCode
	{
		LUA_HOOKCALL = LUA_HOOKCALL_,
		LUA_HOOKRET = LUA_HOOKRET_,
		LUA_HOOKLINE = LUA_HOOKLINE_,
		LUA_HOOKCOUNT = LUA_HOOKCOUNT_,
		LUA_HOOKTAILCALL = LUA_HOOKTAILCALL_,
	};

	/*
	** Event masks
	*/
	[Flags]
	public enum class LuaHookEventMask
	{
		LUA_MASKCALL = LUA_MASKCALL_,
		LUA_MASKRET = LUA_MASKRET_,
		LUA_MASKLINE = LUA_MASKLINE_,
		LUA_MASKCOUNT = LUA_MASKCOUNT_,
	};

	/*
	 * Delegate for function to be called by the debugger in specific events
	 */
	[UnmanagedFunctionPointer(CallingConvention::Cdecl)]
	public delegate void LuaHook( lua_State* L, lua_Debug* ar );
	typedef IntPtr LuaHookPtr;

	/* activation record */
	public ref struct LuaDebug
	{
		LuaHookEventCode event;
		String^ name;           /* (n) */
		String^ namewhat;       /* (n) 'global', 'local', 'field', 'method' */
		String^ what;           /* (S) 'Lua', 'C', 'main', 'tail' */
		String^ source;         /* (S) */
		int currentline;        /* (l) */
		int linedefined;        /* (S) */
		int lastlinedefined;    /* (S) */
		unsigned char nups;     /* (u) number of upvalues */
		unsigned char nparams;  /* (u) number of parameters */
		char isvararg;          /* (u) */
		char istailcall;        /* (t) */
		array<char>^ short_src; /* (S) */
		/* private part */
	};

	/*
	** lauxlib.h
	*/

	public ref struct LuaLReg
	{
		String^ name;
		LuaCFunction^ func;
	};

	/*
	** lualib.h
	*/

#pragma region lualib_Unmacroing
	UNMACRO(char*, LUA_COLIBNAME)
#undef LUA_COLIBNAME
	UNMACRO(char*, LUA_TABLIBNAME)
#undef LUA_TABLIBNAME
	UNMACRO(char*, LUA_IOLIBNAME)
#undef LUA_IOLIBNAME
	UNMACRO(char*, LUA_OSLIBNAME)
#undef LUA_OSLIBNAME
	UNMACRO(char*, LUA_STRLIBNAME)
#undef LUA_STRLIBNAME
	UNMACRO(char*, LUA_BITLIBNAME)
#undef LUA_BITLIBNAME
	UNMACRO(char*, LUA_MATHLIBNAME)
#undef LUA_MATHLIBNAME
	UNMACRO(char*, LUA_DBLIBNAME)
#undef LUA_DBLIBNAME
	UNMACRO(char*, LUA_LOADLIBNAME)
#undef LUA_LOADLIBNAME
#pragma endregion

	public ref class LuaWrapper abstract sealed
	{
	public:
		/*
		** lua.h
		*/

		static initonly String^ LUA_VERSION_MAJOR = toCLRString(LUA_VERSION_MAJOR_, Encoding::ASCII);
		static initonly String^ LUA_VERSION_MINOR = toCLRString(LUA_VERSION_MINOR_, Encoding::ASCII);
		static initonly unsigned int LUA_VERSION_NUM = LUA_VERSION_NUM_;
		static initonly String^ LUA_VERSION_RELEASE = toCLRString(LUA_VERSION_RELEASE_, Encoding::ASCII);

		static initonly String^ LUA_VERSION = toCLRString(LUA_VERSION_, Encoding::ASCII);
		static initonly String^ LUA_RELEASE = toCLRString(LUA_RELEASE_, Encoding::ASCII);
		static initonly String^ LUA_COPYRIGHT = toCLRString(LUA_COPYRIGHT_, Encoding::ASCII);
		static initonly String^ LUA_AUTHORS = toCLRString(LUA_AUTHORS_, Encoding::ASCII);

		/* mark for precompiled code ('<esc>Lua') */
		static initonly System::Collections::ObjectModel::ReadOnlyCollection<unsigned char>^ LUA_SIGNATURE = array<unsigned char>::AsReadOnly(gcnew array<unsigned char>{ LUA_SIGNATURE_[0], LUA_SIGNATURE_[1], LUA_SIGNATURE_[2], LUA_SIGNATURE_[3] });

		/* option for multiple returns in 'lua_pcall' and 'lua_call' */
		static initonly int LUA_MULTRET = LUA_MULTRET_;
		
		/*
		** pseudo-indices
		*/

		static initonly int LUA_REGISTRYINDEX = LUA_REGISTRYINDEX_;

#undef lua_upvalueindex

		static inline int lua_upvalueindex( int i )
		{
			return LUA_REGISTRYINDEX - i;
		}

		/*
		** basic types
		*/
		static initonly int LUA_NUMTAGS = LUA_NUMTAGS_;

		/* minimum Lua stack available to a C function */
		static initonly int LUA_MINSTACK = LUA_MINSTACK_;

		/* predefined values in the registry */
		static initonly int LUA_RIDX_MAINTHREAD = LUA_RIDX_MAINTHREAD_;
		static initonly int LUA_RIDX_GLOBALS = LUA_RIDX_GLOBALS_;
		static initonly int LUA_RIDX_LAST = LUA_RIDX_LAST_;


#define toLuaStatePtr(L) (static_cast<lua_State*>((L).ToPointer()))

		/*
		** state manipulation
		*/

		// BEWARE: the caller must ensure that the delegate being set will not be collected
		static LuaStatePtr lua_newstate( LuaAlloc^ f, IntPtr ud )
		{
			return lua_newstate(Marshal::GetFunctionPointerForDelegate(f), ud);
		}

		static LuaStatePtr lua_newstate( LuaAllocPtr f, IntPtr ud )
		{
			return LuaStatePtr(::lua_newstate(static_cast<lua_Alloc>(f.ToPointer()), ud.ToPointer()));
		}

		static void lua_close( LuaStatePtr L )
		{
			::lua_close(toLuaStatePtr(L));
		}

		static LuaStatePtr lua_newthread( LuaStatePtr L )
		{
			return LuaStatePtr(::lua_newthread(toLuaStatePtr(L)));
		}

		// BEWARE: the caller must ensure that the delegate being set will not be collected
		static void lua_atpanic( LuaStatePtr L, LuaCFunction^ panicf )
		{
			lua_atpanic(L, Marshal::GetFunctionPointerForDelegate(panicf));
		}

		static void lua_atpanic( LuaStatePtr L, LuaCFunctionPtr panicf )
		{
			::lua_atpanic(toLuaStatePtr(L), static_cast<lua_CFunction>(panicf.ToPointer()));
		}

		// MISSING: lua_version

		/*
		** basic stack manipulation
		*/

		static int lua_absindex( LuaStatePtr L, int idx )
		{
			return ::lua_absindex(toLuaStatePtr(L), idx);
		}

		static int lua_gettop( LuaStatePtr L )
		{
			return ::lua_gettop(toLuaStatePtr(L));
		}

		static void lua_settop( LuaStatePtr L, int idx )
		{
			::lua_settop(toLuaStatePtr(L), idx);
		}

		static void lua_pushvalue( LuaStatePtr L, int idx )
		{
			::lua_pushvalue(toLuaStatePtr(L), idx);
		}

		static void lua_remove( LuaStatePtr L, int idx )
		{
			::lua_remove(toLuaStatePtr(L), idx);
		}

		static void lua_insert( LuaStatePtr L, int idx )
		{
			::lua_insert(toLuaStatePtr(L), idx);
		}

		static void lua_replace( LuaStatePtr L, int idx )
		{
			::lua_replace(toLuaStatePtr(L), idx);
		}

		static void lua_copy( LuaStatePtr L, int fromidx, int toidx )
		{
			::lua_copy(toLuaStatePtr(L), fromidx, toidx);
		}

		static bool lua_checkstack( LuaStatePtr L, int sz )
		{
			return ::lua_checkstack(toLuaStatePtr(L), sz) != 0;
		}

		static void lua_xmove( LuaStatePtr from, LuaStatePtr to, int n )
		{
			::lua_xmove(toLuaStatePtr(from), toLuaStatePtr(to), n);
		}

		/*
		** access functions (stack -> C)
		*/

		static bool lua_isnumber( LuaStatePtr L, int idx )
		{
			return ::lua_isnumber(toLuaStatePtr(L), idx) != 0;
		}

		static bool lua_isstring( LuaStatePtr L, int idx )
		{
			return ::lua_isstring(toLuaStatePtr(L), idx) != 0;
		}

		static bool lua_iscfunction( LuaStatePtr L, int idx )
		{
			return ::lua_iscfunction(toLuaStatePtr(L), idx) != 0;
		}

		static bool lua_isuserdata( LuaStatePtr L, int idx )
		{
			return ::lua_isuserdata(toLuaStatePtr(L), idx) != 0;
		}

		static LuaType lua_type( LuaStatePtr L, int idx )
		{
			return static_cast<LuaType>(::lua_type(toLuaStatePtr(L), idx));
		}

		static String^ lua_typename( LuaStatePtr L, LuaType tp )
		{
			const char* ret = ::lua_typename(toLuaStatePtr(L), static_cast<int>(tp));
			return toCLRString(ret, Encoding::ASCII);
		}

		static lua_Number lua_tonumberx( LuaStatePtr L, int idx, [Out] bool% isnum )
		{
			int isnum_;
			lua_Number r = ::lua_tonumberx(toLuaStatePtr(L), idx, &isnum_);
			isnum = isnum_ != 0;
			return r;
		}

		static lua_Number lua_tointegerx( LuaStatePtr L, int idx, [Out] bool% isnum )
		{
			int isnum_;
			lua_Number r = ::lua_tointegerx(toLuaStatePtr(L), idx, &isnum_);
			isnum = isnum_ != 0;
			return r;
		}

		static lua_Number lua_tounsignedx( LuaStatePtr L, int idx, [Out] bool% isnum )
		{
			int isnum_;
			lua_Number r = ::lua_tounsignedx(toLuaStatePtr(L), idx, &isnum_);
			isnum = isnum_ != 0;
			return r;
		}

		static bool lua_toboolean( LuaStatePtr L, int idx )
		{
			return ::lua_toboolean(toLuaStatePtr(L), idx) != 0;
		}

		static String^ lua_tolstring( LuaStatePtr L, int idx, [Out] UIntPtr% len, Encoding^ stringEncoding )
		{
			size_t len_ = ~0u;
			const char* ret = ::lua_tolstring(toLuaStatePtr(L), idx, &len_);
			len = UIntPtr(len_);
			return toCLRString(ret, len_, stringEncoding);
		}

		static UIntPtr lua_rawlen( LuaStatePtr L, int idx )
		{
			return UIntPtr(::lua_rawlen(toLuaStatePtr(L), idx));
		}

		static LuaCFunctionPtr lua_tocfunction( LuaStatePtr L, int idx )
		{
			return IntPtr(::lua_tocfunction(toLuaStatePtr(L), idx));
		}

		static IntPtr lua_touserdata( LuaStatePtr L, int idx )
		{
			return IntPtr(::lua_touserdata(toLuaStatePtr(L), idx));
		}

		static LuaStatePtr lua_tothread( LuaStatePtr L, int idx )
		{
			return LuaStatePtr(::lua_tothread(toLuaStatePtr(L), idx));
		}

		static IntPtr lua_topointer( LuaStatePtr L, int idx )
		{
			return IntPtr(const_cast<void*>(::lua_topointer(toLuaStatePtr(L), idx)));
		}

		/*
		** Comparison and arithmetic functions
		*/

		static void lua_arith( LuaStatePtr L, LuaArithOp op )
		{
			::lua_arith(toLuaStatePtr(L), static_cast<int>(op));
		}

		static bool lua_rawequal( LuaStatePtr L, int idx1, int idx2 )
		{
			return ::lua_rawequal(toLuaStatePtr(L), idx1, idx2) != 0;
		}

		static bool lua_compare( LuaStatePtr L, int idx1, int idx2, LuaCompareOp op )
		{
			return ::lua_compare(toLuaStatePtr(L), idx1, idx2, static_cast<int>(op)) != 0;
		}

		/*
		** push functions (C -> stack)
		*/

		static void lua_pushnil( LuaStatePtr L )
		{
			::lua_pushnil(toLuaStatePtr(L));
		}

		static void lua_pushnumber( LuaStatePtr L, double n )
		{
			::lua_pushnumber(toLuaStatePtr(L), n);
		}

		static void lua_pushinteger( LuaStatePtr L, lua_Integer n )
		{
			::lua_pushinteger(toLuaStatePtr(L), n);
		}

		static void lua_pushunsigned( LuaStatePtr L, lua_Unsigned n )
		{
			::lua_pushunsigned(toLuaStatePtr(L), n);
		}

		static IntPtr lua_pushlstring( LuaStatePtr L, String^ s, int l, Encoding^ stringEncoding )
		{
			const char* ret = ::lua_pushlstring(toLuaStatePtr(L), toCString(s, stringEncoding), l);
			return IntPtr(const_cast<char*>(ret));
		}

		static IntPtr lua_pushstring( LuaStatePtr L, String^ s, Encoding^ stringEncoding )
		{
			array<unsigned char>^ s_bytes = toBytes(s, stringEncoding);
			const char* ret = ::lua_pushlstring(toLuaStatePtr(L), PinnedString(s_bytes), s_bytes == nullptr ? 0 : s_bytes->Length);
			return IntPtr(const_cast<char*>(ret));
		}

#ifdef INCLUDE_UNIMPLEMENTED
		static IntPtr lua_pushvfstring( LuaStatePtr L, String^ fmt, va_list argp )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static IntPtr lua_pushfstring( LuaStatePtr L, String^ fmt, ... array<Object^>^ args )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}
#endif

		// BEWARE: the caller must ensure that the delegate being pushed will not be collected
		static void lua_pushcclosure( LuaStatePtr L, LuaCFunction^ fn, int n )
		{
			lua_pushcclosure(L, Marshal::GetFunctionPointerForDelegate(fn), n);
		}

		static void lua_pushcclosure( LuaStatePtr L, LuaCFunctionPtr fn, int n )
		{
			::lua_pushcclosure(toLuaStatePtr(L), static_cast<lua_CFunction>(fn.ToPointer()), n);
		}

		static void lua_pushboolean( LuaStatePtr L, bool b )
		{
			::lua_pushboolean(toLuaStatePtr(L), b);
		}

		static void lua_pushlightuserdata( LuaStatePtr L, IntPtr p )
		{
			::lua_pushlightuserdata(toLuaStatePtr(L), p.ToPointer());
		}

		static int lua_pushthread( LuaStatePtr L )
		{
			return ::lua_pushthread(toLuaStatePtr(L));
		}

		/*
		** get functions (Lua -> stack)
		*/

		static void lua_getglobal( LuaStatePtr L, String^ var, Encoding^ nameEncoding )
		{
			::lua_getglobal(toLuaStatePtr(L), toCString(var, nameEncoding));
		}

		static void lua_gettable( LuaStatePtr L, int idx )
		{
			::lua_gettable(toLuaStatePtr(L), idx);
		}

		static void lua_getfield( LuaStatePtr L, int idx, String^ k, Encoding^ nameEncoding )
		{
			::lua_getfield(toLuaStatePtr(L), idx, toCString(k, nameEncoding));
		}

		static void lua_rawget( LuaStatePtr L, int idx )
		{
			::lua_rawget(toLuaStatePtr(L), idx);
		}

		static void lua_rawgeti( LuaStatePtr L, int idx, int n )
		{
			::lua_rawgeti(toLuaStatePtr(L), idx, n);
		}

		static void lua_rawgetp( LuaStatePtr L, int idx, IntPtr p )
		{
			::lua_rawgetp(toLuaStatePtr(L), idx, p.ToPointer());
		}

		static void lua_createtable( LuaStatePtr L, int narr, int nrec )
		{
			::lua_createtable(toLuaStatePtr(L), narr, nrec);
		}

		static IntPtr lua_newuserdata( LuaStatePtr L, size_t sz )
		{
			return IntPtr(::lua_newuserdata(toLuaStatePtr(L), sz));
		}

		static bool lua_getmetatable( LuaStatePtr L, int objindex )
		{
			return ::lua_getmetatable(toLuaStatePtr(L), objindex) != 0;
		}

		static void lua_getuservalue( LuaStatePtr L, int idx )
		{
			::lua_getuservalue(toLuaStatePtr(L), idx);
		}

		/*
		** set functions (stack -> Lua)
		*/

		static void lua_setglobal( LuaStatePtr L, String^ var, Encoding^ nameEncoding )
		{
			::lua_setglobal(toLuaStatePtr(L), toCString(var, nameEncoding));
		}

		static void lua_settable( LuaStatePtr L, int idx )
		{
			::lua_settable(toLuaStatePtr(L), idx);
		}

		static void lua_setfield( LuaStatePtr L, int idx, String^ k, Encoding^ nameEncoding )
		{
			::lua_setfield(toLuaStatePtr(L), idx, toCString(k, nameEncoding));
		}

		static void lua_rawset( LuaStatePtr L, int idx )
		{
			::lua_rawset(toLuaStatePtr(L), idx);
		}

		static void lua_rawseti( LuaStatePtr L, int idx, int n )
		{
			::lua_rawseti(toLuaStatePtr(L), idx, n);
		}

		static void lua_rawsetp( LuaStatePtr L, int idx, IntPtr p )
		{
			::lua_rawsetp(toLuaStatePtr(L), idx, p.ToPointer());
		}

		static int lua_setmetatable( LuaStatePtr L, int objindex )
		{
			return ::lua_setmetatable(toLuaStatePtr(L), objindex);
		}

		static void lua_setuservalue( LuaStatePtr L, int idx )
		{
			::lua_setuservalue(toLuaStatePtr(L), idx);
		}

		/*
		** 'load' and 'call' functions (load and run Lua code)
		*/

		// BEWARE: the caller must ensure that the delegate being used will not be collected (it shouldn't be, but consider using GC.KeepAlive anyway)
		static void lua_callk( LuaStatePtr L, int nargs, int nresults, int ctx, LuaCFunction^ k )
		{
			lua_callk(L, nargs, nresults, ctx, Marshal::GetFunctionPointerForDelegate(k));
		}

		static void lua_callk( LuaStatePtr L, int nargs, int nresults, int ctx, LuaCFunctionPtr k )
		{
			::lua_callk(toLuaStatePtr(L), nargs, nresults, ctx, static_cast<lua_CFunction>(k.ToPointer()));
		}

#undef lua_call

		static void lua_call( LuaStatePtr L, int n, int r )
		{
			::lua_callk(toLuaStatePtr(L), n, r, 0, NULL);
		}

		static LuaStatus lua_getctx( LuaStatePtr L, [Out] int% ctx )
		{
			int ctx_;
			int r = ::lua_getctx(toLuaStatePtr(L), &ctx_);
			ctx = ctx_;
			return static_cast<LuaStatus>(r);
		}

		// BEWARE: the caller must ensure that the delegate being used will not be collected (it shouldn't be, but consider using GC.KeepAlive anyway)
		static LuaStatus lua_pcallk( LuaStatePtr L, int nargs, int nresults, int errfunc, int ctx, LuaCFunction^ k )
		{
			return lua_pcallk(L, nargs, nresults, errfunc, ctx, Marshal::GetFunctionPointerForDelegate(k));
		}

		static LuaStatus lua_pcallk( LuaStatePtr L, int nargs, int nresults, int errfunc, int ctx, LuaCFunctionPtr k )
		{
			return static_cast<LuaStatus>(::lua_pcallk(toLuaStatePtr(L), nargs, nresults, errfunc, ctx, static_cast<lua_CFunction>(k.ToPointer())));
		}

#undef lua_pcall

		static LuaStatus lua_pcall( LuaStatePtr L, int n, int r, int f )
		{
			return static_cast<LuaStatus>(::lua_pcallk(toLuaStatePtr(L), n, r, f, 0, NULL));
		}

		// BEWARE: the caller must ensure that the delegate being used will not be collected (it shouldn't be, but consider using GC.KeepAlive anyway)
		static LuaStatus lua_load( LuaStatePtr L, LuaReader^ reader, IntPtr dt, String^ chunkname, String^ mode, Encoding^ chunknameEncoding )
		{
			return lua_load(L, Marshal::GetFunctionPointerForDelegate(reader), dt, chunkname, mode, chunknameEncoding);
		}

		static LuaStatus lua_load( LuaStatePtr L, LuaReaderPtr reader, IntPtr dt, String^ chunkname, String^ mode, Encoding^ chunknameEncoding )
		{
			return static_cast<LuaStatus>(::lua_load(toLuaStatePtr(L), static_cast<lua_Reader>(reader.ToPointer()), dt.ToPointer(), toCString(chunkname, chunknameEncoding), toCString(mode, Encoding::ASCII)));
		}

		static int lua_dump( LuaStatePtr L, LuaWriter^ writer, IntPtr data )
		{
			return lua_dump(L, Marshal::GetFunctionPointerForDelegate(writer), data);
		}

		static int lua_dump( LuaStatePtr L, LuaWriterPtr writer, IntPtr data )
		{
			return ::lua_dump(toLuaStatePtr(L), static_cast<lua_Writer>(writer.ToPointer()), data.ToPointer());
		}

		/*
		** coroutine functions
		*/
		// MISSING: lua_yieldk
		// MISSING: lua_yield
		// MISSING: lua_resume
		// MISSING: lua_status

		/*
		** garbage-collection function and options
		*/

		static int lua_gc( LuaStatePtr L, LuaGCOption what, int data )
		{
			return ::lua_gc(toLuaStatePtr(L), static_cast<int>(what), data);
		}

		/*
		** miscellaneous functions
		*/

#pragma warning(disable:4645)
		__declspec(noreturn)
		static int lua_error( LuaStatePtr L )
		{
			return ::lua_error(toLuaStatePtr(L));
		}

		static int lua_next( LuaStatePtr L, int idx )
		{
			return ::lua_next(toLuaStatePtr(L), idx);
		}

		static void lua_concat( LuaStatePtr L, int n )
		{
			return ::lua_concat(toLuaStatePtr(L), n);
		}

		static void lua_len( LuaStatePtr L, int idx )
		{
			return ::lua_len(toLuaStatePtr(L), idx);
		}

		static LuaAllocPtr lua_getallocf( LuaStatePtr L, [Out] IntPtr% ud )
		{
			void* ud_;
			lua_Alloc r = ::lua_getallocf(toLuaStatePtr(L), &ud_);
			ud = IntPtr(ud_);
			return LuaAllocPtr(r);
		}

		// BEWARE: the caller must ensure that the delegate being pushed will not be collected
		static void lua_setallocf( LuaStatePtr L, LuaAlloc^ f, IntPtr ud )
		{
			lua_setallocf(L, Marshal::GetFunctionPointerForDelegate(f), ud);
		}

		static void lua_setallocf( LuaStatePtr L, LuaAllocPtr f, IntPtr ud )
		{
			::lua_setallocf(toLuaStatePtr(L), static_cast<lua_Alloc>(f.ToPointer()), ud.ToPointer());
		}

		/*
		** ===============================================================
		** some useful macros
		** ===============================================================
		*/

#undef lua_tonumber

		static double lua_tonumber( LuaStatePtr L, int i )
		{
			return ::lua_tonumberx(toLuaStatePtr(L), i, NULL);
		}

#undef lua_tointeger

		static lua_Integer lua_tointeger( LuaStatePtr L, int i )
		{
			return ::lua_tointegerx(toLuaStatePtr(L), i, NULL);
		}

#undef lua_tounsigned

		static lua_Unsigned lua_tounsigned( LuaStatePtr L, int i )
		{
			return ::lua_tounsignedx(toLuaStatePtr(L), i, NULL);
		}

#undef lua_pop

		static void lua_pop( LuaStatePtr L, int n )
		{
			::lua_settop(toLuaStatePtr(L), -n - 1);
		}

#undef lua_newtable
		
		static void lua_newtable( LuaStatePtr L )
		{
			::lua_createtable(toLuaStatePtr(L), 0, 0);
		}

#undef lua_register
		
		// BEWARE: the caller must ensure that the delegate being registered will not be collected
		static void lua_register( LuaStatePtr L, String^ n, LuaCFunction^ f, Encoding^ nameEncoding )
		{
			lua_register(L, n, Marshal::GetFunctionPointerForDelegate(f), nameEncoding);
		}

		static void lua_register( LuaStatePtr L, String^ n, LuaCFunctionPtr f, Encoding^ nameEncoding )
		{
			::lua_pushcfunction(toLuaStatePtr(L), static_cast<lua_CFunction>(f.ToPointer()));
			::lua_setglobal(toLuaStatePtr(L), toCString(n, nameEncoding));
		}

#undef lua_pushcfunction

		// BEWARE: the caller must ensure that the delegate being pushed will not be collected
		static void lua_pushcfunction( LuaStatePtr L, LuaCFunction^ f )
		{
			lua_pushcfunction(L, Marshal::GetFunctionPointerForDelegate(f));
		}

		static void lua_pushcfunction( LuaStatePtr L, LuaCFunctionPtr f )
		{
			::lua_pushcclosure(toLuaStatePtr(L), static_cast<lua_CFunction>(f.ToPointer()), 0);
		}

#undef lua_isfunction

		static bool lua_isfunction( LuaStatePtr L, int n )
		{
			return ::lua_type(toLuaStatePtr(L), n) == static_cast<int>(LuaType::LUA_TFUNCTION);
		}

#undef lua_istable

		static bool lua_istable( LuaStatePtr L, int n )
		{
			return ::lua_type(toLuaStatePtr(L), n) == static_cast<int>(LuaType::LUA_TTABLE);
		}

#undef lua_islightuserdata

		static bool lua_islightuserdata( LuaStatePtr L, int n )
		{
			return ::lua_type(toLuaStatePtr(L), n) == static_cast<int>(LuaType::LUA_TLIGHTUSERDATA);
		}

#undef lua_isnil

		static bool lua_isnil( LuaStatePtr L, int n )
		{
			return ::lua_type(toLuaStatePtr(L), n) == static_cast<int>(LuaType::LUA_TNIL);
		}

#undef lua_isboolean

		static bool lua_isboolean( LuaStatePtr L, int n )
		{
			return ::lua_type(toLuaStatePtr(L), n) == static_cast<int>(LuaType::LUA_TBOOLEAN);
		}

#undef lua_isthread

		static bool lua_isthread( LuaStatePtr L, int n )
		{
			return ::lua_type(toLuaStatePtr(L), n) == static_cast<int>(LuaType::LUA_TTHREAD);
		}

#undef lua_isnone

		static bool lua_isnone( LuaStatePtr L, int n )
		{
			return ::lua_type(toLuaStatePtr(L), n) == static_cast<int>(LuaType::LUA_TNONE);
		}

#undef lua_isnoneornil

		static bool lua_isnoneornil( LuaStatePtr L, int n )
		{
			return ::lua_type(toLuaStatePtr(L), n) <= 0;
		}

#undef lua_pushliteral

		static IntPtr lua_pushliteral( LuaStatePtr L, String^ s, Encoding^ stringEncoding )
		{
			array<unsigned char>^ s_bytes = toBytes(s, stringEncoding);
			const char* ret = ::lua_pushlstring(toLuaStatePtr(L), PinnedString(s_bytes), s_bytes == nullptr ? 0 : s_bytes->Length);
			return IntPtr(const_cast<char*>(ret));
		}

		// MISSING: lua_pushglobaltable

#undef lua_tostring

		static String^ lua_tostring( LuaStatePtr L, int idx, Encoding^ stringEncoding )
		{
			const char* ret = ::lua_tolstring(toLuaStatePtr(L), idx, NULL);
			return toCLRString(ret, stringEncoding);
		}

		/*
		** {======================================================================
		** Debug API
		** =======================================================================
		*/

#ifdef INCLUDE_UNIMPLEMENTED
		static int lua_getstack( LuaStatePtr L, int level, [Out] LuaDebug^% ar )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static int lua_getinfo( LuaStatePtr L, String^ what, LuaDebug^ ar )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static String^ lua_getlocal( LuaStatePtr L, LuaDebug^ ar, int n )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static String^ lua_setlocal( LuaStatePtr L, LuaDebug^ ar, int n )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}
#endif

		static String^ lua_getupvalue( LuaStatePtr L, int funcindex, int n, Encoding^ nameEncoding )
		{
			const char* ret = ::lua_getupvalue(toLuaStatePtr(L), funcindex, n);
			return toCLRString(ret, nameEncoding);
		}

		static String^ lua_setupvalue( LuaStatePtr L, int funcindex, int n, Encoding^ nameEncoding )
		{
			const char* ret = ::lua_setupvalue(toLuaStatePtr(L), funcindex, n);
			return toCLRString(ret, nameEncoding);
		}

#ifdef INCLUDE_UNIMPLEMENTED
		static IntPtr lua_upvalueid( LuaStatePtr L, int fidx, int n )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static void lua_upvaluejoin( LuaStatePtr L, int fidx1, int n1, int fidx2, int n2 )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}
#endif

		// BEWARE: the caller must ensure that the delegate being set will not be collected
		static int lua_sethook( LuaStatePtr L, LuaHook^ func, LuaHookEventMask mask, int count )
		{
			return lua_sethook(L, Marshal::GetFunctionPointerForDelegate(func), mask, count);
		}

		static int lua_sethook( LuaStatePtr L, LuaHookPtr func, LuaHookEventMask mask, int count )
		{
			return ::lua_sethook(toLuaStatePtr(L), static_cast<lua_Hook>(func.ToPointer()), static_cast<int>(mask), count);
		}

		static LuaHookPtr lua_gethook( LuaStatePtr L )
		{
			return LuaHookPtr(::lua_gethook(toLuaStatePtr(L)));
		}

		static int lua_gethookmask( LuaStatePtr L )
		{
			return ::lua_gethookmask(toLuaStatePtr(L));
		}

		static int lua_gethookcount( LuaStatePtr L )
		{
			return ::lua_gethookcount(toLuaStatePtr(L));
		}

		/*
		** lualib.h
		*/

		static initonly LuaCFunctionPtr luaopen_base = LuaCFunctionPtr(::luaopen_base);

		static initonly String^ LUA_COLIBNAME = toCLRString(LUA_COLIBNAME_, Encoding::ASCII);
		static initonly LuaCFunctionPtr luaopen_coroutine = LuaCFunctionPtr(::luaopen_coroutine);

		static initonly String^ LUA_TABLIBNAME = toCLRString(LUA_TABLIBNAME_, Encoding::ASCII);
		static initonly LuaCFunctionPtr luaopen_table = LuaCFunctionPtr(::luaopen_table);

		static initonly String^ LUA_IOLIBNAME = toCLRString(LUA_IOLIBNAME_, Encoding::ASCII);
		static initonly LuaCFunctionPtr luaopen_io = LuaCFunctionPtr(::luaopen_io);

		static initonly String^ LUA_OSLIBNAME = toCLRString(LUA_OSLIBNAME_, Encoding::ASCII);
		static initonly LuaCFunctionPtr luaopen_os = LuaCFunctionPtr(::luaopen_os);

		static initonly String^ LUA_STRLIBNAME = toCLRString(LUA_STRLIBNAME_, Encoding::ASCII);
		static initonly LuaCFunctionPtr luaopen_string = LuaCFunctionPtr(::luaopen_string);

		static initonly String^ LUA_BITLIBNAME = toCLRString(LUA_BITLIBNAME_, Encoding::ASCII);
		static initonly LuaCFunctionPtr luaopen_bit32 = LuaCFunctionPtr(::luaopen_bit32);

		static initonly String^ LUA_MATHLIBNAME = toCLRString(LUA_MATHLIBNAME_, Encoding::ASCII);
		static initonly LuaCFunctionPtr luaopen_math = LuaCFunctionPtr(::luaopen_math);

		static initonly String^ LUA_DBLIBNAME = toCLRString(LUA_DBLIBNAME_, Encoding::ASCII);
		static initonly LuaCFunctionPtr luaopen_debug = LuaCFunctionPtr(::luaopen_debug);

		static initonly String^ LUA_LOADLIBNAME = toCLRString(LUA_LOADLIBNAME_, Encoding::ASCII);
		static initonly LuaCFunctionPtr luaopen_package = LuaCFunctionPtr(::luaopen_package);

		/* open all previous libraries */
		static void luaL_openlibs( LuaStatePtr L )
		{
			::luaL_openlibs(toLuaStatePtr(L));
		}

		/*
		** lauxlib.h
		*/

#undef luaL_checkversion

		static void luaL_checkversion( LuaStatePtr L )
		{
			::luaL_checkversion_(toLuaStatePtr(L), LUA_VERSION_NUM);
		}

		static bool luaL_getmetafield( LuaStatePtr L, int obj, String^ e, Encoding^ nameEncoding )
		{
			return ::luaL_getmetafield(toLuaStatePtr(L), obj, toCString(e, nameEncoding)) != 0;
		}

		static bool luaL_callmeta( LuaStatePtr L, int obj, String^ e, Encoding^ nameEncoding )
		{
			return ::luaL_callmeta(toLuaStatePtr(L), obj, toCString(e, nameEncoding)) != 0;
		}

		static String^ luaL_tolstring( LuaStatePtr L, int idx, [Out] UIntPtr% len, Encoding^ stringEncoding )
		{
			size_t len_ = ~0u;
			const char* ret = ::luaL_tolstring(toLuaStatePtr(L), idx, &len_);
			len = UIntPtr(len_);
			return toCLRString(ret, len_, stringEncoding);
		}

		__declspec(noreturn)
		static int luaL_argerror( LuaStatePtr L, int numarg, String^ extramsg, Encoding^ messageEncoding )
		{
			return ::luaL_argerror(toLuaStatePtr(L), numarg, toCString(extramsg, messageEncoding));
		}

#ifdef INCLUDE_UNIMPLEMENTED
		static String^ luaL_checklstring( LuaStatePtr L, int numArg, [Out] size_t% l )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static String^ luaL_optlstring( LuaStatePtr L, int numArg, String^ def, [Out] size_t% l )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static lua_Number luaL_checknumber( LuaStatePtr L, int numArg )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static lua_Number luaL_optnumber( LuaStatePtr L, int nArg, lua_Number def )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static lua_Integer luaL_checkinteger( LuaStatePtr L, int numArg )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static lua_Integer luaL_optinteger( LuaStatePtr L, int nArg, lua_Number def )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static lua_Unsigned luaL_checkunsigned( LuaStatePtr L, int numArg )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static lua_Unsigned luaL_optunsigned( LuaStatePtr L, int nArg, lua_Number def )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}
#endif

		static void luaL_checkstack( LuaStatePtr L, int sz, String^ msg, Encoding^ messageEncoding )
		{
			::luaL_checkstack(toLuaStatePtr(L), sz, toCString(msg, messageEncoding));
		}

		static void luaL_checktype( LuaStatePtr L, int narg, LuaType t )
		{
			::luaL_checktype(toLuaStatePtr(L), narg, static_cast<int>(t));
		}

		static void luaL_checkany( LuaStatePtr L, int narg )
		{
			::luaL_checkany(toLuaStatePtr(L), narg);
		}

		static bool luaL_newmetatable( LuaStatePtr L, String^ tname, Encoding^ nameEncoding )
		{
			return ::luaL_newmetatable(toLuaStatePtr(L), toCString(tname, nameEncoding)) != 0;
		}

		static void luaL_setmetatable( LuaStatePtr L, String^ tname, Encoding^ nameEncoding )
		{
			return ::luaL_setmetatable(toLuaStatePtr(L), toCString(tname, nameEncoding));
		}

		static IntPtr luaL_testudata( LuaStatePtr L, int ud, String^ tname, Encoding^ nameEncoding )
		{
			return IntPtr(::luaL_testudata(toLuaStatePtr(L), ud, toCString(tname, nameEncoding)));
		}

		static IntPtr luaL_checkudata( LuaStatePtr L, int ud, String^ tname, Encoding^ nameEncoding )
		{
			return IntPtr(::luaL_checkudata(toLuaStatePtr(L), ud, toCString(tname, nameEncoding)));
		}

		static void luaL_where( LuaStatePtr L, int lvl )
		{
			::luaL_where(toLuaStatePtr(L), lvl);
		}

#ifdef INCLUDE_UNIMPLEMENTED
		__declspec(noreturn)
		static int luaL_error( LuaStatePtr L, const char* fmt, ... array<Object^>^ args )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static int luaL_checkoption( LuaStatePtr L, int narg, String^ def, array<String^>^ lst )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static int luaL_fileresult( LuaStatePtr L, int stat, String^ fname )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		static int luaL_execresult( LuaStatePtr L, int stat )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}

		// MISSING: LUA_NOREF
		// MISSING: LUA_REFNIL
#endif

		static int luaL_ref( LuaStatePtr L, int t )
		{
			return ::luaL_ref(toLuaStatePtr(L), t);
		}

		static void luaL_unref( LuaStatePtr L, int t, int ref )
		{
			::luaL_unref(toLuaStatePtr(L), t, ref);
		}

#ifdef INCLUDE_UNIMPLEMENTED
		static int luaL_loadfilex( LuaStatePtr L, String^ filename, String^ mode )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}
#endif

#undef luaL_loadfile

		static LuaStatus luaL_loadfile( LuaStatePtr L, String^ f )
		{
			return static_cast<LuaStatus>(::luaL_loadfilex(toLuaStatePtr(L), toCString(f, Encoding::Default), NULL));
		}

		static LuaStatus luaL_loadbufferx( LuaStatePtr L, array<char>^ buff, size_t sz, String^ name, String^ mode, Encoding^ chunknameEncoding )
		{
			pin_ptr<char> pin_buff = &buff[0];
			return static_cast<LuaStatus>(::luaL_loadbufferx(toLuaStatePtr(L), pin_buff, sz, toCString(name, chunknameEncoding), toCString(mode, Encoding::ASCII)));
		}

		static LuaStatus luaL_loadstring( LuaStatePtr L, String^ s, Encoding^ chunkEncoding )
		{
			return static_cast<LuaStatus>(::luaL_loadstring(toLuaStatePtr(L), toCString(s, chunkEncoding)));
		}

		static LuaStatePtr luaL_newstate( void )
		{
			return LuaStatePtr(::luaL_newstate());
		}

		static int luaL_len( LuaStatePtr L, int idx )
		{
			return ::luaL_len(toLuaStatePtr(L), idx);
		}

		static String^ luaL_gsub( LuaStatePtr L, String^ s, String^ p, String^ r, Encoding^ stringEncoding )
		{
			const char* ret = ::luaL_gsub(toLuaStatePtr(L), toCString(s, stringEncoding), toCString(p, stringEncoding), toCString(r, stringEncoding));
			return toCLRString(ret, stringEncoding);
		}

		// BEWARE: the caller must ensure that the delegates being registered will not be collected
		static void luaL_setfuncs( LuaStatePtr L, array<LuaLReg^>^ l, int nup, Encoding^ nameEncoding )
		{
			HGlobal<luaL_Reg> cl(l->Length + 1);

			cl[l->Length].name = NULL;
			cl[l->Length].func = NULL;
			for (int i = l->Length - 1; i >= 0; --i)
			{
				array<unsigned char>^ name_bytes = nameEncoding->GetBytes(l[i]->name);
				int length = name_bytes->Length;
				char* name = new char[length + 1];
				Marshal::Copy(name_bytes, 0, IntPtr(name), length);
				name[length] = '\0';

				cl[i].name = name;
				cl[i].func = static_cast<lua_CFunction>(Marshal::GetFunctionPointerForDelegate(l[i]->func).ToPointer());
			}

			::luaL_setfuncs(toLuaStatePtr(L), cl, nup);

			for (int i = l->Length - 1; i >= 0; --i)
				delete[] cl[i].name;
		}

#ifdef INCLUDE_UNIMPLEMENTED
		static int luaL_getsubtable( LuaStatePtr L, int idx, String^ fname )
		{
			throw gcnew NotImplementedException(__FUNCTION__); // TODO
		}
#endif

		static void luaL_traceback( LuaStatePtr L, LuaStatePtr L1, String^ msg, int level, Encoding^ messageEncoding )
		{
			::luaL_traceback(toLuaStatePtr(L), toLuaStatePtr(L1), toCString(msg, messageEncoding), level);
		}

		static void luaL_requiref( LuaStatePtr L, String^ modname, LuaCFunction^ openf, bool glb, Encoding^ nameEncoding )
		{
			luaL_requiref(L, modname, Marshal::GetFunctionPointerForDelegate(openf), glb, nameEncoding);
		}

		static void luaL_requiref( LuaStatePtr L, String^ modname, LuaCFunctionPtr openf, bool glb, Encoding^ nameEncoding )
		{
			::luaL_requiref(toLuaStatePtr(L), toCString(modname, nameEncoding), static_cast<lua_CFunction>(openf.ToPointer()), glb);
		}

		/*
		** ===============================================================
		** some useful macros
		** ===============================================================
		*/

#undef luaL_newlibtable

		static void luaL_newlibtable( LuaStatePtr L, array<LuaLReg^>^ l )
		{
			::lua_createtable(toLuaStatePtr(L), 0, l->Length);
		}

#undef luaL_newlib

		static void luaL_newlib( LuaStatePtr L, array<LuaLReg^>^ l, Encoding^ nameEncoding )
		{
			luaL_newlibtable(L, l);
			luaL_setfuncs(L, l, 0, nameEncoding);
		}

#undef luaL_argcheck

		static void luaL_argcheck( LuaStatePtr L, bool cond, int numarg, String^ extramsg, Encoding^ messageEncoding )
		{
			if (!cond)
				::luaL_argerror(toLuaStatePtr(L), numarg, toCString(extramsg, messageEncoding));
		}

#undef luaL_checkstring

		static String^ luaL_checkstring( LuaStatePtr L, int n, Encoding^ stringEncoding )
		{
			const char* ret = ::luaL_checklstring(toLuaStatePtr(L), n, NULL);
			return toCLRString(ret, stringEncoding);
		}

#undef luaL_optstring

		static String^ luaL_optstring( LuaStatePtr L, int n, String^ d, Encoding^ stringEncoding )
		{
			PinnedString pinned_d(toCString(d, stringEncoding));  // cannot be r-value (may be returned)
			const char* ret = ::luaL_optlstring(toLuaStatePtr(L), n, pinned_d, NULL);
			return ret == pinned_d ? d : toCLRString(ret, stringEncoding);
		}

#undef luaL_checkint

		static int luaL_checkint( LuaStatePtr L, int n )
		{
			return static_cast<int>(::luaL_checkinteger(toLuaStatePtr(L), n));
		}

#undef luaL_optint

		static int luaL_optint( LuaStatePtr L, int n, int d )
		{
			return static_cast<int>(::luaL_optinteger(toLuaStatePtr(L), n, d));
		}

#undef luaL_checklong

		static long luaL_checklong( LuaStatePtr L, int n )
		{
			return static_cast<long>(::luaL_checkinteger(toLuaStatePtr(L), n));
		}

#undef luaL_optlong

		static long luaL_optlong( LuaStatePtr L, int n, long d )
		{
			return static_cast<long>(::luaL_optinteger(toLuaStatePtr(L), n, d));
		}

#undef luaL_typename

		static String^ luaL_typename( LuaStatePtr L, int i )
		{
			const char* ret = ::lua_typename(toLuaStatePtr(L), ::lua_type(toLuaStatePtr(L), i));
			return toCLRString(ret, Encoding::ASCII);
		}

#undef luaL_dofile

		static bool luaL_dofile( LuaStatePtr L, String^ fn )
		{
			return luaL_loadfile(L, fn) != LuaStatus::LUA_OK || lua_pcall(L, 0, LUA_MULTRET, 0) != LuaStatus::LUA_OK;
		}

#undef luaL_dostring

		static bool luaL_dostring( LuaStatePtr L, String^ s, Encoding^ chunkEncoding )
		{
			return luaL_loadstring(L, s, chunkEncoding) != LuaStatus::LUA_OK || lua_pcall(L, 0, LUA_MULTRET, 0) != LuaStatus::LUA_OK;
		}

#undef luaL_getmetatable

		static void luaL_getmetatable( LuaStatePtr L, String^ n, Encoding^ nameEncoding )
		{
			::lua_getfield(toLuaStatePtr(L), LUA_REGISTRYINDEX, toCString(n, nameEncoding));
		}

		// UNDOCUMENTED: luaL_opt

#undef luaL_loadbuffer

		static LuaStatus luaL_loadbuffer( LuaStatePtr L, String^ s, size_t sz, String^ n, Encoding^ chunkEncoding, Encoding^ chunknameEncoding )
		{
			return static_cast<LuaStatus>(::luaL_loadbufferx(toLuaStatePtr(L), toCString(s, chunkEncoding), sz, toCString(n, chunknameEncoding), NULL));
		}

		// TODO

		/*
		** additional CLR simplifications
		*/

		static LuaStatus luaW_loadbufferx( LuaStatePtr L, String^ buff, String^ name, String^ mode, Encoding^ chunkEncoding, Encoding^ chunknameEncoding )
		{
			array<unsigned char>^ buff_bytes = toBytes(buff, chunkEncoding);
			return static_cast<LuaStatus>(::luaL_loadbufferx(toLuaStatePtr(L), PinnedString(buff_bytes), buff_bytes == nullptr ? 0 : buff_bytes->Length, toCString(name, chunknameEncoding), toCString(mode, Encoding::ASCII)));
		}

		static LuaStatus luaW_loadbuffer( LuaStatePtr L, String^ buff, String^ name, Encoding^ chunkEncoding, Encoding^ chunknameEncoding )
		{
			return luaW_loadbufferx(L, buff, name, nullptr, chunkEncoding, chunknameEncoding);
		}

		/*
		** custom debug hook functions
		*/

		// BEWARE: the caller must ensure that the delegate being set will not be collected
		static int luaW_presethook( LuaStatePtr L, LuaHook^ func )
		{
			return luaW_presethook(L, Marshal::GetFunctionPointerForDelegate(func));
		}

		static int luaW_presethook( LuaStatePtr L, LuaHookPtr func )
		{
			return ::luaW_presethook(toLuaStatePtr(L), static_cast<lua_Hook>(func.ToPointer()));
		}

		static int luaW_enablehook( LuaStatePtr L )
		{
			return ::luaW_enablehook(toLuaStatePtr(L));
		}

		static int luaW_disablehook( LuaStatePtr L )
		{
			return ::luaW_disablehook(toLuaStatePtr(L));
		}

		/*
		** custom traceback functions
		*/

		static int luaW_countlevels( LuaStatePtr L )
		{
			return ::luaW_countlevels(toLuaStatePtr(L));
		}

		static void luaW_traceback( LuaStatePtr L, LuaStatePtr L1, int level, int bottom )
		{
			::luaW_traceback(toLuaStatePtr(L), toLuaStatePtr(L1), level, bottom);
		}

		/*
		** normally unexported interperter
		*/

		static initonly LuaCFunction^ pmain = safe_cast<LuaCFunction^>(Marshal::GetDelegateForFunctionPointer(IntPtr(&::pmain), LuaCFunction::typeid));
	};

	/*
	** additional CLR helpers
	*/

	public ref class LuaStreamReader
	{
	private:
		System::IO::Stream^ stream;
		array<unsigned char>^ buffer;
		GCHandle bufferHandle;

		LuaReader^ readerDelegate;

	public:
		LuaStreamReader( System::IO::Stream^ stream )
			: stream(stream),
			  buffer(gcnew array<unsigned char>(1 << 14)),
			  bufferHandle(System::Runtime::InteropServices::GCHandle::Alloc(buffer, System::Runtime::InteropServices::GCHandleType::Pinned))
		{
			readerDelegate = gcnew LuaReader(this, &LuaStreamReader::read);
		}

		~LuaStreamReader()
		{
			this->!LuaStreamReader();

			GC::SuppressFinalize(this);
		}

		!LuaStreamReader()
		{
			if (bufferHandle.IsAllocated)
				bufferHandle.Free();
		}

	public:
		property LuaReader^ Reader
		{
			LuaReader^ get()
			{
				return readerDelegate;
			}
		}

	private:
		// non-rentrant!
		const char* read( lua_State* L, void* ud, size_t* sz )
		{
			(void)L;
			(void)ud;
			int read = stream->Read(buffer, 0, buffer->Length);
			*sz = read;
			return reinterpret_cast<char*>(bufferHandle.AddrOfPinnedObject().ToPointer());
		}
	};

	public ref class LuaStreamWriter
	{
	private:
		System::IO::Stream^ stream;

		LuaWriter^ writerDelegate;

	public:
		LuaStreamWriter( System::IO::Stream^ stream )
			: stream(stream)
		{
			writerDelegate = gcnew LuaWriter(this, &LuaStreamWriter::write);
		}

	public:
		property LuaWriter^ Writer
		{
			LuaWriter^ get()
			{
				return writerDelegate;
			}
		}

	private:
		// non-rentrant!
		[System::Security::Permissions::SecurityPermission(System::Security::Permissions::SecurityAction::Assert, UnmanagedCode = true)]
		const int write( lua_State* L, const void* p, size_t sz, void* ud )
		{
			(void)L;
			(void)ud;
			System::IO::UnmanagedMemoryStream bufferStream(reinterpret_cast<unsigned char*>(const_cast<void*>(p)), sz);
			bufferStream.CopyTo(stream);
			return 0;
		}
	};

	public ref class LuaAllocTracker
	{
	internal:
		size_t* _allocated;

	internal:
		LuaAllocTracker()
			: _allocated(new size_t())
		{
			*_allocated = 0;
		}

		~LuaAllocTracker()
		{
			this->!LuaAllocTracker();

			GC::SuppressFinalize(this);
		}

		!LuaAllocTracker()
		{
			delete _allocated;
		}

	public:
		property UIntPtr Allocated
		{
			UIntPtr get() { return UIntPtr(*_allocated); }
		}
	};

	static void* LuaAllocTracker_alloc( void* ud, void* ptr, size_t osize, size_t nsize )
	{
		if (ptr == NULL)
			*(size_t*)ud += nsize;
		else
			*(size_t*)ud += nsize - osize;

		if (nsize == 0)
		{
			free(ptr);
			return NULL;
		}
		else
			return realloc(ptr, nsize);
	}

	public ref class LuaInterjector
	{
	public:
		delegate void Interjection( LuaStatePtr L );

	private:
		String^ message;
		Encoding^ messageEncoding;
		bool cancelled;

		System::Collections::Concurrent::ConcurrentQueue<Interjection^>^ interjections;

		LuaStatePtr L;

	internal:
		LuaHook^ hookDelegate;

	internal:
		LuaInterjector( LuaStatePtr L )
			: message(nullptr),
			  cancelled(false),
			  interjections(gcnew System::Collections::Concurrent::ConcurrentQueue<Interjection^>()),
			  L(L)
		{
			hookDelegate = gcnew LuaHook(this, &LuaInterjector::hook);
		}

	public:
		void Cancel( String^ message, Encoding^ messageEncoding )
		{
			this->message = message;
			this->messageEncoding = messageEncoding;
			cancelled = true;

			LuaWrapper::luaW_enablehook(L);
		}

		void RevertCancel()
		{
			this->message = nullptr;
			cancelled = false;
		}

		void Interject( Interjection^ interjection )
		{
			interjections->Enqueue(interjection);

			LuaWrapper::luaW_enablehook(L);
		}

	internal:
		void hook( lua_State* L, lua_Debug* ar )
		{
			assert(L == this->L.ToPointer());

			(void)ar;
			if (cancelled)
			{
				::luaL_error(L, toCString(message, messageEncoding));
			}
			else
			{
				luaW_disablehook(L);

				Interjection^ interjection;
				while (interjections->TryDequeue(interjection))
					interjection(this->L);
			}
		}
	};

	public ref class LuaHelper
	{
	public:
		// allocation tracker is necessary because lua_gc isn't thread-safe
		static LuaStatePtr luaH_newstate( [Out] LuaAllocTracker^% memoryStats )
		{
			memoryStats = gcnew LuaAllocTracker();
			return LuaWrapper::lua_newstate(LuaAllocPtr(LuaAllocTracker_alloc), IntPtr(memoryStats->_allocated));
		}

		static LuaInterjector^ luaH_setnewinterjectionhook( LuaStatePtr L )
		{
			LuaInterjector^ interjector = gcnew LuaInterjector(L);

			LuaWrapper::luaW_presethook(L, interjector->hookDelegate);

			return interjector;
		}
	};
}
