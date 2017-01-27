﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.SumoLogic.Sinks
{
    /// <summary>
    /// Sink for sending logs to Sumo Logic
    /// </summary>
    public class SumoLogicSink : PeriodicBatchingSink
    {
        private readonly string _endpointUrl;
        private readonly string _sourceName;
        private readonly ITextFormatter _textFormatter;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// The default maximum number of events to include in a single batch.
        /// </summary>
        public const int DefaultBatchSizeLimit = 10;

        /// <summary>
        /// Sumo Logic default source name
        /// </summary>
        public const string DefaultSourceName = "Serilog";
        
        /// <summary>
        /// The default period.
        /// </summary>
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Sink for sending logs to Sumo Logic
        /// </summary>
        /// <param name="endpointUrl">Sumo Logic endpoint URL to send logs to</param>
        /// <param name="sourceName">Sumo Logic source name</param>
        /// <param name="textFormatter">Supplies how logs should be formatted</param>
        /// <param name="batchSizeLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        public SumoLogicSink(
            string endpointUrl,
            string sourceName,
            ITextFormatter textFormatter,
            int batchSizeLimit, 
            TimeSpan period) : base(batchSizeLimit, period)
        {
            _endpointUrl = endpointUrl;
            _sourceName = sourceName;
            _textFormatter = textFormatter;

            _httpClient = new HttpClient();
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            var tasks = events.Select(GetStringContent)
                .Select(content => _httpClient.PostAsync(_endpointUrl, content));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            _httpClient?.Dispose();
        }

        protected string GetFormattedLog(LogEvent logEvent)
        {
            if(logEvent == null)
                throw new ArgumentNullException(nameof(logEvent));

            using (var stringWriter = new StringWriter())
            {
                _textFormatter.Format(logEvent, stringWriter);
                return stringWriter.ToString();
            }
        }

        protected StringContent GetStringContent(LogEvent logEvent)
        {
            var formattedLog = GetFormattedLog(logEvent);
            var content = new StringContent(formattedLog, Encoding.UTF8, "text/plain");
            content.Headers.Add("X-Sumo-Name", _sourceName);

            return content;
        }
    }
}