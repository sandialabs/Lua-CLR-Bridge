/* LuaCLRBridge
 * Copyright 2014 Sandia Corporation.
 * Under the terms of Contract DE-AC04-94AL85000 with Sandia Corporation,
 * the U.S. Government retains certain rights in this software.
 * 
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */
namespace LuaCLRBridge
{
    using System;

#pragma warning disable 1591
    /*! \cond suppress Doxygen */

    [LuaHideInheritedMembers]
    [Serializable]
    public struct CLRInt64
    {
        internal readonly Int64 _value;

        public CLRInt64( Int64 value )
        {
            _value = value;
        }

        public double Value
        {
            get { return _value; }
        }

        #region Addition

        public static object operator +( CLRInt64 lhs, CLRInt64 rhs )
        {
            try
            {
                return new CLRInt64(checked(lhs._value + rhs._value));
            }
            catch (OverflowException)
            {
                return (double)lhs._value + (double)rhs._value;
            }
        }

        public static object operator +( double lhs, CLRInt64 rhs )
        {
            if (lhs % 1 != 0)
                return lhs + rhs._value;

            try
            {
                return new CLRInt64(checked((Int64)lhs + rhs._value));
            }
            catch (OverflowException)
            {
                return lhs + rhs._value;
            }
        }

        public static object operator +( CLRInt64 lhs, double rhs )
        {
            if (rhs % 1 != 0)
                return lhs._value + rhs;

            try
            {
                return new CLRInt64(checked(lhs._value + (Int64)rhs));
            }
            catch (OverflowException)
            {
                return lhs._value + rhs;
            }
        }

        #endregion

        #region Subtraction

        public static object operator -( CLRInt64 lhs, CLRInt64 rhs )
        {
            try
            {
                return new CLRInt64(checked(lhs._value - rhs._value));
            }
            catch (OverflowException)
            {
                return (double)lhs._value - (double)rhs._value;
            }
        }

        public static object operator -( double lhs, CLRInt64 rhs )
        {
            if (lhs % 1 != 0)
                return lhs - rhs._value;

            try
            {
                return new CLRInt64(checked((Int64)lhs - rhs._value));
            }
            catch (OverflowException)
            {
                return lhs - rhs._value;
            }
        }

        public static object operator -( CLRInt64 lhs, double rhs )
        {
            if (rhs % 1 != 0)
                return lhs._value - rhs;

            try
            {
                return new CLRInt64(checked(lhs._value - (Int64)rhs));
            }
            catch (OverflowException)
            {
                return lhs._value - rhs;
            }
        }

        #endregion

        #region Muliplication

        public static object operator *( CLRInt64 lhs, CLRInt64 rhs )
        {
            try
            {
                return new CLRInt64(checked(lhs._value * rhs._value));
            }
            catch (OverflowException)
            {
                return (double)lhs._value * (double)rhs._value;
            }
        }

        public static object operator *( double lhs, CLRInt64 rhs )
        {
            if (lhs % 1 != 0)
                return lhs * rhs._value;

            try
            {
                return new CLRInt64(checked((Int64)lhs * rhs._value));
            }
            catch (OverflowException)
            {
                return lhs * rhs._value;
            }
        }

        public static object operator *( CLRInt64 lhs, double rhs )
        {
            if (rhs % 1 != 0)
                return lhs._value * rhs;

            try
            {
                return new CLRInt64(checked(lhs._value * (Int64)rhs));
            }
            catch (OverflowException)
            {
                return lhs._value * rhs;
            }
        }

        #endregion

        #region Division

        public static object operator /( CLRInt64 lhs, CLRInt64 rhs )
        {
            try
            {
                return new CLRInt64(checked(lhs._value / rhs._value));
            }
            catch (OverflowException)
            {
                return (double)lhs._value / (double)rhs._value;
            }
        }

        public static object operator /( double lhs, CLRInt64 rhs )
        {
            if (lhs % 1 != 0)
                return lhs / rhs._value;

            try
            {
                return new CLRInt64(checked((Int64)lhs / rhs._value));
            }
            catch (OverflowException)
            {
                return lhs / rhs._value;
            }
        }

        public static object operator /( CLRInt64 lhs, double rhs )
        {
            if (rhs % 1 != 0)
                return lhs._value / rhs;

            try
            {
                return new CLRInt64(checked(lhs._value / (Int64)rhs));
            }
            catch (OverflowException)
            {
                return lhs._value / rhs;
            }
        }

        #endregion

        #region Modulus

        public static object operator %( CLRInt64 lhs, CLRInt64 rhs )
        {
            try
            {
                return new CLRInt64(checked(lhs._value % rhs._value));
            }
            catch (OverflowException)
            {
                return (double)lhs._value % (double)rhs._value;
            }
        }

        public static object operator %( double lhs, CLRInt64 rhs )
        {
            if (lhs % 1 != 0)
                return lhs % rhs._value;

            try
            {
                return new CLRInt64(checked((Int64)lhs % rhs._value));
            }
            catch (OverflowException)
            {
                return lhs % rhs._value;
            }
        }

        public static object operator %( CLRInt64 lhs, double rhs )
        {
            if (rhs % 1 != 0)
                return lhs._value % rhs;

            try
            {
                return new CLRInt64(checked(lhs._value % (Int64)rhs));
            }
            catch (OverflowException)
            {
                return lhs._value % rhs;
            }
        }

        #endregion

        #region Negation

        public static object operator -( CLRInt64 operand )
        {
            try
            {
                return new CLRInt64(checked(-operand._value));
            }
            catch (OverflowException)
            {
                return -(double)operand._value;
            }
        }

        #endregion

        #region Equality

        public static bool operator ==( CLRInt64 lhs, CLRInt64 rhs )
        {
            try
            {
                return checked(lhs._value == rhs._value);
            }
            catch (OverflowException)
            {
                return (double)lhs._value == (double)rhs._value;
            }
        }

        public static bool operator !=( CLRInt64 lhs, CLRInt64 rhs )
        {
            return !(lhs == rhs);  // never used
        }

        public static bool operator ==( double lhs, CLRInt64 rhs )
        {
            if (lhs % 1 != 0)
                return lhs == rhs._value;

            try
            {
                return checked((Int64)lhs == rhs._value);
            }
            catch (OverflowException)
            {
                return lhs == rhs._value;
            }
        }

        public static bool operator !=( double lhs, CLRInt64 rhs )
        {
            return !(lhs == rhs);  // never used
        }

        public static bool operator ==( CLRInt64 lhs, double rhs )
        {
            if (rhs % 1 != 0)
                return lhs._value == rhs;

            try
            {
                return checked(lhs._value == (Int64)rhs);
            }
            catch (OverflowException)
            {
                return lhs._value == rhs;
            }
        }

        public static bool operator !=( CLRInt64 lhs, double rhs )
        {
            return !(lhs == rhs);  // never used
        }

        #endregion

        #region Inequality

        public static bool operator <=( CLRInt64 lhs, CLRInt64 rhs )
        {
            try
            {
                return checked(lhs._value <= rhs._value);
            }
            catch (OverflowException)
            {
                return (double)lhs._value <= (double)rhs._value;
            }
        }

        public static bool operator >=( CLRInt64 lhs, CLRInt64 rhs )
        {
            return !(lhs < rhs);  // never used
        }

        public static bool operator <=( double lhs, CLRInt64 rhs )
        {
            if (lhs % 1 != 0)
                return lhs <= rhs._value;

            try
            {
                return checked((Int64)lhs <= rhs._value);
            }
            catch (OverflowException)
            {
                return lhs <= rhs._value;
            }
        }

        public static bool operator >=( double lhs, CLRInt64 rhs )
        {
            return !(lhs < rhs);  // never used
        }

        public static bool operator <=( CLRInt64 lhs, double rhs )
        {
            if (rhs % 1 != 0)
                return lhs._value <= rhs;

            try
            {
                return checked(lhs._value <= (Int64)rhs);
            }
            catch (OverflowException)
            {
                return lhs._value <= rhs;
            }
        }

        public static bool operator >=( CLRInt64 lhs, double rhs )
        {
            return !(lhs < rhs);  // never used
        }

        #endregion

        #region Strict inequality

        public static bool operator <( CLRInt64 lhs, CLRInt64 rhs )
        {
            try
            {
                return checked(lhs._value < rhs._value);
            }
            catch (OverflowException)
            {
                return (double)lhs._value < (double)rhs._value;
            }
        }

        public static bool operator >( CLRInt64 lhs, CLRInt64 rhs )
        {
            return !(lhs <= rhs);  // never used
        }

        public static bool operator <( double lhs, CLRInt64 rhs )
        {
            if (lhs % 1 != 0)
                return lhs < rhs._value;

            try
            {
                return checked((Int64)lhs < rhs._value);
            }
            catch (OverflowException)
            {
                return lhs < rhs._value;
            }
        }

        public static bool operator >( double lhs, CLRInt64 rhs )
        {
            return !(lhs <= rhs);  // never used
        }

        public static bool operator <( CLRInt64 lhs, double rhs )
        {
            if (rhs % 1 != 0)
                return lhs._value < rhs;

            try
            {
                return checked(lhs._value < (Int64)rhs);
            }
            catch (OverflowException)
            {
                return lhs._value < rhs;
            }
        }

        public static bool operator >( CLRInt64 lhs, double rhs )
        {
            return !(lhs <= rhs);  // never used
        }

        #endregion

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public override bool Equals( object obj )
        {
            if (obj is CLRInt64)
                return this == (CLRInt64)obj;
            else if (obj is CLRUInt64)
                return this == (CLRUInt64)obj;
            else if (obj is double)
                return this == (double)obj;
            else
                return false;
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }

    /*! \endcond */
#pragma warning restore 1591

}
