using System;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using System.Linq;

namespace Techsola
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
    public sealed class RequireOnAllTestMethodsAttribute : Attribute, ITestAction
    {
        private readonly Type[] attributeTypes;

        public RequireOnAllTestMethodsAttribute(params Type[] attributeTypes)
        {
            this.attributeTypes = attributeTypes;
        }

        public ActionTargets Targets => ActionTargets.Test;

        public void BeforeTest(ITest test)
        {
            var missingTypes = attributeTypes
                .Except(
                    from data in test.Method!.MethodInfo.GetCustomAttributesData()
                    select data.AttributeType)
                .ToList();

            if (missingTypes.Any())
            {
                Assert.Fail("The test method is missing " + string.Join(", ", missingTypes) + ".");
            }
        }

        public void AfterTest(ITest test)
        {
        }
    }
}
