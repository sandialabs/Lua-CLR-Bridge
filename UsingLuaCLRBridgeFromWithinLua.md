# Using Lua-CLR Bridge from within Lua #

Lua-CLR Bridge provides a semi-transparent interface for Lua to manipulate CLI objects.  Lua-CLR Bridge implements much of the functionality of a *CLS consumer*, as specified in ECMA-335; however it does not necessarily conform completely to the CLS.

## 1.0 Types and Values ##

Many of the basic CLI types and basic Lua types are translated directly.  Some Lua types are translated to non-basic representative types (e.g. `LuaTable`).  The CLI types that do not appear in the following table are treated as `Object`.  When a CLI object is translated, the resulting Lua userdata acts as a proxy to the actual CLI object.

| CLI Type                   | Lua Type                              |
|----------------------------|---------------------------------------|
| `null`                     | `nil`                                 |
| `Bool`                     | boolean                               |
| `Char`                     | number                                |
| `SByte`, `Int16`, `Int32`  | number                                |
| `Byte`, `UInt16`, `UInt32` | number                                |
| `Single`, `Double`         | number                                |
| `Int64`                    | `CLRInt64`                            |
| `UInt64`                   | `CLRUInt64`                           |
| `string`                   | string                                |
| `LuaFunction`              | function                              |
| `LuaUserData`              | userdata                              |
| `IntPtr`                   | light userdata                        |
| `Object`                   | userdata (with identifying metatable) |
| `LuaThread`                | thread                                |
| `LuaTable`                 | table                                 |

Because multiple CLI numeric types map to a single Lua numeric type, Lua-CLR Bridge will perform a narrowing coercion when appropriate if the value is in the range of the target numeric type – this may occur when CLI fields are assigned, CLI properties are set, or CLI methods/operators/constructors are invoked from within Lua.

Nullable-type values (such as `int?`) are dynamically translated to and from values or `nil`.  For example, getting a nullable-type property will return either a value or `nil`, and a nullable-type property can be set to either a value or `nil`.

#### 1.0.1 64-bit Numbers ####

The 64-bit CLI integer types are not translated to the Lua "number" type because 64-bit integer values cannot be exactly represented by it.  The wrapper types that they are instead translated to (`CLRInt64` and `CLRUInt64`) support *many* of the same operations in Lua as numbers but will not work with Lua library functions expecting number-type values.  Wrapped values will not have narrowing coercions performed on them, but they may be explicitly narrowed (§ 1.3).

The exponentiation and concatenation operators are not implemented for wrapped values because they do not have corresponding CLI operators.

Numeric operations that have wrapped values for all operands generally produce wrapped values unless the result of the operation overflows the wrapped type.  The exceptions are negation of an unsigned 64-bit wrapped value, which will always produce a Lua number even if the value is zero, and operations that have one `CLRInt64` operand and one `CLRUInt64` operand, which will always produce a Lua number.

Numeric operations that have wrapped values for only some operands generally produce wrapped values unless one of the operands is non-integral or the result of the operation overflows the wrapped type.  The exception is division, which will always produce a Lua number.

The equal (`==`) and not-equal (`~=`) operators will **always** return `false` and `true`, respectively, when comparing a wrapped value and a Lua number.  To perform an equality comparison between a wrapped value and a Lua number, the Lua number must be explicitly cast to a wrapped type (§ 1.3) (e.g. `x == CLR.Cast.Int64(0)` where `x` is a variable containing a wrapped value).

### 1.1 Types ###

CLI types (instances of `System.Type`) are accessed from within Lua using the `CLR.Type` lookup table.

> In Lua
>
>     CLR.Type['System.Int32']
>
> is equivalent to
>
>     typeof(System.Int32)
>
> in C#.

This mechanism will find only exported types in loaded CLI assemblies.

### 1.2 Delegates ###

Delegates are created using the `CLR.NewDelegate` function.  A delegate can be created from either a Lua function or a CLI method group.

A delegate created from a Lua function operates almost as the inverse of a CLI method call (§ 2.3).  Because Lua does not support passing arguments by reference, `out` and `ref` parameters are returned, in order, following the return value of the delegate (if any) using the multiple-return feature of Lua.  If a value of the wrong type is returned then `InvalidCastException` will be thrown.

> For some C# delegates
>
>     namespace N
>     {
>         public delegate int F();
>         public delegate void G( int i );
>         public delegate int H( out string s, int i );
>         public delegate void I( string s, ref int i );
>     }
>
> well-formed delegates are created, respectively, using
>
>     local dF, dG, dH, dI
>     dF = CLR.NewDelegate(CLR.Type['N.F'],
>                          function( ) local r = 0 return r end)
>     dG = CLR.NewDelegate(CLR.Type['N.G'],
>                          function( i ) return end)
>     dH = CLR.NewDelegate(CLR.Type['N.H'],
>                          function( i ) local r, s = 0, 'hello'
>                                        return r, s end)
>     dI = CLR.NewDelegate(CLR.Type['N.I'],
>                         function( s, i ) i = i + 1 return i end)
>
> in Lua.

A delegate is created from a CLI method group by specifying the name of the method group.

> If a Lua variable `c` is an instance of the C# class `C`
>
>     class C
>     {
>         public int F() { … }
>         public void F( int i ) { … }
>     }
>
> then a delegate is created from each instance method, respectively, using
>
>     local dF, dG
>     dF = CLR.NewDelegate(CLR.Type['N.F'], c.F)
>     dG = CLR.NewDelegate(CLR.Type['N.G'], c.F)
>
> in Lua.

### 1.3 Casting ###

Lua-CLR Bridge provides value-narrowing helpers through `CLR.Cast`.  All the CLI basic numeric types are supported narrowing targets.  Because Lua has only one numeric type, narrowed values are not truly casted to the target type (and therefore will not affect method resolution, etc.) but are simply reduced to the range of the target type.  The exceptions are `Int64` and `UInt64` which return instances of `CLRInt64` and `CLRUInt64` (§ 1.1), respectively.

> In Lua
>
>     CLR.Cast.Int32(value)
>
> narrows the value equivalently to
>
>     (int)value;
>
> in C#.

A CLI object can be tested for being an instance of a type (or one of its subtypes) using `CLR.Is`.  (This is similar to the `is` operator in C#.)

> In Lua
>
>     CLR.Is(cliObject, cliType)
>
> is equivalent to
>
>     cliType.IsInstanceOf(cliObject)
>
> in Lua or C#.

A CLI object can also be tested for being an instance of a type (or one of its subtypes) using `CLR.As`.  (This is similar to the `as` operator in C#.)

> In Lua
>
>     CLR.As(cliObject, cliType)
>
> is equivalent to
>
>     (CLR.Is(cliObject, cliType) and {cliObject} or {nil})[1]
>
> in Lua.  (`(x and {y} or {z})[1]` is a solution to the lack of a ternary operator in Lua.)

### 1.4 Array Elements ###

Elements of single-dimension arrays are accessed in the same manner as Lua table (array) elements.

> If a Lua variable `a` is an instance of the CLI array type `object[]` then the first element of the array is read using
>
>     local value = a[0]
>
> and is written using
>
>     a[0] = value
>
> in Lua.

Elements of multi-dimension arrays (not jagged arrays) are accessed using a Lua table (array) of indexes as the index.

> If a Lua variable `a` is an instance of the CLI array type `object[,]` then the first element of the array is read using
>
>     local value = a[{0, 0}]
>
> and is written using
>
>     a[{0, 0}] = value
>
> in Lua.

*Keep in mind that Lua arrays are 1-indexed and CLI arrays are 0-indexed.  Using both in the same program requires particular attention to avoid off-by-one errors.*

## 2.0 Type Members ##

Lua-CLR Bridge provides semi-transparent access to public members of CLI types.  Unlike Lua, getting a non-existent member from a CLI object throws `MissingMemberException` rather than returning `nil`, and setting a non-existent field or property also throws `MissingMemberException`.

Static members of a CLI type cannot be accessed from an instance of that type – see § 2.5.

### 2.1 Events ###

CLI events are modified using the `CLR.AddHandler` and `CLR.RemoveHandler` functions.  A handler for an event can be created from a Lua function, a CLI method group, or a CLI delegate.

> If a Lua variable `c` is an instance of the C# class `C`
>
>     class C
>     {
>         public event Func<int> E;
>     }
>
> then a Lua function is added to and removed from the event `E`, respectively, using
>
>     local f = function (i) print(i) end
>     CLR.AddHandler(c.E, f)
>     CLR.RemoveHandler(c.E, f)
>
> in Lua.


### 2.2 Fields ###

CLI fields are accessed in the same manner as Lua table fields.

> If a Lua variable `c` is an instance of the C# class `C`
>
>     class C
>     {
>         public object X;
>     }
>
> then the instance field `X` is read using
>
>     local value = c.X
>
> and is written using
>
>     c.X = value
>
> in Lua.

### 2.3 Methods ###

CLI methods are accessed in partially the same manner as Lua methods.  Unlike Lua methods, CLI methods (whether static or instance) are called using a dot (`.`) rather than a colon (`:`).

The runtime binding of methods performed by Lua-CLR Bridge approximates the compile-time binding of methods in C#, including overloading, default parameters, `out` and `ref` parameters, and `params` parameters.

Because Lua does not support passing arguments by reference, `out` and `ref` parameters are returned, in order, following the return value of the function (if any) using the multiple-return feature of Lua.  Even for `out` parameters, an argument must be passed in to facilitate overload resolution; in most cases `out` arguments should be `nil` or `0`.

> If a Lua variable `c` is an instance of the C# class `C`
>
>     class C
>     {
>         public void F() { … }
>         public void F( int i ) { … }
>         public string F( string s ) { … }
>         public void G( int i = 0 ) { … }
>         public int G( out int[] a ) { … }
>         public void H( ref int i ) { … }
>         public void H( params int[] a ) { … }
>     }
>
> then each instance method is invoked, respectively, using
>
>     c.F()
>     c.F(1)
>     local ret = c.F('hello')
>     c.G(); c.G(1)
>     local ret, out = c.G(nil)
>     local ref = c.H(1)
>     c.H(); c.H(1, 2, 3)
>
> in Lua.

If no method overload is applicable then `MissingMethodException` will be thrown.  If multiple method overloads are applicable but none is most specific then `AmbiguousMatchException` will be thrown.  (Whether a method is applicable and most specific is determined in approximately the same manner as in C# with allowances for narrowing numeric values.)

In cases where Lua-CLR Bridge cannot determine which overloaded method should be invoked based on the arguments, it is necessary to use the signature binding hints mechanism specified in § 2.10.

### 2.4 Properties ###

Non-indexed CLI properties are accessed in the same manner as fields. 

> If a Lua variable `c` is an instance of the C# class `C`
>
>     class C
>     {
>         public object X { get; set; }
>     }
>
> then the instance property `X` is read using
>
>     local value = c.X
>
> and is written using
>
>     c.X = value
>
> in Lua.

Indexed properties are accessed similarly to an array-type non-indexed property.  C# code can produce only one kind of indexed property, an indexer.  Typically the name of the indexer property is `Item`, but it can be specified otherwise.

> If a Lua variable `c` is an instance of the C# class `C`
>
>     class C
>     {
>         public object this[ object i ] { get { … } set { … } }
>     }
>
> then the indexed instance property (indexer) is read using
>
>     local value = c.Item[x]
>
> and is written using
>
>     c.Item[x] = value
>
> in Lua.

Multi-dimension indexed properties are accessed using the same syntax as multi-dimension arrays (§ 1.4).

> If a Lua variable `c` is an instance of the C# class `C`
>
>     class C
>     {
>         public object this[ object i, object j ] { get { … } set { … } }
>     }
>
> then the indexed instance property (indexer) is read using
>
>     local value = c.Item[{x, y}]
>
> and is written using
>
>     c.Item[{x, y}] = value
>
> in Lua.

### 2.5 Static Members ###

Static members of CLI types are accessed from within Lua using the `CLR.Static` lookup table.  This lookup table returns instances of `CLRStaticContext` which are used internally by Lua-CLR Bridge to represent a type reference.

> For the C# class `N.C`
>
>     namespace N
>     {
>         public class C
>         {
>             public static object X;
>         }
>     }
>
> the static field `X` is read using
>
>     local C = CLR.Static['N.C']
>     local value = C.X
>
> in Lua.

This mechanism will find only exported types in loaded CLI assemblies.

### 2.6 Constructors ###

CLI constructors are invoked by performing a function call on a `CLRStaticContext` object.  The mechanism for the runtime binding of constructors is the same as for methods as specified in § 2.3.

> For the C# class `N.C`
>
>     namespace N
>     {
>         public class C
>         {
>             public C( int i ) { … }
>         }
>     }
>
> an instance `c` is constructed using
>
>     local C = CLR.Static['N.C']
>     local c = C(1)
>
> in Lua.

In cases where Lua-CLR Bridge cannot determine which overloaded constructor should be invoked based on the arguments, it is necessary to use the signature binding hints mechanism specified in § 2.10.

### 2.7 Nested Types ###

Nested CLI types are accessed in the same manner as most other static members.  Accessing a nested type produces an instance of `CLRStaticContext` which provides access to static members and constructors.

> For the C# class `N.C`
>
>     namespace N
>     {
>         public class C
>         {
>             public class T
>             {
>                 public static object X;
>             }
>         }
>     }
>
> the static field `X` in the nested type `T` is accessed using
>
>     local value = CLR.Static['N.C'].T.X
>
> in Lua.

### 2.8 Operators ###

Lua operators operating on CLI objects that have a corresponding operator overloaded will invoke that operator.

> If Lua variables `c1` and `c2` are instances of the C# class `C`
>
>     class C
>     {
>         public static C operator +( C lhs, C rhs ) { … }
>     }
>
> the addition operator is invoked using
>
>     local value = c1 + c2
>
> in Lua.

The determination of which operator overload is invoked follows CLI rather than Lua semantics.  Whereas Lua attempts to invoke an operator on the left-hand-side object and only then attempts to invoke the operator on the right-hand-side object, the CLI instead builds a set of the operator overloads defined in the types of the operands and invokes the most specific one (determined using the same process as method overloads).

CLI operators (particularly those which do not have an associated Lua operator) may be invoked using the "SpecialName" mechanism specified in § 2.9.1.

#### 2.8.1 Equality and Inequality ####

The equality and inequality operators deserve special attention – partially because Lua only allows half of them to be redefined and partially because they are implicitly defined for some CLI objects and not for others.

Lua allows some control over the equal (`==`), less-than (`<`), and less-than-or-equal operators (`<=`).  The not-equal (`~=`), greater-than (`>`), and greater-than-or-equal (`>=`) operators cannot be controlled but are defined in terms of the controllable operators as specified in the *Lua 5.2 Reference Manual*.  As a result, the non-controllable Lua operators will not invoke the expected CLI operators but will instead invoke the CLI operators that they are defined in terms of.  For example, the not-equal (`~=`) operator in Lua will invoke the CLI equality operator override and negate it.  (If the CLI equality and inequality operator overrides represent sanely defined relations, it should be unimportant which operator is invoked; however invoking a specific CLI operator may be performed using the "SpecialName" mechanism specified in § 2.9.1.)

> If Lua variables `c` and `d` are instances of a C# class `C`
>
>     class C
>     {
>         public static bool operator <( C lhs, C rhs ) { … }
>         public static bool operator >=( C lhs, C rhs ) { … }
>     }
>
> the CLI less-than (rather than greater-than-or-equal) operator overload will be invoked by
>
>     local ne = c >= d
>
> in Lua; if the expressions `c >= d` and `!(d < c)` are equivalent then the results will be as expected.

Lua allows slightly less control over the equal (`==`) operator (and thereby the not-equal (`~=`) operator as well) than other operators, and this results in a small quirk.  If CLI objects are tested for equality or not-equality, the CLI equality operator override will not be invoked if the CLI objects are (referentially) the same.  (If the CLI equality operator override represents a sanely defined relation, it should be unimportant whether the operator is invoked to test equality of an object against itself; however invoking a specific CLI operator may be performed using the "SpecialName" mechanism specified in § 2.9.1.)

> If a Lua variable `c` is an instance of a C# class `C`
>
>     class C
>     {
>         public static bool operator ==( C lhs, C rhs ) { … }
>     }
>
> the CLI equality operator overload will not be invoked by
>
>     local d = c
>     local eq = c == d
>
> in Lua; the value of `eq` will be `true` without invoking the CLI operator because the Lua objects are the same (which is not necessarily equivalent to being equal).

The equality and inequality operators are implicitly defined for CLI enumeration types.  If two enumeration values are of the same enumeration type, they will be compared numerically.

For CLI reference types (`class`es in C#), the Lua equal (`==`) operator (and thereby the not-equal (`~=`) operator as well) is implicitly defined as a reference equality comparison.  For non-enumeration value types (`struct`s in C#), the Lua equal (`==`) operator (and thereby the not-equal (`~=`) operator as well) is not implicitly defined.

The equal (`==`) and not-equal (`~=`) operators will **always** return `false` and `true`, respectively, when comparing a CLI objects to Lua values.  In most cases the `Equals` method of the CLI object provides the desired behavior and should be used instead.

### 2.9 Member Binding Hints ###

Member binding hints provide a mechanism for disambiguating member access in situations where Lua-CLR Bridge does not have enough information to determine which member is being accessed.

Member binding hints are provided by performing a function call on an object (including instances of `CLRStaticContext`) with a Lua table as the only argument.  The elements of the Lua table provide the binding hints.  If unrecognized binding hints are provided, `LuaCLRBridge.BindingHintsException` is thrown.

*Note:*  Member binding hints and signature binding hints (§ 2.10) use similar syntax, so if signature binding-hints are to be specified for a constructor then a member binding-hints table (which may be empty) must be provided even when no hints are otherwise necessary.

#### 2.9.1 "Special Name" Members ####

"Special name" methods are accessible using a member binding-hint table with "SpecialName" set to true.

CLI events consist of two "special name" methods:  an add method and a remove method – `add_`*\<name>* and `remove_`*\<name>*.

CLI properties consist of one or two "special name" methods:  a get method, a set method – `get_`*\<name>* and `set_`*\<name>* – or both.

CLI operators consist of one "special name" method:  an operator method – op_*\<name>*.  The names of operators are specified in ECMA-335 § I.10.3.

> The single-dimension indexed instance property from the example in § 2.4 can be read using
>
>     local value = c{SpecialName = true}.get_Item(x)
>
> and can be written using
>
>     c{SpecialName = true}.set_Item(x, value)
>
> in Lua.

### 2.10 Signature Binding Hints ###

Signature binding hints provide a mechanism for disambiguating constructor/method invocation in situations where Lua-CLR Bridge does not have enough information to determine which constructor/method is being invoked.

Signature binding hints are provided by performing a function call on a method/constructor group with a Lua table as the only argument.  The elements of the Lua table provide the binding hints.  If unrecognized binding hints are provided, `LuaCLRBridge.BindingHintsException` is thrown.

A signature binding-hint table is a Lua table (array) with elements that are either the (unqualified) name of a type as a string or the type itself.  These elements correspond to the types of the parameters of the overload that should be invoked.

> If a Lua variable `c` is an instance of a C# class `C`
>
>     class C
>     {
>         public void F( int l, double r ) { … }
>         public void F( double l, int r ) { … }
>     }
>
> then each instance method can be invoked, respectively, using
>
>     c.F{'Int32', 'Double'}(1, 2)
>     c.F{CLR.Type['System.Double'],
>         CLR.Type['System.Int32']}(1, 2)
>
> in Lua.

*Note:*  Signature binding hints and member binding hints (§ 2.9) use similar syntax, so if signature binding-hints are to be specified for a constructor then a member binding-hints table (which may be empty) must be provided even when no hints are otherwise necessary.

*Note:*  Signature binding hints use the same syntax as constructor/method invocation, so if a constructor/method is to be called with a Lua table as its only argument then a signature binding-hints table (which may be empty) must be provided even when no hints are otherwise necessary.

> A worst-case scenario of binding-hint verbosity (primarily provided to demonstrate how the multiple kinds of binding-hints are combined) follows:
>
> C#:
>
>     namespace N
>     {
>         public class C
>         {
>             public C( LuaTable t ) { … }
>             public object this[ object i ] { get { … } }
>         }
>     }
>
> Lua:
>
>     local C = CLR.Static['N.C']
>     local value = C{}{}({ x = true }){SpecialName = true}.get_Item('x')
>
> Following `C`, (1) the first Lua table is an empty member binding hint on the type `C`, (2) the second is an empty signature binding hint for the subsequent constructor call, (3) the third is the argument to the constructor (the parenthesis around a single table argument are unnecessary in Lua but are helpful to show that it is where an actual invocation is performed), and (4) the fourth is the member binding hint on the newly constructed instance of `C`.
>
> Note that this worst-case only occurs when a constructor has a single Lua table parameter.  If the constructor had any other parameter(s) then the two empty Lua tables would have been unnecessary.  Also, it is unusual to invoke a member from a newly constructed instance.

## 3.0 Generics ##

Type arguments for generic types are specified alongside member binding hints (§ 2.9).  Type arguments are specified in the array portion of the hint table.

> For the generic C# class ``N.C`2``
>
>     namespace N
>     {
>         public class C<T, U>
>         {
>             public C() { … }
>         }
>     }
>
> an instance `c` is constructed using
>
>     local C = CLR.Static['N.C`2']
>     local c = C{CLR.Type['System.Int32'],
>                 CLR.Type['System.Double']}()
>
> in Lua.

*Note:*  Names for generic classes are generated by the C# compiler by appending a backtick (\`) and the number of type parameters that the class has.

Type arguments for generic methods may be explicitly specified alongside signature binding hints (§ 2.10).  Type arguments are specified by an entry in the hint table with a key of `_` and a value that is a Lua table (array) of types.

Type inference for generic methods is implemented.  The runtime type inference performed by Lua-CLR Bridge approximates the compile-time type inference of C#, including covariant and contravariant type arguments.

> If a Lua variable `c` is an instance of a C# class `C`
>
>     class C
>     {
>         public void F<T>() { … }
>         public void F<T>( T t ) { … }
>     }
>
> then each instance method can be invoked, respectively, using
>
>     c.F{_ = {CLR.Type['System.Double']}}()
>     c.F(1); c.F('x')
>
> in Lua.

## 4.0 Exceptions ##

CLI Exceptions are propagated through Lua using its error/protected-call mechanism.  When `pcall` returns false due to a thrown exception, the exception object will be the second return value rather than an error message.

## 5.0 Iteration ##

Lua for-each iteration of some CLR collection types is supported via some `CLR` helper functions.

| Helper   | Supports                       |
|----------|--------------------------------|
| `Items`  | `IEnumerable<…>`               |
| `IPairs` | single-dimensional array       |
| `Pairs`  | `IEnumerable<KeyPairValue<…>>` |

> If a Lua variable `c` is an instance of a C# class `List<T>` then it can be iterated using
>
>     for e in CLR.Item(c) do
>         --
>     end
>
> in Lua.

> If a Lua variable `c` is an instance of a C# type `T[]` then it can be iterated using 
>
>     for i, e in CLR.IPairs(c) do
>         --
>     end
>
> in Lua.

> If a Lua variable `c` is an instance of a C# class `Dictionary<T, U>` then it can be iterated using 
>
>     for k, v in CLR.Pairs(c) do
>         --
>     end
>
> in Lua.
