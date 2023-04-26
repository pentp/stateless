using System;

namespace Stateless.Reflection
{
    /// <summary>
    /// Describes a method - either an action (activate, deactivate, etc.) or a transition guard
    /// </summary>
    public class InvocationInfo
    {
        readonly string _description;                     // _description can be null if user didn't specify a description

        /// <summary>
        /// Is the method synchronous or asynchronous?
        /// </summary>
        public enum Timing
        {
            /// <summary>Method is synchronous</summary>
            Synchronous,

            /// <summary>Method is asynchronous</summary>
            Asynchronous
        }

        internal InvocationInfo(Delegate action, string description, bool sync = true)
        {
            var method = action?.Method;

            // unwrap packed action delegates
            if (method?.DeclaringType == typeof(ParameterConversion))
                method = ((Delegate)action.Target).Method;

            MethodName = method?.Name;
            _description = description;
            IsAsync = !sync;
        }

        /// <summary>
        /// Creates a new instance of <see cref="InvocationInfo"/>.
        /// </summary>
        /// <param name="methodName">The name of the invoked method.</param>
        /// <param name="description">A description of the invoked method.</param>
        /// <param name="timing">Sets a value indicating whether the method is invoked asynchronously.</param>
        public InvocationInfo(string methodName, string description, Timing timing)      // description can be null if user didn't specify a description
        {
            MethodName = methodName;
            _description = description;
            IsAsync = timing == Timing.Asynchronous;
        }

        /// <summary>
        /// The name of the invoked method.  If the method is a lambda or delegate, the name will be a compiler-generated
        /// name that is often not human-friendly (e.g. "(.ctor)b__2_0" except with angle brackets instead of parentheses)
        /// </summary>
        public string MethodName { get; private set; }

        /// <summary>
        /// Text returned for compiler-generated functions where the caller has not specified a description
        /// </summary>
        public static string DefaultFunctionDescription { get; set; } = "Function";

        private static readonly char[] GeneratedFuncChars = { '<', '>' };

        /// <summary>
        /// A description of the invoked method.  Returns:
        /// 1) The user-specified description, if any
        /// 2) else if the method name is compiler-generated, returns DefaultFunctionDescription
        /// 3) else the method name
        /// </summary>
        public string Description
        {
            get
            {
                if (_description != null)
                    return _description;
                if (MethodName.IndexOfAny(GeneratedFuncChars) >= 0)
                    return DefaultFunctionDescription;
                return MethodName;
            }
        }

        internal static string GetDescription(Delegate action)
        {
            var method = action?.Method;

            // unwrap packed action delegates
            if (method?.DeclaringType == typeof(ParameterConversion))
                method = ((Delegate)action.Target).Method;

            var name = method?.Name;
            return name?.IndexOfAny(GeneratedFuncChars) >= 0 ? DefaultFunctionDescription : name;
        }

        /// <summary>
        /// Returns true if the method is invoked asynchronously.
        /// </summary>
        public bool IsAsync { get; }
    }
}
