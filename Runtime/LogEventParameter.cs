using System.Collections.Generic;

namespace Unity.Firebase
{
    /// <summary>
    /// Tham số sự kiện phân tích dạng Fluent API tương thích với cấu trúc log của GitLab Service Firebase.
    /// </summary>
    public class LogEventParameter
    {
        public string Name { get; private set; }
        public Dictionary<string, object> Params { get; private set; }

        public LogEventParameter(string eventName)
        {
            Name = eventName;
            Params = new Dictionary<string, object>();
        }

        public LogEventParameter AddParam(string key, object value)
        {
            if (!string.IsNullOrEmpty(key) && value != null)
            {
                Params[key] = value;
            }
            return this;
        }

        public LogEventParameter AddParam(string key, int value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                Params[key] = value;
            }
            return this;
        }

        public LogEventParameter AddParam(string key, float value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                Params[key] = value;
            }
            return this;
        }

        public LogEventParameter AddParam(string key, double value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                Params[key] = value;
            }
            return this;
        }

        public LogEventParameter AddParam(string key, string value)
        {
            if (!string.IsNullOrEmpty(key) && value != null)
            {
                Params[key] = value;
            }
            return this;
        }

        public LogEventParameter AddParam(string key, bool value)
        {
            if (!string.IsNullOrEmpty(key))
            {
                Params[key] = value;
            }
            return this;
        }
    }
}
