using Otc.ExceptionHandling.Abstractions;
using Otc.ExceptionHandling.Abstractions.Configurations;
using Otc.ExceptionHandling.Abstractions.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Otc.ExceptionHandling.Configuration
{
    public class ExceptionHandlerConfigurationExpression : IExceptionHandlerConfigurationExpression
    {
        public ExceptionHandlerConfigurationExpression()
        {
            Events = new List<IExceptionHandlerEvent>();
            Behaviors = new Dictionary<string, ForExceptionBehavior>();
        }

        public List<IExceptionHandlerEvent> Events { get; }

        public Dictionary<string, ForExceptionBehavior> Behaviors { get; }

        public IExceptionHandlerConfigurationExpression AddEvent(IExceptionHandlerEvent @event)
        {
            Events.Add(@event);

            return this;
        }

        public IExceptionHandlerConfigurationExpression AddEvent<TEvent>() where TEvent : IExceptionHandlerEvent, new() => AddEvent(new TEvent());


        public IExceptionHandlerConfigurationExpression ForException<TException>(int statusCode, ExceptionHandlerBehavior behavior = ExceptionHandlerBehavior.Expose) where TException : Exception
        {
            ForException(typeof(TException).Name, statusCode, behavior);

            return this;
        }

        public IExceptionHandlerConfigurationExpression ForException(string exception, int statusCode, ExceptionHandlerBehavior behavior = ExceptionHandlerBehavior.Expose)
        {
            Behaviors.Add(exception, new ForExceptionBehavior() { StatusCode = statusCode, Behavior = behavior });

            return this;
        }
    }
}
