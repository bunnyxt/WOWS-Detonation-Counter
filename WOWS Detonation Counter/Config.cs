﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WOWS_Detonation_Counter
{
    public class Config
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("mySQLDatabase")]
        public MySqlDatabase MySqlDatabase { get; set; }

        [JsonProperty("mode")]
        public int Mode { get; set; }

        [JsonProperty("mode1")]
        public Mode1 Mode1 { get; set; }

        [JsonProperty("mode2")]
        public Mode2 Mode2 { get; set; }

        [JsonProperty("mode3")]
        public Mode3 Mode3 { get; set; }

        [JsonProperty("mode4")]
        public Mode4 Mode4 { get; set; }

        [JsonProperty("mode5")]
        public Mode5 Mode5 { get; set; }

        [JsonProperty("mail")]
        public Mail Mail { get; set; }

        public static Config FromJson(string json) => JsonConvert.DeserializeObject<Config>(json, Converter.Settings);
    }

    public class Mode1
    {
        [JsonProperty("targetSum")]
        public int TargetSum { get; set; }
    }

    public class Mode2
    {
        [JsonProperty("rangeMin")]
        public int RangeMin { get; set; }

        [JsonProperty("rangeMax")]
        public int RangeMax { get; set; }
    }

    public class Mode3
    {
        [JsonProperty("minId")]
        public int MinId { get; set; }

        [JsonProperty("maxId")]
        public int MaxId { get; set; }
    }

    public class Mode4
    {
        [JsonProperty("targetMin")]
        public int TargetMin { get; set; }

        [JsonProperty("targetMax")]
        public int TargetMax { get; set; }
    }

    public class Mode5
    {
        [JsonProperty("targetMinAccountId")]
        public int TargetMinAccountId { get; set; }

        [JsonProperty("targetMinId")]
        public int TargetMinId { get; set; }

        [JsonProperty("targetMaxId")]
        public int TargetMaxId { get; set; }
    }

    public class MySqlDatabase
    {
        [JsonProperty("server")]
        public string Server { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("database")]
        public string Database { get; set; }
    }

    public class Mail
    {
        [JsonProperty("sender")]
        public string Sender { get; set; }

        [JsonProperty("receiver")]
        public string Receiver { get; set; }

        [JsonProperty("clientHost")]
        public string ClientHost { get; set; }

        [JsonProperty("creditCode")]
        public string CreditCode { get; set; }
    }

    public static class Serialize
    {
        public static string ToJson(this Config self) => JsonConvert.SerializeObject(self, Converter.Settings);
    }

    public class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
        };
    }
}
