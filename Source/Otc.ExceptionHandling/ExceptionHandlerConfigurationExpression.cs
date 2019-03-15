using Otc.ExceptionHandling.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Otc.ExceptionHandling
{
    public class ExceptionHandlerConfigurationExpression : IExceptionHandlerConfigurationExpression
    {
        public ExceptionHandlerConfigurationExpression()
        {
            Events = new List<IExceptionHandlerEvent>();
            Behaviors = new Dictionary<Type, ForExceptionBehavior>();
        }

        public List<IExceptionHandlerEvent> Events { get; }

        public Dictionary<Type, ForExceptionBehavior> Behaviors { get; }

        public IExceptionHandlerConfigurationExpression AddEvent(IExceptionHandlerEvent @event)
        {
            Events.Add(@event);

            return this;
        }

        public IExceptionHandlerConfigurationExpression AddEvent<TEvent>() where TEvent : IExceptionHandlerEvent, new() => AddEvent(new TEvent());


        public IExceptionHandlerConfigurationExpression ForException<TException>(int statusCode, ExceptionHandlerBehavior behavior = ExceptionHandlerBehavior.ClientError) where TException : Exception
        {
            ForException(typeof(TException), statusCode, behavior);

            return this;
        }

        public IExceptionHandlerConfigurationExpression ForException(Type exception, int statusCode, ExceptionHandlerBehavior behavior = ExceptionHandlerBehavior.ClientError)
        {
            Behaviors.Add(exception, new ForExceptionBehavior() { StatusCode = statusCode, Behavior = behavior });

            return this;
        }
    }
}
