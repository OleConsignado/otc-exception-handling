using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Otc.SwaggerSchemaFiltering
{
    internal class RemoveExceptionPropertiesDocumentFilter : IDocumentFilter
    {
        private static void GetAllInstancePublicPropertiesComplexTypesHelper(Type type, IList<Type> result)
        {
            IList<Type> propertiesTypes = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !p.PropertyType.IsPrimitive && !typeof(string).IsAssignableFrom(p.PropertyType))
                .Select(p => p.PropertyType)
                .ToList();

            foreach (Type propertyType in propertiesTypes)
            {
                var typeCandidate = propertyType;

                // extract T from IEnumerable<T>
                while (typeof(IEnumerable).IsAssignableFrom(typeCandidate) && typeCandidate.IsGenericType)
                {
                    while (typeCandidate.GetGenericArguments().Count() > 1)
                    {
                        typeCandidate = typeCandidate.BaseType;
                    }

                    typeCandidate = propertyType.GetGenericArguments().Single();
                }

                // extract T from T[]
                while (typeCandidate.IsArray)
                {
                    typeCandidate = typeCandidate.GetElementType();
                }

                if (!typeof(IEnumerable).IsAssignableFrom(typeCandidate) && !result.Contains(typeCandidate))
                {
                    result.Add(typeCandidate);
                    GetAllInstancePublicPropertiesComplexTypesHelper(typeCandidate, result);
                }
            }
        }

        private static object lockPad = new object();
        private static IEnumerable<string> exceptionInstancePublicPropertiesComplexTypeNames = null;

        private static IEnumerable<Type> GetAllInstancePublicPropertiesComplexTypes(Type type)
        {
            var result = new List<Type>();
            GetAllInstancePublicPropertiesComplexTypesHelper(type, result);
            return result;
        }

        private void RemoveItemFromDictionaryIfExists<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary.Remove(key);
            }
        }

        // IDocumentFilter.Apply is called every schema (/swagger/v*/swagger.json) call.
        // So, exception properties type names is being cached to avoid to extract it multiple times. 
        private static IEnumerable<string> ExceptionInstancePublicPropertiesComplexTypeNames
        {
            get
            {
                if (exceptionInstancePublicPropertiesComplexTypeNames == null)
                {
                    lock (lockPad)
                    {
                        if (exceptionInstancePublicPropertiesComplexTypeNames == null) // re-test
                        {
                            exceptionInstancePublicPropertiesComplexTypeNames =
                                GetAllInstancePublicPropertiesComplexTypes(typeof(Exception))
                                .Select(t => t.Name)
                                .Union(new string[] { nameof(IntPtr) });
                        }
                    }
                }

                return exceptionInstancePublicPropertiesComplexTypeNames;
            }
        }

        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var typeName in ExceptionInstancePublicPropertiesComplexTypeNames)
            {
                RemoveItemFromDictionaryIfExists(swaggerDoc.Components.Schemas, typeName);
            }
        }
    }
}