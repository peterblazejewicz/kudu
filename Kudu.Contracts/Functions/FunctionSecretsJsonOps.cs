﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Kudu.Core.Functions
{
    public class FunctionSecretsJsonOps : IKeyJsonOps<FunctionSecrets>
    {
        public string HostString { get; set; }
        public int RequireKeyCount()
        {
            return 1;
        }

        // have the schema related info enclosed in this class
        public string GenerateKeyUglyJson(Tuple<string,string>[] keyPair, out string unencryptedKey)
        {
            unencryptedKey = keyPair[0].Item1;
            switch (MasterKeyJsonOps.GetVersion(JObject.Parse(this.HostString)))
            {
                case 0:
                    return $"{{\"key\":\"{unencryptedKey}\"}}";
                case 1:
                    return $"{{\"keys\":[{{\"name\":\"default\",\"value\":\"{keyPair[0].Item2}\",\"encrypted\": true }}]}}";
                default:
                    throw new FormatException("Invalid secrets file format.");
            }
            
        }

        public string GetKeyInString(string json, out bool isEncrypted)
        {
            try
            {
                JObject hostJson = JObject.Parse(json);
                if (hostJson["key"]?.Type == JTokenType.String)
                {
                    isEncrypted = false;
                    return hostJson.Value<string>("key");
                }
                else if (hostJson["keys"]?.Type == JTokenType.Array)
                {
                    JArray keys = hostJson.Value<JArray>("keys");
                    if (keys.Count >= 1)
                    {
                        JObject keyObject = (JObject)keys[0];
                        for (int i = 1; i < keys.Count; i++)
                        {
                            // start from the second
                            // if we can't find the key named default, return the 1st key found
                            if (String.Equals(keys[i].Value<string>("name"), "default"))
                            {
                                keyObject = (JObject)keys[i];
                                break;
                            }
                        }
                        isEncrypted = keyObject.Value<bool>("encrypted");
                        return keyObject.Value<string>("value");
                    }
                }
            }
            catch (JsonException)
            {
                // all parse issue ==> format exception
                throw new FormatException("Invalid secrets file format.");
            }
            isEncrypted = false;
            return null;
        }

        public FunctionSecrets GenerateKeyObject(string functionKey, string functionName)
        {
            return new FunctionSecrets {
                Key = functionKey,
                TriggerUrl = String.Format(@"https://{0}/api/{1}?code={2}", System.Environment.GetEnvironmentVariable("WEBSITE_HOSTNAME") ?? "localhost", functionName, functionKey)
            };
        }
    }
}
