// ***********************************************************************
// Copyright (c) 2016 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Reflection;

namespace NUnit.Engine
{
    internal class TestAssemblyWrapper
    {
        private readonly Assembly _testAssembly;
        private readonly object _frameworkController;

        private Type FrameworkControllerType => _frameworkController.GetType();
        public string FullName => _testAssembly.FullName;

        internal TestAssemblyWrapper(Assembly testAssembly, object frameworkController)
        {
            _testAssembly = testAssembly;
            _frameworkController = frameworkController;
        }

        internal object ExecuteMethod(string methodName, params object[] args)
        {
            var method = FrameworkControllerType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            return ExecuteMethod(method, args);
        }

        internal object ExecuteMethod(string methodName, Type[] ptypes, params object[] args)
        {
            var method = FrameworkControllerType.GetMethod(methodName, ptypes);
            return ExecuteMethod(method, args);
        }

        private object ExecuteMethod(MethodInfo method, params object[] args)
        {
            if (method == null)
            {
                throw new NUnitPortableDriverException(NUnitPortableDriver.INVALID_FRAMEWORK_MESSAGE);
            }
            return method.Invoke(_frameworkController, args);
        }
    }
}
