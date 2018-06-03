using System;
using System.Net.Http;

namespace Common.Abstractions
{
    public interface IHttpMessageHandlerFactory
    {
        Uri BaseUri { get; }

        HttpMessageHandler Create();
    }
}