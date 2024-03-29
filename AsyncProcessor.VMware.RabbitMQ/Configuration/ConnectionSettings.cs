﻿using System;
using RabbitMQ.Client;


namespace AsyncProcessor.VMware.RabbitMQ.Configuration
{
    public class ConnectionSettings
    {
        public string UserName { get; set; } = ConnectionFactory.DefaultUser;
        public string Password { get; set; } = ConnectionFactory.DefaultPass;
        public string VirtualHost { get; set; } = ConnectionFactory.DefaultVHost;
        public string HostName { get; set; }
        public int Port { get; set; } = AmqpTcpEndpoint.UseDefaultPort;
    }
}
