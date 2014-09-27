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

namespace StarMQ.Exception
{
    using System;

    [Serializable]
    public class MaxLengthException : StarMqException
    {
        private const string Template = "Exceeded max length of 255 chars for field '{0}'. Value: '{1}'";

        public MaxLengthException(string field, string value)
            : base(String.Format(Template, field, value))
        {
        }
    }
}
