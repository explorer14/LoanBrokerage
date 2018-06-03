using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LoanRequestSender.Filter
{
    public interface IRegisteredBank
    {
        string Name { get; }
        Uri BaseUri { get; }
        string Endpoint { get; }
        string HttpMethod { get; }
    }

    public class ABNAmro : IRegisteredBank
    {
        public Uri BaseUri =>
            new Uri("http://loclhost:9009");

        public string Endpoint =>
            "api/loan-request";

        public string HttpMethod =>
            "PUT";

        public string Name =>
            "ABN Amro";
    }

    public class INGVysya : IRegisteredBank
    {
        public Uri BaseUri =>
            new Uri("http://loclhost:9889");

        public string Endpoint =>
            "api/loan";

        public string HttpMethod =>
            "PUT";

        public string Name =>
            "ING Vysya";
    }

    internal sealed class RegisteredBanks
    {
        public static IReadOnlyCollection<IRegisteredBank> All()
        {
            var registeredBankTypes = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(x => x.Implements(typeof(IRegisteredBank)))
                .ToList()
                .AsReadOnly();

            List<IRegisteredBank> registeredBanks =
                new List<IRegisteredBank>();

            foreach (var type in registeredBankTypes)
                registeredBanks.Add(
                    Activator.CreateInstance(type) as IRegisteredBank);

            return registeredBanks;
        }
    }

    internal static class TypeExtensions
    {
        /// <summary>
        /// Check whether the current type implements an interface. The check involves comparing the
        /// interface names in a case sensitive way so they need to be exact match.
        /// </summary>
        /// <param name="type">The current <see cref="Type"/> instance</param>
        /// <param name="interfaceType">The interface type</param>
        /// <returns>true if the current type implements the interface type, false otherwise</returns>
        public static bool Implements(this Type type, Type interfaceType)
            => type?.GetInterfaces().Count(x => x.Name == interfaceType?.Name) > 0;
    }
}