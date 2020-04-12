using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SignalRTypeScriptHubGenerator.Test
{
    [TestClass]
    public class TraverseTypesTest
    {
        [TestMethod]
        public void GIVEN_TypesThatAreSeparatedOutsideNamespaceFilter_WHEN_TraverseTypes_AllFound()
        {
            var targetType = typeof(ITestInterface);
            var namespaceFilter = "SignalRTypeScriptHubGenerator";
            var expectedTypes = new[] {targetType, typeof(MyClass)};

            var types = SignalRTypeScriptHubGeneratorExtensions.TraverseTypes(targetType, namespaceFilter);

            CollectionAssert.AreEquivalent(expectedTypes, new List<Type>(types));
        }

        [TestMethod]
        public void GIVEN_ThereAreNestedGenericTypesWithDifferentTypes_WHEN_TraverseTypes_AllFound()
        {
            var targetType = typeof(ITestInterface2);
            var namespaceFilter = "SignalRTypeScriptHubGenerator";
            var expectedTypes = new[] { targetType, typeof(MyClass), typeof(MyClass2), typeof(MyGenericClass<MyClass>), typeof(MyGenericClass<MyClass2>) };

            var types = SignalRTypeScriptHubGeneratorExtensions.TraverseTypes(targetType, namespaceFilter);

            CollectionAssert.AreEquivalent(expectedTypes, new List<Type>(types));
        }
    }

    interface ITestInterface
    {
        Task<IEnumerable<Task<MyClass>>> Foo();
    }

    class MyClass
    {
        
    }

    class MyClass2
    {
        
    }

    interface ITestInterface2
    {
        Task<MyGenericClass<MyClass>> Foo();
        Task<MyGenericClass<MyClass2>> Foo2();
    }

    class MyGenericClass<T>
    {

    }
}
