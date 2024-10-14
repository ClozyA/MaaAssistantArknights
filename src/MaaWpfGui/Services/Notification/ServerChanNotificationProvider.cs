// <copyright file="ServerChanNotificationProvider.cs" company="MaaAssistantArknights">
// MaaWpfGui - A part of the MaaCoreArknights project
// Copyright (C) 2021 MistEO and Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License v3.0 only as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY
// </copyright>

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using MaaWpfGui.Constants;
using MaaWpfGui.Helper;
using Serilog;

namespace MaaWpfGui.Services.Notification
{
    public class ServerChanNotificationProvider : IExternalNotificationProvider
    {
        private readonly ILogger _logger = Log.ForContext<ServerChanNotificationProvider>();

        public async Task<bool> SendAsync(string title, string content)
        {
            var sendKey = ConfigurationHelper.GetValue(ConfigurationKeys.ExternalNotificationServerChanSendKey, string.Empty);

            // 根据 sendkey 的前缀选择不同的 API URL
            var url = sendKey.StartsWith("sctp") 
                ? $"https://{sendKey}.push.ft07.com/send" 
                : $"https://sctapi.ftqq.com/{sendKey}.send";

            var postContent = new ServerChanPostContent { Title = title, Content = content };

            // 创建 HttpRequestMessage 并设置请求头
            using (var client = new HttpClient())
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(url))
                {
                    Content = new StringContent(JsonSerializer.Serialize(postContent), System.Text.Encoding.UTF8, "application/json")
                };
                requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // 发送请求
                var response = await client.SendAsync(requestMessage);
                var responseString = await response.Content.ReadAsStringAsync();

                var responseRoot = JsonDocument.Parse(responseString).RootElement;
                var hasCodeProperty = responseRoot.TryGetProperty("code", out var codeElement);

                if (!hasCodeProperty)
                {
                    _logger.Warning("Failed to send ServerChan notification, unknown response: {Response}", responseString);
                    return false;
                }

                var hasCode = codeElement.TryGetInt32(out var code);
                if (!hasCode)
                {
                    _logger.Warning("Failed to send ServerChan notification, unknown code in response: {Response}", responseString);
                    return false;
                }

                if (code == 0)
                {
                    return true;
                }

                _logger.Warning("Failed to send ServerChan notification, code: {Code}", code);
                return false;
            }
        }

        private class ServerChanPostContent
        {
            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("desp")]
            public string Content { get; set; }
        }
    }
}
