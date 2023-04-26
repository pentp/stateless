using System;
using System.Threading.Tasks;

namespace Stateless
{
    static class ParameterConversion
    {
        private static TArg Unpack<TArg>(object[] args, int index)
        {
            if (args.Length == 0) return default;

            if ((uint)args.Length <= (uint)index)
                ThrowTooFewParameters(index, typeof(TArg));

            var arg = args[index];
            if (arg != null && arg is not TArg)
                ThrowWrongArgType(index, typeof(TArg), arg);

            return (TArg)arg;
        }

        public static void Validate(object[] args, Type[] expected)
        {
            if (args.Length == 0)
                return;

            if (args.Length > expected.Length)
                ThrowTooManyParameters(args.Length, expected.Length);

            for (int i = 0; i < expected.Length; ++i)
            {
                var argType = expected[i];

                if (args.Length <= i)
                    ThrowTooFewParameters(i, argType);

                var arg = args[i];
                if (arg != null && !argType.IsAssignableFrom(arg.GetType()))
                    ThrowWrongArgType(i, argType, arg);
            }
        }

        private static void ThrowTooManyParameters(int argsLength, int expectedLength)
            => throw new ArgumentException(string.Format(ParameterConversionResources.TooManyParameters, expectedLength, argsLength));

        private static void ThrowTooFewParameters(int index, Type argType)
            => throw new ArgumentException(string.Format(ParameterConversionResources.ArgOfTypeRequiredInPosition, argType, index));

        private static void ThrowWrongArgType(int index, Type argType, object arg)
            => throw new ArgumentException(string.Format(ParameterConversionResources.WrongArgType, index, arg.GetType(), argType));

        // Use extension methods to avoid closure allocations.
        // The unpacked delegate is stored directly inside the packed delegate.

        public static void Unpack<TArg0>(this Action<TArg0> action, object[] args)
            => action(Unpack<TArg0>(args, 0));

        public static void Unpack<TArg0, TArg1>(this Action<TArg0, TArg1> action, object[] args)
            => action(Unpack<TArg0>(args, 0), Unpack<TArg1>(args, 1));

        public static void Unpack<TArg0, TArg1, TArg2>(this Action<TArg0, TArg1, TArg2> action, object[] args)
            => action(Unpack<TArg0>(args, 0), Unpack<TArg1>(args, 1), Unpack<TArg2>(args, 2));

        public static R Unpack<TArg0, R>(this Func<TArg0, R> action, object[] args)
            => action(Unpack<TArg0>(args, 0));

        public static R Unpack<TArg0, TArg1, R>(this Func<TArg0, TArg1, R> action, object[] args)
            => action(Unpack<TArg0>(args, 0), Unpack<TArg1>(args, 1));

        public static R Unpack<TArg0, TArg1, TArg2, R>(this Func<TArg0, TArg1, TArg2, R> action, object[] args)
            => action(Unpack<TArg0>(args, 0), Unpack<TArg1>(args, 1), Unpack<TArg2>(args, 2));

        public static void Unpack<TArg0, T>(this Action<TArg0, T> action, T t, object[] args)
            => action(Unpack<TArg0>(args, 0), t);

        public static void Unpack<TArg0, TArg1, T>(this Action<TArg0, TArg1, T> action, T t, object[] args)
            => action(Unpack<TArg0>(args, 0), Unpack<TArg1>(args, 1), t);

        public static void Unpack<TArg0, TArg1, TArg2, T>(this Action<TArg0, TArg1, TArg2, T> action, T t, object[] args)
            => action(Unpack<TArg0>(args, 0), Unpack<TArg1>(args, 1), Unpack<TArg2>(args, 2), t);

        public static Task Unpack<TArg0, T>(this Func<TArg0, T, Task> action, T t, object[] args)
            => action(Unpack<TArg0>(args, 0), t);

        public static Task Unpack<TArg0, TArg1, T>(this Func<TArg0, TArg1, T, Task> action, T t, object[] args)
            => action(Unpack<TArg0>(args, 0), Unpack<TArg1>(args, 1), t);

        public static Task Unpack<TArg0, TArg1, TArg2, T>(this Func<TArg0, TArg1, TArg2, T, Task> action, T t, object[] args)
            => action(Unpack<TArg0>(args, 0), Unpack<TArg1>(args, 1), Unpack<TArg2>(args, 2), t);
    }
}
