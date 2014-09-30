#region Apache License v2.0
//Copyright 2014 Stephen Yu

//Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
//in compliance with the License. You may obtain a copy of the License at

//http://www.apache.org/licenses/LICENSE-2.0

//Unless required by applicable law or agreed to in writing, software distributed under the License
//is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
//or implied. See the License for the specific language governing permissions and limitations under
//the License.
#endregion

namespace StarMQ
{
    using System.Diagnostics.CodeAnalysis;

    public interface ILog
    {
        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Warn(string message, System.Exception ex);
        void Error(string message);
        void Error(string message, System.Exception ex);
    }

    [ExcludeFromCodeCoverage]
    internal class EmptyLog : ILog
    {
        public void Debug(string message)
        {
        }

        public void Info(string message)
        {
        }

        public void Warn(string message)
        {
        }

        public void Warn(string message, System.Exception ex)
        {
        }

        public void Error(string message)
        {
        }

        public void Error(string message, System.Exception ex)
        {
        }
    }

    //public class Log4NetLogger : ILog
    //{
    //    private readonly log4net.ILog _log;

    //    public Log4NetLogger(Type type)
    //    {
    //        _log = LogManager.GetLogger(type);
    //    }

    //    public void Debug(string message)
    //    {
    //        _log.Debug(message);
    //    }

    //    public void Info(string message)
    //    {
    //        _log.Info(message);
    //    }

    //    public void Warn(string message)
    //    {
    //        _log.Warn(message);
    //    }

    //    public void Warn(string message, Exception ex)
    //    {
    //        _log.Warn(message, ex);
    //    }

    //    public void Error(string message)
    //    {
    //        _log.Error(message);
    //    }

    //    public void Error(string message, Exception ex)
    //    {
    //        _log.Error(message, ex);
    //    }
    //}
}