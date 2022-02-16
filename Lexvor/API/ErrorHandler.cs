using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;
using Sentry;
using Sentry.Protocol;

namespace Lexvor.API {
    public static class ErrorHandler {
        /// <summary>
        /// This method reads the Sentry DSM from environment variables. Make sure the env is set before calling this.
        /// If there is no dsn in the env, this will throw an exception.
        /// </summary>
        public static void StaticCapture(Exception ex, HttpContext context = null, string area = "Application", Dictionary<string, string> customData = null) {
            var dsn = Environment.GetEnvironmentVariable("APPSETTING_SentryDSN");
            //if (!string.IsNullOrWhiteSpace(dsn)) {
            //    if (context == null) {
            //        Capture(dsn, ex, area, customData);
            //    } else {
            //        Capture(dsn, ex, context, area, customData);
            //    }
            //}

            if (dsn == null) {
                return;
            }

            if (context == null) {
                Capture(dsn, ex, area, customData);
            } else {
                Capture(dsn, ex, context, area, customData);
            }
        }

        public static string Capture(string dsn, Exception ex, HttpContext context, string area = "Application", Dictionary<string, string> customData = null) {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ip = GetRequestIP(context);
            var errorCode = RandomString.Get(6);

            var client = new SentryClient(new SentryOptions() {
                Dsn = new Dsn(dsn),
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                Release = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
            });
            var tags = new Dictionary<string, object>() {
                {"Area", area},
                {"IP", ip},
                {"AccountID", userId},
                {"ErrorCode", errorCode}
            };

            if (customData != null) {
                foreach (var data in customData) {
                    // Will add or overwrite
                    tags[data.Key] = data.Value;
                }
            }

            var e = new SentryEvent(ex) {
                User = new User() { Email = context.User.Identity.Name }
            };
            e.SetExtras(tags);
            client.CaptureEvent(e);

            return errorCode;
        }

        public static void Capture(string dsn, Exception ex, string area = "Application", Dictionary<string, string> customData = null) {
            var client = new SentryClient(new SentryOptions() {
                Dsn = new Dsn(dsn),
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                Release = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
            });

            var tags = new Dictionary<string, object>() {
                {"Area", area}
            };

            if (customData != null) {
                foreach (var data in customData) {
                    // Will add or overwrite
                    tags[data.Key] = data.Value;
                }
            }

            var e = new SentryEvent(ex);
            e.SetExtras(tags);
            client.CaptureEvent(e);
        }

        public static string GetRequestIP(HttpContext context) {
            var ip = context.Connection.RemoteIpAddress.ToString();

            if (string.IsNullOrEmpty(ip) &&
                (context.Request?.Headers?.TryGetValue("X-Forwarded-For", out var values) ?? false)) {
                string rawValues = values.ToString();
                ip = rawValues.SplitCsv().FirstOrDefault();
            }

            return ip;
        }

        public static List<string> SplitCsv(this string csvList, bool nullOrWhitespaceInputReturnsNull = false) {
            if (string.IsNullOrWhiteSpace(csvList))
                return nullOrWhitespaceInputReturnsNull ? null : new List<string>();

            return csvList
                .TrimEnd(',')
                .Split(',')
                .AsEnumerable<string>()
                .Select(s => s.Trim())
                .ToList();
        }
    }
}
