using Otc.ExceptionHandling.Abstractions;
using Otc.ExceptionHandling.Abstractions.Configurations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Otc.ExceptionHandling.Configuration
{
    public class ExceptionHandlerConfiguration : IExceptionHandlerConfiguration
    {
        public ExceptionHandlerConfiguration(Action<IExceptionHandlerConfigurationExpression> action)
        {
            this.Events = new List<IExceptionHandlerEvent>();
            behaviors = new Dictionary<string, ForExceptionBehavior>();
            Build(action);
        }

        private Dictionary<string, ForExceptionBehavior> behaviors;

        public List<IExceptionHandlerEvent> Events { get; }

        public bool HasBehaviors => behaviors.Any();

        public ForExceptionBehavior ValidateBehavior(Exception ex)
        {
            return ValidateBehavior(ex.GetType().Name);
        }

        public ForExceptionBehavior ValidateBehavior(string exceptionName)
        {
            if (behaviors.TryGetValue(exceptionName, out ForExceptionBehavior behavior))
                return behavior;

            return null;
        }

        private void Build(Action<IExceptionHandlerConfigurationExpression> action)
        {
            var configurationExpression = new ExceptionHandlerConfigurationExpression();

            action(configurationExpression);

            this.Events.AddRange(configurationExpression.Events);

            this.behaviors = new Dictionary<string, ForExceptionBehavior>(configurationExpression.Behaviors);
        }
    }
}
