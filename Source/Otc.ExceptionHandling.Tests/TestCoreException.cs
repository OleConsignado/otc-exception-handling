using Otc.DomainBase.Exceptions;

namespace Otc.ExceptionHandling.Tests
{
    public class TestCoreException : CoreException
    {
        public TestCoreException() : base("teste")
        {

        }
        public override string Key => "TestCoreException";
    }
}
